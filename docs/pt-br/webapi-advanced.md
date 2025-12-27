# ASP.NET Web API - Funcionalidades Avançadas

Esta documentação cobre as funcionalidades avançadas do módulo Mvp24Hours.WebAPI, incluindo segurança, performance, observabilidade e padrões de resiliência.

## Sumário

1. [Tratamento de Exceções e Problem Details](#tratamento-de-exceções-e-problem-details)
2. [Funcionalidades de Segurança](#funcionalidades-de-segurança)
3. [Rate Limiting](#rate-limiting)
4. [Performance e Cache](#performance-e-cache)
5. [Idempotência](#idempotência)
6. [Observabilidade e Logging](#observabilidade-e-logging)
7. [Versionamento de API](#versionamento-de-api)
8. [Negociação de Conteúdo](#negociação-de-conteúdo)
9. [Health Checks](#health-checks)
10. [Minimal APIs](#minimal-apis)

---

## Tratamento de Exceções e Problem Details

### Problem Details (RFC 7807)

O módulo implementa a RFC 7807 para respostas de erro padronizadas:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursProblemDetails(options =>
{
    options.IncludeExceptionDetails = !env.IsProduction();
    options.MapStatusCode = true;
    options.TypeUrlBase = "https://api.meuapp.com/errors/";
});

// Ou com configuração completa
services.AddMvp24HoursProblemDetailsAll(options =>
{
    options.IncludeExceptionDetails = false;
    options.TraceIdPropertyName = "traceId";
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursProblemDetails();
app.UseMvp24HoursExceptionHandling();
```

### Mapeamento Personalizado de Exceções

Mapeie exceções de domínio para códigos HTTP específicos:

```csharp
// Mapeador de exceção personalizado
public class MeuExceptionMapper : IExceptionToProblemDetailsMapper
{
    public bool CanMap(Exception exception) => 
        exception is DomainException;

    public ProblemDetails Map(Exception exception, HttpContext context)
    {
        var domainEx = (DomainException)exception;
        return new ProblemDetails
        {
            Status = 422,
            Title = "Erro de Domínio",
            Detail = domainEx.Message,
            Type = "https://api.meuapp.com/errors/domain-error"
        };
    }
}

// Registro
services.AddMvp24HoursExceptionMapper<MeuExceptionMapper>();
```

### Mapeamentos de Exceções Incorporados

A biblioteca mapeia automaticamente exceções do CQRS:
- `NotFoundException` → 404 Not Found
- `ConflictException` → 409 Conflict
- `ForbiddenException` → 403 Forbidden
- `UnauthorizedException` → 401 Unauthorized
- `ValidationException` → 400 Bad Request

---

## Funcionalidades de Segurança

### Security Headers

Adicione headers de segurança para proteger contra ataques comuns:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursSecurityHeaders(options =>
{
    options.EnableHSTS = true;
    options.HSTSMaxAge = TimeSpan.FromDays(365);
    options.EnableXFrameOptions = true;
    options.XFrameOptionsValue = "DENY";
    options.EnableXContentTypeOptions = true;
    options.EnableXXSSProtection = true;
    options.EnableContentSecurityPolicy = true;
    options.ContentSecurityPolicy = "default-src 'self'";
    options.EnableReferrerPolicy = true;
    options.ReferrerPolicy = "strict-origin-when-cross-origin";
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursSecurityHeaders();
```

### Autenticação por API Key

Proteja endpoints com API keys:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursApiKeyAuthentication(options =>
{
    options.HeaderName = "X-API-Key";
    options.QueryParameterName = "api_key";
    options.ValidApiKeys = new[] { "chave1", "chave2" };
    // Ou use um validador personalizado
    options.ApiKeyValidator = async (key, context) =>
    {
        var keyService = context.RequestServices.GetService<IApiKeyService>();
        return await keyService.ValidateAsync(key);
    };
    options.ExcludedPaths = new[] { "/health", "/swagger" };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursApiKeyAuthentication();
```

### Filtragem de IP

Whitelist ou blacklist de endereços IP:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursIpFiltering(options =>
{
    options.Mode = IpFilterMode.Whitelist; // ou Blacklist
    options.AllowedIps = new[] { "192.168.1.0/24", "10.0.0.1" };
    options.BlockedIps = new[] { "1.2.3.4" };
    options.AllowLocalhost = true;
    options.TrustProxyHeaders = true; // Confiar em X-Forwarded-For
    options.ResponseStatusCode = 403;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursIpFiltering();
```

### Limites de Tamanho de Requisição

Limite o tamanho do corpo das requisições:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestSizeLimit(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
    options.MaxMultipartBodyLength = 50 * 1024 * 1024; // 50 MB
    options.EndpointLimits = new Dictionary<string, long>
    {
        { "/api/upload", 100 * 1024 * 1024 }, // 100 MB para uploads
        { "/api/data", 1 * 1024 * 1024 } // 1 MB para dados
    };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRequestSizeLimit();
```

### Sanitização de Entrada

Sanitize entrada do usuário para prevenir XSS:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursInputSanitization(options =>
{
    options.SanitizeQueryStrings = true;
    options.SanitizeFormData = true;
    options.SanitizeJsonBody = true;
    options.RemoveHtmlTags = true;
    options.EncodeHtmlEntities = true;
    options.ExcludedPaths = new[] { "/api/raw-content" };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursInputSanitization();
```

### Anti-Forgery (Proteção CSRF)

Proteja contra ataques CSRF para SPAs:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursAntiForgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.CookieName = "XSRF-TOKEN";
    options.FormFieldName = "__RequestVerificationToken";
    options.SuppressXFrameOptionsHeader = false;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursAntiForgery();
```

### Segurança Completa (Tudo em Um)

Aplique todas as funcionalidades de segurança de uma vez:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursSecurity(options =>
{
    // Security Headers
    options.SecurityHeaders.EnableHSTS = true;
    
    // API Key
    options.ApiKey.Enabled = true;
    options.ApiKey.HeaderName = "X-API-Key";
    
    // Filtragem de IP
    options.IpFiltering.Enabled = true;
    options.IpFiltering.Mode = IpFilterMode.Whitelist;
    
    // Tamanho da Requisição
    options.RequestSize.MaxRequestBodySize = 10 * 1024 * 1024;
    
    // Sanitização
    options.InputSanitization.Enabled = true;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursSecurity();
```

---

## Rate Limiting

### Rate Limiting Básico

Proteja APIs contra abuso usando o rate limiter nativo do .NET 7+:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRateLimiting(options =>
{
    options.EnableRateLimiting = true;
    options.DefaultPolicy = "default";
    options.GlobalLimit = 100; // requisições por janela
    options.GlobalWindow = TimeSpan.FromMinutes(1);
    options.ReturnRetryAfterHeader = true;
    options.QueueLimit = 10;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRateLimiting();
```

### Políticas de Rate Limiting

Configure múltiplas políticas:

```csharp
services.AddMvp24HoursRateLimiting(options =>
{
    options.Policies = new Dictionary<string, RateLimitPolicy>
    {
        // Janela fixa
        ["fixed"] = new RateLimitPolicy
        {
            Type = RateLimitType.FixedWindow,
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        },
        // Janela deslizante
        ["sliding"] = new RateLimitPolicy
        {
            Type = RateLimitType.SlidingWindow,
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4
        },
        // Token bucket
        ["token-bucket"] = new RateLimitPolicy
        {
            Type = RateLimitType.TokenBucket,
            TokenLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = 10
        },
        // Concorrência
        ["concurrency"] = new RateLimitPolicy
        {
            Type = RateLimitType.Concurrency,
            PermitLimit = 10
        }
    };
});
```

### Rate Limiting Distribuído (Redis)

Para implantações multi-instância:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursDistributedRateLimiting(options =>
{
    options.RedisConnectionString = "localhost:6379";
    options.KeyPrefix = "meuapp:ratelimit:";
    options.DefaultPolicy = "distributed";
    options.Policies = new Dictionary<string, RateLimitPolicy>
    {
        ["distributed"] = new RateLimitPolicy
        {
            Type = RateLimitType.SlidingWindow,
            PermitLimit = 1000,
            Window = TimeSpan.FromMinutes(1)
        }
    };
});
```

### Gerador de Chave de Rate Limit Personalizado

Personalize como as chaves de rate limit são geradas:

```csharp
public class TenantRateLimitKeyGenerator : IRateLimitKeyGenerator
{
    public string GetKey(HttpContext context, string policy)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value ?? "anonymous";
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"{policy}:{tenantId}:{clientIp}";
    }
}

// Registro
services.AddMvp24HoursRateLimitKeyGenerator<TenantRateLimitKeyGenerator>();
```

### Headers de Rate Limit

O middleware adiciona automaticamente estes headers de resposta:
- `X-RateLimit-Limit` - Máximo de requisições permitidas
- `X-RateLimit-Remaining` - Requisições restantes na janela
- `X-RateLimit-Reset` - Timestamp Unix quando o limite reseta
- `Retry-After` - Segundos para aguardar (quando limitado)

---

## Performance e Cache

### Compressão de Resposta

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursCompression(options =>
{
    options.EnableGzip = true;
    options.EnableBrotli = true;
    options.CompressionLevel = CompressionLevel.Optimal;
    options.MimeTypes = new[]
    {
        "application/json",
        "application/xml",
        "text/plain",
        "text/html"
    };
    options.EnableForHttps = true;
    options.MinimumSizeBytes = 1024; // Tamanho mínimo para comprimir
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursResponseCompression();
```

### Descompressão de Requisição

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestDecompression(options =>
{
    options.EnableGzip = true;
    options.EnableDeflate = true;
    options.EnableBrotli = true;
    options.MaxDecompressedBodySize = 100 * 1024 * 1024; // 100 MB
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRequestDecompression();
```

### Cache de Resposta

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursResponseCaching(options =>
{
    options.MaximumBodySize = 64 * 1024 * 1024; // 64 MB
    options.UseCaseSensitivePaths = true;
    options.SizeLimit = 100 * 1024 * 1024; // 100 MB cache total
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursResponseCaching();
```

### Output Caching (.NET 7+)

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursOutputCaching(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);
    options.MaximumBodySize = 64 * 1024 * 1024;
    options.SizeLimit = 100 * 1024 * 1024;
    options.Policies = new Dictionary<string, OutputCachePolicy>
    {
        ["short"] = new OutputCachePolicy
        {
            Duration = TimeSpan.FromSeconds(30),
            VaryByQuery = new[] { "page", "limit" }
        },
        ["long"] = new OutputCachePolicy
        {
            Duration = TimeSpan.FromHours(1),
            VaryByHeader = new[] { "Accept-Language" }
        }
    };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursOutputCaching();
```

### Suporte a ETag

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursETag(options =>
{
    options.Algorithm = ETagAlgorithm.SHA256; // ou MD5, SHA1
    options.UseWeakETags = false;
    options.ExcludedPaths = new[] { "/api/stream" };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursETag();
```

### Timeout de Requisição

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestTimeout(options =>
{
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.EndpointTimeouts = new Dictionary<string, TimeSpan>
    {
        { "/api/long-operation", TimeSpan.FromMinutes(5) },
        { "/api/quick", TimeSpan.FromSeconds(5) }
    };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRequestTimeout();
```

### Headers de Cache Control

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursCacheControl(options =>
{
    options.DefaultMaxAge = TimeSpan.FromMinutes(5);
    options.DefaultPrivate = false;
    options.DefaultNoStore = false;
    options.RouteProfiles = new Dictionary<string, CacheControlProfile>
    {
        ["/api/public/*"] = new CacheControlProfile
        {
            MaxAge = TimeSpan.FromHours(1),
            Public = true
        },
        ["/api/user/*"] = new CacheControlProfile
        {
            MaxAge = TimeSpan.FromMinutes(5),
            Private = true
        },
        ["/api/sensitive/*"] = new CacheControlProfile
        {
            NoStore = true,
            NoCache = true
        }
    };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursCacheControl();
```

---

## Idempotência

Garanta que requisições POST/PUT/PATCH sejam idempotentes:

### Configuração Básica

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursIdempotency(options =>
{
    options.Enabled = true;
    options.HeaderName = "Idempotency-Key";
    options.CacheDuration = TimeSpan.FromHours(24);
    options.HttpMethods = new[] { "POST", "PUT", "PATCH" };
    options.ExcludedPaths = new[] { "/api/webhook" };
    options.ReturnRetryAfterOnDuplicate = true;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursIdempotency();
```

### Armazenamento em Memória

```csharp
services.AddMvp24HoursIdempotencyInMemory(options =>
{
    options.Enabled = true;
    options.CacheDuration = TimeSpan.FromHours(24);
    options.SizeLimit = 10000; // Máx de entradas em memória
});
```

### Armazenamento Distribuído (Redis/SQL)

```csharp
services.AddMvp24HoursIdempotencyDistributed(options =>
{
    options.Enabled = true;
    options.CacheDuration = TimeSpan.FromHours(24);
});

// Usa IDistributedCache, então configure seu provedor:
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

### Gerador de Chave de Idempotência Personalizado

```csharp
public class CustomIdempotencyKeyGenerator : IIdempotencyKeyGenerator
{
    public string GenerateKey(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value ?? "anonymous";
        var headerKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        return $"{userId}:{headerKey}";
    }
}

// Registro
services.AddMvp24HoursIdempotencyKeyGenerator<CustomIdempotencyKeyGenerator>();
```

### Integração com CQRS

Ao usar comandos CQRS com `IIdempotentCommand`:

```csharp
public class CriarPedidoCommand : ICommand<ResultadoPedido>, IIdempotentCommand
{
    public Guid IdempotencyKey { get; set; }
    public string ClienteId { get; set; }
    public List<ItemPedido> Itens { get; set; }
}
```

O middleware extrai e usa automaticamente a chave de idempotência dos comandos CQRS.

---

## Observabilidade e Logging

### Contexto de Requisição

Propague IDs de correlação e contexto de requisição:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestContext(options =>
{
    options.CorrelationIdHeader = "X-Correlation-ID";
    options.CausationIdHeader = "X-Causation-ID";
    options.GenerateCorrelationIdIfMissing = true;
    options.IncludeInResponse = true;
    options.PropagateToOutgoingRequests = true;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRequestContext();
```

### Logging de Requisições

Registre todas as requisições com detalhes configuráveis:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestLogging(options =>
{
    options.LogRequestHeaders = true;
    options.LogRequestBody = true;
    options.LogResponseHeaders = true;
    options.LogResponseBody = true;
    options.MaxBodyLogSize = 32768; // 32 KB
    options.SensitiveHeaders = new[] { "Authorization", "X-API-Key" };
    options.SensitiveProperties = new[] { "password", "creditCard" };
    options.ExcludedPaths = new[] { "/health", "/metrics" };
    options.LogLevel = LogLevel.Information;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRequestLogging();
```

### Logger de Requisição Personalizado

```csharp
public class CustomRequestLogger : IRequestLogger
{
    private readonly ILogger<CustomRequestLogger> _logger;

    public CustomRequestLogger(ILogger<CustomRequestLogger> logger)
    {
        _logger = logger;
    }

    public async Task LogRequestAsync(HttpContext context, RequestLogData data)
    {
        _logger.LogInformation(
            "Requisição {Method} {Path} de {IP} com CorrelationId {CorrelationId}",
            data.Method, data.Path, data.ClientIp, data.CorrelationId);
    }

    public async Task LogResponseAsync(HttpContext context, ResponseLogData data)
    {
        _logger.LogInformation(
            "Resposta {StatusCode} para {Method} {Path} em {Duration}ms",
            data.StatusCode, data.Method, data.Path, data.Duration.TotalMilliseconds);
    }
}

// Registro
services.AddMvp24HoursRequestLogging<CustomRequestLogger>(options => { });
```

### Telemetria de Requisição (Prometheus-compatível)

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestTelemetry(options =>
{
    options.Enabled = true;
    options.MeterName = "meuapp.webapi";
    options.HistogramBuckets = new[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 };
    options.RecordRequestSize = true;
    options.RecordResponseSize = true;
    options.IncludeMethod = true;
    options.IncludePath = true;
    options.IncludeStatusCode = true;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRequestTelemetry();
```

### Stack Completa de Observabilidade

Configure todas as funcionalidades de observabilidade de uma vez:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestObservability(options =>
{
    // Contexto de Requisição
    options.Context.CorrelationIdHeader = "X-Correlation-ID";
    options.Context.GenerateCorrelationIdIfMissing = true;
    
    // Logging
    options.Logging.LogRequestBody = true;
    options.Logging.LogResponseBody = false;
    
    // Telemetria
    options.Telemetry.Enabled = true;
    options.Telemetry.MeterName = "meuapp.webapi";
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursFullObservability();
```

### Propagação de Correlation ID para HTTP Clients

```csharp
// Startup.cs => ConfigureServices
services.AddHttpClient("downstream-service")
    .AddMvp24HoursCorrelationIdHandler();
```

---

## Versionamento de API

### Versionamento Básico de API

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursApiVersioning(options =>
{
    options.DefaultVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    
    // Leitores de versão
    options.EnableUrlSegmentVersioning = true;  // /api/v1/...
    options.EnableQueryStringVersioning = true; // ?api-version=1.0
    options.EnableHeaderVersioning = true;      // X-Api-Version: 1.0
    options.EnableMediaTypeVersioning = false;  // Accept: application/json;v=1.0
    
    options.VersionHeaderName = "X-Api-Version";
    options.QueryStringParameterName = "api-version";
});
```

### Swagger com Versionamento de API

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursSwaggerWithVersioning(options =>
{
    options.Title = "Minha API";
    options.Description = "Descrição da Minha API";
    options.ContactName = "Suporte API";
    options.ContactEmail = "suporte@minhaapi.com";
    options.LicenseName = "MIT";
    
    options.Versions = new[]
    {
        new ApiVersionDescription(new ApiVersion(1, 0), "v1", false),
        new ApiVersionDescription(new ApiVersion(2, 0), "v2", false),
        new ApiVersionDescription(new ApiVersion(3, 0), "v3-beta", true)
    };
    
    options.IncludeXmlComments = true;
    options.XmlCommentsFilePath = "MinhaApi.xml";
    
    // Segurança
    options.EnableBearerAuth = true;
    options.EnableApiKeyAuth = true;
    options.ApiKeyHeaderName = "X-API-Key";
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursSwaggerWithVersioning();
```

### Integração com ReDoc

```csharp
// Startup.cs => Configure
app.UseMvp24HoursReDoc(options =>
{
    options.RoutePrefix = "api-docs";
    options.DocumentTitle = "Documentação da Minha API";
    options.ExpandResponses = "200,201";
    options.PathInMiddlePanel = true;
    options.NativeScrollbars = true;
    options.HideHostname = false;
    options.HideDownloadButton = false;
    options.RequiredPropsFirst = true;
});
```

---

## Negociação de Conteúdo

### Configuração Básica

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursContentNegotiation(options =>
{
    options.DefaultContentType = "application/json";
    options.SupportedMediaTypes = new[]
    {
        "application/json",
        "application/xml",
        "text/xml"
    };
    options.RespectBrowserAcceptHeader = true;
    options.ReturnHttpNotAcceptable = true;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursContentNegotiation();
```

### Formatadores de Conteúdo Personalizados

```csharp
public class CsvContentFormatter : IContentFormatter
{
    public string ContentType => "text/csv";
    public string[] SupportedMediaTypes => new[] { "text/csv", "application/csv" };

    public bool CanFormat(Type type) => 
        typeof(IEnumerable).IsAssignableFrom(type);

    public async Task WriteAsync(HttpResponse response, object value)
    {
        response.ContentType = ContentType;
        var csv = ConvertToCsv(value);
        await response.WriteAsync(csv);
    }

    public async Task<object> ReadAsync(HttpRequest request, Type targetType)
    {
        using var reader = new StreamReader(request.Body);
        var csv = await reader.ReadToEndAsync();
        return ParseCsv(csv, targetType);
    }
}

// Registro
services.AddContentFormatter<CsvContentFormatter>();
```

### Suporte JSON e XML

```csharp
// JSON com configurações personalizadas
services.AddMvp24HoursContentNegotiationJson(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Suporte XML
services.AddMvp24HoursContentNegotiationXml(options =>
{
    options.UseDataContractSerializer = false;
    options.WriterSettings = new XmlWriterSettings
    {
        Indent = true,
        OmitXmlDeclaration = false
    };
});
```

### Negociação de Conteúdo Estrita

Rejeite requisições com headers Accept não suportados:

```csharp
services.AddMvp24HoursStrictContentNegotiation();
```

---

## Health Checks

### Health Checks Básicos

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursHealthChecks(options =>
{
    options.HealthPath = "/health";
    options.ReadyPath = "/health/ready";
    options.LivePath = "/health/live";
    options.IncludeDetails = true;
    options.ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursHealthChecks();
```

### Health Check de Cache

```csharp
services.AddMvp24HoursHealthChecks()
    .AddMvp24HoursCacheCheck(options =>
    {
        options.Name = "cache";
        options.Tags = new[] { "ready" };
        options.FailureStatus = HealthStatus.Degraded;
    });
```

### Health Check do RabbitMQ

```csharp
services.AddMvp24HoursHealthChecks()
    .AddMvp24HoursRabbitMQCheck(options =>
    {
        options.Name = "rabbitmq";
        options.ConnectionString = "amqp://localhost";
        options.Tags = new[] { "ready", "messaging" };
        options.FailureStatus = HealthStatus.Unhealthy;
    });
```

### Health Check Personalizado

```csharp
public class MeuServicoHealthCheck : BaseHealthCheck
{
    private readonly IMeuServico _servico;

    public MeuServicoHealthCheck(IMeuServico servico)
    {
        _servico = servico;
    }

    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            var isHealthy = await _servico.PingAsync(cancellationToken);
            return isHealthy 
                ? HealthCheckResult.Healthy("Serviço está respondendo")
                : HealthCheckResult.Degraded("Serviço está lento");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Serviço está fora do ar", ex);
        }
    }
}
```

---

## Minimal APIs

### Model Binders para Minimal APIs

```csharp
// Filtro personalizado
public class ClienteFilter : ExtensionBinder<ClienteFilter>
{
    public string Nome { get; set; }
    public bool? Ativo { get; set; }
    public DateTime? CriadoApos { get; set; }
}

// Uso
app.MapGet("/api/clientes", async (
    ClienteFilter filtro, 
    ModelBinder<PagingCriteriaRequest> paging,
    [FromServices] IClienteService service) =>
{
    if (paging.Error != null)
        return Results.BadRequest(paging.Error);
    
    var result = await service.GetByFilterAsync(filtro, paging.Data);
    return Results.Ok(result);
})
.WithName("GetClientes")
.WithOpenApi();
```

### Integração CQRS com Minimal APIs

```csharp
// Mapear endpoint de comando
app.MapCommand<CriarClienteCommand, ClienteDto>("/api/clientes")
    .WithName("CriarCliente")
    .RequireAuthorization();

// Mapear endpoint de query
app.MapQuery<ObterClientePorIdQuery, ClienteDto>("/api/clientes/{id}")
    .WithName("ObterClientePorId");

// Com filtro de validação
app.MapPost("/api/pedidos", async (CriarPedidoCommand command, IMediator mediator) =>
{
    var result = await mediator.SendAsync(command);
    return Results.Ok(result);
})
.AddEndpointFilter<ValidationEndpointFilter<CriarPedidoCommand>>();
```

### Grupos de Endpoints

```csharp
// Definir grupo de endpoints
var clientesGroup = app.MapGroup("/api/clientes")
    .WithTags("Clientes")
    .RequireAuthorization()
    .AddEndpointFilter<ValidationEndpointFilter>();

clientesGroup.MapGet("/", ObterClientes);
clientesGroup.MapGet("/{id}", ObterClientePorId);
clientesGroup.MapPost("/", CriarCliente);
clientesGroup.MapPut("/{id}", AtualizarCliente);
clientesGroup.MapDelete("/{id}", DeletarCliente);
```

### Typed Results

```csharp
app.MapGet("/api/clientes/{id}", async (Guid id, IClienteService service) =>
{
    var cliente = await service.GetByIdAsync(id);
    return cliente is not null
        ? TypedResults.Ok(cliente)
        : TypedResults.NotFound(new ProblemDetails
        {
            Status = 404,
            Title = "Cliente não encontrado",
            Detail = $"Cliente com ID {id} não foi encontrado"
        });
})
.Produces<ClienteDto>(200)
.ProducesProblem(404);
```

---

## Exemplo de Configuração Completa

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Serviços core
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursWebJson();
builder.Services.AddMvp24HoursModelBinders();

// Tratamento de exceções
builder.Services.AddMvp24HoursProblemDetailsAll(options =>
{
    options.IncludeExceptionDetails = !builder.Environment.IsProduction();
});

// Segurança
builder.Services.AddMvp24HoursSecurity(options =>
{
    options.SecurityHeaders.EnableHSTS = true;
    options.InputSanitization.Enabled = true;
});

// Rate limiting
builder.Services.AddMvp24HoursRateLimiting(options =>
{
    options.GlobalLimit = 100;
    options.GlobalWindow = TimeSpan.FromMinutes(1);
});

// Cache
builder.Services.AddMvp24HoursCompression();
builder.Services.AddMvp24HoursResponseCaching();
builder.Services.AddMvp24HoursETag();

// Idempotência
builder.Services.AddMvp24HoursIdempotencyDistributed(options =>
{
    options.CacheDuration = TimeSpan.FromHours(24);
});

// Observabilidade
builder.Services.AddMvp24HoursRequestObservability();

// Documentação da API
builder.Services.AddMvp24HoursSwaggerWithVersioning(options =>
{
    options.Title = "Minha API";
    options.EnableBearerAuth = true;
});

// Health checks
builder.Services.AddMvp24HoursHealthChecks();

var app = builder.Build();

// Pipeline de middleware (a ordem importa!)
app.UseMvp24HoursProblemDetails();
app.UseMvp24HoursExceptionHandling();
app.UseMvp24HoursRequestContext();
app.UseMvp24HoursRequestLogging();
app.UseMvp24HoursSecurity();
app.UseMvp24HoursRateLimiting();
app.UseMvp24HoursResponseCompression();
app.UseMvp24HoursETag();
app.UseMvp24HoursIdempotency();
app.UseMvp24HoursHealthChecks();

if (!app.Environment.IsProduction())
{
    app.UseMvp24HoursSwaggerWithVersioning();
    app.UseMvp24HoursReDoc();
}

app.MapControllers();
app.Run();
```

---

## Veja Também

- [Configuração Básica WebAPI](webapi.md)
- [Integração CQRS](cqrs/home.md)
- [Health Checks](database/efcore-advanced.md#health-checks)
- [Observabilidade e Tracing](cqrs/observability/tracing.md)

