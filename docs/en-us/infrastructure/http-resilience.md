# HTTP Clients & Resilience

This is the canonical reference for HTTP client configuration in `Mvp24Hours.Infrastructure`. It covers transport and handler options, the current .NET 10 resilience path, the retained Polly policy path, dependency injection, testing, and observability.

> The current source tree targets `net10.0`. For new HTTP integrations, use `Microsoft.Extensions.Http.Resilience` (Polly v8 underneath). The older policy types and `AddHttpClientWithPolly` remain available for compatibility but are marked obsolete.

## Install

```bash
dotnet add package Mvp24Hours.Infrastructure
```

The Infrastructure project currently references both `Microsoft.Extensions.Http.Resilience` and the compatibility package `Microsoft.Extensions.Http.Polly`.

## Choose the registration path

| Need | Registration | Status |
|------|--------------|--------|
| Mvp24Hours transport options plus native resilience | `AddMvpHttpClient(...).AddMvpResilience(...)` | Recommended |
| Standard named client | `AddHttpClientWithStandardResilience(...)` or `AddHttpClient(...).AddStandardResilienceHandler()` | Recommended |
| Standard typed client | `AddTypedHttpClientWithStandardResilience<TClient>(...)` | Recommended |
| Custom Polly v8 HTTP pipeline | `AddHttpClientWithCustomResilience(...)` / `AddTypedHttpClientWithCustomResilience<TClient>(...)` | Recommended for advanced cases |
| Generic, non-HTTP operation | `AddNativeResilience(...)` | Use the separate [Generic Resilience](../modernization/generic-resilience.md) guide |
| Mvp24Hours Polly policy objects | `AddHttpClientWithPolly(...)`, `IHttpResiliencePolicy`, or policy options inside `HttpClientOptions` | Legacy/obsolete path |
| Static `HttpGetAsync`, `HttpPostAsync`, and related helpers | `Mvp24Hours.Extensions.HttpClientExtensions` | Obsolete; migrate to `ITypedHttpClient<TApi>` |

The two `NativeResilienceOptions` classes are different:

- `Mvp24Hours.Infrastructure.Http.Resilience.NativeResilienceOptions` configures an HTTP standard resilience handler and is documented below.
- `Mvp24Hours.Infrastructure.Resilience.Native.NativeResilienceOptions` configures a generic non-HTTP `ResiliencePipeline`; see [Generic Resilience](../modernization/generic-resilience.md).

## Recommended DI registration

Use `AddMvpHttpClient` when certificates, proxy settings, propagation, Mvp24Hours logging, or other `HttpClientOptions` behavior is required. Add native resilience to the returned `IHttpClientBuilder`:

```csharp
using Mvp24Hours.Infrastructure.Http.Extensions;
using Mvp24Hours.Infrastructure.Http.Resilience;

builder.Services
    .AddMvpHttpClient("CatalogApi", options =>
    {
        options.BaseAddress = new Uri("https://catalog.example.com");
        options.Timeout = TimeSpan.FromSeconds(45);
        options.DefaultHeaders["X-Client"] = "orders";
        options.LoggingOptions = new()
        {
            LogRequestHeaders = true,
            SensitiveHeaders = ["Authorization", "Cookie", "Set-Cookie", "X-Api-Key"]
        };
    })
    .AddMvpResilience(options =>
    {
        options.ConfigureOptions(resilience =>
        {
            resilience.TotalRequestTimeout = TimeSpan.FromSeconds(40);
            resilience.AttemptTimeout = TimeSpan.FromSeconds(10);
            resilience.MaxRetryAttempts = 3;
        });
    });
```

Do not also populate `RetryPolicy`, `CircuitBreakerPolicy`, or `TimeoutPolicy` in the same `HttpClientOptions` instance when using the native handler. Those properties install the legacy Polly handler chain, so combining both paths can produce nested retries and timeouts.

For the Microsoft standard defaults without Mvp24Hours transport options:

```csharp
using Mvp24Hours.Infrastructure.Http.Resilience;

builder.Services.AddHttpClientWithStandardResilience(
    "CatalogApi",
    client => client.BaseAddress = new Uri("https://catalog.example.com"));
```

