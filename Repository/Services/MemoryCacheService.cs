using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;

namespace Repo.Repository.Services
{
    /// <summary>
    /// Implementación de caché en memoria como alternativa a Redis
    /// </summary>
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            try
            {
                _cache.TryGetValue(key, out T? value);
                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache for key: {Key}", key);
                return Task.FromResult<T?>(default);
            }
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new MemoryCacheEntryOptions();
                if (expiration.HasValue)
                    options.SetAbsoluteExpiration(expiration.Value);
                else
                    options.SetAbsoluteExpiration(TimeSpan.FromHours(1)); // Default 1 hora

                _cache.Set(key, value, options);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
                return Task.CompletedTask;
            }
        }

        public Task RemoveAsync(string key)
        {
            try
            {
                _cache.Remove(key);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
                return Task.CompletedTask;
            }
        }

        public Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                // Implementación simplificada para MemoryCache
                // En un entorno real, podrías mantener un registro de las claves
                _logger.LogWarning("RemoveByPatternAsync no completamente implementado en MemoryCache");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
                return Task.CompletedTask;
            }
        }

        public Task<bool> ExistsAsync(string key)
        {
            try
            {
                return Task.FromResult(_cache.TryGetValue(key, out _));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return Task.FromResult(false);
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                if (_cache.TryGetValue(key, out T? value))
                    return value!;

                var newValue = await factory();
                await SetAsync(key, newValue, expiration);
                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);
                return await factory(); // Fallback sin caché
            }
        }
    }
}