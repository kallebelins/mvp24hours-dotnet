//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.ValueObjects;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Value Objects.
/// </summary>
public class ValueObjectsTest
{
    #region Email Tests

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("USER@EXAMPLE.COM")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    public void Email_Create_WithValidEmail_CreatesEmail(string emailValue)
    {
        // Act
        var email = Email.Create(emailValue);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(emailValue.ToLowerInvariant());
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("")]
    public void Email_Create_WithInvalidEmail_ThrowsArgumentException(string emailValue)
    {
        // Act
        var act = () => Email.Create(emailValue);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Email_TryParse_WithValidEmail_ReturnsTrue()
    {
        // Arrange
        var emailValue = "user@example.com";

        // Act
        var result = Email.TryParse(emailValue, out var email);

        // Assert
        result.Should().BeTrue();
        email.Should().NotBeNull();
        email.Value.Should().Be(emailValue);
    }

    [Fact]
    public void Email_TryParse_WithInvalidEmail_ReturnsFalse()
    {
        // Arrange
        var emailValue = "invalid";

        // Act
        var result = Email.TryParse(emailValue, out var email);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Email_IsValid_ReturnsExpectedResult(string? emailValue, bool expected)
    {
        // Act
        var result = Email.IsValid(emailValue!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Email_LocalPartAndDomain_AreCorrectlyParsed()
    {
        // Arrange
        var email = Email.Create("user@example.com");

        // Assert
        email.LocalPart.Should().Be("user");
        email.Domain.Should().Be("example.com");
    }

    #endregion

    #region Cpf Tests

    [Theory]
    [InlineData("123.456.789-09", "12345678909")]
    [InlineData("12345678909", "12345678909")]
    [InlineData("529.982.247-25", "52998224725")]
    public void Cpf_Create_WithValidCpf_CreatesCpf(string input, string expectedValue)
    {
        // Act
        var cpf = Cpf.Create(input);

        // Assert
        cpf.Should().NotBeNull();
        cpf.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("12345678909", "123.456.789-09")]
    [InlineData("52998224725", "529.982.247-25")]
    public void Cpf_Formatted_ReturnsFormattedCpf(string input, string expectedFormatted)
    {
        // Act
        var cpf = Cpf.Create(input);

        // Assert
        cpf.Formatted.Should().Be(expectedFormatted);
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("123.456.789-00")]
    [InlineData("12345")]
    [InlineData("")]
    public void Cpf_Create_WithInvalidCpf_ThrowsArgumentException(string cpfValue)
    {
        // Act
        var act = () => Cpf.Create(cpfValue);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cpf_TryParse_WithValidCpf_ReturnsTrue()
    {
        // Arrange
        var cpfValue = "123.456.789-09";

        // Act
        var result = Cpf.TryParse(cpfValue, out var cpf);

        // Assert
        result.Should().BeTrue();
        cpf.Should().NotBeNull();
    }

    [Fact]
    public void Cpf_TryParse_WithInvalidCpf_ReturnsFalse()
    {
        // Arrange
        var cpfValue = "111.111.111-11";

        // Act
        var result = Cpf.TryParse(cpfValue, out var cpf);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Cpf_Equality_SameCpfs_AreEqual()
    {
        // Arrange
        var cpf1 = Cpf.Create("123.456.789-09");
        var cpf2 = Cpf.Create("12345678909");

        // Assert
        cpf1.Should().Be(cpf2);
        (cpf1 == cpf2).Should().BeTrue();
    }

    #endregion

    #region Cnpj Tests

    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("11222333000181", "11222333000181")]
    [InlineData("45.997.418/0001-53", "45997418000153")]
    public void Cnpj_Create_WithValidCnpj_CreatesCnpj(string input, string expectedValue)
    {
        // Act
        var cnpj = Cnpj.Create(input);

        // Assert
        cnpj.Should().NotBeNull();
        cnpj.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("11222333000181", "11.222.333/0001-81")]
    [InlineData("45997418000153", "45.997.418/0001-53")]
    public void Cnpj_Formatted_ReturnsFormattedCnpj(string input, string expectedFormatted)
    {
        // Act
        var cnpj = Cnpj.Create(input);

        // Assert
        cnpj.Formatted.Should().Be(expectedFormatted);
    }

    [Theory]
    [InlineData("11.111.111/1111-11")]
    [InlineData("11.222.333/0001-00")]
    [InlineData("12345")]
    [InlineData("")]
    public void Cnpj_Create_WithInvalidCnpj_ThrowsArgumentException(string cnpjValue)
    {
        // Act
        var act = () => Cnpj.Create(cnpjValue);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Cnpj_TryParse_WithValidCnpj_ReturnsTrue()
    {
        // Arrange
        var cnpjValue = "11.222.333/0001-81";

        // Act
        var result = Cnpj.TryParse(cnpjValue, out var cnpj);

        // Assert
        result.Should().BeTrue();
        cnpj.Should().NotBeNull();
    }

    [Fact]
    public void Cnpj_TryParse_WithInvalidCnpj_ReturnsFalse()
    {
        // Arrange
        var cnpjValue = "11.111.111/1111-11";

        // Act
        var result = Cnpj.TryParse(cnpjValue, out var cnpj);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Money Tests

    [Fact]
    public void Money_Create_CreatesValidMoney()
    {
        // Act
        var money = Money.Create(99.99m, "USD");

        // Assert
        money.Amount.Should().Be(99.99m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Addition_AddsTwoMoneyValues()
    {
        // Arrange
        var price = Money.Create(100m, "USD");
        var tax = Money.Create(10m, "USD");

        // Act
        var total = price + tax;

        // Assert
        total.Amount.Should().Be(110m);
        total.Currency.Should().Be("USD");
    }

    [Fact]
    public void Money_Subtraction_SubtractsTwoMoneyValues()
    {
        // Arrange
        var price = Money.Create(100m, "USD");
        var discount = Money.Create(20m, "USD");

        // Act
        var final = price - discount;

        // Assert
        final.Amount.Should().Be(80m);
    }

    [Fact]
    public void Money_Multiplication_MultipliesByScalar()
    {
        // Arrange
        var price = Money.Create(100m, "USD");

        // Act
        var total = price * 3;

        // Assert
        total.Amount.Should().Be(300m);
    }

    [Fact]
    public void Money_Division_DividesByScalar()
    {
        // Arrange
        var total = Money.Create(100m, "USD");

        // Act
        var split = total / 4;

        // Assert
        split.Amount.Should().Be(25m);
    }

    [Fact]
    public void Money_Division_ByZero_ThrowsDivideByZeroException()
    {
        // Arrange
        var money = Money.Create(100m, "USD");

        // Act
        var act = () => money / 0;

        // Assert
        act.Should().Throw<DivideByZeroException>();
    }

    [Fact]
    public void Money_Comparison_WorksCorrectly()
    {
        // Arrange
        var less = Money.Create(50m, "USD");
        var more = Money.Create(100m, "USD");

        // Assert
        (less < more).Should().BeTrue();
        (more > less).Should().BeTrue();
        (less <= more).Should().BeTrue();
        (more >= less).Should().BeTrue();
    }

    [Fact]
    public void Money_DifferentCurrencies_AdditionThrows()
    {
        // Arrange
        var usd = Money.Create(100m, "USD");
        var brl = Money.Create(100m, "BRL");

        // Act
        var act = () => usd + brl;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Money_DifferentCurrencies_ComparisonThrows()
    {
        // Arrange
        var usd = Money.Create(100m, "USD");
        var brl = Money.Create(100m, "BRL");

        // Act
        var act = () => usd > brl;

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Money_FactoryMethods_WorkCorrectly()
    {
        // Act
        var usd = Money.USD(100m);
        var brl = Money.BRL(100m);
        var eur = Money.EUR(100m);

        // Assert
        usd.Currency.Should().Be("USD");
        brl.Currency.Should().Be("BRL");
        eur.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Money_IsZero_IsPositive_IsNegative_WorkCorrectly()
    {
        // Arrange
        var zero = Money.Zero("USD");
        var positive = Money.USD(100m);
        var negative = Money.USD(-50m);

        // Assert
        zero.IsZero.Should().BeTrue();
        positive.IsPositive.Should().BeTrue();
        negative.IsNegative.Should().BeTrue();
    }

    #endregion

    #region Address Tests

    [Fact]
    public void Address_Create_CreatesValidAddress()
    {
        // Act
        var address = Address.Create(
            street: "Av. Paulista",
            number: "1000",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            country: "Brasil"
        );

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be("Av. Paulista");
        address.City.Should().Be("São Paulo");
        address.Country.Should().Be("Brasil");
    }

    [Fact]
    public void Address_FullAddress_FormatsCorrectly()
    {
        // Arrange
        var address = Address.Create(
            street: "Av. Paulista",
            number: "1000",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            country: "Brasil"
        );

        // Act
        var fullAddress = address.FullAddress;

        // Assert
        fullAddress.Should().Contain("Av. Paulista");
        fullAddress.Should().Contain("1000");
        fullAddress.Should().Contain("São Paulo");
        fullAddress.Should().Contain("SP");
        fullAddress.Should().Contain("Brasil");
    }

    [Fact]
    public void Address_Create_WithMissingStreet_ThrowsArgumentException()
    {
        // Act
        var act = () => Address.Create(
            street: "",
            number: "1000",
            city: "São Paulo",
            state: "SP",
            postalCode: "01310-100",
            country: "Brasil"
        );

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Address_Equality_SameAddresses_AreEqual()
    {
        // Arrange
        var address1 = Address.Create("Street", "100", "City", "ST", "12345", "Country");
        var address2 = Address.Create("Street", "100", "City", "ST", "12345", "Country");

        // Assert
        address1.Should().Be(address2);
    }

    #endregion

    #region DateRange Tests

    [Fact]
    public void DateRange_Create_CreatesValidDateRange()
    {
        // Arrange
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 12, 31);

        // Act
        var range = DateRange.Create(start, end);

        // Assert
        range.Start.Should().Be(start);
        range.End.Should().Be(end);
    }

    [Fact]
    public void DateRange_Create_WithEndBeforeStart_ThrowsArgumentException()
    {
        // Arrange
        var start = new DateTime(2024, 12, 31);
        var end = new DateTime(2024, 1, 1);

        // Act
        var act = () => DateRange.Create(start, end);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DateRange_Duration_ReturnsCorrectDuration()
    {
        // Arrange
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 1, 31);
        var range = DateRange.Create(start, end);

        // Assert
        range.Duration.TotalDays.Should().Be(30);
    }

    [Fact]
    public void DateRange_Contains_DateTime_ReturnsTrue_WhenInRange()
    {
        // Arrange
        var range = DateRange.Create(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );
        var dateInRange = new DateTime(2024, 6, 15);

        // Act
        var result = range.Contains(dateInRange);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DateRange_Contains_DateTime_ReturnsFalse_WhenOutOfRange()
    {
        // Arrange
        var range = DateRange.Create(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31)
        );
        var dateOutOfRange = new DateTime(2025, 1, 1);

        // Act
        var result = range.Contains(dateOutOfRange);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DateRange_Overlaps_ReturnsTrue_WhenRangesOverlap()
    {
        // Arrange
        var range1 = DateRange.Create(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 6, 30)
        );
        var range2 = DateRange.Create(
            new DateTime(2024, 4, 1),
            new DateTime(2024, 9, 30)
        );

        // Act
        var result = range1.Overlaps(range2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DateRange_Overlaps_ReturnsFalse_WhenRangesDontOverlap()
    {
        // Arrange
        var range1 = DateRange.Create(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 3, 31)
        );
        var range2 = DateRange.Create(
            new DateTime(2024, 7, 1),
            new DateTime(2024, 9, 30)
        );

        // Act
        var result = range1.Overlaps(range2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DateRange_GetIntersection_ReturnsIntersection()
    {
        // Arrange
        var range1 = DateRange.Create(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 6, 30)
        );
        var range2 = DateRange.Create(
            new DateTime(2024, 4, 1),
            new DateTime(2024, 9, 30)
        );

        // Act
        var intersection = range1.GetIntersection(range2);

        // Assert
        intersection.Should().NotBeNull();
        intersection!.Start.Should().Be(new DateTime(2024, 4, 1));
        intersection.End.Should().Be(new DateTime(2024, 6, 30));
    }

    #endregion

    #region Percentage Tests

    [Fact]
    public void Percentage_FromPercent_CreatesCorrectPercentage()
    {
        // Act
        var percentage = Percentage.FromPercent(15);

        // Assert
        percentage.Value.Should().Be(15);
        percentage.AsDecimal.Should().Be(0.15m);
    }

    [Fact]
    public void Percentage_FromDecimal_CreatesCorrectPercentage()
    {
        // Act
        var percentage = Percentage.FromDecimal(0.10m);

        // Assert
        percentage.Value.Should().Be(10);
        percentage.AsDecimal.Should().Be(0.10m);
    }

    [Fact]
    public void Percentage_Of_CalculatesCorrectly()
    {
        // Arrange
        var percentage = Percentage.FromPercent(15);

        // Act
        var result = percentage.Of(100m);

        // Assert
        result.Should().Be(15m);
    }

    [Fact]
    public void Percentage_ApplyTo_CalculatesDiscount()
    {
        // Arrange
        var discount = Percentage.FromPercent(15);

        // Act
        var result = discount.ApplyTo(100m);

        // Assert
        result.Should().Be(85m);
    }

    [Fact]
    public void Percentage_AddTo_CalculatesWithAddition()
    {
        // Arrange
        var tax = Percentage.FromPercent(10);

        // Act
        var result = tax.AddTo(100m);

        // Assert
        result.Should().Be(110m);
    }

    [Fact]
    public void Percentage_FromRatio_CalculatesPercentage()
    {
        // Act
        var percentage = Percentage.FromRatio(25m, 100m);

        // Assert
        percentage.Value.Should().Be(25m);
    }

    [Fact]
    public void Percentage_Complement_ReturnsComplement()
    {
        // Arrange
        var percentage = Percentage.FromPercent(30);

        // Act
        var complement = percentage.Complement;

        // Assert
        complement.Value.Should().Be(70);
    }

    [Fact]
    public void Percentage_Zero_ReturnsZeroPercentage()
    {
        // Act
        var zero = Percentage.Zero;

        // Assert
        zero.Value.Should().Be(0);
        zero.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Percentage_Full_Returns100Percent()
    {
        // Act
        var full = Percentage.Full;

        // Assert
        full.Value.Should().Be(100);
        full.IsFull.Should().BeTrue();
    }

    #endregion

    #region PhoneNumber Tests

    [Fact]
    public void PhoneNumber_Create_CreatesValidPhoneNumber()
    {
        // Act
        var phone = PhoneNumber.Create("+55", "11", "999887766");

        // Assert
        phone.Should().NotBeNull();
        phone.CountryCode.Should().Be("+55");
        phone.AreaCode.Should().Be("11");
        phone.Number.Should().Be("999887766");
    }

    [Fact]
    public void PhoneNumber_CreateBrazilian_CreatesPhoneWith55CountryCode()
    {
        // Act
        var phone = PhoneNumber.CreateBrazilian("11", "999887766");

        // Assert
        phone.CountryCode.Should().Be("+55");
    }

    [Fact]
    public void PhoneNumber_CreateUSA_CreatesPhoneWith1CountryCode()
    {
        // Act
        var phone = PhoneNumber.CreateUSA("555", "1234567");

        // Assert
        phone.CountryCode.Should().Be("+1");
    }

    [Fact]
    public void PhoneNumber_FullNumber_ReturnsAllDigits()
    {
        // Arrange
        var phone = PhoneNumber.Create("+55", "11", "999887766");

        // Act
        var fullNumber = phone.FullNumber;

        // Assert
        fullNumber.Should().Be("5511999887766");
    }

    [Fact]
    public void PhoneNumber_Create_WithInvalidNumber_ThrowsArgumentException()
    {
        // Act
        var act = () => PhoneNumber.Create("+55", "11", "12345");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PhoneNumber_TryParse_WithValidData_ReturnsTrue()
    {
        // Act
        var result = PhoneNumber.TryParse("+55", "11", "999887766", out var phone);

        // Assert
        result.Should().BeTrue();
        phone.Should().NotBeNull();
    }

    [Fact]
    public void PhoneNumber_TryParse_WithInvalidData_ReturnsFalse()
    {
        // Act
        var result = PhoneNumber.TryParse("+55", "11", "123", out var phone);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void PhoneNumber_Equality_SameNumbers_AreEqual()
    {
        // Arrange
        var phone1 = PhoneNumber.Create("+55", "11", "999887766");
        var phone2 = PhoneNumber.Create("55", "11", "999887766");

        // Assert
        phone1.Should().Be(phone2);
    }

    #endregion
}

