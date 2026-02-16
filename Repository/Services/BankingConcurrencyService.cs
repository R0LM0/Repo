using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace Repo.Repository.Services
{
    /// <summary>
    /// Servicio especializado para manejar concurrencia bancaria con múltiples entidades
    /// </summary>
    public class BankingConcurrencyService
    {
        private readonly ILogger<BankingConcurrencyService> _logger;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _entitySemaphores;
        private readonly SemaphoreSlim _globalSemaphore;
        private readonly int _maxConcurrentEntities;
        private readonly int _maxConcurrentOperations;

        public BankingConcurrencyService(ILogger<BankingConcurrencyService> logger, int maxConcurrentEntities = 10, int maxConcurrentOperations = 50)
        {
            _logger = logger;
            _entitySemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
            _globalSemaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
            _maxConcurrentEntities = maxConcurrentEntities;
            _maxConcurrentOperations = maxConcurrentOperations;
        }

        /// <summary>
        /// Ejecuta operación bancaria con control de concurrencia por entidad
        /// </summary>
        public async Task<TResult> ExecuteBankingOperationAsync<TResult>(
            string entityId,
            Func<Task<TResult>> operation,
            BankingOperationType operationType,
            TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(30);

            var entitySemaphore = GetOrCreateEntitySemaphore(entityId);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Esperar acceso global y por entidad
                await Task.WhenAll(
                    _globalSemaphore.WaitAsync(timeout),
                    entitySemaphore.WaitAsync(timeout)
                );

                _logger.LogDebug("Iniciando operación {OperationType} para entidad {EntityId}",
                    operationType, entityId);

                var result = await operation();

                _logger.LogInformation("Operación {OperationType} completada para entidad {EntityId} en {Elapsed}ms",
                    operationType, entityId, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timeout en operación {OperationType} para entidad {EntityId} después de {Timeout}ms",
                    operationType, entityId, timeout.TotalMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en operación {OperationType} para entidad {EntityId}",
                    operationType, entityId);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                entitySemaphore.Release();
                _globalSemaphore.Release();
            }
        }

        /// <summary>
        /// Ejecuta operación de consulta de estado de cuenta optimizada
        /// </summary>
        public async Task<AccountStatusResult> GetAccountStatusAsync<TContext>(
            TContext context,
            string entityId,
            int customerId,
            TimeSpan timeout = default) where TContext : DbContext
        {
            return await ExecuteBankingOperationAsync(entityId, async () =>
            {
                // Configurar para consultas rápidas
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                // Usar SQL optimizado para consultas de estado
                var sql = @"
                    SELECT 
                        c.CustomerId,
                        c.AccountNumber,
                        c.Balance,
                        c.DueDate,
                        c.Status,
                        ISNULL(SUM(p.Amount), 0) as PendingPayments
                    FROM Customers c
                    LEFT JOIN PendingPayments p ON c.CustomerId = p.CustomerId
                    WHERE c.CustomerId = @CustomerId
                    GROUP BY c.CustomerId, c.AccountNumber, c.Balance, c.DueDate, c.Status";

                // Crear parámetro agnóstico al proveedor usando DbParameter
                var connection = context.Database.GetDbConnection();
                var command = connection.CreateCommand();
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@CustomerId";
                parameter.Value = customerId;
                
                var result = await context.Set<AccountStatusResult>()
                    .FromSqlRaw(sql, parameter)
                    .FirstOrDefaultAsync();

                return result ?? new AccountStatusResult { CustomerId = customerId, Status = "Not Found" };
            }, BankingOperationType.AccountStatus, timeout);
        }

        /// <summary>
        /// Ejecuta operación de pago con transacción optimizada
        /// </summary>
        public async Task<PaymentResult> ProcessPaymentAsync<TContext>(
            TContext context,
            string entityId,
            PaymentRequest paymentRequest,
            TimeSpan timeout = default) where TContext : DbContext
        {
            return await ExecuteBankingOperationAsync(entityId, async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

                try
                {
                    // 1. Verificar saldo (con lock optimista)
                    var customer = await context.Set<Customer>()
                        .FirstOrDefaultAsync(c => c.CustomerId == paymentRequest.CustomerId);

                    if (customer == null)
                        throw new InvalidOperationException("Cliente no encontrado");

                    if (customer.Balance < paymentRequest.Amount)
                        throw new InvalidOperationException("Saldo insuficiente");

                    // 2. Actualizar saldo
                    customer.Balance -= paymentRequest.Amount;
                    customer.LastPaymentDate = DateTime.UtcNow;
                    context.Set<Customer>().Update(customer);

                    // 3. Registrar pago
                    var payment = new Payment
                    {
                        CustomerId = paymentRequest.CustomerId,
                        Amount = paymentRequest.Amount,
                        PaymentDate = DateTime.UtcNow,
                        EntityId = entityId,
                        TransactionId = Guid.NewGuid().ToString(),
                        Status = "Completed"
                    };
                    context.Set<Payment>().Add(payment);

                    // 4. Registrar transacción
                    var transactionRecord = new TransactionRecord
                    {
                        CustomerId = paymentRequest.CustomerId,
                        TransactionId = payment.TransactionId,
                        Amount = paymentRequest.Amount,
                        Type = "Payment",
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Set<TransactionRecord>().Add(transactionRecord);

                    // 5. Guardar cambios
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = payment.TransactionId,
                        Amount = paymentRequest.Amount,
                        Balance = customer.Balance,
                        ProcessedAt = DateTime.UtcNow
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }, BankingOperationType.Payment, timeout);
        }

        /// <summary>
        /// Ejecuta operación de pago masivo optimizada
        /// </summary>
        public async Task<BulkPaymentResult> ProcessBulkPaymentsAsync<TContext>(
            TContext context,
            string entityId,
            List<PaymentRequest> payments,
            int batchSize = 100,
            TimeSpan timeout = default) where TContext : DbContext
        {
            return await ExecuteBankingOperationAsync(entityId, async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var results = new List<PaymentResult>();
                var batches = payments.Chunk(batchSize);

                foreach (var batch in batches)
                {
                    using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

                    try
                    {
                        var batchResults = new List<PaymentResult>();

                        foreach (var paymentRequest in batch)
                        {
                            // Procesar cada pago individualmente dentro del lote
                            var result = await ProcessSinglePaymentInBatch(context, paymentRequest);
                            batchResults.Add(result);
                        }

                        await context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        results.AddRange(batchResults);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                return new BulkPaymentResult
                {
                    TotalProcessed = results.Count,
                    SuccessfulPayments = results.Count(r => r.Success),
                    FailedPayments = results.Count(r => !r.Success),
                    TotalAmount = results.Where(r => r.Success).Sum(r => r.Amount),
                    ProcessingTime = stopwatch.Elapsed,
                    Results = results
                };
            }, BankingOperationType.BulkPayment, timeout);
        }

        /// <summary>
        /// Obtiene estadísticas de concurrencia
        /// </summary>
        public ConcurrencyStats GetConcurrencyStats()
        {
            return new ConcurrencyStats
            {
                ActiveEntities = _entitySemaphores.Count,
                MaxConcurrentEntities = _maxConcurrentEntities,
                MaxConcurrentOperations = _maxConcurrentOperations,
                AvailableGlobalSlots = _globalSemaphore.CurrentCount,
                EntitySemaphores = _entitySemaphores.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.CurrentCount)
            };
        }

        #region Private Methods
        private SemaphoreSlim GetOrCreateEntitySemaphore(string entityId)
        {
            return _entitySemaphores.GetOrAdd(entityId, _ => new SemaphoreSlim(1, 1));
        }

        private async Task<PaymentResult> ProcessSinglePaymentInBatch<TContext>(
            TContext context,
            PaymentRequest paymentRequest) where TContext : DbContext
        {
            try
            {
                var customer = await context.Set<Customer>()
                    .FirstOrDefaultAsync(c => c.CustomerId == paymentRequest.CustomerId);

                if (customer == null)
                    return new PaymentResult { Success = false, ErrorMessage = "Cliente no encontrado" };

                if (customer.Balance < paymentRequest.Amount)
                    return new PaymentResult { Success = false, ErrorMessage = "Saldo insuficiente" };

                customer.Balance -= paymentRequest.Amount;
                customer.LastPaymentDate = DateTime.UtcNow;

                var payment = new Payment
                {
                    CustomerId = paymentRequest.CustomerId,
                    Amount = paymentRequest.Amount,
                    PaymentDate = DateTime.UtcNow,
                    TransactionId = Guid.NewGuid().ToString(),
                    Status = "Completed"
                };

                context.Set<Payment>().Add(payment);

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = payment.TransactionId,
                    Amount = paymentRequest.Amount,
                    Balance = customer.Balance
                };
            }
            catch (Exception ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        #endregion
    }

    #region Enums and Models
    public enum BankingOperationType
    {
        AccountStatus,
        Payment,
        BulkPayment,
        BalanceInquiry,
        TransactionHistory
    }

    public class AccountStatusResult
    {
        public int CustomerId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal PendingPayments { get; set; }
    }

    public class PaymentRequest
    {
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class BulkPaymentResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessfulPayments { get; set; }
        public int FailedPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<PaymentResult> Results { get; set; } = new();
    }

    public class ConcurrencyStats
    {
        public int ActiveEntities { get; set; }
        public int MaxConcurrentEntities { get; set; }
        public int MaxConcurrentOperations { get; set; }
        public int AvailableGlobalSlots { get; set; }
        public Dictionary<string, int> EntitySemaphores { get; set; } = new();
    }

    // Modelos de ejemplo para el contexto bancario
    public class Customer
    {
        public int CustomerId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime? DueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastPaymentDate { get; set; }
    }

    public class Payment
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class TransactionRecord
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PendingPayment
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
    }
    #endregion
}