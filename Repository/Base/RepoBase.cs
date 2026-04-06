using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Repo.Repository.Extensions;
using Repo.Repository.Interfaces;
using Repo.Repository.Models;
using Repo.Repository.Specifications;
using System.Linq.Expressions;
using System.Threading;

namespace Repo.Repository.Base
{
    public class RepoBase<T, TContext> : IDisposable, IRepo<T>
       where T : class
       where TContext : DbContext
    {
        protected readonly TContext Db;
        protected readonly DbSet<T> Table;
        protected readonly ILogger Logger;  // Campo para el logging
        protected readonly ICacheService? CacheService; // Campo para el caché
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        public RepoBase(TContext context, ILogger logger, ICacheService? cacheService = null)
        {
            Db = context ?? throw new ArgumentNullException(nameof(context));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            CacheService = cacheService;
            Table = Db.Set<T>();
        }

        // Constructor opcional que recibe transacción
        public RepoBase(TContext context, IDbContextTransaction transaction, ILogger logger, ICacheService? cacheService = null)
            : this(context, logger, cacheService)
        {
            _transaction = transaction;
        }

        #region Transacciones
        public void BeginTransaction()
        {
            if (_transaction == null)
                _transaction = Db.Database.BeginTransaction();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction == null)
                _transaction = await Db.Database.BeginTransactionAsync();
        }

