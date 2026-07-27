# Secrets and security helpers

The security infrastructure provides a common `ISecretProvider` abstraction with environment-variable, Azure Key Vault, and AWS Secrets Manager implementations. It also includes rotation coordination and sensitive-data masking. AES helpers and `EncryptionOptions` live in the Core infrastructure contracts, with an EF Core DI registration extension.

## Secret provider contract

`ISecretProvider` exposes:

- `GetSecretAsync(name, cancellationToken)`.
- `GetSecretsAsync(names, cancellationToken)`.
- `GetSecretVersionAsync(name, version, cancellationToken)`.
- `SecretExistsAsync(name, cancellationToken)`.

Azure Key Vault and AWS return `null` for their provider-specific not-found response and rethrow other provider errors. Environment variables return `null` when absent. Multi-get operations issue individual provider reads; they are not provider-native batch calls.

Register one provider as the singleton `ISecretProvider`. Registering another later replaces the effective single-service resolution in normal Microsoft DI usage.

## Environment variables

```csharp
using Mvp24Hours.Infrastructure.Security.Contract;
using Mvp24Hours.Infrastructure.Security.Extensions;

services.AddEnvironmentVariableSecretProvider(options =>
{
    options.VariableNamePrefix = "MYAPP_";
    options.Target = EnvironmentVariableTarget.Process;
    options.CaseSensitive = false;
});

string? apiKey = await secretProvider.GetSecretAsync("ApiKey", cancellationToken);
// Reads MYAPP_ApiKey.
```

| `EnvironmentVariableOptions` property | Type | Default | Behavior |
|---|---|---:|---|
| `Target` | `EnvironmentVariableTarget` | `Process` | Selects process, user, or machine scope. |
| `VariableNamePrefix` | `string?` | `null` | Prepended verbatim to the requested name. |
| `CaseSensitive` | `bool` | `false` | When `false`, the provider performs an explicit case-insensitive name lookup only on Windows. |

Environment variables do not have versions. `GetSecretVersionAsync` ignores the requested version and returns the current value.

## Azure Key Vault

```csharp
services.AddAzureKeyVaultSecretProvider(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.UseManagedIdentity = true;
    // Set ManagedIdentityClientId for a user-assigned identity.
});
```

| `AzureKeyVaultOptions` property | Type | Default | Required / behavior |
|---|---|---:|---|
| `VaultUri` | `Uri?` | `null` | Required; provider construction fails when absent. |
| `VaultUriString` | `string?` | `null` | Convenience wrapper that reads/writes `VaultUri`. |
| `TenantId` | `string?` | `null` | Required with client-secret authentication. |
| `ClientId` | `string?` | `null` | Required with client-secret authentication. |
| `ClientSecret` | `string?` | `null` | Required with client-secret authentication. |
| `UseManagedIdentity` | `bool` | `false` | Uses `DefaultAzureCredential` with optional managed-identity client ID. |
| `ManagedIdentityClientId` | `string?` | `null` | Selects a user-assigned managed identity. |

When `UseManagedIdentity` is `false`, all three client-secret fields are required when the lazily created SDK client is first used. The provider uses `Azure.Security.KeyVault.Secrets.SecretClient` and supports explicit secret versions.

See [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/general/overview).

## AWS Secrets Manager

```csharp
services.AddAwsSecretsManagerProvider(options =>
{
    options.Region = "us-east-1";
    options.SecretNamePrefix = "prod/myapp";
    // The AWS default credential chain is used when keys are omitted.
});
```

| `AwsSecretsManagerOptions` property | Type | Default | Behavior |
|---|---|---:|---|
| `Region` | `string?` | `null` | Uses the AWS SDK's configured/default region when absent. |
| `AccessKeyId` | `string?` | `null` | Used only when both access-key fields are supplied. |
| `SecretAccessKey` | `string?` | `null` | Used only when both access-key fields are supplied. |
| `SessionToken` | `string?` | `null` | Creates session credentials when access-key fields are also supplied. |
| `SecretNamePrefix` | `string?` | `null` | Normalized to one `/` before the requested secret name. |

Without an explicit access-key pair, the provider asks the AWS SDK default credential resolver for credentials. It uses `AmazonSecretsManagerClient`, returns `SecretString`, and supports version IDs through `GetSecretVersionAsync`.

See [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/latest/userguide/intro.html).

## `SecretProviderOptions`

| Property | Type | Default |
|---|---|---:|
| `EnableCaching` | `bool` | `false` |
| `CacheExpiration` | `TimeSpan` | 5 minutes |
| `DefaultTimeout` | `TimeSpan` | 30 seconds |
| `ThrowOnNotFound` | `bool` | `false` |

`SecretProviderOptions` is currently a standalone option model: none of the verified provider registrations binds it, and the three providers do not consume its caching, timeout, or not-found settings. Do not assume these values change runtime behavior until an application wrapper or a future library version explicitly wires them.

## Rotation helper

```csharp
using System.Security.Cryptography;

services.AddEnvironmentVariableSecretProvider();
services.AddSecretRotationHelper();

bool needsRotation = await rotationHelper.NeedsRotationAsync(
    "ApiKey",
    TimeSpan.FromDays(90),
    cancellationToken);

string generated = await rotationHelper.RotateSecretAsync(
    "ApiKey",
    () => Task.FromResult(Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(32))),
    cancellationToken);
```

`AddSecretRotationHelper()` registers scoped `ISecretRotationHelper`. The default `SecretRotationHelper` is a coordinator only:

