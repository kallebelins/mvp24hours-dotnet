//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Security.Options;
using Mvp24Hours.Infrastructure.Security.Providers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Security.Providers;

[Trait("Category", "Unit")]
public class EnvironmentVariableSecretProviderTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EnvironmentVariableSecretProvider((EnvironmentVariableOptions)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullIOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new EnvironmentVariableSecretProvider((IOptions<EnvironmentVariableOptions>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithIOptions_ShouldCreateInstance()
    {
        EnvironmentVariableSecretProvider provider = new(
            SecurityTestHelpers.AsOptions(SecurityTestHelpers.CreateEnvironmentVariableOptions()));

        provider.Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSecretAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        EnvironmentVariableSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretAsync(name!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task GetSecretAsync_WhenVariableExists_ShouldReturnValue()
    {
        string secretName = SecurityTestHelpers.UniqueSecretName("get");
        string variableName = "MVP24H_" + secretName;
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(variableName, "super-secret");

        EnvironmentVariableSecretProvider provider = CreateProvider(prefix: "MVP24H_");

        string? value = await provider.GetSecretAsync(secretName);

        value.Should().Be("super-secret");
    }

    [Fact]
    public async Task GetSecretAsync_WhenVariableMissing_ShouldReturnNull()
    {
        EnvironmentVariableSecretProvider provider = CreateProvider();

        string? value = await provider.GetSecretAsync(SecurityTestHelpers.UniqueSecretName("missing"));

        value.Should().BeNull();
    }

    [Fact]
    public async Task GetSecretAsync_WithoutPrefix_ShouldUseExactName()
    {
        string secretName = SecurityTestHelpers.UniqueSecretName("noprefix");
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(secretName, "plain-value");

        EnvironmentVariableSecretProvider provider = CreateProvider();

        string? value = await provider.GetSecretAsync(secretName);

        value.Should().Be("plain-value");
    }

    [Fact]
    public async Task GetSecretsAsync_WithNull_ShouldThrowArgumentNullException()
    {
        EnvironmentVariableSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.GetSecretsAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("secretNames");
    }

    [Fact]
    public async Task GetSecretsAsync_ShouldSkipBlankNamesAndReturnMixedResults()
    {
        string found = SecurityTestHelpers.UniqueSecretName("found");
        string missing = SecurityTestHelpers.UniqueSecretName("miss");
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(found, "present");

        EnvironmentVariableSecretProvider provider = CreateProvider();

        IDictionary<string, string?> result = await provider.GetSecretsAsync([found, "  ", missing, ""]);

        result.Should().ContainKey(found).WhoseValue.Should().Be("present");
        result.Should().ContainKey(missing).WhoseValue.Should().BeNull();
        result.Should().NotContainKey("  ");
        result.Should().NotContainKey("");
    }

    [Fact]
    public async Task GetSecretVersionAsync_ShouldIgnoreVersionAndReturnCurrentValue()
    {
        string secretName = SecurityTestHelpers.UniqueSecretName("ver");
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(secretName, "v1-value");

        EnvironmentVariableSecretProvider provider = CreateProvider();

        string? value = await provider.GetSecretVersionAsync(secretName, "any-version");

        value.Should().Be("v1-value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SecretExistsAsync_WithInvalidName_ShouldThrowArgumentException(string? name)
    {
        EnvironmentVariableSecretProvider provider = CreateProvider();

        Func<Task> act = () => provider.SecretExistsAsync(name!);

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("secretName");
    }

    [Fact]
    public async Task SecretExistsAsync_WhenPresent_ShouldReturnTrue()
    {
        string secretName = SecurityTestHelpers.UniqueSecretName("exists");
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(secretName, "x");

        EnvironmentVariableSecretProvider provider = CreateProvider();

        bool exists = await provider.SecretExistsAsync(secretName);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task SecretExistsAsync_WhenMissing_ShouldReturnFalse()
    {
        EnvironmentVariableSecretProvider provider = CreateProvider();

        bool exists = await provider.SecretExistsAsync(SecurityTestHelpers.UniqueSecretName("nope"));

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SecretExistsAsync_WhenEmptyString_ShouldReturnTrue()
    {
        string secretName = SecurityTestHelpers.UniqueSecretName("empty");
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(secretName, string.Empty);

        EnvironmentVariableSecretProvider provider = CreateProvider();

        bool exists = await provider.SecretExistsAsync(secretName);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetSecretAsync_OnWindows_WithCaseInsensitive_ShouldResolveActualCase()
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            return;
        }

        string secretName = SecurityTestHelpers.UniqueSecretName("Case");
        string variableName = "MVP24H_" + secretName;
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(variableName, "case-value");

        EnvironmentVariableSecretProvider provider = CreateProvider(prefix: "mvp24h_", caseSensitive: false);

        string? value = await provider.GetSecretAsync(secretName);

        value.Should().Be("case-value");
    }

    private static EnvironmentVariableSecretProvider CreateProvider(
        string? prefix = null,
        bool caseSensitive = false)
    {
        return new EnvironmentVariableSecretProvider(
            SecurityTestHelpers.CreateEnvironmentVariableOptions(prefix, caseSensitive));
    }
}