For a marker-based Mvp24Hours typed client:

```csharp
using Mvp24Hours.Infrastructure.Http.Contract;
using Mvp24Hours.Infrastructure.Http.Extensions;

builder.Services
    .AddMvpTypedHttpClient<ICatalogApi>(options =>
    {
        options.BaseAddress = new Uri("https://catalog.example.com");
        options.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler();

public sealed class CatalogService(ITypedHttpClient<ICatalogApi> client)
{
    public Task<Product?> GetAsync(int id, CancellationToken cancellationToken) =>
        client.GetAsync<Product>($"/products/{id}", cancellationToken);
}
```

`AddMvpTypedHttpClient<TApi>` registers `ITypedHttpClient<TApi>` backed by `TypedHttpClient<TApi>` and registers the JSON serializer when one is not already present.

## `HttpClientOptions`

Namespace: `Mvp24Hours.Infrastructure.Http.Options`

| Property | Type | Default | Applied behavior |
|----------|------|---------|------------------|
| `Name` | `string` | `string.Empty` | Named-client name; marker type name is supplied by typed registration unless changed |
| `BaseAddress` | `Uri?` | `null` | Sets `HttpClient.BaseAddress` when non-null |
| `Timeout` | `TimeSpan` | 30 seconds | Sets `HttpClient.Timeout`; distinct from resilience timeout strategies |
| `DefaultHeaders` | `Dictionary<string,string>` | Empty | Added with `TryAddWithoutValidation` |
| `MaxResponseContentBufferSize` | `long` | `2147483647` (2 GiB - 1) | Sets `HttpClient.MaxResponseContentBufferSize` |
| `EnableDecompression` | `bool` | `true` | Enables GZip, Deflate, and Brotli automatic decompression |
| `Certificate` | `CertificateOptions?` | `null` | Optional client certificate |
| `RetryPolicy` | `RetryPolicyOptions?` | `null` | Installs the legacy Polly retry handler when non-null and enabled |
| `CircuitBreakerPolicy` | `CircuitBreakerPolicyOptions?` | `null` | Installs the legacy Polly circuit-breaker handler when non-null and enabled |
| `TimeoutPolicy` | `TimeoutPolicyOptions?` | `null` | Installs the legacy Polly timeout handler when non-null and enabled |
| `HandlerLifetime` | `TimeSpan` | 2 minutes | Passed to `SetHandlerLifetime` |
| `PropagateCorrelationId` | `bool` | `true` | Adds `PropagationCorrelationIdDelegatingHandler` |
| `PropagateAuthorization` | `bool` | `false` | Adds `PropagationAuthorizationDelegatingHandler` |
| `PropagateHeaders` | `List<string>` | Empty | Adds a handler for the listed incoming headers |
| `EnableLogging` | `bool` | `true` | Adds `LoggingDelegatingHandler` |
| `LoggingOptions` | `HttpLoggingOptions?` | `null` | Uses a default `HttpLoggingOptions` instance when null |
| `EnableTelemetry` | `bool` | `true` | Adds `TelemetryDelegatingHandler` |
| `UserAgent` | `string?` | `null` | Parses and adds the `User-Agent` header when nonblank |
| `AcceptHeader` | `string` | `"application/json"` | Parses and adds the default `Accept` header when nonblank |
| `FollowRedirects` | `bool` | `true` | Sets `HttpClientHandler.AllowAutoRedirect` |
| `MaxRedirects` | `int` | `50` | Sets `HttpClientHandler.MaxAutomaticRedirections` |
| `UseCookies` | `bool` | `true` | Sets `HttpClientHandler.UseCookies` |
| `HttpVersion` | `Version?` | `null` | Present on the option type, but the current registration code does not apply it to `HttpClient.DefaultRequestVersion` |
| `ValidateServerCertificate` | `bool` | `true` | When false, installs `DangerousAcceptAnyServerCertificateValidator` |
| `Proxy` | `ProxyOptions?` | `null` | Configures `WebProxy` when enabled and its address is nonblank |
| `Authentication` | `AuthenticationOptions?` | `null` | Adds the authentication handler when the scheme is not `None` |
| `Compression` | `CompressionHandlerOptions?` | `null` | Adds request compression when enabled |
| `TelemetryOptions` | `TelemetryHandlerOptions?` | `null` | Uses default telemetry handler options when null |

