# Keyed Services (.NET 8+)

Keyed Services is a feature introduced in .NET 8 that allows registering and resolving multiple implementations of the same interface using a unique key. This feature simplifies scenarios where you need different implementations of a service based on context or configuration.

## Overview

Before .NET 8, having multiple implementations of an interface required custom factories or complex patterns. With Keyed Services, the native DI container supports this directly.

### Benefits

- **Simplicity**: Eliminates the need for custom factories
- **Type-safe**: Uses C# type system to ensure compile-time safety
- **Performance**: Integrated directly into .NET's DI container
- **Discoverability**: Centralizes service keys in constants

## Installation

Keyed Services support is available in the `Mvp24Hours.Core` package:

```csharp
using Mvp24Hours.Core.Extensions.KeyedServices;
```

## Predefined Service Keys

The `ServiceKeys` class provides constants for commonly used keys:

```csharp
public static class ServiceKeys
{
    // Default implementation
    public const string Default = "Default";
    
    // Storage
    public const string InMemory = "InMemory";
    public const string Local = "Local";
    public const string AzureBlob = "AzureBlob";
    public const string AwsS3 = "AwsS3";
    
    // Email Providers
    public const string Smtp = "Smtp";
    public const string SendGrid = "SendGrid";
    public const string AzureCommunication = "AzureCommunication";
    
    // SMS Providers
    public const string Twilio = "Twilio";
    public const string AzureCommunicationSms = "AzureCommunicationSms";
    
    // Background Jobs
    public const string Hangfire = "Hangfire";
    public const string Quartz = "Quartz";
    
    // Distributed Locks
    public const string Redis = "Redis";
    public const string SqlServer = "SqlServer";
    public const string PostgreSql = "PostgreSql";
    public const string MongoDb = "MongoDb";
    
    // Serialization
    public const string Json = "Json";
    public const string MessagePack = "MessagePack";
    public const string Xml = "Xml";
    
    // Template Engines
    public const string Scriban = "Scriban";
    public const string Razor = "Razor";
}
```

## Registering Keyed Services

### Basic Registration Methods

```csharp
// Keyed singleton
services.AddKeyedSingletonService<IFileStorage, LocalFileStorage>(ServiceKeys.Local);
services.AddKeyedSingletonService<IFileStorage, AzureBlobStorage>(ServiceKeys.AzureBlob);

// Keyed scoped
services.AddKeyedScopedService<IEmailService, SmtpEmailService>(ServiceKeys.Smtp);
services.AddKeyedScopedService<IEmailService, SendGridEmailService>(ServiceKeys.SendGrid);

// Keyed transient
services.AddKeyedTransientService<IReportGenerator, PdfReportGenerator>("pdf");
services.AddKeyedTransientService<IReportGenerator, ExcelReportGenerator>("excel");
```

### Factory Registration

```csharp
services.AddKeyedSingletonService<ICache>(ServiceKeys.Redis, (sp, key) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Redis");
    return new RedisCache(connectionString);
});
```

### TryAdd Registration (Does Not Overwrite)

```csharp
// Only registers if not already registered
services.TryAddKeyedSingletonService<IFileStorage, LocalFileStorage>(ServiceKeys.Local);
services.TryAddKeyedScopedService<IEmailService, SmtpEmailService>(ServiceKeys.Smtp);
services.TryAddKeyedTransientService<IReportGenerator, PdfReportGenerator>("pdf");
```

### Registration with Default Service

The `AddDefaultKeyed*` methods register the service both with a key and without a key, allowing resolution through either method:

```csharp
// Registers SmtpEmailService as:
// 1. Keyed service with key ServiceKeys.Default
// 2. Default service (no key)
services.AddDefaultKeyedSingletonService<IEmailService, SmtpEmailService>();

// Both resolutions work:
var byKey = sp.GetRequiredKeyedService<IEmailService>(ServiceKeys.Default);
var byDefault = sp.GetRequiredService<IEmailService>();
```

