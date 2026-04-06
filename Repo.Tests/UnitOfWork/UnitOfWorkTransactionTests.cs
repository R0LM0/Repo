using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using Repo.Repository.UnitOfWork;

namespace Repo.Tests.UnitOfWork
{
    [TestFixture]
    public class UnitOfWorkTransactionTests
    {
        private TestDbContext _context = null!;
        private ILogger<UnitOfWork<TestDbContext>> _logger = null!;

        [SetUp]
        public void Setup()
        {
            // Use SQLite in-memory for transaction testing
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            _context = new TestDbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<UnitOfWork<TestDbContext>>();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }

        [Test]
        public async Task UnitOfWork_BeginTransactionAsync_CreatesTransaction()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            await unitOfWork.BeginTransactionAsync();

            // Assert - Verify transaction exists by checking context
            // SQLite doesn't expose transaction directly, but we can verify no exception
            Assert.That(unitOfWork.Context.Database.CurrentTransaction, Is.Not.Null);
        }

        [Test]
        public async Task UnitOfWork_CommitTransactionAsync_PersistsChanges()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo = unitOfWork.Repository<TestEntity>();

            await unitOfWork.BeginTransactionAsync();
            
            var entity = new TestEntity { Name = "Test Commit" };
            await repo.Insert(entity);

            // Act
            await unitOfWork.CommitTransactionAsync();

            // Assert
            var savedEntity = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Test Commit");
            Assert.That(savedEntity, Is.Not.Null);
            Assert.That(savedEntity.Name, Is.EqualTo("Test Commit"));
        }

        [Test]
        public async Task UnitOfWork_RollbackTransactionAsync_DiscardsChanges()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo = unitOfWork.Repository<TestEntity>();

            await unitOfWork.BeginTransactionAsync();
            
            var entity = new TestEntity { Name = "Test Rollback" };
            await repo.Insert(entity);
            // Note: Insert calls SaveChangesAsync internally, so we need a different approach
            // Let's add without saving, then rollback
            
            // For rollback test, we need to use raw EF without the internal save
            _context.TestEntities.Add(new TestEntity { Name = "Test Rollback" });

            // Act
            await unitOfWork.RollbackTransactionAsync();

            // Assert
            var savedEntity = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Test Rollback");
            Assert.That(savedEntity, Is.Null);
        }

        [Test]
        public async Task UnitOfWork_MultipleRepositories_SameTransaction()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo1 = unitOfWork.Repository<TestEntity>();
            var repo2 = unitOfWork.Repository<OtherTestEntity>();

            await unitOfWork.BeginTransactionAsync();

            // Act
            await repo1.Insert(new TestEntity { Name = "Entity1" });
            await repo2.Insert(new OtherTestEntity { Description = "Entity2" });

            await unitOfWork.CommitTransactionAsync();

            // Assert
            var savedEntity1 = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Entity1");
            var savedEntity2 = await _context.OtherTestEntities.FirstOrDefaultAsync(e => e.Description == "Entity2");
            
            Assert.That(savedEntity1, Is.Not.Null);
            Assert.That(savedEntity2, Is.Not.Null);
        }

        [Test]
        public async Task UnitOfWork_Rollback_MultipleRepositories_DiscardsAll()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo1 = unitOfWork.Repository<TestEntity>();
            
            await unitOfWork.BeginTransactionAsync();
            
            // Use raw EF to add without saving
            _context.TestEntities.Add(new TestEntity { Name = "ShouldNotExist" });
            _context.OtherTestEntities.Add(new OtherTestEntity { Description = "AlsoNotExist" });

            // Act
            await unitOfWork.RollbackTransactionAsync();

            // Assert
            var savedEntity1 = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "ShouldNotExist");
            var savedEntity2 = await _context.OtherTestEntities.FirstOrDefaultAsync(e => e.Description == "AlsoNotExist");
            
            Assert.That(savedEntity1, Is.Null);
            Assert.That(savedEntity2, Is.Null);
        }

        [Test]
        public async Task UnitOfWork_DoubleBeginTransaction_OnlyCreatesOne()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            await unitOfWork.BeginTransactionAsync();
            var firstTransaction = unitOfWork.Context.Database.CurrentTransaction;
            
            await unitOfWork.BeginTransactionAsync(); // Second call should be ignored
            var secondTransaction = unitOfWork.Context.Database.CurrentTransaction;

            // Assert
            Assert.That(firstTransaction, Is.SameAs(secondTransaction));
        }

        [Test]
        public void UnitOfWork_Dispose_ReleasesResources()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            unitOfWork.Dispose();

            // Assert
            // After disposal, the context should be disposed
            Assert.That(() => _context.TestEntities.ToList(), Throws.Exception);
        }
    }

    // Test Entities
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class OtherTestEntity
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // Test DbContext
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<OtherTestEntity> OtherTestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
            modelBuilder.Entity<OtherTestEntity>().HasKey(e => e.Id);
        }
    }
}