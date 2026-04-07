using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Repo.Repository.Exceptions;
using Repo.Repository.Security;

namespace Repo.Tests.Security
{
    [TestFixture]
    public class StoredProcedureWhitelistTests
    {
        [Test]
        public void IsAllowed_NullWhitelistConfigured_AllowsAll()
        {
            // Arrange
            var options = new StoredProcedureOptions
            {
                RequireWhitelist = false,
                WhitelistedProcedures = new List<string>()
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(options));

            // Act & Assert
            Assert.That(whitelist.IsAllowed("sp_GetUsers"), Is.True);
            Assert.That(whitelist.IsAllowed("sp_DeleteEverything"), Is.True);
            Assert.That(whitelist.IsAllowed(""), Is.True);
            Assert.That(whitelist.IsAllowed("ANY_PROCEDURE"), Is.True);
        }

        [Test]
        public void IsAllowed_WhitelistEnabled_AllowsOnlyWhitelisted()
        {
            // Arrange
            var options = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_GetUsers", "sp_GetOrders" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(options));

            // Act & Assert
            Assert.That(whitelist.IsAllowed("sp_GetUsers"), Is.True);
            Assert.That(whitelist.IsAllowed("sp_GetOrders"), Is.True);
            Assert.That(whitelist.IsAllowed("sp_GetCustomers"), Is.False);
            Assert.That(whitelist.IsAllowed("sp_DeleteEverything"), Is.False);
        }

        [Test]
        public void IsAllowed_CaseInsensitive_MatchesRegardlessOfCase()
        {
            // Arrange
            var options = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "SP_GETUSERS" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(options));

            // Act & Assert
            Assert.That(whitelist.IsAllowed("sp_getusers"), Is.True);
            Assert.That(whitelist.IsAllowed("Sp_GetUsers"), Is.True);
            Assert.That(whitelist.IsAllowed("SP_GETUSERS"), Is.True);
        }

        [Test]
        public void IsAllowed_ExtractsProcedureNameFromExecCommand()
        {
            // Arrange
            var options = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_GetUsers" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(options));

            // Act & Assert
            Assert.That(whitelist.IsAllowed("EXEC sp_GetUsers @id = 1"), Is.True);
            Assert.That(whitelist.IsAllowed("EXECUTE sp_GetUsers 1, 2"), Is.True);
            Assert.That(whitelist.IsAllowed("exec sp_GetUsers"), Is.True);
            Assert.That(whitelist.IsAllowed("EXEC sp_GetOrders"), Is.False);
        }

        [Test]
        public void IsAllowed_ExtractsFunctionNameFromSelectCommand()
        {
            // Arrange
            var options = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "fn_CalculateTotal", "tvf_GetOrders" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(options));

