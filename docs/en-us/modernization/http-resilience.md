# HTTP Resilience with Microsoft.Extensions.Http.Resilience

> **Current target**: .NET 10 | **Package**: `Microsoft.Extensions.Http.Resilience`

This page explains the modernization boundary for HTTP resilience. The canonical Mvp24Hours API reference, complete option tables, transport configuration, DI examples, testing guidance, and verified implementation caveats live in [HTTP Clients & Resilience](../infrastructure/http-resilience.md).

## Current direction

The .NET 10 source uses `Microsoft.Extensions.Http.Resilience` for new HTTP resilience code. It builds on Polly v8 and integrates resilience pipelines with `IHttpClientFactory`.

The standard handler composes:

1. total request timeout;
2. retry;
3. circuit breaker; and
4. per-attempt timeout.

Start with the standard handler, then use a custom resilience handler when the exact strategy set or ordering must differ:

```csharp
builder.Services.AddHttpClient("CatalogApi", client =>
{
    client.BaseAddress = new Uri("https://catalog.example.com");
}).AddStandardResilienceHandler();
```

Mvp24Hours provides equivalent named and typed registration helpers:

```csharp
using Mvp24Hours.Infrastructure.Http.Resilience;

builder.Services.AddHttpClientWithStandardResilience(
    "CatalogApi",
    client => client.BaseAddress = new Uri("https://catalog.example.com"),
    options =>
    {
        options.Retry.MaxRetryAttempts = 2;
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
    });
```

Use `AddMvpHttpClient(...).AddMvpResilience(...)` when the client also needs the Infrastructure module's certificate, proxy, propagation, logging, serializer, or telemetry configuration. See the [canonical HTTP guide](../infrastructure/http-resilience.md#recommended-di-registration).

## Legacy boundary

The tree still references `Microsoft.Extensions.Http.Polly` and retains `IAsyncPolicy<HttpResponseMessage>`-based retry, circuit-breaker, timeout, bulkhead, fallback, and policy-wrap implementations for compatibility. `AddHttpClientWithPolly` and `HttpResiliencePolicyBuilder` are marked obsolete.

Do not combine the legacy policy properties in `HttpClientOptions` with a native standard handler unless nested retry and timeout behavior is intentional. For migration details and a full map of legacy option behavior, see [Legacy Polly path](../infrastructure/http-resilience.md#legacy-polly-path).

## HTTP versus generic resilience

- HTTP clients use `Microsoft.Extensions.Http.Resilience` and the HTTP `NativeResilienceOptions` in `Mvp24Hours.Infrastructure.Http.Resilience`.
- Non-HTTP operations use `Microsoft.Extensions.Resilience` and the separate generic `NativeResilienceOptions` in `Mvp24Hours.Infrastructure.Resilience.Native`.

The same class name exists in two namespaces, so qualify it when both namespaces are imported. Continue with [Generic Resilience](generic-resilience.md) for non-HTTP pipelines.

## Observability and testing

The Microsoft/Polly path exposes resilience telemetry through its platform integration. Mvp24Hours can additionally emit structured HTTP logs and activities from `Mvp24Hours.Infrastructure.Http`.

Test registrations and failure behavior without live endpoints by using `TestHttpMessageHandler` or `HttpClientTestFixture`. The canonical guide includes the tested capabilities and an example:

- [Observability](../infrastructure/http-resilience.md#observability)
- [Testing](../infrastructure/http-resilience.md#testing)

## See also

- [Resilience Selection Guide](resilience-guide.md)
- [HTTP Clients & Resilience — canonical reference](../infrastructure/http-resilience.md)
- [Generic Resilience](generic-resilience.md)
- [Tracing](../observability/tracing.md)
- [Microsoft HTTP resilience documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Polly v8 documentation](https://www.pollydocs.org/)
