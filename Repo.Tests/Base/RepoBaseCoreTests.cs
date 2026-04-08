using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for RepoBase Core CRUD operations.
    /// Validates Add, Update, Delete (sync), Save, Insert, UpdateAsync.
    /// </summary>
    [TestFixture]
    public class RepoBaseCoreTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<TestEntity, TestDbContext>> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<TestEntity, TestDbContext>>.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region Add (Sync)

        [Test]
        public void Add_WithPersistTrue_SavesToDatabase()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = repo.Add(entity, persist: true);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(entity.Id, Is.GreaterThan(0));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Add_WithPersistFalse_DoesNotSaveToDatabase()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = repo.Add(entity, persist: false);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(0));
            
            // Verify entity is tracked
            var entry = _context.Entry(entity);
            Assert.That(entry.State, Is.EqualTo(EntityState.Added));
        }

        #endregion

        #region Update (Sync)

        [Test]
        public void Update_WithPersistTrue_SavesChanges()
        {
            // Arrange
            var entity = new TestEntity { Name = "Original Name" };
            _context.TestEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            entity.Name = "Updated Name";

            // Act
            var result = repo.Update(entity, persist: true);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            
            var updated = _context.TestEntities.Find(entity.Id);
            Assert.That(updated!.Name, Is.EqualTo("Updated Name"));
        }

        [Test]
        public void Update_WithPersistFalse_DoesNotSaveChanges()
        {
            // Arrange
            var entity = new TestEntity { Name = "Original Name" };
            _context.TestEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            entity.Name = "Updated Name";

            // Act
            var result = repo.Update(entity, persist: false);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            
            // Verify entity is tracked as modified
            var entry = _context.Entry(entity);
            Assert.That(entry.State, Is.EqualTo(EntityState.Modified));
        }

        #endregion

        #region Delete (Sync)

        [Test]
        public void Delete_WithPersistTrue_RemovesFromDatabase()
        {
            // Arrange
            var entity = new TestEntity { Name = "Entity to Delete" };
            _context.TestEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = repo.Delete(entity, persist: true);

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(0));
        }

        [Test]
        public void Delete_WithPersistFalse_DoesNotRemoveFromDatabase()
        {
            // Arrange
            var entity = new TestEntity { Name = "Entity to Delete" };
            _context.TestEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = repo.Delete(entity, persist: false);

            // Assert
            Assert.That(result, Is.EqualTo(0));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(1));
            
            // Verify entity is tracked as deleted
            var entry = _context.Entry(entity);
            Assert.That(entry.State, Is.EqualTo(EntityState.Deleted));
        }

        #endregion

        #region Save (Sync)

        [Test]
        public void Save_PersistsPendingChanges()
        {
            // Arrange
            var entity = new TestEntity { Name = "Pending Entity" };
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            repo.Add(entity, persist: false); // Add without saving

            // Act
            var result = repo.Save();

            // Assert
            Assert.That(result, Is.EqualTo(1));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Save_WithNoChanges_ReturnsZero()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = repo.Save();

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region Insert (Async)

        [Test]
        public async Task Insert_ReturnsCreatedEntity()
        {
            // Arrange
            var entity = new TestEntity { Name = "New Entity" };
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.Insert(entity);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.GreaterThan(0));
            Assert.That(result.Name, Is.EqualTo("New Entity"));
            Assert.That(_context.TestEntities.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Insert_NullEntity_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await repo.Insert(null!));
        }

        #endregion

        #region UpdateAsync

        [Test]
        public async Task UpdateAsync_ModifiesEntity()
        {
            // Arrange
            var entity = new TestEntity { Name = "Original Name" };
            _context.TestEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            entity.Name = "Updated Name";

            // Act
            var result = await repo.UpdateAsync(entity);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Updated Name"));
            
            var updated = await _context.TestEntities.FindAsync(entity.Id);
            Assert.That(updated!.Name, Is.EqualTo("Updated Name"));
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
