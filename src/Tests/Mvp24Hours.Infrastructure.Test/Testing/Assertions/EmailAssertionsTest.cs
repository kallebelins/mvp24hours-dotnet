//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using AssertionException = Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException;

namespace Mvp24Hours.Infrastructure.Test.Testing.Assertions;

[Trait("Category", "Unit")]
public class EmailAssertionsTest
{
    [Fact]
    public async Task AssertEmailSent_WithNoPredicate_ShouldPassWhenEmailExists()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage());

        Action act = () => EmailAssertions.AssertEmailSent(service);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertEmailSent_WithNoPredicate_ShouldThrowWhenNoEmailsSent()
    {
        FakeEmailService service = new();

        Action act = () => EmailAssertions.AssertEmailSent(service);

        act.Should().Throw<AssertionException>()
            .WithMessage("*at least one email*");
    }

    [Fact]
    public async Task AssertEmailSent_WithPredicate_ShouldPassWhenMatchExists()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(subject: "Invoice #42"));

        Action act = () => EmailAssertions.AssertEmailSent(service, m => m.Subject!.Contains("Invoice"));

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertEmailSent_WithPredicate_ShouldThrowWhenNoMatch()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(subject: "Welcome"));

        Action act = () => EmailAssertions.AssertEmailSent(service, m => m.Subject!.Contains("Invoice"));

        act.Should().Throw<AssertionException>()
            .WithMessage("*matching the specified criteria*");
    }

    [Fact]
    public async Task AssertEmailCount_ShouldPassWhenCountMatches()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage());
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(to: "other@example.com"));

        Action act = () => EmailAssertions.AssertEmailCount(service, 2);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertEmailCount_ShouldThrowWhenCountMismatch()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage());

        Action act = () => EmailAssertions.AssertEmailCount(service, 3);

        act.Should().Throw<AssertionException>()
            .WithMessage("*Expected 3 email(s)*but 1 were sent*");
    }

    [Fact]
    public async Task AssertEmailSentTo_ShouldPassWhenRecipientFound()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(to: "recipient@example.com"));

        Action act = () => EmailAssertions.AssertEmailSentTo(service, "recipient@example.com");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertEmailSentTo_ShouldThrowWhenRecipientMissing()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage(to: "other@example.com"));

        Action act = () => EmailAssertions.AssertEmailSentTo(service, "missing@example.com");

        act.Should().Throw<AssertionException>()
            .WithMessage("*missing@example.com*");
    }

    [Fact]
    public void AssertNoEmailsSent_ShouldPassWhenEmpty()
    {
        FakeEmailService service = new();

        Action act = () => EmailAssertions.AssertNoEmailsSent(service);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertNoEmailsSent_ShouldThrowWhenEmailsExist()
    {
        FakeEmailService service = new();
        await service.SendAsync(EmailTestHelpers.CreateValidMessage());

        Action act = () => EmailAssertions.AssertNoEmailsSent(service);

        act.Should().Throw<AssertionException>()
            .WithMessage("*Expected no emails*");
    }

    [Fact]
    public void AssertEmailSent_WithNullService_ShouldThrowArgumentNullException()
    {
        Action act = () => EmailAssertions.AssertEmailSent((IFakeEmailService)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("emailService");
    }

    [Fact]
    public async Task GetLastSentEmail_ShouldReturnLastMessageOrThrow()
    {
        FakeEmailService service = new();
        EmailMessage message = EmailTestHelpers.CreateValidMessage(subject: "Latest");
        await service.SendAsync(message);

        EmailMessage last = EmailAssertions.GetLastSentEmail(service);

        last.Should().BeSameAs(message);
    }

    [Fact]
    public void GetLastSentEmail_WhenEmpty_ShouldThrowAssertionException()
    {
        FakeEmailService service = new();

        Action act = () => EmailAssertions.GetLastSentEmail(service);

        act.Should().Throw<AssertionException>().WithMessage("*No emails were sent*");
    }
}
