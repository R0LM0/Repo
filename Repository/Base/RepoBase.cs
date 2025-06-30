using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;
using Repo.Repository.Models;
using Repo.Repository.Specifications;
using System.Linq.Expressions;

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
        public async Task<int> SaveAsync()
        {
            try
            {
                return await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en SaveAsync");
                throw;
            }
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                return await Table.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync(int id)
        {
            try
            {
                return await Table.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllAsync(int i) para {Entity}", typeof(T).Name);
                throw;
            }
        }


        public async Task<T> GetById(int id)
        {
            try
            {
                var entity = await Table.FindAsync(id);
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

        public async Task<T> Insert(T entity)
        {
            try
            {
                await Table.AddAsync(entity);
                await Db.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en Insert para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T> UpdateAsync(T entity)
        {
            try
            {
                Db.Entry(entity).State = EntityState.Modified;
                await Db.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en UpdateAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var entity = await GetById(id);
                if (entity == null)
                    throw new Exception("Entidad no encontrada.");
                Table.Remove(entity);
                await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, params object[] parameters) where TResult : class
        {
            if (string.IsNullOrWhiteSpace(storedProcedure))
                throw new ArgumentException("El nombre del procedimiento almacenado no puede estar vacío.", nameof(storedProcedure));

            try
            {
                return await Db.Set<TResult>().FromSqlRaw(storedProcedure, parameters).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error al ejecutar el procedimiento almacenado {Procedure} para {Entity}", storedProcedure, typeof(T).Name);
                throw;
            }
        }

        public async Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedure, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(storedProcedure))
                throw new ArgumentException("El nombre del procedimiento almacenado no puede estar vacío.", nameof(storedProcedure));

            try
            {
                // Si el stored procedure incluye la palabra EXEC o @, trátalo como SQL directo
                if (storedProcedure.Contains("EXEC") || storedProcedure.Contains("@"))
                {
                    return await Db.Database.ExecuteSqlRawAsync(storedProcedure, parameters);
                }
                else
                {
                    // Si es solo el nombre del SP, construye la llamada
                    var paramPlaceholders = string.Join(", ", parameters.OfType<SqlParameter>().Select(p => p.ParameterName));
                    var fullCommand = $"EXEC {storedProcedure} {paramPlaceholders}";
                    return await Db.Database.ExecuteSqlRawAsync(fullCommand, parameters);
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
        public async Task<PagedResult<T>> GetPagedAsync(PagedRequest request)
        {
            try
            {
                var query = Table.AsQueryable();

                // Aplicar búsqueda básica (simplificada sin System.Linq.Dynamic.Core)
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = ApplySearchFilter(query, request.SearchTerm);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>> filter, PagedRequest request)
        {
            try
            {
                var query = Table.Where(filter);

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

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
        public async Task<T?> GetBySpecAsync(ISpecification<T> spec)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllBySpecAsync(ISpecification<T> spec)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<PagedResult<T>> GetPagedBySpecAsync(ISpecification<T> spec, PagedRequest request)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<T>(items, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> CountBySpecAsync(ISpecification<T> spec)
        {
            try
            {
                var query = SpecificationEvaluator<T>.GetQuery(Table.AsQueryable(), spec);
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en CountBySpecAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion

        #region NUEVOS MÉTODOS - Búsqueda Avanzada
        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await Table.Where(predicate).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FindAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            try
            {
                var query = Table.Where(predicate);
                query = includes.Aggregate(query, (current, include) => current.Include(include));
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FindAsync con includes para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await Table.FirstOrDefaultAsync(predicate);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en FirstOrDefaultAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await Table.AnyAsync(predicate);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en AnyAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await Table.CountAsync(predicate);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en CountAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion

        #region NUEVOS MÉTODOS - Bulk Operations
        public async Task<int> AddRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                await Table.AddRangeAsync(entities);
                return await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en AddRangeAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                Table.UpdateRange(entities);
                return await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en UpdateRangeAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> DeleteRangeAsync(IEnumerable<T> entities)
        {
            try
            {
                Table.RemoveRange(entities);
                return await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteRangeAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                var entities = await Table.Where(predicate).ToListAsync();
                Table.RemoveRange(entities);
                return await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en DeleteRangeAsync con predicate para {Entity}", typeof(T).Name);
                throw;
            }
        }
        #endregion

        #region NUEVOS MÉTODOS - Soft Delete
        public async Task<int> SoftDeleteAsync(int id, string? deletedBy = null)
        {
            try
            {
                var entity = await GetById(id);
                return await SoftDeleteAsync(entity, deletedBy);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en SoftDeleteAsync para {Entity} id: {Id}", typeof(T).Name, id);
                throw;
            }
        }

        public async Task<int> SoftDeleteAsync(T entity, string? deletedBy = null)
        {
            try
            {
                if (entity is ISoftDelete softDeleteEntity)
                {
                    softDeleteEntity.IsDeleted = true;
                    softDeleteEntity.DeletedAt = DateTime.UtcNow;
                    softDeleteEntity.DeletedBy = deletedBy;

                    Db.Entry(entity).State = EntityState.Modified;
                    return await Db.SaveChangesAsync();
                }
                else
                {
                    // Si no implementa ISoftDelete, hacer eliminación física
                    Table.Remove(entity);
                    return await Db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en SoftDeleteAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllIncludingDeletedAsync()
        {
            try
            {
                // Para entidades que implementan ISoftDelete, incluir las eliminadas
                if (typeof(ISoftDelete).IsAssignableFrom(typeof(T)))
                {
                    return await Table.ToListAsync();
                }
                else
                {
                    return await GetAllAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetAllIncludingDeletedAsync para {Entity}", typeof(T).Name);
                throw;
            }
        }

        public async Task<int> RestoreAsync(int id)
        {
            try
            {
                var entity = await GetById(id);
                if (entity is ISoftDelete softDeleteEntity)
                {
                    softDeleteEntity.IsDeleted = false;
                    softDeleteEntity.DeletedAt = null;
                    softDeleteEntity.DeletedBy = null;

                    Db.Entry(entity).State = EntityState.Modified;
                    return await Db.SaveChangesAsync();
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
        public async Task<T?> GetByIdWithCacheAsync(int id, TimeSpan? cacheExpiration = null)
        {
            if (CacheService == null)
                return await GetById(id);

            var cacheKey = $"{typeof(T).Name}:{id}";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetById(id), cacheExpiration);
        }

        public async Task<IEnumerable<T>> GetAllWithCacheAsync(TimeSpan? cacheExpiration = null)
        {
            if (CacheService == null)
                return await GetAllAsync();

            var cacheKey = $"{typeof(T).Name}:All";
            return await CacheService.GetOrSetAsync(cacheKey, async () => await GetAllAsync(), cacheExpiration);
        }

        public async Task InvalidateCacheAsync(string pattern = "*")
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
