//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Security.Options;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class SecurityTestHelpers
{
    public static string UniqueSecretName(string prefix = "mvp24h-sec")
        => $"{prefix}-{Guid.NewGuid():N}";

    public static IOptions<T> AsOptions<T>(T value) where T : class
        => Options.Create(value);

    public static EnvironmentVariableOptions CreateEnvironmentVariableOptions(
        string? prefix = null,
        bool caseSensitive = false,
        EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
    {
        return new EnvironmentVariableOptions
        {
            VariableNamePrefix = prefix,
            CaseSensitive = caseSensitive,
            Target = target
        };
    }

    public static AwsSecretsManagerOptions CreateAwsOptions(
        string? region = "us-east-1",
        string? prefix = null,
        string? accessKeyId = null,
        string? secretAccessKey = null,
        string? sessionToken = null)
    {
        return new AwsSecretsManagerOptions
        {
            Region = region,
            SecretNamePrefix = prefix,
            AccessKeyId = accessKeyId,
            SecretAccessKey = secretAccessKey,
            SessionToken = sessionToken
        };
    }

    public static AzureKeyVaultOptions CreateAzureOptions(
        string? vaultUri = "https://mvp24hours-test.vault.azure.net/",
        bool useManagedIdentity = false,
        string? tenantId = null,
        string? clientId = null,
        string? clientSecret = null,
        string? managedIdentityClientId = null)
    {
        return new AzureKeyVaultOptions
        {
            VaultUri = vaultUri == null ? null : new Uri(vaultUri),
            UseManagedIdentity = useManagedIdentity,
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecret = clientSecret,
            ManagedIdentityClientId = managedIdentityClientId
        };
    }

    /// <summary>
    /// Sets a process environment variable and restores the previous value on dispose.
    /// </summary>
    public static IDisposable SetEnvironmentVariable(string name, string? value)
    {
        string? previous = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
        return new EnvironmentVariableScope(name, previous);
    }

    private sealed class EnvironmentVariableScope(string name, string? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Environment.SetEnvironmentVariable(name, previous, EnvironmentVariableTarget.Process);
            _disposed = true;
        }
    }
}
