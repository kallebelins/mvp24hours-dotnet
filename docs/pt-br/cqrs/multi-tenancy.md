# Multi-tenancy

## Visão Geral

O Mvp24Hours CQRS oferece suporte completo a multi-tenancy, permitindo que uma única aplicação atenda múltiplos clientes (tenants) de forma isolada e segura. O sistema fornece:

1. **Contexto de Tenant** - Informações sobre o tenant atual
2. **Resolução de Tenant** - Identificação automática do tenant
3. **Injeção via Pipeline** - Propagação automática do contexto
4. **Filtros de Query** - Isolamento automático de dados

## Arquitetura

```
┌────────────────────────────────────────────────────────────────────────────┐
│                              Requisição                                     │
└────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                          TenantBehavior                                     │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ 1. ITenantResolver identifica o tenant (header, claim, subdomain)    │  │
│  │ 2. ITenantStore carrega dados completos do tenant                    │  │
│  │ 3. ITenantContextAccessor armazena contexto para a requisição        │  │
│  │ 4. Validação de ITenantRequired                                      │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                              Handler                                        │
│            (Acessa ITenantContext via ITenantContextAccessor)               │
└────────────────────────────────────────────────────────────────────────────┘
```

## Interfaces Principais

### ITenantContext

Representa as informações do tenant atual:

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

Acesso ao contexto do tenant na requisição atual:

```csharp
public interface ITenantContextAccessor
{
    ITenantContext? Context { get; set; }
}
```

### ITenantResolver

Resolve o tenant a partir da requisição:

```csharp
public interface ITenantResolver
{
    Task<ITenantContext?> ResolveAsync(CancellationToken cancellationToken = default);
}
```

### ITenantStore

Armazena e recupera informações de tenants:

```csharp
public interface ITenantStore
{
    Task<ITenantContext?> GetByIdAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ITenantContext>> GetAllAsync(CancellationToken cancellationToken = default);
}
```

## Configuração

### Registro Básico

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

// Registrar componentes de multi-tenancy
services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
services.AddScoped<ITenantResolver, HeaderTenantResolver>(); // ou customizado
services.AddSingleton<ITenantStore, InMemoryTenantStore>();
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantBehavior<,>));
```

### Implementando um Resolver Customizado

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

        // Tentar obter do header
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId))
        {
            return await _tenantStore.GetByIdAsync(tenantId!, cancellationToken);
        }

        // Tentar obter de claim
        var claim = httpContext.User?.FindFirst("tenant_id");
        if (claim != null)
        {
            return await _tenantStore.GetByIdAsync(claim.Value, cancellationToken);
        }

        return null;
    }
}
```

### Resolver por Subdomínio

```csharp
public class SubdomainTenantResolver : ITenantResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITenantStore _tenantStore;

    public async Task<ITenantContext?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        if (string.IsNullOrEmpty(host)) return null;

        // Extrair subdomínio (ex: "acme" de "acme.app.com")
        var parts = host.Split('.');
        if (parts.Length < 3) return null;

        var subdomain = parts[0];
        return await _tenantStore.GetByIdAsync(subdomain, cancellationToken);
    }
}
```

## Marcadores de Requisição

### ITenantRequired

Marca requisições que exigem um tenant válido:

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
        var tenantId = _tenantContext.Context!.TenantId; // Garantido pelo behavior

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

Para requisições que podem especificar um tenant diferente (operações de admin):

```csharp
public class GetAllProductsQuery : IMediatorQuery<List<ProductDto>>, ITenantAware
{
    // Permite sobrescrever o tenant para operações cross-tenant
    public string? OverrideTenantId { get; init; }
}

// Uso em contexto de admin
var products = await _mediator.SendAsync(new GetAllProductsQuery 
{ 
    OverrideTenantId = "tenant-123" // Consulta como outro tenant
});
```

## Contexto de Usuário

### ICurrentUser

Interface para informações do usuário atual:

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

Injeta o usuário atual no contexto da requisição:

```csharp
// Registro
services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CurrentUserBehavior<,>));

// Uso no handler
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

## Filtros Automáticos por Tenant

### Entidade com Tenant

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

### Query Filter Global (EF Core)

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
        // Filtro automático para todas as entidades com tenant
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

### Extensões para Queries

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

// Uso
var products = await _dbContext.Products
    .ForCurrentTenant(_tenantContext)
    .ToListAsync();
```

## Boas Práticas

### 1. Sempre Valide o Tenant

```csharp
public class GetProductQuery : IMediatorQuery<ProductDto>, ITenantRequired
{
    public int ProductId { get; init; }
}
```

### 2. Segregue Dados Sensíveis

```csharp
// Use connection strings diferentes por tenant se necessário
public class TenantConnectionResolver
{
    public string GetConnectionString(ITenantContext tenant)
    {
        return tenant.GetProperty<string>("ConnectionString") 
            ?? _defaultConnectionString;
    }
}
```

### 3. Cache por Tenant

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

### 4. Auditoria com Tenant

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

## Exemplo Completo

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


