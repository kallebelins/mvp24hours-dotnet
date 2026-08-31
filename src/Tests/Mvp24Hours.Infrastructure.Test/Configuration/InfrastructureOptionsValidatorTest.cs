using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Configuration;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Security.Options;
using Mvp24Hours.Infrastructure.Sms.Options;

namespace Mvp24Hours.Infrastructure.Test.Configuration;

[Trait("Category", "Unit")]
public class InfrastructureOptionsValidatorTest
{
    private readonly InfrastructureOptionsValidator _validator = new();

    [Fact]
    public void Validate_WithNullOptions_ShouldFail()
    {
        ValidateOptionsResult result = _validator.Validate(null, null!);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("cannot be null");
    }

    [Fact]
    public void Validate_WithNoSubOptionsConfigured_ShouldSucceed()
    {
        var options = new InfrastructureOptions();

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithZeroHttpTimeout_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Http = new HttpClientOptions { Timeout = TimeSpan.Zero }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("timeout must be greater than zero");
    }

    [Fact]
    public void Validate_WithZeroMaxResponseContentBufferSize_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Http = new HttpClientOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                MaxResponseContentBufferSize = 0
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxResponseContentBufferSize must be greater than zero");
    }

    [Fact]
    public void Validate_WithValidHttpOptions_ShouldSucceed()
    {
        var options = new InfrastructureOptions
        {
            Http = new HttpClientOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                MaxResponseContentBufferSize = 1024
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidDefaultFromEmail_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Email = new EmailOptions { DefaultFrom = "not-an-email" }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultFrom must be a valid email address");
    }

    [Fact]
    public void Validate_WithInvalidDefaultReplyToEmail_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Email = new EmailOptions { DefaultReplyTo = "also not an email" }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DefaultReplyTo must be a valid email address");
    }

    [Fact]
    public void Validate_WithZeroMaxAttachmentSize_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Email = new EmailOptions { MaxAttachmentSize = 0 }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxAttachmentSize must be greater than zero");
    }

    [Fact]
    public void Validate_WithValidEmailAddresses_ShouldSucceed()
    {
        var options = new InfrastructureOptions
        {
            Email = new EmailOptions
            {
                DefaultFrom = "no-reply@example.com",
                DefaultReplyTo = "support@example.com",
                MaxAttachmentSize = 1024
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithSmsOptionsConfigured_ShouldSucceed()
    {
        // SMS options are provider-specific and validated by provider-specific validators;
        // the shared validator performs no checks here (asserting the no-op branch).
        var options = new InfrastructureOptions
        {
            Sms = new SmsOptions()
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithZeroFileStorageMaxFileSize_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            FileStorage = new FileStorageOptions { MaxFileSize = 0 }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("MaxFileSize must be greater than zero");
    }

    [Fact]
    public void Validate_WithPathTraversalInBasePath_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            FileStorage = new FileStorageOptions
            {
                MaxFileSize = 1024,
                BasePath = "../etc/passwd"
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("path traversal");
    }

    [Fact]
    public void Validate_WithValidFileStorageOptions_ShouldSucceed()
    {
        var options = new InfrastructureOptions
        {
            FileStorage = new FileStorageOptions
            {
                MaxFileSize = 1024,
                BasePath = "uploads/files"
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAzureKeyVaultMissingVaultUri_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Security = new SecurityOptions
            {
                AzureKeyVault = new AzureKeyVaultOptions { UseManagedIdentity = true }
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("VaultUri is required");
    }

    [Fact]
    public void Validate_WithAzureKeyVaultRelativeVaultUri_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Security = new SecurityOptions
            {
                AzureKeyVault = new AzureKeyVaultOptions
                {
                    VaultUri = new Uri("relative/path", UriKind.Relative),
                    UseManagedIdentity = true
                }
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("must be an absolute URI");
    }

    [Fact]
    public void Validate_WithAzureKeyVaultNotUsingManagedIdentityAndMissingCredentials_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Security = new SecurityOptions
            {
                AzureKeyVault = new AzureKeyVaultOptions
                {
                    VaultUri = new Uri("https://myvault.vault.azure.net"),
                    UseManagedIdentity = false
                }
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ClientId, ClientSecret, and TenantId");
    }

    [Fact]
    public void Validate_WithAzureKeyVaultUsingManagedIdentity_ShouldSucceed()
    {
        var options = new InfrastructureOptions
        {
            Security = new SecurityOptions
            {
                AzureKeyVault = new AzureKeyVaultOptions
                {
                    VaultUri = new Uri("https://myvault.vault.azure.net"),
                    UseManagedIdentity = true
                }
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAzureKeyVaultCredentialsProvided_ShouldSucceed()
    {
        var options = new InfrastructureOptions
        {
            Security = new SecurityOptions
            {
                AzureKeyVault = new AzureKeyVaultOptions
                {
                    VaultUri = new Uri("https://myvault.vault.azure.net"),
                    UseManagedIdentity = false,
                    ClientId = "client-id",
                    ClientSecret = "client-secret",
                    TenantId = "tenant-id"
                }
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAwsSecretsManagerMissingRegion_ShouldFail()
    {
        var options = new InfrastructureOptions
        {
            Security = new SecurityOptions
            {
                AwsSecretsManager = new AwsSecretsManagerOptions()
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Region is required");
    }

    [Fact]
    public void Validate_WithAwsSecretsManagerRegionProvided_ShouldSucceed()
    {
        var options = new InfrastructureOptions
        {
            Security = new SecurityOptions
            {
                AwsSecretsManager = new AwsSecretsManagerOptions { Region = "us-east-1" }
            }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleFailures_ShouldAggregateAllErrors()
    {
        var options = new InfrastructureOptions
        {
            Http = new HttpClientOptions { Timeout = TimeSpan.Zero },
            Email = new EmailOptions { MaxAttachmentSize = -1 },
            FileStorage = new FileStorageOptions { MaxFileSize = -1 }
        };

        ValidateOptionsResult result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("HTTP client timeout");
        result.FailureMessage.Should().Contain("MaxAttachmentSize");
        result.FailureMessage.Should().Contain("FileStorage MaxFileSize");
    }
}
