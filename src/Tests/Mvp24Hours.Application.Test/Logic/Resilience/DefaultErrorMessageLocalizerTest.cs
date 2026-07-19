using System.Globalization;
using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;

namespace Mvp24Hours.Application.Test.Logic.Resilience;

[Trait("Category", "Unit")]
public class DefaultErrorMessageLocalizerTest
{
    private readonly DefaultErrorMessageLocalizer _localizer = new();

    [Fact]
    public void GetMessage_KnownErrorCode_ShouldReturnDefaultMessage()
    {
        string message = _localizer.GetMessage(ErrorCodes.Resource.NotFound);

        message.Should().Be("The requested resource was not found.");
    }

    [Fact]
    public void GetMessage_WithFormatArgs_ShouldSubstitutePlaceholders()
    {
        string message = _localizer.GetMessage(ErrorCodes.Validation.Required, "Email");

        message.Should().Be("The field 'Email' is required.");
    }

    [Fact]
    public void GetMessage_UnknownCode_ShouldReturnCodeItself()
    {
        string message = _localizer.GetMessage("CUSTOM.UNKNOWN");

        message.Should().Be("CUSTOM.UNKNOWN");
    }

    [Fact]
    public void GetMessage_EmptyCode_ShouldReturnGenericMessage()
    {
        string message = _localizer.GetMessage("");

        message.Should().Be("An error occurred.");
    }

    [Fact]
    public void HasMessage_KnownCode_ShouldReturnTrue()
    {
        _localizer.HasMessage(ErrorCodes.Auth.Unauthorized).Should().BeTrue();
    }

    [Fact]
    public void HasMessage_UnknownCode_ShouldReturnFalse()
    {
        _localizer.HasMessage("NOT.A.REAL.CODE").Should().BeFalse();
    }

    [Fact]
    public void GetPropertyMessage_ShouldPrependPropertyNameToArgs()
    {
        string message = _localizer.GetPropertyMessage(ErrorCodes.Validation.MaxLength, "Description", 100);

        message.Should().Be("The field 'Description' must not exceed 100 characters.");
    }

    [Fact]
    public void GetMessage_WithCulture_ShouldFormatUsingCulture()
    {
        string message = _localizer.GetMessage(
            ErrorCodes.Validation.Required,
            CultureInfo.InvariantCulture,
            "Name");

        message.Should().Be("The field 'Name' is required.");
    }
}
