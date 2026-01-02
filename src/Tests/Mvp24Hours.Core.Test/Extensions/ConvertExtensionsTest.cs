namespace Mvp24Hours.Core.Test.Extensions;

/// <summary>
/// Testes unitários para ConvertExtensions (métodos existentes).
/// </summary>
public class ConvertExtensionsTest
{
    #region [ ToBase64 / From64 Tests ]

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Test String 123")]
    [InlineData("Special @#$%")]
    public void ToBase64_From64_RoundTrip_ReturnsOriginal(string original)
    {
        // Act
        var base64 = original.ToBase64();
        var decoded = base64.From64();

        // Assert
        decoded.Should().Be(original);
    }

    [Fact]
    public void ToBase64_WithUnicode_EncodesCorrectly()
    {
        // Arrange
        var input = "José 世界 🌍";

        // Act
        var base64 = input.ToBase64();
        var decoded = base64.From64();

        // Assert
        decoded.Should().Be(input);
    }

    [Fact]
    public void ToBase64_WithDifferentEncoding_UsesSpecifiedEncoding()
    {
        // Arrange
        var input = "Hello";
        var encoding = System.Text.Encoding.ASCII;

        // Act
        var base64 = input.ToBase64(encoding);
        var decoded = base64.From64(encoding);

        // Assert
        decoded.Should().Be(input);
    }

    #endregion

    #region [ ToEnum Tests ]

