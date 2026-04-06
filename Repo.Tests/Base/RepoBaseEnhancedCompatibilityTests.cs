using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using Repo.Repository.Models;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for RepoBaseEnhanced backward compatibility (Issue #7).
    /// Validates that RepoBaseEnhanced still works as a deprecated wrapper around RepoBase.
    /// </summary>
    [TestFixture]
    [Obsolete("Testing deprecated class for backward compatibility")]
    public class RepoBaseEnhancedCompatibilityTests
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

        [Test]
        public void RepoBaseEnhanced_CanBeInstantiated()
        {
            // Act - Should compile and work despite being obsolete
            var repo = new RepoBaseEnhanced<TestEntity, TestDbContext>(_context, _logger);

            // Assert
            Assert.That(repo, Is.Not.Null);
            Assert.That(repo, Is.InstanceOf<RepoBase<TestEntity, TestDbContext>>());
        }

        [Test]
        public void RepoBaseEnhanced_Implements_IRepo()
        {
            // Act
            var repo = new RepoBaseEnhanced<TestEntity, TestDbContext>(_context, _logger);

            // Assert
            Assert.That(repo, Is.InstanceOf<IRepo<TestEntity>>());
        }

        [Test]
        public async Task RepoBaseEnhanced_CanPerformCrud()
        {
            // Arrange
            var repo = new RepoBaseEnhanced<TestEntity, TestDbContext>(_context, _logger);
            var entity = new TestEntity { Name = "Test" };

            // Act - Create (use Insert which is async)
            await repo.Insert(entity);

            // Assert
            Assert.That(entity.Id, Is.GreaterThan(0));

            // Act - Read
            var found = await repo.GetById(entity.Id);

            // Assert
            Assert.That(found, Is.Not.Null);
            Assert.That(found.Name, Is.EqualTo("Test"));
        }

        [Test]
        public void RepoBaseEnhanced_InheritsAllMethodsFromRepoBase()
        {
            // Arrange
            var repo = new RepoBaseEnhanced<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert - Verify all IRepo methods are accessible
            Assert.That((object)(() => repo.GetAllAsync()), Is.Not.Null);
            Assert.That((object)(() => repo.GetById(1)), Is.Not.Null);
            Assert.That((object)(() => repo.Insert(new TestEntity())), Is.Not.Null);
            Assert.That((object)(() => repo.UpdateAsync(new TestEntity())), Is.Not.Null);
            Assert.That((object)(() => repo.DeleteAsync(1)), Is.Not.Null);
            Assert.That((object)(() => repo.SaveAsync()), Is.Not.Null);
            Assert.That((object)(() => repo.GetPagedAsync(new PagedRequest())), Is.Not.Null);
            Assert.That((object)(() => repo.FindAsync(e => e.Id == 1)), Is.Not.Null);
        }

        #region Test Infrastructure

        public class TestDbContext : DbContext
        {
            public DbSet<TestEntity> Entities { get; set; } = null!;

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }

        public class TestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion
    }
}
