using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Provides compiled query delegates for high-performance database operations.
    /// 
    /// <para>
    /// Compiled queries are pre-compiled LINQ expressions that skip the expression tree
    /// compilation step on each execution. This provides 20-30% performance improvement
    /// for frequently used queries at the cost of increased memory usage.
    /// </para>
    /// 
    /// <para>
    /// Tradeoffs:
    /// - Performance: 20-30% faster execution for repeated queries
    /// - Memory: Higher memory usage due to cached compiled delegates
    /// - Flexibility: Limited to specific query patterns (no dynamic predicates)
    /// - Startup: Slight increase in startup time for initial compilation
    /// </para>
    /// 
    /// <para>
    /// When to use:
    /// - High-frequency queries (GetById, GetAll in hot paths)
    /// - Stable query patterns that don't change per request
    /// - Scenarios where query compilation time is a bottleneck
    /// </para>
    /// 
    /// <para>
    /// When NOT to use:
    /// - Dynamic queries with varying predicates
    /// - One-off or rarely executed queries
    /// - Memory-constrained environments
    /// </para>
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TContext">The database context type.</typeparam>
    public static class CompiledQueries<T, TContext>
        where T : class
        where TContext : DbContext
    {
        // ReSharper disable StaticMemberInGenericType
        // These are intentionally static per generic type combination
        // to cache compiled queries for each entity/context pair.

        private static readonly Func<TContext, int, CancellationToken, Task<T?>> GetByIdIntCompiled;
        private static readonly Func<TContext, long, CancellationToken, Task<T?>> GetByIdLongCompiled;
        private static readonly Func<TContext, bool, CancellationToken, Task<List<T>>> GetAllCompiled;
        private static readonly Func<TContext, CancellationToken, Task<int>> CountAllCompiled;
        private static readonly Func<TContext, Expression<Func<T, bool>>, CancellationToken, Task<int>> CountWhereCompiled;

        private static readonly object InitializationLock = new();
        private static bool _isInitialized;
        private static string? _idPropertyName;

        /// <summary>
        /// Static constructor initializes compiled query delegates.
        /// </summary>
        static CompiledQueries()
        {
            // Initialize with default implementations that will be replaced
            GetByIdIntCompiled = (context, id, ct) =>
                context.Set<T>().FirstOrDefaultAsync(e => EF.Property<int>(e, GetIdPropertyName()) == id, ct);

            GetByIdLongCompiled = (context, id, ct) =>
                context.Set<T>().FirstOrDefaultAsync(e => EF.Property<long>(e, GetIdPropertyName()) == id, ct);

            GetAllCompiled = (context, asNoTracking, ct) =>
                asNoTracking
                    ? context.Set<T>().AsNoTracking().ToListAsync(ct)
                    : context.Set<T>().ToListAsync(ct);

            CountAllCompiled = (context, ct) =>
                context.Set<T>().CountAsync(ct);

            CountWhereCompiled = (context, predicate, ct) =>
                context.Set<T>().CountAsync(predicate, ct);
        }

        /// <summary>
        /// Initializes the compiled queries. Must be called before using any compiled query methods.
        /// </summary>
        /// <param name="logger">Optional logger for initialization diagnostics.</param>
        public static void Initialize(ILogger? logger = null)
        {
            if (_isInitialized) return;

            lock (InitializationLock)
            {
                if (_isInitialized) return;

                try
                {
                    InitializeCompiledQueries();
                    _isInitialized = true;
                    logger?.LogDebug("Compiled queries initialized for {EntityType} with {ContextType}", typeof(T).Name, typeof(TContext).Name);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to initialize compiled queries for {EntityType}", typeof(T).Name);
                    // Keep the non-compiled fallback implementations
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the compiled queries have been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets an entity by its integer ID using a compiled query.
        /// </summary>
        public static Task<T?> GetByIdAsync(TContext context, int id, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return GetByIdIntCompiled(context, id, cancellationToken);
        }

        /// <summary>
        /// Gets an entity by its long ID using a compiled query.
        /// </summary>
        public static Task<T?> GetByIdAsync(TContext context, long id, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return GetByIdLongCompiled(context, id, cancellationToken);
        }

        /// <summary>
        /// Gets all entities using a compiled query.
        /// </summary>
        public static Task<List<T>> GetAllAsync(TContext context, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return GetAllCompiled(context, asNoTracking, cancellationToken);
        }

        /// <summary>
        /// Counts all entities using a compiled query.
        /// </summary>
        public static Task<int> CountAsync(TContext context, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return CountAllCompiled(context, cancellationToken);
        }

        /// <summary>
        /// Counts entities matching a predicate using a compiled query.
        /// </summary>
        /// <remarks>
        /// Note: This uses EF.CompileAsyncQuery which has limitations with expression parameters.
        /// For complex predicates, the standard CountAsync may be more appropriate.
        /// </remarks>
        public static Task<int> CountAsync(TContext context, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            // Note: EF.CompileAsyncQuery doesn't support expression parameters well,
            // so we fall back to the non-compiled version for parameterized predicates
            return CountWhereCompiled(context, predicate, cancellationToken);
        }

        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private static void InitializeCompiledQueries()
        {
            // Use EF.CompileAsyncQuery for true compiled queries
            // These are compiled once and reused, providing significant performance benefits

            GetByIdIntCompiled = EF.CompileAsyncQuery(
                (TContext context, int id) =>
                    context.Set<T>().FirstOrDefault(e => EF.Property<int>(e, GetIdPropertyName()) == id));

            GetByIdLongCompiled = EF.CompileAsyncQuery(
                (TContext context, long id) =>
                    context.Set<T>().FirstOrDefault(e => EF.Property<long>(e, GetIdPropertyName()) == id));

            GetAllCompiled = EF.CompileAsyncQuery(
                (TContext context, bool asNoTracking) =>
                    asNoTracking
                        ? context.Set<T>().AsNoTracking().ToList()
                        : context.Set<T>().ToList());

            CountAllCompiled = EF.CompileAsyncQuery(
                (TContext context) =>
                    context.Set<T>().Count());

            // For Count with predicate, we can't easily compile it due to expression parameter limitations
            // Keep the fallback implementation
            CountWhereCompiled = (context, predicate, ct) =>
                context.Set<T>().CountAsync(predicate, ct);
        }

        private static string GetIdPropertyName()
        {
            if (_idPropertyName != null) return _idPropertyName;

            // Try to find the key property name from entity configuration
            var entityType = typeof(T);
            var idProp = entityType.GetProperty("Id");
            
            if (idProp != null)
            {
                _idPropertyName = "Id";
                return _idPropertyName;
            }

            // Fallback: look for any property ending with "Id"
            var keyProp = entityType.GetProperties()
                .FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

            _idPropertyName = keyProp?.Name ?? "Id";
            return _idPropertyName;
        }
    }
}
