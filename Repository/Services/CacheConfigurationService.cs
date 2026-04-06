using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;

namespace Repo.Repository.Services
{
    /// <summary>
    /// Servicio de configuración de caché que permite elegir entre Redis y MemoryCache
    /// </summary>
    public static class CacheConfigurationService
    {
        /// <summary>
        /// Configura el servicio de caché según la disponibilidad de Redis
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

                    // Log para indicar que se está usando Redis
                    var logger = services.BuildServiceProvider().GetService<ILogger<RedisCacheService>>();
                    logger?.LogInformation("Configurando Redis como servicio de caché");
                }
                catch (Exception ex)
                {
                    // Si Redis falla, usar MemoryCache como fallback
                    services.AddMemoryCache();
                    services.AddScoped<ICacheService, MemoryCacheService>();

                    var logger = services.BuildServiceProvider().GetService<ILogger<MemoryCacheService>>();
                    logger?.LogWarning(ex, "Redis no disponible, usando MemoryCache como alternativa");
                }
            }
            else
            {
                // Si no hay configuración de Redis, usar MemoryCache
                services.AddMemoryCache();
                services.AddScoped<ICacheService, MemoryCacheService>();
            }

            return services;
        }

        /// <summary>
        /// Configura el servicio de caché forzando el uso de MemoryCache
        /// </summary>
        public static IServiceCollection AddMemoryCacheService(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCacheService>();
            return services;
        }

        /// <summary>
        /// Configura el servicio de caché forzando el uso de Redis
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