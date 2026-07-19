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

namespace Mvp24Hours.Infrastructure.Test.Sms.Providers;

[Trait("Category", "Unit")]
public class InMemorySmsProviderTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new InMemorySmsProvider((SmsOptions)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        InMemorySmsProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SendAsync(null!));
    }

    [Fact]
    public async Task SendAsync_WithValidMessage_ShouldStoreSmsAndReturnSuccess()
    {
        InMemorySmsProvider provider = CreateProvider();
        SmsMessage message = SmsTestHelpers.CreateValidMessage();

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
        provider.SentMessages.Should().HaveCount(1);
        provider.SentMessages[0].To.Should().Be("+5511999999999");
        provider.SentMessages[0].Body.Should().Be("Hello SMS");
    }

    [Fact]
    public async Task SendAsync_ShouldApplyDefaultFrom()
    {
        SmsOptions options = SmsTestHelpers.CreateSmsOptions(defaultFrom: "+5511777777777");
        var provider = new InMemorySmsProvider(options);
        SmsMessage message = SmsTestHelpers.CreateValidMessage(from: null);

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        provider.SentMessages.Single().From.Should().Be("+5511777777777");
    }

    [Fact]
    public async Task SendAsync_WithInvalidMessage_ShouldReturnValidationFailure()
    {
        InMemorySmsProvider provider = CreateProvider();
        var message = new SmsMessage { Body = "No recipient" };

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Recipient", StringComparison.OrdinalIgnoreCase));
        provider.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WithInvalidPhoneNumber_ShouldFailValidation()
    {
        SmsOptions options = SmsTestHelpers.CreateSmsOptions(validatePhoneNumbers: true);
        var provider = new InMemorySmsProvider(options);
        SmsMessage message = SmsTestHelpers.CreateValidMessage(to: "not-a-phone");

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid phone number", StringComparison.OrdinalIgnoreCase));
        provider.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WhenBodyExceedsMaxLength_ShouldFailValidation()
    {
        SmsOptions options = SmsTestHelpers.CreateSmsOptions(maxMessageLength: 5);
        var provider = new InMemorySmsProvider(options);
        SmsMessage message = SmsTestHelpers.CreateValidMessage(body: "Too long message");

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("exceeds maximum", StringComparison.OrdinalIgnoreCase));
        provider.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        InMemorySmsProvider provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.SendAsync(SmsTestHelpers.CreateValidMessage(), cts.Token));
    }

    [Fact]
    public async Task SendBatchAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        InMemorySmsProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SendBatchAsync(null!));
    }

    [Fact]
    public async Task SendBatchAsync_ShouldReturnResultPerMessage()
    {
        InMemorySmsProvider provider = CreateProvider();
        SmsMessage[] messages =
        [
            SmsTestHelpers.CreateValidMessage(to: "+5511111111111", body: "A"),
            SmsTestHelpers.CreateValidMessage(to: "+5511222222222", body: "B"),
            new SmsMessage { Body = "invalid" }
        ];

        IList<SmsSendResult> results = await provider.SendBatchAsync(messages);

        results.Should().HaveCount(3);
        results[0].Success.Should().BeTrue();
        results[1].Success.Should().BeTrue();
        results[2].Success.Should().BeFalse();
        provider.SentMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task SendMmsAsync_WithValidMessage_ShouldStoreMmsAndReturnSuccess()
    {
        InMemorySmsProvider provider = CreateProvider();
        MmsMessage message = SmsTestHelpers.CreateValidMmsMessage();

        SmsSendResult result = await provider.SendMmsAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
        provider.SentMmsMessages.Should().HaveCount(1);
        provider.SentMmsMessages[0].To.Should().Be("+5511999999999");
        provider.SentMmsMessages[0].Attachments.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendMmsAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        InMemorySmsProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SendMmsAsync(null!));
    }

    [Fact]
    public async Task SendMmsAsync_WithInvalidMessage_ShouldReturnValidationFailure()
    {
        InMemorySmsProvider provider = CreateProvider();
        var message = new MmsMessage();

        SmsSendResult result = await provider.SendMmsAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        provider.SentMmsMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearSentMessages_ShouldRemoveStoredSmsAndMms()
    {
        InMemorySmsProvider provider = CreateProvider();
        await provider.SendAsync(SmsTestHelpers.CreateValidMessage());
        await provider.SendMmsAsync(SmsTestHelpers.CreateValidMmsMessage());
        provider.SentMessages.Should().HaveCount(1);
        provider.SentMmsMessages.Should().HaveCount(1);

        provider.ClearSentMessages();

        provider.SentMessages.Should().BeEmpty();
        provider.SentMmsMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_ConcurrentSends_ShouldBeThreadSafe()
    {
        InMemorySmsProvider provider = CreateProvider();
        IEnumerable<Task<SmsSendResult>> tasks = Enumerable.Range(0, 20)
            .Select(i => provider.SendAsync(SmsTestHelpers.CreateValidMessage(
                to: $"+55119999{i:D5}",
                body: $"Message {i}")));

        SmsSendResult[] results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.Success);
        provider.SentMessages.Should().HaveCount(20);
    }

    private static InMemorySmsProvider CreateProvider()
    {
        return new InMemorySmsProvider(SmsTestHelpers.CreateSmsOptions());
    }
}
