//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.Extensions;
using Mvp24Hours.Core.Infrastructure.RateLimiting;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Rate Limiting functionality using System.Threading.RateLimiting.
/// </summary>
[Trait("Category", "Unit")]
public class RateLimitingTest : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IRateLimiterProvider _rateLimiterProvider;

    public RateLimitingTest()
    {
        var services = new ServiceCollection();
        services.AddNativeRateLimiting();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
        _rateLimiterProvider = _serviceProvider.GetRequiredService<IRateLimiterProvider>();
    }

    #region NativeRateLimiterProvider Tests

    [Fact]
    public void GetRateLimiter_WithValidKey_ReturnsRateLimiter()
    {
        // Arrange
        string key = "test_key";
        var options = NativeRateLimiterOptions.SlidingWindow(100);

        // Act
        RateLimiter limiter = _rateLimiterProvider.GetRateLimiter(key, options);

        // Assert
        limiter.Should().NotBeNull();
    }

    [Fact]
    public void GetRateLimiter_WithSameKey_ReturnsSameInstance()
    {
        // Arrange
        string key = "same_key";
        var options = NativeRateLimiterOptions.SlidingWindow(100);

        // Act
        RateLimiter limiter1 = _rateLimiterProvider.GetRateLimiter(key, options);
        RateLimiter limiter2 = _rateLimiterProvider.GetRateLimiter(key, options);

        // Assert
        limiter1.Should().BeSameAs(limiter2);
    }

    [Fact]
    public void GetRateLimiter_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? key = null;
        var options = NativeRateLimiterOptions.SlidingWindow(100);

        // Act
        Func<RateLimiter> act = () => _rateLimiterProvider.GetRateLimiter(key!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetRateLimiter_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        string key = "test_key";
        NativeRateLimiterOptions? options = null;

        // Act
        Func<RateLimiter> act = () => _rateLimiterProvider.GetRateLimiter(key, options!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AcquireAsync_WithAvailablePermits_ReturnsAcquiredLease()
    {
        // Arrange
        string key = "acquire_test";
        var options = NativeRateLimiterOptions.SlidingWindow(permitLimit: 10, window: TimeSpan.FromSeconds(1));

        // Act
        RateLimitLease lease = await _rateLimiterProvider.AcquireAsync(key, options, 1);

        // Assert
        lease.IsAcquired.Should().BeTrue();
        lease.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_WithExceededPermits_ReturnsRejectedLease()
    {
        // Arrange
        string key = "exceed_test";
        var options = NativeRateLimiterOptions.SlidingWindow(permitLimit: 2, window: TimeSpan.FromSeconds(1));

        // Act - Acquire all permits
        RateLimitLease lease1 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease2 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease3 = await _rateLimiterProvider.AcquireAsync(key, options, 1);

        // Assert
        lease1.IsAcquired.Should().BeTrue();
        lease2.IsAcquired.Should().BeTrue();
        lease3.IsAcquired.Should().BeFalse();

        lease1.Dispose();
        lease2.Dispose();
        lease3.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_WithRejectedLease_MayHaveRetryAfterMetadata()
    {
        // Arrange
        string key = "retry_after_test";
        var options = NativeRateLimiterOptions.SlidingWindow(permitLimit: 1, window: TimeSpan.FromSeconds(1));

        // Act
        RateLimitLease lease1 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease2 = await _rateLimiterProvider.AcquireAsync(key, options, 1);

        // Assert
        lease1.IsAcquired.Should().BeTrue();
        lease2.IsAcquired.Should().BeFalse();

        // Some rate limiters may not provide RetryAfter metadata
        // This is acceptable behavior - we just verify the lease is rejected
        if (lease2.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            retryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        }

        lease1.Dispose();
        lease2.Dispose();
    }

    [Fact]
    public void TryRemoveRateLimiter_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        string key = "remove_test";
        var options = NativeRateLimiterOptions.SlidingWindow(100);
        _rateLimiterProvider.GetRateLimiter(key, options);

        // Act
        bool result = _rateLimiterProvider.TryRemoveRateLimiter(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TryRemoveRateLimiter_WithNonExistentKey_ReturnsFalse()
    {
        // Arrange
        string key = "non_existent";

        // Act
        bool result = _rateLimiterProvider.TryRemoveRateLimiter(key);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region NativeRateLimiterOptions Factory Methods Tests

    [Fact]
    public void FixedWindow_WithDefaultValues_CreatesCorrectOptions()
    {
        // Act
        var options = NativeRateLimiterOptions.FixedWindow();

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.FixedWindow);
        options.PermitLimit.Should().Be(100);
        options.Window.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void FixedWindow_WithCustomValues_CreatesCorrectOptions()
    {
        // Arrange
        int permitLimit = 50;
        var window = TimeSpan.FromSeconds(30);

        // Act
        var options = NativeRateLimiterOptions.FixedWindow(permitLimit, window);

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.FixedWindow);
        options.PermitLimit.Should().Be(permitLimit);
        options.Window.Should().Be(window);
    }

    [Fact]
    public void SlidingWindow_WithDefaultValues_CreatesCorrectOptions()
    {
        // Act
        var options = NativeRateLimiterOptions.SlidingWindow();

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.SlidingWindow);
        options.PermitLimit.Should().Be(100);
        options.Window.Should().Be(TimeSpan.FromMinutes(1));
        options.SegmentsPerWindow.Should().Be(4);
    }

    [Fact]
    public void SlidingWindow_WithCustomValues_CreatesCorrectOptions()
    {
        // Arrange
        int permitLimit = 200;
        var window = TimeSpan.FromSeconds(60);
        int segmentsPerWindow = 8;

        // Act
        var options = NativeRateLimiterOptions.SlidingWindow(permitLimit, window, segmentsPerWindow);

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.SlidingWindow);
        options.PermitLimit.Should().Be(permitLimit);
        options.Window.Should().Be(window);
        options.SegmentsPerWindow.Should().Be(segmentsPerWindow);
    }

    [Fact]
    public void TokenBucket_WithDefaultValues_CreatesCorrectOptions()
    {
        // Act
        var options = NativeRateLimiterOptions.TokenBucket();

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.TokenBucket);
        options.PermitLimit.Should().Be(100);
        options.ReplenishmentPeriod.Should().Be(TimeSpan.FromSeconds(10));
        options.TokensPerPeriod.Should().Be(10);
    }

    [Fact]
    public void TokenBucket_WithCustomValues_CreatesCorrectOptions()
    {
        // Arrange
        int tokenLimit = 50;
        var replenishmentPeriod = TimeSpan.FromSeconds(5);
        int tokensPerPeriod = 5;

        // Act
        var options = NativeRateLimiterOptions.TokenBucket(tokenLimit, replenishmentPeriod, tokensPerPeriod);

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.TokenBucket);
        options.PermitLimit.Should().Be(tokenLimit);
        options.ReplenishmentPeriod.Should().Be(replenishmentPeriod);
        options.TokensPerPeriod.Should().Be(tokensPerPeriod);
    }

    [Fact]
    public void Concurrency_WithDefaultValues_CreatesCorrectOptions()
    {
        // Act
        var options = NativeRateLimiterOptions.Concurrency();

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.Concurrency);
        options.PermitLimit.Should().Be(10);
        options.QueueLimit.Should().Be(0);
    }

    [Fact]
    public void Concurrency_WithCustomValues_CreatesCorrectOptions()
    {
        // Arrange
        int permitLimit = 5;
        int queueLimit = 10;

        // Act
        var options = NativeRateLimiterOptions.Concurrency(permitLimit, queueLimit);

        // Assert
        options.Algorithm.Should().Be(RateLimitingAlgorithm.Concurrency);
        options.PermitLimit.Should().Be(permitLimit);
        options.QueueLimit.Should().Be(queueLimit);
    }

    #endregion

    #region RateLimitExceededException Tests

    [Fact]
    public void RateLimitExceededException_WithDefaultConstructor_HasDefaultMessage()
    {
        // Act
        var exception = new RateLimitExceededException();

        // Assert
        exception.Message.Should().Contain("Rate limit exceeded");
        exception.ErrorCode.Should().Be(RateLimitExceededException.DefaultErrorCode);
        exception.RateLimiterKey.Should().Be("default");
    }

    [Fact]
    public void RateLimitExceededException_WithMessage_HasCorrectMessage()
    {
        // Arrange
        string message = "Custom rate limit message";

        // Act
        var exception = new RateLimitExceededException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(RateLimitExceededException.DefaultErrorCode);
    }

    [Fact]
    public void RateLimitExceededException_WithDetails_HasAllProperties()
    {
        // Arrange
        string message = "Rate limit exceeded";
        string key = "test_key";
        var retryAfter = TimeSpan.FromSeconds(30);
        int permitLimit = 100;
        string errorCode = "CUSTOM_ERROR";

        // Act
        var exception = new RateLimitExceededException(message, key, retryAfter, permitLimit, errorCode);

        // Assert
        exception.Message.Should().Be(message);
        exception.RateLimiterKey.Should().Be(key);
        exception.RetryAfter.Should().Be(retryAfter);
        exception.PermitLimit.Should().Be(permitLimit);
        exception.ErrorCode.Should().Be(errorCode);
    }

    [Fact]
    public void RateLimitExceededException_ForKey_WithRetryAfter_IncludesRetryAfterInMessage()
    {
        // Arrange
        string key = "test_key";
        var retryAfter = TimeSpan.FromSeconds(45);

        // Act
        var exception = RateLimitExceededException.ForKey(key, retryAfter);

        // Assert
        exception.RateLimiterKey.Should().Be(key);
        exception.RetryAfter.Should().Be(retryAfter);
        exception.Message.Should().Contain(key);
        exception.Message.Should().Contain("45");
    }

    [Fact]
    public void RateLimitExceededException_ForKey_WithPermitLimit_IncludesPermitLimit()
    {
        // Arrange
        string key = "test_key";
        int permitLimit = 50;

        // Act
        var exception = RateLimitExceededException.ForKey(key, null, permitLimit);

        // Assert
        exception.RateLimiterKey.Should().Be(key);
        exception.PermitLimit.Should().Be(permitLimit);
    }

    #endregion

    #region Service Extensions Tests

    [Fact]
    public void AddNativeRateLimiting_RegistersProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNativeRateLimiting();
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IRateLimiterProvider? rateLimiterProvider = provider.GetRequiredService<IRateLimiterProvider>();
        rateLimiterProvider.Should().NotBeNull();
        rateLimiterProvider.Should().BeOfType<NativeRateLimiterProvider>();
    }

    [Fact]
    public void AddSlidingWindowRateLimiter_RegistersRateLimiter()
    {
        // Arrange
        var services = new ServiceCollection();
        string key = "sliding_window_test";

        // Act
        services.AddSlidingWindowRateLimiter(key, permitLimit: 200);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IEnumerable<RateLimiterRegistration> registrations = provider.GetServices<RateLimiterRegistration>();
        registrations.Should().Contain(r => r.Key == key);
    }

    [Fact]
    public void AddFixedWindowRateLimiter_RegistersRateLimiter()
    {
        // Arrange
        var services = new ServiceCollection();
        string key = "fixed_window_test";

        // Act
        services.AddFixedWindowRateLimiter(key, permitLimit: 150);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IEnumerable<RateLimiterRegistration> registrations = provider.GetServices<RateLimiterRegistration>();
        registrations.Should().Contain(r => r.Key == key);
    }

    [Fact]
    public void AddTokenBucketRateLimiter_RegistersRateLimiter()
    {
        // Arrange
        var services = new ServiceCollection();
        string key = "token_bucket_test";

        // Act
        services.AddTokenBucketRateLimiter(key, tokenLimit: 75);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IEnumerable<RateLimiterRegistration> registrations = provider.GetServices<RateLimiterRegistration>();
        registrations.Should().Contain(r => r.Key == key);
    }

    [Fact]
    public void AddConcurrencyRateLimiter_RegistersRateLimiter()
    {
        // Arrange
        var services = new ServiceCollection();
        string key = "concurrency_test";

        // Act
        services.AddConcurrencyRateLimiter(key, permitLimit: 5);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IEnumerable<RateLimiterRegistration> registrations = provider.GetServices<RateLimiterRegistration>();
        registrations.Should().Contain(r => r.Key == key);
    }

    #endregion

    #region Algorithm-Specific Tests

    [Fact]
    public async Task FixedWindowRateLimiter_ResetsAfterWindow()
    {
        // Arrange
        string key = "fixed_window_reset";
        var options = NativeRateLimiterOptions.FixedWindow(
            permitLimit: 2,
            window: TimeSpan.FromMilliseconds(100));

        // Act - Acquire all permits
        RateLimitLease lease1 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease2 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease3 = await _rateLimiterProvider.AcquireAsync(key, options, 1);

        lease1.IsAcquired.Should().BeTrue();
        lease2.IsAcquired.Should().BeTrue();
        lease3.IsAcquired.Should().BeFalse();

        lease1.Dispose();
        lease2.Dispose();
        lease3.Dispose();

        // Wait for window to reset
        await Task.Delay(150);

        // Act - Try again after window reset
        RateLimitLease lease4 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease5 = await _rateLimiterProvider.AcquireAsync(key, options, 1);

        // Assert
        lease4.IsAcquired.Should().BeTrue();
        lease5.IsAcquired.Should().BeTrue();

        lease4.Dispose();
        lease5.Dispose();
    }

    [Fact]
    public async Task ConcurrencyLimiter_LimitsConcurrentOperations()
    {
        // Arrange
        string key = "concurrency_limit";
        var options = NativeRateLimiterOptions.Concurrency(permitLimit: 2);

        // Act - Try to acquire 3 permits concurrently
        RateLimitLease lease1 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease2 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        RateLimitLease lease3 = await _rateLimiterProvider.AcquireAsync(key, options, 1);

        // Assert
        lease1.IsAcquired.Should().BeTrue();
        lease2.IsAcquired.Should().BeTrue();
        lease3.IsAcquired.Should().BeFalse();

        lease1.Dispose();
        lease2.Dispose();
        lease3.Dispose();

        // After releasing, should be able to acquire again
        RateLimitLease lease4 = await _rateLimiterProvider.AcquireAsync(key, options, 1);
        lease4.IsAcquired.Should().BeTrue();
        lease4.Dispose();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesAllRateLimiters()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNativeRateLimiting();
        ServiceProvider provider = services.BuildServiceProvider();
        IRateLimiterProvider rateLimiterProvider = provider.GetRequiredService<IRateLimiterProvider>();

        string key1 = "dispose_test_1";
        string key2 = "dispose_test_2";
        var options = NativeRateLimiterOptions.SlidingWindow(100);

        rateLimiterProvider.GetRateLimiter(key1, options);
        rateLimiterProvider.GetRateLimiter(key2, options);

        // Act
        rateLimiterProvider.Dispose();

        // Assert - Should throw ObjectDisposedException
        Func<RateLimiter> act = () => rateLimiterProvider.GetRateLimiter(key1, options);
        act.Should().Throw<ObjectDisposedException>();
    }

    #endregion

    public void Dispose()
    {
        _rateLimiterProvider?.Dispose();
        _serviceProvider?.Dispose();
    }
}

