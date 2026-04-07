using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for Cache behavior (Issue #35).
    /// Validates cache integration for GetById and GetAll operations.
    /// </summary>
    [TestFixture]
    public class CacheBehaviorTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<CachedEntity, TestDbContext>> _logger = null!;
        private FakeCacheService _cacheService = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<CachedEntity, TestDbContext>>.Instance;
            _cacheService = new FakeCacheService();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region GetByIdWithCacheAsync - int overload

        [Test]
        public async Task GetByIdWithCacheAsync_IntId_CacheMiss_FetchesFromDbAndStoresInCache()
        {
            // Arrange
            var entity = new CachedEntity { Id = 1, Name = "Test Entity" };
            _context.CachedEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // Act
            var result = await repo.GetByIdWithCacheAsync(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Test Entity"));
            Assert.That(_cacheService.GetCallCount($"CachedEntity:1"), Is.EqualTo(1));
            Assert.That(_cacheService.SetCallCount($"CachedEntity:1"), Is.EqualTo(1));
        }

        [Test]
        public async Task GetByIdWithCacheAsync_IntId_CacheHit_ReturnsFromCache()
        {
            // Arrange
            var entity = new CachedEntity { Id = 2, Name = "Original" };
            _context.CachedEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // First call - cache miss
            await repo.GetByIdWithCacheAsync(2);

            // Modify entity in DB directly (bypassing cache)
            entity.Name = "Modified";
            _context.SaveChanges();

            // Act - Second call should return cached version
            var result = await repo.GetByIdWithCacheAsync(2);

            // Assert - Should get original cached value
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Original"));
        }

        [Test]
        public async Task GetByIdWithCacheAsync_IntId_WithExpiration_SetsExpiration()
        {
            // Arrange
            var entity = new CachedEntity { Id = 3, Name = "Test" };
            _context.CachedEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);
            var expiration = TimeSpan.FromMinutes(5);

            // Act
            await repo.GetByIdWithCacheAsync(3, expiration);

            // Assert
            Assert.That(_cacheService.LastExpiration, Is.EqualTo(expiration));
        }

        #endregion

        #region GetByIdWithCacheAsync - long overload

        [Test]
        public async Task GetByIdWithCacheAsync_LongId_CacheMiss_FetchesFromDb()
        {
            // Arrange
            var entity = new CachedEntity { Id = 4, Name = "Test Entity" };
            _context.CachedEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // Act
            var result = await repo.GetByIdWithCacheAsync(4L);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Test Entity"));
        }

        [Test]
        public async Task GetByIdWithCacheAsync_LongId_CacheHit_ReturnsFromCache()
        {
            // Arrange
            var entity = new CachedEntity { Id = 5, Name = "Original" };
            _context.CachedEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // First call - cache miss
            await repo.GetByIdWithCacheAsync(5L);

            // Modify entity in DB
            entity.Name = "Modified";
            _context.SaveChanges();

            // Act - Second call should return cached version
            var result = await repo.GetByIdWithCacheAsync(5L);

            // Assert
            Assert.That(result!.Name, Is.EqualTo("Original"));
        }

        #endregion

        #region GetByIdWithCacheAsync - No Cache Service

        [Test]
        public async Task GetByIdWithCacheAsync_NoCacheService_DelegatesToGetById()
        {
            // Arrange
            var entity = new CachedEntity { Id = 6, Name = "Test Entity" };
            _context.CachedEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, null);

            // Act
            var result = await repo.GetByIdWithCacheAsync(6);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Name, Is.EqualTo("Test Entity"));
        }

        #endregion

        #region GetAllWithCacheAsync

        [Test]
        public async Task GetAllWithCacheAsync_CacheMiss_FetchesFromDbAndStoresInCache()
        {
            // Arrange
            _context.CachedEntities.AddRange(
                new CachedEntity { Id = 10, Name = "Entity1" },
                new CachedEntity { Id = 11, Name = "Entity2" }
            );
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // Act
            var results = await repo.GetAllWithCacheAsync();

            // Assert
            Assert.That(results.Count(), Is.EqualTo(2));
            Assert.That(_cacheService.GetCallCount("CachedEntity:All"), Is.EqualTo(1));
            Assert.That(_cacheService.SetCallCount("CachedEntity:All"), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllWithCacheAsync_CacheHit_ReturnsFromCache()
        {
            // Arrange
            _context.CachedEntities.Add(new CachedEntity { Id = 12, Name = "Entity1" });
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // First call - cache miss
            await repo.GetAllWithCacheAsync();

            // Add new entity to DB
            _context.CachedEntities.Add(new CachedEntity { Id = 13, Name = "Entity2" });
            _context.SaveChanges();

            // Act - Second call should return cached (stale) data
            var results = await repo.GetAllWithCacheAsync();

            // Assert - Should have only 1 entity (from cache)
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetAllWithCacheAsync_WithExpiration_SetsExpiration()
        {
            // Arrange
            _context.CachedEntities.Add(new CachedEntity { Id = 14, Name = "Test" });
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);
            var expiration = TimeSpan.FromHours(1);

            // Act
            await repo.GetAllWithCacheAsync(expiration);

            // Assert
            Assert.That(_cacheService.LastExpiration, Is.EqualTo(expiration));
        }

        [Test]
        public async Task GetAllWithCacheAsync_NoCacheService_DelegatesToGetAllAsync()
        {
            // Arrange
            _context.CachedEntities.Add(new CachedEntity { Id = 15, Name = "Test" });
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, null);

            // Act
            var results = await repo.GetAllWithCacheAsync();

            // Assert
            Assert.That(results.Count(), Is.EqualTo(1));
        }

        #endregion

        #region InvalidateCacheAsync

        [Test]
        public async Task InvalidateCacheAsync_WithPattern_RemovesByPattern()
        {
            // Arrange
            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);
            await repo.GetByIdWithCacheAsync(20);
            await repo.GetByIdWithCacheAsync(21);

            // Act
            await repo.InvalidateCacheAsync("*");

            // Assert
            Assert.That(_cacheService.RemoveByPatternCallCount("CachedEntity:*"), Is.EqualTo(1));
        }

        [Test]
        public async Task InvalidateCacheAsync_WithSpecificPattern_RemovesMatching()
        {
            // Arrange
            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // Act
            await repo.InvalidateCacheAsync("20");

            // Assert
            Assert.That(_cacheService.RemoveByPatternCallCount("CachedEntity:20"), Is.EqualTo(1));
        }

        [Test]
        public async Task InvalidateCacheAsync_NoCacheService_DoesNotThrow()
        {
            // Arrange
            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, null);

            // Act & Assert - Should not throw
            Assert.DoesNotThrowAsync(async () => await repo.InvalidateCacheAsync("*"));
        }

        #endregion

        #region Cache Key Generation

        [Test]
        public async Task GetByIdWithCacheAsync_GeneratesCorrectCacheKey()
        {
            // Arrange
            var entity = new CachedEntity { Id = 100, Name = "Test" };
            _context.CachedEntities.Add(entity);
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // Act
            await repo.GetByIdWithCacheAsync(100);

            // Assert
            Assert.That(_cacheService.LastGetKey, Is.EqualTo("CachedEntity:100"));
        }

        [Test]
        public async Task GetAllWithCacheAsync_GeneratesCorrectCacheKey()
        {
            // Arrange
            _context.CachedEntities.Add(new CachedEntity { Id = 101, Name = "Test" });
            _context.SaveChanges();

            var repo = new RepoBase<CachedEntity, TestDbContext>(_context, _logger, _cacheService);

            // Act
            await repo.GetAllWithCacheAsync();

            // Assert
            Assert.That(_cacheService.LastGetKey, Is.EqualTo("CachedEntity:All"));
        }

        #endregion

        #region Test Infrastructure

        public class TestDbContext : DbContext
        {
            public DbSet<CachedEntity> CachedEntities { get; set; } = null!;

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }

        public class CachedEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        /// <summary>
        /// Fake cache service for testing cache behavior without external dependencies.
        /// </summary>
        public class FakeCacheService : ICacheService
        {
            private readonly Dictionary<string, object> _cache = new();
            private readonly Dictionary<string, int> _getCounts = new();
            private readonly Dictionary<string, int> _setCounts = new();
            private readonly Dictionary<string, int> _removeByPatternCounts = new();

            public string? LastGetKey { get; private set; }
            public TimeSpan? LastExpiration { get; private set; }

            public Task<T?> GetAsync<T>(string key)
            {
                LastGetKey = key;
                _getCounts[key] = _getCounts.GetValueOrDefault(key) + 1;
                
                if (_cache.TryGetValue(key, out var value))
                {
                    return Task.FromResult((T?)value);
                }
                return Task.FromResult(default(T?));
            }

            public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
            {
                _cache[key] = value!;
                _setCounts[key] = _setCounts.GetValueOrDefault(key) + 1;
                LastExpiration = expiration;
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string key)
            {
                _cache.Remove(key);
                return Task.CompletedTask;
            }

            public Task RemoveByPatternAsync(string pattern)
            {
                _removeByPatternCounts[pattern] = _removeByPatternCounts.GetValueOrDefault(pattern) + 1;
                
                // Simple wildcard implementation for testing
                var keysToRemove = _cache.Keys
                    .Where(k => pattern == "*" || k.Contains(pattern.TrimEnd('*').TrimStart('*')))
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    _cache.Remove(key);
                }
                
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(string key)
            {
                return Task.FromResult(_cache.ContainsKey(key));
            }

            public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
            {
                var cached = await GetAsync<T>(key);
                if (cached != null)
                {
                    return cached;
                }

                var value = await factory();
                await SetAsync(key, value, expiration);
                return value;
            }

            public int GetCallCount(string key) => _getCounts.GetValueOrDefault(key);
            public int SetCallCount(string key) => _setCounts.GetValueOrDefault(key);
            public int RemoveByPatternCallCount(string pattern) => _removeByPatternCounts.GetValueOrDefault(pattern);
        }

        #endregion
    }
}
