# Web API advanced configuration

This page is the configuration reference for `Mvp24Hours.WebAPI` on .NET 10. The option names and defaults below come from `src/Mvp24Hours.WebAPI/Configuration` and the `Mvp24Hours.WebAPI.Test` configuration tests.

## Registration and middleware

Register only the features your API uses, then add their middleware in a deliberate order:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvp24HoursProblemDetails();
builder.Services.AddMvp24HoursRequestContext();
builder.Services.AddMvp24HoursRequestObservability();
builder.Services.AddMvp24HoursSecurityHeaders();
builder.Services.AddMvp24HoursRateLimiting(o =>
    o.AddDefaultPolicy(100, TimeSpan.FromMinutes(1)));
builder.Services.AddMvp24HoursOutputCache();

var app = builder.Build();
app.UseMvp24HoursRequestContext();
app.UseMvp24HoursRequestObservability();
app.UseMvp24HoursProblemDetails();
app.UseMvp24HoursSecurityHeaders();
app.UseMvp24HoursRateLimiting();
app.UseMvp24HoursOutputCache();
```

`AddMvp24HoursOutputCaching` and `UseMvp24HoursOutputCaching` are compatibility APIs. New applications should use `AddMvp24HoursOutputCache` and `UseMvp24HoursOutputCache`.

## Production matrix

| Middleware / feature | Options class | Tested defaults | Production guidance |
|---|---|---|---|
| Legacy exception middleware | `ExceptionOptions` | tracing off; built-in status mapper | Prefer RFC 7807 Problem Details for new APIs. |
| Correlation ID | `CorrelationIdOptions` | `X-Correlation-ID`; response header on | Accept a gateway-provided ID only from trusted infrastructure. |
| Security headers | `SecurityHeadersOptions` | HSTS, CSP and frame protection on | Review CSP and preload before deployment. |
| ETag | `ETagOptions` | enabled; content hash; strong ETags | Keep strong hashes unless representation semantics require weak ETags. |
| Rate limiting | `RateLimitingOptions` | enabled; headers on; HTTP 429 | Define an explicit default policy and endpoint overrides. |
| Distributed rate limit state | `DistributedRateLimitOptions` | disabled; in-memory fallback on | Configure Redis and decide whether fallback is acceptable. |
| API versioning | `ApiVersioningOptions` | v1; URL/header/query readers | Prefer one public version strategy to avoid ambiguity. |
| Health routes | `HealthCheckOptions` | `/health`, `/health/ready`, `/health/live`; details off | Keep exception details off and protect detailed responses. |
| Problem Details | `ProblemDetailsOptions` / `MvpProblemDetailsOptions` | RFC 7807; HTTP 500 fallback; details off | Keep details and stack traces off outside development. |
| Idempotency | `IdempotencyOptions` | distributed cache; 24 h; POST/PUT/PATCH | Register `IDistributedCache`; require keys on selected write routes. |
| Cache-Control | `CacheControlOptions` | enabled; no default policy | Create explicit public/private policies. |
| Compression | `CompressionOptions` | Brotli/Gzip; HTTPS off; 1 KiB minimum | Enable HTTPS compression only after assessing BREACH-style risks. |
| Content negotiation | `ContentNegotiationOptions` | JSON default; XML available; 406 off | Enable 406 for strict public contracts. |
| Request timeout | `RequestTimeoutOptions` | enabled; 30 s | Set shorter endpoint-specific budgets where possible. |
| Request decompression | `RequestDecompressionOptions` | enabled; gzip/deflate/br; 10 MiB | Keep the expanded-body limit aligned with request-size limits. |
| Request size | `RequestSizeLimitOptions` | 30 MiB; POST/PUT/PATCH | Lower globally and grant explicit upload exceptions. |
| Response caching | `ResponseCachingOptions` | enabled; 100 KiB body; 100 MiB cache | Use only for HTTP-cache-safe responses. |
| Output cache | `OutputCachingOptions` | enabled; 5 min; 100 MiB | Add named policies; do not cache authenticated data by default. |
| Input sanitization | `InputSanitizationOptions` | validate mode; all detectors on | Treat this as defense in depth, not input validation or parameterization. |
| API-key authentication | `ApiKeyAuthenticationOptions` | header on; query off; auth required | Store keys in a secret provider; leave query-string keys disabled. |
| API-key rate limit | `ApiKeyRateLimitOptions` | disabled; 60/min | Enable only when per-key limits are needed. |
| CORS | `CorsOptions` | deny-by-default; OPTIONS allowed | Enumerate origins, methods and headers; avoid credentialed wildcards. |
| Request telemetry | `RequestTelemetryOptions` | traces/metrics on; exception details off | Exclude health/metrics routes and avoid sensitive header enrichment. |
| Request body tracing | `RequestBodyTracingOptions` | disabled; POST/PUT/PATCH; 16 KiB max | Enable only where needed and always keep redaction lists updated. |
| IP filtering | `IpFilteringOptions` | disabled; localhost allowed | Configure trusted proxies before forwarded headers. |
| Swashbuckle | `SwaggerOptions` | title `API`; OpenAPI 3.1; UI at `swagger` | Restrict UI exposure in production. |
| Native OpenAPI | `NativeOpenApiOptions` | document `v1`; UI on; ReDoc off | Prefer this .NET 10 path for new APIs when its feature set is sufficient. |
| Anti-forgery | `AntiForgeryOptions` | enabled for unsafe methods | Use for cookie-authenticated browser clients. |
| Request context | `RequestContextOptions` | response/outgoing propagation on; W3C flag off | Enable W3C mode when integrating with distributed tracing. |
| Request logging | `RequestLoggingOptions` | basic; bodies/headers off; 3 s slow threshold | Keep bodies off by default and extend sensitive-field lists. |

### Request body tracing

Use request body tracing to enrich the current Activity with a sanitized payload snapshot.

```csharp
builder.Services.AddMvp24HoursRequestObservability(
    configureBodyTracing: bodyTracing =>
    {
        bodyTracing.Enabled = true;
        bodyTracing.MaxBodySizeBytes = 8 * 1024;
        bodyTracing.ExcludedPaths.Add("/api/payments/webhook");
        bodyTracing.SensitiveProperties.Add("document");
    });

