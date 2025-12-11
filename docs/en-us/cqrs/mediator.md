# IMediator, ISender and IPublisher

## Overview

The Mediator is composed of three main interfaces that define communication capabilities:

| Interface | Responsibility |
|-----------|----------------|
| `ISender` | Send requests (Commands/Queries) to handlers |
| `IPublisher` | Publish notifications to multiple handlers |
| `IStreamSender` | Send requests that return streams |
| `IMediator` | Combines all interfaces above |

## ISender

Interface for sending requests that expect a response.

```csharp
public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(
        IMediatorRequest<TResponse> request, 
        CancellationToken cancellationToken = default);
}
```

### Usage

```csharp
public class OrderController : ControllerBase
{
    private readonly ISender _sender;

    public OrderController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command)
    {
        // Send command and await response
        var result = await _sender.SendAsync(command);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        // Send query and await response
        var result = await _sender.SendAsync(new GetOrderByIdQuery { Id = id });
        return result is not null ? Ok(result) : NotFound();
    }
}
```

## IPublisher

Interface for publishing notifications (in-process events).

```csharp
public interface IPublisher
{
    Task PublishAsync<TNotification>(
        TNotification notification, 
        CancellationToken cancellationToken = default)
        where TNotification : IMediatorNotification;
}
```

### Usage

```csharp
public class OrderService
{
    private readonly IPublisher _publisher;

    public OrderService(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task ProcessOrder(Order order)
    {
        // Process the order...

        // Publish notification to all interested handlers
        await _publisher.PublishAsync(new OrderProcessedNotification
        {
            OrderId = order.Id,
            ProcessedAt = DateTime.UtcNow
        });
    }
}
```

### Publishing Strategies

The Publisher supports different strategies:

| Strategy | Description |
|----------|-------------|
| `Sequential` | Executes handlers one after another (default) |
| `ParallelNoWait` | Fires all in parallel without waiting |
| `ParallelWhenAll` | Executes in parallel and waits for all |

## IStreamSender

Interface for requests that return streams (`IAsyncEnumerable`).

```csharp
public interface IStreamSender
{
    IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request, 
        CancellationToken cancellationToken = default);
}
```

### Usage

```csharp
// Define stream request
public record GetOrdersStreamQuery : IStreamRequest<OrderDto>
{
    public DateTime FromDate { get; init; }
}

// Implement handler
public class GetOrdersStreamQueryHandler 
    : IStreamRequestHandler<GetOrdersStreamQuery, OrderDto>
{
    public async IAsyncEnumerable<OrderDto> Handle(
        GetOrdersStreamQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var order in _repository.GetOrdersAsync(request.FromDate))
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
                
            yield return MapToDto(order);
        }
    }
}

// Use in controller
[HttpGet("stream")]
public async IAsyncEnumerable<OrderDto> GetOrdersStream(
    DateTime fromDate,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await foreach (var order in _mediator.CreateStream(
        new GetOrdersStreamQuery { FromDate = fromDate }, 
        cancellationToken))
    {
        yield return order;
    }
}
```

## IMediator

Main interface that combines all capabilities:

```csharp
public interface IMediator : ISender, IPublisher, IStreamSender
{
}
```

### When to use which interface?

| Scenario | Recommended Interface |
|----------|----------------------|
| Only send Commands/Queries | `ISender` |
| Only publish notifications | `IPublisher` |
| Only streams | `IStreamSender` |
| Multiple operations | `IMediator` |

### Example with IMediator

```csharp
public class OrderApplicationService
{
    private readonly IMediator _mediator;

    public OrderApplicationService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<OrderDto> CreateAndNotify(CreateOrderCommand command)
    {
        // Send command
        var order = await _mediator.SendAsync(command);

        // Publish notification
        await _mediator.PublishAsync(new OrderCreatedNotification
        {
            OrderId = order.Id
        });

        return order;
    }
}
```

## Dependency Injection

All interfaces are automatically registered:

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

// Registers:
// - IMediator -> Mediator (Scoped)
// - ISender -> IMediator (Scoped)
// - IPublisher -> IMediator (Scoped)
// - IStreamSender -> IMediator (Scoped)
```

## Best Practices

1. **Prefer specific interfaces**: Use `ISender` or `IPublisher` when possible
2. **Avoid circular dependency**: Don't inject Mediator in handlers
3. **Use CancellationToken**: Always propagate the cancellation token
4. **Don't mix responsibilities**: Commands should modify, Queries should read

