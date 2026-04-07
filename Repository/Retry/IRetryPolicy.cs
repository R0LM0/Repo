namespace Repo.Repository.Retry
{
    /// <summary>
    /// Defines a retry policy for transient fault handling.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Executes an operation with retry logic.
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Executes an operation with retry logic.
        /// </summary>
        Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    }
}
