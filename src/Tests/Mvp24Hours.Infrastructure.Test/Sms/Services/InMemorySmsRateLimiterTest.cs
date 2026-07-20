//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Services;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Sms.Services;

[Trait("Category", "Unit")]
public class InMemorySmsRateLimiterTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new InMemorySmsRateLimiter(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsAllowedAsync_WithInvalidDestination_ShouldThrowArgumentException(string? destination)
    {
        InMemorySmsRateLimiter limiter = CreateLimiter();

        Func<Task> act = async () => await limiter.IsAllowedAsync(destination!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("destination");
    }

    [Fact]
    public async Task IsAllowedAsync_WhenDisabled_ShouldAlwaysReturnTrue()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(enabled: false, maxMessagesPerDestination: 1);
        string destination = "+5511999999999";

        await limiter.RecordSentAsync(destination);
        await limiter.RecordSentAsync(destination);

        bool allowed = await limiter.IsAllowedAsync(destination);

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_WithNoRecords_ShouldReturnTrue()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(maxMessagesPerDestination: 3);

        bool allowed = await limiter.IsAllowedAsync("+5511999999999");

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task RecordSentAsync_WhenDisabled_ShouldNotTrackCount()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(enabled: false);
        string destination = "+5511999999999";

        await limiter.RecordSentAsync(destination);
        await limiter.RecordSentAsync(destination);

        int count = await limiter.GetCountAsync(destination);

        count.Should().Be(0);
    }

    [Fact]
    public async Task RecordSentAsync_AndGetCountAsync_ShouldTrackSentMessages()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(maxMessagesPerDestination: 5);
        string destination = "+5511999999999";

        await limiter.RecordSentAsync(destination);
        await limiter.RecordSentAsync(destination);

        int count = await limiter.GetCountAsync(destination);

        count.Should().Be(2);
    }

    [Fact]
    public async Task IsAllowedAsync_WhenWindowLimitReached_ShouldReturnFalse()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(maxMessagesPerDestination: 2);
        string destination = "+5511999999999";

        await limiter.RecordSentAsync(destination);
        await limiter.RecordSentAsync(destination);

        bool allowed = await limiter.IsAllowedAsync(destination);

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_BelowWindowLimit_ShouldReturnTrue()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(maxMessagesPerDestination: 3);
        string destination = "+5511999999999";

        await limiter.RecordSentAsync(destination);
        await limiter.RecordSentAsync(destination);

        bool allowed = await limiter.IsAllowedAsync(destination);

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task ResetAsync_ShouldClearDestinationCounter()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(maxMessagesPerDestination: 1);
        string destination = "+5511999999999";

        await limiter.RecordSentAsync(destination);
        await limiter.ResetAsync(destination);

        int count = await limiter.GetCountAsync(destination);
        bool allowed = await limiter.IsAllowedAsync(destination);

        count.Should().Be(0);
        allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData("+55 (11) 99999-9999", "+5511999999999")]
    [InlineData("+55-11-99999-9999", "+5511999999999")]
    [InlineData("+55.11.99999.9999", "+5511999999999")]
    public async Task PhoneNormalization_ShouldTreatFormattedNumbersAsSameDestination(
        string firstFormat,
        string secondFormat)
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(maxMessagesPerDestination: 2);

        await limiter.RecordSentAsync(firstFormat);
        int count = await limiter.GetCountAsync(secondFormat);
        bool allowed = await limiter.IsAllowedAsync(secondFormat);

        count.Should().Be(1);
        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetCountAsync_WithInvalidDestination_ShouldThrowArgumentException()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter();

        Func<Task> act = async () => await limiter.GetCountAsync("  ");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("destination");
    }

    [Fact]
    public async Task RecordSentAsync_WithInvalidDestination_ShouldThrowArgumentException()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter();

        Func<Task> act = async () => await limiter.RecordSentAsync("");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("destination");
    }

    [Fact]
    public async Task ResetAsync_WithInvalidDestination_ShouldThrowArgumentException()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter();

        Func<Task> act = async () => await limiter.ResetAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("destination");
    }

    [Fact]
    public async Task GetCountAsync_AfterTimeWindowExpires_ShouldReturnZero()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(
            maxMessagesPerDestination: 1,
            timeWindow: TimeSpan.FromMilliseconds(50));
        string destination = "+5511888888888";

        await limiter.RecordSentAsync(destination);
        await Task.Delay(100);

        int count = await limiter.GetCountAsync(destination);
        bool allowed = await limiter.IsAllowedAsync(destination);

        count.Should().Be(0);
        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task DifferentDestinations_ShouldHaveIndependentCounters()
    {
        InMemorySmsRateLimiter limiter = CreateLimiter(maxMessagesPerDestination: 1);

        await limiter.RecordSentAsync("+5511111111111");

        bool firstAllowed = await limiter.IsAllowedAsync("+5511111111111");
        bool secondAllowed = await limiter.IsAllowedAsync("+5522222222222");

        firstAllowed.Should().BeFalse();
        secondAllowed.Should().BeTrue();
    }

    private static InMemorySmsRateLimiter CreateLimiter(
        bool enabled = true,
        int maxMessagesPerDestination = 10,
        TimeSpan? timeWindow = null)
    {
        return new InMemorySmsRateLimiter(
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsRateLimitOptions(
                enabled: enabled,
                maxMessagesPerDestination: maxMessagesPerDestination,
                timeWindow: timeWindow)));
    }
}
