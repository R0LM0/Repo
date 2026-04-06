using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.UnitOfWork;

namespace Repo.Tests.StoredProcedures
{
    [TestFixture]
    public class StoredProcedureIntegrationTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<TestEntity, TestDbContext>> _logger = null!;
        private RepoBase<TestEntity, TestDbContext> _repo = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            _context = new TestDbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();

            // Seed test data
            _context.TestEntities.AddRange(
                new TestEntity { Name = "Entity1", Value = 100 },
                new TestEntity { Name = "Entity2", Value = 200 },
                new TestEntity { Name = "Entity3", Value = 300 }
            );
            _context.SaveChanges();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<RepoBase<TestEntity, TestDbContext>>();
            _repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            _repo.Dispose();
            _context.Database.CloseConnection();
            _context.Dispose();
        }

        [Test]
        public async Task ExecuteStoredProcedureAsync_RawSqlQuery_ReturnsResults()
        {
            // Arrange
            var sql = "SELECT * FROM TestEntities WHERE Value > {0}";

            // Act
            var results = await _repo.ExecuteStoredProcedureAsync<TestEntity>(sql, default, 150);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(2));
            Assert.That(results.Any(e => e.Name == "Entity2"), Is.True);
            Assert.That(results.Any(e => e.Name == "Entity3"), Is.True);
        }

        [Test]
        public async Task ExecuteStoredProcedureAsync_WithParameters_ReturnsFilteredResults()
        {
            // Arrange
            var sql = "SELECT * FROM TestEntities WHERE Name = {0}";

            // Act
            var results = await _repo.ExecuteStoredProcedureAsync<TestEntity>(sql, default, "Entity1");

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
            Assert.That(results.First().Name, Is.EqualTo("Entity1"));
        }

        [Test]
        public async Task ExecuteStoredProcedureNonQueryAsync_Update_ModifiesRows()
        {
            // Arrange
            var sql = "UPDATE TestEntities SET Value = Value + 10 WHERE Value < 200";

            // Act
            var affectedRows = await _repo.ExecuteStoredProcedureNonQueryAsync(sql);

            // Assert
            Assert.That(affectedRows, Is.EqualTo(1));
            
            var updatedEntity = await _context.TestEntities.FindAsync(1);
            Assert.That(updatedEntity!.Value, Is.EqualTo(110));
        }

        [Test]
        public async Task ExecuteStoredProcedureNonQueryAsync_Delete_RemovesRows()
        {
            // Arrange
            var sql = "DELETE FROM TestEntities WHERE Name = 'Entity3'";

            // Act
            var affectedRows = await _repo.ExecuteStoredProcedureNonQueryAsync(sql);

            // Assert
            Assert.That(affectedRows, Is.EqualTo(1));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task ExecuteStoredProcedureNonQueryAsync_Insert_AddsRows()
        {
            // Arrange
            var sql = "INSERT INTO TestEntities (Name, Value) VALUES ('Entity4', 400)";

            // Act
            var affectedRows = await _repo.ExecuteStoredProcedureNonQueryAsync(sql);

            // Assert
            Assert.That(affectedRows, Is.EqualTo(1));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(4));
            
            var newEntity = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Entity4");
            Assert.That(newEntity, Is.Not.Null);
            Assert.That(newEntity!.Value, Is.EqualTo(400));
        }

        [Test]
        public void ExecuteStoredProcedureAsync_EmptyStoredProcedureName_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _repo.ExecuteStoredProcedureAsync<TestEntity>("", default, 1));
            
            Assert.That(ex!.ParamName, Is.EqualTo("storedProcedure"));
        }

        [Test]
        public void ExecuteStoredProcedureNonQueryAsync_EmptyStoredProcedureName_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _repo.ExecuteStoredProcedureNonQueryAsync(""));
            
            Assert.That(ex!.ParamName, Is.EqualTo("storedProcedure"));
        }

        [Test]
        public async Task ExecuteStoredProcedureAsync_NoMatchingResults_ReturnsEmpty()
        {
            // Arrange
            var sql = "SELECT * FROM TestEntities WHERE Value > 1000";

            // Act
            var results = await _repo.ExecuteStoredProcedureAsync<TestEntity>(sql);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Empty);
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
        }
    }
}
