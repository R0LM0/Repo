using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repo.Repository.Services;
using System.Diagnostics;

namespace Examples
{
    /// <summary>
    /// Controlador API optimizado para manejar múltiples entidades bancarias
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BankingController : ControllerBase
    {
        private readonly BankingConcurrencyService _concurrencyService;
        private readonly YourDbContext _context;
        private readonly ILogger<BankingController> _logger;
        private readonly HighPerformanceRepo<Customer, YourDbContext> _highPerfRepo;

        public BankingController(
            BankingConcurrencyService concurrencyService,
            YourDbContext context,
            ILogger<BankingController> logger,
            ILogger<Customer> customerLogger,
            ICacheService? cacheService = null)
        {
            _concurrencyService = concurrencyService;
            _context = context;
            _logger = logger;
            _highPerfRepo = new HighPerformanceRepo<Customer, YourDbContext>(context, customerLogger, cacheService);
        }

        /// <summary>
        /// Consulta estado de cuenta optimizada para múltiples entidades
        /// </summary>
        [HttpGet("account-status/{entityId}/{customerId}")]
        public async Task<ActionResult<AccountStatusResult>> GetAccountStatus(
            string entityId,
            int customerId,
            [FromQuery] int timeoutSeconds = 30)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Consulta de estado de cuenta iniciada - Entidad: {EntityId}, Cliente: {CustomerId}",
                    entityId, customerId);

                var result = await _concurrencyService.GetAccountStatusAsync(
                    _context,
                    entityId,
                    customerId,
                    TimeSpan.FromSeconds(timeoutSeconds));

                _logger.LogInformation("Consulta completada en {Elapsed}ms - Entidad: {EntityId}, Cliente: {CustomerId}",
                    stopwatch.ElapsedMilliseconds, entityId, customerId);

