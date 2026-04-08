using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for RepoBase Bulk operations.
    /// Validates AddRangeAsync, UpdateRangeAsync, DeleteRangeAsync.
    /// </summary>
    [TestFixture]
    public class RepoBaseBulkTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<BulkTestEntity, TestDbContext>> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<BulkTestEntity, TestDbContext>>.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region AddRangeAsync

        [Test]
        public async Task AddRangeAsync_MultipleEntities_AddsAll()
        {
            // Arrange
            var entities = new List<BulkTestEntity>
            {
                new() { Name = "Entity 1" },
                new() { Name = "Entity 2" },
                new() { Name = "Entity 3" }
            };
            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.AddRangeAsync(entities);

            // Assert
            Assert.That(result, Is.EqualTo(3));
            Assert.That(_context.BulkTestEntities.Count(), Is.EqualTo(3));
            Assert.That(entities.All(e => e.Id > 0), Is.True);
        }

        [Test]
        public async Task AddRangeAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            var entities = new List<BulkTestEntity>();
            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.AddRangeAsync(entities);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task AddRangeAsync_SingleEntity_AddsSuccessfully()
        {
            // Arrange
            var entities = new List<BulkTestEntity> { new() { Name = "Single Entity" } };
            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.AddRangeAsync(entities);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(_context.BulkTestEntities.Count(), Is.EqualTo(1));
        }

        #endregion

        #region UpdateRangeAsync

        [Test]
        public async Task UpdateRangeAsync_MultipleEntities_UpdatesAll()
        {
            // Arrange
            var entities = new List<BulkTestEntity>
            {
                new() { Id = 1, Name = "Original 1" },
                new() { Id = 2, Name = "Original 2" },
                new() { Id = 3, Name = "Original 3" }
            };
            _context.BulkTestEntities.AddRange(entities);
            _context.SaveChanges();

            // Modify entities
            foreach (var entity in entities)
            {
                entity.Name = $"Updated {entity.Id}";
            }

            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.UpdateRangeAsync(entities);

            // Assert
            Assert.That(result, Is.EqualTo(3));
            
            var updated = await _context.BulkTestEntities.ToListAsync();
            Assert.That(updated.All(e => e.Name.StartsWith("Updated")), Is.True);
        }

        [Test]
        public async Task UpdateRangeAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            var entities = new List<BulkTestEntity>();
            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.UpdateRangeAsync(entities);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region DeleteRangeAsync (Entities)

        [Test]
        public async Task DeleteRangeAsync_Entities_RemovesAll()
        {
            // Arrange
            var entities = new List<BulkTestEntity>
            {
                new() { Id = 1, Name = "Entity 1" },
                new() { Id = 2, Name = "Entity 2" },
                new() { Id = 3, Name = "Entity 3" }
            };
            _context.BulkTestEntities.AddRange(entities);
            _context.SaveChanges();

            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.DeleteRangeAsync(entities.Take(2).ToList());

            // Assert
            Assert.That(result, Is.EqualTo(2));
            Assert.That(_context.BulkTestEntities.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task DeleteRangeAsync_Entities_EmptyList_ReturnsZero()
        {
            // Arrange
            var entities = new List<BulkTestEntity>();
            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.DeleteRangeAsync(entities);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region DeleteRangeAsync (Predicate)

        [Test]
        public async Task DeleteRangeAsync_Predicate_RemovesMatching()
        {
            // Arrange
            var entities = new List<BulkTestEntity>
            {
                new() { Id = 1, Name = "To Delete", IsActive = false },
                new() { Id = 2, Name = "To Keep", IsActive = true },
                new() { Id = 3, Name = "To Delete", IsActive = false }
            };
            _context.BulkTestEntities.AddRange(entities);
            _context.SaveChanges();

            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.DeleteRangeAsync(e => !e.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(2));
            Assert.That(_context.BulkTestEntities.Count(), Is.EqualTo(1));
            Assert.That(_context.BulkTestEntities.First().IsActive, Is.True);
        }

        [Test]
        public async Task DeleteRangeAsync_Predicate_NoMatch_ReturnsZero()
        {
            // Arrange
            var entities = new List<BulkTestEntity>
            {
                new() { Id = 1, Name = "Active", IsActive = true },
                new() { Id = 2, Name = "Active 2", IsActive = true }
            };
            _context.BulkTestEntities.AddRange(entities);
            _context.SaveChanges();

            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.DeleteRangeAsync(e => !e.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            Assert.That(_context.BulkTestEntities.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task DeleteRangeAsync_Predicate_EmptyTable_ReturnsZero()
        {
            // Arrange
            var repo = new RepoBase<BulkTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.DeleteRangeAsync(e => !e.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region Test Entities

        public class BulkTestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; } = true;
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<BulkTestEntity> BulkTestEntities => Set<BulkTestEntity>();
        }

        #endregion
    }
}
