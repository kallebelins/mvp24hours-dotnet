//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Results;

namespace Mvp24Hours.Infrastructure.Test.Sms.Results;

[Trait("Category", "Unit")]
public class SmsSendResultTest
{
    [Fact]
    public void Successful_WithDefaults_ShouldSetQueuedStatusAndTimestamp()
    {
        var result = SmsSendResult.Successful();

        result.Success.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.Status.Should().Be(SmsDeliveryStatus.Queued);
        result.Errors.Should().BeEmpty();
        result.Exception.Should().BeNull();
        result.FirstError.Should().BeNull();
        result.SentAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Successful_WithCustomValues_ShouldSetProperties()
    {
        DateTimeOffset sentAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var result = SmsSendResult.Successful("msg-123", SmsDeliveryStatus.Delivered, sentAt);

        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("msg-123");
        result.Status.Should().Be(SmsDeliveryStatus.Delivered);
        result.SentAt.Should().Be(sentAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failed_WithEmptyErrorMessage_ShouldThrowArgumentException(string? errorMessage)
    {
        Action act = () => SmsSendResult.Failed(errorMessage!);

        act.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_WithSingleError_ShouldSetFailedStatus()
    {
        var exception = new InvalidOperationException("provider down");

        var result = SmsSendResult.Failed("send failed", exception);

        result.Success.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
        result.Errors.Should().ContainSingle().Which.Should().Be("send failed");
        result.FirstError.Should().Be("send failed");
        result.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Failed_WithNullErrorsList_ShouldThrowArgumentNullException()
    {
        Action act = () => SmsSendResult.Failed((IList<string>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("errors");
    }

    [Fact]
    public void Failed_WithEmptyErrorsList_ShouldThrowArgumentException()
    {
        Action act = () => SmsSendResult.Failed([]);

        act.Should().Throw<ArgumentException>().WithParameterName("errors");
    }

    [Fact]
    public void Failed_WithMultipleErrors_ShouldPreserveAllErrors()
    {
        var result = SmsSendResult.Failed(["error-a", "error-b"]);

        result.Errors.Should().BeEquivalentTo(["error-a", "error-b"]);
        result.FirstError.Should().Be("error-a");
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
    }

    [Fact]
    public void Failed_WithNullException_ShouldThrowArgumentNullException()
    {
        Action act = () => SmsSendResult.Failed((Exception)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }

    [Fact]
    public void Failed_FromException_ShouldUseExceptionMessage()
    {
        var exception = new TimeoutException("timed out");

        var result = SmsSendResult.Failed(exception);

        result.Errors.Should().ContainSingle().Which.Should().Be("timed out");
        result.Exception.Should().BeSameAs(exception);
        result.Status.Should().Be(SmsDeliveryStatus.Failed);
    }
}
