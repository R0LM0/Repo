using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Repo.Repository.Retry;

namespace Repo.Repository.Extensions
{
    /// <summary>
    /// Extension methods for registering retry policies in the dependency injection container.
    /// </summary>
    public static class RetryServiceExtensions
    {
        /// <summary>
        /// Adds repository retry policy services to the dependency injection container.
        /// 
        /// Configuration Example:
        /// <code>
        /// services.AddRepositoryRetryPolicy(options =>
        /// {
        ///     options.MaxRetryAttempts = 5;
        ///     options.InitialDelay = TimeSpan.FromMilliseconds(100);
        ///     options.MaxDelay = TimeSpan.FromSeconds(60);
        ///     options.UseExponentialBackoff = true;
        ///     options.UseJitter = true;
        ///     options.EnableRetry = true;
        /// });
        /// </code>
        /// 
        /// To disable retry globally:
        /// <code>
        /// services.AddRepositoryRetryPolicy(options =>
        /// {
        ///     options.EnableRetry = false;
        /// });
        /// </code>
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for retry policy options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRepositoryRetryPolicy(
            this IServiceCollection services,
            Action<RetryPolicyOptions>? configure = null)
        {
            // Configure options
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                services.AddOptions<RetryPolicyOptions>();
            }

            // Register the retry policy as singleton (stateless, thread-safe)
            services.AddSingleton<IRetryPolicy, DefaultRetryPolicy>();

            return services;
        }

        /// <summary>
        /// Adds repository retry policy services with explicit options instance.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The retry policy options instance.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRepositoryRetryPolicy(
            this IServiceCollection services,
            RetryPolicyOptions options)
        {
            services.AddSingleton(Options.Create(options));
            services.AddSingleton<IRetryPolicy, DefaultRetryPolicy>();

            return services;
        }
    }
}
