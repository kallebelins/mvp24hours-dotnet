//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Sms.Providers;
using Mvp24Hours.Infrastructure.Sms.Results;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Sms.Providers;

[Trait("Category", "Unit")]
public class TwilioSmsProviderTest
{
    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowInvalidOperationException()
    {
        IOptions<SmsOptions> smsOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions());
        IOptions<TwilioSmsOptions> twilioOptions = SmsTestHelpers.AsOptions(new TwilioSmsOptions
        {
            AccountSid = string.Empty,
            AuthToken = string.Empty
        });
        IHttpClientFactory factory = SmsTestHelpers.CreateHttpClientFactory(new TestHttpMessageHandler());

        Action act = () => _ = new TwilioSmsProvider(smsOptions, twilioOptions, factory);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid Twilio configuration*");
    }

    [Fact]
    public void Constructor_WithAccountSidNotStartingWithAc_ShouldThrowInvalidOperationException()
    {
        IOptions<SmsOptions> smsOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions());
        IOptions<TwilioSmsOptions> twilioOptions = SmsTestHelpers.AsOptions(new TwilioSmsOptions
        {
            AccountSid = "INVALID_SID",
            AuthToken = "token"
        });
        IHttpClientFactory factory = SmsTestHelpers.CreateHttpClientFactory(new TestHttpMessageHandler());

        Action act = () => _ = new TwilioSmsProvider(smsOptions, twilioOptions, factory);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must start with 'AC'*");
    }

    [Fact]
    public void Constructor_WithNullHttpClientFactory_ShouldThrowArgumentNullException()
    {
        IOptions<SmsOptions> smsOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions());
        IOptions<TwilioSmsOptions> twilioOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateTwilioOptions());

        Action act = () => _ = new TwilioSmsProvider(smsOptions, twilioOptions, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsSuccess_ShouldReturnSuccessfulResult()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/Messages.json", HttpStatusCode.Created, new
            {
                sid = "SMaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                status = "queued"
            });
        TwilioSmsProvider provider = CreateProvider(handler);
        SmsMessage message = SmsTestHelpers.CreateValidMessage(from: "+5511888888888");
        message.Metadata = new Dictionary<string, string> { ["CampaignId"] = "42" };

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("SMaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
        handler.RequestCount.Should().Be(1);

        RecordedRequest request = handler.ReceivedRequests.Single();
        request.Method.Should().Be("POST");
        request.RequestUri.Should().Contain("/2010-04-01/Accounts/");
        request.RequestUri.Should().Contain("/Messages.json");
        request.GetHeader("Authorization").Should().StartWith("Basic ");
        request.Body.Should().Contain("To=%2B5511999999999");
        request.Body.Should().Contain("Body=Hello+SMS");
        request.Body.Should().Contain("From=%2B5511888888888");
        request.Body.Should().Contain("MetaCampaignId=42");
    }

    [Theory]
    [InlineData("sent", SmsDeliveryStatus.Sent)]
    [InlineData("delivered", SmsDeliveryStatus.Delivered)]
    [InlineData("failed", SmsDeliveryStatus.Failed)]
    [InlineData("undelivered", SmsDeliveryStatus.Undelivered)]
    [InlineData("unknown-status", SmsDeliveryStatus.Unknown)]
    public async Task SendAsync_ShouldMapTwilioStatus(string twilioStatus, SmsDeliveryStatus expected)
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/Messages.json", HttpStatusCode.Created, new
            {
                sid = "SMbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                status = twilioStatus
            });
        TwilioSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeTrue();
        result.Status.Should().Be(expected);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsError_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/Messages.json", HttpStatusCode.BadRequest, new { message = "Invalid 'To' Phone Number" });
        TwilioSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("Invalid 'To' Phone Number");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsErrorWithoutJsonMessage_ShouldUseStatusCode()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/Messages.json", HttpStatusCode.InternalServerError, "not-json");
        TwilioSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("500");
    }

    [Fact]
    public async Task SendAsync_WhenHttpRequestFails_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().SimulateNetworkFailure();
        TwilioSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WithInvalidMessage_ShouldNotCallApi()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/Messages.json", HttpStatusCode.Created, new { sid = "SM1", status = "queued" });
        TwilioSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(new SmsMessage());

        result.Success.Should().BeFalse();
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_ShouldApplyDefaultFromFromSmsOptions()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/Messages.json", HttpStatusCode.Created, new { sid = "SM1", status = "queued" });
        TwilioSmsProvider provider = new(
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions(defaultFrom: "+5511666666666")),
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateTwilioOptions()),
            SmsTestHelpers.CreateHttpClientFactory(handler));

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage(from: null));

        result.Success.Should().BeTrue();
        handler.ReceivedRequests.Single().Body.Should().Contain("From=%2B5511666666666");
    }

    private static TwilioSmsProvider CreateProvider(TestHttpMessageHandler handler)
    {
        return new TwilioSmsProvider(
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions()),
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateTwilioOptions()),
            SmsTestHelpers.CreateHttpClientFactory(handler));
    }
}