## Resolving Keyed Services

### Programmatic Resolution

```csharp
public class StorageManager
{
    private readonly IServiceProvider _serviceProvider;

    public StorageManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task UploadAsync(string storageType, Stream content)
    {
        var storage = storageType switch
        {
            "azure" => _serviceProvider.GetRequiredKeyedService<IFileStorage>(ServiceKeys.AzureBlob),
            "aws" => _serviceProvider.GetRequiredKeyedService<IFileStorage>(ServiceKeys.AwsS3),
            _ => _serviceProvider.GetRequiredKeyedService<IFileStorage>(ServiceKeys.Local)
        };

        await storage.UploadAsync(content);
    }
}
```

### Attribute-Based Resolution

Use the `[FromKeyedServices]` attribute to inject specific services in the constructor:

```csharp
public class NotificationService
{
    private readonly IEmailService _smtpService;
    private readonly IEmailService _sendGridService;

    public NotificationService(
        [FromKeyedServices(ServiceKeys.Smtp)] IEmailService smtpService,
        [FromKeyedServices(ServiceKeys.SendGrid)] IEmailService sendGridService)
    {
        _smtpService = smtpService;
        _sendGridService = sendGridService;
    }

    public async Task SendEmailAsync(EmailMessage message, bool useSendGrid = false)
    {
        var service = useSendGrid ? _sendGridService : _smtpService;
        await service.SendAsync(message);
    }
}
```

### Resolution in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    [HttpGet("{format}")]
    public async Task<IActionResult> GenerateReport(
        string format,
        [FromKeyedServices("pdf")] IReportGenerator pdfGenerator,
        [FromKeyedServices("excel")] IReportGenerator excelGenerator)
    {
        var generator = format.ToLower() == "pdf" ? pdfGenerator : excelGenerator;
        var report = await generator.GenerateAsync();
        return File(report, generator.ContentType);
    }
}
```

## Usage Scenarios

### Multi-tenant with Different Providers

```csharp
// Registration
services.AddKeyedScopedService<IPaymentGateway, StripePaymentGateway>("stripe");
services.AddKeyedScopedService<IPaymentGateway, PayPalPaymentGateway>("paypal");
services.AddKeyedScopedService<IPaymentGateway, PagarMePaymentGateway>("pagarme");

// Usage
public class PaymentService
{
    private readonly IServiceProvider _sp;
    private readonly ITenantContext _tenant;

    public PaymentService(IServiceProvider sp, ITenantContext tenant)
    {
        _sp = sp;
        _tenant = tenant;
    }

    public async Task ProcessPaymentAsync(PaymentRequest request)
    {
        var gateway = _sp.GetRequiredKeyedService<IPaymentGateway>(_tenant.PaymentProvider);
        await gateway.ProcessAsync(request);
    }
}
```

### Feature Flags

```csharp
// Registration
services.AddKeyedSingletonService<ISearchEngine, ElasticSearchEngine>("elasticsearch");
services.AddKeyedSingletonService<ISearchEngine, LuceneSearchEngine>("lucene");
services.AddKeyedSingletonService<ISearchEngine, InMemorySearchEngine>("inmemory");

// Usage with feature flag
public class SearchService
{
    private readonly IServiceProvider _sp;
    private readonly IFeatureManager _features;

    public async Task<SearchResults> SearchAsync(string query)
    {
        var engineKey = await _features.IsEnabledAsync("UseElasticSearch")
            ? "elasticsearch"
            : "lucene";
            
        var engine = _sp.GetRequiredKeyedService<ISearchEngine>(engineKey);
        return await engine.SearchAsync(query);
    }
}
```

### Fallback and Resilience

```csharp
public class ResilientStorageService
{
    private readonly IFileStorage _primaryStorage;
    private readonly IFileStorage _fallbackStorage;
    private readonly ILogger<ResilientStorageService> _logger;

