# Multi-tenancy

## Overview

Mvp24Hours CQRS provides comprehensive multi-tenancy support, enabling a single application to serve multiple clients (tenants) in an isolated and secure manner. The system provides:

1. **Tenant Context** - Information about the current tenant
2. **Tenant Resolution** - Automatic tenant identification
3. **Pipeline Injection** - Automatic context propagation
4. **Query Filters** - Automatic data isolation

## Architecture

```
┌────────────────────────────────────────────────────────────────────────────┐
│                              Request                                        │
└────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                          TenantBehavior                                     │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ 1. ITenantResolver identifies the tenant (header, claim, subdomain)  │  │
│  │ 2. ITenantStore loads complete tenant data                          │  │
│  │ 3. ITenantContextAccessor stores context for the request            │  │
│  │ 4. ITenantRequired validation                                       │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                              Handler                                        │
│            (Accesses ITenantContext via ITenantContextAccessor)             │
└────────────────────────────────────────────────────────────────────────────┘
```

## Main Interfaces

### ITenantContext

Represents the current tenant's information:

```csharp
public interface ITenantContext
{
    string? TenantId { get; }
    string? TenantName { get; }
    bool IsResolved { get; }
    IReadOnlyDictionary<string, object?> Properties { get; }
    T? GetProperty<T>(string key, T? defaultValue = default);
}
```

### ITenantContextAccessor

Access to tenant context in the current request:

```csharp
public interface ITenantContextAccessor
{
    ITenantContext? Context { get; set; }
}
```

### ITenantResolver

Resolves the tenant from the request:

```csharp
public interface ITenantResolver
{
    Task<ITenantContext?> ResolveAsync(CancellationToken cancellationToken = default);
}
```

### ITenantStore

Stores and retrieves tenant information:

```csharp
public interface ITenantStore
{
    Task<ITenantContext?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ITenantContext>> GetAllAsync(CancellationToken cancellationToken = default);
}
```

## Configuration

### Basic Registration

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

// Register multi-tenancy components
services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
services.AddScoped<ITenantResolver, HeaderTenantResolver>(); // or custom
services.AddSingleton<ITenantStore, InMemoryTenantStore>();
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBehavior<,>));
```

### Implementing a Custom Resolver

```csharp
public class HeaderTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantStore _tenantStore;

    public HeaderTenantResolver(
        IHttpContextAccessor httpContextAccessor,
        ITenantStore tenantStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantStore = tenantStore;
    }

    public async Task<ITenantContext?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Try to get from header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
            return await _tenantStore.GetByIdAsync(tenantId!, cancellationToken);
        }

        // Try to get from claim
        var claim = httpContext.User?.FindFirst("tenant_id");
        if (claim != null)
        {
            return await _tenantStore.GetByIdAsync(claim.Value, cancellationToken);
        }

        return null;
    }
}
```

### Subdomain Resolver

```csharp
public class SubdomainTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantStore _tenantStore;

    public async Task<ITenantContext?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        if (string.IsNullOrEmpty(host)) return null;

        // Extract subdomain (e.g., "acme" from "acme.app.com")
        var parts = host.Split('.');
        if (parts.Length < 3) return null;

        var subdomain = parts[0];
        return await _tenantStore.GetByIdAsync(subdomain, cancellationToken);
    }
}
```

## Request Markers

### ITenantRequired

Marks requests that require a valid tenant:

```csharp
public class CreateProductCommand : IMediatorCommand<int>, ITenantRequired
{
    public string Name { get; init; }
    public decimal Price { get; init; }
}

// Handler
public class CreateProductHandler : IMediatorCommandHandler<CreateProductCommand, int>
{
    private readonly ITenantContextAccessor _tenantContext;
    private readonly IRepository<Product> _repository;

    public CreateProductHandler(
        ITenantContextAccessor tenantContext,
        IRepository<Product> repository)
    {
        _tenantContext = tenantContext;
        _repository = repository;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.Context!.TenantId; // Guaranteed by behavior

        var product = new Product
        {
            TenantId = tenantId,
            Name = request.Name,
            Price = request.Price
        };

        await _repository.AddAsync(product);
        return product.Id;
    }
}
```

### ITenantAware

For requests that can specify a different tenant (admin operations):

```csharp
public class GetAllProductsQuery : IMediatorQuery<List<ProductDto>>, ITenantAware
{
    // Allows overriding the tenant for cross-tenant operations
    public string? OverrideTenantId { get; init; }
}

