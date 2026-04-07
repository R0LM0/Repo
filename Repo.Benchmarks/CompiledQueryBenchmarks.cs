using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Repo.Repository.Base;

namespace Repo.Benchmarks
{
    /// <summary>
    /// Benchmarks demonstrating the performance improvement of compiled queries.
    /// 
    /// <para>
    /// Run with: dotnet run -c Release --filter '*'
    /// Or: dotnet run -c Release --filter '*CompiledQueryBenchmarks*'
    /// </para>
    /// </summary>
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    [SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
    public class CompiledQueryBenchmarks
    {
        private TestDbContext _context = null!;
        private RepoBase<BenchmarkEntity, TestDbContext> _standardRepo = null!;
        private CompiledRepoBase<BenchmarkEntity, TestDbContext> _compiledRepo = null!;
        private ILogger<RepoBase<BenchmarkEntity, TestDbContext>> _logger = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            _context = new TestDbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();

            // Seed data
            for (int i = 1; i <= 1000; i++)
            {
                _context.Entities.Add(new BenchmarkEntity
                {
                    Id = i,
                    Name = $"Entity {i}",
                    Data = $"Data for entity {i}"
                });
            }
            _context.SaveChanges();

            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<BenchmarkEntity, TestDbContext>>.Instance;

            var repoOptions = new RepoOptions
            {
                EnableCompiledQueries = true,
                LogCompiledQueryUsage = false
            };

            _standardRepo = new RepoBase<BenchmarkEntity, TestDbContext>(_context, _logger);
            _compiledRepo = new CompiledRepoBase<BenchmarkEntity, TestDbContext>(_context, _logger, repoOptions);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Reset context state between iterations
            _context.ChangeTracker.Clear();
        }

        // ==================== GetById Benchmarks ====================

        [Benchmark(Baseline = true, Description = "Standard GetById (int)")]
        public async Task<BenchmarkEntity> Standard_GetById_Int()
        {
            return await _standardRepo.GetById(500);
        }

        [Benchmark(Description = "Compiled GetById (int)")]
        public async Task<BenchmarkEntity> Compiled_GetById_Int()
        {
            return await _compiledRepo.GetById(500);
        }

        [Benchmark(Description = "Standard GetById (long)")]
        public async Task<BenchmarkEntity> Standard_GetById_Long()
        {
            return await _standardRepo.GetById(500L);
        }

        [Benchmark(Description = "Compiled GetById (long)")]
        public async Task<BenchmarkEntity> Compiled_GetById_Long()
        {
            return await _compiledRepo.GetById(500L);
        }

        // ==================== GetAll Benchmarks ====================

        [Benchmark(Description = "Standard GetAllAsync (tracking)")]
        public async Task<IEnumerable<BenchmarkEntity>> Standard_GetAll_Tracking()
        {
            return await _standardRepo.GetAllAsync(asNoTracking: false);
        }

        [Benchmark(Description = "Compiled GetAllAsync (tracking)")]
        public async Task<IEnumerable<BenchmarkEntity>> Compiled_GetAll_Tracking()
        {
            return await _compiledRepo.GetAllAsync(asNoTracking: false);
        }

        [Benchmark(Description = "Standard GetAllAsync (no tracking)")]
        public async Task<IEnumerable<BenchmarkEntity>> Standard_GetAll_NoTracking()
        {
            return await _standardRepo.GetAllAsync(asNoTracking: true);
        }

        [Benchmark(Description = "Compiled GetAllAsync (no tracking)")]
        public async Task<IEnumerable<BenchmarkEntity>> Compiled_GetAll_NoTracking()
        {
            return await _compiledRepo.GetAllAsync(asNoTracking: true);
        }

        // ==================== Count Benchmarks ====================

        [Benchmark(Description = "Standard CountAsync")]
        public async Task<int> Standard_Count()
        {
            return await _standardRepo.CountAsync();
        }

        [Benchmark(Description = "Compiled CountAsync")]
        public async Task<int> Compiled_Count()
        {
            return await _compiledRepo.CountAllAsync();
        }
    }

    /// <summary>
    /// Entity used for benchmarking.
    /// </summary>
    public class BenchmarkEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test database context for benchmarks.
    /// </summary>
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<BenchmarkEntity> Entities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BenchmarkEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.Property(e => e.Data).HasMaxLength(500);
            });
        }
    }

    /// <summary>
    /// Entry point for benchmarks.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("================================================================");
            Console.WriteLine("Repo Compiled Queries Benchmark");
            Console.WriteLine("================================================================");
            Console.WriteLine();
            Console.WriteLine("This benchmark compares standard EF Core queries vs compiled queries.");
            Console.WriteLine("Compiled queries provide 20-30% performance improvement by skipping");
            Console.WriteLine("the expression tree compilation step on each execution.");
            Console.WriteLine();
            Console.WriteLine("Expected improvements:");
            Console.WriteLine("  - GetById: ~20-30% faster");
            Console.WriteLine("  - GetAllAsync: ~15-25% faster");
            Console.WriteLine("  - CountAsync: ~10-20% faster");
            Console.WriteLine();
            Console.WriteLine("Tradeoffs:");
            Console.WriteLine("  - Higher memory usage (compiled delegates are cached)");
            Console.WriteLine("  - Slight startup overhead for initial compilation");
            Console.WriteLine("  - Limited flexibility (no dynamic predicates)");
            Console.WriteLine();
            Console.WriteLine("================================================================");
            Console.WriteLine();

            var config = DefaultConfig.Instance
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

            BenchmarkRunner.Run<CompiledQueryBenchmarks>(config);
        }
    }
}
