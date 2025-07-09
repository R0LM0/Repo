using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Data;

namespace Repo.Repository.Services
{
    /// <summary>
    /// Servicio de configuración para optimizar el rendimiento de la base de datos
    /// </summary>
    public class HighPerformanceConfigService
    {
        private readonly ILogger<HighPerformanceConfigService> _logger;
        private readonly IConfiguration _configuration;

        public HighPerformanceConfigService(ILogger<HighPerformanceConfigService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Configura DbContext para alto rendimiento
        /// </summary>
        public void ConfigureDbContext<TContext>(TContext context) where TContext : DbContext
        {
            try
            {
                // Configuraciones críticas para alto rendimiento
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                context.ChangeTracker.AutoDetectChangesEnabled = false;
                context.ChangeTracker.LazyLoadingEnabled = false;

                // Configurar timeout de comandos
                context.Database.SetCommandTimeout(300); // 5 minutos

                // Configurar conexión SQL para alto rendimiento
                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                // Configuraciones específicas para SQL Server
                if (connection is Microsoft.Data.SqlClient.SqlConnection sqlConnection)
                {
                    ConfigureSqlConnection(sqlConnection);
                }

                _logger.LogInformation("DbContext configurado para alto rendimiento");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configurando DbContext para alto rendimiento");
                throw;
            }
        }

        /// <summary>
        /// Configura conexión SQL Server para alto rendimiento
        /// </summary>
        private void ConfigureSqlConnection(Microsoft.Data.SqlClient.SqlConnection connection)
        {
            // Configuraciones de conexión para alto rendimiento
            var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.ConnectionString)
            {
                // Pool de conexiones optimizado
                MaxPoolSize = 200,
                MinPoolSize = 10,
                Pooling = true,

                // Configuraciones de rendimiento
                MultipleActiveResultSets = true,
                Enlist = false,
                LoadBalanceTimeout = 30,

                // Configuraciones de red
                ConnectTimeout = 30,
                CommandTimeout = 300,

                // Configuraciones de memoria
                PacketSize = 8192,

                // Configuraciones de seguridad (ajustar según necesidades)
                IntegratedSecurity = false,
                PersistSecurityInfo = false
            };

            // Aplicar configuración si es posible
            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }

            connection.ConnectionString = connectionStringBuilder.ConnectionString;
        }

        /// <summary>
        /// Obtiene configuración optimizada para bulk operations
        /// </summary>
        public BulkOperationConfig GetBulkOperationConfig()
        {
            return new BulkOperationConfig
            {
                BatchSize = 1000,
                UseTempDB = true,
                SetOutputIdentity = false,
                PreserveInsertOrder = false,
                WithHoldlock = false,
                EnableStreaming = true,
                BulkCopyTimeout = 300,
                NotifyAfter = 1000
            };
        }

        /// <summary>
        /// Obtiene configuración de concurrencia
        /// </summary>
        public ConcurrencyConfig GetConcurrencyConfig()
        {
            return new ConcurrencyConfig
            {
                MaxConcurrency = 4,
                MaxBatchSize = 1000,
                BatchTimeoutMs = 5000,
                SemaphoreTimeoutMs = 30000
            };
        }

        /// <summary>
        /// Obtiene configuración de caché
        /// </summary>
        public CacheConfig GetCacheConfig()
        {
            return new CacheConfig
            {
                EnableCache = true,
                CacheExpirationMinutes = 30,
                CacheMaxSize = 10000,
                EnableDistributedCache = false
            };
        }

        /// <summary>
        /// Obtiene configuración de monitoreo
        /// </summary>
        public MonitoringConfig GetMonitoringConfig()
        {
            return new MonitoringConfig
            {
                EnablePerformanceMonitoring = true,
                LogSlowQueries = true,
                SlowQueryThresholdMs = 1000,
                EnableMetrics = true
            };
        }
    }

    /// <summary>
    /// Configuración para operaciones bulk
    /// </summary>
    public class BulkOperationConfig
    {
        public int BatchSize { get; set; } = 1000;
        public bool UseTempDB { get; set; } = true;
        public bool SetOutputIdentity { get; set; } = false;
        public bool PreserveInsertOrder { get; set; } = false;
        public bool WithHoldlock { get; set; } = false;
        public bool EnableStreaming { get; set; } = true;
        public int BulkCopyTimeout { get; set; } = 300;
        public int NotifyAfter { get; set; } = 1000;
    }

    /// <summary>
    /// Configuración de concurrencia
    /// </summary>
    public class ConcurrencyConfig
    {
        public int MaxConcurrency { get; set; } = 4;
        public int MaxBatchSize { get; set; } = 1000;
        public int BatchTimeoutMs { get; set; } = 5000;
        public int SemaphoreTimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Configuración de caché
    /// </summary>
    public class CacheConfig
    {
        public bool EnableCache { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 30;
        public int CacheMaxSize { get; set; } = 10000;
        public bool EnableDistributedCache { get; set; } = false;
    }

    /// <summary>
    /// Configuración de monitoreo
    /// </summary>
    public class MonitoringConfig
    {
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public bool LogSlowQueries { get; set; } = true;
        public int SlowQueryThresholdMs { get; set; } = 1000;
        public bool EnableMetrics { get; set; } = true;
    }
}