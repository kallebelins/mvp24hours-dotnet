//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Security.Cryptography;
using Mvp24Hours.Infrastructure.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Security.Helpers;

[Trait("Category", "Unit")]
public class EncryptionHelperTest
{
    [Fact]
    public void CreateKeyBase64_Default_ShouldReturn32ByteKey()
    {
        string key = EncryptionHelper.CreateKeyBase64();

        key.Should().NotBeNullOrWhiteSpace();
        Convert.FromBase64String(key).Should().HaveCount(32);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void CreateKeyBase64_WithKeySize_ShouldReturnRequestedLength(int keySizeInBytes)
    {
        string key = EncryptionHelper.CreateKeyBase64(keySizeInBytes);

        Convert.FromBase64String(key).Should().HaveCount(keySizeInBytes);
    }

    [Fact]
    public void CreateKeyBase64_ShouldGenerateUniqueKeys()
    {
        string key1 = EncryptionHelper.CreateKeyBase64();
        string key2 = EncryptionHelper.CreateKeyBase64();

        key1.Should().NotBe(key2);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ShouldRestorePlainText()
    {
        string key = EncryptionHelper.CreateKeyBase64();
        const string plainText = "Hello Mvp24Hours!";

        string cipher = EncryptionHelper.EncryptWithAes(plainText, key, out string iv);
        string decrypted = EncryptionHelper.DecryptWithAes(cipher, key, iv);

        decrypted.Should().Be(plainText);
        cipher.Should().NotBe(plainText);
        iv.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void EncryptWithAes_EmptyPlainText_ShouldRoundTrip()
    {
        string key = EncryptionHelper.CreateKeyBase64();

        string cipher = EncryptionHelper.EncryptWithAes(string.Empty, key, out string iv);
        string decrypted = EncryptionHelper.DecryptWithAes(cipher, key, iv);

        decrypted.Should().BeEmpty();
    }

    [Fact]
    public void EncryptWithAes_SamePlainText_ShouldProduceDifferentIvAndCipher()
    {
        string key = EncryptionHelper.CreateKeyBase64();
        const string plainText = "same-payload";

        string cipher1 = EncryptionHelper.EncryptWithAes(plainText, key, out string iv1);
        string cipher2 = EncryptionHelper.EncryptWithAes(plainText, key, out string iv2);

        iv1.Should().NotBe(iv2);
        cipher1.Should().NotBe(cipher2);
    }

    [Fact]
    public void DecryptWithAes_WithWrongKey_ShouldThrowCryptographicException()
    {
        string key = EncryptionHelper.CreateKeyBase64();
        string wrongKey = EncryptionHelper.CreateKeyBase64();
        string cipher = EncryptionHelper.EncryptWithAes("secret", key, out string iv);

        Action act = () => EncryptionHelper.DecryptWithAes(cipher, wrongKey, iv);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void DecryptWithAes_WithWrongIv_ShouldThrowCryptographicException()
    {
        string key = EncryptionHelper.CreateKeyBase64();
        string cipher = EncryptionHelper.EncryptWithAes("secret", key, out _);
        EncryptionHelper.EncryptWithAes("other", key, out string otherIv);

        Action act = () => EncryptionHelper.DecryptWithAes(cipher, key, otherIv);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void EncryptWithAes_WithInvalidBase64Key_ShouldThrowFormatException()
    {
        Action act = () => EncryptionHelper.EncryptWithAes("text", "not-base64!!!", out _);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void DecryptWithAes_WithInvalidBase64Cipher_ShouldThrowFormatException()
    {
        string key = EncryptionHelper.CreateKeyBase64();
        EncryptionHelper.EncryptWithAes("text", key, out string iv);

        Action act = () => EncryptionHelper.DecryptWithAes("%%%invalid%%%", key, iv);

        act.Should().Throw<FormatException>();
    }
}
