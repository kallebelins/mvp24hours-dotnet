//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Models;
using Mvp24Hours.Infrastructure.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using AssertionException = Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException;

namespace Mvp24Hours.Infrastructure.Test.Testing.Assertions;

[Trait("Category", "Unit")]
public class SmsAssertionsTest
{
    [Fact]
    public async Task AssertSmsSent_WithNoPredicate_ShouldPassWhenMessageExists()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage());

        Action act = () => SmsAssertions.AssertSmsSent(service);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertSmsSent_WithNoPredicate_ShouldThrowWhenNoMessagesSent()
    {
        FakeSmsService service = new();

        Action act = () => SmsAssertions.AssertSmsSent(service);

        act.Should().Throw<AssertionException>()
            .WithMessage("*at least one SMS*");
    }

    [Fact]
    public async Task AssertSmsSent_WithPredicate_ShouldPassWhenMatchExists()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(body: "Your code is 9999"));

        Action act = () => SmsAssertions.AssertSmsSent(service, m => m.Body!.Contains("9999"));

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertSmsSent_WithPredicate_ShouldThrowWhenNoMatch()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(body: "Hello"));

        Action act = () => SmsAssertions.AssertSmsSent(service, m => m.Body!.Contains("9999"));

        act.Should().Throw<AssertionException>()
            .WithMessage("*matching the specified criteria*");
    }

    [Fact]
    public async Task AssertSmsCount_ShouldPassWhenCountMatches()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage());
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(to: "+5511888888888"));

        Action act = () => SmsAssertions.AssertSmsCount(service, 2);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertSmsCount_ShouldThrowWhenCountMismatch()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage());

        Action act = () => SmsAssertions.AssertSmsCount(service, 0);

        act.Should().Throw<AssertionException>()
            .WithMessage("*Expected 0 SMS message(s)*but 1 were sent*");
    }

    [Fact]
    public async Task AssertSmsSentTo_ShouldPassWhenRecipientFound()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(to: "+5511999999999"));

        Action act = () => SmsAssertions.AssertSmsSentTo(service, "+5511999999999");

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertSmsSentTo_ShouldThrowWhenRecipientMissing()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(to: "+5511111111111"));

        Action act = () => SmsAssertions.AssertSmsSentTo(service, "+5599999999999");

        act.Should().Throw<AssertionException>()
            .WithMessage("*+5599999999999*");
    }

    [Fact]
    public async Task AssertSmsSentContaining_ShouldPassWhenTextFound()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage(body: "Verification PIN 4321"));

        Action act = () => SmsAssertions.AssertSmsSentContaining(service, "PIN");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoSmsSent_ShouldPassWhenEmpty()
    {
        FakeSmsService service = new();

        Action act = () => SmsAssertions.AssertNoSmsSent(service);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task AssertNoSmsSent_ShouldThrowWhenMessagesExist()
    {
        FakeSmsService service = new();
        await service.SendAsync(SmsTestHelpers.CreateValidMessage());

        Action act = () => SmsAssertions.AssertNoSmsSent(service);

        act.Should().Throw<AssertionException>()
            .WithMessage("*Expected no SMS messages*");
    }

    [Fact]
    public void AssertSmsSent_WithNullService_ShouldThrowArgumentNullException()
    {
        Action act = () => SmsAssertions.AssertSmsSent((IFakeSmsService)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("smsService");
    }

    [Fact]
    public async Task GetLastSentSms_ShouldReturnLastMessageOrThrow()
    {
        FakeSmsService service = new();
        SmsMessage message = SmsTestHelpers.CreateValidMessage(body: "Latest SMS");
        await service.SendAsync(message);

        SmsMessage last = SmsAssertions.GetLastSentSms(service);

        last.Should().BeSameAs(message);
    }
}
