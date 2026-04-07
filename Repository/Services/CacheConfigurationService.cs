using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;

namespace Repo.Repository.Services
{
    /// <summary>
    /// Cache configuration service that allows choosing between Redis and MemoryCache
    /// </summary>
    public static class CacheConfigurationService
    {
        /// <summary>
        /// Configures the cache service based on Redis availability
        /// </summary>
        public static IServiceCollection AddCacheService(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConnectionString = configuration.GetConnectionString("Redis");

            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                try
                {
                    // Intentar configurar Redis
                    services.AddStackExchangeRedisCache(options =>
                    {
                        options.Configuration = redisConnectionString;
                        options.InstanceName = "AdvancedRepo";
                    });

                    services.AddScoped<ICacheService, RedisCacheService>();

                    // Log to indicate Redis is being used
                    var logger = services.BuildServiceProvider().GetService<ILogger<RedisCacheService>>();
                    logger?.LogInformation("Configurando Redis como servicio de caché");
                }
                catch (Exception ex)
                {
                    // If Redis fails, use MemoryCache as fallback
                    services.AddMemoryCache();
                    services.AddScoped<ICacheService, MemoryCacheService>();

                    var logger = services.BuildServiceProvider().GetService<ILogger<MemoryCacheService>>();
                    logger?.LogWarning(ex, "Redis no disponible, usando MemoryCache como alternativa");
                }
            }
            else
            {
                // If no Redis configuration, use MemoryCache
                services.AddMemoryCache();
                services.AddScoped<ICacheService, MemoryCacheService>();
            }

            return services;
        }

        /// <summary>
        /// Configures the cache service forcing the use of MemoryCache
        /// </summary>
        public static IServiceCollection AddMemoryCacheService(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
            return services;
        }

        /// <summary>
        /// Configures the cache service forcing the use of Redis
        /// </summary>
        public static IServiceCollection AddRedisCacheService(this IServiceCollection services, string connectionString)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = "AdvancedRepo";
            });

            services.AddScoped<ICacheService, RedisCacheService>();
            return services;
        }
    }
}