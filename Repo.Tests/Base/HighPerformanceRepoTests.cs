using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Interfaces;
using System.Data;

namespace Repo.Tests.Base
{
    /// <summary>
    /// Tests for HighPerformanceRepo behavior (Issue #35).
    /// Validates high-performance operations, metrics, and configuration.
    /// Note: Bulk operations require SQLite real database connection.
    /// </summary>
    [TestFixture]
    public class HighPerformanceRepoTests
    {
        private TestDbContext _context = null!;
        private ILogger<HighPerformanceRepo<HighPerfEntity, TestDbContext>> _logger = null!;

        [SetUp]
        public void SetUp()
        {
            // Use SQLite in-memory mode for high-performance tests requiring real SQL connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .Options;

            _context = new TestDbContext(options);
            _context.Database.EnsureCreated();
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HighPerformanceRepo<HighPerfEntity, TestDbContext>>.Instance;
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region Constructor and Configuration

        [Test]
        public void Constructor_WithDefaultParameters_CreatesInstance()
        {
            // Act
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Assert
            Assert.That(repo, Is.Not.Null);
            Assert.That(repo, Is.InstanceOf<IHighPerformanceRepo<HighPerfEntity>>());
        }

        [Test]
        public void Constructor_WithCustomParameters_CreatesInstance()
        {
            // Act
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(
                _context, _logger, null, maxConcurrency: 8, maxBatchSize: 500, batchTimeoutMs: 1000);

            // Assert
            Assert.That(repo, Is.Not.Null);
        }

        #endregion

        #region ConfigureForHighPerformance

        [Test]
        public void ConfigureForHighPerformance_SetsTrackingBehavior()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act
            repo.ConfigureForHighPerformance(commandTimeout: 120, enableRetryOnFailure: false);

            // Assert
            Assert.That(_context.ChangeTracker.QueryTrackingBehavior, Is.EqualTo(QueryTrackingBehavior.NoTracking));
            Assert.That(_context.ChangeTracker.AutoDetectChangesEnabled, Is.False);
        }

