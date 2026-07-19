//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Security.Contract;
using Mvp24Hours.Infrastructure.Security.Extensions;
using Mvp24Hours.Infrastructure.Security.Helpers;
using Mvp24Hours.Infrastructure.Security.Providers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Security.Extensions;

[Trait("Category", "Unit")]
public class SecurityServiceExtensionsTest
{
    [Fact]
    public void AddEnvironmentVariableSecretProvider_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => SecurityServiceExtensions.AddEnvironmentVariableSecretProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEnvironmentVariableSecretProvider_ShouldRegisterISecretProvider()
    {
        var services = new ServiceCollection();

        services.AddEnvironmentVariableSecretProvider(options =>
        {
            options.VariableNamePrefix = "MVP24H_";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        ISecretProvider provider = sp.GetRequiredService<ISecretProvider>();

        provider.Should().BeOfType<EnvironmentVariableSecretProvider>();
    }

    [Fact]
    public async Task AddEnvironmentVariableSecretProvider_ResolvedProvider_ShouldReadEnvVar()
    {
        string secretName = SecurityTestHelpers.UniqueSecretName("di");
        string variableName = "MVP24H_" + secretName;
        using IDisposable scope = SecurityTestHelpers.SetEnvironmentVariable(variableName, "from-di");

        var services = new ServiceCollection();
        services.AddEnvironmentVariableSecretProvider(o => o.VariableNamePrefix = "MVP24H_");

        ServiceProvider sp = services.BuildServiceProvider();
        ISecretProvider provider = sp.GetRequiredService<ISecretProvider>();

        string? value = await provider.GetSecretAsync(secretName);

        value.Should().Be("from-di");
    }

    [Fact]
    public void AddAzureKeyVaultSecretProvider_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => SecurityServiceExtensions.AddAzureKeyVaultSecretProvider(
            null!,
            _ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddAzureKeyVaultSecretProvider_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddAzureKeyVaultSecretProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddAzureKeyVaultSecretProvider_ShouldRegisterProvider()
    {
        var services = new ServiceCollection();

        services.AddAzureKeyVaultSecretProvider(options =>
        {
            options.VaultUri = new Uri("https://mvp24hours-test.vault.azure.net/");
            options.UseManagedIdentity = true;
        });

        ServiceProvider sp = services.BuildServiceProvider();
        ISecretProvider provider = sp.GetRequiredService<ISecretProvider>();

        provider.Should().BeOfType<AzureKeyVaultSecretProvider>();
    }

    [Fact]
    public void AddAwsSecretsManagerProvider_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => SecurityServiceExtensions.AddAwsSecretsManagerProvider(null!, _ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddAwsSecretsManagerProvider_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddAwsSecretsManagerProvider(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddAwsSecretsManagerProvider_ShouldRegisterProvider()
    {
        var services = new ServiceCollection();

        services.AddAwsSecretsManagerProvider(options =>
        {
            options.Region = "us-east-1";
            options.AccessKeyId = "AKIAINVALIDTESTKEY0000";
            options.SecretAccessKey = "invalidSecretAccessKeyForUnitTest000000";
        });

        ServiceProvider sp = services.BuildServiceProvider();
        ISecretProvider provider = sp.GetRequiredService<ISecretProvider>();

        provider.Should().BeOfType<AwsSecretsManagerProvider>();
    }

    [Fact]
    public void AddSecretRotationHelper_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => SecurityServiceExtensions.AddSecretRotationHelper(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddSecretRotationHelper_ShouldRegisterScopedHelper()
    {
        var services = new ServiceCollection();
        services.AddEnvironmentVariableSecretProvider();
        services.AddSecretRotationHelper();

        ServiceProvider sp = services.BuildServiceProvider();

        using IServiceScope scope1 = sp.CreateScope();
        using IServiceScope scope2 = sp.CreateScope();

        ISecretRotationHelper helper1 = scope1.ServiceProvider.GetRequiredService<ISecretRotationHelper>();
        ISecretRotationHelper helper2 = scope1.ServiceProvider.GetRequiredService<ISecretRotationHelper>();
        ISecretRotationHelper helper3 = scope2.ServiceProvider.GetRequiredService<ISecretRotationHelper>();

        helper1.Should().BeOfType<SecretRotationHelper>();
        helper1.Should().BeSameAs(helper2);
        helper1.Should().NotBeSameAs(helper3);
    }
}
