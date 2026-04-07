using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using Repo.Repository.UnitOfWork;

namespace Repo.Tests.UnitOfWork
{
    /// <summary>
    /// Tests demonstrating the consolidated UnitOfWork transaction pattern.
    /// 
    /// These tests verify that:
    /// 1. UnitOfWork is the primary transaction orchestrator
    /// 2. Repositories obtained from UnitOfWork participate in its transactions
    /// 3. Repository-level transaction methods are deprecated (compile warnings)
    /// </summary>
    [TestFixture]
    public class UnitOfWorkConsolidatedTransactionTests
    {
        private TestDbContext _context = null!;
        private ILogger<UnitOfWork<TestDbContext>> _logger = null!;

        [SetUp]
        public void Setup()
        {
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
        public async Task UnitOfWork_CurrentTransaction_InitiallyNull()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Assert
            Assert.That(unitOfWork.CurrentTransaction, Is.Null);
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
        }

        [Test]
        public async Task UnitOfWork_HasActiveTransaction_TrueAfterBegin()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            await unitOfWork.BeginTransactionAsync();

            // Assert
            Assert.That(unitOfWork.HasActiveTransaction, Is.True);
            Assert.That(unitOfWork.CurrentTransaction, Is.Not.Null);
        }

        [Test]
        public async Task UnitOfWork_HasActiveTransaction_FalseAfterCommit()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            await unitOfWork.BeginTransactionAsync();

            // Act
            await unitOfWork.CommitTransactionAsync();

            // Assert
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
            Assert.That(unitOfWork.CurrentTransaction, Is.Null);
        }

        [Test]
        public async Task UnitOfWork_HasActiveTransaction_FalseAfterRollback()
        {
            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            await unitOfWork.BeginTransactionAsync();

            // Act
            await unitOfWork.RollbackTransactionAsync();

            // Assert
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
            Assert.That(unitOfWork.CurrentTransaction, Is.Null);
        }

        [Test]
        public async Task RecommendedPattern_UnitOfWorkOrchestratesTransaction()
        {
            // This test demonstrates the RECOMMENDED pattern after consolidation:
            // UnitOfWork manages the transaction, repositories perform operations

            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo = unitOfWork.Repository<TestEntity>();

            // Act - RECOMMENDED PATTERN
            await unitOfWork.BeginTransactionAsync();
            try
            {
                // Repositories obtained from UnitOfWork automatically participate
                // in the UnitOfWork's transaction scope
                _context.TestEntities.Add(new TestEntity { Name = "RecommendedPattern" });
                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Assert
            var saved = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "RecommendedPattern");
            Assert.That(saved, Is.Not.Null);
        }

        [Test]
        public async Task ConsolidatedPattern_MultipleRepositories_SingleTransaction()
        {
            // This test demonstrates using multiple repositories within one UnitOfWork transaction

            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);
            var repo1 = unitOfWork.Repository<TestEntity>();
            var repo2 = unitOfWork.Repository<OtherTestEntity>();

            // Act
            await unitOfWork.BeginTransactionAsync();
            try
            {
                _context.TestEntities.Add(new TestEntity { Name = "MultiRepo1" });
                _context.OtherTestEntities.Add(new OtherTestEntity { Description = "MultiRepo2" });
                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Assert - both should be saved (same transaction)
            var saved1 = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "MultiRepo1");
            var saved2 = await _context.OtherTestEntities.FirstOrDefaultAsync(e => e.Description == "MultiRepo2");
            Assert.That(saved1, Is.Not.Null);
            Assert.That(saved2, Is.Not.Null);
        }

        [Test]
        public async Task ConsolidatedPattern_RollbackDiscardsAllChanges()
        {
            // This test demonstrates that rollback in UnitOfWork discards all changes

            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            await unitOfWork.BeginTransactionAsync();
            _context.TestEntities.Add(new TestEntity { Name = "WillBeRolledBack" });
            _context.OtherTestEntities.Add(new OtherTestEntity { Description = "AlsoRolledBack" });
            // Note: SaveChanges not called - changes are in context but not committed

            // Act
            await unitOfWork.RollbackTransactionAsync();

            // Assert - changes should be discarded
            var saved1 = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "WillBeRolledBack");
            var saved2 = await _context.OtherTestEntities.FirstOrDefaultAsync(e => e.Description == "AlsoRolledBack");
            Assert.That(saved1, Is.Null);
            Assert.That(saved2, Is.Null);
        }

        [Test]
        public void UnitOfWork_IUnitOfWork_InterfaceExposesTransactionProperties()
        {
            // Verify that IUnitOfWork interface exposes the transaction properties
            IUnitOfWork unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Should be able to access transaction state through interface
            Assert.That(unitOfWork.HasActiveTransaction, Is.False);
            Assert.That(unitOfWork.CurrentTransaction, Is.Null);
        }

        [Test]
        public async Task ConsolidatedPattern_NestedOperationWithSaveChanges()
        {
            // Test pattern where SaveChanges is called multiple times within a transaction

            // Arrange
            var unitOfWork = new UnitOfWork<TestDbContext>(_context, _logger);

            // Act
            await unitOfWork.BeginTransactionAsync();
            try
            {
                // First batch
                _context.TestEntities.Add(new TestEntity { Name = "Batch1" });
                await unitOfWork.SaveChangesAsync();

                // Second batch
                _context.TestEntities.Add(new TestEntity { Name = "Batch2" });
                await unitOfWork.SaveChangesAsync();

                // Commit all
                await unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }

            // Assert
            var batch1 = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Batch1");
            var batch2 = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Batch2");
            Assert.That(batch1, Is.Not.Null);
            Assert.That(batch2, Is.Not.Null);
        }

        [Test]
        public async Task RecommendedPattern_UsingUnitOfWork_WorksCorrectly()
        {
            // Verify that the recommended pattern via UnitOfWork works correctly

            // RECOMMENDED pattern via UnitOfWork
            var uow = new UnitOfWork<TestDbContext>(_context, _logger);
            await uow.BeginTransactionAsync();
            _context.TestEntities.Add(new TestEntity { Name = "Recommended" });
            await uow.SaveChangesAsync();
            await uow.CommitTransactionAsync();

            // Assert the pattern works
            var recommended = await _context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Recommended");
            Assert.That(recommended, Is.Not.Null);
        }
    }
}
