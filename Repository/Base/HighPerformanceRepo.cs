using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Repo.Repository.Interfaces;
using Repo.Repository.Models;
using Repo.Repository.Specifications;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace Repo.Repository.Base
{
    /// <summary>
    /// Repositorio optimizado para alto rendimiento - miles de transacciones por segundo
    /// </summary>
    public class HighPerformanceRepo<T, TContext> : RepoBase<T, TContext>, IHighPerformanceRepo<T>
        where T : class
        where TContext : DbContext
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly ActionBlock<T> _processingBlock;
        private readonly ConcurrentQueue<T> _batchQueue;
        private readonly Timer _batchTimer;
        private readonly int _maxBatchSize;
        private readonly TimeSpan _batchTimeout;

        public HighPerformanceRepo(TContext context, ILogger logger, ICacheService? cacheService = null,
            int maxConcurrency = 4, int maxBatchSize = 1000, int batchTimeoutMs = 5000)
            : base(context, logger, cacheService)
        {
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _maxBatchSize = maxBatchSize;
            _batchTimeout = TimeSpan.FromMilliseconds(batchTimeoutMs);
            _batchQueue = new ConcurrentQueue<T>();

            // Configurar Dataflow para procesamiento paralelo
            var executionDataflowBlockOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                BoundedCapacity = maxBatchSize * 2
            };

            _processingBlock = new ActionBlock<T>(
                async entity => await ProcessEntityAsync(entity),
                executionDataflowBlockOptions);

            // Timer for automatic batch processing
            _batchTimer = new Timer(ProcessBatchTimer, null, batchTimeoutMs, batchTimeoutMs);
        }

        #region Bulk Operations Optimizadas
        public async Task<int> BulkInsertAsync(IEnumerable<T> entities, int batchSize = 1000)
        {
            var stopwatch = Stopwatch.StartNew();
            var totalInserted = 0;

            try
            {
                // Configurar para alto rendimiento
                ConfigureForHighPerformance();

                // Procesar en lotes para evitar problemas de memoria
                var batches = entities.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    await Db.BulkInsertAsync(batch.ToList(), new BulkConfig
                    {
                        BatchSize = batchSize,
                        UseTempDB = true,
                        SetOutputIdentity = false,
                        PreserveInsertOrder = false,
                        WithHoldlock = false,
                        EnableStreaming = true
                    });

                    totalInserted += batch.Length;

                    // Clear context periodically
                    if (totalInserted % (batchSize * 10) == 0)
                    {
                        await ClearContextAsync();
                    }
                }

                Logger.LogInformation("BulkInsert completado: {Count} registros en {Elapsed}ms",
                    totalInserted, stopwatch.ElapsedMilliseconds);

                return totalInserted;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en BulkInsertAsync: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<int> BulkUpdateAsync(IEnumerable<T> entities, int batchSize = 1000)
        {
            var stopwatch = Stopwatch.StartNew();
            var totalUpdated = 0;

            try
            {
                ConfigureForHighPerformance();

                var batches = entities.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    await Db.BulkUpdateAsync(batch.ToList(), new BulkConfig
                    {
                        BatchSize = batchSize,
                        UseTempDB = true,
                        SetOutputIdentity = false,
                        PreserveInsertOrder = false,
                        WithHoldlock = false,
                        EnableStreaming = true
                    });

                    totalUpdated += batch.Length;

                    if (totalUpdated % (batchSize * 10) == 0)
                    {
                        await ClearContextAsync();
                    }
                }

                Logger.LogInformation("BulkUpdate completado: {Count} registros en {Elapsed}ms",
                    totalUpdated, stopwatch.ElapsedMilliseconds);

                return totalUpdated;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en BulkUpdateAsync: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<int> BulkDeleteAsync(IEnumerable<T> entities, int batchSize = 1000)
        {
            var stopwatch = Stopwatch.StartNew();
            var totalDeleted = 0;

            try
            {
                ConfigureForHighPerformance();

                var batches = entities.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    await Db.BulkDeleteAsync(batch.ToList(), new BulkConfig
                    {
                        BatchSize = batchSize,
                        UseTempDB = true,
                        SetOutputIdentity = false,
                        PreserveInsertOrder = false,
                        WithHoldlock = false,
                        EnableStreaming = true
                    });

                    totalDeleted += batch.Length;

                    if (totalDeleted % (batchSize * 10) == 0)
                    {
                        await ClearContextAsync();
                    }
                }

                Logger.LogInformation("BulkDelete completado: {Count} registros en {Elapsed}ms",
                    totalDeleted, stopwatch.ElapsedMilliseconds);

                return totalDeleted;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en BulkDeleteAsync: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                ConfigureForHighPerformance();

                // Obtener entidades que coinciden con el predicado
                var entitiesToDelete = await Table.Where(predicate).ToListAsync();

                if (!entitiesToDelete.Any())
                    return 0;

                // Usar bulk delete para las entidades encontradas
                await Db.BulkDeleteAsync(entitiesToDelete, new BulkConfig
                {
                    BatchSize = 1000,
                    UseTempDB = true,
                    SetOutputIdentity = false,
                    PreserveInsertOrder = false,
                    WithHoldlock = false,
                    EnableStreaming = true
                });

                return entitiesToDelete.Count;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en BulkDeleteAsync con predicate: {Message}", ex.Message);
                throw;
            }
        }

        public async Task<int> BulkMergeAsync(IEnumerable<T> entities, int batchSize = 1000)
        {
            var stopwatch = Stopwatch.StartNew();
            var totalMerged = 0;

            try
            {
                ConfigureForHighPerformance();

                var batches = entities.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    await Db.BulkInsertOrUpdateAsync(batch.ToList(), new BulkConfig
                    {
                        BatchSize = batchSize,
                        UseTempDB = true,
                        SetOutputIdentity = false,
                        PreserveInsertOrder = false,
                        WithHoldlock = false,
                        EnableStreaming = true
                    });

                    totalMerged += batch.Length;

                    if (totalMerged % (batchSize * 10) == 0)
                    {
                        await ClearContextAsync();
                    }
                }

                Logger.LogInformation("BulkMerge completado: {Count} registros en {Elapsed}ms",
                    totalMerged, stopwatch.ElapsedMilliseconds);

                return totalMerged;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en BulkMergeAsync: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }
        #endregion

        #region Batch Processing
        public async Task ProcessBatchAsync<TResult>(
            Expression<Func<T, bool>> filter,
            Func<IEnumerable<T>, Task<TResult>> processor,
            int batchSize = 1000,
            int maxConcurrency = 4)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task>();

            try
            {
                ConfigureForHighPerformance();

                // Usar streaming para evitar cargar todo en memoria
                await foreach (var batch in StreamBatchesAsync(filter, batchSize))
                {
                    await semaphore.WaitAsync();

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            await processor(batch);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en ProcessBatchAsync: {Message}", ex.Message);
                throw;
            }
        }

        public async Task ProcessParallelAsync<TResult>(
            IEnumerable<T> data,
            Func<T, Task<TResult>> processor,
            int maxConcurrency = 4)
        {
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var tasks = new List<Task<TResult>>();

            try
            {
                foreach (var item in data)
                {
                    await semaphore.WaitAsync();

                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            return await processor(item);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en ProcessParallelAsync: {Message}", ex.Message);
                throw;
            }
        }
        #endregion

        #region Streaming Operations
        public IAsyncEnumerable<T> StreamAsync(Expression<Func<T, bool>>? filter = null, int bufferSize = 1000)
        {
            return StreamAsyncImpl(filter, bufferSize);
        }

        private async IAsyncEnumerable<T> StreamAsyncImpl(Expression<Func<T, bool>>? filter, int bufferSize)
        {
            ConfigureForHighPerformance();

            var query = filter != null ? Table.Where(filter) : Table.AsQueryable();
            query = query.AsNoTracking().AsSplitQuery();

            await foreach (var entity in query.AsAsyncEnumerable())
            {
                yield return entity;
            }
        }

        public async Task<PagedResult<T>> GetPagedOptimizedAsync(PagedRequest request, Expression<Func<T, bool>>? filter = null)
        {
            try
            {
                ConfigureForHighPerformance();

                var query = filter != null ? Table.Where(filter) : Table.AsQueryable();
                query = query.AsNoTracking().AsSplitQuery();

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                return new PagedResult<T>
                {
                    Data = items,
                    TotalCount = totalCount,
                    Page = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error en GetPagedOptimizedAsync: {Message}", ex.Message);
                throw;
            }
        }
        #endregion

        #region Performance Monitoring
        public async Task<PerformanceMetrics> ExecuteWithMetricsAsync(Func<Task> operation)
        {
            var metrics = new PerformanceMetrics
            {
                MemoryUsageBefore = GC.GetTotalMemory(false)
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await operation();
                metrics.ExecutionTime = stopwatch.Elapsed;
            }
            catch (Exception ex)
            {
                metrics.Exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                metrics.MemoryUsageAfter = GC.GetTotalMemory(false);

                Logger.LogInformation("Operación completada en {Elapsed}ms, Memoria: {MemoryBefore} -> {MemoryAfter}",
                    metrics.ExecutionTime.TotalMilliseconds,
                    metrics.MemoryUsageBefore,
                    metrics.MemoryUsageAfter);
            }

            return metrics;
        }

        public async Task<(TResult Result, PerformanceMetrics Metrics)> ExecuteWithMetricsAsync<TResult>(Func<Task<TResult>> operation)
        {
            var metrics = new PerformanceMetrics
            {
                MemoryUsageBefore = GC.GetTotalMemory(false)
            };

            var stopwatch = Stopwatch.StartNew();
            TResult result = default!;

            try
            {
                result = await operation();
                metrics.ExecutionTime = stopwatch.Elapsed;
            }
            catch (Exception ex)
            {
                metrics.Exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                metrics.MemoryUsageAfter = GC.GetTotalMemory(false);

                Logger.LogInformation("Operación completada en {Elapsed}ms, Memoria: {MemoryBefore} -> {MemoryAfter}",
                    metrics.ExecutionTime.TotalMilliseconds,
                    metrics.MemoryUsageBefore,
                    metrics.MemoryUsageAfter);
            }

            return (result, metrics);
        }
        #endregion

        #region Connection Management
        public void ConfigureForHighPerformance(int commandTimeout = 300, bool enableRetryOnFailure = true)
        {
            // Configurar DbContext para alto rendimiento
            Db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            Db.ChangeTracker.AutoDetectChangesEnabled = false;

            // Configure SQL connection
            var connection = Db.Database.GetDbConnection() as SqlConnection;
            if (connection != null)
            {
                // connection.CommandTimeout = commandTimeout; // No se puede asignar, es de solo lectura

                if (enableRetryOnFailure)
                {
                    // You could implement retry policy here
                    // var retryPolicy = SqlServerRetryPolicy.Create();
                    // Implement retry policy as needed
                }
            }
        }

        public async Task ClearContextAsync()
        {
            Db.ChangeTracker.Clear();
            await Task.CompletedTask;
        }
        #endregion

        #region Transaction Management
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            using var transaction = await Db.Database.BeginTransactionAsync(isolationLevel);

            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ExecuteDistributedTransactionAsync(params Func<Task>[] operations)
        {
            // Implement distributed transaction if needed
            // For now, execute sequentially
            foreach (var operation in operations)
            {
                await operation();
            }
        }
        #endregion

        #region Private Methods
        private async Task ProcessEntityAsync(T entity)
        {
            await _semaphore.WaitAsync();

            try
            {
                _batchQueue.Enqueue(entity);

                if (_batchQueue.Count >= _maxBatchSize)
                {
                    await ProcessCurrentBatchAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async void ProcessBatchTimer(object? state)
        {
            if (_batchQueue.Count > 0)
            {
                await ProcessCurrentBatchAsync();
            }
        }

        private async Task ProcessCurrentBatchAsync()
        {
            var batch = new List<T>();

            while (_batchQueue.TryDequeue(out var entity) && batch.Count < _maxBatchSize)
            {
                batch.Add(entity);
            }

            if (batch.Count > 0)
            {
                await BulkInsertAsync(batch, _maxBatchSize);
            }
        }

        private async IAsyncEnumerable<IEnumerable<T>> StreamBatchesAsync(Expression<Func<T, bool>> filter, int batchSize)
        {
            var batch = new List<T>();

            await foreach (var entity in StreamAsync(filter))
            {
                batch.Add(entity);

                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>();
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        private string GetTableName()
        {
            var entityType = Db.Model.FindEntityType(typeof(T));
            return entityType?.GetTableName() ?? typeof(T).Name;
        }
        #endregion

        #region Dispose
        public new void Dispose(bool disposing)
        {
            if (disposing)
            {
                _semaphore?.Dispose();
                _batchTimer?.Dispose();
                _processingBlock?.Complete();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}