using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Repo.Repository.Retry
{
    /// <summary>
    /// Default implementation of retry policy with exponential backoff and jitter.
    /// 
    /// This policy handles transient database faults including:
    /// - SQL Server timeout errors (-2)
    /// - Connection failures (53, 258)
    /// - Login failures (4060, 18456, 18452, 18450)
    /// - Azure SQL transient errors (40197, 40501, 40613)
    /// - General timeout exceptions
    /// 
    /// IMPORTANT: This policy should ONLY be used for safe operations (reads).
    /// Write operations (Insert, Update, Delete) should NOT be retried unless they are idempotent.
    /// </summary>
    public class DefaultRetryPolicy : IRetryPolicy
    {
        private readonly RetryPolicyOptions _options;
        private readonly ILogger<DefaultRetryPolicy>? _logger;

        /// <summary>
        /// SQL Server transient error numbers based on Microsoft documentation.
        /// Reference: https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues
        /// </summary>
        private static readonly int[] TransientSqlErrorNumbers =
        {
            -2,     // Timeout expired
            53,     // Could not establish connection
            258,    // Timeout waiting for server response
            4060,   // Cannot open database
            18456,  // Login failed for user
            18452,  // Login failed - untrusted domain
            18450,  // Login failed - user not associated with trusted connection
            40197,  // Azure SQL - service error
            40501,  // Azure SQL - service busy
            40613,  // Azure SQL - database unavailable
            11001,  // Host not found
            10928,  // Azure SQL - resource limit
            10929,  // Azure SQL - resource limit
            233,    // Client initialization error
            1205,   // Deadlock victim
            121,    // Semaphore timeout
            64,     // Named pipe error
            10054,  // Network error
            10060,  // Connection timeout
            19,     // Physical connection failure
            20      // Instance failure
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRetryPolicy"/> class.
        /// </summary>
        /// <param name="options">The retry policy options.</param>
        /// <param name="logger">Optional logger for retry attempts.</param>
        public DefaultRetryPolicy(IOptions<RetryPolicyOptions> options, ILogger<DefaultRetryPolicy>? logger = null)
        {
            _options = options?.Value ?? new RetryPolicyOptions();
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRetryPolicy"/> class with explicit options.
        /// </summary>
        /// <param name="options">The retry policy options.</param>
        /// <param name="logger">Optional logger for retry attempts.</param>
        public DefaultRetryPolicy(RetryPolicyOptions options, ILogger<DefaultRetryPolicy>? logger = null)
        {
            _options = options ?? new RetryPolicyOptions();
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableRetry)
            {
                return await operation();
            }

            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= _options.MaxRetryAttempts)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger?.LogInformation("Retry attempt {Attempt} of {MaxAttempts}", attempt, _options.MaxRetryAttempts);
                    }

                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt >= _options.MaxRetryAttempts || !IsTransientException(ex))
                    {
                        _logger?.LogError(ex, "Operation failed after {Attempts} attempts", attempt + 1);
                        throw;
                    }

                    var delay = CalculateDelay(attempt);
                    
                    _logger?.LogWarning(
                        ex,
                        "Transient error detected (attempt {Attempt}/{MaxAttempts}). Retrying in {DelayMs}ms. Error: {ErrorMessage}",
                        attempt + 1,
                        _options.MaxRetryAttempts,
                        delay.TotalMilliseconds,
                        ex.Message);

                    await Task.Delay(delay, cancellationToken);
                    attempt++;
                }
            }

            // This should never be reached, but just in case
            throw new InvalidOperationException("Retry loop completed without success or proper exception", lastException);
        }

        /// <inheritdoc />
        public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableRetry)
            {
                await operation();
                return;
            }

            int attempt = 0;
            Exception? lastException = null;

            while (attempt <= _options.MaxRetryAttempts)
            {
                try
                {
                    if (attempt > 0)
                    {
                        _logger?.LogInformation("Retry attempt {Attempt} of {MaxAttempts}", attempt, _options.MaxRetryAttempts);
                    }

                    await operation();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (attempt >= _options.MaxRetryAttempts || !IsTransientException(ex))
                    {
                        _logger?.LogError(ex, "Operation failed after {Attempts} attempts", attempt + 1);
                        throw;
                    }

                    var delay = CalculateDelay(attempt);
                    
                    _logger?.LogWarning(
                        ex,
                        "Transient error detected (attempt {Attempt}/{MaxAttempts}). Retrying in {DelayMs}ms. Error: {ErrorMessage}",
                        attempt + 1,
                        _options.MaxRetryAttempts,
                        delay.TotalMilliseconds,
                        ex.Message);

                    await Task.Delay(delay, cancellationToken);
                    attempt++;
                }
            }

            // This should never be reached, but just in case
            throw new InvalidOperationException("Retry loop completed without success or proper exception", lastException);
        }

        /// <summary>
        /// Determines whether an exception is transient and should be retried.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>True if the exception is transient; otherwise, false.</returns>
        private bool IsTransientException(Exception ex)
        {
            // Check for timeout exceptions
            if (ex is TimeoutException)
            {
                return true;
            }

            // Check for SQL exceptions
            if (ex is SqlException sqlEx)
            {
                return IsTransientSqlError(sqlEx);
            }

            // Check for EF Core transient exceptions
            if (ex is InvalidOperationException && 
                (ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase) ||
                 ex.Message.Contains("retry", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Check for operation canceled (might be due to timeout)
            if (ex is OperationCanceledException)
            {
                return true;
            }

            // Check inner exception recursively
            if (ex.InnerException != null)
            {
                return IsTransientException(ex.InnerException);
            }

            return false;
        }

        /// <summary>
        /// Determines whether a SQL exception is transient.
        /// </summary>
        /// <param name="ex">The SQL exception to check.</param>
        /// <returns>True if the SQL error is transient; otherwise, false.</returns>
        private bool IsTransientSqlError(SqlException ex)
        {
            return TransientSqlErrorNumbers.Contains(ex.Number);
        }

        /// <summary>
        /// Calculates the delay for the next retry attempt using exponential backoff with optional jitter.
        /// </summary>
        /// <param name="attempt">The current attempt number (0-based).</param>
        /// <returns>The delay to wait before the next retry.</returns>
        private TimeSpan CalculateDelay(int attempt)
        {
            double delayMs;

            if (_options.UseExponentialBackoff)
            {
                // Exponential backoff: initial * 2^attempt
                delayMs = _options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt);
            }
            else
            {
                // Fixed delay
                delayMs = _options.InitialDelay.TotalMilliseconds;
            }

            // Add jitter to prevent thundering herd (recommended for distributed systems)
            if (_options.UseJitter)
            {
                var jitter = Random.Shared.NextDouble() * 0.1 * delayMs; // 10% jitter
                delayMs += jitter;
            }

            // Cap at maximum delay
            delayMs = Math.Min(delayMs, _options.MaxDelay.TotalMilliseconds);

            return TimeSpan.FromMilliseconds(delayMs);
        }
    }
}
