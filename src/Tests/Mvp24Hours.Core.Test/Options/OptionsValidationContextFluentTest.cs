//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Options;

namespace Mvp24Hours.Core.Test.Options;

/// <summary>
/// Unit tests for fluent OptionsValidationContext validators.
/// </summary>
[Trait("Category", "Unit")]
public class OptionsValidationContextFluentTest
{
    #region StringPropertyValidator

    [Fact]
    public void StringPropertyValidator_NotNullOrEmpty_WithValidValue_Succeeds()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Name", "value").NotNullOrEmpty();

        context.ToResult().Succeeded.Should().BeTrue();
    }

    [Fact]
    public void StringPropertyValidator_NotNullOrEmpty_WithEmptyValue_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Name", string.Empty).NotNullOrEmpty();

        OptionsValidationResult result = context.ToResult();
        result.Succeeded.Should().BeFalse();
        result.Failures.Should().ContainSingle(e => e.Contains("Name") && e.Contains("required"));
    }

    [Fact]
    public void StringPropertyValidator_MaxLength_WhenExceeded_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Name", "toolongvalue").MaxLength(5);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void StringPropertyValidator_Matches_WhenPatternFails_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Name", "abc").Matches("^[0-9]+$");

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void StringPropertyValidator_IsEmail_WithValidEmail_Succeeds()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Email", "user@example.com").IsEmail();

        context.ToResult().Succeeded.Should().BeTrue();
    }

    [Fact]
    public void StringPropertyValidator_IsEmail_WithInvalidEmail_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Email", "not-an-email").IsEmail();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void StringPropertyValidator_IsUri_WithValidAbsoluteUri_Succeeds()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Website", "https://example.com").IsUri();

        context.ToResult().Succeeded.Should().BeTrue();
    }

    [Fact]
    public void StringPropertyValidator_IsUri_WithInvalidUri_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Website", "not-a-uri").IsUri();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void StringPropertyValidator_Must_WhenPredicateFails_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Name", "bad")
            .Must(v => v == "good", "Name must be 'good'.");

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void StringPropertyValidator_When_ConditionFalse_SkipsNestedValidation()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Email", null)
            .When(false, v => v.NotNullOrEmpty());

        context.ToResult().Succeeded.Should().BeTrue();
    }

    [Fact]
    public void StringPropertyValidator_When_ConditionTrue_AppliesNestedValidation()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Email", null)
            .When(true, v => v.NotNullOrEmpty());

        context.ToResult().Succeeded.Should().BeFalse();
    }

    #endregion

    #region NumericPropertyValidator

    [Fact]
    public void NumericPropertyValidator_InRange_WhenOutOfRange_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Port", 70000).InRange(1, 65535);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void NumericPropertyValidator_GreaterThan_WhenNotMet_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Port", 0).GreaterThan(0);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void NumericPropertyValidator_LessThanOrEqualTo_WhenExceeded_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Port", 8080).LessThanOrEqualTo(8000);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void NumericPropertyValidator_Must_WhenPredicateFails_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("Port", 443)
            .Must(p => p != 443, "Port 443 is reserved.");

        context.ToResult().Succeeded.Should().BeFalse();
    }

    #endregion

    #region NullableNumericPropertyValidator

    [Fact]
    public void NullableNumericPropertyValidator_NotNull_WhenNull_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("OptionalTimeout", (int?)null).NotNull();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void NullableNumericPropertyValidator_InRangeIfPresent_WhenOutOfRange_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("OptionalTimeout", (int?)5000)
            .InRangeIfPresent(1, 3600);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void NullableNumericPropertyValidator_InRangeIfPresent_WhenNull_Succeeds()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateProperty("OptionalTimeout", (int?)null)
            .InRangeIfPresent(1, 3600);

        context.ToResult().Succeeded.Should().BeTrue();
    }

    #endregion

    #region TimeSpanPropertyValidator

    [Fact]
    public void TimeSpanPropertyValidator_Positive_WhenZero_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateTimeSpan("Timeout", TimeSpan.Zero).Positive();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TimeSpanPropertyValidator_NotNegative_WhenNegative_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateTimeSpan("Timeout", TimeSpan.FromSeconds(-1)).NotNegative();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TimeSpanPropertyValidator_MaxValue_WhenExceeded_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateTimeSpan("Timeout", TimeSpan.FromMinutes(10))
            .MaxValue(TimeSpan.FromMinutes(5));

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TimeSpanPropertyValidator_MinValue_WhenBelowMinimum_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateTimeSpan("Timeout", TimeSpan.FromSeconds(1))
            .MinValue(TimeSpan.FromSeconds(5));

        context.ToResult().Succeeded.Should().BeFalse();
    }

    #endregion

    #region UriPropertyValidator

    [Fact]
    public void UriPropertyValidator_NotNull_WhenNull_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateUri("Endpoint", null).NotNull();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void UriPropertyValidator_IsAbsolute_WhenRelative_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateUri("Endpoint", new Uri("/relative", UriKind.Relative)).IsAbsolute();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void UriPropertyValidator_IsHttps_WhenHttp_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateUri("Endpoint", new Uri("http://example.com")).IsHttps();

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void UriPropertyValidator_HasScheme_WhenSchemeNotAllowed_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateUri("Endpoint", new Uri("ftp://example.com"))
            .HasScheme(["https", "http"]);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void UriPropertyValidator_WithValidHttpsUri_Succeeds()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ValidateUri("Endpoint", new Uri("https://example.com"))
            .NotNull()
            .IsAbsolute()
            .IsHttps();

        context.ToResult().Succeeded.Should().BeTrue();
    }

    #endregion

    #region Context-level validators

    [Fact]
    public void AtLeastOne_WhenNoConditionTrue_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.AtLeastOne("At least one auth method is required.", false, false);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void ExactlyOne_WhenMultipleConditionsTrue_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.ExactlyOne("Exactly one auth method is required.", true, true);

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void When_ConditionFalse_AddsError()
    {
        var context = new OptionsValidationContext<object>("FluentTestOptions");

        context.When(false, "Condition must be true.");

        context.ToResult().Succeeded.Should().BeFalse();
    }

    [Fact]
    public void ToResult_WithNoErrors_ReturnsSuccess()
    {
        var context = new OptionsValidationContext<object>("MyOptions");

        context.ValidateProperty("Name", "valid").NotNullOrEmpty();
        context.ValidateProperty("Port", 8080).InRange(1, 65535);

        OptionsValidationResult result = context.ToResult();

        result.Succeeded.Should().BeTrue();
        result.Failures.Should().BeEmpty();
    }

    #endregion
}