        public void CommitTransaction()
        {
            _transaction?.Commit();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public void RollbackTransaction()
        {
            _transaction?.Rollback();
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }
        #endregion

        #region Métodos Sincrónicos
        public T Find(int? id)
        {
            var entity = Table.Find(id);
            if (entity == null)
                throw new Exception("Entidad no encontrada.");
            return entity;
        }

        public T Find(long? id)
        {
            if (!id.HasValue)
                throw new ArgumentNullException(nameof(id));
            var entity = Table.Find((int)id.Value);
            if (entity == null)
                throw new Exception("Entidad no encontrada.");
            return entity;
        }

        public IEnumerable<T> GetAll() => Table.ToList();

        public IEnumerable<T> GetAll(int id)
        {
            return Table.ToList();
        }

        public int Add(T entity, bool persist = true)
        {
            Table.Add(entity);
            return persist ? Save() : 0;
        }

        public int Update(T entity, bool persist = true)
        {
            Table.Update(entity);
            return persist ? Save() : 0;
        }

        public int Delete(T entity, bool persist = true)
        {
            Table.Remove(entity);
            return persist ? Save() : 0;
        }

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

        #region Métodos Asíncronos
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
        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await Table.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Table.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllAsync(int i) para {Entity}", typeof(T).Name);
                throw;
            }
        }


        public async Task<T> GetById(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await Table.FindAsync(new object[] { id }, cancellationToken);
                if (entity == null)
                    throw new Exception("Entidad no encontrada.");
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetById para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<T> GetById(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await Table.FindAsync(new object[] { (int)id }, cancellationToken);
                if (entity == null)
                    throw new Exception("Entidad no encontrada.");
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetById para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

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

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                if (entity == null)
                    throw new Exception("Entidad no encontrada.");
                Table.Remove(entity);
                await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                if (entity == null)
                    throw new Exception("Entidad no encontrada.");
                Table.Remove(entity);
                await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters) where TResult : class
        {
            if (string.IsNullOrWhiteSpace(storedProcedure))
                throw new ArgumentException("El nombre del procedimiento almacenado no puede estar vacío.", nameof(storedProcedure));

            try
            {
                return await Db.Set<TResult>().FromSqlRaw(storedProcedure, parameters).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al ejecutar el procedimiento almacenado {Procedure} para {Entity}", storedProcedure, typeof(T).Name);
                throw;
            }
        }

        public async Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedure, CancellationToken cancellationToken = default, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedure))
                throw new ArgumentException("El nombre del procedimiento almacenado no puede estar vacío.", nameof(storedProcedure));

            try
            {
                // Si el stored procedure incluye la palabra EXEC o @, trátalo como SQL directo
                if (storedProcedure.Contains("EXEC") || storedProcedure.Contains("@"))
                {
                    return await Db.Database.ExecuteSqlRawAsync(storedProcedure, parameters, cancellationToken);
                }
                else
                {
                    // Si es solo el nombre del SP, construye la llamada
                    var paramPlaceholders = string.Join(", ", parameters.OfType<SqlParameter>().Select(p => p.ParameterName));
                    var fullCommand = $"EXEC {storedProcedure} {paramPlaceholders}";
                    return await Db.Database.ExecuteSqlRawAsync(fullCommand, parameters, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al ejecutar el procedimiento almacenado (non query) {Procedure} para {Entity}", storedProcedure, typeof(T).Name);
                throw;
            }
        }
        #endregion

        #region NUEVOS MÉTODOS - Paginación y Filtrado
        public async Task<PagedResult<T>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Table.AsQueryable();

                // Aplica búsqueda si hay SearchTerm
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = ApplySearchFilter(query, request.SearchTerm);
                }

                // Aplica ordenamiento dinámico
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    query = query.OrderByDynamic(request.SortBy, request.IsAscending);
                }
                else
                {
                    // Orden por PK o por la primera propiedad si no se especifica
                    var pk = typeof(T).GetProperties().First().Name;
                    query = query.OrderByDynamic(pk, true);
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>> filter, PagedRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Table.Where(filter);

                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedAsync con filtro para {Entity}", typeof(T).Name);
                throw;
            }
        }

        private IQueryable<T> ApplySearchFilter(IQueryable<T> query, string searchTerm)
        {
            // Implementación simplificada sin System.Linq.Dynamic.Core
            // En una implementación real, necesitarías definir qué propiedades buscar
            // Por ahora, retornamos la query sin filtros
            Logger.LogWarning("Búsqueda dinámica no implementada. Retornando query sin filtros.");
            return query;
        }
        #endregion

        #region NUEVOS MÉTODOS - Especificaciones
        public async Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<PagedResult<T>> GetPagedBySpecAsync(ISpecification<T> spec, PagedRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> CountBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en CountBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion

        #region NUEVOS MÉTODOS - Búsqueda Avanzada
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Table.Where(predicate).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FindAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            try
            {
                var query = Table.Where(predicate);
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                return await query.ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FindAsync con includes para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Table.FirstOrDefaultAsync(predicate, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FirstOrDefaultAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

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

        #region NUEVOS MÉTODOS - Bulk Operations
        public async Task<int> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                await Table.AddRangeAsync(entities, cancellationToken);
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en AddRangeAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                Table.UpdateRange(entities);
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en UpdateRangeAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                Table.RemoveRange(entities);
                return await Db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteRangeAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

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
                Logger.LogError(ex, "Error en DeleteRangeAsync con predicate para {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion

        #region NUEVOS MÉTODOS - Soft Delete
        public async Task<int> SoftDeleteAsync(int id, string? deletedBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                return await SoftDeleteAsync(entity, deletedBy, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en SoftDeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<int> SoftDeleteAsync(long id, string? deletedBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await GetById(id, cancellationToken);
                return await SoftDeleteAsync(entity, deletedBy, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en SoftDeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

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
                    // Si no implementa ISoftDelete, hacer eliminación física
                    Table.Remove(entity);
                    return await Db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en SoftDeleteAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Para entidades que implementan ISoftDelete, incluir las eliminadas
                if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
                {
                    return await Table.ToListAsync(cancellationToken);
                }
                else
                {
                    return await GetAllAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllIncludingDeletedAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

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
                Logger.LogError(ex, "Error en RestoreAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

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
                Logger.LogError(ex, "Error en RestoreAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }
        #endregion

        #region NUEVOS MÉTODOS - Caché
        public async Task<T?> GetByIdWithCacheAsync(int id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
        {
            if (CacheService == null)
                return await GetById(id, cancellationToken);

            var cacheKey = $"{typeof(T).Name}:{id}";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetById(id, cancellationToken), cacheExpiration);
        }

        public async Task<T?> GetByIdWithCacheAsync(long id, TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
        {
            if (CacheService == null)
                return await GetById(id, cancellationToken);

            var cacheKey = $"{typeof(T).Name}:{id}";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetById(id, cancellationToken), cacheExpiration);
        }

        public async Task<IEnumerable<T>> GetAllWithCacheAsync(TimeSpan? cacheExpiration = null, CancellationToken cancellationToken = default)
        {
            if (CacheService == null)
                return await GetAllAsync(cancellationToken);

            var cacheKey = $"{typeof(T).Name}:All";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetAllAsync(cancellationToken), cacheExpiration);
        }

        public async Task InvalidateCacheAsync(string pattern = "*", CancellationToken cancellationToken = default)
        {
            if (CacheService != null)
            {
                var cachePattern = $"{typeof(T).Name}:{pattern}";
                await CacheService.RemoveByPatternAsync(cachePattern);
            }
        }
        #endregion

        #region Dispose
        public virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            if (disposing)
                Db.Dispose();
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