var app = builder.Build();
app.UseMvp24HoursRequestObservability(); // telemetry -> body tracing -> logging
```

`RequestBodyTracingOptions` captures only selected methods/content-types and stores redacted payload data in Activity tags (`http.request.body`, `http.request.body_truncated`, and `http.request.body_redacted_fields`).

## Core option tables

### Problem Details and request context

| Name | Type | Default | Description |
|---|---|---|---|
| `IncludeExceptionDetails` | `bool` | `false` | Includes exception messages in Problem Details. |
| `IncludeStackTrace` | `bool` | `false` | Includes stack traces. |
| `UseRfc7807ContentType` | `bool` | `true` | Uses `application/problem+json`. |
| `FallbackStatusCode` | `int` | `500` | Status for unmapped exceptions. |
| `LogExceptions` | `bool` | `true` | Logs mapped exceptions. |
| `IncludeCorrelationId` | `bool` | `true` | Adds correlation data to errors. |
| `CorrelationIdHeader` | `string` | `X-Correlation-ID` | Request-context correlation header. |
| `CausationIdHeader` | `string` | `X-Causation-ID` | Request-context causation header. |
| `IncludeInResponse` | `bool` | `true` | Returns request context in response headers. |
| `PropagateToOutgoingRequests` | `bool` | `true` | Makes context available to outgoing propagation. |
| `UseW3CTraceContext` | `bool` | `false` | Enables `traceparent`/`tracestate` handling. |

### Rate limiting and idempotency

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultPolicyName` | `string` | `default` | Policy selected when no endpoint mapping exists. |
| `IncludeRateLimitHeaders` | `bool` | `true` | Emits limit, remaining, reset and retry headers. |
| `UseForwardedHeaders` | `bool` | `true` | Reads forwarded client addresses. Trust proxies first. |
| `RateLimitedStatusCode` | `int` | `429` | Rejection status. |
| `UseProblemDetails` | `bool` | `true` | Formats rate-limit failures as Problem Details. |
| `DistributedRateLimitOptions.Enabled` | `bool` | `false` | Enables distributed state. |
| `FallbackToInMemory` | `bool` | `true` | Uses local counters when distributed state fails. |
| `IdempotencyOptions.StorageType` | `IdempotencyStorageType` | `DistributedCache` | Storage backend. |
| `KeySource` | `IdempotencyKeySource` | `HeaderOrRequestBody` | How the key is derived. |
| `CacheDuration` | `TimeSpan` | `24 h` | Replay retention. |
| `RequireIdempotencyKey` | `bool` | `false` | Rejects protected writes without a key. |
| `IntegrateWithCqrs` | `bool` | `true` | Enables CQRS-aware key behavior. |
| `EnableAtomicAcquisitionUsingDistributedLock` | `bool` | `true` | Uses distributed locking for atomic key acquisition when available. |
| `DistributedLockProviderName` | `string?` | `null` | Specific distributed lock provider name; default provider when null. |
| `DistributedLockAcquisitionTimeout` | `TimeSpan` | `1 s` | Timeout for atomic acquisition lock. |
| `DistributedLockDuration` | `TimeSpan` | `10 s` | Lease duration for atomic acquisition lock. |

