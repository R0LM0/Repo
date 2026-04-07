using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Repo.Repository.Retry;
using System.Diagnostics;

namespace Repo.Tests.Retry
{
    public class DefaultRetryPolicyTests
    {
        private readonly Mock<ILogger<DefaultRetryPolicy>> _loggerMock;
        private readonly RetryPolicyOptions _defaultOptions;

        public DefaultRetryPolicyTests()
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

        [Fact]
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
            Assert.Equal(42, result);
            Assert.Equal(1, callCount);
        }

        [Fact]
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
            Assert.Equal(1, callCount);
        }

        #endregion

        #region Transient Exception Scenarios

        [Theory]
        [InlineData(-2)]    // Timeout expired
        [InlineData(53)]    // Could not establish connection
        [InlineData(258)]   // Timeout waiting for server response
        [InlineData(4060)]  // Cannot open database
        [InlineData(18456)] // Login failed for user
        [InlineData(40197)] // Azure SQL - service error
        [InlineData(40501)] // Azure SQL - service busy
        [InlineData(40613)] // Azure SQL - database unavailable
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
            Assert.Equal(42, result);
            Assert.Equal(3, callCount);
            VerifyLoggerWarningLogged();
        }

        [Fact]
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
            Assert.Equal(42, result);
            Assert.Equal(3, callCount);
            VerifyLoggerWarningLogged();
        }

        [Fact]
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
            Assert.Equal(42, result);
            Assert.Equal(3, callCount);
            VerifyLoggerWarningLogged();
        }

        [Fact]
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
            Assert.Equal(42, result);
            Assert.Equal(3, callCount);
        }

        #endregion

        #region Non-Transient Exception Scenarios

        [Fact]
        public async Task ExecuteAsync_WithNonTransientException_ShouldNotRetry()
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
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => policy.ExecuteAsync(Operation));

            Assert.Equal("Non-transient error", exception.Message);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task ExecuteAsync_WithNonTransientSqlException_ShouldNotRetry()
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
            await Assert.ThrowsAsync<SqlException>(() => policy.ExecuteAsync(Operation));
            Assert.Equal(1, callCount);
        }

        #endregion

        #region Max Retry Scenarios

        [Fact]
        public async Task ExecuteAsync_WithMaxRetriesExceeded_ShouldThrow()
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
            await Assert.ThrowsAsync<TimeoutException>(() => policy.ExecuteAsync(Operation));
            Assert.Equal(3, callCount); // Initial + 2 retries
            VerifyLoggerErrorLogged();
        }

        #endregion

        #region Exponential Backoff Scenarios

        [Fact]
        public async Task ExecuteAsync_WithExponentialBackoff_ShouldIncreaseDelay()
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
            await Assert.ThrowsAsync<TimeoutException>(() => policy.ExecuteAsync(Operation));

            // Verify exponential backoff: 100ms, 200ms, 400ms
            Assert.True(delays.Count >= 2);
            Assert.True(delays[0] >= TimeSpan.FromMilliseconds(80), $"First delay was {delays[0]}");
            Assert.True(delays[1] >= TimeSpan.FromMilliseconds(160), $"Second delay was {delays[1]}");
        }

        [Fact]
        public async Task ExecuteAsync_WithFixedDelay_ShouldNotIncrease()
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
            await Assert.ThrowsAsync<TimeoutException>(() => policy.ExecuteAsync(Operation));

            // Verify fixed delay
            Assert.True(delays.Count >= 2);
            Assert.True(delays[0] >= TimeSpan.FromMilliseconds(40), $"First delay was {delays[0]}");
            Assert.True(delays[1] >= TimeSpan.FromMilliseconds(40), $"Second delay was {delays[1]}");
        }

        [Fact]
        public async Task ExecuteAsync_WithMaxDelayCap_ShouldNotExceedMax()
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
            await Assert.ThrowsAsync<TimeoutException>(() => policy.ExecuteAsync(Operation));
            stopwatch.Stop();

            // Assert - should complete quickly due to max delay cap
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), 
                $"Should not take too long with max delay cap. Elapsed: {stopwatch.Elapsed}");
        }

        #endregion

        #region Disable Retry Scenarios

        [Fact]
        public async Task ExecuteAsync_WithRetryDisabled_ShouldNotRetry()
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
            await Assert.ThrowsAsync<TimeoutException>(() => policy.ExecuteAsync(Operation));
            Assert.Equal(1, callCount);
        }

        #endregion

        #region Constructor Overload Tests

        [Fact]
        public async Task ExecuteAsync_WithExplicitOptions_ShouldUseOptions()
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
            await Assert.ThrowsAsync<TimeoutException>(() => policy.ExecuteAsync(Operation));
            Assert.Equal(2, callCount); // Initial + 1 retry
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
        /// Helper class to build SqlException instances for testing
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
                // SqlException has a private constructor, so we use reflection
                var errorCollection = CreateErrorCollection();
                var exception = typeof(SqlException).GetMethod(
                    "CreateSqlException",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                    null,
                    new[] { typeof(SqlErrorCollection), typeof(string), typeof(Guid) },
                    null);

                if (exception != null)
                {
                    return (SqlException)exception.Invoke(null, new object[] { errorCollection, "Server", Guid.NewGuid() })!;
                }

                // Fallback: throw standard SqlException
                throw new SqlException(_errorMessage, new Exception("Inner"));
            }

            private SqlErrorCollection CreateErrorCollection()
            {
                // Create a SqlErrorCollection using reflection
                var collection = typeof(SqlErrorCollection).GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null)?.Invoke(null) as SqlErrorCollection;

                if (collection == null)
                {
                    throw new InvalidOperationException("Failed to create SqlErrorCollection");
                }

                // Add error to collection using reflection
                var addMethod = typeof(SqlErrorCollection).GetMethod(
                    "Add",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (addMethod != null)
                {
                    var error = CreateSqlError();
                    addMethod.Invoke(collection, new object[] { error });
                }

                return collection;
            }

            private object CreateSqlError()
            {
                // Create SqlError using reflection
                var errorType = typeof(SqlError);
                var constructor = errorType.GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null,
                    new[] { typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(uint), typeof(Exception) },
                    null);

                if (constructor != null)
                {
                    return constructor.Invoke(new object[]
                    {
                        _errorNumber, // errorCode
                        (byte)0,      // errorClass
                        (byte)0,      // state
                        "Server",     // server
                        _errorMessage, // message
                        "Procedure",  // procedure
                        0,            // lineNumber
                        (uint)0,      // win32ErrorCode
                        null          // exception
                    });
                }

                throw new InvalidOperationException("Failed to create SqlError");
            }
        }

        #endregion
    }
}
