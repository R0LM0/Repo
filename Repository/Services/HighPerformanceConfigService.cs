using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Data;

namespace Repo.Repository.Services
{
    /// <summary>
    /// Configuration service for optimizing database performance
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
        /// Configures DbContext for high performance
        /// </summary>
        public void ConfigureDbContext<TContext>(TContext context) where TContext : DbContext
        {
            try
            {
                // Critical configurations for high performance
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                context.ChangeTracker.AutoDetectChangesEnabled = false;
                context.ChangeTracker.LazyLoadingEnabled = false;

                // Configure command timeout
                context.Database.SetCommandTimeout(300); // 5 minutes

                // Configure SQL connection for high performance
                var connection = context.Database.GetDbConnection();
                if (connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }

                // SQL Server specific configurations
                if (connection is Microsoft.Data.SqlClient.SqlConnection sqlConnection)
                {
                    ConfigureSqlConnection(sqlConnection);
                }

                _logger.LogInformation("DbContext configured for high performance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring DbContext for high performance");
                throw;
            }
        }

        /// <summary>
        /// Configures SQL Server connection for high performance
        /// </summary>
        private void ConfigureSqlConnection(Microsoft.Data.SqlClient.SqlConnection connection)
        {
            // Connection configurations for high performance
            var connectionStringBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.ConnectionString)
            {
                // Optimized connection pool
                MaxPoolSize = 200,
                MinPoolSize = 10,
                Pooling = true,

                // Performance configurations
                MultipleActiveResultSets = true,
                Enlist = false,
                LoadBalanceTimeout = 30,

                // Network configurations
                ConnectTimeout = 30,
                CommandTimeout = 300,

                // Memory configurations
                PacketSize = 8192,

                // Security configurations (adjust as needed)
                IntegratedSecurity = false,
                PersistSecurityInfo = false
            };

            // Apply configuration if possible
            if (connection.State == ConnectionState.Open)
            {
                connection.Close();
            }

            connection.ConnectionString = connectionStringBuilder.ConnectionString;
        }

        /// <summary>
        /// Gets optimized configuration for bulk operations
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
        /// Gets concurrency configuration
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
        /// Gets cache configuration
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
        /// Gets monitoring configuration
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
    /// Configuration for bulk operations
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
    /// Concurrency configuration
    /// </summary>
    public class ConcurrencyConfig
    {
        public int MaxConcurrency { get; set; } = 4;
        public int MaxBatchSize { get; set; } = 1000;
        public int BatchTimeoutMs { get; set; } = 5000;
        public int SemaphoreTimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// Cache configuration
    /// </summary>
    public class CacheConfig
    {
        public bool EnableCache { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 30;
        public int CacheMaxSize { get; set; } = 10000;
        public bool EnableDistributedCache { get; set; } = false;
    }

    /// <summary>
    /// Monitoring configuration
    /// </summary>
    public class MonitoringConfig
    {
        public bool EnablePerformanceMonitoring { get; set; } = true;
        public bool LogSlowQueries { get; set; } = true;
        public int SlowQueryThresholdMs { get; set; } = 1000;
        public bool EnableMetrics { get; set; } = true;
    }
}