The primary handler is an `HttpClientHandler` restricted by this registration to TLS 1.2 and TLS 1.3. `ValidateServerCertificate = false` disables server identity validation and should be limited to controlled development tests.

### Certificate and proxy example

```csharp
using System.Security.Cryptography.X509Certificates;
using Mvp24Hours.Infrastructure.Http.Extensions;

builder.Services.AddMvpHttpClient("PartnerApi", options =>
{
    options.BaseAddress = new Uri("https://partner.example.com");
    options.Certificate = new()
    {
        Thumbprint = configuration["PartnerApi:CertificateThumbprint"],
        StoreLocation = StoreLocation.CurrentUser,
        StoreName = StoreName.My
    };
    options.Proxy = new()
    {
        Enabled = true,
        Address = configuration["PartnerApi:ProxyAddress"],
        UseDefaultCredentials = true,
        BypassOnLocal = true
    };
});
```

Certificate loading checks file path first, then base64 content, thumbprint, and subject name. Keep certificate and proxy passwords in a secret provider rather than source or ordinary checked-in configuration.

## Full HTTP option reference

### `CertificateOptions`

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `FilePath` | `string?` | `null` | Certificate file path |
| `Password` | `string?` | `null` | Password used for file or base64 certificate loading |
| `Thumbprint` | `string?` | `null` | Certificate-store thumbprint lookup |
| `StoreLocation` | `StoreLocation` | `CurrentUser` | Store location |
| `StoreName` | `StoreName` | `My` | Store name |
| `SubjectName` | `string?` | `null` | Certificate-store subject lookup |
| `Base64Certificate` | `string?` | `null` | Base64-encoded certificate |
| `KeyStorageFlags` | `X509KeyStorageFlags` | `DefaultKeySet` | Key storage flags used while loading file/base64 content |

### `ProxyOptions`

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `Enabled` | `bool` | `false` | Enables proxy configuration |
| `Address` | `string?` | `null` | Proxy address; required in practice for the handler to enable the proxy |
| `BypassOnLocal` | `bool` | `true` | Bypasses proxy for local addresses |
| `BypassList` | `List<string>` | Empty | Proxy bypass patterns |
| `UseDefaultCredentials` | `bool` | `false` | Uses process/default credentials |
| `Username` | `string?` | `null` | Explicit proxy username |
| `Password` | `string?` | `null` | Explicit proxy password |

Explicit `NetworkCredential` is created only when both username and password are nonblank.

### `HttpLoggingOptions`

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `LogRequestHeaders` | `bool` | `false` | Includes request and request-content headers |
| `LogRequestBody` | `bool` | `false` | Includes request body |
| `LogResponseHeaders` | `bool` | `false` | Includes response and response-content headers |
| `LogResponseBody` | `bool` | `false` | Includes response body |
| `MaxBodyLogSize` | `int` | `4096` | Maximum logged characters before truncation |
| `SensitiveHeaders` | `List<string>` | `Authorization`, `Cookie`, `Set-Cookie`, `X-Api-Key` | Header names masked case-insensitively |
| `MaskValue` | `string` | `"***"` | Replacement text for a sensitive value |

Request details are logged at Debug. Responses use Information for success, Warning for 4xx, and Error for 5xx. Body logging buffers and reads content; leave it disabled unless required and safe. A connection-refused `SocketException` is converted by this handler into an HTTP `502 Bad Gateway` response, while other failures are rethrown.

