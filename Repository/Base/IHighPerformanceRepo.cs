using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Repo.Repository.Models;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Interface for high-performance operations optimized for thousands of transactions per second
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IHighPerformanceRepo<T> where T : class
    {
        #region Bulk Operations Optimizadas
        /// <summary>
        /// Inserts thousands of records optimized using bulk operations
        /// </summary>
        Task<int> BulkInsertAsync(IEnumerable<T> entities, int batchSize = 1000);

        /// <summary>
        /// Updates thousands of records optimized
        /// </summary>
        Task<int> BulkUpdateAsync(IEnumerable<T> entities, int batchSize = 1000);

        /// <summary>
        /// Deletes thousands of records optimized
        /// </summary>
        Task<int> BulkDeleteAsync(IEnumerable<T> entities, int batchSize = 1000);

        /// <summary>
        /// Deletes records by predicate using bulk operations
        /// </summary>
        Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Optimized merge (insert/update) operation
        /// </summary>
        Task<int> BulkMergeAsync(IEnumerable<T> entities, int batchSize = 1000);
        #endregion

        #region Batch Processing
        /// <summary>
        /// Processes large data volumes in batches to avoid memory issues
        /// </summary>
        Task ProcessBatchAsync<TResult>(
            Expression<Func<T, bool>> filter,
            Func<IEnumerable<T>, Task<TResult>> processor,
            int batchSize = 1000,
            int maxConcurrency = 4);

        /// <summary>
        /// Processes data in parallel with concurrency control
        /// </summary>
        Task ProcessParallelAsync<TResult>(
            IEnumerable<T> data,
            Func<T, Task<TResult>> processor,
            int maxConcurrency = 4);
        #endregion

        #region Streaming Operations
        /// <summary>
        /// Gets data in streaming to avoid loading everything in memory
        /// </summary>
        IAsyncEnumerable<T> StreamAsync(Expression<Func<T, bool>>? filter = null, int bufferSize = 1000);

        /// <summary>
        /// Gets optimized paginated data for large volumes
        /// </summary>
        Task<PagedResult<T>> GetPagedOptimizedAsync(PagedRequest request, Expression<Func<T, bool>>? filter = null);
        #endregion

        #region Performance Monitoring
        /// <summary>
        /// Executes operation with performance metrics
        /// </summary>
        Task<PerformanceMetrics> ExecuteWithMetricsAsync(Func<Task> operation);

        /// <summary>
        /// Executes operation with performance metrics and returns result
        /// </summary>
        Task<(TResult Result, PerformanceMetrics Metrics)> ExecuteWithMetricsAsync<TResult>(Func<Task<TResult>> operation);
        #endregion

        #region Connection Management
        /// <summary>
        /// Configures context for high-performance operations
        /// </summary>
        void ConfigureForHighPerformance(int commandTimeout = 300, bool enableRetryOnFailure = true);

        /// <summary>
        /// Clears context to free memory
        /// </summary>
        Task ClearContextAsync();
        #endregion

        #region Transaction Management
        /// <summary>
        /// Executes operation in optimized transaction for large volumes
        /// </summary>
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// Executes multiple operations in distributed transaction
        /// </summary>
        Task ExecuteDistributedTransactionAsync(params Func<Task>[] operations);
        #endregion
    }

    /// <summary>
    /// Performance metrics for monitoring
    /// </summary>
    public class PerformanceMetrics
    {
        public TimeSpan ExecutionTime { get; set; }
        public long MemoryUsageBefore { get; set; }
        public long MemoryUsageAfter { get; set; }
        public int RecordsProcessed { get; set; }
        public double RecordsPerSecond => RecordsProcessed / ExecutionTime.TotalSeconds;
        public Exception? Exception { get; set; }
    }
}