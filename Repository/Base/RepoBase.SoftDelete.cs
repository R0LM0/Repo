// -----------------------------------------------------------------------------
// RepoBase.SoftDelete.cs
// -----------------------------------------------------------------------------
// Partial class: Soft Delete Operations
// 
// Purpose:
//   Contains soft delete functionality for entities implementing ISoftDelete.
//   Falls back to hard delete for entities that don't support soft deletion.
//
// Dependencies:
//   - ISoftDelete - Interface marking entities as soft-delete capable
//
// Methods:
//   - SoftDeleteAsync (3 overloads) - Soft delete by ID or entity
//   - GetAllIncludingDeletedAsync - Get all entities including soft-deleted
//   - RestoreAsync (2 overloads) - Restore soft-deleted entities by ID
// -----------------------------------------------------------------------------

using Repo.Repository.Interfaces;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region NEW METHODS - Soft Delete
        /// <summary>
        /// Soft deletes an entity by its integer ID.
        /// If entity doesn't implement ISoftDelete, performs hard delete.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="deletedBy">Optional identifier of who performed the deletion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> SoftDeleteAsync(int id, string? deletedBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                return await SoftDeleteAsync(entity, deletedBy, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in SoftDeleteAsync for {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Soft deletes an entity by its long ID.
        /// If entity doesn't implement ISoftDelete, performs hard delete.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="deletedBy">Optional identifier of who performed the deletion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> SoftDeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                return await SoftDeleteAsync(entity, deletedBy, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in SoftDeleteAsync for {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Soft deletes an entity.
        /// If entity doesn't implement ISoftDelete, performs hard delete.
        /// </summary>
        /// <param name="entity">The entity to soft delete.</param>
        /// <param name="deletedBy">Optional identifier of who performed the deletion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> SoftDeleteAsync(T entity, string? deletedBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entity is ISoftDelete softDeleteEntity)
                {
                    softDeleteEntity.IsDeleted = true;
                    softDeleteEntity.DeletedAt = DateTime.UtcNow;
                    softDeleteEntity.DeletedBy = deletedBy;

                    Db.Entry(entity).State = EntityState.Modified;
                    return await Db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // If it doesn't implement ISoftDelete, do physical deletion
                    Table.Remove(entity);
                    return await Db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in SoftDeleteAsync for {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets all entities including soft-deleted ones.
        /// For entities not implementing ISoftDelete, behaves like GetAllAsync.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of all entities including soft-deleted.</returns>
        public async Task<IEnumerable<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // For entities that implement ISoftDelete, include the deleted ones
                if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
                {
                    return await Table.ToListAsync(cancellationToken);
                }
                else
                {
                    return await GetAllAsync(false, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in GetAllIncludingDeletedAsync for {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Restores a soft-deleted entity by its integer ID.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows (0 if entity doesn't support soft delete).</returns>
        public async Task<int> RestoreAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                if (entity is ISoftDelete softDeleteEntity)
                {
                    softDeleteEntity.IsDeleted = false;
                    softDeleteEntity.DeletedAt = null;
                    softDeleteEntity.DeletedBy = null;

                    Db.Entry(entity).State = EntityState.Modified;
                    return await Db.SaveChangesAsync(cancellationToken);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in RestoreAsync for {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Restores a soft-deleted entity by its long ID.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows (0 if entity doesn't support soft delete).</returns>
        public async Task<int> RestoreAsync(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                if (entity is ISoftDelete softDeleteEntity)
                {
                    softDeleteEntity.IsDeleted = false;
                    softDeleteEntity.DeletedAt = null;
                    softDeleteEntity.DeletedBy = null;

                    Db.Entry(entity).State = EntityState.Modified;
                    return await Db.SaveChangesAsync(cancellationToken);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in RestoreAsync for {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }
        #endregion
    }
}
