using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for repository-specific exception types (Issue #12) and AsNoTracking behavior (Issue #13).
    /// </summary>
    [TestFixture]
    public class RepoBaseExceptionAndTrackingTests
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

        #region Issue #12 - EntityNotFoundException Tests

        [Test]
        public void Find_IntId_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.Throws<EntityNotFoundException>(() => repo.Find(999));
            Assert.That(ex!.Message, Does.Contain("TestEntity"));
            Assert.That(ex.Message, Does.Contain("no encontrada"));
        }

        [Test]
        public void Find_LongId_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.Throws<EntityNotFoundException>(() => repo.Find(999L));
            Assert.That(ex!.Message, Does.Contain("TestEntity"));
        }

        [Test]
        public void Find_LongId_NullId_ThrowsArgumentNullException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => repo.Find((long?)null));
        }

        [Test]
        public async Task GetById_IntId_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<EntityNotFoundException>(async () => await repo.GetById(999));
            Assert.That(ex!.Message, Does.Contain("TestEntity"));
        }

        [Test]
        public async Task GetById_LongId_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<EntityNotFoundException>(async () => await repo.GetById(999L));
            Assert.That(ex!.Message, Does.Contain("TestEntity"));
        }

        [Test]
        public async Task DeleteAsync_IntId_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<EntityNotFoundException>(async () => await repo.DeleteAsync(999));
            Assert.That(ex!.Message, Does.Contain("TestEntity"));
        }

        [Test]
        public async Task DeleteAsync_LongId_EntityNotFound_ThrowsEntityNotFoundException()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<EntityNotFoundException>(async () => await repo.DeleteAsync(999L));
            Assert.That(ex!.Message, Does.Contain("TestEntity"));
        }

        [Test]
        public void EntityNotFoundException_IsRepositoryException()
        {
            // Arrange & Act
            var ex = new EntityNotFoundException("Test message");

            // Assert
            Assert.That(ex, Is.InstanceOf<RepositoryException>());
        }

        [Test]
        public void EntityNotFoundException_WithInnerException()
        {
            // Arrange
            var innerEx = new InvalidOperationException("Inner error");

            // Act
            var ex = new EntityNotFoundException("Test message", innerEx);

            // Assert
            Assert.That(ex.InnerException, Is.SameAs(innerEx));
        }

        [Test]
        public void RepositoryException_CanBeCaughtAsException()
        {
            // Arrange & Act
            try
            {
                throw new EntityNotFoundException("Test");
            }
            // Assert - Should be catchable as Exception (backward compatibility)
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<EntityNotFoundException>());
            }
        }

        #endregion

        #region Issue #13 - AsNoTracking Tests

        [Test]
        public void GetAll_DefaultTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = repo.GetAll();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetAll_AsNoTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = repo.GetAll(asNoTracking: true);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetAll_WithIntOverload_DefaultTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = repo.GetAll(1);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public void GetAll_WithIntOverload_AsNoTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = repo.GetAll(1, asNoTracking: true);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllAsync_DefaultTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.GetAllAsync();

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllAsync_AsNoTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.GetAllAsync(asNoTracking: true);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllAsync_WithIntOverload_DefaultTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.GetAllAsync(1);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllAsync_WithIntOverload_AsNoTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.GetAllAsync(1, asNoTracking: true);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task FindAsync_DefaultTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1", Value = 100 });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.FindAsync(e => e.Value > 50);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task FindAsync_AsNoTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1", Value = 100 });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.FindAsync(e => e.Value > 50, asNoTracking: true);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task FindAsync_WithIncludes_DefaultTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1", Value = 100 });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.FindAsync(e => e.Value > 50, asNoTracking: false, default);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task FindAsync_WithIncludes_AsNoTracking_ReturnsEntities()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1", Value = 100 });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var results = await repo.FindAsync(e => e.Value > 50, asNoTracking: true, default);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task FirstOrDefaultAsync_DefaultTracking_ReturnsEntity()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1", Value = 100 });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FirstOrDefaultAsync(e => e.Value > 50);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Entity1"));
        }

        [Test]
        public async Task FirstOrDefaultAsync_AsNoTracking_ReturnsEntity()
        {
            // Arrange
            _context.TestEntities.Add(new TestEntity { Name = "Entity1", Value = 100 });
            _context.SaveChanges();

            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FirstOrDefaultAsync(e => e.Value > 50, asNoTracking: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Entity1"));
        }

        [Test]
        public async Task FirstOrDefaultAsync_NotFound_ReturnsNull()
        {
            // Arrange
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FirstOrDefaultAsync(e => e.Value > 50);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region Test Infrastructure

        public class TestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; } = null!;

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }

        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        #endregion
    }
}