### Performance and content

| Name | Type | Default | Description |
|---|---|---|---|
| `CompressionOptions.EnableForHttps` | `bool` | `false` | Compresses HTTPS responses. |
| `MinimumCompressionSize` | `int` | `1024` | Minimum response bytes. |
| `ContentNegotiationOptions.DefaultMediaType` | `string` | `application/json` | Default response media type. |
| `Return406WhenNoMatch` | `bool` | `false` | Rejects unsupported `Accept` values. |
| `JsonOptions` | `JsonSerializationOptions` | camel case; depth 32 | JSON formatter settings. |
| `XmlOptions` | `XmlSerializationOptions` | declaration on; no indent | XML formatter settings. |
| `RequestTimeoutOptions.DefaultTimeout` | `TimeSpan` | `30 s` | Default request deadline. |
| `RequestDecompressionOptions.MaxRequestBodySize` | `long` | `10 MiB` | Maximum decompressed body. |
| `RequestSizeLimitOptions.DefaultMaxBodySize` | `long?` | `30 MiB` | Default incoming body limit. |
| `ResponseCachingOptions.MaximumBodySize` | `long` | `100 KiB` | Largest response-cache entry. |
| `OutputCachingOptions.DefaultExpirationTimeSpan` | `TimeSpan` | `5 min` | Default output-cache lifetime. |
| `OutputCachingOptions.UseDistributedCache` | `bool` | `false` | Enables the configured distributed provider. |
| `OutputCachePolicyOptions.CacheAuthenticatedRequests` | `bool` | `false` | Allows authenticated response caching. |

## API versioning

Prefer one public versioning strategy. The library registers URL path, header, and query readers through `AddMvp24HoursApiVersioning`.

| Strategy | Example | When to prefer |
|---|---|---|
| URL path | `/api/v1/orders` | Public REST APIs where the version is part of the route contract |
| Header | `X-API-Version: 1.0` | Clean resource URLs with explicit client headers |
| Query string | `/api/orders?api-version=1.0` | Compatibility during migration |

### `ApiVersioningOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultApiVersion` | `ApiVersion` | `1.0` | Version used when none is supplied. |
| `AssumeDefaultVersionWhenUnspecified` | `bool` | `true` | Applies the default when the request omits a version. |
| `ReportApiVersions` | `bool` | `true` | Emits supported/deprecated version response headers. |
| `Strategy` | `ApiVersioningStrategy` | URL + header + query | Flags selecting which readers are registered. |
| `HeaderName` | `string` | `X-API-Version` | Header reader name. |
| `QueryStringParameterName` | `string` | `api-version` | Query reader name. |
| `UrlSegmentPattern` | `string` | `v{version}` | URL segment convention. |
| `SupportedApiVersions` | `List<ApiVersion>` | empty | Explicit supported versions. |
| `DeprecatedApiVersions` | `List<ApiVersion>` | empty | Versions reported as deprecated. |

