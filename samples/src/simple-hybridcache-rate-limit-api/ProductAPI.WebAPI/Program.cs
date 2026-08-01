using Microsoft.AspNetCore.Http.HttpResults;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using Mvp24Hours.WebAPI.Extensions;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// ─── HybridCache ─────────────────────────────────────────────────────────────
// Default: in-memory L1 only.
// Set ConnectionStrings:Redis in appsettings / environment to enable Redis L2.
var redisConnection = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddMvpHybridCacheWithRedis(redisConnection, opts =>
    {
        opts.DefaultExpiration = TimeSpan.FromMinutes(5);
        opts.RedisInstanceName = "products:";
        opts.EnableDetailedLogging = builder.Environment.IsDevelopment();
    });
}
else
{
    builder.Services.AddMvpHybridCache(opts =>
    {
        opts.DefaultExpiration = TimeSpan.FromMinutes(5);
        opts.EnableDetailedLogging = builder.Environment.IsDevelopment();
    });
}

builder.Services.AddSingleton<HybridCacheProvider>(sp =>
    (HybridCacheProvider)sp.GetRequiredService<ICacheProvider>());

// ─── Rate limiting ────────────────────────────────────────────────────────────
// Mvp24Hours sliding-window rate limiter — 20 requests/min per IP by default.
builder.Services.AddMvp24HoursRateLimiting(options =>
    options.AddDefaultPolicy(
        permitLimit: builder.Configuration.GetValue("RateLimit:PermitLimit", 20),
        window: TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimit:WindowSeconds", 60))));

// ─── Native OpenAPI ───────────────────────────────────────────────────────────
builder.Services.AddMvp24HoursNativeOpenApi(opts =>
{
    opts.Title = "Simple HybridCache + Rate Limiting API";
    opts.Description = "Demonstrates HybridCache for read-heavy endpoints and Mvp24Hours rate limiting.";
    opts.Version = "v1";
});

// ─── ProblemDetails (RFC 7807) ────────────────────────────────────────────────
builder.Services.AddMvp24HoursProblemDetails(opts =>
    opts.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// ─── Health checks ────────────────────────────────────────────────────────────
builder.Services.AddMvp24HoursHealthChecks();

// ─── In-memory product store ──────────────────────────────────────────────────
builder.Services.AddSingleton<ProductStore>();

var app = builder.Build();

// ─── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMvp24HoursProblemDetails();
app.UseHttpsRedirection();

// Rate limiting — must come before endpoint routing so the 429 is returned early.
app.UseMvp24HoursRateLimiting();

app.MapMvp24HoursNativeOpenApi();
app.UseMvp24HoursHealthChecks();

// ─── Product endpoints ─────────────────────────────────────────────────────────
var products = app.MapGroup("/api/products")
    .WithTags("Products");

products.MapGet("/", ListProducts)
    .WithName("ListProducts")
    .WithSummary("List all products (cached)");

products.MapGet("/{id:int}", GetProduct)
    .WithName("GetProduct")
    .WithSummary("Get a product by ID (cached)");

// Cache management endpoints
products.MapDelete("/{id:int}/cache", InvalidateProductCache)
    .WithName("InvalidateProductCache")
    .WithSummary("Evict a specific product from the cache");

products.MapDelete("/cache", InvalidateAllProductsCache)
    .WithName("InvalidateAllProductsCache")
    .WithSummary("Evict the full product list from the cache");

app.Run();

// ─── Endpoint handlers ─────────────────────────────────────────────────────────

static async Task<Ok<IEnumerable<ProductDto>>> ListProducts(
    HybridCacheProvider cache,
    ProductStore store,
    ILogger<Program> logger,
    CancellationToken ct)
{
    const string CacheKey = "products:all";

    var list = await cache.GetOrCreateAsync<IEnumerable<ProductDto>>(
        CacheKey,
        async _ =>
        {
            logger.LogInformation("Cache MISS — loading all products from store");
            await Task.Delay(10, _);   // simulate lightweight I/O
            return store.GetAll();
        },
        tags: ["products"],
        cancellationToken: ct);

    return TypedResults.Ok(list);
}

static async Task<Results<Ok<ProductDto>, NotFound>> GetProduct(
    int id,
    HybridCacheProvider cache,
    ProductStore store,
    ILogger<Program> logger,
    CancellationToken ct)
{
    var cacheKey = $"products:{id}";

    var product = await cache.GetOrCreateAsync<ProductDto?>(
        cacheKey,
        async _ =>
        {
            logger.LogInformation("Cache MISS — loading product {ProductId} from store", id);
            await Task.Delay(5, _);   // simulate lightweight I/O
            return store.GetById(id);
        },
        tags: [$"product:{id}", "products"],
        cancellationToken: ct);

    return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
}

static async Task<NoContent> InvalidateProductCache(
    int id,
    HybridCacheProvider cache,
    ILogger<Program> logger,
    CancellationToken ct)
{
    logger.LogInformation("Evicting cache for product {ProductId}", id);
    await cache.InvalidateByTagAsync($"product:{id}", ct);
    return TypedResults.NoContent();
}

static async Task<NoContent> InvalidateAllProductsCache(
    HybridCacheProvider cache,
    ILogger<Program> logger,
    CancellationToken ct)
{
    logger.LogInformation("Evicting full product list cache");
    await cache.InvalidateByTagAsync("products", ct);
    return TypedResults.NoContent();
}

// ─── Domain types ──────────────────────────────────────────────────────────────

public record ProductDto(int Id, string Name, string Category, decimal Price, int Stock);

internal sealed class ProductStore
{
    private readonly ConcurrentDictionary<int, ProductDto> _data = new();

    public ProductStore()
    {
        // Seed data so the cache has entries to serve immediately.
        Add(new ProductDto(1, "Widget Pro", "Hardware", 29.99m, 150));
        Add(new ProductDto(2, "Gadget Plus", "Electronics", 99.99m, 42));
        Add(new ProductDto(3, "Thingamajig", "Tools", 14.50m, 300));
        Add(new ProductDto(4, "Doohickey", "Hardware", 7.25m, 500));
        Add(new ProductDto(5, "Gizmo X", "Electronics", 199.00m, 18));
    }

    public IEnumerable<ProductDto> GetAll() => _data.Values.OrderBy(p => p.Id);

    public ProductDto? GetById(int id) =>
        _data.TryGetValue(id, out var p) ? p : null;

    private void Add(ProductDto p) => _data[p.Id] = p;
}

public partial class Program { }
