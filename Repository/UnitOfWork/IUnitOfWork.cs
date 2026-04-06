using Microsoft.EntityFrameworkCore.Storage;
using Repo.Repository.Base;
using System.Data;

namespace Repo.Repository.UnitOfWork
{
    /// <summary>
    /// Unit of Work - Central transaction orchestrator for the repository pattern.
    /// 
    /// This interface provides the PRIMARY entry point for transaction management.
    /// All transaction operations should be coordinated through UnitOfWork, not individual repositories.
    /// 
    /// Usage Pattern:
    ///     await unitOfWork.BeginTransactionAsync();
    ///     try {
    ///         // Perform operations via repositories obtained from unitOfWork.Repository&lt;T&gt;()
    ///         await unitOfWork.CommitTransactionAsync();
    ///     } catch {
    ///         await unitOfWork.RollbackTransactionAsync();
    ///         throw;
    ///     }
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets a repository for the specified entity type.
        /// Repositories obtained through this method participate in the UnitOfWork's transaction scope.
        /// </summary>
        IRepo<T> Repository<T>() where T : class;

        /// <summary>
        /// Saves all pending changes to the database.
        /// When inside a transaction, changes are persisted but not committed until CommitTransactionAsync is called.
        /// </summary>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Begins a new database transaction with default isolation level.
        /// This is the PRIMARY method for starting transactions - use UnitOfWork, not repository methods.
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// Begins a new database transaction with specified isolation level.
        /// Use this overload when you need explicit control over transaction isolation behavior.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction (e.g., ReadCommitted, Serializable, Snapshot).</param>
        Task BeginTransactionAsync(IsolationLevel isolationLevel);

        /// <summary>
        /// Commits the current transaction.
        /// All changes made since BeginTransactionAsync are permanently saved.
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the current transaction.
        /// All changes made since BeginTransactionAsync are discarded.
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Gets the current database transaction if one is active.
        /// Returns null if no transaction is in progress.
        /// </summary>
        IDbContextTransaction? CurrentTransaction { get; }

        /// <summary>
        /// Returns true if a transaction is currently active.
        /// </summary>
        bool HasActiveTransaction { get; }

        Task<bool> HasChangesAsync();
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
        Task<IEnumerable<T>> ExecuteSqlRawAsync<T>(string sql, params object[] parameters) where T : class;
    }

    public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        TContext Context { get; }
    }
}