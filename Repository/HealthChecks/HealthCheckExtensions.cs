using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Repo.Repository.HealthChecks
{
    /// <summary>
    /// Extension methods for registering repository health checks.
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Adds a repository health check for the specified DbContext.
        /// </summary>
        /// <typeparam name="TContext">The DbContext type.</typeparam>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="name">The name of the health check. Defaults to "repository_{ContextName}".</param>
        /// <param name="failureStatus">The status to report when the health check fails. Defaults to Unhealthy.</param>
        /// <param name="tags">Tags for the health check.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddRepositoryCheck<TContext>(
            this IHealthChecksBuilder builder,
            string? name = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null) where TContext : DbContext
        {
            name ??= $"repository_{typeof(TContext).Name}";
            tags ??= new[] { "repository", "database", "efcore" };

            return builder.AddCheck<RepositoryHealthCheck<TContext>>(name, failureStatus, tags);
        }
    }
}
