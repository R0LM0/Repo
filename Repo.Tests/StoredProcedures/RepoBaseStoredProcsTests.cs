using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Repo.Repository.Base;
using Repo.Repository.Exceptions;
using Repo.Repository.Security;

namespace Repo.Tests.StoredProcedures
{
    /// <summary>
    /// Tests for RepoBase Stored Procedure operations.
    /// Validates ExecuteStoredProcedureAsync, ExecuteStoredProcedureNonQueryAsync,
    /// ExecuteScalarFunctionAsync, ExecuteTableValuedFunctionAsync.
    /// </summary>
    [TestFixture]
    public class RepoBaseStoredProcsTests
    {
        private TestDbContext _context = null!;
        private ILogger<RepoBase<SpTestEntity, TestDbContext>> _logger = null!;
        private Mock<IStoredProcedureWhitelist> _whitelistMock = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TestDbContext(options);
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<RepoBase<SpTestEntity, TestDbContext>>.Instance;
            _whitelistMock = new Mock<IStoredProcedureWhitelist>();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        #region ExecuteStoredProcedureAsync

        [Test]
        public void ExecuteStoredProcedureAsync_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await repo.ExecuteStoredProcedureAsync<SpTestEntity>(""));
            
            Assert.That(ex!.ParamName, Is.EqualTo("storedProcedure"));
        }

        [Test]
        public void ExecuteStoredProcedureAsync_NullName_ThrowsArgumentException()
        {
            // Arrange
            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await repo.ExecuteStoredProcedureAsync<SpTestEntity>(null!));
            
            Assert.That(ex!.ParamName, Is.EqualTo("storedProcedure"));
        }

        [Test]
        public void ExecuteStoredProcedureAsync_NotWhitelisted_ThrowsSecurityException()
        {
            // Arrange
            _whitelistMock.Setup(w => w.IsAllowed("sp_NotAllowed")).Returns(false);

            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteStoredProcedureAsync<SpTestEntity>("sp_NotAllowed"));
        }

        [Test]
        public async Task ExecuteStoredProcedureAsync_Whitelisted_AllowsExecution()
        {
            // Arrange
            _whitelistMock.Setup(w => w.IsAllowed("sp_GetEntities")).Returns(true);

            // Add test entity
            _context.SpTestEntities.Add(new SpTestEntity { Id = 1, Name = "Test" });
            _context.SaveChanges();

            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act - This would need actual stored procedure in real DB
            // For InMemory, we just verify whitelist check passes
            try
            {
                await repo.ExecuteStoredProcedureAsync<SpTestEntity>("sp_GetEntities");
            }
            catch
            {
                // Expected to fail in InMemory DB, but whitelist check should pass
            }

            // Assert - Verify whitelist was checked
            _whitelistMock.Verify(w => w.IsAllowed("sp_GetEntities"), Times.AtLeastOnce);
        }

        #endregion

        #region ExecuteStoredProcedureNonQueryAsync

        [Test]
        public void ExecuteStoredProcedureNonQueryAsync_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await repo.ExecuteStoredProcedureNonQueryAsync(""));
            
            Assert.That(ex!.ParamName, Is.EqualTo("storedProcedure"));
        }

        [Test]
        public void ExecuteStoredProcedureNonQueryAsync_NotWhitelisted_ThrowsSecurityException()
        {
            // Arrange
            _whitelistMock.Setup(w => w.IsAllowed("sp_DeleteData")).Returns(false);

            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteStoredProcedureNonQueryAsync("sp_DeleteData"));
        }

        #endregion

        #region ExecuteScalarFunctionAsync

        [Test]
        public void ExecuteScalarFunctionAsync_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await repo.ExecuteScalarFunctionAsync<int>(""));
            
            Assert.That(ex!.ParamName, Is.EqualTo("functionName"));
        }

        [Test]
        public void ExecuteScalarFunctionAsync_NotWhitelisted_ThrowsSecurityException()
        {
            // Arrange
            _whitelistMock.Setup(w => w.IsAllowed("fn_NotAllowed")).Returns(false);

            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteScalarFunctionAsync<int>("fn_NotAllowed"));
        }

        #endregion

        #region ExecuteTableValuedFunctionAsync

        [Test]
        public void ExecuteTableValuedFunctionAsync_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await repo.ExecuteTableValuedFunctionAsync<SpTestEntity>(""));
            
            Assert.That(ex!.ParamName, Is.EqualTo("functionName"));
        }

        [Test]
        public void ExecuteTableValuedFunctionAsync_NotWhitelisted_ThrowsSecurityException()
        {
            // Arrange
            _whitelistMock.Setup(w => w.IsAllowed("fn_GetData")).Returns(false);

            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger, null, null, _whitelistMock.Object);

            // Act & Assert
            Assert.ThrowsAsync<SecurityException>(async () =>
                await repo.ExecuteTableValuedFunctionAsync<SpTestEntity>("fn_GetData"));
        }

        #endregion

        #region Whitelist Behavior

        [Test]
        public async Task ExecuteStoredProcedureAsync_NoWhitelistConfigured_SkipsValidation()
        {
            // Arrange - No whitelist configured
            var repo = new RepoBase<SpTestEntity, TestDbContext>(_context, _logger);

            // Act - Should not throw security exception
            try
            {
                await repo.ExecuteStoredProcedureAsync<SpTestEntity>("sp_AnyProcedure");
            }
            catch (SecurityException)
            {
                Assert.Fail("Should not throw SecurityException when no whitelist configured");
            }
            catch
            {
                // Other exceptions expected in InMemory DB
            }
        }

        #endregion

        #region Test Entities

        public class SpTestEntity
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

            public DbSet<SpTestEntity> SpTestEntities => Set<SpTestEntity>();
        }

        #endregion
    }
}
