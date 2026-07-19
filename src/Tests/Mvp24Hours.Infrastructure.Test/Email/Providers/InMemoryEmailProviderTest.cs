//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.Email.Providers;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Email.Providers;

[Trait("Category", "Unit")]
public class InMemoryEmailProviderTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new InMemoryEmailProvider((EmailOptions)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        InMemoryEmailProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SendAsync(null!));
    }

    [Fact]
    public async Task SendAsync_WithValidMessage_ShouldStoreEmailAndReturnSuccess()
    {
        InMemoryEmailProvider provider = CreateProvider();
        EmailMessage message = EmailTestHelpers.CreateValidMessage();

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
        provider.SentEmails.Should().HaveCount(1);
        provider.SentEmails[0].Subject.Should().Be("Test Subject");
        provider.SentEmails[0].To.Should().Contain("user@example.com");
    }

    [Fact]
    public async Task SendAsync_ShouldApplyDefaultFromAndSubjectPrefix()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions(
            defaultFrom: "default@example.com",
            defaultReplyTo: "reply@example.com",
            defaultSubjectPrefix: "[TEST] ");
        options.DefaultHeaders["X-App"] = "Infrastructure.Test";

        var provider = new InMemoryEmailProvider(options);
        EmailMessage message = EmailTestHelpers.CreateValidMessage(subject: "Hello");

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeTrue();
        EmailMessage stored = provider.SentEmails.Single();
        stored.From.Should().Be("default@example.com");
        stored.ReplyTo.Should().Be("reply@example.com");
        stored.Subject.Should().Be("[TEST] Hello");
        stored.Headers.Should().ContainKey("X-App").WhoseValue.Should().Be("Infrastructure.Test");
    }

    [Fact]
    public async Task SendAsync_WithInvalidMessage_ShouldReturnValidationFailure()
    {
        InMemoryEmailProvider provider = CreateProvider();
        var message = new EmailMessage { Subject = "No recipients or body" };

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        provider.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WhenRecipientsExceedMax_ShouldFailValidation()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions(maxRecipientsPerEmail: 1);
        var provider = new InMemoryEmailProvider(options);
        EmailMessage message = EmailTestHelpers.CreateValidMessage();
        message.Cc = ["cc@example.com"];

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("recipients", StringComparison.OrdinalIgnoreCase));
        provider.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WhenAttachmentsExceedMax_ShouldFailValidation()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions(maxAttachmentsPerEmail: 1);
        var provider = new InMemoryEmailProvider(options);
        EmailMessage message = EmailTestHelpers.CreateValidMessage();
        message.Attachments =
        [
            new EmailAttachment("a.txt", [1], "text/plain"),
            new EmailAttachment("b.txt", [2], "text/plain")
        ];

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("attachments", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SendAsync_WhenAttachmentSizeExceedsMax_ShouldFailValidation()
    {
        EmailOptions options = EmailTestHelpers.CreateEmailOptions(maxAttachmentSize: 2);
        var provider = new InMemoryEmailProvider(options);
        EmailMessage message = EmailTestHelpers.CreateValidMessage();
        message.Attachments = [new EmailAttachment("big.bin", [1, 2, 3, 4], "application/octet-stream")];

        EmailSendResult result = await provider.SendAsync(message);

        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("big.bin", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SendAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        InMemoryEmailProvider provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.SendAsync(EmailTestHelpers.CreateValidMessage(), cts.Token));
    }

    [Fact]
    public async Task SendBatchAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        InMemoryEmailProvider provider = CreateProvider();
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.SendBatchAsync(null!));
    }

    [Fact]
    public async Task SendBatchAsync_ShouldReturnResultPerMessage()
    {
        InMemoryEmailProvider provider = CreateProvider();
        EmailMessage[] messages =
        [
            EmailTestHelpers.CreateValidMessage(to: "a@example.com", subject: "A"),
            EmailTestHelpers.CreateValidMessage(to: "b@example.com", subject: "B"),
            new EmailMessage { Subject = "invalid" }
        ];

        IList<EmailSendResult> results = await provider.SendBatchAsync(messages);

        results.Should().HaveCount(3);
        results[0].Success.Should().BeTrue();
        results[1].Success.Should().BeTrue();
        results[2].Success.Should().BeFalse();
        provider.SentEmails.Should().HaveCount(2);
    }

    [Fact]
    public async Task ClearSentEmails_ShouldRemoveStoredMessages()
    {
        InMemoryEmailProvider provider = CreateProvider();
        await provider.SendAsync(EmailTestHelpers.CreateValidMessage());
        provider.SentEmails.Should().HaveCount(1);

        provider.ClearSentEmails();

        provider.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_ConcurrentSends_ShouldBeThreadSafe()
    {
        InMemoryEmailProvider provider = CreateProvider();
        IEnumerable<Task<EmailSendResult>> tasks = Enumerable.Range(0, 20)
            .Select(i => provider.SendAsync(EmailTestHelpers.CreateValidMessage(
                to: $"user{i}@example.com",
                subject: $"Subject {i}")));

        EmailSendResult[] results = await Task.WhenAll(tasks);

        results.Should().OnlyContain(r => r.Success);
        provider.SentEmails.Should().HaveCount(20);
    }

    private static InMemoryEmailProvider CreateProvider()
    {
        return new InMemoryEmailProvider(EmailTestHelpers.CreateEmailOptions());
    }
}
