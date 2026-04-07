using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;

namespace Repo.Repository.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheService> _logger;

        // registro de todas las keys guardadas
        private readonly ConcurrentDictionary<string, byte> _keys = new();

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

                // expiración absoluta por defecto 1h
                options.SetAbsoluteExpiration(expiration ?? TimeSpan.FromHours(1));

                // cuando se evicte, quitar del registro
                options.RegisterPostEvictionCallback((k, v, reason, state) =>
                {
                    _keys.TryRemove(k.ToString()!, out _);
                });

                _cache.Set(key, value!, options);

                // registrar la clave
                _keys.TryAdd(key, 0);

                _logger.LogDebug("Cache SET {Key}. KeysCount={Count}", key, _keys.Count);


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
                _keys.TryRemove(key, out _);
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
                // Soportamos prefijos tipo "v_proyectos:*"
                var prefix = pattern.EndsWith("*") ? pattern[..^1] : pattern;

                var keysToRemove = _keys.Keys
                    .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var k in keysToRemove)
                {
                    _cache.Remove(k);
                    _keys.TryRemove(k, out _);
                }

                _logger.LogDebug("Removed {Count} cache items by pattern {Pattern}", keysToRemove.Count, pattern);
                
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
                // fallback sin cache
                return await factory();
            }
        }
    }
}
