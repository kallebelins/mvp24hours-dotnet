using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Security;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Security;

[Trait("Category", "Unit")]
public class SecurityTest
{
    [Fact]
    public void AesFieldEncryptor_ShouldRoundTripStringAndBytes()
    {
        byte[] key = AesFieldEncryptor.GenerateKey();
        using var encryptor = new AesFieldEncryptor(key);

        string plain = "sensitive-value-123";
        string? encrypted = encryptor.Encrypt(plain);
        encrypted.Should().NotBeNullOrEmpty().And.NotBe(plain);

        encryptor.Decrypt(encrypted).Should().Be(plain);
        encryptor.Encrypt(null).Should().BeNull();
        encryptor.Decrypt(null).Should().BeNull();

        byte[] data = "binary-data"u8.ToArray();
        byte[]? encryptedBytes = encryptor.EncryptBytes(data);
        encryptor.DecryptBytes(encryptedBytes).Should().BeEquivalentTo(data);
    }

    [Fact]
    public void AesFieldEncryptor_FromBase64Key_ShouldCreateInstance()
    {
        string base64 = AesFieldEncryptor.GenerateKeyAsBase64();
        using var encryptor = AesFieldEncryptor.FromBase64Key(base64);
        encryptor.Encrypt("test").Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AesFieldEncryptor_ShouldValidateKeyLength()
    {
        Action shortKey = () => _ = new AesFieldEncryptor(new byte[16]);
        shortKey.Should().Throw<ArgumentException>();

        Action nullKey = () => _ = new AesFieldEncryptor(null!);
        nullKey.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EncryptedStringSerializer_ShouldEncryptOnWriteAndDecryptOnRead()
    {
        byte[] key = AesFieldEncryptor.GenerateKey();
        IFieldEncryptor encryptor = new AesFieldEncryptor(key);
        var serializer = new EncryptedStringSerializer(encryptor);

        using var stream = new MemoryStream();
        using IBsonWriter writer = new BsonBinaryWriter(stream);
        writer.WriteStartDocument();
        var writeContext = BsonSerializationContext.CreateRoot(writer);
        writer.WriteName("secret");
        serializer.Serialize(writeContext, default, "secret");
        writer.WriteEndDocument();

        stream.Position = 0;
        using IBsonReader reader = new BsonBinaryReader(stream);
        reader.ReadStartDocument();
        reader.ReadName();
        var readContext = BsonDeserializationContext.CreateRoot(reader);
        string decrypted = serializer.Deserialize(readContext, default);
        reader.ReadEndDocument();

        decrypted.Should().Be("secret");
    }

    [Fact]
    public void EncryptionKeyHelper_ShouldDeriveKeysAndSalt()
    {
        byte[] salt = EncryptionKeyHelper.GenerateSalt();
        byte[] derived = EncryptionKeyHelper.DeriveKeyFromPassword("password", salt);
        derived.Should().HaveCount(32);

        byte[] master = AesFieldEncryptor.GenerateKey();
        byte[] tenantKey = EncryptionKeyHelper.DerivePerTenantKey(master, "tenant-a");
        tenantKey.Should().HaveCount(32);

        Action shortSalt = () => EncryptionKeyHelper.DeriveKeyFromPassword("pwd", new byte[8]);
        shortSalt.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MongoDbAuthenticationOptions_ShouldCreateScramSha256Credential()
    {
        var options = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.ScramSha256,
            Username = "appuser",
            Password = "secret",
            AuthDatabase = "admin"
        };

        MongoCredential? credential = options.CreateCredential();
        credential.Should().NotBeNull();
        credential!.Mechanism.Should().Be("SCRAM-SHA-256");

        var settings = new MongoClientSettings();
        options.ApplyTo(settings);
        settings.Credential.Should().NotBeNull();
    }

    [Fact]
    public void MongoDbAuthenticationOptions_Default_ShouldReturnNullCredentialWithoutUsername()
    {
        var options = new MongoDbAuthenticationOptions();
        options.CreateCredential().Should().BeNull();
    }

    [Fact]
    public void MongoDbAuthenticationOptions_ScramSha1_ShouldRequireUsername()
    {
        var options = new MongoDbAuthenticationOptions { Mechanism = MongoDbAuthMechanism.ScramSha1 };
        Action act = () => options.CreateCredential();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MongoDbAuthenticationExtensions_ShouldConfigureSettings()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");

        settings.WithScramSha256("user", "pass").Credential.Should().NotBeNull();
        settings.WithAuthentication(new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.Ldap,
            Username = "ldap-user",
            Password = "pwd"
        }).Credential.Should().NotBeNull();
    }

    [Fact]
    public void MongoDbAuthenticationOptions_X509_ShouldEnableTlsWithCertificate()
    {
        using X509Certificate2 certificate = CreateSelfSignedCertificate();
        var options = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.X509,
            Certificate = certificate,
            ValidateServerCertificate = false
        };

        var settings = new MongoClientSettings();
        options.ApplyTo(settings);

        settings.UseTls.Should().BeTrue();
        settings.SslSettings.ClientCertificates.Should().ContainSingle();
        settings.Credential.Should().NotBeNull();
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=MongoDbTest",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
