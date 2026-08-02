//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Resiliency;

[Trait("Category", "Unit")]
public class ResiliencyTest
{
    #region [ MongoDbResiliencyOptions - Defaults ]

    [Fact]
    public void MongoDbResiliencyOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbResiliencyOptions();

        Assert.True(opts.EnableAutoReconnect);
        Assert.Equal(5, opts.MaxReconnectAttempts);
        Assert.Equal(1000, opts.ReconnectDelayMilliseconds);
        Assert.Equal(30000, opts.MaxReconnectDelayMilliseconds);
        Assert.True(opts.UseExponentialBackoffForReconnect);
        Assert.Equal(0.2, opts.ReconnectJitterFactor);

        Assert.True(opts.EnableRetry);
        Assert.Equal(3, opts.RetryCount);
        Assert.Equal(100, opts.RetryBaseDelayMilliseconds);
        Assert.Equal(5000, opts.RetryMaxDelayMilliseconds);
        Assert.True(opts.UseExponentialBackoff);
        Assert.Equal(0.2, opts.RetryJitterFactor);
        Assert.Empty(opts.AdditionalRetryableExceptions);
        Assert.Empty(opts.NonRetryableExceptions);

        Assert.True(opts.EnableCircuitBreaker);
        Assert.Equal(5, opts.CircuitBreakerFailureThreshold);
        Assert.Equal(60, opts.CircuitBreakerSamplingDurationSeconds);
        Assert.Equal(30, opts.CircuitBreakerDurationSeconds);
        Assert.Equal(10, opts.CircuitBreakerMinimumThroughput);
        Assert.Null(opts.CircuitBreakerFailureRateThreshold);
        Assert.True(opts.TrackCircuitBreakerMetrics);

        Assert.True(opts.EnableOperationTimeout);
        Assert.Equal(30, opts.DefaultOperationTimeoutSeconds);
        Assert.Null(opts.ReadOperationTimeoutSeconds);
        Assert.Null(opts.WriteOperationTimeoutSeconds);
        Assert.Equal(120, opts.BulkOperationTimeoutSeconds);

        Assert.True(opts.EnableAutomaticFailover);
        Assert.Equal(30, opts.ServerSelectionTimeoutSeconds);
        Assert.Equal(10, opts.HeartbeatFrequencySeconds);
        Assert.True(opts.EnableServerMonitoring);
        Assert.True(opts.AllowReadsWithoutPrimary);

