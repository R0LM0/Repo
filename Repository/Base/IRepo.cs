using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Repo.Repository.Models;
using Repo.Repository.Specifications;

namespace Repo.Repository.Base
{
    public interface IRepo<T> where T : class
    {
        T Find(int? id);   //Metodo para buscar por ID
        T Find(long? id);  // Metodo para buscar por ID (BigInt/long)
        IEnumerable<T> GetAll(); //Metodo Obtener todo
        IEnumerable<T> GetAll(int id); //Metodo Obtener todo
        int Add(T entity, bool persist = true);// Bool persist es un parametro opcional 
        int Update(T entity, bool persist = true);  //Metodo para Actualizar
        int Delete(T entity, bool persist = true);  //Metodo para Borrar
        int Save();//Metodo para Salvar

        // Métodos asíncronos
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(int id); // Metodo para Obtener ID async 
        Task<T> GetById(int id);
        Task<T> GetById(long id); // Metodo para obtener por ID (BigInt/long) async
        Task<T> Insert(T entity);
        Task<T> UpdateAsync(T entity);
        Task DeleteAsync(int id);
        Task DeleteAsync(long id); // Metodo para eliminar por ID (BigInt/long) async
        Task<int> SaveAsync();

        // Métodos para procedimientos almacenados:
        Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, params object[] parameters) where TResult : class;
        Task<int> ExecuteStoredProcedureNonQueryAsync(string storedProcedure, params object[] parameters);

        // NUEVOS MÉTODOS - Paginación y Filtrado
        Task<PagedResult<T>> GetPagedAsync(PagedRequest request);
        Task<PagedResult<T>> GetPagedAsync(Expression<Func<T, bool>> filter, PagedRequest request);

        // NUEVOS MÉTODOS - Especificaciones
        Task<T?> GetBySpecAsync(ISpecification<T> spec);
        Task<IEnumerable<T>> GetAllBySpecAsync(ISpecification<T> spec);
        Task<PagedResult<T>> GetPagedBySpecAsync(ISpecification<T> spec, PagedRequest request);
        Task<int> CountBySpecAsync(ISpecification<T> spec);

        // NUEVOS MÉTODOS - Búsqueda Avanzada
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        // NUEVOS MÉTODOS - Bulk Operations
        Task<int> AddRangeAsync(IEnumerable<T> entities);
        Task<int> UpdateRangeAsync(IEnumerable<T> entities);
        Task<int> DeleteRangeAsync(IEnumerable<T> entities);
        Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate);

        // NUEVOS MÉTODOS - Soft Delete
        Task<int> SoftDeleteAsync(int id, string? deletedBy = null);
        Task<int> SoftDeleteAsync(long id, string? deletedBy = null); // Soft delete por ID (BigInt/long)
        Task<int> SoftDeleteAsync(T entity, string? deletedBy = null);
        Task<IEnumerable<T>> GetAllIncludingDeletedAsync();
        Task<int> RestoreAsync(int id);
        Task<int> RestoreAsync(long id); // Restaurar por ID (BigInt/long)

        // NUEVOS MÉTODOS - Caché
        Task<T?> GetByIdWithCacheAsync(int id, TimeSpan? cacheExpiration = null);
        Task<T?> GetByIdWithCacheAsync(long id, TimeSpan? cacheExpiration = null); // GetById con caché por ID (BigInt/long)
        Task<IEnumerable<T>> GetAllWithCacheAsync(TimeSpan? cacheExpiration = null);
        Task InvalidateCacheAsync(string pattern = "*");
    }
}