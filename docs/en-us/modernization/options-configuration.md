# Options Configuration

This document describes the Options Pattern implementation in Mvp24Hours, following .NET 9 best practices for typed configuration.

## Overview

The Options Pattern in .NET provides a strongly-typed approach to accessing configuration values. Mvp24Hours extends this pattern with:

- **Fluent validation API** - Easy-to-use validation context
- **Custom validators** - Base classes for building validators
- **Data Annotations support** - Automatic DataAnnotations validation
- **Startup validation** - Fail-fast with `ValidateOnStart()`
- **Assembly scanning** - Auto-registration of validators

## Understanding IOptions Variants

.NET provides three interfaces for accessing options:

| Interface | Lifetime | Reloading | Use Case |
|-----------|----------|-----------|----------|
| `IOptions<T>` | Singleton | No | Static configuration that never changes |
| `IOptionsMonitor<T>` | Singleton | Yes (OnChange) | Configuration that may change at runtime |
| `IOptionsSnapshot<T>` | Scoped | Yes (per-request) | Per-request configuration (e.g., tenant-specific) |

### When to Use Each

```csharp
// IOptions<T> - Best for static configuration
public class DatabaseService
{
    private readonly DatabaseOptions _options;
    
    public DatabaseService(IOptions<DatabaseOptions> options)
    {
        _options = options.Value; // Read once, cached forever
    }
}

// IOptionsMonitor<T> - Best for runtime changes
public class FeatureToggleService
{
    private readonly IOptionsMonitor<FeatureFlags> _options;
    
    public FeatureToggleService(IOptionsMonitor<FeatureFlags> options)
    {
        _options = options;
        _options.OnChange(flags => 
            Console.WriteLine($"Feature flags changed: {flags.EnableNewUI}"));
    }
    
    public bool IsNewUIEnabled => _options.CurrentValue.EnableNewUI;
}

// IOptionsSnapshot<T> - Best for per-request configuration
public class TenantService
{
    private readonly TenantOptions _options;
    
    public TenantService(IOptionsSnapshot<TenantOptions> options)
    {
        _options = options.Value; // Fresh per request
    }
}
```

## Configuration Classes

### Naming Convention

All configuration classes should follow the `*Options` naming convention:

```csharp
// ✅ Good
public class DatabaseOptions { }
public class CachingOptions { }
public class RateLimitingOptions { }

// ❌ Avoid
public class DatabaseConfig { }
public class CachingSettings { }
```

### Using Data Annotations

Add validation attributes to your options class:

```csharp
public class DatabaseOptions
{
    [Required(ErrorMessage = "ConnectionString is required.")]
    public string? ConnectionString { get; set; }

    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
    public int Port { get; set; } = 5432;

    [Range(1, 3600, ErrorMessage = "Timeout must be between 1 and 3600 seconds.")]
    public int TimeoutSeconds { get; set; } = 30;

    [EmailAddress(ErrorMessage = "NotificationEmail must be a valid email.")]
    public string? NotificationEmail { get; set; }
}
```

## Registration Methods

### Basic Registration with Validation

```csharp
// With Data Annotations validation and fail-fast
services.AddOptionsWithValidation<DatabaseOptions>(
    configuration.GetSection("Database"),
    validateOnStart: true);
```

### Registration with Custom Validator

```csharp
services.AddOptionsWithValidation<DatabaseOptions, DatabaseOptionsValidator>(
    configuration.GetSection("Database"),
    validateOnStart: true);
```

### Registration with Inline Validation

```csharp
services.AddOptionsWithValidation<DatabaseOptions>(
    configuration.GetSection("Database"),
    options => !string.IsNullOrEmpty(options.ConnectionString),
    "ConnectionString is required.");
```

### Registration for IOptionsMonitor

```csharp
services.AddOptionsForMonitor<FeatureFlags>(
    configuration.GetSection("Features"),
    options => Console.WriteLine($"Features changed: {options.EnableNewUI}"));
```

### Registration for IOptionsSnapshot

```csharp
services.AddOptionsForSnapshot<TenantOptions>(
    configuration.GetSection("Tenant"));
```

## Custom Validators

### Using OptionsValidatorBase

Create a custom validator by extending `OptionsValidatorBase<T>`:

```csharp
public class DatabaseOptionsValidator : OptionsValidatorBase<DatabaseOptions>
{
    protected override void ConfigureValidation(
        OptionsValidationContext<DatabaseOptions> context,
        DatabaseOptions options)
    {
        // Validate connection string format
        context.ValidateProperty(nameof(options.ConnectionString), options.ConnectionString)
            .NotNullOrEmpty("ConnectionString is required.")
            .Must(cs => cs?.Contains("Host=") == true, "ConnectionString must contain Host.");

        // Validate port range
        context.ValidateProperty(nameof(options.Port), options.Port)
            .InRange(1, 65535, "Port must be between 1 and 65535.");

        // Validate timeout
        context.ValidateTimeSpan(nameof(options.TimeoutSeconds), 
            TimeSpan.FromSeconds(options.TimeoutSeconds))
            .Positive("Timeout must be positive.")
            .MaxValue(TimeSpan.FromHours(1), "Timeout cannot exceed 1 hour.");

        // Cross-property validation
        if (options.EnableSsl && string.IsNullOrEmpty(options.CertificatePath))
        {
            context.AddPropertyError(nameof(options.CertificatePath),
                "CertificatePath is required when SSL is enabled.");
        }
    }
}
```

