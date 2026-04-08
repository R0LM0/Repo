using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using System.Linq.Expressions;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for RepoBase Query operations.
    /// Validates AnyAsync, CountAsync, FindAsync, FirstOrDefaultAsync.
    /// </summary>
    [TestFixture]
    public class RepoBaseQueriesTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<QueryTestEntity, TestDbContext>> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<QueryTestEntity, TestDbContext>>.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region AnyAsync

        [Test]
        public async Task AnyAsync_MatchingPredicate_ReturnsTrue()
        {
            // Arrange
            _context.QueryTestEntities.AddRange(
                new QueryTestEntity { Id = 1, Name = "Entity 1", IsActive = true },
                new QueryTestEntity { Id = 2, Name = "Entity 2", IsActive = false }
            );
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.AnyAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task AnyAsync_NoMatch_ReturnsFalse()
        {
            // Arrange
            _context.QueryTestEntities.Add(new QueryTestEntity { Id = 1, Name = "Entity 1", IsActive = false });
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.AnyAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task AnyAsync_EmptyTable_ReturnsFalse()
        {
            // Arrange
            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.AnyAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region CountAsync

        [Test]
        public async Task CountAsync_ReturnsCorrectCount()
        {
            // Arrange
            _context.QueryTestEntities.AddRange(
                new QueryTestEntity { Id = 1, Name = "Entity 1", IsActive = true },
                new QueryTestEntity { Id = 2, Name = "Entity 2", IsActive = true },
                new QueryTestEntity { Id = 3, Name = "Entity 3", IsActive = false }
            );
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.CountAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public async Task CountAsync_NoMatch_ReturnsZero()
        {
            // Arrange
            _context.QueryTestEntities.Add(new QueryTestEntity { Id = 1, Name = "Entity 1", IsActive = false });
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.CountAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        #endregion

        #region FindAsync

        [Test]
        public async Task FindAsync_ReturnsMatchingEntities()
        {
            // Arrange
            _context.QueryTestEntities.AddRange(
                new QueryTestEntity { Id = 1, Name = "Active Entity", IsActive = true },
                new QueryTestEntity { Id = 2, Name = "Inactive Entity", IsActive = false },
                new QueryTestEntity { Id = 3, Name = "Another Active", IsActive = true }
            );
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FindAsync(e => e.IsActive);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.All(e => e.IsActive), Is.True);
        }

        [Test]
        public async Task FindAsync_WithNoTracking_DoesNotTrackEntities()
        {
            // Arrange
            _context.QueryTestEntities.Add(new QueryTestEntity { Id = 1, Name = "Entity", IsActive = true });
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FindAsync(e => e.IsActive, asNoTracking: true);
            var entity = result.First();

            // Assert
            var entry = _context.Entry(entity);
            Assert.That(entry.State, Is.EqualTo(EntityState.Detached));
        }

        [Test]
        public async Task FindAsync_WithIncludes_LoadsRelatedData()
        {
            // Arrange
            var parent = new QueryTestEntity { Id = 1, Name = "Parent", IsActive = true };
            var child = new RelatedEntity { Id = 1, Name = "Child", ParentId = 1 };
            _context.QueryTestEntities.Add(parent);
            _context.RelatedEntities.Add(child);
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FindAsync(
                e => e.Id == 1, 
                asNoTracking: true, 
                includes: new Expression<Func<QueryTestEntity, object>>[] { e => e.RelatedItems! });

            // Assert
            var entity = result.First();
            Assert.That(entity.RelatedItems, Is.Not.Null);
        }

        #endregion

        #region FirstOrDefaultAsync

        [Test]
        public async Task FirstOrDefaultAsync_MatchingPredicate_ReturnsEntity()
        {
            // Arrange
            _context.QueryTestEntities.AddRange(
                new QueryTestEntity { Id = 1, Name = "First", IsActive = true },
                new QueryTestEntity { Id = 2, Name = "Second", IsActive = true }
            );
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FirstOrDefaultAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.IsActive, Is.True);
        }

        [Test]
        public async Task FirstOrDefaultAsync_NoMatch_ReturnsNull()
        {
            // Arrange
            _context.QueryTestEntities.Add(new QueryTestEntity { Id = 1, Name = "Entity", IsActive = false });
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FirstOrDefaultAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task FirstOrDefaultAsync_EmptyTable_ReturnsNull()
        {
            // Arrange
            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FirstOrDefaultAsync(e => e.IsActive);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task FirstOrDefaultAsync_WithNoTracking_DoesNotTrackEntity()
        {
            // Arrange
            _context.QueryTestEntities.Add(new QueryTestEntity { Id = 1, Name = "Entity", IsActive = true });
            _context.SaveChanges();

            var repo = new RepoBase<QueryTestEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.FirstOrDefaultAsync(e => e.IsActive, asNoTracking: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            var entry = _context.Entry(result!);
            Assert.That(entry.State, Is.EqualTo(EntityState.Detached));
        }

        #endregion

        #region Test Entities

        public class QueryTestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public List<RelatedEntity>? RelatedItems { get; set; }
        }

        public class RelatedEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int ParentId { get; set; }
            public QueryTestEntity? Parent { get; set; }
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<QueryTestEntity> QueryTestEntities => Set<QueryTestEntity>();
            public DbSet<RelatedEntity> RelatedEntities => Set<RelatedEntity>();

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<QueryTestEntity>()
                    .HasMany(e => e.RelatedItems)
                    .WithOne(r => r.Parent)
                    .HasForeignKey(r => r.ParentId);
            }
        }

        #endregion
    }
}
