//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net.Security;
using System.Security.Authentication;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Security;
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Security;

[Trait("Category", "Unit")]
public class SecurityTest
{
    #region [ MongoDbAuthMechanism ]

    [Fact]
    public void MongoDbAuthMechanism_HasExpectedValues()
    {
        MongoDbAuthMechanism[] values = Enum.GetValues<MongoDbAuthMechanism>();
        Assert.Contains(MongoDbAuthMechanism.Default, values);
        Assert.Contains(MongoDbAuthMechanism.ScramSha1, values);
        Assert.Contains(MongoDbAuthMechanism.ScramSha256, values);
        Assert.Contains(MongoDbAuthMechanism.X509, values);
        Assert.Contains(MongoDbAuthMechanism.AwsIam, values);
        Assert.Contains(MongoDbAuthMechanism.Ldap, values);
        Assert.Contains(MongoDbAuthMechanism.Gssapi, values);
    }

    #endregion

    #region [ MongoDbAuthenticationOptions - Defaults ]

    [Fact]
    public void MongoDbAuthenticationOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbAuthenticationOptions();

        Assert.Equal(MongoDbAuthMechanism.Default, opts.Mechanism);
        Assert.Null(opts.Username);
        Assert.Null(opts.Password);
        Assert.Equal("admin", opts.AuthDatabase);
        Assert.Null(opts.CertificatePath);
        Assert.Null(opts.CertificatePassword);
        Assert.Null(opts.Certificate);
        Assert.Null(opts.CaCertificatePath);
        Assert.True(opts.ValidateServerCertificate);
        Assert.Null(opts.AwsAccessKeyId);
        Assert.Null(opts.AwsSecretAccessKey);
        Assert.Null(opts.AwsSessionToken);
        Assert.Null(opts.LdapBindDn);
        Assert.Null(opts.KerberosServiceName);
        Assert.True(opts.AllowedTlsProtocols.HasFlag(SslProtocols.Tls12));
        Assert.True(opts.AllowedTlsProtocols.HasFlag(SslProtocols.Tls13));
    }

    [Fact]
    public void MongoDbAuthenticationOptions_CanAssignProperties()
    {
        var opts = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.ScramSha256,
            Username = "admin",
            Password = "secret",
            AuthDatabase = "mydb",
            CertificatePath = "/path/cert.pem",
            CertificatePassword = "pass",
            CaCertificatePath = "/path/ca.pem",
            ValidateServerCertificate = false,
            AwsAccessKeyId = "AKID",
            AwsSecretAccessKey = "secret",
            AwsSessionToken = "token",
            LdapBindDn = "cn=user",
            KerberosServiceName = "mongodb"
        };

        Assert.Equal(MongoDbAuthMechanism.ScramSha256, opts.Mechanism);
        Assert.Equal("admin", opts.Username);
        Assert.Equal("secret", opts.Password);
        Assert.Equal("mydb", opts.AuthDatabase);
        Assert.Equal("/path/cert.pem", opts.CertificatePath);
        Assert.Equal("pass", opts.CertificatePassword);
        Assert.Equal("/path/ca.pem", opts.CaCertificatePath);
        Assert.False(opts.ValidateServerCertificate);
        Assert.Equal("AKID", opts.AwsAccessKeyId);
        Assert.Equal("secret", opts.AwsSecretAccessKey);
        Assert.Equal("token", opts.AwsSessionToken);
        Assert.Equal("cn=user", opts.LdapBindDn);
        Assert.Equal("mongodb", opts.KerberosServiceName);
    }

    #endregion

    #region [ MongoDbAuthenticationOptions - CreateCredential ]

    [Fact]
    public void CreateCredential_Default_WithNoUsername_ReturnsNull()
    {
        var opts = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.Default
        };

        MongoCredential? credential = opts.CreateCredential();
        Assert.Null(credential);
    }

    [Fact]
    public void CreateCredential_Default_WithUsername_ReturnsCredential()
    {
        var opts = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.Default,
            Username = "user",
            Password = "pass"
        };

        MongoCredential? credential = opts.CreateCredential();
        Assert.NotNull(credential);
    }

    [Fact]
    public void CreateCredential_ScramSha256_WithoutUsername_Throws()
    {
        var opts = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.ScramSha256
        };

        Assert.Throws<InvalidOperationException>(() => opts.CreateCredential());
    }

    [Fact]
    public void CreateCredential_Ldap_WithoutUsername_Throws()
    {
        var opts = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.Ldap
        };

        Assert.Throws<InvalidOperationException>(() => opts.CreateCredential());
    }

    [Fact]
    public void CreateCredential_ScramSha1_WithoutUsername_Throws()
    {
        var opts = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.ScramSha1
        };

        Assert.Throws<InvalidOperationException>(() => opts.CreateCredential());
    }

    [Fact]
    public void CreateCredential_AwsIam_WithNoCredentials_CreatesExternalCredential()
    {
        var opts = new MongoDbAuthenticationOptions
        {
            Mechanism = MongoDbAuthMechanism.AwsIam
        };

        MongoCredential? credential = opts.CreateCredential();
        Assert.NotNull(credential);
    }

    [Fact]
    public void ApplyTo_NullSettings_Throws()
    {
        var opts = new MongoDbAuthenticationOptions();
        Assert.Throws<ArgumentNullException>(() => opts.ApplyTo(null!));
    }

    #endregion
}
