//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Guard clause validations.
/// </summary>
[Trait("Category", "Unit")]
public class GuardTest
{
    #region Null Tests

    [Fact]
    public void Null_WithValidValue_ReturnsValue()
    {
        // Arrange
        string value = "test";

        // Act
        string result = Guard.Against.Null(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Null_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        Func<string> act = () => Guard.Against.Null(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void Null_WithCustomMessage_IncludesMessage()
    {
        // Arrange
        string? value = null;
        string customMessage = "Custom error message";

        // Act
        Func<string> act = () => Guard.Against.Null(value, nameof(value), customMessage);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage($"*{customMessage}*");
    }

    #endregion

    #region NullOrEmpty String Tests

    [Fact]
    public void NullOrEmpty_String_WithValidValue_ReturnsValue()
    {
        // Arrange
        string value = "test";

        // Act
        string result = Guard.Against.NullOrEmpty(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void NullOrEmpty_String_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        Func<string> act = () => Guard.Against.NullOrEmpty(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NullOrEmpty_String_WithEmpty_ThrowsArgumentException()
    {
        // Arrange
        string value = string.Empty;

        // Act
        Func<string> act = () => Guard.Against.NullOrEmpty(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region NullOrEmpty Collection Tests

    [Fact]
    public void NullOrEmpty_Collection_WithValidValue_ReturnsValue()
    {
        // Arrange
        var value = new List<int> { 1, 2, 3 };

        // Act
        IEnumerable<int> result = Guard.Against.NullOrEmpty(value, nameof(value));

        // Assert
        result.Should().BeEquivalentTo(value);
    }

    [Fact]
    public void NullOrEmpty_Collection_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        List<int>? value = null;

        // Act
        Func<IEnumerable<int>> act = () => Guard.Against.NullOrEmpty(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NullOrEmpty_Collection_WithEmpty_ThrowsArgumentException()
    {
        // Arrange
        var value = new List<int>();

        // Act
        Func<IEnumerable<int>> act = () => Guard.Against.NullOrEmpty(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region NullOrWhiteSpace Tests

    [Fact]
    public void NullOrWhiteSpace_WithValidValue_ReturnsValue()
    {
        // Arrange
        string value = "test";

        // Act
        string result = Guard.Against.NullOrWhiteSpace(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void NullOrWhiteSpace_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        Func<string> act = () => Guard.Against.NullOrWhiteSpace(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NullOrWhiteSpace_WithWhitespace_ThrowsArgumentException()
    {
        // Arrange
        string value = "   ";

        // Act
        Func<string> act = () => Guard.Against.NullOrWhiteSpace(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region Default Tests

    [Fact]
    public void Default_WithNonDefaultGuid_ReturnsValue()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        Guid result = Guard.Against.Default(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Default_WithDefaultGuid_ThrowsArgumentException()
    {
        // Arrange
        Guid value = Guid.Empty;

        // Act
        Func<Guid> act = () => Guard.Against.Default(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void Default_WithDefaultDateTime_ThrowsArgumentException()
    {
        // Arrange
        var value = default(DateTime);

        // Act
        Func<DateTime> act = () => Guard.Against.Default(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void Default_WithDefaultInt_ThrowsArgumentException()
    {
        // Arrange
        int value = default;

        // Act
        Func<int> act = () => Guard.Against.Default(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region OutOfRange Tests

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void OutOfRange_WithValidValue_ReturnsValue(int value, int min, int max)
    {
        // Act
        int result = Guard.Against.OutOfRange(value, min, max, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0, 1, 10)]
    [InlineData(11, 1, 10)]
    [InlineData(-5, 1, 10)]
    public void OutOfRange_WithOutOfRangeValue_ThrowsArgumentOutOfRangeException(int value, int min, int max)
    {
        // Act
        Func<int> act = () => Guard.Against.OutOfRange(value, min, max, nameof(value));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region NegativeOrZero Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void NegativeOrZero_Int_WithPositiveValue_ReturnsValue(int value)
    {
        // Act
        int result = Guard.Against.NegativeOrZero(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NegativeOrZero_Int_WithNegativeOrZero_ThrowsArgumentOutOfRangeException(int value)
    {
        // Act
        Func<int> act = () => Guard.Against.NegativeOrZero(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(100L)]
    [InlineData(long.MaxValue)]
    public void NegativeOrZero_Long_WithPositiveValue_ReturnsValue(long value)
    {
        // Act
        long result = Guard.Against.NegativeOrZero(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void NegativeOrZero_Long_WithNegativeOrZero_ThrowsArgumentOutOfRangeException(long value)
    {
        // Act
        Func<long> act = () => Guard.Against.NegativeOrZero(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    [Fact]
    public void NegativeOrZero_Decimal_WithPositiveValue_ReturnsValue()
    {
        // Arrange
        decimal value = 1.5m;

        // Act
        decimal result = Guard.Against.NegativeOrZero(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void NegativeOrZero_Decimal_WithNegativeOrZero_ThrowsArgumentOutOfRangeException(double inputValue)
    {
        // Arrange
        decimal value = (decimal)inputValue;

        // Act
        Func<decimal> act = () => Guard.Against.NegativeOrZero(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region Negative Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void Negative_Int_WithNonNegativeValue_ReturnsValue(int value)
    {
        // Act
        int result = Guard.Against.Negative(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Negative_Int_WithNegativeValue_ThrowsArgumentOutOfRangeException(int value)
    {
        // Act
        Func<int> act = () => Guard.Against.Negative(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    public void Negative_Decimal_WithNonNegativeValue_ReturnsValue(double inputValue)
    {
        // Arrange
        decimal value = (decimal)inputValue;

        // Act
        decimal result = Guard.Against.Negative(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Negative_Decimal_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        decimal value = -1.5m;

        // Act
        Func<decimal> act = () => Guard.Against.Negative(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region InvalidEmail Tests

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user@subdomain.example.com")]
    public void InvalidEmail_WithValidEmail_ReturnsValue(string email)
    {
        // Act
        string result = Guard.Against.InvalidEmail(email, nameof(email));

        // Assert
        result.Should().Be(email);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@.com")]
    public void InvalidEmail_WithInvalidEmail_ThrowsArgumentException(string email)
    {
        // Act
        Func<string> act = () => Guard.Against.InvalidEmail(email, nameof(email));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(email));
    }

    [Fact]
    public void InvalidEmail_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? email = null;

        // Act
        Func<string> act = () => Guard.Against.InvalidEmail(email!, nameof(email));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(email));
    }

    #endregion

    #region InvalidCpf Tests

    [Theory]
    [InlineData("123.456.789-09")]
    [InlineData("12345678909")]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void InvalidCpf_WithValidCpf_ReturnsValue(string cpf)
    {
        // Act
        string result = Guard.Against.InvalidCpf(cpf, nameof(cpf));

        // Assert
        result.Should().Be(cpf);
    }

    [Theory]
    [InlineData("111.111.111-11")] // All same digits
    [InlineData("123.456.789-00")] // Invalid check digits
    [InlineData("12345")] // Too short
    [InlineData("123456789012345")] // Too long
    public void InvalidCpf_WithInvalidCpf_ThrowsArgumentException(string cpf)
    {
        // Act
        Func<string> act = () => Guard.Against.InvalidCpf(cpf, nameof(cpf));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(cpf));
    }

    [Fact]
    public void InvalidCpf_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? cpf = null;

        // Act
        Func<string> act = () => Guard.Against.InvalidCpf(cpf!, nameof(cpf));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(cpf));
    }

    #endregion

    #region InvalidCnpj Tests

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    [InlineData("45.997.418/0001-53")]
    [InlineData("45997418000153")]
    public void InvalidCnpj_WithValidCnpj_ReturnsValue(string cnpj)
    {
        // Act
        string result = Guard.Against.InvalidCnpj(cnpj, nameof(cnpj));

        // Assert
        result.Should().Be(cnpj);
    }

    [Theory]
    [InlineData("11.111.111/1111-11")] // All same digits
    [InlineData("11.222.333/0001-00")] // Invalid check digits
    [InlineData("12345")] // Too short
    [InlineData("123456789012345678")] // Too long
    public void InvalidCnpj_WithInvalidCnpj_ThrowsArgumentException(string cnpj)
    {
        // Act
        Func<string> act = () => Guard.Against.InvalidCnpj(cnpj, nameof(cnpj));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(cnpj));
    }

    [Fact]
    public void InvalidCnpj_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        string? cnpj = null;

        // Act
        Func<string> act = () => Guard.Against.InvalidCnpj(cnpj!, nameof(cnpj));

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName(nameof(cnpj));
    }

    #endregion

    #region InvalidFormat Tests

    [Fact]
    public void InvalidFormat_WithMatchingPattern_ReturnsValue()
    {
        // Arrange
        string value = "ABC-123";
        string pattern = @"^[A-Z]{3}-\d{3}$";

        // Act
        string result = Guard.Against.InvalidFormat(value, pattern, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void InvalidFormat_WithNonMatchingPattern_ThrowsArgumentException()
    {
        // Arrange
        string value = "ABC123";
        string pattern = @"^[A-Z]{3}-\d{3}$";

        // Act
        Func<string> act = () => Guard.Against.InvalidFormat(value, pattern, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region EmptyGuid Tests

    [Fact]
    public void EmptyGuid_WithValidGuid_ReturnsValue()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        Guid result = Guard.Against.EmptyGuid(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void EmptyGuid_WithEmptyGuid_ThrowsArgumentException()
    {
        // Arrange
        Guid value = Guid.Empty;

        // Act
        Func<Guid> act = () => Guard.Against.EmptyGuid(value, nameof(value));

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(value));
    }

    #endregion

    #region Condition Tests

    [Fact]
    public void Condition_WithFalseCondition_DoesNotThrow()
    {
        // Act
        Action act = () => Guard.Against.Condition(false, "param", "Error message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Condition_WithTrueCondition_ThrowsArgumentException()
    {
        // Arrange
        string message = "Error message";

        // Act
        Action act = () => Guard.Against.Condition(true, "param", message);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{message}*");
    }

    #endregion

    #region InvalidOperation Tests

    [Fact]
    public void InvalidOperation_WithFalseCondition_DoesNotThrow()
    {
        // Act
        Action act = () => Guard.Against.InvalidOperation(false, "Error message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void InvalidOperation_WithTrueCondition_ThrowsInvalidOperationException()
    {
        // Arrange
        string message = "Invalid state error";

        // Act
        Action act = () => Guard.Against.InvalidOperation(true, message);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage(message);
    }

    #endregion
}

