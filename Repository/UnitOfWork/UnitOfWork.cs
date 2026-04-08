using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;
using Repo.Repository.Interfaces;
using System.Data;
using System.Text.RegularExpressions;

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

            // Security: Validate that T is a registered entity type
            var entityType = _context.Model.FindEntityType(type);
            if (entityType == null)
                throw new InvalidOperationException($"Type '{type.Name}' is not a registered entity in the DbContext.");

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
                var result = await _context.SaveChangesAsync().ConfigureAwait(false);
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
                _transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
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
                _transaction = await _context.Database.BeginTransactionAsync(isolationLevel).ConfigureAwait(false);
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
                await _transaction.CommitAsync().ConfigureAwait(false);
                await _transaction.DisposeAsync().ConfigureAwait(false);
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
                await _transaction.RollbackAsync().ConfigureAwait(false);
                await _transaction.DisposeAsync().ConfigureAwait(false);
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

        // Allowed SQL operations whitelist - only SELECT statements for safety
        private static readonly Regex _allowedSqlPattern = new Regex(
            @"^\s*SELECT\s+.+\s+FROM\s+\w+|^\s*INSERT\s+INTO\s+\w+|^\s*UPDATE\s+\w+\s+SET|^\s*DELETE\s+FROM\s+\w+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL cannot be empty", nameof(sql));

            // Security: Validate SQL is in whitelist
            if (!IsSafeSql(sql))
                throw new SecurityException("SQL operation not allowed. Only SELECT, INSERT, UPDATE, DELETE are permitted.");

            try
            {
                var result = await _context.Database.ExecuteSqlRawAsync(sql, parameters).ConfigureAwait(false);
                // Security: Don't log full SQL in production - only parameter count
                _logger.LogInformation("Executed SQL command with {ParameterCount} parameters, affected {RowCount} rows", 
                    parameters.Length, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL command with {ParameterCount} parameters", parameters.Length);
                throw;
            }
        }

        private static bool IsSafeSql(string sql)
        {
            // Block dangerous operations
            var dangerousKeywords = new[] { "DROP", "TRUNCATE", "ALTER", "CREATE", "EXEC", "EXECUTE", "sp_", "xp_" };
            var upperSql = sql.ToUpperInvariant();
            
            foreach (var keyword in dangerousKeywords)
            {
                if (upperSql.Contains(keyword))
                    return false;
            }
            
            return _allowedSqlPattern.IsMatch(sql);
        }

        public async Task<IEnumerable<T>> ExecuteSqlRawAsync<T>(string sql, params object[] parameters) where T : class
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentException("SQL cannot be empty", nameof(sql));

            // Security: Validate SQL is safe
            if (!IsSafeSql(sql))
                throw new SecurityException("SQL operation not allowed.");

            try
            {
                var result = await _context.Set<T>().FromSqlRaw(sql, parameters).ToListAsync().ConfigureAwait(false);
                // Security: Don't log full SQL
                _logger.LogInformation("Executed SQL query with {ParameterCount} parameters, returned {ResultCount} results",
                    parameters.Length, result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL query with {ParameterCount} parameters", parameters.Length);
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