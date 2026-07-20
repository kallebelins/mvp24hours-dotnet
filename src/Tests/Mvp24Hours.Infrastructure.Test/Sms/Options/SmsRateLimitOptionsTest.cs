//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Sms.Options;

[Trait("Category", "Unit")]
public class SmsRateLimitOptionsTest
{
    [Fact]
    public void DefaultConstructor_ShouldUseExpectedValues()
    {
        var options = new SmsRateLimitOptions();

        options.Enabled.Should().BeTrue();
        options.MaxMessagesPerDestination.Should().Be(10);
        options.TimeWindow.Should().Be(TimeSpan.FromHours(1));
        options.ThrowOnExceeded.Should().BeFalse();
    }

    [Fact]
    public void CreateSmsRateLimitOptions_ShouldApplyCustomValues()
    {
        SmsRateLimitOptions options = SmsTestHelpers.CreateSmsRateLimitOptions(
            enabled: false,
            maxMessagesPerDestination: 3,
            timeWindow: TimeSpan.FromMinutes(15),
            throwOnExceeded: true);

        options.Enabled.Should().BeFalse();
        options.MaxMessagesPerDestination.Should().Be(3);
        options.TimeWindow.Should().Be(TimeSpan.FromMinutes(15));
        options.ThrowOnExceeded.Should().BeTrue();
    }
}