### `RetryPolicyOptions` (legacy HTTP policy)

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `Enabled` | `bool` | `true` | Enables the legacy policy when the options object is supplied |
| `MaxRetries` | `int` | `3` | Number of retry attempts after the original request |
| `InitialDelay` | `TimeSpan` | 1 second | Base delay |
| `MaxDelay` | `TimeSpan` | 30 seconds | Delay cap |
| `BackoffType` | `BackoffType` | `Exponential` | `Constant`, `Linear`, `Exponential`, or `DecorrelatedJitter` |
| `JitterFactor` | `double` | `0.1` | Random delay factor |
| `RetryStatusCodes` | `List<int>` | `408, 429, 500, 502, 503, 504` | Additional result status codes handled |
| `RetryOnTimeout` | `bool` | `true` | Exposed option; current implementations always handle Polly `TimeoutRejectedException` and do not read this flag |

The standalone legacy `RetryPolicy` honors `Retry-After` delta values up to `MaxDelay`. Both the standalone policy and the `HttpClientOptions` registration handle transient HTTP errors and add jitter according to these settings.

### `CircuitBreakerPolicyOptions` (legacy HTTP policy)

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `Enabled` | `bool` | `true` | Enables the legacy policy when the options object is supplied |
| `FailureThreshold` | `int` | `5` | Compatibility property; the current advanced circuit breaker does not read it |
| `SamplingDuration` | `TimeSpan` | 30 seconds | Window used to calculate the failure ratio |
| `MinimumThroughput` | `int` | `10` | Minimum handled outcomes in the sampling window |
| `BreakDuration` | `TimeSpan` | 30 seconds | Open-circuit duration |
| `FailureRatio` | `double` | `0.5` | Failure ratio that opens the circuit |
| `OnBreak` | `Action<CircuitBreakerStateChangeInfo>?` | `null` | Open callback used by the standalone legacy `CircuitBreakerPolicy` |
| `OnReset` | `Action<CircuitBreakerStateChangeInfo>?` | `null` | Reset callback used by the standalone legacy `CircuitBreakerPolicy` |
| `OnHalfOpen` | `Action<CircuitBreakerStateChangeInfo>?` | `null` | Half-open callback used by the standalone legacy `CircuitBreakerPolicy` |

The inline policy created from `HttpClientOptions` uses `FailureRatio`, `SamplingDuration`, `MinimumThroughput`, and `BreakDuration`; its callback bodies are currently empty. Use native `NativeResilienceBuilder.OnCircuitBreak` / `OnCircuitReset`, or the standalone legacy policy, when callbacks are required.

### `CircuitBreakerStateChangeInfo`

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `ServiceName` | `string` | `string.Empty` | Logical service name |
| `NewState` | `Polly.CircuitBreaker.CircuitState` | Enum zero value (`Closed`) | New Polly circuit state |
| `BreakDuration` | `TimeSpan?` | `null` | Open duration, when applicable |
| `Reason` | `string?` | `null` | State-change reason |
| `Timestamp` | `DateTime` | `default(DateTime)` | State-change timestamp; populated by the standalone policy |

### `TimeoutPolicyOptions` (legacy HTTP policy)

| Property | Type | Default | Meaning |
|----------|------|---------|---------|
| `Enabled` | `bool` | `true` | Enables the policy when the options object is supplied |
| `Timeout` | `TimeSpan` | 30 seconds | Polly timeout duration |

This timeout is separate from `HttpClientOptions.Timeout`. Avoid conflicting timeout layers; for the native path, ensure total timeout, per-attempt timeout, and `HttpClient.Timeout` express the intended overall budget.

## Native HTTP resilience (.NET 10 path)

`Microsoft.Extensions.Http.Resilience` builds Polly v8 pipelines. `AddStandardResilienceHandler` supplies total timeout, retry, circuit breaker, and per-attempt timeout strategies. Mvp24Hours exposes direct named/typed registration methods and `AddMvpResilience` for a smaller option model plus callbacks.

```csharp
using Mvp24Hours.Infrastructure.Http.Resilience;

builder.Services
    .AddHttpClient("InventoryApi", client =>
        client.BaseAddress = new Uri("https://inventory.example.com"))
    .AddMvpResilience(resilience => resilience
        .WithOptions(NativeResilienceOptions.LowLatency)
        .OnRetry((arguments, delay) =>
            logger.LogWarning(
                "Inventory retry {Attempt} after {Delay}",
                arguments.AttemptNumber,
                delay))
        .OnCircuitBreak(arguments =>
            logger.LogError(
                "Inventory circuit opened for {BreakDuration}",
                arguments.BreakDuration)));
```

