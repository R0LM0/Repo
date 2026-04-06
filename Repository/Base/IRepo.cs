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
    /// IMPORTANT: Transaction Management Consolidation
    /// - Transaction methods (BeginTransaction, CommitTransaction, RollbackTransaction) are OBSOLETE
    /// - Use UnitOfWork for all transaction orchestration instead
    /// - Repositories obtained via IUnitOfWork.Repository&lt;T&gt;() automatically participate in UnitOfWork transactions
    /// </summary>
    public interface IRepo<T> where T : class
    {
        T Find(int? id);   //Metodo para buscar por ID
        T Find(long? id);  // Metodo para buscar por ID (BigInt/long)
        IEnumerable<T> GetAll(); //Metodo Obtener todo
        IEnumerable<T> GetAll(int id); //Metodo Obtener todo
        int Add(T entity, bool persist = true);// Bool persist es un parametro opcional 
        int Update(T entity, bool persist = true);  //Metodo para Actualizar
        int Delete(T entity, bool persist = true);  //Metodo para Borrar
        int Save();//Metodo para Salvar

        // Métodos asíncronos
        Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllAsync(int id, CancellationToken cancellationToken = default);
        Task<T> GetById(int id, CancellationToken cancellationToken = default);
        Task<T> GetById(long id, CancellationToken cancellationToken = default);
        Task<T> Insert(T entity, CancellationToken cancellationToken = default);
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task DeleteAsync(long id, CancellationToken cancellationToken = default);
        Task<int> SaveAsync(CancellationToken cancellationToken = default);

        // Métodos para procedimientos almacenados:
        Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters) where TResult : class;
        Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters);

        // NUEVOS MÉTODOS - Funciones de Base de Datos
        /// <summary>
        /// Executes a scalar database function and returns the result.
        /// </summary>
        /// <typeparam name="TResult">The return type of the scalar function.</typeparam>
        /// <param name="functionName">Name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>The scalar result.</returns>
        Task<TResult> ExecuteScalarFunctionAsync<TResult>(string functionName, params object[] parameters);

        /// <summary>
        /// Executes a table-valued database function and returns the results.
        /// </summary>
        /// <typeparam name="TResult">The entity type returned by the function.</typeparam>
        /// <param name="functionName">Name of the database function.</param>
        /// <param name="parameters">Parameters to pass to the function.</param>
        /// <returns>Enumerable of results from the table-valued function.</returns>
        Task<IEnumerable<TResult>> ExecuteTableValuedFunctionAsync<TResult>(string functionName, params object[] parameters) where TResult : class;

        // NUEVOS MÉTODOS - Paginación y Filtrado
        Task<PagedResult<T>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
        Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>> filter, PagedRequest request, CancellationToken cancellationToken = default);

        // NUEVOS MÉTODOS - Especificaciones
        Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
        Task<PagedResult<T>> GetPagedBySpecAsync(ISpecification<T> spec, PagedRequest request, CancellationToken cancellationToken = default);
        Task<int> CountBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);

        // NUEVOS MÉTODOS - Búsqueda Avanzada
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        // NUEVOS MÉTODOS - Bulk Operations
        Task<int> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        // NUEVOS MÉTODOS - Soft Delete
        Task<int> SoftDeleteAsync(int id, string? deletedBy = null, CancellationToken cancellationToken = default);
        Task<int> SoftDeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default);
        Task<int> SoftDeleteAsync(T entity, string? deletedBy = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
        Task<int> RestoreAsync(int id, CancellationToken cancellationToken = default);
        Task<int> RestoreAsync(long id, CancellationToken cancellationToken = default);

        // NUEVOS MÉTODOS - Caché
        Task<T?> GetByIdWithCacheAsync(int id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default);
        Task<T?> GetByIdWithCacheAsync(long id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllWithCacheAsync(TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default);
        Task InvalidateCacheAsync(string pattern = "*", CancellationToken cancellationToken = default);

        #region DEPRECATED - Transaction Management
        /// <summary>
        /// OBSOLETE: Use UnitOfWork.BeginTransactionAsync() instead.
        /// Repositories should not manage their own transactions.
        /// This method will be removed in a future version.
        /// </summary>
        [Obsolete("Use IUnitOfWork.BeginTransactionAsync() instead. Repository-level transaction methods are deprecated.", false)]
        void BeginTransaction();

        /// <summary>
        /// OBSOLETE: Use UnitOfWork.BeginTransactionAsync() instead.
        /// Repositories should not manage their own transactions.
        /// This method will be removed in a future version.
        /// </summary>
        [Obsolete("Use IUnitOfWork.BeginTransactionAsync() instead. Repository-level transaction methods are deprecated.", false)]
        Task BeginTransactionAsync();

        /// <summary>
        /// OBSOLETE: Use UnitOfWork.CommitTransactionAsync() instead.
        /// Repositories should not manage their own transactions.
        /// This method will be removed in a future version.
        /// </summary>
        [Obsolete("Use IUnitOfWork.CommitTransactionAsync() instead. Repository-level transaction methods are deprecated.", false)]
        void CommitTransaction();

        /// <summary>
        /// OBSOLETE: Use UnitOfWork.CommitTransactionAsync() instead.
        /// Repositories should not manage their own transactions.
        /// This method will be removed in a future version.
        /// </summary>
        [Obsolete("Use IUnitOfWork.CommitTransactionAsync() instead. Repository-level transaction methods are deprecated.", false)]
        Task CommitTransactionAsync();

        /// <summary>
        /// OBSOLETE: Use UnitOfWork.RollbackTransactionAsync() instead.
        /// Repositories should not manage their own transactions.
        /// This method will be removed in a future version.
        /// </summary>
        [Obsolete("Use IUnitOfWork.RollbackTransactionAsync() instead. Repository-level transaction methods are deprecated.", false)]
        void RollbackTransaction();

        /// <summary>
        /// OBSOLETE: Use UnitOfWork.RollbackTransactionAsync() instead.
        /// Repositories should not manage their own transactions.
        /// This method will be removed in a future version.
        /// </summary>
        [Obsolete("Use IUnitOfWork.RollbackTransactionAsync() instead. Repository-level transaction methods are deprecated.", false)]
        Task RollbackTransactionAsync();
        #endregion
    }
}
