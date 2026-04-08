using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;
using Repo.Repository.Retry;
using Repo.Repository.Security;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Base repository implementation for entity operations.
    /// 
    /// Transaction Management:
    /// - Use IUnitOfWork for all transaction orchestration
    /// - Repositories obtained via unitOfWork.Repository&lt;T&gt;() automatically participate in UnitOfWork transactions
    /// 
    /// Retry Policy for Transient Faults:
    /// - Read operations (GetAll, GetById, Find, etc.) automatically retry on transient failures
    /// - Configure via AddRepositoryRetryPolicy() in DI container
    /// - Only safe operations are retried. Write operations (Insert, Update, Delete) are NOT retried
    /// - Default: 3 retries with exponential backoff (200ms, 400ms, 800ms)
    /// 
    /// Recommended Usage:
    ///     using var unitOfWork = new UnitOfWork&lt;MyContext&gt;(context, logger);
    ///     await unitOfWork.BeginTransactionAsync();
    ///     try {
    ///         var repo = unitOfWork.Repository&lt;MyEntity&gt;();
    ///         await repo.Insert(entity);
    ///         await unitOfWork.CommitTransactionAsync();
    ///     } catch {
    ///         await unitOfWork.RollbackTransactionAsync();
    ///         throw;
    ///     }
    /// </summary>
    public partial class RepoBase<T, TContext> : IDisposable, IRepo<T>
       where T : class
       where TContext : DbContext
    {
        protected readonly TContext Db;
        protected readonly DbSet<T> Table;
        protected readonly ILogger Logger;  // Field for logging
        protected readonly ICacheService? CacheService; // Field for caching
        protected readonly IRetryPolicy? RetryPolicy; // Field for transient fault retry
        protected readonly IStoredProcedureWhitelist? Whitelist; // Field for stored procedure whitelist
        private bool _disposed = false;

        public RepoBase(TContext context, ILogger logger, ICacheService? cacheService = null, IRetryPolicy? retryPolicy = null, IStoredProcedureWhitelist? whitelist = null)
        {
            Db = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CacheService = cacheService;
            RetryPolicy = retryPolicy;
            Whitelist = whitelist;
            Table = Db.Set<T>();
        }

        #region Dispose
        public virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
                Db.Dispose();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
