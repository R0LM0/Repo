// -----------------------------------------------------------------------------
// RepoBase.Core.cs
// -----------------------------------------------------------------------------
// Partial class: Core CRUD Operations
// 
// Purpose:
//   Contains fundamental CRUD operations for the repository pattern including
//   synchronous and asynchronous methods for entity retrieval, creation, 
//   modification, and persistence.
//
// Methods:
//   - Find (int?, long?) - Retrieve single entity by ID
//   - GetAll variants - Retrieve all entities with optional no-tracking
//   - Add/Update/Delete (sync) - Modify entities with optional persistence
//   - Save/SaveAsync - Persist changes to database
//   - GetById (int, long) - Async entity retrieval by ID
//   - Insert - Async entity creation
//   - UpdateAsync - Async entity modification
//   - DeleteAsync (int, long) - Async entity deletion by ID
// -----------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Repo.Repository.Exceptions;
using System.Threading;

namespace Repo.Repository.Base
{
    public partial class RepoBase<T, TContext>
    {
        #region Métodos Sincrónicos
        /// <summary>
        /// Finds an entity by its integer ID.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <returns>The found entity.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        public T Find(int? id)
        {
            var entity = Table.Find(id);
            if (entity == null)
                throw new EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
            return entity;
        }

        /// <summary>
        /// Finds an entity by its long ID (converted to int).
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <returns>The found entity.</returns>
        /// <exception cref="ArgumentNullException">Thrown when id is null.</exception>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        public T Find(long? id)
        {
            if (!id.HasValue)
                throw new ArgumentNullException(nameof(id));
            var entity = Table.Find((int)id.Value);
            if (entity == null)
                throw new EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
            return entity;
        }

        /// <summary>
        /// Gets all entities with optional no-tracking.
        /// </summary>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <returns>A list of all entities.</returns>
        public IEnumerable<T> GetAll(bool asNoTracking = false) => asNoTracking ? Table.AsNoTracking().ToList() : Table.ToList();

        /// <summary>
        /// Gets all entities with optional no-tracking (overload with unused id parameter for compatibility).
        /// </summary>
        /// <param name="id">Unused parameter for API compatibility.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <returns>A list of all entities.</returns>
        public IEnumerable<T> GetAll(int id, bool asNoTracking = false)
        {
            return asNoTracking ? Table.AsNoTracking().ToList() : Table.ToList();
        }

        /// <summary>
        /// Adds a new entity to the context.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="persist">If true, saves changes immediately.</param>
        /// <returns>The number of affected rows if persisted, otherwise 0.</returns>
        public int Add(T entity, bool persist = true)
        {
            Table.Add(entity);
            return persist ? Save() : 0;
        }

        /// <summary>
        /// Updates an existing entity in the context.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="persist">If true, saves changes immediately.</param>
        /// <returns>The number of affected rows if persisted, otherwise 0.</returns>
        public int Update(T entity, bool persist = true)
        {
            Table.Update(entity);
            return persist ? Save() : 0;
        }

        /// <summary>
        /// Deletes an entity from the context.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="persist">If true, saves changes immediately.</param>
        /// <returns>The number of affected rows if persisted, otherwise 0.</returns>
        public int Delete(T entity, bool persist = true)
        {
            Table.Remove(entity);
            return persist ? Save() : 0;
        }

        /// <summary>
        /// Saves all changes made in the context to the database.
        /// </summary>
        /// <returns>The number of affected rows.</returns>
        public int Save()
        {
            try
            {
                return Db.SaveChanges();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en Save");
                throw;
            }
        }
        #endregion

        #region Métodos Asíncronos - Core CRUD
        /// <summary>
        /// Asynchronously saves all changes made in the context to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of affected rows.</returns>
        public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en SaveAsync");
                throw;
            }
        }

        /// <summary>
        /// Gets all entities asynchronously with optional no-tracking.
        /// </summary>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of all entities.</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync(bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = asNoTracking ? Table.AsNoTracking() : Table;
                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets all entities asynchronously with optional no-tracking (overload with unused id parameter).
        /// </summary>
        /// <param name="id">Unused parameter for API compatibility.</param>
        /// <param name="asNoTracking">If true, entities will not be tracked by the context.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of all entities.</returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync(int id, bool asNoTracking = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = asNoTracking ? Table.AsNoTracking() : Table;
                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllAsync(int i) para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Gets an entity by its integer ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The found entity.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        public async Task<T> GetById(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await Table.FindAsync(new object[] { id }, cancellationToken);
                if (entity == null)
                    throw new EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetById para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Gets an entity by its long ID asynchronously (converted to int).
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The found entity.</returns>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        public async Task<T> GetById(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await Table.FindAsync(new object[] { (int)id }, cancellationToken);
                if (entity == null)
                    throw new EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetById para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Inserts a new entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The inserted entity.</returns>
        public async Task<T> Insert(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await Table.AddAsync(entity, cancellationToken);
                await Db.SaveChangesAsync(cancellationToken);
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en Insert para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Updates an entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated entity.</returns>
        public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            try
            {
                Db.Entry(entity).State = EntityState.Modified;
                await Db.SaveChangesAsync(cancellationToken);
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en UpdateAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// Deletes an entity by its integer ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                if (entity == null)
                    throw new EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
                Table.Remove(entity);
                await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        /// <summary>
        /// Deletes an entity by its long ID asynchronously.
        /// </summary>
        /// <param name="id">The entity ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <exception cref="EntityNotFoundException">Thrown when entity is not found.</exception>
        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                if (entity == null)
                    throw new EntityNotFoundException($"Entidad {typeof(T).Name} no encontrada.");
                Table.Remove(entity);
                await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }
        #endregion
    }
}
