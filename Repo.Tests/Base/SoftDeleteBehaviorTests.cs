using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for Soft Delete behavior (Issue #35).
    /// Validates soft delete, restore, and retrieval of deleted entities.
    /// </summary>
    [TestFixture]
    public class SoftDeleteBehaviorTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<SoftDeleteEntity, TestDbContext>> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<SoftDeleteEntity, TestDbContext>>.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region SoftDeleteAsync - int ID overload

        [Test]
        public async Task SoftDeleteAsync_IntId_SetsIsDeletedTrue()
        {
            // Arrange
            var entity = new SoftDeleteEntity { Id = 1, Name = "Test Entity" };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.SoftDeleteAsync(1, "TestUser");

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.IsDeleted, Is.True);
            Assert.That(entity.DeletedAt, Is.Not.Null);
            Assert.That(entity.DeletedBy, Is.EqualTo("TestUser"));
        }

        [Test]
        public async Task SoftDeleteAsync_IntId_WithoutDeletedBy_SetsIsDeleted()
        {
            // Arrange
            var entity = new SoftDeleteEntity { Id = 2, Name = "Test Entity" };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.SoftDeleteAsync(2);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.IsDeleted, Is.True);
            Assert.That(entity.DeletedAt, Is.Not.Null);
            Assert.That(entity.DeletedBy, Is.Null);
        }

        #endregion

        #region SoftDeleteAsync - long ID overload

        [Test]
        public async Task SoftDeleteAsync_LongId_SetsIsDeletedTrue()
        {
            // Arrange
            var entity = new SoftDeleteEntity { Id = 3, Name = "Test Entity" };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.SoftDeleteAsync(3L, "TestUser");

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.IsDeleted, Is.True);
            Assert.That(entity.DeletedAt, Is.Not.Null);
            Assert.That(entity.DeletedBy, Is.EqualTo("TestUser"));
        }

        #endregion

        #region SoftDeleteAsync - Entity overload

        [Test]
        public async Task SoftDeleteAsync_Entity_SetsIsDeletedTrue()
        {
            // Arrange
            var entity = new SoftDeleteEntity { Id = 4, Name = "Test Entity" };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.SoftDeleteAsync(entity, "TestUser");

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.IsDeleted, Is.True);
            Assert.That(entity.DeletedAt, Is.Not.Null);
            Assert.That(entity.DeletedBy, Is.EqualTo("TestUser"));
        }

        [Test]
        public async Task SoftDeleteAsync_Entity_WithoutDeletedBy_SetsIsDeleted()
        {
            // Arrange
            var entity = new SoftDeleteEntity { Id = 5, Name = "Test Entity" };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.SoftDeleteAsync(entity);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.IsDeleted, Is.True);
            Assert.That(entity.DeletedAt, Is.Not.Null);
        }

        #endregion

        #region SoftDeleteAsync - Non-ISoftDelete fallback

        [Test]
        public async Task SoftDeleteAsync_NonSoftDeleteEntity_PerformsHardDelete()
        {
            // Arrange
            var nonSoftDeleteLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<NonSoftDeleteEntity, TestDbContext>>.Instance;
            var entity = new NonSoftDeleteEntity { Id = 1, Name = "Test Entity" };
            _context.NonSoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<NonSoftDeleteEntity, TestDbContext>(_context, nonSoftDeleteLogger);

            // Act
            var result = await repo.SoftDeleteAsync(1, "TestUser");

            // Assert - Should hard delete since entity doesn't implement ISoftDelete
            Assert.That(result, Is.EqualTo(1));
            Assert.That(_context.NonSoftDeleteEntities.Find(1), Is.Null);
        }

        #endregion

        #region GetAllIncludingDeletedAsync

        [Test]
        public async Task GetAllIncludingDeletedAsync_ReturnsDeletedAndNonDeleted()
        {
            // Arrange
            var entity1 = new SoftDeleteEntity { Id = 10, Name = "Active" };
            var entity2 = new SoftDeleteEntity { Id = 11, Name = "Deleted", IsDeleted = true, DeletedAt = DateTime.UtcNow };
            _context.SoftDeleteEntities.AddRange(entity1, entity2);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.GetAllIncludingDeletedAsync();

            // Assert
            Assert.That(results.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetAllIncludingDeletedAsync_NonSoftDeleteEntity_ReturnsAll()
        {
            // Arrange
            var nonSoftDeleteLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<NonSoftDeleteEntity, TestDbContext>>.Instance;
            var entity1 = new NonSoftDeleteEntity { Id = 10, Name = "Entity1" };
            var entity2 = new NonSoftDeleteEntity { Id = 11, Name = "Entity2" };
            _context.NonSoftDeleteEntities.AddRange(entity1, entity2);
            _context.SaveChanges();

            var repo = new RepoBase<NonSoftDeleteEntity, TestDbContext>(_context, nonSoftDeleteLogger);

            // Act
            var results = await repo.GetAllIncludingDeletedAsync();

            // Assert - For non-ISoftDelete entities, delegates to GetAllAsync
            Assert.That(results.Count(), Is.EqualTo(2));
        }

        #endregion

        #region RestoreAsync - int ID overload

        [Test]
        public async Task RestoreAsync_IntId_RestoresDeletedEntity()
        {
            // Arrange
            var entity = new SoftDeleteEntity 
            { 
                Id = 20, 
                Name = "Deleted Entity",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = "TestUser"
            };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.RestoreAsync(20);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.IsDeleted, Is.False);
            Assert.That(entity.DeletedAt, Is.Null);
            Assert.That(entity.DeletedBy, Is.Null);
        }

        [Test]
        public async Task RestoreAsync_IntId_NonSoftDeleteEntity_ReturnsZero()
        {
            // Arrange
            var nonSoftDeleteLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<NonSoftDeleteEntity, TestDbContext>>.Instance;
            var entity = new NonSoftDeleteEntity { Id = 20, Name = "Entity" };
            _context.NonSoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<NonSoftDeleteEntity, TestDbContext>(_context, nonSoftDeleteLogger);

            // Act
            var result = await repo.RestoreAsync(20);

            // Assert - Non-ISoftDelete entities return 0
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region RestoreAsync - long ID overload

        [Test]
        public async Task RestoreAsync_LongId_RestoresDeletedEntity()
        {
            // Arrange
            var entity = new SoftDeleteEntity 
            { 
                Id = 21, 
                Name = "Deleted Entity",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                DeletedBy = "TestUser"
            };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.RestoreAsync(21L);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.IsDeleted, Is.False);
            Assert.That(entity.DeletedAt, Is.Null);
            Assert.That(entity.DeletedBy, Is.Null);
        }

        #endregion

        #region Integration - End to End

        [Test]
        public async Task SoftDeleteAndRestore_FullCycle()
        {
            // Arrange
            var entity = new SoftDeleteEntity { Id = 100, Name = "Test Entity" };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act - Soft delete
            await repo.SoftDeleteAsync(100, "DeleterUser");

            // Assert - Soft deleted
            Assert.That(entity.IsDeleted, Is.True);
            Assert.That(entity.DeletedBy, Is.EqualTo("DeleterUser"));

            // Act - Restore
            await repo.RestoreAsync(100);

            // Assert - Restored
            Assert.That(entity.IsDeleted, Is.False);
            Assert.That(entity.DeletedAt, Is.Null);
            Assert.That(entity.DeletedBy, Is.Null);
        }

        [Test]
        public async Task SoftDelete_TimestampSetCorrectly()
        {
            // Arrange
            var beforeDelete = DateTime.UtcNow.AddSeconds(-1);
            var entity = new SoftDeleteEntity { Id = 101, Name = "Test Entity" };
            _context.SoftDeleteEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<SoftDeleteEntity, TestDbContext>(_context, _logger);

            // Act
            await repo.SoftDeleteAsync(101, "TestUser");
            var afterDelete = DateTime.UtcNow.AddSeconds(1);

            // Assert
            Assert.That(entity.DeletedAt, Is.Not.Null);
            Assert.That(entity.DeletedAt, Is.GreaterThan(beforeDelete));
            Assert.That(entity.DeletedAt, Is.LessThan(afterDelete));
        }

        #endregion

        #region Test Infrastructure

        public class TestDbContext : DbContext
        {
            public DbSet<SoftDeleteEntity> SoftDeleteEntities { get; set; } = null!;
            public DbSet<NonSoftDeleteEntity> NonSoftDeleteEntities { get; set; } = null!;

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }

        public class SoftDeleteEntity : ISoftDelete
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool? IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public string? DeletedBy { get; set; }
        }

        public class NonSoftDeleteEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion
    }
}
