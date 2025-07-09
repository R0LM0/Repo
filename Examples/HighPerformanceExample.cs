using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Repo.Repository.Base;
using Repo.Repository.Services;
using System.Diagnostics;

namespace Examples
{
    /// <summary>
    /// Ejemplo de uso del repositorio de alto rendimiento para miles de transacciones por segundo
    /// </summary>
    public class HighPerformanceExample
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HighPerformanceExample> _logger;

        public HighPerformanceExample(IServiceProvider serviceProvider, ILogger<HighPerformanceExample> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Ejemplo de inserción masiva de registros de medidores de agua
        /// </summary>
        public async Task BulkInsertWaterMetersExample()
        {
            _logger.LogInformation("Iniciando inserción masiva de medidores de agua...");

            // Crear repositorio de alto rendimiento
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WaterMeter>>();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();

            var highPerfRepo = new HighPerformanceRepo<WaterMeter, YourDbContext>(
                context, logger, cacheService,
                maxConcurrency: 8,
                maxBatchSize: 2000,
                batchTimeoutMs: 3000);

            // Generar datos de prueba (simular millones de registros)
            var waterMeters = GenerateWaterMeters(100000); // 100k registros

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Configurar para alto rendimiento
                highPerfRepo.ConfigureForHighPerformance();

                // Ejecutar con métricas de rendimiento
                var (insertedCount, metrics) = await highPerfRepo.ExecuteWithMetricsAsync(async () =>
                {
                    return await highPerfRepo.BulkInsertAsync(waterMeters, batchSize: 2000);
                });

                _logger.LogInformation("Inserción completada: {Count} registros en {Elapsed}ms",
                    insertedCount, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("Rendimiento: {RecordsPerSecond:F2} registros/segundo",
                    metrics.RecordsPerSecond);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en inserción masiva: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Ejemplo de actualización masiva de lecturas de medidores
        /// </summary>
        public async Task BulkUpdateReadingsExample()
        {
            _logger.LogInformation("Iniciando actualización masiva de lecturas...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WaterMeterReading>>();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();

            var highPerfRepo = new HighPerformanceRepo<WaterMeterReading, YourDbContext>(
                context, logger, cacheService,
                maxConcurrency: 6,
                maxBatchSize: 1500);

            // Simular lecturas actualizadas
            var readings = await GenerateUpdatedReadings(50000);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var (updatedCount, metrics) = await highPerfRepo.ExecuteWithMetricsAsync(async () =>
                {
                    return await highPerfRepo.BulkUpdateAsync(readings, batchSize: 1500);
                });

                _logger.LogInformation("Actualización completada: {Count} registros en {Elapsed}ms",
                    updatedCount, stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("Rendimiento: {RecordsPerSecond:F2} registros/segundo",
                    metrics.RecordsPerSecond);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en actualización masiva: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Ejemplo de procesamiento en lotes para análisis de datos
        /// </summary>
        public async Task BatchProcessingExample()
        {
            _logger.LogInformation("Iniciando procesamiento en lotes...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WaterMeterReading>>();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();

            var highPerfRepo = new HighPerformanceRepo<WaterMeterReading, YourDbContext>(
                context, logger, cacheService,
                maxConcurrency: 4,
                maxBatchSize: 1000);

            var stopwatch = Stopwatch.StartNew();
            var totalProcessed = 0;

            try
            {
                // Procesar lecturas del último mes en lotes
                await highPerfRepo.ProcessBatchAsync(
                    filter: r => r.ReadingDate >= DateTime.Now.AddMonths(-1),
                    processor: async (batch) =>
                    {
                        var batchCount = batch.Count();
                        totalProcessed += batchCount;

                        // Simular procesamiento de análisis
                        await AnalyzeReadings(batch);

                        _logger.LogDebug("Procesado lote de {Count} registros. Total: {Total}",
                            batchCount, totalProcessed);

                        return batchCount;
                    },
                    batchSize: 1000,
                    maxConcurrency: 4);

                _logger.LogInformation("Procesamiento completado: {Total} registros en {Elapsed}ms",
                    totalProcessed, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento por lotes: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Ejemplo de streaming para reportes grandes
        /// </summary>
        public async Task StreamingReportExample()
        {
            _logger.LogInformation("Generando reporte con streaming...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WaterMeterReading>>();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();

            var highPerfRepo = new HighPerformanceRepo<WaterMeterReading, YourDbContext>(
                context, logger, cacheService);

            var stopwatch = Stopwatch.StartNew();
            var recordCount = 0;
            var totalConsumption = 0.0m;

            try
            {
                // Usar streaming para evitar cargar todo en memoria
                await foreach (var reading in highPerfRepo.StreamAsync(
                    filter: r => r.ReadingDate >= DateTime.Now.AddDays(-30),
                    bufferSize: 500))
                {
                    recordCount++;
                    totalConsumption += reading.Consumption;

                    // Procesar cada registro individualmente
                    await ProcessReadingForReport(reading);

                    if (recordCount % 10000 == 0)
                    {
                        _logger.LogInformation("Procesados {Count} registros...", recordCount);
                    }
                }

                _logger.LogInformation("Reporte completado: {Count} registros, Consumo total: {Consumption} en {Elapsed}ms",
                    recordCount, totalConsumption, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en generación de reporte: {Message}", ex.Message);
                throw;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Ejemplo de transacción distribuida para operaciones complejas
        /// </summary>
        public async Task DistributedTransactionExample()
        {
            _logger.LogInformation("Ejecutando transacción distribuida...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<YourDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<WaterMeter>>();
            var cacheService = scope.ServiceProvider.GetService<ICacheService>();

            var highPerfRepo = new HighPerformanceRepo<WaterMeter, YourDbContext>(
                context, logger, cacheService);

            try
            {
                var result = await highPerfRepo.ExecuteInTransactionAsync(async () =>
                {
                    // Operación 1: Actualizar medidores
                    var meters = await GenerateWaterMeters(1000);
                    var updatedCount = await highPerfRepo.BulkUpdateAsync(meters);

                    // Operación 2: Insertar lecturas
                    var readings = await GenerateReadings(1000);
                    var insertedCount = await highPerfRepo.BulkInsertAsync(readings);

                    // Operación 3: Actualizar estadísticas
                    await UpdateStatistics();

                    return new { Updated = updatedCount, Inserted = insertedCount };
                }, IsolationLevel.ReadCommitted);

                _logger.LogInformation("Transacción completada: {Updated} actualizados, {Inserted} insertados",
                    result.Updated, result.Inserted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en transacción distribuida: {Message}", ex.Message);
                throw;
            }
        }

        #region Helper Methods
        private List<WaterMeter> GenerateWaterMeters(int count)
        {
            var meters = new List<WaterMeter>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                meters.Add(new WaterMeter
                {
                    Id = i + 1,
                    SerialNumber = $"WM{DateTime.Now:yyyyMMdd}{i:D6}",
                    CustomerId = random.Next(1, 10000),
                    InstallationDate = DateTime.Now.AddDays(-random.Next(1, 365)),
                    Status = "Active",
                    LastReading = random.Next(0, 10000),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            return meters;
        }

        private async Task<List<WaterMeterReading>> GenerateUpdatedReadings(int count)
        {
            var readings = new List<WaterMeterReading>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                readings.Add(new WaterMeterReading
                {
                    Id = i + 1,
                    WaterMeterId = random.Next(1, 10000),
                    ReadingDate = DateTime.Now.AddDays(-random.Next(1, 30)),
                    CurrentReading = random.Next(1000, 50000),
                    PreviousReading = random.Next(0, 40000),
                    Consumption = random.Next(100, 5000),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            return readings;
        }

        private async Task<List<WaterMeterReading>> GenerateReadings(int count)
        {
            var readings = new List<WaterMeterReading>();
            var random = new Random();

            for (int i = 0; i < count; i++)
            {
                readings.Add(new WaterMeterReading
                {
                    WaterMeterId = random.Next(1, 10000),
                    ReadingDate = DateTime.Now,
                    CurrentReading = random.Next(1000, 50000),
                    PreviousReading = random.Next(0, 40000),
                    Consumption = random.Next(100, 5000),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            return readings;
        }

        private async Task AnalyzeReadings(IEnumerable<WaterMeterReading> readings)
        {
            // Simular análisis de datos
            await Task.Delay(10); // Simular procesamiento
        }

        private async Task ProcessReadingForReport(WaterMeterReading reading)
        {
            // Simular procesamiento para reporte
            await Task.Delay(1); // Simular procesamiento
        }

        private async Task UpdateStatistics()
        {
            // Simular actualización de estadísticas
            await Task.Delay(100);
        }
        #endregion
    }

    #region Model Classes (Ejemplo)
    public class WaterMeter
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public DateTime InstallationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int LastReading { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class WaterMeterReading
    {
        public int Id { get; set; }
        public int WaterMeterId { get; set; }
        public DateTime ReadingDate { get; set; }
        public int CurrentReading { get; set; }
        public int PreviousReading { get; set; }
        public decimal Consumption { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class YourDbContext : DbContext
    {
        public DbSet<WaterMeter> WaterMeters { get; set; }
        public DbSet<WaterMeterReading> WaterMeterReadings { get; set; }

        public YourDbContext(DbContextOptions<YourDbContext> options) : base(options)
        {
        }
    }
    #endregion
}