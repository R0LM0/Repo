using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using System.Data;

namespace Repo.Repository.UnitOfWork
{
    public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
    {
        private readonly TContext _context;
        private readonly ILogger<UnitOfWork<TContext>> _logger;
        private readonly ICacheService? _cacheService;
        private readonly Dictionary<Type, object> _repositories;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        public TContext Context => _context;

        /// <summary>
        /// Gets the current database transaction if one is active.
        /// </summary>
        public IDbContextTransaction? CurrentTransaction => _transaction;

        /// <summary>
        /// Returns true if a transaction is currently active.
        /// </summary>
        public bool HasActiveTransaction => _transaction != null;

        public UnitOfWork(TContext context, ILogger<UnitOfWork<TContext>> logger, ICacheService? cacheService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService;
            _repositories = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Gets a repository for the specified entity type.
        /// Repositories participate in the UnitOfWork's transaction scope automatically.
        /// </summary>
        public IRepo<T> Repository<T>() where T : class
        {
            var type = typeof(T);

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(RepoBase<T, TContext>);
                // Repositories no longer receive transaction directly - they use the shared context
                var repository = Activator.CreateInstance(repositoryType, _context, _logger, _cacheService);
                _repositories[type] = repository!;
            }

            return (IRepo<T>)_repositories[type];
        }

        /// <summary>
        /// Saves all pending changes to the database.
        /// </summary>
        public async Task<int> SaveChangesAsync()
        {
            try
            {
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("Saved {Count} changes to database", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes to database");
                throw;
            }
        }

        /// <summary>
        /// Begins a new database transaction.
        /// This is the PRIMARY method for starting transactions.
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
                _logger.LogInformation("Database transaction started with default isolation level");
            }
            else
            {
                _logger.LogDebug("Transaction already active, reusing existing transaction");
            }
        }

        /// <summary>
        /// Begins a new database transaction with specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction.</param>
        public async Task BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync(isolationLevel);
                _logger.LogInformation("Database transaction started with isolation level: {IsolationLevel}", isolationLevel);
            }
            else
            {
                _logger.LogDebug("Transaction already active (isolation level: {ExistingLevel}), reusing existing transaction", 
                    _transaction.GetDbTransaction().IsolationLevel);
            }
        }

        /// <summary>
        /// Commits the current transaction.
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("Database transaction committed");
            }
            else
            {
                _logger.LogWarning("CommitTransactionAsync called but no active transaction");
            }
        }

        /// <summary>
        /// Rolls back the current transaction.
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("Database transaction rolled back");
            }
            else
            {
                _logger.LogWarning("RollbackTransactionAsync called but no active transaction");
            }
        }

        public async Task<bool> HasChangesAsync()
        {
            return await Task.FromResult(_context.ChangeTracker.HasChanges());
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            try
            {
                var result = await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                _logger.LogInformation("Executed SQL: {Sql} with {ParameterCount} parameters", sql, parameters.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL: {Sql}", sql);
                throw;
            }
        }

        public async Task<IEnumerable<T>> ExecuteSqlRawAsync<T>(string sql, params object[] parameters) where T : class
        {
            try
            {
                var result = await _context.Set<T>().FromSqlRaw(sql, parameters).ToListAsync();
                _logger.LogInformation("Executed SQL query: {Sql} with {ParameterCount} parameters, returned {ResultCount} results",
                    sql, parameters.Length, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query: {Sql}", sql);
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
                _repositories.Clear();
            }
            _disposed = true;
        }
    }
}