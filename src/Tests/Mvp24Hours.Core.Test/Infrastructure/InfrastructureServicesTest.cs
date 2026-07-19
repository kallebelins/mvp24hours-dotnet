using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
using Mvp24Hours.Core.Infrastructure.Security;

namespace Mvp24Hours.Core.Test.Infrastructure;

[Trait("Category", "Unit")]
public class InfrastructureServicesTest
{
    [Fact]
    public void ClockAdapter_WrapsIClockAsTimeProvider()
    {
        var clock = new TestClock(new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        var adapter = new ClockAdapter(clock);

        adapter.GetUtcNow().UtcDateTime.Should().Be(clock.UtcNow);
        adapter.Clock.Should().BeSameAs(clock);
    }

    [Fact]
    public void AesEncryptionProvider_RoundTripsPlainText()
    {
        string key = AesEncryptionProvider.GenerateKey();
        using var provider = AesEncryptionProvider.CreateFromKey(key);

        string encrypted = provider.Encrypt("secret-value");
        string decrypted = provider.Decrypt(encrypted);

        encrypted.Should().NotBe("secret-value");
        decrypted.Should().Be("secret-value");
    }

    [Fact]
    public void AesEncryptionProvider_ComputeBlindIndex_IsDeterministic()
    {
        string key = AesEncryptionProvider.GenerateKey();
        using var provider = AesEncryptionProvider.CreateFromKey(key);

        string index1 = provider.ComputeBlindIndex("Test@Example.com");
        string index2 = provider.ComputeBlindIndex("test@example.com");

        index1.Should().Be(index2);
    }

    [Fact]
    public void AesEncryptionProvider_InvalidKey_Throws()
    {
        Action act = () => _ = new AesEncryptionProvider(new EncryptionOptions { Key = Convert.ToBase64String(new byte[16]) });

        act.Should().Throw<ArgumentException>();
    }
}
