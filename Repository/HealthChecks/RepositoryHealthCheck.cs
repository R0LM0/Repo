using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Repo.Repository.HealthChecks
{
    /// <summary>
    /// Health check for repository database connectivity.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    public class RepositoryHealthCheck<TContext> : IHealthCheck where TContext : DbContext
    {
        private readonly TContext _context;
        private readonly ILogger<RepositoryHealthCheck<TContext>> _logger;

        /// <summary>
        /// Creates a new repository health check.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public RepositoryHealthCheck(TContext context, ILogger<RepositoryHealthCheck<TContext>> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Checks the health of the database connection.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The health check result.</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Try to connect to the database
                await _context.Database.CanConnectAsync(cancellationToken);
                
                _logger.LogDebug("Database health check passed for {Context}", typeof(TContext).Name);
                return HealthCheckResult.Healthy($"Database connection for {typeof(TContext).Name} is healthy.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed for {Context}", typeof(TContext).Name);
                return HealthCheckResult.Unhealthy($"Database connection for {typeof(TContext).Name} is unhealthy.", ex);
            }
        }
    }
}
