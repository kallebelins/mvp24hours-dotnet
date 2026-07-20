//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.RateLimiting;

namespace Mvp24Hours.Infrastructure.Test.Email.RateLimiting;

[Trait("Category", "Unit")]
public class EmailRateLimiterTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EmailRateLimiter(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task TryAcquire_FixedWindow_AfterWaitAsyncUpToMax_ShouldReturnFalse()
    {
        var limiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 2,
            WindowSize = TimeSpan.FromSeconds(1),
            Strategy = RateLimitStrategy.FixedWindow
        });

        await limiter.WaitAsync();
        await limiter.WaitAsync();

        limiter.TryAcquire().Should().BeFalse();
        limiter.GetRemainingRequests().Should().Be(0);
    }

    [Fact]
    public async Task TryAcquire_SlidingWindow_AfterWaitAsyncUpToMax_ShouldReturnFalse()
    {
        var limiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 2,
            WindowSize = TimeSpan.FromSeconds(1),
            Strategy = RateLimitStrategy.SlidingWindow
        });

        await limiter.WaitAsync();
        await limiter.WaitAsync();

        limiter.TryAcquire().Should().BeFalse();
    }

    [Fact]
    public void TryAcquire_TokenBucket_ShouldAllowUpToMaxThenDeny()
    {
        var limiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 2,
            WindowSize = TimeSpan.FromSeconds(1),
            Strategy = RateLimitStrategy.TokenBucket
        });

        limiter.TryAcquire().Should().BeTrue();
        limiter.TryAcquire().Should().BeTrue();
        limiter.TryAcquire().Should().BeFalse();
    }

    [Fact]
    public async Task WaitAsync_AfterWindowExpires_ShouldAllowAdditionalRequests()
    {
        var limiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 2,
            WindowSize = TimeSpan.FromMilliseconds(200),
            Strategy = RateLimitStrategy.FixedWindow
        });

        await limiter.WaitAsync();
        await limiter.WaitAsync();
        limiter.GetRemainingRequests().Should().Be(0);

        await Task.Delay(250);

        await limiter.WaitAsync();
        limiter.GetRemainingRequests().Should().Be(1);
    }

    [Fact]
    public async Task GetTimeUntilNextRequest_WhenAtCapacity_ShouldReturnPositiveDelay()
    {
        var limiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 2,
            WindowSize = TimeSpan.FromMilliseconds(500),
            Strategy = RateLimitStrategy.FixedWindow
        });

        await limiter.WaitAsync();
        await limiter.WaitAsync();

        TimeSpan? waitTime = limiter.GetTimeUntilNextRequest();

        waitTime.Should().NotBeNull();
        waitTime!.Value.Should().BeGreaterThan(TimeSpan.Zero);
        waitTime.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void GetRemainingRequests_WhenUnused_ShouldReturnMaxRequests()
    {
        var limiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 5,
            WindowSize = TimeSpan.FromSeconds(1),
            Strategy = RateLimitStrategy.FixedWindow
        });

        limiter.GetRemainingRequests().Should().Be(5);
    }

    [Fact]
    public async Task GetTimeUntilNextRequest_WhenUnderCapacity_ShouldReturnZero()
    {
        var limiter = new EmailRateLimiter(new RateLimitOptions
        {
            MaxRequestsPerWindow = 3,
            WindowSize = TimeSpan.FromSeconds(1),
            Strategy = RateLimitStrategy.FixedWindow
        });

        await limiter.WaitAsync();

        limiter.GetTimeUntilNextRequest().Should().Be(TimeSpan.Zero);
    }
}