### Using SimpleOptionsValidatorBase

For simple validation with a single predicate:

```csharp
public class ApiKeyValidator : SimpleOptionsValidatorBase<ApiOptions>
{
    protected override string FailureMessage 
        => "ApiKey is required and must start with 'API_'.";

    protected override bool IsValid(ApiOptions options)
        => !string.IsNullOrEmpty(options.ApiKey) && 
           options.ApiKey.StartsWith("API_");
}
```

### Using DelegateOptionsValidator

For inline validators:

```csharp
var validator = DelegateOptionsValidator<ApiOptions>.Create(
    opts => !string.IsNullOrEmpty(opts.ApiKey),
    "ApiKey is required.");
```

### Composite Validators

Combine multiple validators:

```csharp
var composite = new CompositeOptionsValidator<ApiOptions>(new[]
{
    new ApiKeyValidator(),
    DelegateOptionsValidator<ApiOptions>.Create(
        opts => opts.RateLimit > 0,
        "RateLimit must be positive.")
});
```

## Validation Context API

The `OptionsValidationContext<T>` provides a fluent API for validation:

### String Properties

```csharp
context.ValidateProperty("Name", options.Name)
    .NotNullOrEmpty()
    .MaxLength(100)
    .MinLength(3)
    .Matches(@"^[a-zA-Z]+$", "Name must contain only letters.")
    .IsEmail()
    .IsUri(UriKind.Absolute);
```

### Numeric Properties

```csharp
context.ValidateProperty("Port", options.Port)
    .GreaterThan(0)
    .LessThan(65536)
    .InRange(1, 65535);
```

### TimeSpan Properties

```csharp
context.ValidateTimeSpan("Timeout", options.Timeout)
    .Positive()
    .NotNegative()
    .MinValue(TimeSpan.FromSeconds(1))
    .MaxValue(TimeSpan.FromHours(1));
```

### URI Properties

```csharp
context.ValidateUri("Endpoint", options.Endpoint)
    .NotNull()
    .IsAbsolute()
    .IsHttps()
    .HasScheme(new[] { "http", "https" });
```

### Cross-Property Validation

```csharp
context.AtLeastOne("Either ApiKey or ClientCredentials must be configured.",
    !string.IsNullOrEmpty(options.ApiKey),
    options.ClientCredentials != null);

context.ExactlyOne("Specify either Primary or Fallback connection, not both.",
    options.UsePrimaryConnection,
    options.UseFallbackConnection);
```

## Assembly Scanning

Register all validators from an assembly:

```csharp
services.AddOptionsValidatorsFromAssembly(typeof(Program).Assembly);

// Or using a marker type
services.AddOptionsValidatorsFromAssemblyContaining<DatabaseOptionsValidator>();
```

## Best Practices

### 1. Always Use ValidateOnStart for Critical Configuration

```csharp
services.AddOptionsWithValidation<DatabaseOptions>(
    configuration.GetSection("Database"),
    validateOnStart: true); // Fail fast if invalid
```

### 2. Use IOptionsMonitor for Feature Flags

```csharp
public class FeatureService
{
    private readonly IOptionsMonitor<FeatureFlags> _flags;
    
    public FeatureService(IOptionsMonitor<FeatureFlags> flags)
    {
        _flags = flags;
    }
    
    public bool IsEnabled(string feature) => 
        _flags.CurrentValue.EnabledFeatures.Contains(feature);
}
```

### 3. Use IOptionsSnapshot for Multi-Tenant Scenarios

```csharp
public class TenantService
{
    private readonly IOptionsSnapshot<TenantOptions> _options;
    
    public TenantService(IOptionsSnapshot<TenantOptions> options)
    {
        _options = options;
    }
    
    public string GetTenantTheme() => _options.Value.Theme;
}
```

### 4. Combine DataAnnotations with Custom Validation

```csharp
public class OptionsValidator : OptionsValidatorBase<MyOptions>
{
    // IncludeDataAnnotations is true by default
    // Custom validation runs after DataAnnotations
    
    protected override void ConfigureValidation(
        OptionsValidationContext<MyOptions> context,
        MyOptions options)
    {
        // Business rule validation
        if (options.MaxRetries > 10 && !options.EnableCircuitBreaker)
        {
            context.AddError("CircuitBreaker must be enabled for more than 10 retries.");
        }
    }
}
```

### 5. Use Named Options for Multiple Instances

```csharp
services.Configure<DatabaseOptions>("Primary", 
    configuration.GetSection("Database:Primary"));
services.Configure<DatabaseOptions>("Reporting", 
    configuration.GetSection("Database:Reporting"));

// In your service:
public class ReportService
{
    private readonly DatabaseOptions _options;
    
    public ReportService(IOptionsSnapshot<DatabaseOptions> options)
    {
        _options = options.Get("Reporting");
    }
}
```

## Configuration Example

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=myapp;Username=admin;Password=secret",
    "Port": 5432,
    "TimeoutSeconds": 30,
    "EnableSsl": true,
    "CertificatePath": "/certs/db.crt"
  },
  "Features": {
    "EnableNewUI": true,
    "EnabledFeatures": ["dark-mode", "export-pdf"]
  },
  "Tenant": {
    "Theme": "modern",
    "MaxUsers": 100
  }
}
```

## See Also

- [Time Provider](time-provider.md) - Abstracting time for testability
- [Periodic Timer](periodic-timer.md) - Modern timer patterns
- [HTTP Resilience](http-resilience.md) - Resilience configuration

