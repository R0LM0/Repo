using Repo.Repository.Base;

namespace Repo.Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IRepo<T> Repository<T>() where T : class;
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<bool> HasChangesAsync();
        Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters);
        Task<IEnumerable<T>> ExecuteSqlRawAsync<T>(string sql, params object[] parameters) where T : class;
    }

    public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        TContext Context { get; }
    }
}