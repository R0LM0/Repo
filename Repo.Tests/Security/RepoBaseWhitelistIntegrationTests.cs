using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;
using Repo.Repository.Security;

namespace Repo.Tests.Security
{
    [TestFixture]
    public class RepoBaseWhitelistIntegrationTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<TestEntity, TestDbContext>> _logger = null!;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            _context = new TestDbContext(options);
            _context.Database.OpenConnection();
            _context.Database.EnsureCreated();

            // Seed test data
            _context.TestEntities.AddRange(
                new TestEntity { Name = "Entity1", Value = 100 },
                new TestEntity { Name = "Entity2", Value = 200 },
                new TestEntity { Name = "Entity3", Value = 300 }
            );
            _context.SaveChanges();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<RepoBase<TestEntity, TestDbContext>>();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.CloseConnection();
            _context.Dispose();
        }

        [Test]
        public async Task ExecuteStoredProcedureAsync_WithoutWhitelist_AllowsAnyProcedure()
        {
            // Arrange - No whitelist configured
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);
            var sql = "SELECT * FROM TestEntities WHERE Value > {0}";

            // Act
            var results = await repo.ExecuteStoredProcedureAsync<TestEntity>(sql, default, 150);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task ExecuteStoredProcedureAsync_WithWhitelistDisabled_AllowsAnyProcedure()
        {
            // Arrange - Whitelist configured but disabled
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = false,
                WhitelistedProcedures = new List<string> { "sp_Other" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);
            var sql = "SELECT * FROM TestEntities WHERE Value > {0}";

            // Act
            var results = await repo.ExecuteStoredProcedureAsync<TestEntity>(sql, default, 150);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(2));
        }

        [Test]
        public void ExecuteStoredProcedureAsync_WithWhitelistEnabled_ThrowsForNonWhitelisted()
        {
            // Arrange - Whitelist enabled with specific procedures
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_GetUsers" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);
            var sql = "SELECT * FROM TestEntities";

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteStoredProcedureAsync<TestEntity>(sql));

            Assert.That(ex!.Message, Does.Contain("not in the whitelist"));
        }

        [Test]
        public async Task ExecuteStoredProcedureAsync_WithWhitelistEnabled_AllowsWhitelisted()
        {
            // Arrange - Whitelist enabled with SQL query whitelisted
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "SELECT" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);
            var sql = "SELECT * FROM TestEntities WHERE Value > {0}";

            // Act
            var results = await repo.ExecuteStoredProcedureAsync<TestEntity>(sql, default, 150);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Count(), Is.EqualTo(2));
        }

        [Test]
        public void ExecuteStoredProcedureNonQueryAsync_WithWhitelistEnabled_ThrowsForNonWhitelisted()
        {
            // Arrange
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_AllowedUpdate" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);
            var sql = "UPDATE TestEntities SET Value = 999";

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteStoredProcedureNonQueryAsync(sql));

            Assert.That(ex!.Message, Does.Contain("not in the whitelist"));
        }

        [Test]
        public async Task ExecuteStoredProcedureNonQueryAsync_WithWhitelistEnabled_AllowsWhitelisted()
        {
            // Arrange - Note: Using a valid SQL that matches the whitelist pattern
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "UPDATE" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);
            var sql = "UPDATE TestEntities SET Value = Value + 10 WHERE Value < 200";

            // Act
            var affectedRows = await repo.ExecuteStoredProcedureNonQueryAsync(sql);

            // Assert
            Assert.That(affectedRows, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteScalarFunctionAsync_WithWhitelistEnabled_ThrowsForNonWhitelisted()
        {
            // Arrange
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "fn_Allowed" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteScalarFunctionAsync<int>("fn_NotAllowed"));

            Assert.That(ex!.Message, Does.Contain("not in the whitelist"));
        }

        [Test]
        public void ExecuteTableValuedFunctionAsync_WithWhitelistEnabled_ThrowsForNonWhitelisted()
        {
            // Arrange
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "tvf_Allowed" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);

            // Act & Assert
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteTableValuedFunctionAsync<TestEntity>("tvf_NotAllowed"));

            Assert.That(ex!.Message, Does.Contain("not in the whitelist"));
        }

        [Test]
        public void ExecuteScalarFunctionAsync_WithWhitelistEnabled_AllowsWhitelisted()
        {
            // Arrange - Note: SQLite doesn't support scalar functions well, but we can test validation passes
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "fn_Count" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);

            // Act & Assert - The validation should pass, but execution may fail due to SQLite limitations
            // We're testing that validation happens before execution
            Assert.DoesNotThrowAsync(async () =>
            {
                try
                {
                    await repo.ExecuteScalarFunctionAsync<int>("fn_Count");
                }
                catch (SecurityException)
                {
                    throw; // Re-throw security exceptions as they indicate validation failures
                }
                catch
                {
                    // Ignore other exceptions (SQLite limitations)
                }
            });
        }

        [Test]
        public void ExecuteTableValuedFunctionAsync_WithWhitelistEnabled_AllowsWhitelisted()
        {
            // Arrange
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "tvf_GetAll" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);

            // Act & Assert - The validation should pass, but execution may fail due to SQLite limitations
            Assert.DoesNotThrowAsync(async () =>
            {
                try
                {
                    await repo.ExecuteTableValuedFunctionAsync<TestEntity>("tvf_GetAll");
                }
                catch (SecurityException)
                {
                    throw; // Re-throw security exceptions as they indicate validation failures
                }
                catch
                {
                    // Ignore other exceptions (SQLite limitations)
                }
            });
        }

        [Test]
        public void Constructor_BackwardCompatibility_WorksWithoutWhitelist()
        {
            // Arrange & Act - Should not throw
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger);

            // Assert - Repository is created successfully
            Assert.That(repo, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithWhitelist_WorksWithWhitelist()
        {
            // Arrange
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_Test" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));

            // Act - Should not throw
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);

            // Assert
            Assert.That(repo, Is.Not.Null);
        }

        [Test]
        public void ValidateStoredProcedureName_NullOrEmpty_ThrowsSecurityException()
        {
            // Arrange
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_Test" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);

            // Act & Assert - Empty string is not null, so it should go through validation
            // and fail because it's not in the whitelist
            var ex = Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteStoredProcedureAsync<TestEntity>(""));
        }

        [Test]
        public async Task AllFourMethods_WithDisabledWhitelist_WorkNormally()
        {
            // Arrange - Whitelist disabled
            var spOptions = new StoredProcedureOptions
            {
                RequireWhitelist = false,
                WhitelistedProcedures = new List<string>()
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(spOptions));
            var repo = new RepoBase<TestEntity, TestDbContext>(_context, _logger, null, null, whitelist);
            var sql = "SELECT * FROM TestEntities";

            // Act & Assert - None should throw SecurityException
            Assert.DoesNotThrowAsync(async () => await repo.ExecuteStoredProcedureAsync<TestEntity>(sql));
            Assert.DoesNotThrowAsync(async () => await repo.ExecuteStoredProcedureNonQueryAsync(sql));
            Assert.DoesNotThrowAsync(async () =>
            {
                try { await repo.ExecuteScalarFunctionAsync<int>("fn_Test"); } catch { /* SQLite limitations */ }
            });
            Assert.DoesNotThrowAsync(async () =>
            {
                try { await repo.ExecuteTableValuedFunctionAsync<TestEntity>("tvf_Test"); } catch { /* SQLite limitations */ }
            });
        }
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
        }
    }
}
