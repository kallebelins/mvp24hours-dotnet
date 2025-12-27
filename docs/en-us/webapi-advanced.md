# ASP.NET Web API - Advanced Features

This documentation covers the advanced features of the Mvp24Hours.WebAPI module, including security, performance, observability, and resilience patterns.

## Table of Contents

1. [Exception Handling & Problem Details](#exception-handling--problem-details)
2. [Security Features](#security-features)
3. [Rate Limiting](#rate-limiting)
4. [Performance & Caching](#performance--caching)
5. [Idempotency](#idempotency)
6. [Observability & Logging](#observability--logging)
7. [API Versioning](#api-versioning)
8. [Content Negotiation](#content-negotiation)
9. [Health Checks](#health-checks)
10. [Minimal APIs](#minimal-apis)

---

## Exception Handling & Problem Details

### Problem Details (RFC 7807)

The module implements RFC 7807 for standardized error responses:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursProblemDetails(options =>
{
    options.IncludeExceptionDetails = !env.IsProduction();
    options.MapStatusCode = true;
    options.TypeUrlBase = "https://api.myapp.com/errors/";
});

// Or with full configuration
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

### Custom Exception Mapping

Map domain exceptions to specific HTTP status codes:

```csharp
// Custom exception mapper
public class MyExceptionMapper : IExceptionToProblemDetailsMapper
{
    public bool CanMap(Exception exception) => 
        exception is DomainException;

    public ProblemDetails Map(Exception exception, HttpContext context)
    {
        var domainEx = (DomainException)exception;
        return new ProblemDetails
        {
            Status = 422,
            Title = "Domain Error",
            Detail = domainEx.Message,
            Type = "https://api.myapp.com/errors/domain-error"
        };
    }
}

// Registration
services.AddMvp24HoursExceptionMapper<MyExceptionMapper>();
```

### Built-in Exception Mappings

The library automatically maps CQRS exceptions:
- `NotFoundException` → 404 Not Found
- `ConflictException` → 409 Conflict
- `ForbiddenException` → 403 Forbidden
- `UnauthorizedException` → 401 Unauthorized
- `ValidationException` → 400 Bad Request

---

## Security Features

### Security Headers

Add security headers to protect against common attacks:

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

### API Key Authentication

Protect endpoints with API keys:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursApiKeyAuthentication(options =>
{
    options.HeaderName = "X-API-Key";
    options.QueryParameterName = "api_key";
    options.ValidApiKeys = new[] { "key1", "key2" };
    // Or use a custom validator
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

### IP Filtering

Whitelist or blacklist IP addresses:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursIpFiltering(options =>
{
    options.Mode = IpFilterMode.Whitelist; // or Blacklist
    options.AllowedIps = new[] { "192.168.1.0/24", "10.0.0.1" };
    options.BlockedIps = new[] { "1.2.3.4" };
    options.AllowLocalhost = true;
    options.TrustProxyHeaders = true; // Trust X-Forwarded-For
    options.ResponseStatusCode = 403;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursIpFiltering();
```

### Request Size Limits

Limit request body sizes:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestSizeLimit(options =>
{
    options.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
    options.MaxMultipartBodyLength = 50 * 1024 * 1024; // 50 MB
    options.EndpointLimits = new Dictionary<string, long>
    {
        { "/api/upload", 100 * 1024 * 1024 }, // 100 MB for uploads
        { "/api/data", 1 * 1024 * 1024 } // 1 MB for data
    };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRequestSizeLimit();
```

### Input Sanitization

Sanitize user input to prevent XSS:

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

### Anti-Forgery (CSRF Protection)

Protect against CSRF attacks for SPAs:

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

### All-in-One Security

Apply all security features at once:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursSecurity(options =>
{
    // Security Headers
    options.SecurityHeaders.EnableHSTS = true;
    
    // API Key
    options.ApiKey.Enabled = true;
    options.ApiKey.HeaderName = "X-API-Key";
    
    // IP Filtering
    options.IpFiltering.Enabled = true;
    options.IpFiltering.Mode = IpFilterMode.Whitelist;
    
    // Request Size
    options.RequestSize.MaxRequestBodySize = 10 * 1024 * 1024;
    
    // Input Sanitization
    options.InputSanitization.Enabled = true;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursSecurity();
```

---

## Rate Limiting

### Basic Rate Limiting

Protect APIs from abuse using .NET 7+ built-in rate limiters:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRateLimiting(options =>
{
    options.EnableRateLimiting = true;
    options.DefaultPolicy = "default";
    options.GlobalLimit = 100; // requests per window
    options.GlobalWindow = TimeSpan.FromMinutes(1);
    options.ReturnRetryAfterHeader = true;
    options.QueueLimit = 10;
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursRateLimiting();
```

### Rate Limiting Policies

Configure multiple policies:

```csharp
services.AddMvp24HoursRateLimiting(options =>
{
    options.Policies = new Dictionary<string, RateLimitPolicy>
    {
        // Fixed window
        ["fixed"] = new RateLimitPolicy
        {
            Type = RateLimitType.FixedWindow,
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        },
        // Sliding window
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
        // Concurrency
        ["concurrency"] = new RateLimitPolicy
        {
            Type = RateLimitType.Concurrency,
            PermitLimit = 10
        }
    };
});
```

### Distributed Rate Limiting (Redis)

For multi-instance deployments:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursDistributedRateLimiting(options =>
{
    options.RedisConnectionString = "localhost:6379";
    options.KeyPrefix = "myapp:ratelimit:";
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

### Custom Rate Limit Key Generator

Customize how rate limit keys are generated:

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

// Registration
services.AddMvp24HoursRateLimitKeyGenerator<TenantRateLimitKeyGenerator>();
```

### Rate Limit Headers

The middleware automatically adds these response headers:
- `X-RateLimit-Limit` - Maximum requests allowed
- `X-RateLimit-Remaining` - Remaining requests in window
- `X-RateLimit-Reset` - Unix timestamp when the limit resets
- `Retry-After` - Seconds to wait (when rate limited)

---

## Performance & Caching

### Response Compression

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
    options.MinimumSizeBytes = 1024; // Minimum size to compress
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursResponseCompression();
```

### Request Decompression

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

### Response Caching

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursResponseCaching(options =>
{
    options.MaximumBodySize = 64 * 1024 * 1024; // 64 MB
    options.UseCaseSensitivePaths = true;
    options.SizeLimit = 100 * 1024 * 1024; // 100 MB total cache
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

### ETag Support

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursETag(options =>
{
    options.Algorithm = ETagAlgorithm.SHA256; // or MD5, SHA1
    options.UseWeakETags = false;
    options.ExcludedPaths = new[] { "/api/stream" };
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursETag();
```

### Request Timeout

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

### Cache Control Headers

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

## Idempotency

Ensure POST/PUT/PATCH requests are idempotent:

### Basic Configuration

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

### In-Memory Store

```csharp
services.AddMvp24HoursIdempotencyInMemory(options =>
{
    options.Enabled = true;
    options.CacheDuration = TimeSpan.FromHours(24);
    options.SizeLimit = 10000; // Max entries in memory
});
```

### Distributed Store (Redis/SQL)

```csharp
services.AddMvp24HoursIdempotencyDistributed(options =>
{
    options.Enabled = true;
    options.CacheDuration = TimeSpan.FromHours(24);
});

// Uses IDistributedCache, so configure your provider:
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

### Custom Idempotency Key Generator

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

// Registration
services.AddMvp24HoursIdempotencyKeyGenerator<CustomIdempotencyKeyGenerator>();
```

### Integration with CQRS

When using CQRS commands with `IIdempotentCommand`:

```csharp
public class CreateOrderCommand : ICommand<OrderResult>, IIdempotentCommand
{
    public Guid IdempotencyKey { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
}
```

The middleware automatically extracts and uses the idempotency key from CQRS commands.

---

## Observability & Logging

### Request Context

Propagate correlation IDs and request context:

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

### Request Logging

Log all incoming requests with configurable details:

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

### Custom Request Logger

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
            "Request {Method} {Path} from {IP} with CorrelationId {CorrelationId}",
            data.Method, data.Path, data.ClientIp, data.CorrelationId);
    }

    public async Task LogResponseAsync(HttpContext context, ResponseLogData data)
    {
        _logger.LogInformation(
            "Response {StatusCode} for {Method} {Path} in {Duration}ms",
            data.StatusCode, data.Method, data.Path, data.Duration.TotalMilliseconds);
    }
}

// Registration
services.AddMvp24HoursRequestLogging<CustomRequestLogger>(options => { });
```

### Request Telemetry (Prometheus-compatible)

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestTelemetry(options =>
{
    options.Enabled = true;
    options.MeterName = "myapp.webapi";
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

### Full Observability Stack

Configure all observability features at once:

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursRequestObservability(options =>
{
    // Request Context
    options.Context.CorrelationIdHeader = "X-Correlation-ID";
    options.Context.GenerateCorrelationIdIfMissing = true;
    
    // Logging
    options.Logging.LogRequestBody = true;
    options.Logging.LogResponseBody = false;
    
    // Telemetry
    options.Telemetry.Enabled = true;
    options.Telemetry.MeterName = "myapp.webapi";
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursFullObservability();
```

### Correlation ID Propagation to HTTP Clients

```csharp
// Startup.cs => ConfigureServices
services.AddHttpClient("downstream-service")
    .AddMvp24HoursCorrelationIdHandler();
```

---

## API Versioning

### Basic API Versioning

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursApiVersioning(options =>
{
    options.DefaultVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    
    // Version readers
    options.EnableUrlSegmentVersioning = true;  // /api/v1/...
    options.EnableQueryStringVersioning = true; // ?api-version=1.0
    options.EnableHeaderVersioning = true;      // X-Api-Version: 1.0
    options.EnableMediaTypeVersioning = false;  // Accept: application/json;v=1.0
    
    options.VersionHeaderName = "X-Api-Version";
    options.QueryStringParameterName = "api-version";
});
```

### Swagger with API Versioning

```csharp
// Startup.cs => ConfigureServices
services.AddMvp24HoursSwaggerWithVersioning(options =>
{
    options.Title = "My API";
    options.Description = "My API Description";
    options.ContactName = "API Support";
    options.ContactEmail = "support@myapi.com";
    options.LicenseName = "MIT";
    
    options.Versions = new[]
    {
        new ApiVersionDescription(new ApiVersion(1, 0), "v1", false),
        new ApiVersionDescription(new ApiVersion(2, 0), "v2", false),
        new ApiVersionDescription(new ApiVersion(3, 0), "v3-beta", true)
    };
    
    options.IncludeXmlComments = true;
    options.XmlCommentsFilePath = "MyApi.xml";
    
    // Security
    options.EnableBearerAuth = true;
    options.EnableApiKeyAuth = true;
    options.ApiKeyHeaderName = "X-API-Key";
});
```

```csharp
// Startup.cs => Configure
app.UseMvp24HoursSwaggerWithVersioning();
```

### ReDoc Integration

```csharp
// Startup.cs => Configure
app.UseMvp24HoursReDoc(options =>
{
    options.RoutePrefix = "api-docs";
    options.DocumentTitle = "My API Documentation";
    options.ExpandResponses = "200,201";
    options.PathInMiddlePanel = true;
    options.NativeScrollbars = true;
    options.HideHostname = false;
    options.HideDownloadButton = false;
    options.RequiredPropsFirst = true;
});
```

---

## Content Negotiation

### Basic Configuration

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

### Custom Content Formatters

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

// Registration
services.AddContentFormatter<CsvContentFormatter>();
```

### JSON and XML Support

```csharp
// JSON with custom settings
services.AddMvp24HoursContentNegotiationJson(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// XML support
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

### Strict Content Negotiation

Reject requests with unsupported Accept headers:

```csharp
services.AddMvp24HoursStrictContentNegotiation();
```

---

## Health Checks

### Basic Health Checks

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

### Cache Health Check

```csharp
services.AddMvp24HoursHealthChecks()
    .AddMvp24HoursCacheCheck(options =>
    {
        options.Name = "cache";
        options.Tags = new[] { "ready" };
        options.FailureStatus = HealthStatus.Degraded;
    });
```

### RabbitMQ Health Check

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

### Custom Health Check

```csharp
public class MyServiceHealthCheck : BaseHealthCheck
{
    private readonly IMyService _service;

    public MyServiceHealthCheck(IMyService service)
    {
        _service = service;
    }

    protected override async Task<HealthCheckResult> CheckHealthCoreAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            var isHealthy = await _service.PingAsync(cancellationToken);
            return isHealthy 
                ? HealthCheckResult.Healthy("Service is responding")
                : HealthCheckResult.Degraded("Service is slow");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Service is down", ex);
        }
    }
}
```

---

## Minimal APIs

### Model Binders for Minimal APIs

```csharp
// Custom filter model
public class CustomerFilter : ExtensionBinder<CustomerFilter>
{
    public string Name { get; set; }
    public bool? Active { get; set; }
    public DateTime? CreatedAfter { get; set; }
}

// Usage
app.MapGet("/api/customers", async (
    CustomerFilter filter, 
    ModelBinder<PagingCriteriaRequest> paging,
    [FromServices] ICustomerService service) =>
{
    if (paging.Error != null)
        return Results.BadRequest(paging.Error);
    
    var result = await service.GetByFilterAsync(filter, paging.Data);
    return Results.Ok(result);
})
.WithName("GetCustomers")
.WithOpenApi();
```

### CQRS Integration with Minimal APIs

```csharp
// Map a command endpoint
app.MapCommand<CreateCustomerCommand, CustomerDto>("/api/customers")
    .WithName("CreateCustomer")
    .RequireAuthorization();

// Map a query endpoint
app.MapQuery<GetCustomerByIdQuery, CustomerDto>("/api/customers/{id}")
    .WithName("GetCustomerById");

// With validation filter
app.MapPost("/api/orders", async (CreateOrderCommand command, IMediator mediator) =>
{
    var result = await mediator.SendAsync(command);
    return Results.Ok(result);
})
.AddEndpointFilter<ValidationEndpointFilter<CreateOrderCommand>>();
```

### Endpoint Groups

```csharp
// Define endpoint group
var customersGroup = app.MapGroup("/api/customers")
    .WithTags("Customers")
    .RequireAuthorization()
    .AddEndpointFilter<ValidationEndpointFilter>();

customersGroup.MapGet("/", GetCustomers);
customersGroup.MapGet("/{id}", GetCustomerById);
customersGroup.MapPost("/", CreateCustomer);
customersGroup.MapPut("/{id}", UpdateCustomer);
customersGroup.MapDelete("/{id}", DeleteCustomer);
```

### Typed Results

```csharp
app.MapGet("/api/customers/{id}", async (Guid id, ICustomerService service) =>
{
    var customer = await service.GetByIdAsync(id);
    return customer is not null
        ? TypedResults.Ok(customer)
        : TypedResults.NotFound(new ProblemDetails
        {
            Status = 404,
            Title = "Customer not found",
            Detail = $"Customer with ID {id} was not found"
        });
})
.Produces<CustomerDto>(200)
.ProducesProblem(404);
```

---

## Complete Configuration Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Core services
builder.Services.AddMvp24HoursWebEssential();
builder.Services.AddMvp24HoursWebJson();
builder.Services.AddMvp24HoursModelBinders();

// Exception handling
builder.Services.AddMvp24HoursProblemDetailsAll(options =>
{
    options.IncludeExceptionDetails = !builder.Environment.IsProduction();
});

// Security
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

// Caching
builder.Services.AddMvp24HoursCompression();
builder.Services.AddMvp24HoursResponseCaching();
builder.Services.AddMvp24HoursETag();

// Idempotency
builder.Services.AddMvp24HoursIdempotencyDistributed(options =>
{
    options.CacheDuration = TimeSpan.FromHours(24);
});

// Observability
builder.Services.AddMvp24HoursRequestObservability();

// API documentation
builder.Services.AddMvp24HoursSwaggerWithVersioning(options =>
{
    options.Title = "My API";
    options.EnableBearerAuth = true;
});

// Health checks
builder.Services.AddMvp24HoursHealthChecks();

var app = builder.Build();

// Middleware pipeline (order matters!)
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

## See Also

- [Basic WebAPI Configuration](webapi.md)
- [CQRS Integration](cqrs/home.md)
- [Health Checks](database/efcore-advanced.md#health-checks)
- [Observability & Tracing](cqrs/observability/tracing.md)