- `GetSecretCreationDateAsync` and `GetLastRotationDateAsync` return `null`.
- Consequently, `NeedsRotationAsync` returns `true` because rotation age is unknown.
- `RotateSecretAsync` invokes and validates the generator and returns the new value.
- It does **not** write the new value to `ISecretProvider` or schedule rotation.

Persist the generated secret with the provider's management API or a custom rotation implementation.

## AES encryption

Two verified AES surfaces exist:

1. `Mvp24Hours.Infrastructure.Helpers.EncryptionHelper` offers static `CreateKeyBase64`, `EncryptWithAes`, and `DecryptWithAes` methods. Encryption generates a random IV and returns it separately.
2. `Mvp24Hours.Core.Infrastructure.Security.AesEncryptionProvider` implements `IExtendedEncryptionProvider` and can be created from `EncryptionOptions` or `CreateFromKey`.

```csharp
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Security;

string key = configuration["Encryption:Key"]
    ?? throw new InvalidOperationException("Encryption key is missing.");

var encryption = new AesEncryptionProvider(new EncryptionOptions
{
    Key = key,
    Deterministic = false,
    KeyId = "customer-data-v1"
});

string cipherText = encryption.Encrypt("sensitive value");
string plainText = encryption.Decrypt(cipherText);
```

| `EncryptionOptions` property | Type | Default | Required / behavior |
|---|---|---:|---|
| `Key` | `required string` | none | Required since v10. Must be Base64 for exactly 32 bytes (256 bits). |
| `InitializationVector` | `string?` | `null` | If set, must be Base64 for exactly 16 bytes (128 bits). |
| `Deterministic` | `bool` | `false` | Random-IV encryption by default. Deterministic mode has weaker confidentiality and requires a fixed IV for decryption. |
| `KeyId` | `string?` | `null` | Metadata for key-rotation scenarios. |
| `BlindIndexSalt` | `string?` | `null` | UTF-8 salt used by blind-index computation. |

The C# `required` modifier is a v10 source-breaking change for object initializers; runtime validation still rejects an empty, invalid Base64, or non-32-byte key. Generate a key once with `AesEncryptionProvider.GenerateKey()` or `EncryptionHelper.CreateKeyBase64()`, then store it in a secret manager. Do not generate a fresh production key at every startup.

The verified DI helper is in `Mvp24Hours.Infrastructure.Data.EFCore.Extensions`:

```csharp
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;

services.AddMvp24HoursEncryptionProvider(
    _ => AesEncryptionProvider.CreateFromKey(key),
    ServiceLifetime.Singleton);
```

There is no encryption-provider registration in `SecurityServiceExtensions`. ASP.NET Core Data Protection is a separate API and is not wrapped by this module. See [ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction).

## Sensitive-data masking

`SensitiveDataMasker` provides verified helpers:

- `MaskPassword`, `MaskApiKey`, `MaskCreditCard`, `MaskEmail`, and `MaskPhoneNumber`.
- `MaskPattern` for a regular-expression match.
- `MaskDictionary` and `MaskJson` for named sensitive fields.

```csharp
string maskedEmail = SensitiveDataMasker.MaskEmail("alex@example.com");

IDictionary<string, string?> safe = SensitiveDataMasker.MaskDictionary(
    values,
    ["password", "token", "clientSecret"]);
```

`LoggingExtensions` adds masked Information, Debug, Warning, Error, and Critical calls plus `LogDictionaryWithMasking`. Automatic argument masking is heuristic: any string of 8–128 characters is classified as password-like before the narrower API-key, email, card, and phone checks. The overloads accepting `sensitiveKeys` cannot map template names to argument positions and currently apply the same value heuristics to all arguments. Prefer explicit `SensitiveDataMasker` calls when exact disclosure rules matter.

Masking reduces accidental disclosure; it is not encryption and does not make logs an appropriate secret store.

## Password hashing (application pattern)

Mvp24Hours does not ship a password-hasher Options class. For local credential stores, use ASP.NET Core Identity's `IPasswordHasher<TUser>` or a PBKDF2 helper owned by the application:

```csharp
public static string HashPassword(string password)
{
    const int iterations = 100_000;
    byte[] salt = RandomNumberGenerator.GetBytes(16);
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        password,
        salt,
        iterations,
        HashAlgorithmName.SHA256,
        32);

    return $"v1.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
}
```

Store only the derived hash, prefer Identity when user management is required, and keep pepper/key material in a secret provider rather than source control. API keys and JWT signing secrets belong in the providers above; password hashes belong in the identity store.

## Observability and testing

Azure Key Vault and AWS Secrets Manager log not-found reads at Debug and provider failures at Error without logging secret values. `SecretRotationHelper` logs secret names at Information. There is no secrets-specific health-check registration, metric, or tracing instrument in the verified production source.

The Infrastructure test suites under `src/Tests/Mvp24Hours.Infrastructure.Test/Security/` cover:

- DI registration and lifetimes under `Security/Extensions`.
- Environment-variable reads, prefixes, scope, case behavior, multi-get, existence, and current-value version behavior.
- Azure/AWS constructor and argument guards; their SDK clients are not injectable, so unit tests do not claim successful live cloud retrieval.
- Rotation coordinator behavior and its non-persisting generator callback.
- Masking and masked logging behavior.
- Static AES helper round trips, random IVs, and invalid input.

Core tests in `src/Tests/Mvp24Hours.Core.Test/Infrastructure/InfrastructureServicesTest.cs` separately cover `AesEncryptionProvider` round trips, blind-index determinism, and invalid key length. Use test credentials only for constructor/guard tests; use provider-managed integration environments for live Azure or AWS verification.

