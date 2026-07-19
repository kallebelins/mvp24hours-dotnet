//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Providers;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Email.Providers;

[Trait("Category", "Unit")]
public class AzureCommunicationEmailProviderTest
{
    [Fact]
    public void Constructor_WithInvalidConnectionString_ShouldThrowInvalidOperationException()
    {
        IOptions<EmailOptions> emailOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions());
        IOptions<AzureCommunicationEmailOptions> azureOptions = EmailTestHelpers.AsOptions(new AzureCommunicationEmailOptions
        {
            ConnectionString = "invalid"
        });
        IHttpClientFactory factory = EmailTestHelpers.CreateHttpClientFactory(new TestHttpMessageHandler());

        Action act = () => _ = new AzureCommunicationEmailProvider(emailOptions, azureOptions, factory);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid Azure Communication Services configuration*");
    }

    [Fact]
    public void Constructor_WithNullHttpClientFactory_ShouldThrowArgumentNullException()
    {
        IOptions<EmailOptions> emailOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions());
        IOptions<AzureCommunicationEmailOptions> azureOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateAzureOptions());

        Action act = () => _ = new AzureCommunicationEmailProvider(emailOptions, azureOptions, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsSuccess_ShouldReturnMessageIdFromResponse()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("emails:send", HttpStatusCode.Accepted, new { messageId = "azure-msg-123" });
        AzureCommunicationEmailProvider provider = CreateProvider(handler);
        EmailMessage message = EmailTestHelpers.CreateValidMessage(
            htmlBody: "<p>Azure</p>",
            plainTextBody: "Azure");
        message.Bcc = ["bcc@example.com"];
        message.Attachments = [new EmailAttachment("a.txt", "data"u8.ToArray(), "text/plain")];

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("azure-msg-123");
        handler.RequestCount.Should().Be(1);

        RecordedRequest request = handler.ReceivedRequests.Single();
        request.Method.Should().Be("POST");
        request.RequestUri.Should().Contain("emails:send");
        request.HasHeader("x-ms-date").Should().BeTrue();
        request.GetHeader("Authorization").Should().StartWith("HMAC-SHA256");

        using var doc = JsonDocument.Parse(request.Body!);
        // ApplyDefaults fills From from EmailOptions.DefaultFrom before the provider builds the request.
        doc.RootElement.GetProperty("senderAddress").GetString().Should().Be("noreply@example.com");
        doc.RootElement.GetProperty("content").GetProperty("subject").GetString().Should().Be("Test Subject");
        doc.RootElement.GetProperty("recipients").GetProperty("to")[0]
            .GetProperty("email").GetString().Should().Be("user@example.com");
    }

    [Fact]
    public async Task SendAsync_WhenResponseOmitsMessageId_ShouldGenerateOne()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("emails:send", HttpStatusCode.OK, new { status = "Running" });
        AzureCommunicationEmailProvider provider = CreateProvider(handler);

        EmailSendResult result = await provider.SendAsync(EmailTestHelpers.CreateValidMessage());

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsError_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("emails:send", HttpStatusCode.Unauthorized, new { error = new { message = "denied" } });
        AzureCommunicationEmailProvider provider = CreateProvider(handler);

        EmailSendResult result = await provider.SendAsync(EmailTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("Azure Communication Services API error");
        result.FirstError.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task SendAsync_WhenHttpRequestFails_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().SimulateNetworkFailure();
        AzureCommunicationEmailProvider provider = CreateProvider(handler);

        EmailSendResult result = await provider.SendAsync(EmailTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("HTTP error");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WithDisplayNameFrom_ShouldParseAddress()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("emails:send", HttpStatusCode.Accepted, new { messageId = "id-1" });
        AzureCommunicationEmailProvider provider = CreateProvider(handler);
        EmailMessage message = EmailTestHelpers.CreateValidMessage(from: "Contoso Support <support@contoso.com>");

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        using var doc = JsonDocument.Parse(handler.ReceivedRequests.Single().Body!);
        doc.RootElement.GetProperty("senderAddress").GetString().Should().Be("support@contoso.com");
    }

    [Fact]
    public async Task SendAsync_WithInvalidMessage_ShouldNotCallApi()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("emails:send", HttpStatusCode.Accepted, new { messageId = "id-1" });
        AzureCommunicationEmailProvider provider = CreateProvider(handler);

        EmailSendResult result = await provider.SendAsync(new EmailMessage());

        result.Success.Should().BeFalse();
        handler.RequestCount.Should().Be(0);
    }

    private static AzureCommunicationEmailProvider CreateProvider(TestHttpMessageHandler handler)
    {
        return new AzureCommunicationEmailProvider(
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions()),
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateAzureOptions()),
            EmailTestHelpers.CreateHttpClientFactory(handler));
    }
}
