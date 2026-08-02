using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.EFCore.Converters;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Converters;

[Trait("Category", "Unit")]
public class EncryptedValueConvertersTest
{
    private static Mock<IEncryptionProvider> CreateEncryptionMock()
    {
        var mock = new Mock<IEncryptionProvider>();
        mock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(plain => $"enc:{plain}");
        mock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(cipher => cipher["enc:".Length..]);
        return mock;
    }

    private static Mock<IExtendedEncryptionProvider> CreateExtendedEncryptionMock()
    {
        var mock = new Mock<IExtendedEncryptionProvider>();
        mock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(plain => $"enc:{plain}");
        mock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(cipher => cipher["enc:".Length..]);
        mock.Setup(x => x.Encrypt(It.IsAny<byte[]>())).Returns<byte[]>(data => [.. data.Select(b => (byte)(b + 1))]);
        mock.Setup(x => x.Decrypt(It.IsAny<byte[]>())).Returns<byte[]>(data => [.. data.Select(b => (byte)(b - 1))]);
        return mock;
    }

    [Fact]
    public void EncryptedStringConverter_ShouldRoundTripThroughProvider()
    {
        Mock<IEncryptionProvider> encryption = CreateEncryptionMock();
        var converter = new EncryptedStringConverter(encryption.Object);

        string cipher = (string)converter.ConvertToProvider("secret")!;
        string plain = (string)converter.ConvertFromProvider(cipher)!;

        cipher.Should().Be("enc:secret");
        plain.Should().Be("secret");
        encryption.Verify(x => x.Encrypt("secret"), Times.Once);
        encryption.Verify(x => x.Decrypt("enc:secret"), Times.Once);
    }

    [Fact]
    public void EncryptedStringConverter_Create_ShouldReturnConfiguredConverter()
    {
        Mock<IEncryptionProvider> encryption = CreateEncryptionMock();

        var converter = EncryptedStringConverter.Create(encryption.Object);

        ((string)converter.ConvertFromProvider(converter.ConvertToProvider("x")!)!).Should().Be("x");
    }

    [Fact]
    public void EncryptedStringConverter_WithNullProvider_ShouldThrow()
    {
        Action act = () => new EncryptedStringConverter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void NullableEncryptedStringConverter_ShouldPassThroughNullOrEmpty()
    {
        Mock<IEncryptionProvider> encryption = CreateEncryptionMock();
        var converter = new NullableEncryptedStringConverter(encryption.Object);

        converter.ConvertToProvider(null!).Should().BeNull();
        converter.ConvertFromProvider(string.Empty).Should().Be(string.Empty);
        ((string)converter.ConvertFromProvider(converter.ConvertToProvider("value")!)!).Should().Be("value");
    }

    [Fact]
    public void EncryptedBinaryConverter_ShouldRoundTripBytes()
    {
        Mock<IExtendedEncryptionProvider> encryption = CreateExtendedEncryptionMock();
        var converter = new EncryptedBinaryConverter(encryption.Object);
        byte[] original = [1, 2, 3, 4];

        byte[]? cipher = (byte[]?)converter.ConvertToProvider(original);
        byte[]? plain = (byte[]?)converter.ConvertFromProvider(cipher);

        plain.Should().Equal(original);
    }

    [Fact]
    public void EncryptedJsonConverter_ShouldRoundTripObject()
    {
        Mock<IEncryptionProvider> encryption = CreateEncryptionMock();
        var converter = new EncryptedJsonConverter<SamplePayload>(encryption.Object);
        var payload = new SamplePayload { Name = "Test", Count = 3 };

        string? cipher = (string?)converter.ConvertToProvider(payload);
        var roundTrip = (SamplePayload?)converter.ConvertFromProvider(cipher);

        roundTrip!.Name.Should().Be("Test");
        roundTrip.Count.Should().Be(3);
    }

    [Fact]
    public void HasEncryptedConversion_Extension_ShouldConfigurePropertyBuilder()
    {
        Mock<IEncryptionProvider> encryption = CreateEncryptionMock();
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<EncryptedEntity>();

        modelBuilder.Entity<EncryptedEntity>()
            .Property(e => e.Secret)
            .HasEncryptedConversion(encryption.Object);

        modelBuilder.FinalizeModel();
        IMutableProperty property = modelBuilder.Model.FindEntityType(typeof(EncryptedEntity))!
            .FindProperty(nameof(EncryptedEntity.Secret))!;

        property.GetValueConverter().Should().NotBeNull();
    }

    [Fact]
    public void ApplyEncryptedConverters_ShouldReturnModelBuilderWithoutThrowing()
    {
        Mock<IEncryptionProvider> encryption = CreateEncryptionMock();
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<EncryptedEntity>(entity =>
        {
            entity.Property(e => e.Secret);
            entity.Property(e => e.Plain);
        });

        ModelBuilder result = modelBuilder.ApplyEncryptedConverters(encryption.Object);

        result.Should().BeSameAs(modelBuilder);
    }

    [Fact]
    public void ApplyEncryptedConverters_WithNullProvider_ShouldThrow()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<EncryptedEntity>();

        Action act = () => modelBuilder.ApplyEncryptedConverters(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class SamplePayload
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private sealed class EncryptedEntity
    {
        public int Id { get; set; }

        [Encrypted]
        public string Secret { get; set; } = string.Empty;

        public string Plain { get; set; } = string.Empty;
    }
}
