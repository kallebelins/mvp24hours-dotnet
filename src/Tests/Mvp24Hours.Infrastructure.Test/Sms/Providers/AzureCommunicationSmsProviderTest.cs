//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Sms.Providers;
using Mvp24Hours.Infrastructure.Sms.Results;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Sms.Providers;

[Trait("Category", "Unit")]
public class AzureCommunicationSmsProviderTest
{
    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowInvalidOperationException()
    {
        IOptions<SmsOptions> smsOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions());
        IOptions<AzureCommunicationSmsOptions> azureOptions = SmsTestHelpers.AsOptions(new AzureCommunicationSmsOptions
        {
            ConnectionString = string.Empty
        });
        IHttpClientFactory factory = SmsTestHelpers.CreateHttpClientFactory(new TestHttpMessageHandler());

        Action act = () => _ = new AzureCommunicationSmsProvider(smsOptions, azureOptions, factory);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid Azure Communication Services configuration*");
    }

    [Fact]
    public void Constructor_WithInvalidConnectionString_ShouldThrowArgumentException()
    {
        IOptions<SmsOptions> smsOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions());
        IOptions<AzureCommunicationSmsOptions> azureOptions = SmsTestHelpers.AsOptions(new AzureCommunicationSmsOptions
        {
            ConnectionString = "not-a-valid-connection-string"
        });
        IHttpClientFactory factory = SmsTestHelpers.CreateHttpClientFactory(new TestHttpMessageHandler());

        Action act = () => _ = new AzureCommunicationSmsProvider(smsOptions, azureOptions, factory);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*endpoint*");
    }

    [Fact]
    public void Constructor_WithNullHttpClientFactory_ShouldThrowArgumentNullException()
    {
        IOptions<SmsOptions> smsOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions());
        IOptions<AzureCommunicationSmsOptions> azureOptions = SmsTestHelpers.AsOptions(SmsTestHelpers.CreateAzureOptions());

        Action act = () => _ = new AzureCommunicationSmsProvider(smsOptions, azureOptions, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsSuccess_ShouldReturnMessageIdFromResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/sms", HttpStatusCode.Accepted, new
            {
                value = new[]
                {
                    new
                    {
                        messageId = "azure-sms-123",
                        httpStatusCode = 202
                    }
                }
            });
        AzureCommunicationSmsProvider provider = CreateProvider(handler);
        SmsMessage message = SmsTestHelpers.CreateValidMessage(from: "+5511888888888");

        SmsSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("azure-sms-123");
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
        handler.RequestCount.Should().Be(1);

        RecordedRequest request = handler.ReceivedRequests.Single();
        request.Method.Should().Be("POST");
        request.RequestUri.Should().Contain("/sms");
        request.RequestUri.Should().Contain("api-version=2021-03-07");
        request.HasHeader("x-ms-date").Should().BeTrue();
        request.GetHeader("Authorization").Should().StartWith("HMAC-SHA256");

        using var doc = JsonDocument.Parse(request.Body!);
        doc.RootElement.GetProperty("from").GetString().Should().Be("+5511888888888");
        doc.RootElement.GetProperty("message").GetString().Should().Be("Hello SMS");
        doc.RootElement.GetProperty("smsRecipients")[0].GetProperty("to").GetString()
            .Should().Be("+5511999999999");
        doc.RootElement.GetProperty("smsSendOptions").GetProperty("enableDeliveryReport").GetBoolean()
            .Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WhenEnableDeliveryReports_ShouldSerializeOption()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/sms", HttpStatusCode.Accepted, new
            {
                value = new[]
                {
                    new { messageId = "id-1", httpStatusCode = 202 }
                }
            });
        AzureCommunicationSmsProvider provider = new(
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions()),
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateAzureOptions(enableDeliveryReports: true)),
            SmsTestHelpers.CreateHttpClientFactory(handler));

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeTrue();
        using var doc = JsonDocument.Parse(handler.ReceivedRequests.Single().Body!);
        doc.RootElement.GetProperty("smsSendOptions").GetProperty("enableDeliveryReport").GetBoolean()
            .Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WhenItemHttpStatusIndicatesFailure_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/sms", HttpStatusCode.OK, new
            {
                value = new[]
                {
                    new
                    {
                        messageId = (string?)null,
                        httpStatusCode = 400,
                        errorMessage = "Invalid recipient"
                    }
                }
            });
        AzureCommunicationSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("Invalid recipient");
    }

    [Fact]
    public async Task SendAsync_WhenResponseValueIsEmpty_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/sms", HttpStatusCode.OK, new { value = Array.Empty<object>() });
        AzureCommunicationSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("empty response");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsError_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/sms", HttpStatusCode.Unauthorized, new { error = new { message = "denied" } });
        AzureCommunicationSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("Azure Communication Services API error");
        result.FirstError.Should().Contain("Unauthorized");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WhenHttpRequestFails_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().SimulateNetworkFailure();
        AzureCommunicationSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WithInvalidMessage_ShouldNotCallApi()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/sms", HttpStatusCode.Accepted, new
            {
                value = new[] { new { messageId = "id-1", httpStatusCode = 202 } }
            });
        AzureCommunicationSmsProvider provider = CreateProvider(handler);

        SmsSendResult result = await provider.SendAsync(new SmsMessage());

        result.Success.Should().BeFalse();
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_ShouldApplyDefaultFromFromSmsOptions()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/sms", HttpStatusCode.Accepted, new
            {
                value = new[] { new { messageId = "id-1", httpStatusCode = 202 } }
            });
        AzureCommunicationSmsProvider provider = new(
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions(defaultFrom: "+5511555555555")),
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateAzureOptions()),
            SmsTestHelpers.CreateHttpClientFactory(handler));

        SmsSendResult result = await provider.SendAsync(SmsTestHelpers.CreateValidMessage(from: null));

        result.Success.Should().BeTrue();
        using var doc = JsonDocument.Parse(handler.ReceivedRequests.Single().Body!);
        doc.RootElement.GetProperty("from").GetString().Should().Be("+5511555555555");
    }

    private static AzureCommunicationSmsProvider CreateProvider(TestHttpMessageHandler handler)
    {
        return new AzureCommunicationSmsProvider(
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateSmsOptions()),
            SmsTestHelpers.AsOptions(SmsTestHelpers.CreateAzureOptions()),
            SmsTestHelpers.CreateHttpClientFactory(handler));
    }
}
