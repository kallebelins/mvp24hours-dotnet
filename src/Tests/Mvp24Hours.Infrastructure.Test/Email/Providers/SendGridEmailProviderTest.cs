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
public class SendGridEmailProviderTest
{
    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowInvalidOperationException()
    {
        IOptions<EmailOptions> emailOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions());
        IOptions<SendGridEmailOptions> sendGridOptions = EmailTestHelpers.AsOptions(new SendGridEmailOptions
        {
            ApiKey = string.Empty
        });
        IHttpClientFactory factory = EmailTestHelpers.CreateHttpClientFactory(new TestHttpMessageHandler());

        Action act = () => _ = new SendGridEmailProvider(emailOptions, sendGridOptions, factory);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid SendGrid configuration*");
    }

    [Fact]
    public void Constructor_WithNullHttpClientFactory_ShouldThrowArgumentNullException()
    {
        IOptions<EmailOptions> emailOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions());
        IOptions<SendGridEmailOptions> sendGridOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateSendGridOptions());

        Action act = () => _ = new SendGridEmailProvider(emailOptions, sendGridOptions, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("httpClientFactory");
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsSuccess_ShouldReturnSuccessfulResult()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/mail/send", HttpStatusCode.Accepted);
        SendGridEmailProvider provider = CreateProvider(handler);
        EmailMessage message = EmailTestHelpers.CreateValidMessage(
            htmlBody: "<p>Hi</p>",
            plainTextBody: "Hi");
        message.Cc = ["cc@example.com"];
        message.Attachments = [new EmailAttachment("note.txt", "hello"u8.ToArray(), "text/plain")];
        message.Headers["X-Custom"] = "1";

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
        handler.RequestCount.Should().Be(1);
        RecordedRequest request = handler.ReceivedRequests.Single();
        request.Method.Should().Be("POST");
        request.RequestUri.Should().Contain("/mail/send");
        request.GetHeader("Authorization").Should().StartWith("Bearer ");
        request.Body.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(request.Body!);
        doc.RootElement.GetProperty("from").GetProperty("email").GetString().Should().Be("noreply@example.com");
        doc.RootElement.GetProperty("personalizations")[0].GetProperty("to")[0]
            .GetProperty("email").GetString().Should().Be("user@example.com");
        doc.RootElement.GetProperty("content").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsError_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/mail/send", HttpStatusCode.BadRequest, new { errors = new[] { new { message = "invalid" } } });
        SendGridEmailProvider provider = CreateProvider(handler);

        EmailSendResult result = await provider.SendAsync(EmailTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("SendGrid API error");
        result.FirstError.Should().Contain("BadRequest");
    }

    [Fact]
    public async Task SendAsync_WhenHttpRequestFails_ShouldReturnFailure()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler().SimulateNetworkFailure();
        SendGridEmailProvider provider = CreateProvider(handler);

        EmailSendResult result = await provider.SendAsync(EmailTestHelpers.CreateValidMessage());

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("HTTP error");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_WithDisplayNameFrom_ShouldSerializeName()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/mail/send", HttpStatusCode.Accepted);
        SendGridEmailProvider provider = CreateProvider(handler);
        EmailMessage message = EmailTestHelpers.CreateValidMessage(from: "Sender Name <sender@example.com>");

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        using var doc = JsonDocument.Parse(handler.ReceivedRequests.Single().Body!);
        JsonElement from = doc.RootElement.GetProperty("from");
        from.GetProperty("email").GetString().Should().Be("sender@example.com");
        from.GetProperty("name").GetString().Should().Be("Sender Name");
    }

    [Fact]
    public async Task SendAsync_WithInvalidMessage_ShouldNotCallApi()
    {
        TestHttpMessageHandler handler = new TestHttpMessageHandler()
            .WhenPost("/mail/send", HttpStatusCode.Accepted);
        SendGridEmailProvider provider = CreateProvider(handler);

        EmailSendResult result = await provider.SendAsync(new EmailMessage());

        result.Success.Should().BeFalse();
        handler.RequestCount.Should().Be(0);
    }

    private static SendGridEmailProvider CreateProvider(TestHttpMessageHandler handler)
    {
        return new SendGridEmailProvider(
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions()),
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateSendGridOptions()),
            EmailTestHelpers.CreateHttpClientFactory(handler));
    }
}
