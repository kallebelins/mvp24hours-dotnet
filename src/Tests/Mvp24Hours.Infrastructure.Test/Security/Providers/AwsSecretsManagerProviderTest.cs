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
/// AWS Secrets Manager uses a non-injectable SDK client.
/// Tests cover constructor guards and argument validation (before network calls).
/// </summary>
[Trait("Category", "Unit")]
public class AwsSecretsManagerProviderTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AwsSecretsManagerProvider((AwsSecretsManagerOptions)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullIOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new AwsSecretsManagerProvider((IOptions<AwsSecretsManagerOptions>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldCreateInstance()
    {
        AwsSecretsManagerProvider provider = new(SecurityTestHelpers.CreateAwsOptions());

        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithIOptionsAndLogger_ShouldCreateInstance()
    {
        var logger = new Mock<ILogger<AwsSecretsManagerProvider>>();

        AwsSecretsManagerProvider provider = new(
            SecurityTestHelpers.AsOptions(SecurityTestHelpers.CreateAwsOptions()),
            logger.Object);

        provider.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        AwsSecretsManagerProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretAsync(name!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretsAsync_WithNull_ShouldThrowArgumentNullException()
    {
        AwsSecretsManagerProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("secretNames");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretVersionAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        AwsSecretsManagerProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretVersionAsync(name!, "v1");

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretVersionAsync_WithInvalidVersion_ShouldThrowArgumentException(string? version)
    {
        AwsSecretsManagerProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretVersionAsync("ApiKey", version!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("version");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SecretExistsAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        AwsSecretsManagerProvider provider = CreateProvider();

        Func<Task> act = () => provider.SecretExistsAsync(name!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretAsync_WithInvalidCredentials_ShouldThrow()
    {
        AwsSecretsManagerProvider provider = new(SecurityTestHelpers.CreateAwsOptions(
            accessKeyId: "AKIAINVALIDTESTKEY0000",
            secretAccessKey: "invalidSecretAccessKeyForUnitTest000000"));

        Func<Task> act = () => provider.GetSecretAsync("mvp24hours-unit-test-secret");

        await act.Should().ThrowAsync<Exception>();
    }

    private static AwsSecretsManagerProvider CreateProvider()
    {
        return new(SecurityTestHelpers.CreateAwsOptions());
    }
}
