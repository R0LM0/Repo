using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;

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

        public UnitOfWork(TContext context, ILogger<UnitOfWork<TContext>> logger, ICacheService? cacheService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService;
            _repositories = new Dictionary<Type, object>();
        }

        public IRepo<T> Repository<T>() where T : class
        {
            var type = typeof(T);

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(RepoBase<T, TContext>);
                var repository = Activator.CreateInstance(repositoryType, _context, _logger, _cacheService);
                _repositories[type] = repository!;
            }

            return (IRepo<T>)_repositories[type];
        }

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

        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
            {
                _transaction = await _context.Database.BeginTransactionAsync();
                _logger.LogInformation("Database transaction started");
            }
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("Database transaction committed");
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("Database transaction rolled back");
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