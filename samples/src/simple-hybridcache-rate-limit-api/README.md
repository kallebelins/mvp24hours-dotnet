# simple-hybridcache-rate-limit-api

Demonstrates `.NET`'s native **HybridCache** for read-heavy endpoints (stampede-safe L1 + optional Redis L2) combined with the **Mvp24Hours rate limiter** (sliding window per IP) to protect against abusive clients. A fake in-memory Product store is used so no database is required.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: project references by default; published packages are optional

## Features

- `AddMvpHybridCache()` — in-memory L1 with stampede protection (`.NET 9+` native)
- `AddMvpHybridCacheWithRedis(conn)` — optional Redis L2; activated by setting `ConnectionStrings:Redis`
- `HybridCacheProvider.GetOrCreateAsync` — cache-aside pattern with tag-based invalidation
- `AddMvp24HoursRateLimiting` / `UseMvp24HoursRateLimiting` — sliding-window rate limiter (429 on excess)
- Native OpenAPI, ProblemDetails (RFC 7807), and health checks — no Swashbuckle
- Endpoints to explicitly evict cache entries by tag

## Architecture

- Tier: `Simple`
- Shape: Single-project Minimal API
- Why this shape fits: caching and rate limiting are cross-cutting — no domain layer is needed for a focused teaching sample

## Layers

- `ProductAPI.WebAPI` — single host project; `Program.cs` wires HybridCache, rate limiter, and Product endpoints

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Optional** — Redis for L2 distributed cache (default configuration uses in-memory only)

## Configuration

No secrets are required for the default (in-memory) configuration. When using Redis L2, configure the connection string via environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `ConnectionStrings:Redis` | No | Redis connection string for L2 cache | `localhost:6379` |
| `RateLimit:PermitLimit` | No | Max requests in the window (default: 20) | `20` |
| `RateLimit:WindowSeconds` | No | Rate limit window in seconds (default: 60) | `60` |

Set `ConnectionStrings:Redis` to an empty string or omit it entirely to stay on in-memory mode.

## Run

From this sample's solution directory:

```bash
dotnet restore
dotnet run --project ProductAPI.WebAPI/ProductAPI.WebAPI.csproj
```

### With Redis L2 (optional)

From `samples/src/simple-hybridcache-rate-limit-api`:

```bash
docker compose up -d
```

Redis listens on localhost port **6379**. Then set the connection string:

```bash
# PowerShell
$env:ConnectionStrings__Redis = "localhost:6379"
dotnet run --project ProductAPI.WebAPI/ProductAPI.WebAPI.csproj

# bash
ConnectionStrings__Redis=localhost:6379 dotnet run --project ProductAPI.WebAPI/ProductAPI.WebAPI.csproj
```

## Explore the API

- OpenAPI document: `https://localhost:5001/openapi/v1.json`
- Health: `https://localhost:5001/health`

| Endpoint | Description |
| --- | --- |
| `GET /api/products` | List all products (cached under `products:all`) |
| `GET /api/products/{id}` | Get a product by ID (cached under `products:{id}`) |
| `DELETE /api/products/{id}/cache` | Evict a single product from the cache |
| `DELETE /api/products/cache` | Evict the full list from the cache |

### Observe rate limiting

Send more than 20 requests in 60 seconds to see `HTTP 429 Too Many Requests`:

```bash
for i in $(seq 1 25); do curl -s -o /dev/null -w "%{http_code}\n" https://localhost:5001/api/products; done
```

## Key code patterns

### HybridCache — GetOrCreateAsync with tags

```csharp
var product = await cache.GetOrCreateAsync<ProductDto?>(
    $"products:{id}",
    async ct =>
    {
        // factory runs only on cache miss
        return await LoadFromDatabaseAsync(id, ct);
    },
    tags: [$"product:{id}", "products"],
    cancellationToken: ct);
```

### Tag-based invalidation

```csharp
// Invalidate a single product
await cache.InvalidateByTagAsync($"product:{id}", ct);

// Invalidate everything tagged "products" (list + individual entries)
await cache.InvalidateByTagAsync("products", ct);
```

### Rate limiting

```csharp
// Registration — sliding-window 20 req/min per IP
builder.Services.AddMvp24HoursRateLimiting(options =>
    options.AddDefaultPolicy(permitLimit: 20, window: TimeSpan.FromMinutes(1)));

// Middleware — returns 429 when the window is exceeded
app.UseMvp24HoursRateLimiting();
```

### Optional Redis L2

```csharp
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddMvpHybridCacheWithRedis(redisConnection, opts =>
    {
        opts.DefaultExpiration = TimeSpan.FromMinutes(5);
        opts.RedisInstanceName = "products:";
    });
}
else
{
    builder.Services.AddMvpHybridCache(opts =>
    {
        opts.DefaultExpiration = TimeSpan.FromMinutes(5);
    });
}
```

## Related documentation

- [Getting started](../../../docs/en-us/getting-started.md)
- [HybridCache modernization](../../../docs/en-us/modernization/hybrid-cache.md)
- [Caching advanced](../../../docs/en-us/caching-advanced.md)
- [Rate limiting](../../../docs/en-us/modernization/rate-limiting.md)
- [Microsoft HybridCache docs](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid)
- [System.Threading.RateLimiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)

## What this sample intentionally does not cover

- Persistent database (in-memory store only)
- Redis distributed rate limiting (`AddMvp24HoursDistributedRateLimiting`) — in-memory limiter is used per instance
- Output caching (`AddMvp24HoursOutputCache`) — a separate, HTTP-layer concern
- Authentication-scoped rate limiting (per-user or per-API-key policies)
- Multi-instance deployments where per-instance in-memory state diverges