For complete Microsoft option control, configure `HttpStandardResilienceOptions`:

```csharp
builder.Services.AddHttpClientWithStandardResilience(
    "InventoryApi",
    client => client.BaseAddress = new Uri("https://inventory.example.com"),
    options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.Retry.Delay = TimeSpan.FromMilliseconds(250);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
    });
```

### HTTP `NativeResilienceOptions`

Namespace: `Mvp24Hours.Infrastructure.Http.Resilience`

| Property | Type | Default | Mapping |
|----------|------|---------|---------|
| `TotalRequestTimeout` | `TimeSpan` | 30 seconds | `HttpStandardResilienceOptions.TotalRequestTimeout.Timeout` |
| `AttemptTimeout` | `TimeSpan` | 10 seconds | `HttpStandardResilienceOptions.AttemptTimeout.Timeout` |
| `MaxRetryAttempts` | `int` | `3` | `HttpStandardResilienceOptions.Retry.MaxRetryAttempts` |
| `RetryDelay` | `TimeSpan` | 2 seconds | `HttpStandardResilienceOptions.Retry.Delay` |
| `UseJitter` | `bool` | `true` | `HttpStandardResilienceOptions.Retry.UseJitter` |
| `CircuitBreakerFailureRatio` | `double` | `0.1` | `HttpStandardResilienceOptions.CircuitBreaker.FailureRatio` |
| `CircuitBreakerSamplingDuration` | `TimeSpan` | 30 seconds | Circuit-breaker sampling duration |
| `CircuitBreakerMinimumThroughput` | `int` | `10` | Circuit-breaker minimum throughput |
| `CircuitBreakerBreakDuration` | `TimeSpan` | 30 seconds | Circuit-breaker open duration |
| `EnableRetry` | `bool` | `true` | Controls whether the wrapper assigns retry settings |
| `EnableCircuitBreaker` | `bool` | `true` | Controls whether the wrapper assigns circuit-breaker settings |
| `EnableAttemptTimeout` | `bool` | `true` | Controls whether the wrapper assigns attempt-timeout settings |
| `EnableTotalTimeout` | `bool` | `true` | Controls whether the wrapper assigns total-timeout settings |

The wrapper sets retry backoff to `DelayBackoffType.Exponential`.

Important current behavior: `NativeResilienceOptions.Disabled` skips adding the standard handler because all four flags are false. When only some flags are false, `NativeResilienceBuilder` still calls `AddStandardResilienceHandler`; a false flag prevents the wrapper from overriding that strategy but does not remove the strategy's Microsoft default. Use `AddHttpClientWithCustomResilience` / `AddResilienceHandler` when the exact strategy set must omit individual strategies.

### Presets

| Preset | Total / attempt timeout | Retries / delay | Circuit breaker |
|--------|-------------------------|-----------------|-----------------|
| `HighAvailability` | 2 minutes / 15 seconds | 5 / 1 second, jitter | Ratio 0.25; 60-second sample; throughput 20; 15-second break |
| `LowLatency` | 10 seconds / 3 seconds | 2 / 500 ms, jitter | Ratio 0.1; 15-second sample; throughput 5; 10-second break |
| `BatchProcessing` | 5 minutes / 1 minute | 10 / 5 seconds, jitter | Ratio 0.5; 2-minute sample; throughput 50; 1-minute break |
| `Disabled` | Strategies disabled | Strategies disabled | Strategies disabled |

Each preset returns a new options instance.

### Custom native pipeline

Use the custom extension when the standard handler's fixed composition is not appropriate:

```csharp
using Microsoft.Extensions.Http.Resilience;
using Polly;

builder.Services.AddHttpClientWithCustomResilience(
    "SearchApi",
    "search-api",
    client => client.BaseAddress = new Uri("https://search.example.com"),
    pipeline => pipeline
        .AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddTimeout(TimeSpan.FromSeconds(8)));
```

