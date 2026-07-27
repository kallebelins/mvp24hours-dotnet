# Options Validation

Mvp24Hours.Core builds on `Microsoft.Extensions.Options` with Data Annotations, custom validators, fail-fast startup validation, monitor/snapshot registration, and assembly scanning.

## Data Annotations and fail-fast startup

`AddOptionsWithValidation<TOptions>` binds an `IConfigurationSection`, calls `ValidateDataAnnotations()`, and calls `ValidateOnStart()` by default.

```csharp
using System.ComponentModel.DataAnnotations;
using Mvp24Hours.Core.Extensions.Options;

public sealed class DatabaseOptions
{
    [Required]
    public string? ConnectionString { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 5432;
}

builder.Services.AddOptionsWithValidation<DatabaseOptions>(
    builder.Configuration.GetSection("Database"));
```

Set `validateOnStart: false` only when validation should be deferred until `IOptions<TOptions>.Value` is resolved.

## Registration overloads

| API | Validation performed | Default startup behavior |
|---|---|---|
| `AddOptionsWithValidation<TOptions>(section, validateOnStart)` | Data Annotations | Fail fast |
| `AddOptionsWithValidation<TOptions, TValidator>(section, validateOnStart)` | Data Annotations plus `IOptionsValidator<TOptions>` | Fail fast |
| `AddOptionsWithValidation<TOptions>(section, predicate, message, validateOnStart)` | Data Annotations plus one predicate | Fail fast |
| `AddOptionsWithValidation<TOptions>(section, validations, validateOnStart)` | Data Annotations plus multiple predicate/message pairs | Fail fast |
| `AddOptionsForMonitor<TOptions>(section)` | Data Annotations; change token registration | Validate on access/change |
| `AddOptionsForSnapshot<TOptions>(section)` | Data Annotations | Validate per scope on access |

All overloads return either `OptionsBuilder<TOptions>` or `IServiceCollection`, so standard Options APIs remain available.

## Custom validators

Implement `IOptionsValidator<TOptions>` directly, or derive from one of the supplied base classes. The generic registration adapts it to Microsoft's `IValidateOptions<TOptions>`.

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Options;
using Mvp24Hours.Core.Extensions.Options;

public sealed class DatabaseOptionsValidator
    : OptionsValidatorBase<DatabaseOptions>
{
    protected override void ConfigureValidation(
        OptionsValidationContext<DatabaseOptions> context,
        DatabaseOptions options)
    {
        if (options.ConnectionString?.Contains("Password=", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.AddPropertyError(
                nameof(options.ConnectionString),
                "Store credentials in a secret provider.");
        }
    }
}

builder.Services.AddOptionsWithValidation<
    DatabaseOptions,
    DatabaseOptionsValidator>(
    builder.Configuration.GetSection("Database"));
```

`OptionsValidatorBase<TOptions>` runs Data Annotations and custom rules. `SimpleOptionsValidatorBase<TOptions>` is appropriate for a single Boolean rule and failure message. `DelegateOptionsValidator<TOptions>` and `CompositeOptionsValidator<TOptions>` support programmatic composition.

## Register validators separately

Use these methods when options binding is already configured:

```csharp
services.AddOptionsValidator<DatabaseOptions, DatabaseOptionsValidator>();

services.AddOptionsValidatorsFromAssemblyContaining<DatabaseOptionsValidator>();
// Or: services.AddOptionsValidatorsFromAssembly(typeof(DatabaseOptionsValidator).Assembly);
```

Assembly scanning registers concrete implementations of `IOptionsValidator<TOptions>` and their adapters.

## Predicate validation

For a local rule, avoid a validator class:

```csharp
builder.Services.AddOptionsWithValidation<DatabaseOptions>(
    builder.Configuration.GetSection("Database"),
    options => options.Port != 0,
    "Database:Port must be configured.");
```

For several rules:

```csharp
var rules = new (Func<DatabaseOptions, bool>, string)[]
{
    (options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "Database:ConnectionString is required."),
    (options => options.Port is >= 1 and <= 65535,
        "Database:Port must be between 1 and 65535.")
};

builder.Services.AddOptionsWithValidation(
    builder.Configuration.GetSection("Database"),
    rules);
```

## Monitor and snapshot

Use `IOptionsMonitor<TOptions>` for singleton consumers that react to configuration reloads:

```csharp
services.AddOptionsForMonitor<FeatureOptions>(
    configuration.GetSection("Features"));

public sealed class FeatureService(IOptionsMonitor<FeatureOptions> options)
{
    public bool IsEnabled => options.CurrentValue.Enabled;
}
```

Use `IOptionsSnapshot<TOptions>` for scoped consumers that need one value per scope:

```csharp
services.AddOptionsForSnapshot<TenantOptions>(
    configuration.GetSection("Tenant"));
```

## Validate an instance directly

`ValidateWithDataAnnotations` returns the library's `OptionsValidationResult` without using DI:

```csharp
OptionsValidationResult result =
    OptionsValidationExtensions.ValidateWithDataAnnotations(options);

if (!result.Succeeded)
{
    logger.LogError("Invalid options: {Failures}", result.FailureMessage);
}
```

`OptionsValidationResult.Fail` accepts one message or a sequence. `Failures` retains individual errors and `FailureMessage` joins them with `"; "`.

## Choosing an options interface

| Consumer | Use |
|---|---|
| Configuration is static after startup | `IOptions<TOptions>` |
| Singleton must observe reloads | `IOptionsMonitor<TOptions>` |
| Scoped/request consumer needs a fresh value | `IOptionsSnapshot<TOptions>` |

For production configuration, keep startup validation enabled unless delayed configuration is intentional.

## Related documentation

- [Core overview](home.md)
- [Infrastructure abstractions](infrastructure-abstractions.md)
- [.NET options pattern](https://learn.microsoft.com/dotnet/core/extensions/options)
- [.NET options validation](https://learn.microsoft.com/dotnet/core/extensions/options-validation)
