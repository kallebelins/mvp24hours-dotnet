# Keyed Services (.NET 8+)

Keyed Services é um recurso introduzido no .NET 8 que permite registrar e resolver múltiplas implementações da mesma interface usando uma chave única. Este recurso simplifica cenários onde você precisa de diferentes implementações de um serviço baseadas em contexto ou configuração.

## Visão Geral

Antes do .NET 8, para ter múltiplas implementações de uma interface, era necessário usar factories customizadas ou padrões complexos. Com Keyed Services, o container de DI nativo suporta isso diretamente.

### Benefícios

- **Simplicidade**: Elimina a necessidade de factories customizadas
- **Type-safe**: Usa o sistema de tipos do C# para garantir segurança em tempo de compilação
- **Performance**: Integrado diretamente ao container de DI do .NET
- **Descobribilidade**: Centraliza as chaves de serviços em constantes

## Instalação

O suporte a Keyed Services está disponível no pacote `Mvp24Hours.Core`:

```csharp
using Mvp24Hours.Core.Extensions.KeyedServices;
```

## Chaves de Serviço Predefinidas

A classe `ServiceKeys` fornece constantes para chaves comumente usadas:

```csharp
public static class ServiceKeys
{
    // Implementação padrão
    public const string Default = "Default";
    
    // Armazenamento
    public const string InMemory = "InMemory";
    public const string Local = "Local";
    public const string AzureBlob = "AzureBlob";
    public const string AwsS3 = "AwsS3";
    
    // Provedores de Email
    public const string Smtp = "Smtp";
    public const string SendGrid = "SendGrid";
    public const string AzureCommunication = "AzureCommunication";
    
    // Provedores de SMS
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
    
    // Serialização
    public const string Json = "Json";
    public const string MessagePack = "MessagePack";
    public const string Xml = "Xml";
    
    // Template Engines
    public const string Scriban = "Scriban";
    public const string Razor = "Razor";
}
```

## Registrando Serviços por Chave

### Métodos de Registro Básico

```csharp
// Singleton por chave
services.AddKeyedSingletonService<IFileStorage, LocalFileStorage>(ServiceKeys.Local);
services.AddKeyedSingletonService<IFileStorage, AzureBlobStorage>(ServiceKeys.AzureBlob);

// Scoped por chave
services.AddKeyedScopedService<IEmailService, SmtpEmailService>(ServiceKeys.Smtp);
services.AddKeyedScopedService<IEmailService, SendGridEmailService>(ServiceKeys.SendGrid);

// Transient por chave
services.AddKeyedTransientService<IReportGenerator, PdfReportGenerator>("pdf");
services.AddKeyedTransientService<IReportGenerator, ExcelReportGenerator>("excel");
```

### Registro com Factory

```csharp
services.AddKeyedSingletonService<ICache>(ServiceKeys.Redis, (sp, key) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Redis");
    return new RedisCache(connectionString);
});
```

### Registro com TryAdd (Não Sobrescreve)

```csharp
// Só registra se não existir
services.TryAddKeyedSingletonService<IFileStorage, LocalFileStorage>(ServiceKeys.Local);
services.TryAddKeyedScopedService<IEmailService, SmtpEmailService>(ServiceKeys.Smtp);
services.TryAddKeyedTransientService<IReportGenerator, PdfReportGenerator>("pdf");
```

### Registro com Serviço Padrão

Os métodos `AddDefaultKeyed*` registram o serviço tanto com uma chave quanto sem chave, permitindo resolução por qualquer um dos métodos:

```csharp
// Registra SmtpEmailService como:
// 1. Serviço keyed com chave ServiceKeys.Default
// 2. Serviço padrão (sem chave)
services.AddDefaultKeyedSingletonService<IEmailService, SmtpEmailService>();

// Ambas as resoluções funcionam:
var byKey = sp.GetRequiredKeyedService<IEmailService>(ServiceKeys.Default);
var byDefault = sp.GetRequiredService<IEmailService>();
```

## Resolvendo Serviços por Chave

### Resolução Programática

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

### Resolução via Atributo

Use o atributo `[FromKeyedServices]` para injetar serviços específicos no construtor:

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

### Resolução em Controllers

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

## Cenários de Uso

### Multi-tenant com Diferentes Provedores

```csharp
// Registro
services.AddKeyedScopedService<IPaymentGateway, StripePaymentGateway>("stripe");
services.AddKeyedScopedService<IPaymentGateway, PayPalPaymentGateway>("paypal");
services.AddKeyedScopedService<IPaymentGateway, PagarMePaymentGateway>("pagarme");

// Uso
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
// Registro
services.AddKeyedSingletonService<ISearchEngine, ElasticSearchEngine>("elasticsearch");
services.AddKeyedSingletonService<ISearchEngine, LuceneSearchEngine>("lucene");
services.AddKeyedSingletonService<ISearchEngine, InMemorySearchEngine>("inmemory");

// Uso com feature flag
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

### Fallback e Resiliência

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
            _logger.LogWarning(ex, "Falha no storage primário, usando fallback");
            return await _fallbackStorage.ReadAsync(path);
        }
    }
}
```

## Substituindo Factories Customizadas

### Antes (Factory Customizada)

```csharp
// Factory antiga
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
        _ => throw new ArgumentException($"Provider desconhecido: {provider}")
    };
}

// Registro antigo
services.AddSingleton<IEmailServiceFactory, EmailServiceFactory>();
```

### Depois (Keyed Services)

```csharp
// Registro moderno
services.AddKeyedScopedService<IEmailService, SmtpEmailService>(ServiceKeys.Smtp);
services.AddKeyedScopedService<IEmailService, SendGridEmailService>(ServiceKeys.SendGrid);

// Uso direto
var service = sp.GetRequiredKeyedService<IEmailService>(ServiceKeys.Smtp);
```

## Testando Serviços por Chave

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

        // Assert - Deve ser o primeiro registrado
        Assert.IsType<LocalFileStorage>(storage);
    }
}
```

## Boas Práticas

1. **Use constantes para chaves**: Evite strings mágicas usando `ServiceKeys` ou suas próprias constantes
2. **Documente as chaves disponíveis**: Mantenha documentação clara sobre quais implementações estão disponíveis
3. **Prefira injeção via atributo**: Use `[FromKeyedServices]` quando possível para melhor legibilidade
4. **Combine com configuração**: Use `IConfiguration` para determinar qual chave usar em runtime
5. **Teste todas as implementações**: Garanta que cada implementação registrada funcione corretamente
6. **Use TryAdd para bibliotecas**: Permita que consumidores sobrescrevam implementações padrão

## Referências

- [Documentação oficial do .NET sobre Keyed Services](https://learn.microsoft.com/pt-br/dotnet/core/extensions/dependency-injection#keyed-services)
- [What's new in .NET 8 - Keyed DI services](https://learn.microsoft.com/pt-br/dotnet/core/whats-new/dotnet-8#keyed-di-services)