        Assert.True(opts.LogRetryAttempts);
        Assert.True(opts.LogCircuitBreakerStateChanges);
        Assert.True(opts.LogConnectionEvents);
        Assert.True(opts.LogTimeoutEvents);
    }

    [Fact]
    public void MongoDbResiliencyOptions_IsSealed()
    {
        Assert.True(typeof(MongoDbResiliencyOptions).IsSealed);
    }

    #endregion

    #region [ MongoDbResiliencyOptions - Timeout Helpers ]

    [Fact]
    public void GetReadTimeout_UsesDefaultTimeout_WhenReadTimeoutNotSet()
    {
        var opts = new MongoDbResiliencyOptions { DefaultOperationTimeoutSeconds = 45 };
        Assert.Equal(TimeSpan.FromSeconds(45), opts.GetReadTimeout());
    }

    [Fact]
    public void GetReadTimeout_UsesReadTimeout_WhenSet()
    {
        var opts = new MongoDbResiliencyOptions
        {
            DefaultOperationTimeoutSeconds = 30,
            ReadOperationTimeoutSeconds = 15
        };
        Assert.Equal(TimeSpan.FromSeconds(15), opts.GetReadTimeout());
    }

    [Fact]
    public void GetReadTimeout_ReturnsMaxValue_WhenTimeoutIsZero()
    {
        var opts = new MongoDbResiliencyOptions { DefaultOperationTimeoutSeconds = 0 };
        Assert.Equal(TimeSpan.MaxValue, opts.GetReadTimeout());
    }

    [Fact]
    public void GetWriteTimeout_UsesDefaultTimeout_WhenWriteTimeoutNotSet()
    {
        var opts = new MongoDbResiliencyOptions { DefaultOperationTimeoutSeconds = 60 };
        Assert.Equal(TimeSpan.FromSeconds(60), opts.GetWriteTimeout());
    }

    [Fact]
    public void GetWriteTimeout_UsesWriteTimeout_WhenSet()
    {
        var opts = new MongoDbResiliencyOptions
        {
            DefaultOperationTimeoutSeconds = 30,
            WriteOperationTimeoutSeconds = 90
        };
        Assert.Equal(TimeSpan.FromSeconds(90), opts.GetWriteTimeout());
    }

    [Fact]
    public void GetWriteTimeout_ReturnsMaxValue_WhenTimeoutIsZero()
    {
        var opts = new MongoDbResiliencyOptions { DefaultOperationTimeoutSeconds = 0 };
        Assert.Equal(TimeSpan.MaxValue, opts.GetWriteTimeout());
    }

    [Fact]
    public void GetBulkOperationTimeout_ReturnsBulkTimeout()
    {
        var opts = new MongoDbResiliencyOptions { BulkOperationTimeoutSeconds = 300 };
        Assert.Equal(TimeSpan.FromSeconds(300), opts.GetBulkOperationTimeout());
    }

    [Fact]
    public void GetBulkOperationTimeout_ReturnsMaxValue_WhenZero()
    {
        var opts = new MongoDbResiliencyOptions { BulkOperationTimeoutSeconds = 0 };
        Assert.Equal(TimeSpan.MaxValue, opts.GetBulkOperationTimeout());
    }

    #endregion

    #region [ MongoDbResiliencyOptions - Factory Methods ]

    [Fact]
    public void CreateProduction_ReturnsNonNullOptions()
    {
        var opts = MongoDbResiliencyOptions.CreateProduction();
        Assert.NotNull(opts);
    }

    [Fact]
    public void CreateProduction_HasCorrectValues()
    {
        var opts = MongoDbResiliencyOptions.CreateProduction();

        Assert.Equal(10, opts.MaxReconnectAttempts);
        Assert.Equal(3, opts.RetryCount);
        Assert.True(opts.EnableCircuitBreaker);
        Assert.Equal(30, opts.DefaultOperationTimeoutSeconds);
        Assert.Equal(300, opts.BulkOperationTimeoutSeconds);
        Assert.True(opts.EnableAutomaticFailover);
    }

    [Fact]
    public void CreateDevelopment_ReturnsNonNullOptions()
    {
        var opts = MongoDbResiliencyOptions.CreateDevelopment();
        Assert.NotNull(opts);
    }

    [Fact]
    public void CreateDevelopment_DisablesCircuitBreaker()
    {
        var opts = MongoDbResiliencyOptions.CreateDevelopment();
        Assert.False(opts.EnableCircuitBreaker);
    }

    [Fact]
    public void CreateDevelopment_HasLongerTimeout_ThanProduction()
    {
        var dev = MongoDbResiliencyOptions.CreateDevelopment();
        var prod = MongoDbResiliencyOptions.CreateProduction();

        Assert.True(dev.DefaultOperationTimeoutSeconds >= prod.DefaultOperationTimeoutSeconds);
    }

    [Fact]
    public void CreateDevelopment_HasFewerRetries_ThanDefault()
    {
        var dev = MongoDbResiliencyOptions.CreateDevelopment();
        Assert.True(dev.RetryCount < 3);
    }

    #endregion

    #region [ MongoDbResiliencyException ]

    [Fact]
    public void MongoDbResiliencyException_DefaultConstructor_HasDefaultMessage()
    {
        var ex = new MongoDbResiliencyException();
        Assert.NotEmpty(ex.Message);
        Assert.Equal("MONGODB_RESILIENCY_ERROR", ex.ErrorCode);
    }

    [Fact]
    public void MongoDbResiliencyException_MessageConstructor_SetsMessage()
    {
        var ex = new MongoDbResiliencyException("Custom error");
        Assert.Equal("Custom error", ex.Message);
        Assert.Equal("MONGODB_RESILIENCY_ERROR", ex.ErrorCode);
    }

    [Fact]
    public void MongoDbResiliencyException_MessageAndCodeConstructor_SetsBoth()
    {
        var ex = new MongoDbResiliencyException("Error", "MY_CODE");
        Assert.Equal("Error", ex.Message);
        Assert.Equal("MY_CODE", ex.ErrorCode);
    }

    [Fact]
    public void MongoDbResiliencyException_WithInnerException_WrapsIt()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new MongoDbResiliencyException("outer", inner);
        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void MongoDbResiliencyException_IsException()
    {
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(MongoDbResiliencyException)));
    }

    #endregion

    #region [ MongoDbCircuitBreakerOpenException ]

    [Fact]
    public void MongoDbCircuitBreakerOpenException_DefaultConstructor_SetsDefaults()
    {
        var ex = new MongoDbCircuitBreakerOpenException();

        Assert.Equal("MONGODB_CIRCUIT_BREAKER_OPEN", ex.ErrorCode);
        Assert.Null(ex.RemainingDuration);
        Assert.True(ex.OpenedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MongoDbCircuitBreakerOpenException_WithDuration_SetsDuration()
    {
        var remaining = TimeSpan.FromSeconds(15);
        var ex = new MongoDbCircuitBreakerOpenException(remaining);

        Assert.Equal(remaining, ex.RemainingDuration);
        Assert.Contains("15", ex.Message);
    }

    [Fact]
    public void MongoDbCircuitBreakerOpenException_WithAllParams_SetsAll()
    {
        var remaining = TimeSpan.FromSeconds(10);
        DateTimeOffset openedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var ex = new MongoDbCircuitBreakerOpenException("Circuit open", remaining, openedAt);

        Assert.Equal("Circuit open", ex.Message);
        Assert.Equal(remaining, ex.RemainingDuration);
        Assert.Equal(openedAt, ex.OpenedAt);
    }

    [Fact]
    public void MongoDbCircuitBreakerOpenException_WithInnerException_WrapsIt()
    {
        var inner = new Exception("cause");
        var ex = new MongoDbCircuitBreakerOpenException("open", inner);

        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void MongoDbCircuitBreakerOpenException_IsMongoDbResiliencyException()
    {
        Assert.True(typeof(MongoDbResiliencyException).IsAssignableFrom(typeof(MongoDbCircuitBreakerOpenException)));
    }

    #endregion

    #region [ MongoDbOperationTimeoutException ]

    [Fact]
    public void MongoDbOperationTimeoutException_DefaultConstructor_SetsCode()
    {
        var ex = new MongoDbOperationTimeoutException();
        Assert.Equal("MONGODB_OPERATION_TIMEOUT", ex.ErrorCode);
    }

    [Fact]
    public void MongoDbOperationTimeoutException_WithTimeout_SetsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var ex = new MongoDbOperationTimeoutException(timeout);

        Assert.Equal(timeout, ex.Timeout);
        Assert.Contains("30", ex.Message);
    }

    [Fact]
    public void MongoDbOperationTimeoutException_WithTimeoutAndType_SetsAll()
    {
        var timeout = TimeSpan.FromSeconds(10);
        var ex = new MongoDbOperationTimeoutException(timeout, "read");

        Assert.Equal(timeout, ex.Timeout);
        Assert.Equal("read", ex.OperationType);
        Assert.Contains("read", ex.Message);
    }

    [Fact]
    public void MongoDbOperationTimeoutException_WithInnerException_WrapsIt()
    {
        var inner = new TimeoutException("timeout");
        var timeout = TimeSpan.FromSeconds(5);
        var ex = new MongoDbOperationTimeoutException("timed out", timeout, inner);

        Assert.Equal(inner, ex.InnerException);
        Assert.Equal(timeout, ex.Timeout);
    }

    #endregion

    #region [ MongoDbRetryExhaustedException ]

    [Fact]
    public void MongoDbRetryExhaustedException_DefaultConstructor_SetsCode()
    {
        var ex = new MongoDbRetryExhaustedException();
        Assert.Equal("MONGODB_RETRY_EXHAUSTED", ex.ErrorCode);
    }

    [Fact]
    public void MongoDbRetryExhaustedException_WithCount_SetsCount()
    {
        var ex = new MongoDbRetryExhaustedException(3);
        Assert.Equal(3, ex.RetryCount);
        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public void MongoDbRetryExhaustedException_WithAllParams_SetsAll()
    {
        var duration = TimeSpan.FromSeconds(15);
        var inner = new Exception("last error");
        var ex = new MongoDbRetryExhaustedException(5, duration, inner);

        Assert.Equal(5, ex.RetryCount);
        Assert.Equal(duration, ex.TotalRetryDuration);
        Assert.Equal(inner, ex.InnerException);
    }

    #endregion

    #region [ MongoDbConnectionRecoveryException ]

    [Fact]
    public void MongoDbConnectionRecoveryException_DefaultConstructor_SetsCode()
    {
        var ex = new MongoDbConnectionRecoveryException();
        Assert.Equal("MONGODB_CONNECTION_RECOVERY_FAILED", ex.ErrorCode);
    }

    [Fact]
    public void MongoDbConnectionRecoveryException_WithAttempts_SetsAttempts()
    {
        var ex = new MongoDbConnectionRecoveryException(5);
        Assert.Equal(5, ex.ReconnectAttempts);
        Assert.Contains("5", ex.Message);
    }

    [Fact]
    public void MongoDbConnectionRecoveryException_WithAllParams_SetsAll()
    {
        var duration = TimeSpan.FromSeconds(60);
        var inner = new Exception("network error");
        var ex = new MongoDbConnectionRecoveryException(10, duration, inner);

        Assert.Equal(10, ex.ReconnectAttempts);
        Assert.Equal(duration, ex.TotalReconnectDuration);
        Assert.Equal(inner, ex.InnerException);
    }

    #endregion

    #region [ MongoDbFailoverException ]

    [Fact]
    public void MongoDbFailoverException_DefaultConstructor_SetsCode()
    {
        var ex = new MongoDbFailoverException();
        Assert.Equal("MONGODB_FAILOVER_FAILED", ex.ErrorCode);
    }

    [Fact]
    public void MongoDbFailoverException_WithTimeout_SetsTimeout()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var ex = new MongoDbFailoverException(timeout);

        Assert.Equal(timeout, ex.ServerSelectionTimeout);
        Assert.Contains("30", ex.Message);
    }

    [Fact]
    public void MongoDbFailoverException_WithInnerException_WrapsIt()
    {
        var inner = new Exception("cause");
        var ex = new MongoDbFailoverException("failover failed", inner);

        Assert.Equal(inner, ex.InnerException);
    }

    [Fact]
    public void MongoDbFailoverException_IsMongoDbResiliencyException()
    {
        Assert.True(typeof(MongoDbResiliencyException).IsAssignableFrom(typeof(MongoDbFailoverException)));
    }

    #endregion
}
