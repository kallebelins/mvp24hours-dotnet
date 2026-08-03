//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Sms.Providers;
using Mvp24Hours.Infrastructure.Sms.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Sms;

[Trait("Category", "Unit")]
public class SmsProviderEdgeCasesTest
{
    [Fact]
    public async Task InMemorySmsProvider_SendAsync_WithEmptyDestination_ShouldFail()
    {
        InMemorySmsProvider provider = CreateProvider();
        SmsMessage message = SmsTestHelpers.CreateValidMessage();
        message.To = "  ";

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("Recipient");
    }

    [Fact]
    public async Task InMemorySmsProvider_SendBatchAsync_WithEmptyCollection_ShouldReturnEmptyResults()
    {
        InMemorySmsProvider provider = CreateProvider();

        IList<SmsSendResult> results = await provider.SendBatchAsync([]);

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task InMemorySmsProvider_SendAsync_WithEmptyBody_ShouldFail()
    {
        InMemorySmsProvider provider = CreateProvider();
        SmsMessage message = SmsTestHelpers.CreateValidMessage();
        message.Body = "  ";

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("Body");
    }

    private static InMemorySmsProvider CreateProvider()
    {
        return new InMemorySmsProvider(SmsTestHelpers.CreateSmsOptions());
    }
}
