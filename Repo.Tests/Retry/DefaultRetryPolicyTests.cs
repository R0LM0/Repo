using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Repo.Repository.Retry;
using System.Diagnostics;

namespace Repo.Tests.Retry
{
    [TestFixture]
    public class DefaultRetryPolicyTests
    {
        private Mock<ILogger<DefaultRetryPolicy>> _loggerMock = null!;
        private RetryPolicyOptions _defaultOptions = null!;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<DefaultRetryPolicy>>();
            _defaultOptions = new RetryPolicyOptions
            {
                MaxRetryAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(50),
                MaxDelay = TimeSpan.FromSeconds(5),
                UseExponentialBackoff = true,
                UseJitter = false, // Disable jitter for predictable tests
                EnableRetry = true
            };
        }

        private DefaultRetryPolicy CreatePolicy(RetryPolicyOptions? options = null)
        {
            var opts = options ?? _defaultOptions;
            return new DefaultRetryPolicy(Options.Create(opts), _loggerMock.Object);
        }

        #region Success Scenarios

        [Test]
        public async Task ExecuteAsync_WithSuccess_ShouldNotRetry()
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                return Task.FromResult(42);
            }

            // Act
            var result = await policy.ExecuteAsync(Operation);

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ExecuteAsync_Void_WithSuccess_ShouldNotRetry()
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task Operation()
            {
                callCount++;
                return Task.CompletedTask;
            }

            // Act
            await policy.ExecuteAsync(Operation);

            // Assert
            Assert.That(callCount, Is.EqualTo(1));
        }

        #endregion

        #region Transient Exception Scenarios

        [Test]
        [TestCase(-2)]    // Timeout expired
        [TestCase(53)]    // Could not establish connection
        [TestCase(258)]   // Timeout waiting for server response
        [TestCase(4060)]  // Cannot open database
        [TestCase(18456)] // Login failed for user
        [TestCase(40197)] // Azure SQL - service error
        [TestCase(40501)] // Azure SQL - service busy
        [TestCase(40613)] // Azure SQL - database unavailable
        public async Task ExecuteAsync_WithTransientSqlException_ShouldRetry(int errorNumber)
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new SqlExceptionBuilder()
                        .WithError(errorNumber, "Transient error")
                        .Build();
                }
                return Task.FromResult(42);
            }

            // Act
            var result = await policy.ExecuteAsync(Operation);

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(callCount, Is.EqualTo(3));
            VerifyLoggerWarningLogged();
        }

        [Test]
        public async Task ExecuteAsync_WithTimeoutException_ShouldRetry()
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new TimeoutException("Connection timeout");
                }
                return Task.FromResult(42);
            }

            // Act
            var result = await policy.ExecuteAsync(Operation);

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(callCount, Is.EqualTo(3));
            VerifyLoggerWarningLogged();
        }

        [Test]
        public async Task ExecuteAsync_WithOperationCanceledException_ShouldRetry()
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new OperationCanceledException("Operation was cancelled");
                }
                return Task.FromResult(42);
            }

            // Act
            var result = await policy.ExecuteAsync(Operation);

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(callCount, Is.EqualTo(3));
            VerifyLoggerWarningLogged();
        }

        [Test]
        public async Task ExecuteAsync_WithTransientInnerException_ShouldRetry()
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                if (callCount < 3)
                {
                    var inner = new SqlExceptionBuilder()
                        .WithError(-2, "Timeout")
                        .Build();
                    throw new InvalidOperationException("Outer exception", inner);
                }
                return Task.FromResult(42);
            }

            // Act
            var result = await policy.ExecuteAsync(Operation);

            // Assert
            Assert.That(result, Is.EqualTo(42));
            Assert.That(callCount, Is.EqualTo(3));
        }

        #endregion

        #region Non-Transient Exception Scenarios

        [Test]
        public void ExecuteAsync_WithNonTransientException_ShouldNotRetry()
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                throw new InvalidOperationException("Non-transient error");
            }

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await policy.ExecuteAsync(Operation));

            Assert.That(exception.Message, Is.EqualTo("Non-transient error"));
            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void ExecuteAsync_WithNonTransientSqlException_ShouldNotRetry()
        {
            // Arrange
            var policy = CreatePolicy();
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                throw new SqlExceptionBuilder()
                    .WithError(50000, "Custom error") // Non-transient error number
                    .Build();
            }

            // Act & Assert
            Assert.ThrowsAsync<SqlException>(async () => await policy.ExecuteAsync(Operation));
            Assert.That(callCount, Is.EqualTo(1));
        }

        #endregion

        #region Max Retry Scenarios

        [Test]
        public void ExecuteAsync_WithMaxRetriesExceeded_ShouldThrow()
        {
            // Arrange
            var options = new RetryPolicyOptions
            {
                MaxRetryAttempts = 2,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                EnableRetry = true
            };
            var policy = CreatePolicy(options);
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                throw new TimeoutException("Always fails");
            }

            // Act & Assert
            Assert.ThrowsAsync<TimeoutException>(async () => await policy.ExecuteAsync(Operation));
            Assert.That(callCount, Is.EqualTo(3)); // Initial + 2 retries
            VerifyLoggerErrorLogged();
        }

        #endregion

        #region Exponential Backoff Scenarios

        [Test]
        public void ExecuteAsync_WithExponentialBackoff_ShouldIncreaseDelay()
        {
            // Arrange
            var options = new RetryPolicyOptions
            {
                MaxRetryAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromSeconds(10),
                UseExponentialBackoff = true,
                UseJitter = false,
                EnableRetry = true
            };
            var policy = CreatePolicy(options);
            var callCount = 0;
            var delays = new List<TimeSpan>();
            var stopwatch = new Stopwatch();

            Task<int> Operation()
            {
                callCount++;
                if (callCount > 1)
                {
                    stopwatch.Stop();
                    delays.Add(stopwatch.Elapsed);
                }
                
                if (callCount <= 3)
                {
                    stopwatch.Restart();
                    throw new TimeoutException("Transient");
                }
                
                return Task.FromResult(42);
            }

            // Act & Assert
            Assert.ThrowsAsync<TimeoutException>(async () => await policy.ExecuteAsync(Operation));

            // Verify exponential backoff: 100ms, 200ms, 400ms
            Assert.That(delays.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(delays[0], Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(80)), $"First delay was {delays[0]}");
            Assert.That(delays[1], Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(160)), $"Second delay was {delays[1]}");
        }

        [Test]
        public void ExecuteAsync_WithFixedDelay_ShouldNotIncrease()
        {
            // Arrange
            var options = new RetryPolicyOptions
            {
                MaxRetryAttempts = 3,
                InitialDelay = TimeSpan.FromMilliseconds(50),
                UseExponentialBackoff = false,
                UseJitter = false,
                EnableRetry = true
            };
            var policy = CreatePolicy(options);
            var callCount = 0;
            var delays = new List<TimeSpan>();
            var stopwatch = new Stopwatch();

            Task<int> Operation()
            {
                callCount++;
                if (callCount > 1)
                {
                    stopwatch.Stop();
                    delays.Add(stopwatch.Elapsed);
                }
                
                if (callCount <= 3)
                {
                    stopwatch.Restart();
                    throw new TimeoutException("Transient");
                }
                
                return Task.FromResult(42);
            }

            // Act & Assert
            Assert.ThrowsAsync<TimeoutException>(async () => await policy.ExecuteAsync(Operation));

            // Verify fixed delay
            Assert.That(delays.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(delays[0], Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(40)), $"First delay was {delays[0]}");
            Assert.That(delays[1], Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(40)), $"Second delay was {delays[1]}");
        }

        [Test]
        public void ExecuteAsync_WithMaxDelayCap_ShouldNotExceedMax()
        {
            // Arrange
            var options = new RetryPolicyOptions
            {
                MaxRetryAttempts = 5,
                InitialDelay = TimeSpan.FromMilliseconds(100),
                MaxDelay = TimeSpan.FromMilliseconds(150),
                UseExponentialBackoff = true,
                UseJitter = false,
                EnableRetry = true
            };
            var policy = CreatePolicy(options);
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                throw new TimeoutException("Transient");
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            Assert.ThrowsAsync<TimeoutException>(async () => await policy.ExecuteAsync(Operation));
            stopwatch.Stop();

            // Assert - should complete quickly due to max delay cap
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)), 
                $"Should not take too long with max delay cap. Elapsed: {stopwatch.Elapsed}");
        }

        #endregion

        #region Disable Retry Scenarios

        [Test]
        public void ExecuteAsync_WithRetryDisabled_ShouldNotRetry()
        {
            // Arrange
            var options = new RetryPolicyOptions
            {
                EnableRetry = false,
                MaxRetryAttempts = 3
            };
            var policy = CreatePolicy(options);
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                throw new TimeoutException("Transient");
            }

            // Act & Assert
            Assert.ThrowsAsync<TimeoutException>(async () => await policy.ExecuteAsync(Operation));
            Assert.That(callCount, Is.EqualTo(1));
        }

        #endregion

        #region Constructor Overload Tests

        [Test]
        public void ExecuteAsync_WithExplicitOptions_ShouldUseOptions()
        {
            // Arrange
            var options = new RetryPolicyOptions
            {
                MaxRetryAttempts = 1,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                EnableRetry = true
            };
            var policy = new DefaultRetryPolicy(options, _loggerMock.Object);
            var callCount = 0;

            Task<int> Operation()
            {
                callCount++;
                throw new TimeoutException("Transient");
            }

            // Act & Assert
            Assert.ThrowsAsync<TimeoutException>(async () => await policy.ExecuteAsync(Operation));
            Assert.That(callCount, Is.EqualTo(2)); // Initial + 1 retry
        }

        #endregion

        #region Helper Methods

        private void VerifyLoggerWarningLogged()
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Transient error detected")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        private void VerifyLoggerErrorLogged()
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region SqlException Builder Helper

        /// <summary>
        /// Helper class to build SqlException instances for testing using reflection
        /// since SqlError and SqlErrorCollection don't have public constructors.
        /// </summary>
        private class SqlExceptionBuilder
        {
            private int _errorNumber = -1;
            private string _errorMessage = "Test error";

            public SqlExceptionBuilder WithError(int number, string message)
            {
                _errorNumber = number;
                _errorMessage = message;
                return this;
            }

            public SqlException Build()
            {
                // Create SqlError using reflection (constructor is internal)
                var sqlErrorType = typeof(SqlError);
                var sqlErrorConstructor = sqlErrorType.GetConstructor(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null,
                    new[] { typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(uint) },
                    null);
                
                var error = (SqlError)sqlErrorConstructor!.Invoke(new object[] { _errorNumber, (byte)1, (byte)1, "Server", _errorMessage, "Proc", 0, (uint)0 });

                // Create SqlErrorCollection using reflection (constructor is internal)
                var errorCollectionType = typeof(SqlErrorCollection);
                var errorCollectionConstructor = errorCollectionType.GetConstructor(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null,
                    Type.EmptyTypes,
                    null);
                
                var errorCollection = (SqlErrorCollection)errorCollectionConstructor!.Invoke(null);

                // Add error to collection using reflection (Add method is internal)
                var addMethod = errorCollectionType.GetMethod("Add", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                addMethod!.Invoke(errorCollection, new object[] { error });

                // Create SqlException using reflection
                var exceptionConstructor = typeof(SqlException).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid) },
                    null)!;
                
                var exception = (SqlException)exceptionConstructor.Invoke(new object[] { _errorMessage, errorCollection, null!, Guid.NewGuid() });
                
                return exception;
            }
        }

        #endregion
    }
}
