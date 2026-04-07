// -----------------------------------------------------------------------------
// RepoBase.Queries.cs
// -----------------------------------------------------------------------------
// Partial class: Advanced Query Operations
// 
// Purpose:
//   Contains advanced query methods for filtering, searching, and counting
//   entities using expressions and predicate-based queries.
//
// Methods:
//   - FindAsync (2 overloads) - Find entities matching a predicate with optional includes
//   - FirstOrDefaultAsync - Get first entity matching a predicate
//   - AnyAsync - Check if any entity matches a predicate
//   - CountAsync - Count entities matching a predicate
// -----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region NUEVOS MÉTODOS - Búsqueda Avanzada
        /// <summary>
        /// Finds all entities matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching entities.</returns>
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = asNoTracking ? Table.AsNoTracking().Where(predicate) : Table.Where(predicate);
                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FindAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Finds all entities matching the specified predicate with eager loading of related entities.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="includes">Navigation properties to include.</param>
        /// <returns>A list of matching entities with included relations.</returns>
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            try
            {
                var query = Table.Where(predicate);
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                if (asNoTracking) query = query.AsNoTracking();
                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FindAsync con includes para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets the first entity matching the specified predicate, or null if not found.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The first matching entity, or null.</returns>
        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = asNoTracking ? Table.AsNoTracking() : Table;
                return await query.FirstOrDefaultAsync(predicate, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FirstOrDefaultAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Checks if any entity matches the specified predicate.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if any entity matches; otherwise, false.</returns>
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Table.AnyAsync(predicate, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en AnyAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Counts entities matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of matching entities.</returns>
        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Table.CountAsync(predicate, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en CountAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion
    }
}