    public ResilientStorageService(
        [FromKeyedServices(ServiceKeys.AzureBlob)] IFileStorage primaryStorage,
        [FromKeyedServices(ServiceKeys.Local)] IFileStorage fallbackStorage,
        ILogger<ResilientStorageService> logger)
    {
        _primaryStorage = primaryStorage;
        _fallbackStorage = fallbackStorage;
        _logger = logger;
    }

    public async Task<byte[]> ReadAsync(string path)
    {
        try
        {
            return await _primaryStorage.ReadAsync(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary storage failed, using fallback");
            return await _fallbackStorage.ReadAsync(path);
        }
    }
}
```

## Replacing Custom Factories

### Before (Custom Factory)

```csharp
// Old factory
public interface IEmailServiceFactory
{
    IEmailService Create(string provider);
}

public class EmailServiceFactory : IEmailServiceFactory
{
    private readonly IServiceProvider _sp;
    
    public EmailServiceFactory(IServiceProvider sp) => _sp = sp;
    
    public IEmailService Create(string provider) => provider switch
    {
        "smtp" => ActivatorUtilities.CreateInstance<SmtpEmailService>(_sp),
        "sendgrid" => ActivatorUtilities.CreateInstance<SendGridEmailService>(_sp),
        _ => throw new ArgumentException($"Unknown provider: {provider}")
    };
}

// Old registration
services.AddSingleton<IEmailServiceFactory, EmailServiceFactory>();
```

### After (Keyed Services)

```csharp
// Modern registration
services.AddKeyedScopedService<IEmailService, SmtpEmailService>(ServiceKeys.Smtp);
services.AddKeyedScopedService<IEmailService, SendGridEmailService>(ServiceKeys.SendGrid);

// Direct usage
var service = sp.GetRequiredKeyedService<IEmailService>(ServiceKeys.Smtp);
```

## Testing Keyed Services

```csharp
public class KeyedServicesTests
{
    [Fact]
    public void ShouldResolveCorrectImplementation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingletonService<IFileStorage, LocalFileStorage>(ServiceKeys.Local);
        services.AddKeyedSingletonService<IFileStorage, AzureBlobStorage>(ServiceKeys.AzureBlob);
        
        var sp = services.BuildServiceProvider();

        // Act
        var local = sp.GetRequiredKeyedService<IFileStorage>(ServiceKeys.Local);
        var azure = sp.GetRequiredKeyedService<IFileStorage>(ServiceKeys.AzureBlob);

        // Assert
        Assert.IsType<LocalFileStorage>(local);
        Assert.IsType<AzureBlobStorage>(azure);
    }

    [Fact]
    public void TryAdd_ShouldNotOverwrite()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddKeyedSingletonService<IFileStorage, LocalFileStorage>(ServiceKeys.Local);
        services.TryAddKeyedSingletonService<IFileStorage, AzureBlobStorage>(ServiceKeys.Local);
        
        var sp = services.BuildServiceProvider();

        // Act
        var storage = sp.GetRequiredKeyedService<IFileStorage>(ServiceKeys.Local);

        // Assert - Should be the first one registered
        Assert.IsType<LocalFileStorage>(storage);
    }
}
```

## Best Practices

1. **Use constants for keys**: Avoid magic strings by using `ServiceKeys` or your own constants
2. **Document available keys**: Maintain clear documentation about which implementations are available
3. **Prefer attribute injection**: Use `[FromKeyedServices]` when possible for better readability
4. **Combine with configuration**: Use `IConfiguration` to determine which key to use at runtime
5. **Test all implementations**: Ensure each registered implementation works correctly
6. **Use TryAdd for libraries**: Allow consumers to override default implementations

## References

- [Official .NET documentation on Keyed Services](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services)
- [What's new in .NET 8 - Keyed DI services](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8#keyed-di-services)

