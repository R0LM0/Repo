// -----------------------------------------------------------------------------
// RepoBase.Cache.cs
// -----------------------------------------------------------------------------
// Partial class: Caching Operations
// 
// Purpose:
//   Contains caching wrapper methods for entity retrieval operations.
//   Requires ICacheService to be provided during repository construction.
//
// Dependencies:
//   - ICacheService - Interface for cache operations
//
// Methods:
//   - GetByIdWithCacheAsync (2 overloads) - Get entity by ID with caching
//   - GetAllWithCacheAsync - Get all entities with caching
//   - InvalidateCacheAsync - Remove cached entries matching a pattern
// -----------------------------------------------------------------------------

using Repo.Repository.Interfaces;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region NEW METHODS - Cache
        /// <summary>
        /// Gets an entity by integer ID with caching support.
        /// If cache service is not configured, falls back to GetById.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cacheExpiration">Optional cache expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or freshly retrieved entity.</returns>
        public async Task<T?> GetByIdWithCacheAsync(int id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
        {
            if (CacheService == null)
                return await GetById(id, cancellationToken);

            var cacheKey = $"{typeof(T).Name}:{id}";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetById(id, cancellationToken), cacheExpiration);
        }

        /// <summary>
        /// Gets an entity by long ID with caching support.
        /// If cache service is not configured, falls back to GetById.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cacheExpiration">Optional cache expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or freshly retrieved entity.</returns>
        public async Task<T?> GetByIdWithCacheAsync(long id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
        {
            if (CacheService == null)
                return await GetById(id, cancellationToken);

            var cacheKey = $"{typeof(T).Name}:{id}";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetById(id, cancellationToken), cacheExpiration);
        }

        /// <summary>
        /// Gets all entities with caching support.
        /// If cache service is not configured, falls back to GetAllAsync.
        /// </summary>
        /// <param name="cacheExpiration">Optional cache expiration time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The cached or freshly retrieved list of entities.</returns>
        public async Task<IEnumerable<T>> GetAllWithCacheAsync(TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
        {
            if (CacheService == null)
                return await GetAllAsync(false, cancellationToken);

            var cacheKey = $"{typeof(T).Name}:All";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetAllAsync(false, cancellationToken), cacheExpiration);
        }

        /// <summary>
        /// Invalidates cache entries matching the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern to match cache keys (default: "*" for all).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task InvalidateCacheAsync(string pattern = "*", CancellationToken cancellationToken = default)
        {
            if (CacheService != null)
            {
                var cachePattern = $"{typeof(T).Name}:{pattern}";
                await CacheService.RemoveByPatternAsync(cachePattern);
            }
        }
        #endregion
    }
}
