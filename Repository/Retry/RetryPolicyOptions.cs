namespace Repo.Repository.Retry
{
    /// <summary>
    /// Configuration options for retry policies.
    /// </summary>
    public class RetryPolicyOptions
    {
        /// <summary>
        /// Maximum number of retry attempts. Default: 3
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;
        
        /// <summary>
        /// Initial delay before first retry. Default: 200ms
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(200);
        
        /// <summary>
        /// Maximum delay cap for exponential backoff. Default: 30 seconds
        /// </summary>
        public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// Whether to use exponential backoff. Default: true
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;
        
        /// <summary>
        /// Whether to add jitter to delays (recommended for distributed systems). Default: true
        /// </summary>
        public bool UseJitter { get; set; } = true;
        
        /// <summary>
        /// Enable/disable retry policy. Default: true
        /// </summary>
        public bool EnableRetry { get; set; } = true;
    }
}