// Usage in admin context
var products = await _mediator.SendAsync(new GetAllProductsQuery 
{ 
    OverrideTenantId = "tenant-123" // Query as another tenant
});
```

## User Context

### ICurrentUser

Interface for current user information:

```csharp
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<Claim> Claims { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    T? GetClaim<T>(string claimType, T? defaultValue = default);
}
```

### CurrentUserBehavior

Injects the current user into the request context:

```csharp
// Registration
services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CurrentUserBehavior<,>));

// Usage in handler
public class CreateOrderHandler : IMediatorCommandHandler<CreateOrderCommand, Order>
{
    private readonly ICurrentUserAccessor _currentUser;

    public async Task<Order> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            CreatedBy = _currentUser.User?.UserId,
            CustomerName = _currentUser.User?.UserName
        };
        
        // ...
    }
}
```

## Automatic Tenant Filters

### Entity with Tenant

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}

public class Product : ITenantEntity
{
    public int Id { get; set; }
    public string TenantId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

### Global Query Filter (EF Core)

```csharp
public class AppDbContext : DbContext
{
    private readonly ITenantContextAccessor _tenantContext;

    public AppDbContext(
        DbContextOptions options,
        ITenantContextAccessor tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Automatic filter for all tenant entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(CreateTenantFilter(entityType.ClrType));
            }
        }
    }

    private LambdaExpression CreateTenantFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var tenantProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
        var tenantValue = Expression.Property(
            Expression.Constant(_tenantContext),
            nameof(ITenantContextAccessor.Context));
        var tenantId = Expression.Property(tenantValue, nameof(ITenantContext.TenantId));
        var comparison = Expression.Equal(tenantProperty, tenantId);
        
        return Expression.Lambda(comparison, parameter);
    }
}
```

### Query Extensions

```csharp
public static class TenantQueryExtensions
{
    public static IQueryable<T> ForTenant<T>(
        this IQueryable<T> query, 
        ITenantContext tenant) where T : ITenantEntity
    {
        if (tenant?.TenantId == null)
            return query;
            
        return query.Where(e => e.TenantId == tenant.TenantId);
    }

    public static IQueryable<T> ForCurrentTenant<T>(
        this IQueryable<T> query,
        ITenantContextAccessor accessor) where T : ITenantEntity
    {
        return query.ForTenant(accessor.Context);
    }
}

// Usage
var products = await _dbContext.Products
    .ForCurrentTenant(_tenantContext)
    .ToListAsync();
```

## Best Practices

### 1. Always Validate the Tenant

```csharp
public class GetProductQuery : IMediatorQuery<ProductDto>, ITenantRequired
{
    public int ProductId { get; init; }
}
```

### 2. Segregate Sensitive Data

```csharp
// Use different connection strings per tenant if needed
public class TenantConnectionResolver
{
    public string GetConnectionString(ITenantContext tenant)
    {
        return tenant.GetProperty<string>("ConnectionString") 
            ?? _defaultConnectionString;
    }
}
```

### 3. Cache per Tenant

```csharp
public class TenantAwareCacheKeyGenerator
{
    public string GenerateKey<T>(T request, ITenantContext tenant)
    {
        var baseKey = typeof(T).Name;
        return $"{tenant.TenantId}:{baseKey}:{JsonSerializer.Serialize(request)}";
    }
}
```

### 4. Audit with Tenant

```csharp
public class TenantAuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var tenant = _tenantContext.Context?.TenantId ?? "system";
        var user = _currentUser.User?.UserId ?? "anonymous";
        
        _logger.LogInformation(
            "[Audit] Tenant={Tenant} User={User} Request={Request}",
            tenant, user, typeof(TRequest).Name);
            
        return await next();
    }
}
```

## Complete Example

```csharp
// Startup/Program.cs
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
services.AddScoped<ITenantResolver, HeaderTenantResolver>();
services.AddSingleton<ITenantStore, InMemoryTenantStore>();
services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CurrentUserBehavior<,>));

// Command
public record CreateOrderCommand(string ProductId, int Quantity) 
    : IMediatorCommand<OrderDto>, ITenantRequired;

// Handler
public class CreateOrderHandler : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly ITenantContextAccessor _tenant;
    private readonly ICurrentUserAccessor _user;
    private readonly IRepository<Order> _repository;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = new Order
        {
            TenantId = _tenant.Context!.TenantId,
            CreatedBy = _user.User?.UserId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(order);
        return order.ToDto();
    }
}
```


