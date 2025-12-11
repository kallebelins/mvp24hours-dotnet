# Queries

## Visão Geral

Queries representam operações de leitura no sistema. Seguindo o padrão CQRS, queries nunca modificam estado e sempre retornam dados.

## Interface

```csharp
public interface IMediatorQuery<out TResponse> : IMediatorRequest<TResponse>
{
}
```

> **Nota**: Diferente de Commands, Queries sempre têm retorno.

## Criando Queries

### Query Simples

```csharp
public record GetOrderByIdQuery : IMediatorQuery<OrderDto>
{
    public Guid OrderId { get; init; }
}
```

### Query com Filtros

```csharp
public record GetOrdersQuery : IMediatorQuery<IEnumerable<OrderDto>>
{
    public OrderStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
```

### Query Paginada

```csharp
public record GetOrdersPagedQuery : IMediatorQuery<PagedResult<OrderDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; }
    public bool Descending { get; init; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

### Query com Cache

```csharp
public record GetOrderByIdQuery : IMediatorQuery<OrderDto>, ICacheableRequest
{
    public Guid OrderId { get; init; }
    
    // Chave única para cache
    public string CacheKey => $"order:{OrderId}";
    
    // Duração do cache (opcional)
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
```

## Criando Handlers

### Handler Simples

```csharp
public class GetOrderByIdQueryHandler 
    : IMediatorQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IMapper _mapper;

    public GetOrderByIdQueryHandler(
        IOrderRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(
        GetOrderByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId);
        
        if (order is null)
            throw new NotFoundException("Order", request.OrderId);

        return _mapper.Map<OrderDto>(order);
    }
}
```

### Handler com Filtros

```csharp
public class GetOrdersQueryHandler 
    : IMediatorQueryHandler<GetOrdersQuery, IEnumerable<OrderDto>>
{
    private readonly IOrderRepository _repository;
    private readonly IMapper _mapper;

    public GetOrdersQueryHandler(
        IOrderRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OrderDto>> Handle(
        GetOrdersQuery request, 
        CancellationToken cancellationToken)
    {
        var specification = new OrderSpecification()
            .WithStatus(request.Status)
            .WithDateRange(request.FromDate, request.ToDate);

        var orders = await _repository.ListAsync(specification);

        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}
```

### Handler Paginado

```csharp
public class GetOrdersPagedQueryHandler 
    : IMediatorQueryHandler<GetOrdersPagedQuery, PagedResult<OrderDto>>
{
    private readonly IOrderRepository _repository;
    private readonly IMapper _mapper;

    public GetOrdersPagedQueryHandler(
        IOrderRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResult<OrderDto>> Handle(
        GetOrdersPagedQuery request, 
        CancellationToken cancellationToken)
    {
        var specification = new OrderSpecification()
            .WithSearch(request.SearchTerm)
            .WithPaging(request.Page, request.PageSize)
            .WithSorting(request.SortBy, request.Descending);

        var orders = await _repository.ListAsync(specification);
        var totalCount = await _repository.CountAsync(specification);

        return new PagedResult<OrderDto>
        {
            Items = _mapper.Map<IEnumerable<OrderDto>>(orders),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
```

## Queries com Stream

Para grandes volumes de dados, use `IStreamRequest`:

```csharp
public record GetAllOrdersStreamQuery : IStreamRequest<OrderDto>
{
    public DateTime? FromDate { get; init; }
}

public class GetAllOrdersStreamQueryHandler 
    : IStreamRequestHandler<GetAllOrdersStreamQuery, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IMapper _mapper;

    public async IAsyncEnumerable<OrderDto> Handle(
        GetAllOrdersStreamQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var order in _repository.StreamAllAsync(request.FromDate))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
                
            yield return _mapper.Map<OrderDto>(order);
        }
    }
}
```

## Enviando Queries

```csharp
public class OrderController : ControllerBase
{
    private readonly ISender _sender;

    public OrderController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(
            new GetOrderByIdQuery { OrderId = id }, 
            cancellationToken);
            
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.SendAsync(new GetOrdersPagedQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = search
        }, cancellationToken);
        
        return Ok(result);
    }
}
```

## Boas Práticas

1. **Somente Leitura**: Queries nunca devem modificar estado
2. **Cache**: Use `ICacheableRequest` para dados frequentemente acessados
3. **Paginação**: Sempre pagine listas grandes
4. **Projeção**: Retorne apenas os campos necessários (DTOs específicos)
5. **Streaming**: Use `IStreamRequest` para grandes volumes
6. **Especificações**: Use Specification Pattern para queries complexas