```csharp
builder.Services.AddMvp24HoursApiVersioning(options =>
{
    options.Strategy = ApiVersioningStrategy.UrlPath;
    options.SupportedApiVersions.Add(new ApiVersion(1, 0));
    options.SupportedApiVersions.Add(new ApiVersion(2, 0));
    options.DeprecatedApiVersions.Add(new ApiVersion(1, 0));
});

builder.Services.AddMvp24HoursSwaggerWithVersioning(options =>
{
    options.Title = "Orders API";
    options.ShowDeprecationWarnings = true;
    options.Versions.Add(new SwaggerVersionInfo
    {
        Version = "v1",
        Title = "Orders API v1",
        IsDeprecated = true,
        DeprecationMessage = "Use v2. Removal planned for 2027-01-01."
    });
    options.Versions.Add(new SwaggerVersionInfo
    {
        Version = "v2",
        Title = "Orders API v2"
    });
});
```

Mark controllers with `[ApiVersion("1.0")]` and route templates such as `api/v{version:apiVersion}/[controller]`. For Native OpenAPI, configure deprecation on `NativeOpenApiOptions` version entries and see [Native OpenAPI](modernization/native-openapi.md).

## Authentication methods

| Method | Library ownership | Guidance |
|---|---|---|
| API key | Mvp24Hours (`AddMvp24HoursApiKeyAuthentication`) | Prefer for service-to-service or partner keys stored in a secret provider. |
| JWT Bearer | ASP.NET Core application pattern | Use `Microsoft.AspNetCore.Authentication.JwtBearer` for generic JWT APIs. For Keycloak, prefer the dedicated package. |
| Keycloak | `Mvp24Hours.Infrastructure.Identity.Keycloak` | JWT bearer, role mapping, UMA/RPT policies, Admin REST, and optional local-user sync. |
| Cookie / anti-forgery | Partial (`AntiForgeryOptions`) | Use for browser cookie sessions with unsafe methods. |

API-key authentication is documented in the production matrix above. For Keycloak-backed APIs, use [Keycloak identity integration](identity/keycloak.md):

```csharp
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Extensions;

builder.Services.AddKeycloakServices(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseKeycloakCurrentUser();
app.UseAuthorization();
```

For non-Keycloak JWT issuers, compose ASP.NET Core JWT bearer authentication directly and store signing keys with [Secrets & Security](infrastructure/secrets-security.md).

## Focused examples

```csharp
builder.Services.AddMvp24HoursContentNegotiation(options =>
{
    options.Return406WhenNoMatch = true;
    options.JsonOptions.WriteIndented = false;
    options.XmlOptions.UseDataContractSerializer = false;
});

builder.Services.AddMvp24HoursApiKeyAuthentication(options =>
{
    options.ApiKeys.Add(builder.Configuration["ApiKeys:Partner"]!);
    options.EnableQueryStringKey = false;
    options.RateLimit.Enabled = true;
    options.RateLimit.DefaultRequestsPerMinute = 120;
});

builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "Orders API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = !builder.Environment.IsProduction();
});
```

## Test reference

Defaults and registrations are exercised by:

- `src/Tests/Mvp24Hours.WebAPI.Test/Configuration/ConfigurationOptionsTest.cs`
- `src/Tests/Mvp24Hours.WebAPI.Test/Configuration/MoreConfigurationOptionsTest.cs`
- `src/Tests/Mvp24Hours.WebAPI.Test/Extensions/ExtensionsSmokeTest.cs`

## Related pages

- [Web API basics](webapi.md)
- [OpenAPI / Swagger](documentation.md)
- [Observability](observability/home.md)
- [Logging](observability/logging.md)
