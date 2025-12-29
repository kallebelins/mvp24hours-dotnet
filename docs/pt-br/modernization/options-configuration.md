# Configuração de Options

Este documento descreve a implementação do Options Pattern no Mvp24Hours, seguindo as melhores práticas do .NET 9 para configuração tipada.

## Visão Geral

O Options Pattern no .NET fornece uma abordagem fortemente tipada para acessar valores de configuração. O Mvp24Hours estende este padrão com:

- **API de validação fluente** - Contexto de validação fácil de usar
- **Validadores customizados** - Classes base para construir validadores
- **Suporte a DataAnnotations** - Validação automática de DataAnnotations
- **Validação no startup** - Falha rápida com `ValidateOnStart()`
- **Escaneamento de assembly** - Auto-registro de validadores

## Entendendo as Variantes de IOptions

O .NET fornece três interfaces para acessar options:

| Interface | Lifetime | Recarregamento | Caso de Uso |
|-----------|----------|----------------|-------------|
| `IOptions<T>` | Singleton | Não | Configuração estática que nunca muda |
| `IOptionsMonitor<T>` | Singleton | Sim (OnChange) | Configuração que pode mudar em runtime |
| `IOptionsSnapshot<T>` | Scoped | Sim (por request) | Configuração por request (ex: específica do tenant) |

### Quando Usar Cada Um

```csharp
// IOptions<T> - Melhor para configuração estática
public class DatabaseService
{
    private readonly DatabaseOptions _options;
    
    public DatabaseService(IOptions<DatabaseOptions> options)
    {
        _options = options.Value; // Lê uma vez, cached para sempre
    }
}

// IOptionsMonitor<T> - Melhor para mudanças em runtime
public class FeatureToggleService
{
    private readonly IOptionsMonitor<FeatureFlags> _options;
    
    public FeatureToggleService(IOptionsMonitor<FeatureFlags> options)
    {
        _options = options;
        _options.OnChange(flags => 
            Console.WriteLine($"Feature flags mudaram: {flags.EnableNewUI}"));
    }
    
    public bool IsNewUIEnabled => _options.CurrentValue.EnableNewUI;
}

// IOptionsSnapshot<T> - Melhor para configuração por request
public class TenantService
{
    private readonly TenantOptions _options;
    
    public TenantService(IOptionsSnapshot<TenantOptions> options)
    {
        _options = options.Value; // Fresco por request
    }
}
```

## Classes de Configuração

### Convenção de Nomenclatura

Todas as classes de configuração devem seguir a convenção de nomenclatura `*Options`:

```csharp
// ✅ Bom
public class DatabaseOptions { }
public class CachingOptions { }
public class RateLimitingOptions { }

// ❌ Evitar
public class DatabaseConfig { }
public class CachingSettings { }
```

### Usando Data Annotations

Adicione atributos de validação à sua classe de options:

```csharp
public class DatabaseOptions
{
    [Required(ErrorMessage = "ConnectionString é obrigatória.")]
    public string? ConnectionString { get; set; }

    [Range(1, 65535, ErrorMessage = "Port deve estar entre 1 e 65535.")]
    public int Port { get; set; } = 5432;

    [Range(1, 3600, ErrorMessage = "Timeout deve estar entre 1 e 3600 segundos.")]
    public int TimeoutSeconds { get; set; } = 30;

    [EmailAddress(ErrorMessage = "NotificationEmail deve ser um email válido.")]
    public string? NotificationEmail { get; set; }
}
```

## Métodos de Registro

### Registro Básico com Validação

```csharp
// Com validação de Data Annotations e fail-fast
services.AddOptionsWithValidation<DatabaseOptions>(
    configuration.GetSection("Database"),
    validateOnStart: true);
```

### Registro com Validador Customizado

```csharp
services.AddOptionsWithValidation<DatabaseOptions, DatabaseOptionsValidator>(
    configuration.GetSection("Database"),
    validateOnStart: true);
```

### Registro com Validação Inline

```csharp
services.AddOptionsWithValidation<DatabaseOptions>(
    configuration.GetSection("Database"),
    options => !string.IsNullOrEmpty(options.ConnectionString),
    "ConnectionString é obrigatória.");
```

### Registro para IOptionsMonitor

```csharp
services.AddOptionsForMonitor<FeatureFlags>(
    configuration.GetSection("Features"),
    options => Console.WriteLine($"Features mudaram: {options.EnableNewUI}"));
```

### Registro para IOptionsSnapshot

```csharp
services.AddOptionsForSnapshot<TenantOptions>(
    configuration.GetSection("Tenant"));
```

## Validadores Customizados

### Usando OptionsValidatorBase

Crie um validador customizado estendendo `OptionsValidatorBase<T>`:

```csharp
public class DatabaseOptionsValidator : OptionsValidatorBase<DatabaseOptions>
{
    protected override void ConfigureValidation(
        OptionsValidationContext<DatabaseOptions> context,
        DatabaseOptions options)
    {
        // Validar formato da connection string
        context.ValidateProperty(nameof(options.ConnectionString), options.ConnectionString)
            .NotNullOrEmpty("ConnectionString é obrigatória.")
            .Must(cs => cs?.Contains("Host=") == true, "ConnectionString deve conter Host.");

        // Validar range da porta
        context.ValidateProperty(nameof(options.Port), options.Port)
            .InRange(1, 65535, "Port deve estar entre 1 e 65535.");

        // Validar timeout
        context.ValidateTimeSpan(nameof(options.TimeoutSeconds), 
            TimeSpan.FromSeconds(options.TimeoutSeconds))
            .Positive("Timeout deve ser positivo.")
            .MaxValue(TimeSpan.FromHours(1), "Timeout não pode exceder 1 hora.");

        // Validação entre propriedades
        if (options.EnableSsl && string.IsNullOrEmpty(options.CertificatePath))
        {
            context.AddPropertyError(nameof(options.CertificatePath),
                "CertificatePath é obrigatório quando SSL está habilitado.");
        }
    }
}
```

### Usando SimpleOptionsValidatorBase

Para validação simples com um único predicado:

```csharp
public class ApiKeyValidator : SimpleOptionsValidatorBase<ApiOptions>
{
    protected override string FailureMessage 
        => "ApiKey é obrigatória e deve começar com 'API_'.";

    protected override bool IsValid(ApiOptions options)
        => !string.IsNullOrEmpty(options.ApiKey) && 
           options.ApiKey.StartsWith("API_");
}
```

### Usando DelegateOptionsValidator

Para validadores inline:

```csharp
var validator = DelegateOptionsValidator<ApiOptions>.Create(
    opts => !string.IsNullOrEmpty(opts.ApiKey),
    "ApiKey é obrigatória.");
```

### Validadores Compostos

Combine múltiplos validadores:

```csharp
var composite = new CompositeOptionsValidator<ApiOptions>(new[]
{
    new ApiKeyValidator(),
    DelegateOptionsValidator<ApiOptions>.Create(
        opts => opts.RateLimit > 0,
        "RateLimit deve ser positivo.")
});
```

## API do Contexto de Validação

O `OptionsValidationContext<T>` fornece uma API fluente para validação:

### Propriedades String

```csharp
context.ValidateProperty("Name", options.Name)
    .NotNullOrEmpty()
    .MaxLength(100)
    .MinLength(3)
    .Matches(@"^[a-zA-Z]+$", "Name deve conter apenas letras.")
    .IsEmail()
    .IsUri(UriKind.Absolute);
```

### Propriedades Numéricas

```csharp
context.ValidateProperty("Port", options.Port)
    .GreaterThan(0)
    .LessThan(65536)
    .InRange(1, 65535);
```

### Propriedades TimeSpan

```csharp
context.ValidateTimeSpan("Timeout", options.Timeout)
    .Positive()
    .NotNegative()
    .MinValue(TimeSpan.FromSeconds(1))
    .MaxValue(TimeSpan.FromHours(1));
```

### Propriedades URI

```csharp
context.ValidateUri("Endpoint", options.Endpoint)
    .NotNull()
    .IsAbsolute()
    .IsHttps()
    .HasScheme(new[] { "http", "https" });
```

### Validação Entre Propriedades

```csharp
context.AtLeastOne("ApiKey ou ClientCredentials deve ser configurado.",
    !string.IsNullOrEmpty(options.ApiKey),
    options.ClientCredentials != null);

context.ExactlyOne("Especifique Primary ou Fallback connection, não ambos.",
    options.UsePrimaryConnection,
    options.UseFallbackConnection);
```

## Escaneamento de Assembly

Registre todos os validadores de um assembly:

```csharp
services.AddOptionsValidatorsFromAssembly(typeof(Program).Assembly);

// Ou usando um tipo marcador
services.AddOptionsValidatorsFromAssemblyContaining<DatabaseOptionsValidator>();
```

## Boas Práticas

### 1. Sempre Use ValidateOnStart para Configuração Crítica

```csharp
services.AddOptionsWithValidation<DatabaseOptions>(
    configuration.GetSection("Database"),
    validateOnStart: true); // Falha rápido se inválido
```

### 2. Use IOptionsMonitor para Feature Flags

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

### 3. Use IOptionsSnapshot para Cenários Multi-Tenant

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

### 4. Combine DataAnnotations com Validação Customizada

```csharp
public class OptionsValidator : OptionsValidatorBase<MyOptions>
{
    // IncludeDataAnnotations é true por padrão
    // Validação customizada executa após DataAnnotations
    
    protected override void ConfigureValidation(
        OptionsValidationContext<MyOptions> context,
        MyOptions options)
    {
        // Validação de regra de negócio
        if (options.MaxRetries > 10 && !options.EnableCircuitBreaker)
        {
            context.AddError("CircuitBreaker deve estar habilitado para mais de 10 retries.");
        }
    }
}
```

### 5. Use Named Options para Múltiplas Instâncias

```csharp
services.Configure<DatabaseOptions>("Primary", 
    configuration.GetSection("Database:Primary"));
services.Configure<DatabaseOptions>("Reporting", 
    configuration.GetSection("Database:Reporting"));

// No seu serviço:
public class ReportService
{
    private readonly DatabaseOptions _options;
    
    public ReportService(IOptionsSnapshot<DatabaseOptions> options)
    {
        _options = options.Get("Reporting");
    }
}
```

## Exemplo de Configuração

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

## Veja Também

- [Time Provider](time-provider.md) - Abstraindo tempo para testabilidade
- [Periodic Timer](periodic-timer.md) - Padrões modernos de timer
- [HTTP Resilience](http-resilience.md) - Configuração de resiliência