        [Test]
        public void ConfigureForHighPerformance_DefaultParameters_Works()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => repo.ConfigureForHighPerformance());
        }

        #endregion

        #region ClearContextAsync

        [Test]
        public async Task ClearContextAsync_ClearsChangeTracker()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            _context.HighPerfEntities.Add(new HighPerfEntity { Name = "Test" });
            Assert.That(_context.ChangeTracker.Entries().Count(), Is.EqualTo(1));

            // Act
            await repo.ClearContextAsync();

            // Assert
            Assert.That(_context.ChangeTracker.Entries().Count(), Is.EqualTo(0));
        }

        #endregion

        #region ExecuteWithMetricsAsync - Action overload

        [Test]
        public async Task ExecuteWithMetricsAsync_Action_ReturnsMetrics()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var operationExecuted = false;

            // Act
            var metrics = await repo.ExecuteWithMetricsAsync(async () =>
            {
                await Task.Delay(10);
                operationExecuted = true;
            });

            // Assert
            Assert.That(operationExecuted, Is.True);
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.ExecutionTime, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(metrics.MemoryUsageBefore, Is.GreaterThanOrEqualTo(0));
            Assert.That(metrics.MemoryUsageAfter, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ExecuteWithMetricsAsync_Action_Throws_PreservesException()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await repo.ExecuteWithMetricsAsync(async () =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Test error");
                });
            });

            Assert.That(ex!.Message, Is.EqualTo("Test error"));
        }

        #endregion

        #region ExecuteWithMetricsAsync - Func<TResult> overload

        [Test]
        public async Task ExecuteWithMetricsAsync_Func_ReturnsResultAndMetrics()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act
            var (result, metrics) = await repo.ExecuteWithMetricsAsync(async () =>
            {
                await Task.Delay(10);
                return 42;
            });

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(metrics, Is.Not.Null);
            Assert.That(metrics.ExecutionTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void ExecuteWithMetricsAsync_Func_Throws_PreservesException()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await repo.ExecuteWithMetricsAsync(async () =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Test error");
                });
            });

            Assert.That(ex!.Message, Is.EqualTo("Test error"));
        }

        #endregion

        #region ProcessParallelAsync

        [Test]
        public async Task ProcessParallelAsync_ProcessesAllItems()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var items = Enumerable.Range(1, 10).Select(i => new HighPerfEntity { Id = i, Name = $"Item{i}" }).ToList();
            var processedItems = new List<string>();

            // Act
            await repo.ProcessParallelAsync(items, async item =>
            {
                await Task.Delay(1);
                lock (processedItems)
                {
                    processedItems.Add(item.Name);
                }
                return item.Name;
            }, maxConcurrency: 4);

            // Assert
            Assert.That(processedItems.Count, Is.EqualTo(10));
            Assert.That(processedItems, Is.EquivalentTo(items.Select(i => i.Name)));
        }

        [Test]
        public async Task ProcessParallelAsync_EmptyCollection_CompletesWithoutError()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var items = new List<HighPerfEntity>();

            // Act & Assert - Should not throw
            await repo.ProcessParallelAsync(items, async item =>
            {
                await Task.CompletedTask;
                return item.Name;
            });
        }

        [Test]
        public void ProcessParallelAsync_ThrowsException_PropagatesError()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var items = new List<HighPerfEntity> { new() { Name = "Item1" } };

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await repo.ProcessParallelAsync<object>(items, async (HighPerfEntity _) =>
                {
                    await Task.CompletedTask;
                    throw new InvalidOperationException("Test error");
                });
            });
        }

        #endregion

        #region ExecuteInTransactionAsync

        [Test]
        public async Task ExecuteInTransactionAsync_CommitsOnSuccess()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act
            var result = await repo.ExecuteInTransactionAsync(async () =>
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = "Transaction Test" });
                await _context.SaveChangesAsync();
                return 42;
            });

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(_context.HighPerfEntities.Count(), Is.EqualTo(1));
        }

        [Test]
        public void ExecuteInTransactionAsync_RollsBackOnException()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await repo.ExecuteInTransactionAsync<int>(async () =>
                {
                    _context.HighPerfEntities.Add(new HighPerfEntity { Name = "Transaction Test" });
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException("Test error");
                });
            });

            // Assert - Transaction should have rolled back
            Assert.That(_context.HighPerfEntities.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task ExecuteInTransactionAsync_WithIsolationLevel_UsesSpecifiedLevel()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act - Should not throw with explicit isolation level
            var result = await repo.ExecuteInTransactionAsync(async () =>
            {
                await Task.CompletedTask;
                return 123;
            }, IsolationLevel.ReadCommitted);

            // Assert
            Assert.That(result, Is.EqualTo(123));
        }

        #endregion

        #region ExecuteDistributedTransactionAsync

        [Test]
        public async Task ExecuteDistributedTransactionAsync_ExecutesAllOperations()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var operationsExecuted = new List<int>();

            // Act
            await repo.ExecuteDistributedTransactionAsync(
                async () => { await Task.Delay(1); operationsExecuted.Add(1); },
                async () => { await Task.Delay(1); operationsExecuted.Add(2); },
                async () => { await Task.Delay(1); operationsExecuted.Add(3); }
            );

            // Assert
            Assert.That(operationsExecuted, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task ExecuteDistributedTransactionAsync_EmptyOperations_Completes()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act & Assert - Should not throw
            await repo.ExecuteDistributedTransactionAsync();
        }

        #endregion

        #region GetPagedOptimizedAsync

        [Test]
        public async Task GetPagedOptimizedAsync_ReturnsPagedResults()
        {
            // Arrange
            for (int i = 1; i <= 25; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"Entity{i}" });
            }
            _context.SaveChanges();

            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var request = new Repo.Repository.Models.PagedRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await repo.GetPagedOptimizedAsync(request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(10));
            Assert.That(result.TotalCount, Is.EqualTo(25));
            Assert.That(result.TotalPages, Is.EqualTo(3));
        }

        [Test]
        public async Task GetPagedOptimizedAsync_WithFilter_ReturnsFilteredResults()
        {
            // Arrange
            for (int i = 1; i <= 10; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"Entity{i}", Value = i });
            }
            _context.SaveChanges();

            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var request = new Repo.Repository.Models.PagedRequest { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await repo.GetPagedOptimizedAsync(request, e => e.Value > 5);

            // Assert
            Assert.That(result.Data.Count, Is.EqualTo(5));
            Assert.That(result.TotalCount, Is.EqualTo(5));
        }

        [Test]
        public async Task GetPagedOptimizedAsync_SecondPage_ReturnsCorrectPage()
        {
            // Arrange
            for (int i = 1; i <= 25; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"Entity{i}" });
            }
            _context.SaveChanges();

            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var request = new Repo.Repository.Models.PagedRequest { PageNumber = 2, PageSize = 10 };

            // Act
            var result = await repo.GetPagedOptimizedAsync(request);

            // Assert
            Assert.That(result.Page, Is.EqualTo(2));
            Assert.That(result.Data.Count, Is.EqualTo(10));
        }

        #endregion

        #region Bulk Operations (Requires real SQL database)

        [Test]
        public async Task BulkInsertAsync_InsertsEntities()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var entities = Enumerable.Range(1, 100)
                .Select(i => new HighPerfEntity { Name = $"BulkEntity{i}" })
                .ToList();

            // Act
            var result = await repo.BulkInsertAsync(entities, batchSize: 50);

            // Assert
            Assert.That(result, Is.EqualTo(100));
            Assert.That(_context.HighPerfEntities.Count(), Is.EqualTo(100));
        }

        [Test]
        public async Task BulkInsertAsync_EmptyList_ReturnsZero()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var entities = new List<HighPerfEntity>();

            // Act
            var result = await repo.BulkInsertAsync(entities);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task BulkUpdateAsync_UpdatesEntities()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var entities = Enumerable.Range(1, 50)
                .Select(i => new HighPerfEntity { Name = $"Original{i}" })
                .ToList();
            await repo.BulkInsertAsync(entities);

            // Modify entities
            foreach (var entity in entities)
            {
                entity.Name = $"Updated{entity.Id}";
            }

            // Act
            var result = await repo.BulkUpdateAsync(entities, batchSize: 25);

            // Assert
            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public async Task BulkDeleteAsync_DeletesEntities()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var entities = Enumerable.Range(1, 50)
                .Select(i => new HighPerfEntity { Name = $"ToDelete{i}" })
                .ToList();
            await repo.BulkInsertAsync(entities);

            // Act
            var result = await repo.BulkDeleteAsync(entities, batchSize: 25);

            // Assert
            Assert.That(result, Is.EqualTo(50));
            Assert.That(_context.HighPerfEntities.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task BulkDeleteAsync_WithPredicate_DeletesMatchingEntities()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            for (int i = 1; i <= 20; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"Entity{i}", Value = i });
            }
            _context.SaveChanges();

            // Act
            var result = await repo.BulkDeleteAsync(e => e.Value > 10);

            // Assert
            Assert.That(result, Is.EqualTo(10));
            Assert.That(_context.HighPerfEntities.Count(), Is.EqualTo(10));
        }

        [Test]
        public async Task BulkDeleteAsync_PredicateNoMatches_ReturnsZero()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            _context.HighPerfEntities.Add(new HighPerfEntity { Name = "Entity", Value = 5 });
            _context.SaveChanges();

            // Act
            var result = await repo.BulkDeleteAsync(e => e.Value > 100);

            // Assert
            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public async Task BulkMergeAsync_InsertsAndUpdatesEntities()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            
            // Insert initial entities
            var existing = Enumerable.Range(1, 10)
                .Select(i => new HighPerfEntity { Name = $"Existing{i}" })
                .ToList();
            await repo.BulkInsertAsync(existing);

            // Prepare merge list (some exist, some new)
            var mergeEntities = new List<HighPerfEntity>();
            for (int i = 1; i <= 20; i++)
            {
                mergeEntities.Add(new HighPerfEntity 
                { 
                    Id = i <= 10 ? existing[i - 1].Id : 0,
                    Name = $"Merged{i}" 
                });
            }

            // Act
            var result = await repo.BulkMergeAsync(mergeEntities, batchSize: 15);

            // Assert
            Assert.That(result, Is.EqualTo(20));
        }

        #endregion

        #region StreamAsync

        [Test]
        public async Task StreamAsync_ReturnsAllEntities()
        {
            // Arrange
            for (int i = 1; i <= 100; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"StreamEntity{i}", Value = i });
            }
            _context.SaveChanges();

            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var count = 0;

            // Act
            await foreach (var entity in repo.StreamAsync())
            {
                count++;
            }

            // Assert
            Assert.That(count, Is.EqualTo(100));
        }

        [Test]
        public async Task StreamAsync_WithFilter_ReturnsFilteredEntities()
        {
            // Arrange
            for (int i = 1; i <= 50; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"StreamEntity{i}", Value = i });
            }
            _context.SaveChanges();

            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var count = 0;

            // Act
            await foreach (var entity in repo.StreamAsync(e => e.Value > 40))
            {
                count++;
            }

            // Assert
            Assert.That(count, Is.EqualTo(10));
        }

        #endregion

        #region ProcessBatchAsync

        [Test]
        public async Task ProcessBatchAsync_ProcessesAllBatches()
        {
            // Arrange
            for (int i = 1; i <= 50; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"BatchEntity{i}", Value = i });
            }
            _context.SaveChanges();

            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var processedCount = 0;

            // Act
            await repo.ProcessBatchAsync(
                e => e.Value > 0,
                async batch =>
                {
                    processedCount += batch.Count();
                    await Task.CompletedTask;
                    return batch.Count();
                },
                batchSize: 10,
                maxConcurrency: 2);

            // Assert
            Assert.That(processedCount, Is.EqualTo(50));
        }

        [Test]
        public async Task ProcessBatchAsync_WithFilter_ProcessesMatchingOnly()
        {
            // Arrange
            for (int i = 1; i <= 50; i++)
            {
                _context.HighPerfEntities.Add(new HighPerfEntity { Name = $"BatchEntity{i}", Value = i });
            }
            _context.SaveChanges();

            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);
            var processedCount = 0;

            // Act
            await repo.ProcessBatchAsync(
                e => e.Value > 40,
                async batch =>
                {
                    processedCount += batch.Count();
                    await Task.CompletedTask;
                    return batch.Count();
                },
                batchSize: 5);

            // Assert
            Assert.That(processedCount, Is.EqualTo(10));
        }

        #endregion

        #region Dispose

        [Test]
        public void Dispose_DoesNotThrow()
        {
            // Arrange
            var repo = new HighPerformanceRepo<HighPerfEntity, TestDbContext>(_context, _logger);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => repo.Dispose());
        }

        #endregion

        #region Test Infrastructure

        public class TestDbContext : DbContext
        {
            public DbSet<HighPerfEntity> HighPerfEntities { get; set; } = null!;

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }
        }

        public class HighPerfEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        #endregion
    }
}
