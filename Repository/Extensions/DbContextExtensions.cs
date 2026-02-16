using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repo.Repository.Services;

namespace Repo.Repository.Extensions
{
    /// <summary>
    /// Extensiones para configurar DbContext con múltiples proveedores de base de datos
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Configura DbContext con soporte para SQL Server y PostgreSQL
        /// </summary>
        /// <typeparam name="TContext">Tipo del DbContext</typeparam>
        /// <param name="services">Colección de servicios</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        /// <param name="connectionString">Cadena de conexión (opcional, si no se proporciona se usa DatabaseProviderService)</param>
        /// <param name="provider">Proveedor de base de datos (opcional, si no se proporciona se lee de configuración)</param>
        /// <returns>DbContextOptionsBuilder para encadenar configuraciones adicionales</returns>
        public static IServiceCollection AddDbContextWithProvider<TContext>(
            this IServiceCollection services,
            IConfiguration configuration,
            string? connectionString = null,
            DatabaseProvider? provider = null)
            where TContext : DbContext
        {
            // Registrar DatabaseProviderService
            services.AddScoped<DatabaseProviderService>();

            // Configurar DbContext
            services.AddDbContext<TContext>((serviceProvider, options) =>
            {
                var dbProviderService = serviceProvider.GetRequiredService<DatabaseProviderService>();
                
                // Convertir DbContextOptionsBuilder a DbContextOptionsBuilder<TContext>
                var typedOptions = (DbContextOptionsBuilder<TContext>)options;
                
                // Si se proporciona proveedor explícitamente, usarlo
                if (provider.HasValue)
                {
                    ConfigureDbContextByProvider(typedOptions, provider.Value, connectionString ?? dbProviderService.GetConnectionString());
                }
                else
                {
                    // Usar configuración del servicio
                    dbProviderService.ConfigureDbContext(typedOptions);
                }
            });

            return services;
        }

        /// <summary>
        /// Configura DbContext con un proveedor específico
        /// </summary>
        public static DbContextOptionsBuilder<TContext> ConfigureDbContextByProvider<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            DatabaseProvider provider,
            string connectionString)
            where TContext : DbContext
        {
            switch (provider)
            {
                case DatabaseProvider.PostgreSQL:
                    optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name);
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                    });
                    break;

                case DatabaseProvider.SqlServer:
                    optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(TContext).Assembly.GetName().Name);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
                    break;

                default:
                    throw new NotSupportedException($"Proveedor de base de datos '{provider}' no soportado");
            }

            return optionsBuilder;
        }

        /// <summary>
        /// Configura DbContext para SQL Server
        /// </summary>
        public static DbContextOptionsBuilder<TContext> UseSqlServerProvider<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string connectionString)
            where TContext : DbContext
        {
            return optionsBuilder.ConfigureDbContextByProvider(DatabaseProvider.SqlServer, connectionString);
        }

        /// <summary>
        /// Configura DbContext para PostgreSQL
        /// </summary>
        public static DbContextOptionsBuilder<TContext> UsePostgreSQLProvider<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            string connectionString)
            where TContext : DbContext
        {
            return optionsBuilder.ConfigureDbContextByProvider(DatabaseProvider.PostgreSQL, connectionString);
        }
    }
}