## Legacy Polly path

The net10 tree still carries Polly-based `RetryPolicy`, `CircuitBreakerPolicy`, `TimeoutPolicy`, `BulkheadPolicy`, `FallbackPolicy`, `PolicyWrap`, and `IHttpResiliencePolicy`. `HttpClientResilienceExtensions` and `HttpResiliencePolicyBuilder` are explicitly obsolete.

```csharp
#pragma warning disable CS0618 // Migration-only example
builder.Services.AddHttpClientWithPolly("LegacyApi", policies =>
{
    policies
        .AddRetryPolicy(options => options.MaxRetries = 3)
        .AddCircuitBreakerPolicy(options =>
        {
            options.FailureRatio = 0.5;
            options.BreakDuration = TimeSpan.FromSeconds(30);
        })
        .AddTimeoutPolicy(options => options.Timeout = TimeSpan.FromSeconds(10));
});
#pragma warning restore CS0618
```

Keep this path only while migrating existing consumers. The native replacement is based on `Microsoft.Extensions.Http.Resilience`; generic non-HTTP work uses `Microsoft.Extensions.Resilience`. Both are built on Polly v8, but their pipeline APIs are not the old `IAsyncPolicy<HttpResponseMessage>` integration.

The older `Mvp24Hours.Patterns.Test` HTTP tests exercise obsolete static request helpers against WireMock. They demonstrate compatibility, not the recommended registration model.

## Testing

The Infrastructure test helpers avoid network calls:

- `TestHttpMessageHandler` records method, URI, headers, body, and timestamp.
- It can return default or conditional responses and simulate network failures or timeouts.
- `HttpClientTestFixture` wraps the handler with setup and verification methods.
- The HTTP resilience tests verify named and typed native registration, custom pipelines, presets, callback configuration, disabled registration, and the retained legacy policy behavior.

```csharp
using System.Net;
using Mvp24Hours.Infrastructure.Testing.Http;

var handler = new TestHttpMessageHandler()
    .WhenGet("/products/42", HttpStatusCode.OK, new { id = 42 });

using var client = new HttpClient(handler)
{
    BaseAddress = new Uri("https://example.test")
};

using HttpResponseMessage response = await client.GetAsync("/products/42");

Assert.Equal(HttpStatusCode.OK, response.StatusCode);
Assert.True(handler.VerifyRequestUrl("/products/42"));
Assert.Equal(1, handler.RequestCount);
```

For resilience tests, keep delays and timeouts short, assert the received request count, and test final outcomes for transient status codes, network failure, timeout, and open-circuit behavior. `NativeResilienceOptions.Disabled` is useful when a test needs the production registration shape without resilience timing.

## Observability

`HttpClientOptions.EnableLogging` adds structured request/response logs with duration and masking. Review `SensitiveHeaders` before enabling header logs and avoid body logs for credentials, tokens, or personal data.

`HttpClientOptions.EnableTelemetry` adds `TelemetryDelegatingHandler`, which emits client activities from:

```text
Mvp24Hours.Infrastructure.Http
```

The activity records HTTP method, URL data, status code, content sizes when available, protocol version, duration, and exception events. `TelemetryHandlerOptions.RecordFullUrl` defaults to false so query strings are not recorded. Register this source with the application's OpenTelemetry tracing configuration; see [Tracing](../observability/tracing.md).

Native resilience also exposes Polly strategy telemetry through the Microsoft/Polly integration. Prefer the platform's documented telemetry hooks and the `NativeResilienceBuilder` retry/circuit callbacks instead of relying on the empty callback bodies in the inline legacy `HttpClientOptions` policy registration.

## Modernization links

- [HTTP Resilience modernization overview](../modernization/http-resilience.md) explains the platform direction and migration boundary.
- [Generic Resilience](../modernization/generic-resilience.md) covers non-HTTP `ResiliencePipeline` registration.
- [Microsoft HTTP resilience documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Polly v8 documentation](https://www.pollydocs.org/)

