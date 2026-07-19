//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Results;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Fakes;

namespace Mvp24Hours.Infrastructure.Test.Testing.Fakes;

[Trait("Category", "Unit")]
public class FakeEmailServiceTest
{
    [Fact]
    public async Task SendAsync_WithValidMessage_ShouldReturnSuccessAndStoreEmail()
    {
        FakeEmailService service = new();
        EmailMessage message = EmailTestHelpers.CreateValidMessage();

        EmailSendResult result = await service.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrWhiteSpace();
        service.SentEmails.Should().HaveCount(1);
        service.SentEmails[0].Should().BeSameAs(message);
    }

    [Fact]
    public async Task SendAsync_WithNullMessage_ShouldReturnFailedWithoutStoring()
    {
        FakeEmailService service = new();

        EmailSendResult result = await service.SendAsync(null!);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Contain("null");
        service.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WhenShouldFail_ShouldReturnFailedButStillStoreEmail()
    {
        FakeEmailService service = new()
        {
            ShouldFail = true,
            FailureMessage = "SMTP unavailable"
        };
        EmailMessage message = EmailTestHelpers.CreateValidMessage();

        EmailSendResult result = await service.SendAsync(message);

        result.Success.Should().BeFalse();
        result.FirstError.Should().Be("SMTP unavailable");
        service.SentEmails.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_WithCustomResultFactory_ShouldUseFactoryOverShouldFail()
    {
        FakeEmailService service = new()
        {
            ShouldFail = true,
            FailureMessage = "Should be ignored",
            CustomResultFactory = msg => EmailSendResult.Successful("custom-id")
        };
        EmailMessage message = EmailTestHelpers.CreateValidMessage();

        EmailSendResult result = await service.SendAsync(message);

        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("custom-id");
        service.SentEmails.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendBatchAsync_WithNullMessages_ShouldThrowArgumentNullException()
    {
        FakeEmailService service = new();

        Func<Task> act = async () => await service.SendBatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("messages");
    }

    [Fact]
    public async Task SendBatchAsync_WithMultipleMessages_ShouldStoreAllAndReturnResults()
    {
        FakeEmailService service = new();
        EmailMessage first = EmailTestHelpers.CreateValidMessage(to: "a@example.com", subject: "First");
        EmailMessage second = EmailTestHelpers.CreateValidMessage(to: "b@example.com", subject: "Second");

        IList<EmailSendResult> results = await service.SendBatchAsync([first, second]);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Success);
        service.SentEmails.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEmailsSentTo_ShouldMatchCaseInsensitively()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(to: "User@Example.com"));
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(to: "other@example.com"));

        IEnumerable<EmailMessage> matches = service.GetEmailsSentTo("user@example.com");

        matches.Should().HaveCount(1);
        matches.First().To.Should().Contain("User@Example.com");
    }

    [Fact]
    public async Task GetEmailsSentTo_WithEmptyAddress_ShouldReturnEmpty()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage());

        service.GetEmailsSentTo("").Should().BeEmpty();
        service.GetEmailsSentTo("   ").Should().BeEmpty();
    }

    [Fact]
    public async Task WasEmailSentWithSubject_ShouldMatchPartialCaseInsensitive()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(subject: "Welcome to MVP24Hours"));

        service.WasEmailSentWithSubject("welcome").Should().BeTrue();
        service.WasEmailSentWithSubject("MISSING").Should().BeFalse();
        service.WasEmailSentWithSubject("").Should().BeFalse();
    }

    [Fact]
    public async Task ClearSentEmails_ShouldRemoveAllStoredEmails()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage());
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(to: "other@example.com"));

        service.ClearSentEmails();

        service.SentEmails.Should().BeEmpty();
        service.GetLastSentEmail().Should().BeNull();
    }

    [Fact]
    public async Task GetLastSentEmail_ShouldReturnMostRecentlySentEmail()
    {
        FakeEmailService service = new();
        EmailMessage first = EmailTestHelpers.CreateValidMessage(subject: "First");
        EmailMessage second = EmailTestHelpers.CreateValidMessage(subject: "Second");
        await service.SendAsync(first);
        await service.SendAsync(second);

        service.GetLastSentEmail().Should().BeSameAs(second);
    }

    [Fact]
    public async Task SentEmails_ShouldReturnSnapshotNotLiveView()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage());
        IReadOnlyList<EmailMessage> snapshot = service.SentEmails;

        await service.SendAsync(EmailTestHelpers.CreateValidMessage(to: "other@example.com"));

        snapshot.Should().HaveCount(1);
        service.SentEmails.Should().HaveCount(2);
    }
}
