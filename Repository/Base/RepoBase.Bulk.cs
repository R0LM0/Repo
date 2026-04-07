// -----------------------------------------------------------------------------
// RepoBase.Bulk.cs
// -----------------------------------------------------------------------------
// Partial class: Bulk Operations
// 
// Purpose:
//   Contains bulk operation methods for efficiently handling multiple entities
//   in a single database round-trip.
//
// Methods:
//   - AddRangeAsync - Add multiple entities
//   - UpdateRangeAsync - Update multiple entities
//   - DeleteRangeAsync (2 overloads) - Delete multiple entities by entities or predicate
// -----------------------------------------------------------------------------

using System.Linq.Expressions;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region NEW METHODS - Bulk Operations
        /// <summary>
        /// Adds multiple entities in a single operation.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                await Table.AddRangeAsync(entities, cancellationToken);
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in AddRangeAsync for {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Updates multiple entities in a single operation.
        /// </summary>
        /// <param name="entities">The entities to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                Table.UpdateRange(entities);
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in UpdateRangeAsync for {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Deletes multiple entities in a single operation.
        /// </summary>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                Table.RemoveRange(entities);
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in DeleteRangeAsync for {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Deletes all entities matching the specified predicate.
        /// </summary>
        /// <param name="predicate">The filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await Table.Where(predicate).ToListAsync(cancellationToken);
                Table.RemoveRange(entities);
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in DeleteRangeAsync with predicate for {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion
    }
}
