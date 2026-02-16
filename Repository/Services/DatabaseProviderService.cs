using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Repo.Repository.Services
{
    /// <summary>
    /// Enum para identificar el proveedor de base de datos
    /// </summary>
    public enum DatabaseProvider
    {
        SqlServer,
        PostgreSQL
    }

    /// <summary>
    /// Servicio para configurar el proveedor de base de datos de forma dinámica
    /// </summary>
    public class DatabaseProviderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseProviderService> _logger;

        public DatabaseProviderService(IConfiguration configuration, ILogger<DatabaseProviderService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene el proveedor de base de datos configurado desde appsettings.json
        /// </summary>
        public DatabaseProvider GetProvider()
        {
            var provider = _configuration["Database:Provider"] ?? "SqlServer";
            
            if (Enum.TryParse<DatabaseProvider>(provider, ignoreCase: true, out var result))
            {
                _logger.LogInformation("Proveedor de base de datos configurado: {Provider}", result);
                return result;
            }

            _logger.LogWarning("Proveedor '{Provider}' no reconocido. Usando SqlServer por defecto.", provider);
            return DatabaseProvider.SqlServer;
        }

        /// <summary>
        /// Obtiene la cadena de conexión según el proveedor configurado
        /// </summary>
        public string GetConnectionString()
        {
            var provider = GetProvider();
            var connectionStringKey = provider switch
            {
                DatabaseProvider.PostgreSQL => "Database:PostgreSQL:ConnectionString",
                DatabaseProvider.SqlServer => "Database:SqlServer:ConnectionString",
                _ => "ConnectionStrings:DefaultConnection"
            };

            var connectionString = _configuration[connectionStringKey] 
                ?? _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException($"No se encontró la cadena de conexión para el proveedor {provider}");

            _logger.LogInformation("Cadena de conexión obtenida para proveedor: {Provider}", provider);
            return connectionString;
        }

        /// <summary>
        /// Configura DbContextOptionsBuilder según el proveedor configurado
        /// </summary>
        public void ConfigureDbContext<TContext>(DbContextOptionsBuilder<TContext> optionsBuilder) 
            where TContext : DbContext
        {
            var provider = GetProvider();
            var connectionString = GetConnectionString();

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
                    _logger.LogInformation("DbContext configurado para PostgreSQL");
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
                    _logger.LogInformation("DbContext configurado para SQL Server");
                    break;

                default:
                    throw new NotSupportedException($"Proveedor de base de datos '{provider}' no soportado");
            }
        }
    }
}
