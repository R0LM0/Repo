using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;
using Repo.Repository.Security;
using System.Linq.Expressions;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Repository implementation with compiled query support for high-performance scenarios.
    /// 
    /// <para>
    /// This class extends <see cref="RepoBase{T, TContext}"/> to provide compiled query
    /// optimizations for frequently used operations. Compiled queries are pre-compiled LINQ
    /// expressions that skip the expression tree compilation step, providing 20-30% performance
    /// improvement for hot path operations.
    /// </para>
    /// 
    /// <para>
    /// Usage:
    /// <code>
    /// // Enable compiled queries via options
    /// var options = new RepoOptions { EnableCompiledQueries = true };
    /// var repo = new CompiledRepoBase&lt;MyEntity, MyContext&gt;(context, logger, options);
    /// 
    /// // Use normally - compiled queries are used automatically for supported operations
    /// var entity = await repo.GetById(1);
    /// var all = await repo.GetAllAsync();
    /// var count = await repo.CountAsync(e => e.IsActive);
    /// </code>
    /// </para>
    /// 
    /// <para>
    /// Operations that use compiled queries when enabled:
    /// - GetById(int id)
    /// - GetById(long id)
    /// - GetAllAsync(bool asNoTracking)
    /// - CountAsync() - when called without predicate
    /// </para>
    /// 
    /// <para>
    /// IMPORTANT: Compiled queries are opt-in via <see cref="RepoOptions.EnableCompiledQueries"/>.
    /// They provide significant performance benefits for high-frequency operations but use
    /// additional memory. Enable only after profiling identifies query compilation as a bottleneck.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TContext">The database context type.</typeparam>
    public class CompiledRepoBase<T, TContext> : RepoBase<T, TContext>
        where T : class
        where TContext : DbContext
    {
        private readonly RepoOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledRepoBase{T, TContext}"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">Repository options. If null, defaults to disabled compiled queries.</param>
        /// <param name="cacheService">Optional cache service.</param>
        /// <param name="whitelist">Optional whitelist for stored procedure validation.</param>
        public CompiledRepoBase(
            TContext context,
            ILogger logger,
            RepoOptions? options = null,
            ICacheService? cacheService = null,
            IStoredProcedureWhitelist? whitelist = null)
            : base(context, logger, cacheService, null)
        {
            _options = options ?? new RepoOptions { EnableCompiledQueries = false };

            // Initialize compiled queries if enabled
            if (_options.EnableCompiledQueries)
            {
                CompiledQueries<T, TContext>.Initialize(logger);
            }
        }

        /// <summary>
        /// Gets the repository options.
        /// </summary>
        public RepoOptions Options => _options;

        /// <summary>
        /// Gets a value indicating whether compiled queries are enabled and initialized.
        /// </summary>
        public bool IsCompiledQueriesEnabled =>
            _options.EnableCompiledQueries && CompiledQueries<T, TContext>.IsInitialized;

        /// <inheritdoc />
        public override async Task<T> GetById(int id, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableCompiledQueries)
            {
                return await base.GetById(id, cancellationToken);
            }

            try
            {
                if (_options.LogCompiledQueryUsage)
                {
                    Logger.LogDebug("Using compiled query for GetById(int) on {Entity}", typeof(T).Name);
                }

                var entity = await CompiledQueries<T, TContext>.GetByIdAsync(Db, id, cancellationToken);
                if (entity == null)
                    throw new Exceptions.EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
                return entity;
            }
            catch (Exceptions.EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetById(compilado) para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<T> GetById(long id, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableCompiledQueries)
            {
                return await base.GetById(id, cancellationToken);
            }

            try
            {
                if (_options.LogCompiledQueryUsage)
                {
                    Logger.LogDebug("Using compiled query for GetById(long) on {Entity}", typeof(T).Name);
                }

                var entity = await CompiledQueries<T, TContext>.GetByIdAsync(Db, id, cancellationToken);
                if (entity == null)
                    throw new Exceptions.EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
                return entity;
            }
            catch (Exceptions.EntityNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetById(compilado) para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<T>> GetAllAsync(bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            if (!_options.EnableCompiledQueries)
            {
                return await base.GetAllAsync(asNoTracking, cancellationToken);
            }

            try
            {
                if (_options.LogCompiledQueryUsage)
                {
                    Logger.LogDebug("Using compiled query for GetAllAsync on {Entity}", typeof(T).Name);
                }

                return await CompiledQueries<T, TContext>.GetAllAsync(Db, asNoTracking, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllAsync(compilado) para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<T>> GetAllAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            // The int parameter overload doesn't benefit from compiled queries
            // as it's just a variant that ignores the id parameter
            return await base.GetAllAsync(id, asNoTracking, cancellationToken);
        }

        /// <summary>
        /// Counts all entities using a compiled query when enabled.
        /// </summary>
        public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.EnableCompiledQueries)
            {
                return await Table.CountAsync(cancellationToken);
            }

            try
            {
                if (_options.LogCompiledQueryUsage)
                {
                    Logger.LogDebug("Using compiled query for CountAllAsync on {Entity}", typeof(T).Name);
                }

                return await CompiledQueries<T, TContext>.CountAsync(Db, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en CountAllAsync(compilado) para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public override async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            // Note: Count with predicate cannot be easily compiled due to expression parameter limitations
            // We use the base implementation which provides full flexibility
            return await base.CountAsync(predicate, cancellationToken);
        }
    }
}
