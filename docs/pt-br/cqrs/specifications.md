# Specification Pattern no CQRS

## Visão Geral

O Mvp24Hours CQRS estende o padrão Specification existente com recursos avançados para consultas tipadas, composição de expressões e integração com Entity Framework Core. Os principais componentes são:

1. **Specification\<T\>** - Classe base com suporte a expressões
2. **Operadores de Composição** - And, Or, Not para combinar especificações
3. **SpecificationEvaluator** - Avaliação otimizada para EF Core
4. **PaginatedQuery** - Base para queries paginadas
5. **SortedQuery** - Base para queries ordenadas

## Arquitetura

```
┌────────────────────────────────────────────────────────────────────────────┐
│                              Query                                          │
│       (GetProductsByCategory : PaginatedQuery<Product, List<ProductDto>>)   │
└────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                          Handler                                            │
│   ┌────────────────────────────────────────────────────────────────────┐   │
│   │  var spec = new ProductByCategorySpec(query.CategoryId)             │   │
│   │            .And(new ActiveProductSpec())                            │   │
│   │            .And(new InStockSpec());                                 │   │
│   │                                                                     │   │
│   │  var products = await SpecificationEvaluator<Product>               │   │
│   │      .GetQuery(_dbContext.Products, spec)                           │   │
│   │      .ApplyPaging(query)                                            │   │
│   │      .ToListAsync();                                                │   │
│   └────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────┘
```

## Specification\<T\>

### Classe Base

```csharp
public abstract class Specification<T> : ISpecification<T>, ISpecificationQuery<T>
    where T : class
{
    public abstract Expression<Func<T, bool>> Criteria { get; }
    
    public virtual bool IsSatisfiedBy(T entity)
    {
        return Criteria.Compile()(entity);
    }
    
    // Alias para compatibilidade com ISpecificationQuery
    Expression<Func<T, bool>> ISpecificationQuery<T>.IsSatisfiedByExpression => Criteria;
}
```

### Implementando Especificações

```csharp
public class ActiveProductSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> Criteria => 
        p => p.IsActive && !p.IsDeleted;
}

public class ProductByCategorySpec : Specification<Product>
{
    private readonly int _categoryId;

    public ProductByCategorySpec(int categoryId)
    {
        _categoryId = categoryId;
    }

    public override Expression<Func<Product, bool>> Criteria => 
        p => p.CategoryId == _categoryId;
}

public class PriceRangeSpec : Specification<Product>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public PriceRangeSpec(decimal minPrice, decimal maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> Criteria => 
        p => p.Price >= _minPrice && p.Price <= _maxPrice;
}
```

## Operadores de Composição

### And (E)

```csharp
// Produtos ativos E na categoria especificada
var spec = new ActiveProductSpec()
    .And(new ProductByCategorySpec(categoryId));

// Uso
var products = query.Where(spec.Criteria);
```

### Or (Ou)

```csharp
// Produtos em promoção OU com frete grátis
var spec = new OnSaleSpec()
    .Or(new FreeShippingSpec());
```

### Not (Negação)

```csharp
// Produtos NÃO esgotados
var spec = new OutOfStockSpec().Not();

// Equivalente a:
var spec = new InStockSpec();
```

### Composição Complexa

```csharp
// (Ativos E Em estoque) E (Em promoção OU Categoria = 5)
var spec = new ActiveProductSpec()
    .And(new InStockSpec())
    .And(
        new OnSaleSpec().Or(new ProductByCategorySpec(5))
    );
```

## SpecificationEvaluator

O `SpecificationEvaluator<T>` aplica especificações a queries IQueryable de forma otimizada:

```csharp
public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(
        IQueryable<T> inputQuery, 
        ISpecification<T> specification)
    {
        return inputQuery.Where(specification.Criteria);
    }

    public static IQueryable<T> GetQuery(
        IQueryable<T> inputQuery, 
        ISpecificationQuery<T> specification)
    {
        return inputQuery.Where(specification.IsSatisfiedByExpression);
    }
}
```

### Uso no Handler

```csharp
public class GetProductsHandler : IMediatorQueryHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly AppDbContext _dbContext;

    public async Task<List<ProductDto>> Handle(
        GetProductsQuery request, 
        CancellationToken cancellationToken)
    {
        var spec = new ActiveProductSpec()
            .And(new ProductByCategorySpec(request.CategoryId));

        var products = await SpecificationEvaluator<Product>
            .GetQuery(_dbContext.Products, spec)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price
            })
            .ToListAsync(cancellationToken);

        return products;
    }
}
```

## PaginatedQuery\<T\>

Base para queries com paginação integrada ao `IPagingCriteria`:

```csharp
public abstract class PaginatedQuery<TResponse> : IMediatorQuery<TResponse>, IPagingCriteria
{
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    public virtual int MaxPageSize { get; } = 100;
    public IReadOnlyCollection<string> OrderBy { get; set; }
    public IReadOnlyCollection<string> Navigation { get; set; }

    // Implementação de IPagingCriteria
    int IPagingCriteria.Limit => PageSize;
    int IPagingCriteria.Offset => Page;
}
```

### Implementando Query Paginada

```csharp
public class GetCustomersQuery : PaginatedQuery<PagedResult<CustomerDto>>
{
    public string? NameFilter { get; init; }
    public bool? ActiveOnly { get; init; }
}

public class GetCustomersHandler : IMediatorQueryHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly IRepository<Customer> _repository;

    public async Task<PagedResult<CustomerDto>> Handle(
        GetCustomersQuery request, 
        CancellationToken cancellationToken)
    {
        var spec = BuildSpecification(request);

        var totalCount = await _repository.CountAsync(spec.Criteria);

        var customers = await _repository
            .GetByAsync(spec.Criteria, request) // IPagingCriteria
            .ToListAsync();

        return new PagedResult<CustomerDto>
        {
            Items = customers.Select(c => c.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }

    private Specification<Customer> BuildSpecification(GetCustomersQuery request)
    {
        var spec = Specification<Customer>.All; // Todos

        if (!string.IsNullOrEmpty(request.NameFilter))
        {
            spec = spec.And(new CustomerNameContainsSpec(request.NameFilter));
        }

        if (request.ActiveOnly == true)
        {
            spec = spec.And(new ActiveCustomerSpec());
        }

        return spec;
    }
}
```

## PaginatedQuery\<TEntity, TResponse\>

Versão tipada com suporte a expressões:

```csharp
public abstract class PaginatedQuery<TEntity, TResponse> 
    : PaginatedQuery<TResponse>, IPagingCriteriaExpression<TEntity>
    where TEntity : class
{
    public IList<Expression<Func<TEntity, dynamic>>> OrderByAscendingExpr { get; }
    public IList<Expression<Func<TEntity, dynamic>>> OrderByDescendingExpr { get; }
    public IList<Expression<Func<TEntity, dynamic>>> NavigationExpr { get; }

    public PaginatedQuery<TEntity, TResponse> OrderByAsc(
        Expression<Func<TEntity, dynamic>> expression);
    
    public PaginatedQuery<TEntity, TResponse> OrderByDesc(
        Expression<Func<TEntity, dynamic>> expression);
    
    public PaginatedQuery<TEntity, TResponse> Include(
        Expression<Func<TEntity, dynamic>> expression);
}
```

### Uso com Expressões

```csharp
public class GetOrdersQuery : PaginatedQuery<Order, List<OrderDto>>
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public OrderStatus? Status { get; init; }
}

// Na API
[HttpGet("orders")]
public async Task<PagedResult<OrderDto>> GetOrders(
    [FromQuery] int page = 0,
    [FromQuery] int pageSize = 20,
    [FromQuery] DateTime? fromDate = null,
    [FromQuery] string? sortBy = null)
{
    var query = new GetOrdersQuery
    {
        Page = page,
        PageSize = pageSize,
        FromDate = fromDate
    };

    // Ordenação tipada
    if (sortBy == "date")
        query.OrderByDesc(o => o.CreatedAt);
    else if (sortBy == "total")
        query.OrderByDesc(o => o.Total);
    else
        query.OrderByAsc(o => o.Id);

    // Incluir navegações
    query.Include(o => o.Customer);
    query.Include(o => o.Items);

    return await _mediator.SendAsync(query);
}
```

## SortedQuery\<T\>

Base para queries apenas com ordenação (sem paginação):

```csharp
public abstract class SortedQuery<TResponse> : IMediatorQuery<TResponse>
{
    public IReadOnlyCollection<string> OrderBy { get; set; }

    public SortedQuery<TResponse> WithOrderBy(params string[] orderBy)
    {
        OrderBy = orderBy;
        return this;
    }
}
```

### Exemplo

```csharp
public class GetTopProductsQuery : SortedQuery<List<ProductDto>>
{
    public int Top { get; init; } = 10;
    public string? Category { get; init; }
}

// Uso
var query = new GetTopProductsQuery { Top = 5, Category = "Electronics" }
    .WithOrderBy("-sales", "name"); // -sales = descending, name = ascending
```

## Especificações Comuns

### Specification.All e Specification.None

```csharp
// Retorna todos os registros
var allSpec = Specification<Product>.All;

// Não retorna nenhum registro
var noneSpec = Specification<Product>.None;

// Uso condicional
var spec = showAll 
    ? Specification<Product>.All 
    : new ActiveProductSpec();
```

