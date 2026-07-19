//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Results;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Fakes;

namespace Mvp24Hours.Infrastructure.Test.Testing.Fakes;

[Trait("Category", "Unit")]
public class FakeSmsServiceTest
{
    [Fact]
    public async Task SendAsync_WithValidMessage_ShouldReturnSuccessAndStoreMessage()
    {
        FakeSmsService service = new();
        SmsMessage message = SmsTestHelpers.CreateValidMessage();

        SmsSendResult result = await service.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
        service.SentMessages.Should().HaveCount(1);
        service.SentMessages[0].Should().BeSameAs(message);
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_ShouldReturnFailedWithoutStoring()
    {
        FakeSmsService service = new();

        SmsSendResult result = await service.SendAsync(null!);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("null");
        service.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WhenShouldFail_ShouldReturnFailedButStillStoreMessage()
    {
        FakeSmsService service = new()
        {
            ShouldFail = true,
            FailureMessage = "Gateway down"
        };
        SmsMessage message = SmsTestHelpers.CreateValidMessage();

        SmsSendResult result = await service.SendAsync(message);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Be("Gateway down");
        service.SentMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_WithCustomResultFactory_ShouldUseFactoryOverShouldFail()
    {
        FakeSmsService service = new()
        {
            ShouldFail = true,
            CustomResultFactory = _ => SmsSendResult.Successful("custom-sms-id", SmsDeliveryStatus.Delivered)
        };
        SmsMessage message = SmsTestHelpers.CreateValidMessage();

        SmsSendResult result = await service.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("custom-sms-id");
        service.SentMessages.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendBatchAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        FakeSmsService service = new();

        Func<Task> act = async () => await service.SendBatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("messages");
    }

    [Fact]
    public async Task SendBatchAsync_WithMultipleMessages_ShouldStoreAllAndReturnResults()
    {
        FakeSmsService service = new();
        SmsMessage first = SmsTestHelpers.CreateValidMessage(to: "+5511111111111", body: "First");
        SmsMessage second = SmsTestHelpers.CreateValidMessage(to: "+5522222222222", body: "Second");

        IList<SmsSendResult> results = await service.SendBatchAsync([first, second]);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
        service.SentMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task SendMmsAsync_WithValidMessage_ShouldStoreInSentMmsMessages()
    {
        FakeSmsService service = new();
        MmsMessage message = SmsTestHelpers.CreateValidMmsMessage();

        SmsSendResult result = await service.SendMmsAsync(message);

        result.Success.Should().BeTrue();
        service.SentMmsMessages.Should().HaveCount(1);
        service.SentMmsMessages[0].Should().BeSameAs(message);
        service.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task SendMmsAsync_WithNullMessage_ShouldReturnFailedWithoutStoring()
    {
        FakeSmsService service = new();

        SmsSendResult result = await service.SendMmsAsync(null!);

        result.Success.Should().BeFalse();
        service.SentMmsMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesSentTo_ShouldMatchCaseInsensitively()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(to: "+5511999999999"));
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(to: "+5511888888888"));

        IEnumerable<SmsMessage> matches = service.GetMessagesSentTo("+5511999999999");

        matches.Should().HaveCount(1);
    }

    [Fact]
    public async Task WasMessageSentContaining_ShouldMatchPartialCaseInsensitive()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(body: "Your verification code is 1234"));

        service.WasMessageSentContaining("verification").Should().BeTrue();
        service.WasMessageSentContaining("missing").Should().BeFalse();
        service.WasMessageSentContaining("").Should().BeFalse();
    }

    [Fact]
    public async Task ClearSentMessages_ShouldClearSmsAndMmsCollections()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage());
        await service.SendMmsAsync(SmsTestHelpers.CreateValidMmsMessage());

        service.ClearSentMessages();

        service.SentMessages.Should().BeEmpty();
        service.SentMmsMessages.Should().BeEmpty();
        service.GetLastSentMessage().Should().BeNull();
    }

    [Fact]
    public async Task GetLastSentMessage_ShouldReturnMostRecentlySentSms()
    {
        FakeSmsService service = new();
        SmsMessage first = SmsTestHelpers.CreateValidMessage(body: "First");
        SmsMessage second = SmsTestHelpers.CreateValidMessage(body: "Second");
        await service.SendAsync(first);
        await service.SendAsync(second);

        service.GetLastSentMessage().Should().BeSameAs(second);
    }
}
