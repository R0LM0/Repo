using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Repo.Repository.Models;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Interfaz para operaciones de alto rendimiento optimizada para miles de transacciones por segundo
    /// </summary>
    /// <typeparam name="T">Tipo de entidad</typeparam>
    public interface IHighPerformanceRepo<T> where T : class
    {
        #region Bulk Operations Optimizadas
        /// <summary>
        /// Inserta miles de registros de forma optimizada usando bulk operations
        /// </summary>
        Task<int> BulkInsertAsync(IEnumerable<T> entities, int batchSize = 1000);

        /// <summary>
        /// Actualiza miles de registros de forma optimizada
        /// </summary>
        Task<int> BulkUpdateAsync(IEnumerable<T> entities, int batchSize = 1000);

        /// <summary>
        /// Elimina miles de registros de forma optimizada
        /// </summary>
        Task<int> BulkDeleteAsync(IEnumerable<T> entities, int batchSize = 1000);

        /// <summary>
        /// Elimina registros por predicado usando bulk operations
        /// </summary>
        Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Operación de merge (insert/update) optimizada
        /// </summary>
        Task<int> BulkMergeAsync(IEnumerable<T> entities, int batchSize = 1000);
        #endregion

        #region Batch Processing
        /// <summary>
        /// Procesa grandes volúmenes de datos en lotes para evitar problemas de memoria
        /// </summary>
        Task ProcessBatchAsync<TResult>(
            Expression<Func<T, bool>> filter,
            Func<IEnumerable<T>, Task<TResult>> processor,
            int batchSize = 1000,
            int maxConcurrency = 4);

        /// <summary>
        /// Procesa datos en paralelo con control de concurrencia
        /// </summary>
        Task ProcessParallelAsync<TResult>(
            IEnumerable<T> data,
            Func<T, Task<TResult>> processor,
            int maxConcurrency = 4);
        #endregion

        #region Streaming Operations
        /// <summary>
        /// Obtiene datos en streaming para evitar cargar todo en memoria
        /// </summary>
        IAsyncEnumerable<T> StreamAsync(Expression<Func<T, bool>>? filter = null, int bufferSize = 1000);

        /// <summary>
        /// Obtiene datos paginados optimizados para grandes volúmenes
        /// </summary>
        Task<PagedResult<T>> GetPagedOptimizedAsync(PagedRequest request, Expression<Func<T, bool>>? filter = null);
        #endregion

        #region Performance Monitoring
        /// <summary>
        /// Ejecuta operación con métricas de rendimiento
        /// </summary>
        Task<PerformanceMetrics> ExecuteWithMetricsAsync(Func<Task> operation);

        /// <summary>
        /// Ejecuta operación con métricas de rendimiento y retorna resultado
        /// </summary>
        Task<(TResult Result, PerformanceMetrics Metrics)> ExecuteWithMetricsAsync<TResult>(Func<Task<TResult>> operation);
        #endregion

        #region Connection Management
        /// <summary>
        /// Configura el contexto para operaciones de alto rendimiento
        /// </summary>
        void ConfigureForHighPerformance(int commandTimeout = 300, bool enableRetryOnFailure = true);

        /// <summary>
        /// Limpia el contexto para liberar memoria
        /// </summary>
        Task ClearContextAsync();
        #endregion

        #region Transaction Management
        /// <summary>
        /// Ejecuta operación en transacción optimizada para grandes volúmenes
        /// </summary>
        Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// Ejecuta múltiples operaciones en transacción distribuida
        /// </summary>
        Task ExecuteDistributedTransactionAsync(params Func<Task>[] operations);
        #endregion
    }

    /// <summary>
    /// Métricas de rendimiento para monitoreo
    /// </summary>
    public class PerformanceMetrics
    {
        public TimeSpan ExecutionTime { get; set; }
        public long MemoryUsageBefore { get; set; }
        public long MemoryUsageAfter { get; set; }
        public int RecordsProcessed { get; set; }
        public double RecordsPerSecond => RecordsProcessed / ExecutionTime.TotalSeconds;
        public Exception? Exception { get; set; }
    }
}