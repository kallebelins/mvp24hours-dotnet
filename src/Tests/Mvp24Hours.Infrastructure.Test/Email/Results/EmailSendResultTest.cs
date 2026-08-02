//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Results;

namespace Mvp24Hours.Infrastructure.Test.Email.Results;

[Trait("Category", "Unit")]
public class EmailSendResultTest
{
    [Fact]
    public void Successful_ShouldCreateSuccessResult()
    {
        DateTimeOffset sentAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        var result = EmailSendResult.Successful("msg-42", sentAt);

        result.Success.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.MessageId.Should().Be("msg-42");
        result.Errors.Should().BeEmpty();
        result.Exception.Should().BeNull();
        result.FirstError.Should().BeNull();
        result.SentAt.Should().Be(sentAt);
    }

    [Fact]
    public void Failed_WithErrorMessage_ShouldCreateFailureResult()
    {
        var exception = new InvalidOperationException("inner");

        var result = EmailSendResult.Failed("Send failed", exception);

        result.Success.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.MessageId.Should().BeNull();
        result.Errors.Should().ContainSingle("Send failed");
        result.FirstError.Should().Be("Send failed");
        result.Exception.Should().BeSameAs(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Failed_WithInvalidErrorMessage_ShouldThrowArgumentException(string? errorMessage)
    {
        Action act = () => EmailSendResult.Failed(errorMessage!);

        act.Should().Throw<ArgumentException>().WithParameterName("errorMessage");
    }

    [Fact]
    public void Failed_WithErrorsList_ShouldCreateFailureResult()
    {
        var result = EmailSendResult.Failed(["Error 1", "Error 2"]);

        result.Errors.Should().Equal("Error 1", "Error 2");
        result.FirstError.Should().Be("Error 1");
    }

    [Fact]
    public void Failed_WithNullErrorsList_ShouldThrowArgumentNullException()
    {
        Action act = () => EmailSendResult.Failed((IList<string>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("errors");
    }

    [Fact]
    public void Failed_WithEmptyErrorsList_ShouldThrowArgumentException()
    {
        Action act = () => EmailSendResult.Failed([]);

        act.Should().Throw<ArgumentException>().WithParameterName("errors");
    }

    [Fact]
    public void Failed_WithException_ShouldUseExceptionMessage()
    {
        var exception = new TimeoutException("Timed out");

        var result = EmailSendResult.Failed(exception);

        result.Errors.Should().ContainSingle("Timed out");
        result.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void Failed_WithNullException_ShouldThrowArgumentNullException()
    {
        Action act = () => EmailSendResult.Failed((Exception)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("exception");
    }
}