            // Act & Assert
            Assert.That(whitelist.IsAllowed("SELECT fn_CalculateTotal(@p0, @p1)"), Is.True);
            Assert.That(whitelist.IsAllowed("SELECT * FROM tvf_GetOrders(@p0)"), Is.True);
            Assert.That(whitelist.IsAllowed("SELECT fn_Unknown(@p0)"), Is.False);
        }

        [Test]
        public void IsAllowed_EmptyOrNullName_ReturnsFalseWhenWhitelistEnabled()
        {
            // Arrange
            var options = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_GetUsers" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(options));

            // Act & Assert
            Assert.That(whitelist.IsAllowed(""), Is.False);
            Assert.That(whitelist.IsAllowed("   "), Is.False);
            Assert.That(whitelist.IsAllowed(null!), Is.False);
        }

        [Test]
        public void GetWhitelistedNames_ReturnsCopyOfList()
        {
            // Arrange
            var options = new StoredProcedureOptions
            {
                RequireWhitelist = true,
                WhitelistedProcedures = new List<string> { "sp_GetUsers", "sp_GetOrders" }
            };
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create(options));

            // Act
            var result = whitelist.GetWhitelistedNames();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result, Does.Contain("sp_GetUsers"));
            Assert.That(result, Does.Contain("sp_GetOrders"));
        }

        [Test]
        public void Constructor_NullOptions_CreatesEmptyWhitelist()
        {
            // Arrange & Act
            var whitelist = new DefaultStoredProcedureWhitelist(Options.Create<StoredProcedureOptions>(null!));

            // Assert
            Assert.That(whitelist.GetWhitelistedNames(), Is.Empty);
        }
    }

    [TestFixture]
    public class StoredProcedureOptionsTests
    {
        [Test]
        public void DefaultValues_AreCorrect()
        {
            // Arrange
            var options = new StoredProcedureOptions();

            // Assert
            Assert.That(options.RequireWhitelist, Is.False);
            Assert.That(options.ThrowOnValidationFailure, Is.True);
            Assert.That(options.WhitelistedProcedures, Is.Empty);
        }

        [Test]
        public void AddWhitelistedProcedure_AddsUniqueNames()
        {
            // Arrange
            var options = new StoredProcedureOptions();

            // Act
            options.AddWhitelistedProcedure("sp_GetUsers");
            options.AddWhitelistedProcedure("sp_GetUsers"); // Duplicate
            options.AddWhitelistedProcedure("sp_GetOrders");

            // Assert
            Assert.That(options.WhitelistedProcedures.Count, Is.EqualTo(2));
            Assert.That(options.WhitelistedProcedures, Does.Contain("sp_GetUsers"));
            Assert.That(options.WhitelistedProcedures, Does.Contain("sp_GetOrders"));
        }

        [Test]
        public void AddWhitelistedProcedure_SkipsNullOrEmpty()
        {
            // Arrange
            var options = new StoredProcedureOptions();

            // Act
            options.AddWhitelistedProcedure("");
            options.AddWhitelistedProcedure("   ");
            options.AddWhitelistedProcedure(null!);
            options.AddWhitelistedProcedure("sp_GetUsers");

            // Assert
            Assert.That(options.WhitelistedProcedures.Count, Is.EqualTo(1));
            Assert.That(options.WhitelistedProcedures[0], Is.EqualTo("sp_GetUsers"));
        }

        [Test]
        public void AddWhitelistedProcedures_AddsMultiple()
        {
            // Arrange
            var options = new StoredProcedureOptions();

            // Act
            options.AddWhitelistedProcedures("sp_GetUsers", "sp_GetOrders", "sp_GetCustomers");

            // Assert
            Assert.That(options.WhitelistedProcedures.Count, Is.EqualTo(3));
        }

        [Test]
        public void AddWhitelistedProcedure_ReturnsSameInstance_ForChaining()
        {
            // Arrange
            var options = new StoredProcedureOptions();

            // Act
            var result = options.AddWhitelistedProcedure("sp_GetUsers")
                               .AddWhitelistedProcedure("sp_GetOrders");

            // Assert
            Assert.That(result, Is.SameAs(options));
            Assert.That(options.WhitelistedProcedures.Count, Is.EqualTo(2));
        }
    }

    [TestFixture]
    public class SecurityExceptionTests
    {
        [Test]
        public void Constructor_WithMessage_SetsMessage()
        {
            // Arrange & Act
            var ex = new SecurityException("Test message");

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Test message"));
        }

        [Test]
        public void Constructor_WithInnerException_SetsInnerException()
        {
            // Arrange
            var inner = new InvalidOperationException("Inner error");

            // Act
            var ex = new SecurityException("Test message", inner);

            // Assert
            Assert.That(ex.Message, Is.EqualTo("Test message"));
            Assert.That(ex.InnerException, Is.SameAs(inner));
        }

        [Test]
        public void ProcedureNotWhitelisted_CreatesDescriptiveMessage()
        {
            // Arrange & Act
            var ex = SecurityException.ProcedureNotWhitelisted("sp_MaliciousProc");

            // Assert
            Assert.That(ex.Message, Does.Contain("sp_MaliciousProc"));
            Assert.That(ex.Message, Does.Contain("not in the whitelist"));
            Assert.That(ex.Message, Does.Contain("Access denied"));
            Assert.That(ex.Message, Does.Contain("StoredProcedureOptions.WhitelistedProcedures"));
        }

        [Test]
        public void SecurityException_IsRepositoryException()
        {
            // Arrange & Act
            var ex = new SecurityException("Test");

            // Assert
            Assert.That(ex, Is.InstanceOf<RepositoryException>());
        }
    }
}
