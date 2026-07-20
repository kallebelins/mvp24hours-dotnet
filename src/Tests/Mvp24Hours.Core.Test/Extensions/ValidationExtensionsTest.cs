//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FluentValidation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using ValidationException = Mvp24Hours.Core.Exceptions.ValidationException;

namespace Mvp24Hours.Core.Test.Extensions;

/// <summary>
/// Unit tests for validation helpers (ValidatorExtensions, ValidatorEntityExtensions, ValidatorNumberExtensions)
/// and ValidationException. Domain/Validation folder does not exist in Core — these are the real equivalents.
/// </summary>
[Trait("Category", "Unit")]
public class ValidationExtensionsTest
{
    private sealed class Person
    {
        [Required]
        public string? Name { get; set; }

        [Range(1, 120)]
        public int Age { get; set; }
    }

    private sealed class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(p => p.Name).NotEmpty().WithErrorCode("NAME_REQUIRED").WithMessage("Name is required");
            RuleFor(p => p.Age).InclusiveBetween(1, 120).WithErrorCode("AGE_RANGE").WithMessage("Age out of range");
        }
    }

    #region ValidatorExtensions — primitives

    [Theory]
    [InlineData("42", true)]
    [InlineData("x", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsInt32_ParsesAsExpected(string? value, bool expected)
    {
        value!.IsInt32().Should().Be(expected);
    }

    [Theory]
    [InlineData("9223372036854775807", true)]
    [InlineData("not-long", false)]
    public void IsLong_ParsesAsExpected(string value, bool expected)
    {
        value.IsLong().Should().Be(expected);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", true)]
    [InlineData("yes", false)]
    public void IsBoolean_ParsesAsExpected(string value, bool expected)
    {
        value.IsBoolean().Should().Be(expected);
    }

    [Theory]
    [InlineData("10.5", true)]
    [InlineData("abc", false)]
    public void IsDecimal_ParsesAsExpected(string value, bool expected)
    {
        value.IsDecimal().Should().Be(expected);
    }

    [Fact]
    public void IsDateTime_UsesCulture()
    {
        "07/20/2026".IsDateTime(new CultureInfo("en-US")).Should().BeTrue();
        "".IsDateTime().Should().BeFalse();
    }

    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void IsValidWebUrl_ValidatesAsExpected(string value, bool expected)
    {
        value.IsValidWebUrl().Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData(" ", false)]
    [InlineData(null, false)]
    public void HasValue_DetectsMeaningfulStrings(string? value, bool expected)
    {
        value.HasValue().Should().Be(expected);
    }

    [Fact]
    public void IsValidRegex_MatchesPattern()
    {
        "ABC123".IsValidRegex(@"^[A-Z]+\d+$").Should().BeTrue();
        "abc".IsValidRegex(@"^\d+$").Should().BeFalse();
        "".IsValidRegex(@"^.+$").Should().BeFalse();
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("invalid@", false)]
    [InlineData("", false)]
    public void IsValidEmail_ValidatesAsExpected(string email, bool expected)
    {
        email.IsValidEmail().Should().Be(expected);
    }

    [Fact]
    public void IsValidDate_And_IsValidRange()
    {
        DateTime.MinValue.IsValidDate().Should().BeFalse();
        DateTime.MaxValue.IsValidDate().Should().BeFalse();
        DateTime.UtcNow.IsValidDate().Should().BeTrue();

        DateTime start = DateTime.UtcNow;
        DateTime end = start.AddDays(1);
        start.IsValidRange(end).Should().BeTrue();
        end.IsValidRange(start).Should().BeFalse();
    }

    [Theory]
    [InlineData("5511999999999", true)]
    [InlineData("abc", false)]
    public void IsValidPhoneNumber_ValidatesDigits(string input, bool expected)
    {
        input.IsValidPhoneNumber().Should().Be(expected);
    }

    [Fact]
    public void IsValidConstraint_And_IsNumeric()
    {
        "A".IsValidConstraint("A", "B").Should().BeTrue();
        "C".IsValidConstraint("A", "B").Should().BeFalse();
        "".IsValidConstraint("A").Should().BeFalse();

        "12.34".IsNumeric().Should().BeTrue();
        "12a".IsNumeric().Should().BeFalse();
    }

    [Fact]
    public void IsValidCEP_RequiresFormattedZip()
    {
        "01310-100".IsValidCEP().Should().BeTrue();
        "01310100".IsValidCEP().Should().BeFalse();
    }

    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("123.456.789-00", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void IsValidCpf_ValidatesCheckDigits(string cpf, bool expected)
    {
        cpf.IsValidCpf().Should().Be(expected);
    }

    [Theory]
    [InlineData("11.222.333/0001-81", true)]
    [InlineData("11.222.333/0001-00", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    public void IsValidCnpj_ValidatesCheckDigits(string cnpj, bool expected)
    {
        cnpj.IsValidCnpj().Should().Be(expected);
    }

    [Fact]
    public void IsValidCreditCard_NumberOnly_MatchesKnownPatterns()
    {
        // Visa test pattern (Luhn-valid style match against generated regex)
        "4111111111111111".IsValidCreditCard().Should().BeTrue();
        "1234".IsValidCreditCard().Should().BeFalse();
        "".IsValidCreditCard().Should().BeFalse();
    }

    #endregion

    #region ValidatorEntityExtensions

    [Fact]
    public void TryValidate_WithDataAnnotations_ReturnsErrors()
    {
        var person = new Person { Name = null, Age = 0 };

        IList<IMessageResult> errors = person.TryValidate();

        errors.Should().NotBeEmpty();
        errors.Should().OnlyContain(e => e.Type == MessageType.Error);
    }

    [Fact]
    public void TryValidate_WithDataAnnotations_ReturnsEmptyWhenValid()
    {
        var person = new Person { Name = "Ada", Age = 30 };

        person.TryValidate().Should().BeEmpty();
    }

    [Fact]
    public void TryValidate_WithFluentValidator_MapsErrors()
    {
        var person = new Person { Name = "", Age = 200 };

        IList<IMessageResult> errors = person.TryValidate(new PersonValidator());

        errors.Should().HaveCount(2);
        errors.Select(e => e.Key).Should().Contain(["NAME_REQUIRED", "AGE_RANGE"]);
    }

    [Fact]
    public void TryValidate_WithFluentValidator_ReturnsEmptyWhenValid()
    {
        var person = new Person { Name = "Ada", Age = 30 };

        person.TryValidate(new PersonValidator()).Should().BeEmpty();
    }

    #endregion

    #region ValidatorNumberExtensions

    [Fact]
    public void IsNullOrDefault_ReturnsDefaultOrFallback()
    {
        int? missing = null;
        missing.IsNullOrDefault().Should().Be(0);
        missing.IsNullOrDefault(99).Should().Be(99);
        ((int?)5).IsNullOrDefault(99).Should().Be(5);
    }

    [Fact]
    public void IsZeroOrDefault_ReturnsAccordingToImplementation()
    {
        // Actual behavior: when value == 0 returns value; otherwise returns valueDefault.
        0.IsZeroOrDefault(10).Should().Be(0);
        5.IsZeroOrDefault(10).Should().Be(10);
        0L.IsZeroOrDefault(7L).Should().Be(0L);
        3L.IsZeroOrDefault(7L).Should().Be(7L);
        0m.IsZeroOrDefault(1.5m).Should().Be(0m);
        2.5m.IsZeroOrDefault(1.5m).Should().Be(1.5m);
    }

    #endregion

    #region ValidationException

    [Fact]
    public void ValidationException_Constructors_StoreExpectedState()
    {
        new ValidationException().Message.Should().NotBeNull();
        new ValidationException("failed").Message.Should().Be("failed");

        IList<IMessageResult> errors = [new MessageResult("Email", "Required", MessageType.Error)];
        var withErrors = new ValidationException("failed", errors);
        withErrors.ValidationErrors.Should().ContainSingle();

        var withInner = new ValidationException("failed", new InvalidOperationException("inner"));
        withInner.InnerException!.Message.Should().Be("inner");

        var withCode = new ValidationException("failed", "VAL_001", new Dictionary<string, object> { ["field"] = "Email" });
        withCode.ErrorCode.Should().Be("VAL_001");
        withCode.Context.Should().ContainKey("field");

        var withCodeAndErrors = new ValidationException("failed", "VAL_002", errors, new Dictionary<string, object> { ["n"] = 1 });
        withCodeAndErrors.ValidationErrors.Should().HaveCount(1);
        withCodeAndErrors.ErrorCode.Should().Be("VAL_002");
    }

    #endregion
}
