//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Security.Options;
using Mvp24Hours.Infrastructure.Security.Providers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Security.Providers;

/// <summary>
/// Azure Key Vault uses a non-injectable SecretClient.
/// Tests cover constructor guards, credential validation, and argument validation.
/// </summary>
[Trait("Category", "Unit")]
public class AzureKeyVaultSecretProviderTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AzureKeyVaultSecretProvider((AzureKeyVaultOptions)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullIOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AzureKeyVaultSecretProvider((IOptions<AzureKeyVaultOptions>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullVaultUri_ShouldThrowInvalidOperationException()
    {
        Action act = () => _ = new AzureKeyVaultSecretProvider(
            SecurityTestHelpers.CreateAzureOptions(vaultUri: null));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Vault URI*");
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldCreateInstance()
    {
        AzureKeyVaultSecretProvider provider = new(
            SecurityTestHelpers.CreateAzureOptions(useManagedIdentity: true));

        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithIOptionsAndLogger_ShouldCreateInstance()
    {
        var logger = new Mock<ILogger<AzureKeyVaultSecretProvider>>();

        AzureKeyVaultSecretProvider provider = new(
            SecurityTestHelpers.AsOptions(SecurityTestHelpers.CreateAzureOptions(useManagedIdentity: true)),
            logger.Object);

        provider.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        AzureKeyVaultSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretAsync(name!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretsAsync_WithNull_ShouldThrowArgumentNullException()
    {
        AzureKeyVaultSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("secretNames");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretVersionAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        AzureKeyVaultSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretVersionAsync(name!, "v1");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretVersionAsync_WithInvalidVersion_ShouldThrowArgumentException(string? version)
    {
        AzureKeyVaultSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretVersionAsync("ApiKey", version!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("version");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SecretExistsAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        AzureKeyVaultSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.SecretExistsAsync(name!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretAsync_WithoutCredentials_ShouldThrowInvalidOperationException()
    {
        AzureKeyVaultSecretProvider provider = new(SecurityTestHelpers.CreateAzureOptions(
            useManagedIdentity: false,
            tenantId: null,
            clientId: null,
            clientSecret: null));

        Func<Task> act = () => provider.GetSecretAsync("ApiKey");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Managed Identity or Client Secret*");
    }

    private static AzureKeyVaultSecretProvider CreateProvider()
    {
        return new(SecurityTestHelpers.CreateAzureOptions(useManagedIdentity: true));
    }
}
