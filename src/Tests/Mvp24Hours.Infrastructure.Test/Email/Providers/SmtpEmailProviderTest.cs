//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Providers;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Providers;

[Trait("Category", "Unit")]
public class SmtpEmailProviderTest
{
    [Fact]
    public void Constructor_WithInvalidSmtpOptions_ShouldThrowInvalidOperationException()
    {
        IOptions<EmailOptions> emailOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions());
        IOptions<SmtpEmailOptions> smtpOptions = EmailTestHelpers.AsOptions(new SmtpEmailOptions
        {
            Host = string.Empty,
            Port = 587
        });

        Action act = () => _ = new SmtpEmailProvider(emailOptions, smtpOptions);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid SMTP configuration*");
    }

    [Fact]
    public void Constructor_WithNullSmtpOptions_ShouldThrowArgumentNullException()
    {
        IOptions<EmailOptions> emailOptions = EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions());

        Action act = () => _ = new SmtpEmailProvider(emailOptions, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("smtpOptions");
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        SmtpEmailProvider provider = CreateProvider();
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCertificateCallback_ShouldStillConstruct()
    {
        SmtpEmailOptions smtp = EmailTestHelpers.CreateSmtpOptions();
        smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;

        var provider = new SmtpEmailProvider(
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions()),
            EmailTestHelpers.AsOptions(smtp),
            NullLogger<SmtpEmailProvider>.Instance);

        provider.Should().NotBeNull();
    }

    [Fact]
    public async Task SendAsync_WithInvalidMessage_ShouldReturnValidationFailure()
    {
        SmtpEmailProvider provider = CreateProvider();

        EmailSendResult result = await provider.SendAsync(new EmailMessage());

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        SmtpEmailProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SendAsync(null!));
    }

    [Fact]
    public async Task SendAsync_WhenSmtpHostUnreachable_ShouldReturnFailure()
    {
        // Port 1 on loopback is not an SMTP listener; short timeout keeps the test fast.
        SmtpEmailOptions smtp = EmailTestHelpers.CreateSmtpOptions(
            host: "127.0.0.1",
            port: 1,
            timeout: 500);
        var provider = new SmtpEmailProvider(
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions()),
            EmailTestHelpers.AsOptions(smtp),
            NullLogger<SmtpEmailProvider>.Instance);

        EmailMessage message = EmailTestHelpers.CreateValidMessage(
            htmlBody: "<b>Hi</b>",
            plainTextBody: "Hi",
            from: "Sender <noreply@example.com>");
        message.Cc = ["cc@example.com"];
        message.Bcc = ["bcc@example.com"];
        message.ReplyTo = "reply@example.com";
        message.Priority = EmailPriority.High;
        message.RequestReadReceipt = true;
        message.Headers["X-Test"] = "smtp";
        message.Attachments =
        [
            new EmailAttachment("note.txt", "hello"u8.ToArray(), "text/plain")
            {
                IsInline = true,
                ContentId = "note"
            }
        ];

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.FirstError.Should().Match(e =>
            e.Contains("SMTP", StringComparison.OrdinalIgnoreCase) ||
            e.Contains("Failed to send email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendBatchAsync_ShouldReturnResultsForEachMessage()
    {
        SmtpEmailOptions smtp = EmailTestHelpers.CreateSmtpOptions(
            host: "127.0.0.1",
            port: 1,
            timeout: 500);
        var provider = new SmtpEmailProvider(
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions()),
            EmailTestHelpers.AsOptions(smtp));

        IList<EmailSendResult> results = await provider.SendBatchAsync(
        [
            EmailTestHelpers.CreateValidMessage(to: "a@example.com"),
            new EmailMessage { Subject = "invalid" }
        ]);

        results.Should().HaveCount(2);
        results[0].Success.Should().BeFalse();
        results[1].Success.Should().BeFalse();
        results[1].Errors.Should().NotBeEmpty();
    }

    private static SmtpEmailProvider CreateProvider()
    {
        return new SmtpEmailProvider(
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateEmailOptions()),
            EmailTestHelpers.AsOptions(EmailTestHelpers.CreateSmtpOptions()),
            NullLogger<SmtpEmailProvider>.Instance);
    }
}
