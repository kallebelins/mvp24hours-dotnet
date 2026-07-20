//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Sms.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Sms.Options;

[Trait("Category", "Unit")]
public class SmsOptionsTest
{
    [Fact]
    public void DefaultConstructor_ShouldUseExpectedValues()
    {
        var options = new SmsOptions();

        options.DefaultFrom.Should().BeNull();
        options.DefaultCountryCode.Should().BeNull();
        options.MaxMessageLength.Should().BeNull();
        options.ValidatePhoneNumbers.Should().BeTrue();
    }

    [Fact]
    public void Default_StaticProperty_ShouldReturnNewInstance()
    {
        SmsOptions options = SmsOptions.Default;

        options.Should().NotBeNull();
        options.ValidatePhoneNumbers.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidOptions_ShouldReturnEmptyList()
    {
        SmsOptions options = SmsTestHelpers.CreateSmsOptions(
            defaultCountryCode: "BR",
            maxMessageLength: 160);

        IList<string> errors = options.Validate();

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidMaxMessageLength_ShouldReturnError(int maxLength)
    {
        SmsOptions options = SmsTestHelpers.CreateSmsOptions(maxMessageLength: maxLength);

        IList<string> errors = options.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be("Maximum message length must be greater than zero.");
    }

    [Theory]
    [InlineData("BRA")]
    [InlineData("B1")]
    [InlineData("1B")]
    public void Validate_WithInvalidCountryCode_ShouldReturnError(string countryCode)
    {
        SmsOptions options = SmsTestHelpers.CreateSmsOptions(defaultCountryCode: countryCode);

        IList<string> errors = options.Validate();

        errors.Should().ContainSingle()
            .Which.Should().Be("Default country code must be a valid ISO 3166-1 alpha-2 code (2 letters).");
    }

    [Theory]
    [InlineData("BR")]
    [InlineData("US")]
    [InlineData("gb")]
    public void Validate_WithValidCountryCode_ShouldReturnEmptyList(string countryCode)
    {
        SmsOptions options = SmsTestHelpers.CreateSmsOptions(defaultCountryCode: countryCode);

        IList<string> errors = options.Validate();

        errors.Should().BeEmpty();
    }
}
