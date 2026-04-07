using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for CompiledRepoBase and CompiledQueries functionality (Issue #52).
    /// </summary>
    [TestFixture]
    public class CompiledQueryTests
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

            // Seed test data
            SeedData();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        private void SeedData()
        {
            _context.Entities.AddRange(
                new TestEntity { Id = 1, Name = "Entity 1", Value = 100 },
                new TestEntity { Id = 2, Name = "Entity 2", Value = 200 },
                new TestEntity { Id = 3, Name = "Entity 3", Value = 300 }
            );
            _context.SaveChanges();
        }

        #region Compiled Queries Enabled Tests

        [Test]
        public void CompiledRepoBase_WhenEnabled_InitializesCompiledQueries()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };

            // Act
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Assert
            Assert.That(repo.IsCompiledQueriesEnabled, Is.True);
            Assert.That(CompiledQueries<TestEntity, TestDbContext>.IsInitialized, Is.True);
        }

        [Test]
        public async Task CompiledRepoBase_GetById_Int_WhenEnabled_ReturnsEntity()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var result = await repo.GetById(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Name, Is.EqualTo("Entity 1"));
        }

        [Test]
        public async Task CompiledRepoBase_GetById_Long_WhenEnabled_ReturnsEntity()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var result = await repo.GetById(2L);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(2));
            Assert.That(result.Name, Is.EqualTo("Entity 2"));
        }

        [Test]
        public void CompiledRepoBase_GetById_WhenEnabled_EntityNotFound_ThrowsException()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act & Assert
            var ex = Assert.ThrowsAsync<EntityNotFoundException>(async () =>
                await repo.GetById(999));
            Assert.That(ex.Message, Does.Contain("no encontrada"));
        }

        [Test]
        public async Task CompiledRepoBase_GetAllAsync_WhenEnabled_ReturnsAllEntities()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var result = await repo.GetAllAsync();

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list.Select(e => e.Id), Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task CompiledRepoBase_GetAllAsync_WithNoTracking_ReturnsEntities()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var result = await repo.GetAllAsync(asNoTracking: true);

            // Assert
            var list = result.ToList();
            Assert.That(list.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task CompiledRepoBase_CountAllAsync_WhenEnabled_ReturnsCorrectCount()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var count = await repo.CountAllAsync();

            // Assert
            Assert.That(count, Is.EqualTo(3));
        }

        #endregion

        #region Compiled Queries Disabled Tests

        [Test]
        public void CompiledRepoBase_WhenDisabled_DoesNotInitializeCompiledQueries()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = false };

            // Act
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Assert
            Assert.That(repo.IsCompiledQueriesEnabled, Is.False);
        }

        [Test]
        public async Task CompiledRepoBase_GetById_WhenDisabled_UsesStandardQuery()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = false };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var result = await repo.GetById(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1));
        }

        [Test]
        public async Task CompiledRepoBase_GetAllAsync_WhenDisabled_UsesStandardQuery()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = false };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var result = await repo.GetAllAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task CompiledRepoBase_CountAllAsync_WhenDisabled_UsesStandardCount()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = false };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act
            var count = await repo.CountAllAsync();

            // Assert
            Assert.That(count, Is.EqualTo(3));
        }

        #endregion

        #region Default Options Tests

        [Test]
        public void CompiledRepoBase_WithDefaultOptions_DisablesCompiledQueries()
        {
            // Arrange & Act
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Assert
            Assert.That(repo.Options.EnableCompiledQueries, Is.False);
            Assert.That(repo.IsCompiledQueriesEnabled, Is.False);
        }

        [Test]
        public void CompiledRepoBase_WithNullOptions_DisablesCompiledQueries()
        {
            // Arrange & Act
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, null);

            // Assert
            Assert.That(repo.Options.EnableCompiledQueries, Is.False);
            Assert.That(repo.IsCompiledQueriesEnabled, Is.False);
        }

        #endregion

        #region Logging Tests

        [Test]
        public void CompiledRepoBase_WithLoggingEnabled_LogsCompiledQueryUsage()
        {
            // Arrange
            var repoOptions = new RepoOptions
            {
                EnableCompiledQueries = true,
                LogCompiledQueryUsage = true
            };

            // Act - should not throw
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Assert - repo is created successfully
            Assert.That(repo.Options.LogCompiledQueryUsage, Is.True);
        }

        #endregion

        #region Backward Compatibility Tests

        [Test]
        public void CompiledRepoBase_Implements_IRepo()
        {
            // Arrange
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.That(repo, Is.InstanceOf<IRepo<TestEntity>>());
        }

        [Test]
        public void CompiledRepoBase_Inherits_RepoBase()
        {
            // Arrange
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.That(repo, Is.InstanceOf<RepoBase<TestEntity, TestDbContext>>());
        }

        [Test]
        public async Task CompiledRepoBase_CountAsync_WithPredicate_WorksWithStandardQuery()
        {
            // Arrange
            var repoOptions = new RepoOptions { EnableCompiledQueries = true };
            var repo = new CompiledRepoBase<TestEntity, TestDbContext>(_context, _logger, repoOptions);

            // Act - Count with predicate should use standard query (not compiled)
            var count = await repo.CountAsync(e => e.Value > 150);

            // Assert
            Assert.That(count, Is.EqualTo(2)); // Entities 2 and 3
        }

        #endregion

        #region RepoOptions Tests

        [Test]
        public void RepoOptions_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var options = new RepoOptions();

            // Assert
            Assert.That(options.EnableCompiledQueries, Is.False, "Compiled queries should be disabled by default for backward compatibility");
            Assert.That(options.LogCompiledQueryUsage, Is.False, "Logging should be disabled by default");
        }

        [Test]
        public void RepoOptions_CanEnableCompiledQueries()
        {
            // Arrange & Act
            var options = new RepoOptions { EnableCompiledQueries = true };

            // Assert
            Assert.That(options.EnableCompiledQueries, Is.True);
        }

        [Test]
        public void RepoOptions_CanEnableLogging()
        {
            // Arrange & Act
            var options = new RepoOptions { LogCompiledQueryUsage = true };

            // Assert
            Assert.That(options.LogCompiledQueryUsage, Is.True);
        }

        #endregion

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
            public int Value { get; set; }
        }

        #endregion
    }
}
