//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Security.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Security.Helpers;

[Trait("Category", "Unit")]
public class SensitiveDataMaskerTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskPassword_WithNullOrEmpty_ShouldReturnEmpty(string? password)
    {
        SensitiveDataMasker.MaskPassword(password).Should().BeEmpty();
    }

    [Fact]
    public void MaskPassword_Default_ShouldMaskEntireValue()
    {
        SensitiveDataMasker.MaskPassword("MySecretPassword123")
            .Should().Be(new string('*', "MySecretPassword123".Length));
    }

    [Fact]
    public void MaskPassword_WithVisibleChars_ShouldShowSuffix()
    {
        SensitiveDataMasker.MaskPassword("password", visibleChars: 3)
            .Should().Be("*****ord");
    }

    [Fact]
    public void MaskPassword_WhenVisibleCharsExceedLength_ShouldMaskAll()
    {
        SensitiveDataMasker.MaskPassword("ab", visibleChars: 5)
            .Should().Be("**");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskApiKey_WithNullOrEmpty_ShouldReturnEmpty(string? apiKey)
    {
        SensitiveDataMasker.MaskApiKey(apiKey).Should().BeEmpty();
    }

    [Fact]
    public void MaskApiKey_Default_ShouldKeepPrefix()
    {
        SensitiveDataMasker.MaskApiKey("sk_live_1234567890abcdef")
            .Should().Be("sk_live" + new string('*', "sk_live_1234567890abcdef".Length - 7));
    }

    [Fact]
    public void MaskApiKey_WhenPrefixExceedsLength_ShouldMaskAll()
    {
        SensitiveDataMasker.MaskApiKey("short", prefixLength: 10)
            .Should().Be("*****");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskCreditCard_WithNullOrEmpty_ShouldReturnEmpty(string? card)
    {
        SensitiveDataMasker.MaskCreditCard(card).Should().BeEmpty();
    }

    [Fact]
    public void MaskCreditCard_ShouldShowLastFourDigits()
    {
        SensitiveDataMasker.MaskCreditCard("4111111111111111")
            .Should().Be("************1111");
    }

    [Fact]
    public void MaskCreditCard_WithSpacesAndDashes_ShouldCleanBeforeMasking()
    {
        SensitiveDataMasker.MaskCreditCard("4111-1111 1111-1111")
            .Should().Be("************1111");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskEmail_WithNullOrEmpty_ShouldReturnEmpty(string? email)
    {
        SensitiveDataMasker.MaskEmail(email).Should().BeEmpty();
    }

    [Fact]
    public void MaskEmail_ShouldMaskLocalPartKeepingFirstCharAndDomain()
    {
        SensitiveDataMasker.MaskEmail("john.doe@example.com")
            .Should().Be("j*******@example.com");
    }

    [Fact]
    public void MaskEmail_WithoutAt_ShouldMaskEntireValue()
    {
        SensitiveDataMasker.MaskEmail("not-an-email")
            .Should().Be(new string('*', "not-an-email".Length));
    }

    [Fact]
    public void MaskEmail_WithSingleCharLocalPart_ShouldKeepLocalPart()
    {
        SensitiveDataMasker.MaskEmail("a@example.com")
            .Should().Be("a@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MaskPhoneNumber_WithNullOrEmpty_ShouldReturnEmpty(string? phone)
    {
        SensitiveDataMasker.MaskPhoneNumber(phone).Should().BeEmpty();
    }

    [Fact]
    public void MaskPhoneNumber_WithoutDash_ShouldMaskDigits()
    {
        SensitiveDataMasker.MaskPhoneNumber("11987654321")
            .Should().Be("*******4321");
    }

    [Fact]
    public void MaskPhoneNumber_WithDash_ShouldPreserveDashBeforeLastFour()
    {
        SensitiveDataMasker.MaskPhoneNumber("11-98765-4321")
            .Should().Contain("-");
        SensitiveDataMasker.MaskPhoneNumber("11-98765-4321")
            .Should().EndWith("4321");
    }

    [Fact]
    public void MaskPattern_WithNullInput_ShouldReturnEmpty()
    {
        SensitiveDataMasker.MaskPattern(null, @"\d+").Should().BeEmpty();
    }

    [Fact]
    public void MaskPattern_WithEmptyPattern_ShouldReturnInputUnchanged()
    {
        SensitiveDataMasker.MaskPattern("MyValue123", "   ")
            .Should().Be("MyValue123");
    }

    [Fact]
    public void MaskPattern_ShouldReplaceMatches()
    {
        SensitiveDataMasker.MaskPattern("MyValue123", @"\d+", '#')
            .Should().Be("MyValue###");
    }

    [Fact]
    public void MaskDictionary_WithNull_ShouldReturnEmptyDictionary()
    {
        SensitiveDataMasker.MaskDictionary(null!, ["password"])
            .Should().BeEmpty();
    }

    [Fact]
    public void MaskDictionary_ShouldMaskSensitiveKeysCaseInsensitive()
    {
        var data = new Dictionary<string, string?>
        {
            ["Password"] = "secret123",
            ["User"] = "alice"
        };

        IDictionary<string, string?> masked = SensitiveDataMasker.MaskDictionary(data, ["password"]);

        masked["Password"].Should().Be(new string('*', "secret123".Length));
        masked["User"].Should().Be("alice");
    }

    [Fact]
    public void MaskJson_WithNull_ShouldReturnEmpty()
    {
        SensitiveDataMasker.MaskJson(null, ["password"]).Should().BeEmpty();
    }

    [Fact]
    public void MaskJson_ShouldMaskSensitiveValuesCaseInsensitive()
    {
        const string json = """{"Password":"secret123","user":"alice"}""";

        string masked = SensitiveDataMasker.MaskJson(json, ["password"]);

        masked.Should().Contain("\"password\": \"" + new string('*', "secret123".Length) + "\"");
        masked.Should().Contain("\"user\":\"alice\"");
    }
}
