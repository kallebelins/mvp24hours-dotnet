# Queries

## Overview

Queries represent read operations in the system. Following the CQRS pattern, queries never modify state and always return data.

## Interface

```csharp
public interface IMediatorQuery<out TResponse> : IMediatorRequest<TResponse>
{
}
```

> **Note**: Unlike Commands, Queries always have a return value.

## Creating Queries

### Simple Query

```csharp
public record GetOrderByIdQuery : IMediatorQuery<OrderDto>
{
    public Guid OrderId { get; init; }
}
```

### Query with Filters

```csharp
public record GetOrdersQuery : IMediatorQuery<IEnumerable<OrderDto>>
{
    public OrderStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
```

### Paginated Query

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

### Cached Query

```csharp
public record GetOrderByIdQuery : IMediatorQuery<OrderDto>, ICacheableRequest
{
    public Guid OrderId { get; init; }
    
    // Unique cache key
    public string CacheKey => $"order:{OrderId}";
    
    // Cache duration (optional)
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
```

## Creating Handlers

### Simple Handler

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

### Handler with Filters

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

### Paginated Handler

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

## Stream Queries

For large data volumes, use `IStreamRequest`:

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

## Sending Queries

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

## Best Practices

1. **Read-Only**: Queries should never modify state
2. **Cache**: Use `ICacheableRequest` for frequently accessed data
3. **Pagination**: Always paginate large lists
4. **Projection**: Return only necessary fields (specific DTOs)
5. **Streaming**: Use `IStreamRequest` for large volumes
6. **Specifications**: Use Specification Pattern for complex queries

