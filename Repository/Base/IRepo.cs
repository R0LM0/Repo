using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Repo.Repository.Models;
using Repo.Repository.Specifications;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Repository interface for entity operations.
    /// 
    /// Transaction Management:
    /// - Use IUnitOfWork for all transaction orchestration
    /// - Repositories obtained via IUnitOfWork.Repository&lt;T&gt;() automatically participate in UnitOfWork transactions
    /// 
    /// Retry Policy for Transient Faults:
    /// - Read operations (GetAllAsync, GetById, FindAsync, GetPagedAsync, GetBySpecAsync, GetAllBySpecAsync) 
    ///   automatically retry on transient database failures
    /// - Configure via AddRepositoryRetryPolicy() in DI container during startup
    /// - Only safe operations are retried. Write operations (Insert, Update, Delete) are NOT retried
    ///   unless they are explicitly idempotent
    /// - Default: 3 retries with exponential backoff (200ms, 400ms, 800ms)
    /// - Handles: SqlException transient errors, TimeoutException, InvalidOperationException (EF Core transient errors)
    /// - To disable retry globally: services.AddRepositoryRetryPolicy(options => options.EnableRetry = false)
    /// </summary>
    public interface IRepo<T> where T : class
    {
        /// <summary>
        /// Finds an entity by its integer ID.
        /// </summary>
        /// <param name="id">The entity ID. Can be null.</param>
        /// <returns>The found entity.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        T Find(int? id);

        /// <summary>
        /// Finds an entity by its long ID (bigint).
        /// </summary>
        /// <param name="id">The entity ID. Can be null.</param>
        /// <returns>The found entity.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        T Find(long? id);

        /// <summary>
        /// Gets all entities with optional no-tracking.
        /// </summary>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <returns>An enumerable of all entities.</returns>
        IEnumerable<T> GetAll(bool asNoTracking = false);

        /// <summary>
        /// Gets all entities with optional no-tracking (overload with unused id parameter for API compatibility).
        /// </summary>
        /// <param name="id">Unused parameter for API compatibility.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <returns>An enumerable of all entities.</returns>
        IEnumerable<T> GetAll(int id, bool asNoTracking = false);

        /// <summary>
        /// Adds a new entity to the context.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="persist">If true, saves changes immediately. Default is true.</param>
        /// <returns>The number of affected rows if persisted, otherwise 0.</returns>
        int Add(T entity, bool persist = true);

        /// <summary>
        /// Updates an existing entity in the context.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="persist">If true, saves changes immediately. Default is true.</param>
        /// <returns>The number of affected rows if persisted, otherwise 0.</returns>
        int Update(T entity, bool persist = true);

        /// <summary>
        /// Deletes an entity from the context.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="persist">If true, saves changes immediately. Default is true.</param>
        /// <returns>The number of affected rows if persisted, otherwise 0.</returns>
        int Delete(T entity, bool persist = true);

        /// <summary>
        /// Saves all changes made in the context to the database.
        /// </summary>
        /// <returns>The number of affected rows.</returns>
        int Save();

        /// <summary>
        /// Gets all entities asynchronously with optional no-tracking.
        /// </summary>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of all entities.</returns>
        Task<IEnumerable<T>> GetAllAsync(bool asNoTracking = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities asynchronously with optional no-tracking (overload with unused id parameter).
        /// </summary>
        /// <param name="id">Unused parameter for API compatibility.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of all entities.</returns>
        Task<IEnumerable<T>> GetAllAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by its integer ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the found entity.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task<T> GetById(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by its long ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the found entity.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task<T> GetById(long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a new entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the inserted entity.</returns>
        Task<T> Insert(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the updated entity.</returns>
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by its integer ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by its long ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task DeleteAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all changes made in the context to the database asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        Task<int> SaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a stored procedure that returns a result set asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of entities returned by the stored procedure.</typeparam>
        /// <param name="storedProcedure">The name of the stored procedure.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="parameters">Parameters to pass to the stored procedure.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of results.</returns>
        /// <exception cref="ArgumentException">Thrown when stored procedure name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when stored procedure is not whitelisted.</exception>
        Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters) where TResult : class;

        /// <summary>
        /// Executes a stored procedure that does not return a result set asynchronously.
        /// </summary>
        /// <param name="storedProcedure">The name of the stored procedure.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="parameters">Parameters to pass to the stored procedure.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        /// <exception cref="ArgumentException">Thrown when stored procedure name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when stored procedure is not whitelisted.</exception>
        Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters);

        /// <summary>
        /// Executes a scalar database function and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the scalar function.</typeparam>
        /// <param name="functionName">Name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>A task representing the asynchronous operation, containing the scalar result.</returns>
        /// <exception cref="ArgumentException">Thrown when function name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when function is not whitelisted.</exception>
        Task<TResult> ExecuteScalarFunctionAsync<TResult>(string functionName, params object[] parameters);

        /// <summary>
        /// Executes a table-valued database function and returns the results.
        /// </summary>
        /// <typeparam name="TResult">The entity type returned by the function.</typeparam>
        /// <param name="functionName">Name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of results from the table-valued function.</returns>
        /// <exception cref="ArgumentException">Thrown when function name is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when function is not whitelisted.</exception>
        Task<IEnumerable<TResult>> ExecuteTableValuedFunctionAsync<TResult>(string functionName, params object[] parameters) where TResult : class;

        /// <summary>
        /// Gets a paginated result set asynchronously.
        /// </summary>
        /// <param name="request">The pagination request containing page number, page size, and sorting options.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing a paginated result set.</returns>
        Task<PagedResult<T>> GetPagedAsync(PagedRequest request, bool asNoTracking = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a filtered and paginated result set asynchronously.
        /// </summary>
        /// <param name="filter">Expression to filter entities.</param>
        /// <param name="request">The pagination request containing page number, page size, and sorting options.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing a filtered paginated result set.</returns>
        Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>> filter, PagedRequest request, bool asNoTracking = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a single entity by specification asynchronously.
        /// </summary>
        /// <param name="spec">The specification containing criteria and includes.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the found entity or null if not found.</returns>
        Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities matching a specification asynchronously.
        /// </summary>
        /// <param name="spec">The specification containing criteria, includes, and ordering.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of matching entities.</returns>
        Task<IEnumerable<T>> GetAllBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated result set by specification asynchronously.
        /// </summary>
        /// <param name="spec">The specification containing criteria, includes, ordering, and paging.</param>
        /// <param name="request">The pagination request containing page number and page size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing a paginated result set.</returns>
        Task<PagedResult<T>> GetPagedBySpecAsync(ISpecification<T> spec, PagedRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts entities matching a specification asynchronously.
        /// </summary>
        /// <param name="spec">The specification containing criteria.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the count of matching entities.</returns>
        Task<int> CountBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities matching a predicate asynchronously.
        /// </summary>
        /// <param name="predicate">Expression to filter entities.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of matching entities.</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities matching a predicate with includes asynchronously.
        /// </summary>
        /// <param name="predicate">Expression to filter entities.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="includes">Expressions specifying related entities to include.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of matching entities with includes.</returns>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

        /// <summary>
        /// Gets the first entity matching a predicate or null if none found asynchronously.
        /// </summary>
        /// <param name="predicate">Expression to filter entities.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context. Default is false.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the first matching entity or null.</returns>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether any entity matches the predicate asynchronously.
        /// </summary>
        /// <param name="predicate">Expression to filter entities.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing true if any entity matches; otherwise, false.</returns>
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts entities matching the predicate asynchronously.
        /// </summary>
        /// <param name="predicate">Expression to filter entities.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the count of matching entities.</returns>
        Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple entities asynchronously.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        Task<int> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates multiple entities asynchronously.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple entities asynchronously.
        /// </summary>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes entities matching a predicate asynchronously.
        /// </summary>
        /// <param name="predicate">Expression to filter entities to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes an entity by its integer ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="deletedBy">Optional identifier of the user performing the deletion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task<int> SoftDeleteAsync(int id, string? deletedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes an entity by its long ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="deletedBy">Optional identifier of the user performing the deletion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task<int> SoftDeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes an entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to soft delete.</param>
        /// <param name="deletedBy">Optional identifier of the user performing the deletion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        Task<int> SoftDeleteAsync(T entity, string? deletedBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities including soft-deleted ones asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of all entities including soft-deleted.</returns>
        Task<IEnumerable<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted entity by its integer ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task<int> RestoreAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted entity by its long ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        Task<int> RestoreAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by its integer ID with caching support asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cacheExpiration">Optional cache expiration time. Default is implementation-specific.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the found entity or null if not found.</returns>
        Task<T?> GetByIdWithCacheAsync(int id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by its long ID with caching support asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cacheExpiration">Optional cache expiration time. Default is implementation-specific.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing the found entity or null if not found.</returns>
        Task<T?> GetByIdWithCacheAsync(long id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all entities with caching support asynchronously.
        /// </summary>
        /// <param name="cacheExpiration">Optional cache expiration time. Default is implementation-specific.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, containing an enumerable of all entities.</returns>
        Task<IEnumerable<T>> GetAllWithCacheAsync(TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates cache entries matching a pattern asynchronously.
        /// </summary>
        /// <param name="pattern">The pattern to match cache keys. Default is "*" (all).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InvalidateCacheAsync(string pattern = "*", CancellationToken cancellationToken = default);
    }
}
