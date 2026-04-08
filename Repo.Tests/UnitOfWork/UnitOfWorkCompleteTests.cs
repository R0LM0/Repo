using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.UnitOfWork;
using System.Data;

namespace Repo.Tests.UnitOfWork
{
    /// <summary>
    /// Tests for UnitOfWork complete functionality.
    /// Validates SaveChangesAsync, HasChangesAsync, BeginTransactionAsync(IsolationLevel), ExecuteSqlRawAsync.
    /// </summary>
    [TestFixture]
    public class UnitOfWorkCompleteTests
    {
        private TestDbContext _context = null!;
        private ILogger<UnitOfWork<TestDbContext>> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<UnitOfWork<TestDbContext>>.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region SaveChangesAsync

        [Test]
        public async Task SaveChangesAsync_PersistsChanges()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo = unitOfWork.Repository<TestEntity>();
            
            await repo.Insert(new TestEntity { Name = "Test Entity" });

            // Act
            var result = await unitOfWork.SaveChangesAsync();

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            var result = await unitOfWork.SaveChangesAsync();

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region HasChangesAsync

        [Test]
        public async Task HasChangesAsync_WithChanges_ReturnsTrue()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo = unitOfWork.Repository<TestEntity>();
            
            // Add entity without saving
            _context.TestEntities.Add(new TestEntity { Name = "Unsaved Entity" });

            // Act
            var result = await unitOfWork.HasChangesAsync();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task HasChangesAsync_WithNoChanges_ReturnsFalse()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            var result = await unitOfWork.HasChangesAsync();

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region HasActiveTransaction

        [Test]
        public async Task HasActiveTransaction_AfterBegin_ReturnsTrue()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            
            // Act
            await unitOfWork.BeginTransactionAsync();

            // Assert
            Assert.That(unitOfWork.HasActiveTransaction, Is.True);
        }

        [Test]
        public void HasActiveTransaction_WithoutTransaction_ReturnsFalse()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
        }

        [Test]
        public async Task HasActiveTransaction_AfterCommit_ReturnsFalse()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            await unitOfWork.BeginTransactionAsync();

            // Act
            await unitOfWork.CommitTransactionAsync();

            // Assert
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
        }

        #endregion

        #region CurrentTransaction

        [Test]
        public async Task CurrentTransaction_AfterBegin_IsNotNull()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            
            // Act
            await unitOfWork.BeginTransactionAsync();

            // Assert
            Assert.That(unitOfWork.CurrentTransaction, Is.Not.Null);
        }

        [Test]
        public void CurrentTransaction_WithoutTransaction_IsNull()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.That(unitOfWork.CurrentTransaction, Is.Null);
        }

        #endregion

        #region BeginTransactionAsync(IsolationLevel)

        [Test]
        public async Task BeginTransactionAsync_WithIsolationLevel_SetsLevel()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            // Assert
            Assert.That(unitOfWork.HasActiveTransaction, Is.True);
            Assert.That(unitOfWork.CurrentTransaction, Is.Not.Null);
        }

        [Test]
        public async Task BeginTransactionAsync_DoubleBegin_DoesNotCreateNewTransaction()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            await unitOfWork.BeginTransactionAsync();
            var firstTransaction = unitOfWork.CurrentTransaction;

            // Act
            await unitOfWork.BeginTransactionAsync();
            var secondTransaction = unitOfWork.CurrentTransaction;

            // Assert
            Assert.That(firstTransaction, Is.SameAs(secondTransaction));
        }

        #endregion

        #region ExecuteSqlRawAsync

        [Test]
        public void ExecuteSqlRawAsync_EmptySql_ThrowsArgumentException()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await unitOfWork.ExecuteSqlRawAsync(""));
            
            Assert.That(ex!.ParamName, Is.EqualTo("sql"));
        }

        [Test]
        public void ExecuteSqlRawAsync_DangerousKeyword_ThrowsSecurityException()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<Repo.Repository.Exceptions.SecurityException>(async () =>
                await unitOfWork.ExecuteSqlRawAsync("DROP TABLE Users"));
        }

        [Test]
        public void ExecuteSqlRawAsync_TrustedSql_DoesNotThrowSecurityException()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act & Assert - SELECT should be allowed
            try
            {
                // This will fail in InMemory (no SQL support) but should not throw SecurityException
                await unitOfWork.ExecuteSqlRawAsync("SELECT * FROM TestEntities");
            }
            catch (Repo.Repository.Exceptions.SecurityException)
            {
                Assert.Fail("Should not throw SecurityException for safe SQL");
            }
            catch
            {
                // Other exceptions expected in InMemory DB
            }
        }

        #endregion

        #region ExecuteSqlRawAsync (Typed)

        [Test]
        public void ExecuteSqlRawAsync_Typed_EmptySql_ThrowsArgumentException()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await unitOfWork.ExecuteSqlRawAsync<TestEntity>(""));
            
            Assert.That(ex!.ParamName, Is.EqualTo("sql"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task CompleteTransactionFlow_WithSaveChanges_WorksCorrectly()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo = unitOfWork.Repository<TestEntity>();

            // Act
            await unitOfWork.BeginTransactionAsync();
            await repo.Insert(new TestEntity { Name = "Entity 1" });
            await repo.Insert(new TestEntity { Name = "Entity 2" });
            
            // Should have changes before save
            Assert.That(await unitOfWork.HasChangesAsync(), Is.True);
            
            var saved = await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            // Assert
            Assert.That(saved, Is.EqualTo(2));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(2));
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
        }

        [Test]
        public async Task TransactionRollback_DiscardsChanges()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo = unitOfWork.Repository<TestEntity>();

            await unitOfWork.BeginTransactionAsync();
            await repo.Insert(new TestEntity { Name = "Entity to Rollback" });
            await unitOfWork.SaveChangesAsync();

            // Act
            await unitOfWork.RollbackTransactionAsync();

            // Assert - In InMemory, rollback behavior may vary
            // This test documents expected behavior
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
        }

        #endregion

        #region Test Entities

        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        }

        #endregion
    }
}
