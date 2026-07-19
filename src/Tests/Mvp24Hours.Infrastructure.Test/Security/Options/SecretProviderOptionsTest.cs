//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Security.Options;

namespace Mvp24Hours.Infrastructure.Test.Security.Options;

[Trait("Category", "Unit")]
public class SecretProviderOptionsTest
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        var options = new SecretProviderOptions();

        options.EnableCaching.Should().BeFalse();
        options.CacheExpiration.Should().Be(TimeSpan.FromMinutes(5));
        options.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.ThrowOnNotFound.Should().BeFalse();
    }
}

[Trait("Category", "Unit")]
public class EnvironmentVariableOptionsTest
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        var options = new EnvironmentVariableOptions();

        options.Target.Should().Be(EnvironmentVariableTarget.Process);
        options.VariableNamePrefix.Should().BeNull();
        options.CaseSensitive.Should().BeFalse();
    }
}

[Trait("Category", "Unit")]
public class AwsSecretsManagerOptionsTest
{
    [Fact]
    public void Default_ShouldHaveNullOptionalFields()
    {
        var options = new AwsSecretsManagerOptions();

        options.Region.Should().BeNull();
        options.AccessKeyId.Should().BeNull();
        options.SecretAccessKey.Should().BeNull();
        options.SessionToken.Should().BeNull();
        options.SecretNamePrefix.Should().BeNull();
    }
}

[Trait("Category", "Unit")]
public class AzureKeyVaultOptionsTest
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        var options = new AzureKeyVaultOptions();

        options.VaultUri.Should().BeNull();
        options.VaultUriString.Should().BeNull();
        options.UseManagedIdentity.Should().BeFalse();
        options.TenantId.Should().BeNull();
        options.ClientId.Should().BeNull();
        options.ClientSecret.Should().BeNull();
        options.ManagedIdentityClientId.Should().BeNull();
    }

    [Fact]
    public void VaultUriString_WhenSet_ShouldParseUri()
    {
        var options = new AzureKeyVaultOptions
        {
            VaultUriString = "https://myvault.vault.azure.net/"
        };

        options.VaultUri.Should().Be(new Uri("https://myvault.vault.azure.net/"));
        options.VaultUriString.Should().Be("https://myvault.vault.azure.net/");
    }

    [Fact]
    public void VaultUriString_WhenWhitespace_ShouldClearUri()
    {
        var options = new AzureKeyVaultOptions
        {
            VaultUri = new Uri("https://myvault.vault.azure.net/"),
            VaultUriString = "   "
        };

        options.VaultUri.Should().BeNull();
    }
}