### Especificação por ID

```csharp
public class ByIdSpec<T> : Specification<T> where T : IEntity
{
    private readonly object _id;

    public ByIdSpec(object id) => _id = id;

    public override Expression<Func<T, bool>> Criteria => 
        e => e.Id.Equals(_id);
}

// Uso
var spec = new ByIdSpec<Product>(productId);
```

### Especificação por IDs

```csharp
public class ByIdsSpec<T> : Specification<T> where T : IEntity<int>
{
    private readonly IEnumerable<int> _ids;

    public ByIdsSpec(IEnumerable<int> ids) => _ids = ids;

    public override Expression<Func<T, bool>> Criteria => 
        e => _ids.Contains(e.Id);
}
```

## Integração com Repository

```csharp
public interface ISpecificationRepository<T> where T : class
{
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<List<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default);
}

public class SpecificationRepository<T> : ISpecificationRepository<T> where T : class
{
    private readonly DbContext _context;

    public async Task<List<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await SpecificationEvaluator<T>
            .GetQuery(_context.Set<T>(), spec)
            .ToListAsync(ct);
    }

    public async Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default)
    {
        return await SpecificationEvaluator<T>
            .GetQuery(_context.Set<T>(), spec)
            .FirstOrDefaultAsync(ct);
    }
}
```

## Boas Práticas

### 1. Nomeie Especificações Claramente

```csharp
// ✅ Bom - nome descritivo
public class OrdersPlacedInLastThirtyDaysSpec : Specification<Order> { }
public class CustomerWithVerifiedEmailSpec : Specification<Customer> { }

// ❌ Evite - nome vago
public class OrderSpec : Specification<Order> { }
```

### 2. Mantenha Especificações Pequenas e Focadas

```csharp
// ✅ Bom - especificações atômicas
var spec = new ActiveProductSpec()
    .And(new InStockSpec())
    .And(new PriceRangeSpec(10, 100));

// ❌ Evite - especificação com muitas responsabilidades
public class ActiveInStockPricedProductSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> Criteria =>
        p => p.IsActive && p.Stock > 0 && p.Price >= 10 && p.Price <= 100;
}
```

### 3. Reutilize Especificações

```csharp
public static class ProductSpecs
{
    public static Specification<Product> Active => new ActiveProductSpec();
    public static Specification<Product> InStock => new InStockSpec();
    public static Specification<Product> OnSale => new OnSaleSpec();
    
    public static Specification<Product> AvailableForPurchase =>
        Active.And(InStock);
}

// Uso
var spec = ProductSpecs.AvailableForPurchase
    .And(new ProductByCategorySpec(categoryId));
```

### 4. Use com Validação In-Memory

```csharp
var spec = new ValidOrderSpec();

// Validar uma entidade antes de salvar
if (!spec.IsSatisfiedBy(order))
{
    throw new ValidationException("Order is not valid");
}
```

## Exemplo Completo

```csharp
// Especificações
public class ActiveCustomerSpec : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> Criteria => 
        c => c.IsActive && !c.IsDeleted;
}

public class CustomerWithOrdersSpec : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> Criteria => 
        c => c.Orders.Any();
}

public class CustomerNameContainsSpec : Specification<Customer>
{
    private readonly string _search;
    public CustomerNameContainsSpec(string search) => _search = search.ToLower();

    public override Expression<Func<Customer, bool>> Criteria => 
        c => c.Name.ToLower().Contains(_search);
}

// Query
public class SearchCustomersQuery : PaginatedQuery<Customer, PagedResult<CustomerDto>>
{
    public string? Search { get; init; }
    public bool ActiveOnly { get; init; } = true;
    public bool WithOrdersOnly { get; init; }
}

// Handler
public class SearchCustomersHandler 
    : IMediatorQueryHandler<SearchCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly AppDbContext _dbContext;

    public async Task<PagedResult<CustomerDto>> Handle(
        SearchCustomersQuery request, 
        CancellationToken cancellationToken)
    {
        // Construir especificação dinamicamente
        var spec = Specification<Customer>.All;

        if (request.ActiveOnly)
            spec = spec.And(new ActiveCustomerSpec());

        if (request.WithOrdersOnly)
            spec = spec.And(new CustomerWithOrdersSpec());

        if (!string.IsNullOrEmpty(request.Search))
            spec = spec.And(new CustomerNameContainsSpec(request.Search));

        // Aplicar especificação e paginação
        var query = SpecificationEvaluator<Customer>.GetQuery(_dbContext.Customers, spec);

        var totalCount = await query.CountAsync(cancellationToken);

        var customers = await query
            .OrderBy(c => c.Name)
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                OrderCount = c.Orders.Count
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>
        {
            Items = customers,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}
```