    [Theory]
    [InlineData("Active", TestEnum.Active)]
    [InlineData("Inactive", TestEnum.Inactive)]
    [InlineData("Pending", TestEnum.Pending)]
    [InlineData("active", TestEnum.Active)] // Case insensitive
    [InlineData("ACTIVE", TestEnum.Active)]
    public void ToEnum_WithValidString_ReturnsEnumValue(string input, TestEnum expected)
    {
        // Act
        var result = input.ToEnum<TestEnum>();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Invalid")]
    [InlineData("Unknown")]
    public void ToEnum_WithInvalidString_ReturnsDefault(string input)
    {
        // Act
        var result = input.ToEnum<TestEnum>();

        // Assert
        result.Should().Be(default(TestEnum));
    }

    [Fact]
    public void ToEnum_WithCustomDefault_ReturnsCustomDefault()
    {
        // Arrange
        var input = "Invalid";

        // Act
        var result = input.ToEnum(TestEnum.Pending);

        // Assert
        result.Should().Be(TestEnum.Pending);
    }

    #endregion

    #region [ ToInt Tests ]

    [Theory]
    [InlineData("123", 123)]
    [InlineData("0", 0)]
    [InlineData("-456", -456)]
    [InlineData("2147483647", int.MaxValue)]
    public void ToInt_WithValidString_ReturnsInteger(string input, int expected)
    {
        // Act
        var result = input.ToInt();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("12.34")]
    public void ToInt_WithInvalidString_ReturnsNull(string? input)
    {
        // Act
        var result = input.ToInt();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToInt_WithInvalidStringAndDefault_ReturnsDefault()
    {
        // Arrange
        var input = "abc";

        // Act
        var result = input.ToInt(-1);

        // Assert
        result.Should().Be(-1);
    }

    #endregion

    #region [ ToLong Tests ]

    [Theory]
    [InlineData("123456789", 123456789L)]
    [InlineData("0", 0L)]
    [InlineData("-987654321", -987654321L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    public void ToLong_WithValidString_ReturnsLong(string input, long expected)
    {
        // Act
        var result = input.ToLong();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    public void ToLong_WithInvalidString_ReturnsNull(string? input)
    {
        // Act
        var result = input.ToLong();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region [ ToBoolean Tests ]

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("FALSE", false)]
    public void ToBoolean_WithValidString_ReturnsBool(string input, bool expected)
    {
        // Act
        var result = input.ToBoolean();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("1")]
    [InlineData("0")]
    public void ToBoolean_WithInvalidString_ReturnsNull(string? input)
    {
        // Act
        var result = input.ToBoolean();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region [ ToDecimal Tests ]

    [Theory]
    [InlineData("123.45", 123.45)]
    [InlineData("0", 0)]
    [InlineData("-456.78", -456.78)]
    public void ToDecimal_WithValidString_ReturnsDecimal(string input, double expectedDouble)
    {
        // Arrange
        var expected = (decimal)expectedDouble;

        // Act
        var result = input.ToDecimal();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    public void ToDecimal_WithInvalidString_ReturnsNull(string? input)
    {
        // Act
        var result = input.ToDecimal();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region [ ToDateTime Tests ]

    [Fact]
    public void ToDateTime_WithValidString_ReturnsDateTime()
    {
        // Arrange
        var input = "2025-01-02";

        // Act
        var result = input.ToDateTime();

        // Assert
        result.Should().NotBeNull();
        result!.Value.Year.Should().Be(2025);
        result.Value.Month.Should().Be(1);
        result.Value.Day.Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid date")]
    public void ToDateTime_WithInvalidString_ReturnsNull(string? input)
    {
        // Act
        var result = input.ToDateTime();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToDateTime_WithCustomDefault_ReturnsCustomDefault()
    {
        // Arrange
        var input = "invalid";
        var defaultDate = new DateTime(2020, 1, 1);

        // Act
        var result = input.ToDateTime(defaultDate);

        // Assert
        result.Should().Be(defaultDate);
    }

    #endregion

    #region [ NullSafe Tests ]

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  hello  ", "hello")]
    [InlineData("test", "test")]
    public void NullSafe_ReturnsNonNullTrimmedString(string? input, string expected)
    {
        // Act
        var result = input.NullSafe();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region [ Replace Multiple Tests ]

    [Fact]
    public void Replace_WithMultipleOldValues_ReplacesAll()
    {
        // Arrange
        var input = "Hello World, Welcome World";
        var oldValues = new List<string> { "Hello", "Welcome" };

        // Act
        var result = input.Replace(oldValues, "Hi");

        // Assert
        result.Should().Be("Hi World, Hi World");
    }

    #endregion

    #region [ OnlyNumbers Tests ]

    [Theory]
    [InlineData("abc123def456", "123456")]
    [InlineData("test!@#123", "123")]
    [InlineData("12345", "12345")]
    [InlineData("abcdef", "")]
    public void OnlyNumbers_ReturnsOnlyDigits(string input, string expected)
    {
        // Act
        var result = input.OnlyNumbers();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void OnlyNumbers_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.OnlyNumbers();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region [ OnlyNumbersLetters Tests ]

    [Theory]
    [InlineData("abc123!@#", "abc123")]
    [InlineData("test@#$456", "test456")]
    [InlineData("Hello123World", "Hello123World")]
    public void OnlyNumbersLetters_ReturnsAlphanumeric(string input, string expected)
    {
        // Act
        var result = input.OnlyNumbersLetters();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region [ OnlyLetters Tests ]

    [Theory]
    [InlineData("abc123", "abc")]
    [InlineData("test@#$456", "test")]
    [InlineData("Hello123World", "HelloWorld")]
    public void OnlyLetters_ReturnsOnlyLetters(string input, string expected)
    {
        // Act
        var result = input.OnlyLetters();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region [ RemoveDiacritics Tests ]

    [Theory]
    [InlineData("Olá Mundo", "Ola Mundo")]
    [InlineData("Àçênto", "Acento")]
    [InlineData("Café", "Cafe")]
    [InlineData("Zürich", "Zurich")]
    public void RemoveDiacritics_RemovesAccents(string input, string expected)
    {
        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RemoveDiacritics_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? input = null;

        // Act
        var result = input.RemoveDiacritics();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region [ ReplaceSpecialChar Tests ]

    [Theory]
    [InlineData("Olá", "Ola")]
    [InlineData("Café", "Cafe")]
    public void ReplaceSpecialChar_RemovesDiacritics(string input, string expected)
    {
        // Act
        var result = input.ReplaceSpecialChar();

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region [ Hash Tests ]

    [Fact]
    public void GetSHA256Hash_GeneratesConsistentHash()
    {
        // Arrange
        var input = "test string";

        // Act
        var hash1 = input.GetSHA256Hash();
        var hash2 = input.GetSHA256Hash();

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetSHA512Hash_GeneratesConsistentHash()
    {
        // Arrange
        var input = "test string";

        // Act
        var hash1 = input.GetSHA512Hash();
        var hash2 = input.GetSHA512Hash();

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetSHA256Hash_DifferentInputs_DifferentHashes()
    {
        // Arrange
        var input1 = "test1";
        var input2 = "test2";

        // Act
        var hash1 = input1.GetSHA256Hash();
        var hash2 = input2.GetSHA256Hash();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region [ Zip / UnZip Tests ]

    [Theory]
    [InlineData("Hello World")]
    [InlineData("This is a longer string that should compress better")]
    public void ZipBase64_UnZipBase64_RoundTrip_ReturnsOriginal(string original)
    {
        // Act
        var zipped = original.ZipBase64();
        var unzipped = zipped.UnZipBase64();

        // Assert
        unzipped.Should().Be(original);
    }

    [Fact]
    public void ZipByte_UnZip_RoundTrip_ReturnsOriginal()
    {
        // Arrange
        var original = "Test string for compression";

        // Act
        var zipped = original.ZipByte();
        var unzipped = zipped.UnZip();

        // Assert
        unzipped.Should().Be(original);
    }

    [Fact]
    public void ZipBase64_CompressesLongStrings()
    {
        // Arrange
        var longString = new string('a', 1000);

        // Act
        var zipped = longString.ZipBase64();

        // Assert
        zipped.Length.Should().BeLessThan(longString.Length);
    }

    #endregion

    #region [ Performance Tests ]

    [Fact]
    public void ConvertExtensions_WithManyConversions_ShouldPerformEfficiently()
    {
        // Arrange
        var inputs = Enumerable.Range(1, 10000).Select(i => i.ToString()).ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = inputs.Select(x => x.ToInt()).ToList();
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(10000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region [ Helper Enums ]

    public enum TestEnum
    {
        Active = 0,
        Inactive = 1,
        Pending = 2
    }

    #endregion
}