                return Ok(result);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timeout en consulta - Entidad: {EntityId}, Cliente: {CustomerId}",
                    entityId, customerId);
                return StatusCode(408, new { Error = "Timeout en la consulta", EntityId = entityId, CustomerId = customerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en consulta - Entidad: {EntityId}, Cliente: {CustomerId}",
                    entityId, customerId);
                return StatusCode(500, new { Error = "Error interno del servidor" });
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Procesa pago individual optimizado
        /// </summary>
        [HttpPost("payment/{entityId}")]
        public async Task<ActionResult<PaymentResult>> ProcessPayment(
            string entityId,
            [FromBody] PaymentRequest paymentRequest,
            [FromQuery] int timeoutSeconds = 30)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Procesamiento de pago iniciado - Entidad: {EntityId}, Cliente: {CustomerId}, Monto: {Amount}",
                    entityId, paymentRequest.CustomerId, paymentRequest.Amount);

                var result = await _concurrencyService.ProcessPaymentAsync(
                    _context,
                    entityId,
                    paymentRequest,
                    TimeSpan.FromSeconds(timeoutSeconds));

                _logger.LogInformation("Pago procesado en {Elapsed}ms - Entidad: {EntityId}, Transacción: {TransactionId}",
                    stopwatch.ElapsedMilliseconds, entityId, result.TransactionId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Error de negocio en pago - Entidad: {EntityId}, Cliente: {CustomerId}: {Message}",
                    entityId, paymentRequest.CustomerId, ex.Message);
                return BadRequest(new { Error = ex.Message, EntityId = entityId, CustomerId = paymentRequest.CustomerId });
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timeout en pago - Entidad: {EntityId}, Cliente: {CustomerId}",
                    entityId, paymentRequest.CustomerId);
                return StatusCode(408, new { Error = "Timeout en el procesamiento", EntityId = entityId, CustomerId = paymentRequest.CustomerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en pago - Entidad: {EntityId}, Cliente: {CustomerId}",
                    entityId, paymentRequest.CustomerId);
                return StatusCode(500, new { Error = "Error interno del servidor" });
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Procesa pagos masivos optimizados
        /// </summary>
        [HttpPost("bulk-payments/{entityId}")]
        public async Task<ActionResult<BulkPaymentResult>> ProcessBulkPayments(
            string entityId,
            [FromBody] List<PaymentRequest> payments,
            [FromQuery] int batchSize = 100,
            [FromQuery] int timeoutSeconds = 60)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Procesamiento masivo iniciado - Entidad: {EntityId}, Pagos: {Count}, Lote: {BatchSize}",
                    entityId, payments.Count, batchSize);

                var result = await _concurrencyService.ProcessBulkPaymentsAsync(
                    _context,
                    entityId,
                    payments,
                    batchSize,
                    TimeSpan.FromSeconds(timeoutSeconds));

                _logger.LogInformation("Procesamiento masivo completado en {Elapsed}ms - Entidad: {EntityId}, Exitosos: {Success}, Fallidos: {Failed}",
                    stopwatch.ElapsedMilliseconds, entityId, result.SuccessfulPayments, result.FailedPayments);

                return Ok(result);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timeout en procesamiento masivo - Entidad: {EntityId}, Pagos: {Count}",
                    entityId, payments.Count);
                return StatusCode(408, new { Error = "Timeout en el procesamiento masivo", EntityId = entityId, PaymentCount = payments.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en procesamiento masivo - Entidad: {EntityId}, Pagos: {Count}",
                    entityId, payments.Count);
                return StatusCode(500, new { Error = "Error interno del servidor" });
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Consulta masiva de estados de cuenta optimizada
        /// </summary>
        [HttpPost("bulk-account-status/{entityId}")]
        public async Task<ActionResult<List<AccountStatusResult>>> GetBulkAccountStatus(
            string entityId,
            [FromBody] List<int> customerIds,
            [FromQuery] int timeoutSeconds = 45)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new List<AccountStatusResult>();

            try
            {
                _logger.LogInformation("Consulta masiva iniciada - Entidad: {EntityId}, Clientes: {Count}",
                    entityId, customerIds.Count);

                // Procesar en paralelo con control de concurrencia
                var tasks = customerIds.Select(async customerId =>
                {
                    try
                    {
                        return await _concurrencyService.GetAccountStatusAsync(
                            _context,
                            entityId,
                            customerId,
                            TimeSpan.FromSeconds(timeoutSeconds));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Error en consulta individual - Cliente: {CustomerId}: {Message}",
                            customerId, ex.Message);
                        return new AccountStatusResult
                        {
                            CustomerId = customerId,
                            Status = "Error",
                            Balance = 0
                        };
                    }
                });

                results = (await Task.WhenAll(tasks)).ToList();

                _logger.LogInformation("Consulta masiva completada en {Elapsed}ms - Entidad: {EntityId}, Resultados: {Count}",
                    stopwatch.ElapsedMilliseconds, entityId, results.Count);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en consulta masiva - Entidad: {EntityId}, Clientes: {Count}",
                    entityId, customerIds.Count);
                return StatusCode(500, new { Error = "Error interno del servidor" });
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// Obtiene estadísticas de concurrencia
        /// </summary>
        [HttpGet("concurrency-stats")]
        public ActionResult<ConcurrencyStats> GetConcurrencyStats()
        {
            try
            {
                var stats = _concurrencyService.GetConcurrencyStats();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de concurrencia");
                return StatusCode(500, new { Error = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Health check para el sistema bancario
        /// </summary>
        [HttpGet("health")]
        public ActionResult<object> HealthCheck()
        {
            try
            {
                var stats = _concurrencyService.GetConcurrencyStats();
                var healthStatus = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    ActiveEntities = stats.ActiveEntities,
                    AvailableSlots = stats.AvailableGlobalSlots,
                    MaxConcurrentOperations = stats.MaxConcurrentOperations,
                    DatabaseConnection = _context.Database.CanConnect()
                };

                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en health check");
                return StatusCode(503, new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint para pruebas de carga
        /// </summary>
        [HttpPost("load-test/{entityId}")]
        public async Task<ActionResult<LoadTestResult>> LoadTest(
            string entityId,
            [FromBody] LoadTestRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new List<TestResult>();

            try
            {
                _logger.LogInformation("Prueba de carga iniciada - Entidad: {EntityId}, Operaciones: {Count}",
                    entityId, request.OperationCount);

                var tasks = new List<Task<TestResult>>();

                for (int i = 0; i < request.OperationCount; i++)
                {
                    var customerId = request.StartCustomerId + i;

                    if (request.OperationType == "AccountStatus")
                    {
                        tasks.Add(TestAccountStatus(entityId, customerId));
                    }
                    else if (request.OperationType == "Payment")
                    {
                        var paymentRequest = new PaymentRequest
                        {
                            CustomerId = customerId,
                            Amount = request.PaymentAmount ?? 100.00m,
                            Description = $"Test payment {i}"
                        };
                        tasks.Add(TestPayment(entityId, paymentRequest));
                    }
                }

                results = (await Task.WhenAll(tasks)).ToList();

                var loadTestResult = new LoadTestResult
                {
                    EntityId = entityId,
                    OperationType = request.OperationType,
                    TotalOperations = request.OperationCount,
                    SuccessfulOperations = results.Count(r => r.Success),
                    FailedOperations = results.Count(r => !r.Success),
                    AverageResponseTime = TimeSpan.FromMilliseconds(results.Average(r => r.ResponseTimeMs)),
                    TotalTime = stopwatch.Elapsed,
                    Results = results
                };

                _logger.LogInformation("Prueba de carga completada - Entidad: {EntityId}, Exitosos: {Success}, Fallidos: {Failed}, Tiempo: {Elapsed}ms",
                    entityId, loadTestResult.SuccessfulOperations, loadTestResult.FailedOperations, stopwatch.ElapsedMilliseconds);

                return Ok(loadTestResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en prueba de carga - Entidad: {EntityId}", entityId);
                return StatusCode(500, new { Error = "Error interno del servidor" });
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        #region Private Methods for Load Testing
        private async Task<TestResult> TestAccountStatus(string entityId, int customerId)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _concurrencyService.GetAccountStatusAsync(_context, entityId, customerId, TimeSpan.FromSeconds(10));

                return new TestResult
                {
                    Success = true,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    CustomerId = customerId
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    Success = false,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    CustomerId = customerId,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<TestResult> TestPayment(string entityId, PaymentRequest paymentRequest)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _concurrencyService.ProcessPaymentAsync(_context, entityId, paymentRequest, TimeSpan.FromSeconds(10));

                return new TestResult
                {
                    Success = result.Success,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    CustomerId = paymentRequest.CustomerId,
                    TransactionId = result.TransactionId
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    Success = false,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    CustomerId = paymentRequest.CustomerId,
                    ErrorMessage = ex.Message
                };
            }
        }
        #endregion
    }

    #region Load Testing Models
    public class LoadTestRequest
    {
        public int OperationCount { get; set; } = 100;
        public string OperationType { get; set; } = "AccountStatus"; // "AccountStatus" or "Payment"
        public int StartCustomerId { get; set; } = 1;
        public decimal? PaymentAmount { get; set; } = 100.00m;
    }

    public class LoadTestResult
    {
        public string EntityId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty;
        public int TotalOperations { get; set; }
        public int SuccessfulOperations { get; set; }
        public int FailedOperations { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public TimeSpan TotalTime { get; set; }
        public List<TestResult> Results { get; set; } = new();
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public long ResponseTimeMs { get; set; }
        public int CustomerId { get; set; }
        public string? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
    }
    #endregion
}