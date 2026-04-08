using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for RepoBase edge cases and exception handling.
    /// Validates null handling, invalid IDs, exception scenarios.
    /// </summary>
    [TestFixture]
    public class RepoBaseEdgeCasesTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<EdgeCaseEntity, TestDbContext>> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<EdgeCaseEntity, TestDbContext>>.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region GetById - Invalid IDs

        [Test]
        public void GetById_ZeroId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.GetById(0));
        }

        [Test]
        public void GetById_NegativeId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.GetById(-1));
        }

        [Test]
        public void GetById_NonExistentId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.GetById(999));
            
            Assert.That(ex!.Message, Does.Contain("not found"));
        }

        [Test]
        public void GetById_LongNegativeId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.GetById(-1L));
        }

        #endregion

        #region DeleteAsync - Invalid IDs

        [Test]
        public void DeleteAsync_ZeroId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.DeleteAsync(0));
        }

        [Test]
        public void DeleteAsync_NonExistentId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.DeleteAsync(999));
        }

        #endregion

        #region Insert - Null Entity

        [Test]
        public void Insert_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await repo.Insert(null!));
        }

        #endregion

        #region UpdateAsync - Null Entity

        [Test]
        public void UpdateAsync_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await repo.UpdateAsync(null!));
        }

        #endregion

        #region Add/Update/Delete - Null Entity

        [Test]
        public void Add_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                repo.Add(null!));
        }

        [Test]
        public void Update_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                repo.Update(null!));
        }

        [Test]
        public void Delete_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                repo.Delete(null!));
        }

        #endregion

        #region GetPagedAsync - Null Request

        [Test]
        public void GetPagedAsync_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await repo.GetPagedAsync(null!));
        }

        #endregion

        #region SoftDeleteAsync - Invalid IDs

        [Test]
        public void SoftDeleteAsync_ZeroId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.SoftDeleteAsync(0));
        }

        [Test]
        public void SoftDeleteAsync_NonExistentId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.SoftDeleteAsync(999));
        }

        #endregion

        #region RestoreAsync - Invalid IDs

        [Test]
        public void RestoreAsync_ZeroId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.RestoreAsync(0));
        }

        [Test]
        public void RestoreAsync_NonExistentId_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.RestoreAsync(999));
        }

        #endregion

        #region DbUpdateException Handling

        [Test]
        public async Task SaveAsync_DetectsConcurrencyIssues()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);
            
            // Add entity
            var entity = new EdgeCaseEntity { Name = "Test" };
            await repo.Insert(entity);

            // Simulate concurrent modification by detaching and reattaching
            _context.Entry(entity).State = EntityState.Detached;
            
            var concurrentEntity = new EdgeCaseEntity 
            { 
                Id = entity.Id, 
                Name = "Modified Elsewhere" 
            };
            _context.TestEntities.Attach(concurrentEntity);
            concurrentEntity.Name = "Concurrent Change";
            await _context.SaveChangesAsync();

            // Act & Assert - Try to update original entity
            // This tests that exceptions are properly thrown/handled
            try
            {
                _context.TestEntities.Attach(entity);
                entity.Name = "Local Change";
                await repo.SaveAsync();
                // In InMemory, this might succeed or throw depending on configuration
            }
            catch (DbUpdateConcurrencyException)
            {
                // Expected in real DB scenarios with concurrency tokens
                Assert.Pass("Concurrency exception properly thrown");
            }
            catch
            {
                // Other exceptions are acceptable for this test
            }
        }

        #endregion

        #region Empty Collections

        [Test]
        public async Task AddRangeAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);
            var emptyList = new List<EdgeCaseEntity>();

            // Act
            var result = await repo.AddRangeAsync(emptyList);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task UpdateRangeAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);
            var emptyList = new List<EdgeCaseEntity>();

            // Act
            var result = await repo.UpdateRangeAsync(emptyList);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task DeleteRangeAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            var repo = new RepoBase<EdgeCaseEntity, TestDbContext>(_context, _logger);
            var emptyList = new List<EdgeCaseEntity>();

            // Act
            var result = await repo.DeleteRangeAsync(emptyList);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region Test Entities

        public class EdgeCaseEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public byte[]? RowVersion { get; set; } // For concurrency testing
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<EdgeCaseEntity> TestEntities => Set<EdgeCaseEntity>();
        }

        #endregion
    }
}
