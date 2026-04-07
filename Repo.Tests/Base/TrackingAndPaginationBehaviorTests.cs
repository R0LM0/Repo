using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Models;
using Repo.Repository.Specifications;
using System.Linq.Expressions;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for tracking configuration in specifications and pagination behavior (Issue #34).
    /// Verifies that SpecificationEvaluator respects IsTrackingEnabled and GetPagedAsync
    /// correctly applies asNoTracking parameter.
    /// </summary>
    [TestFixture]
    public class TrackingAndPaginationBehaviorTests
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

        #region Specification Tracking Tests

        [Test]
        public async Task GetBySpecAsync_WithTrackingDisabled_ReturnsDetachedEntities()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test1", Value = 100 };
            _context.TestEntities.Add(entity);
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Name == "Test1") { IsTrackingEnabled = false };

            // Act
            var result = await repo.GetBySpecAsync(spec);

            // Assert
            Assert.That(result, Is.Not.Null);
            var entry = _context.Entry(result!);
            Assert.That(entry.State, Is.EqualTo(EntityState.Detached), 
                "Entity should be detached when tracking is disabled in specification");
        }

        [Test]
        public async Task GetBySpecAsync_WithTrackingEnabled_ReturnsTrackedEntities()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test1", Value = 100 };
            _context.TestEntities.Add(entity);
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Name == "Test1") { IsTrackingEnabled = true };

            // Act
            var result = await repo.GetBySpecAsync(spec);

            // Assert
            Assert.That(result, Is.Not.Null);
            var entry = _context.Entry(result!);
            Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                "Entity should be tracked (Unchanged state) when tracking is enabled in specification");
        }

        [Test]
        public async Task GetBySpecAsync_DefaultTrackingEnabled_ReturnsTrackedEntities()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test1", Value = 100 };
            _context.TestEntities.Add(entity);
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Name == "Test1");
            // IsTrackingEnabled defaults to true in BaseSpecification

            // Act
            var result = await repo.GetBySpecAsync(spec);

            // Assert
            Assert.That(result, Is.Not.Null);
            var entry = _context.Entry(result!);
            Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                "Entity should be tracked by default (IsTrackingEnabled defaults to true)");
        }

        [Test]
        public async Task GetAllBySpecAsync_WithTrackingDisabled_ReturnsDetachedEntities()
        {
            // Arrange
            _context.TestEntities.AddRange(
                new TestEntity { Name = "Test1", Value = 100 },
                new TestEntity { Name = "Test2", Value = 200 }
            );
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Value > 50) { IsTrackingEnabled = false };

            // Act
            var results = await repo.GetAllBySpecAsync(spec);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(2));
            
            foreach (var item in results)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Detached), 
                    "All entities should be detached when tracking is disabled");
            }
        }

        [Test]
        public async Task GetAllBySpecAsync_WithTrackingEnabled_ReturnsTrackedEntities()
        {
            // Arrange
            _context.TestEntities.AddRange(
                new TestEntity { Name = "Test1", Value = 100 },
                new TestEntity { Name = "Test2", Value = 200 }
            );
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Value > 50) { IsTrackingEnabled = true };

            // Act
            var results = await repo.GetAllBySpecAsync(spec);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(2));
            
            foreach (var item in results)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                    "All entities should be tracked when tracking is enabled");
            }
        }

        #endregion

        #region Specification Pagination Tests

        [Test]
        public async Task GetPagedBySpecAsync_WithTrackingDisabled_ReturnsDetachedEntities()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Value > 0) { IsTrackingEnabled = false };
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };

            // Act
            var result = await repo.GetPagedBySpecAsync(spec, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Detached), 
                    "Paged entities should be detached when tracking is disabled in specification");
            }
        }

        [Test]
        public async Task GetPagedBySpecAsync_WithTrackingEnabled_ReturnsTrackedEntities()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Value > 0) { IsTrackingEnabled = true };
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };

            // Act
            var result = await repo.GetPagedBySpecAsync(spec, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                    "Paged entities should be tracked when tracking is enabled in specification");
            }
        }

        [Test]
        public async Task CountBySpecAsync_WithTrackingDisabled_DoesNotTrackEntities()
        {
            // Arrange
            _context.TestEntities.AddRange(
                new TestEntity { Name = "Test1", Value = 100 },
                new TestEntity { Name = "Test2", Value = 200 }
            );
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var spec = new TestSpecification(e => e.Value > 50) { IsTrackingEnabled = false };

            // Act
            var count = await repo.CountBySpecAsync(spec);

            // Assert
            Assert.That(count, Is.EqualTo(2));
            // CountAsync doesn't materialize entities, so nothing should be tracked
            Assert.That(_context.ChangeTracker.Entries().Count(), Is.EqualTo(0),
                "CountBySpecAsync should not track any entities");
        }

        #endregion

        #region GetPagedAsync Tracking Tests

        [Test]
        public async Task GetPagedAsync_WithAsNoTrackingTrue_ReturnsDetachedEntities()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };

            // Act
            var result = await repo.GetPagedAsync(request, asNoTracking: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Detached), 
                    "Entities should be detached when asNoTracking is true");
            }
        }

        [Test]
        public async Task GetPagedAsync_WithAsNoTrackingFalse_ReturnsTrackedEntities()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };

            // Act
            var result = await repo.GetPagedAsync(request, asNoTracking: false);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                    "Entities should be tracked when asNoTracking is false");
            }
        }

        [Test]
        public async Task GetPagedAsync_DefaultAsNoTracking_ReturnsTrackedEntities()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };

            // Act - Call without specifying asNoTracking (should default to false)
            var result = await repo.GetPagedAsync(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                    "Entities should be tracked by default (asNoTracking defaults to false)");
            }
        }

        [Test]
        public async Task GetPagedAsync_WithFilter_AndAsNoTrackingTrue_ReturnsDetachedEntities()
        {
            // Arrange
            for (int i = 1; i <= 10; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };
            Expression<Func<TestEntity, bool>> filter = e => e.Value > 300;

            // Act
            var result = await repo.GetPagedAsync(filter, request, asNoTracking: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            Assert.That(result.TotalCount, Is.EqualTo(7)); // 400, 500, 600, 700, 800, 900, 1000
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Detached), 
                    "Filtered paged entities should be detached when asNoTracking is true");
            }
        }

        [Test]
        public async Task GetPagedAsync_WithFilter_AndAsNoTrackingFalse_ReturnsTrackedEntities()
        {
            // Arrange
            for (int i = 1; i <= 10; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };
            Expression<Func<TestEntity, bool>> filter = e => e.Value > 300;

            // Act
            var result = await repo.GetPagedAsync(filter, request, asNoTracking: false);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            Assert.That(result.TotalCount, Is.EqualTo(7));
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                    "Filtered paged entities should be tracked when asNoTracking is false");
            }
        }

        [Test]
        public async Task GetPagedAsync_WithFilter_DefaultAsNoTracking_ReturnsTrackedEntities()
        {
            // Arrange
            for (int i = 1; i <= 10; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            // Clear change tracker to start fresh
            _context.ChangeTracker.Clear();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };
            Expression<Func<TestEntity, bool>> filter = e => e.Value > 300;

            // Act - Call without specifying asNoTracking (should default to false)
            var result = await repo.GetPagedAsync(filter, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            
            foreach (var item in result.Data)
            {
                var entry = _context.Entry(item);
                Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged), 
                    "Filtered paged entities should be tracked by default");
            }
        }

        #endregion

        #region Backward Compatibility Tests

        [Test]
        public async Task GetPagedAsync_ExistingCodeWithoutAsNoTrackingParameter_WorksCorrectly()
        {
            // Arrange - Simulate existing code that doesn't use asNoTracking parameter
            for (int i = 1; i <= 5; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 1, PageSize = 3 };

            // Act - This is how existing code would call it (only 1 parameter)
            var result = await repo.GetPagedAsync(request);

            // Assert - Should work and return tracked entities (backward compatible)
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            Assert.That(result.TotalCount, Is.EqualTo(5));
            Assert.That(result.Page, Is.EqualTo(1));
            Assert.That(result.PageSize, Is.EqualTo(3));
            Assert.That(result.TotalPages, Is.EqualTo(2));
            Assert.That(result.HasNextPage, Is.True);
            Assert.That(result.HasPreviousPage, Is.False);
        }

        [Test]
        public async Task GetPagedAsync_WithFilter_ExistingCodeWithoutAsNoTrackingParameter_WorksCorrectly()
        {
            // Arrange - Simulate existing code that doesn't use asNoTracking parameter
            for (int i = 1; i <= 10; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var request = new PagedRequest { PageNumber = 2, PageSize = 3 };
            Expression<Func<TestEntity, bool>> filter = e => e.Value > 200;

            // Act - This is how existing code would call it (only 2 parameters)
            var result = await repo.GetPagedAsync(filter, request);

            // Assert - Should work and return correct page
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(3));
            Assert.That(result.Page, Is.EqualTo(2));
            // Items should be: 300, 400, 500 on page 1; 600, 700, 800 on page 2
            Assert.That(result.Data[0].Value, Is.EqualTo(600));
            Assert.That(result.Data[1].Value, Is.EqualTo(700));
            Assert.That(result.Data[2].Value, Is.EqualTo(800));
        }

        [Test]
        public async Task Specification_IsTrackingEnabled_DefaultsToTrue()
        {
            // Arrange
            var spec = new TestSpecification(e => e.Value > 0);

            // Assert
            Assert.That(spec.IsTrackingEnabled, Is.True, 
                "IsTrackingEnabled should default to true for backward compatibility");
        }

        #endregion

        #region Combined Behavior Tests

        [Test]
        public async Task MultipleOperations_TrackingStateIsConsistent()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 100 });
            }
            await _context.SaveChangesAsync();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert - Operation 1: Get with no tracking
            _context.ChangeTracker.Clear();
            var noTrackingResult = await repo.GetPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 2 }, asNoTracking: true);
            foreach (var item in noTrackingResult.Data)
            {
                Assert.That(_context.Entry(item).State, Is.EqualTo(EntityState.Detached));
            }

            // Act & Assert - Operation 2: Get with tracking (default)
            _context.ChangeTracker.Clear();
            var trackingResult = await repo.GetPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 2 });
            foreach (var item in trackingResult.Data)
            {
                Assert.That(_context.Entry(item).State, Is.EqualTo(EntityState.Unchanged));
            }

            // Act & Assert - Operation 3: Get with spec no tracking
            _context.ChangeTracker.Clear();
            var noTrackingSpec = new TestSpecification(e => e.Value > 0) { IsTrackingEnabled = false };
            var noTrackingSpecResult = await repo.GetAllBySpecAsync(noTrackingSpec);
            foreach (var item in noTrackingSpecResult)
            {
                Assert.That(_context.Entry(item).State, Is.EqualTo(EntityState.Detached));
            }

            // Act & Assert - Operation 4: Get with spec tracking
            _context.ChangeTracker.Clear();
            var trackingSpec = new TestSpecification(e => e.Value > 0) { IsTrackingEnabled = true };
            var trackingSpecResult = await repo.GetAllBySpecAsync(trackingSpec);
            foreach (var item in trackingSpecResult)
            {
                Assert.That(_context.Entry(item).State, Is.EqualTo(EntityState.Unchanged));
            }
        }

        [Test]
        public async Task PagedResult_HasCorrectPaginationMetadata()
        {
            // Arrange
            for (int i = 1; i <= 25; i++)
            {
                _context.TestEntities.Add(new TestEntity { Name = $"Test{i}", Value = i * 10 });
            }
            await _context.SaveChangesAsync();
            
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act - Page 1
            var page1 = await repo.GetPagedAsync(new PagedRequest { PageNumber = 1, PageSize = 10 });
            
            // Assert - Page 1
            Assert.That(page1.Data.Count, Is.EqualTo(10));
            Assert.That(page1.TotalCount, Is.EqualTo(25));
            Assert.That(page1.Page, Is.EqualTo(1));
            Assert.That(page1.PageSize, Is.EqualTo(10));
            Assert.That(page1.TotalPages, Is.EqualTo(3));
            Assert.That(page1.HasPreviousPage, Is.False);
            Assert.That(page1.HasNextPage, Is.True);

            // Act - Page 2
            var page2 = await repo.GetPagedAsync(new PagedRequest { PageNumber = 2, PageSize = 10 });
            
            // Assert - Page 2
            Assert.That(page2.Data.Count, Is.EqualTo(10));
            Assert.That(page2.Page, Is.EqualTo(2));
            Assert.That(page2.HasPreviousPage, Is.True);
            Assert.That(page2.HasNextPage, Is.True);

            // Act - Page 3 (last page)
            var page3 = await repo.GetPagedAsync(new PagedRequest { PageNumber = 3, PageSize = 10 });
            
            // Assert - Page 3
            Assert.That(page3.Data.Count, Is.EqualTo(5));
            Assert.That(page3.Page, Is.EqualTo(3));
            Assert.That(page3.HasPreviousPage, Is.True);
            Assert.That(page3.HasNextPage, Is.False);
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

        public class TestSpecification : BaseSpecification<TestEntity>
        {
            public TestSpecification(Expression<Func<TestEntity, bool>> criteria)
            {
                AddCriteria(criteria);
            }
        }

        #endregion
    }
}
