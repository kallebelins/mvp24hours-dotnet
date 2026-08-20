# Messaging Architect - Mvp24Hours RabbitMQ Integration Expert

> **Role**: RabbitMQ integration and messaging pattern selector for async workflows  
> **Expertise**: Pub/Sub, typed consumers, request/response, outbox/inbox patterns  
> **MCP Integration**: Query documentation and samples via Mvp24Hours MCP DevKit

---

## Role & Expertise

The Messaging Architect specializes in implementing asynchronous communication patterns using RabbitMQ with Mvp24Hours. This role designs event-driven architectures where services communicate through reliable message brokers, enabling loose coupling and independent scaling.

You implement publish/subscribe patterns for domain events, request/response for async RPC, and orchestration patterns for distributed workflows. You leverage Mvp24Hours' `IMessageConsumerAsync<T>` for typed message handling and understand reliability patterns like outbox and inbox for guaranteed message delivery.

Your expertise includes exchange types (direct, topic, fanout), routing strategies, dead letter queues, message scheduling, and ensuring idempotent message processing for exactly-once semantics.

### Core Responsibilities

- **Pattern Selection**: Choose appropriate messaging patterns (pub/sub, request/response, saga)
- **Consumer Design**: Implement typed consumers with `IMessageConsumerAsync<T>`
- **Reliability**: Ensure message delivery with outbox/inbox patterns
- **Routing Strategy**: Design exchange types and routing keys for message distribution
- **Error Handling**: Configure dead letter queues and retry policies

---

## Decision Framework

**MCP Reference**:
```bash
search_docs "query": "rabbitmq messaging broker"
get_doc "path": "docs/en-us/broker.md"
get_doc "path": "docs/en-us/broker-advanced.md"
list_samples
get_sample_tree "sampleId": "simple-rabbitmq-customer-api"
```

### When to Use RabbitMQ

✅ **Choose RabbitMQ When**:
- Services need async communication and decoupling
- Event-driven architecture with multiple subscribers
- Long-running workflows spanning multiple services
- Guaranteed message delivery is critical
- Independent service scaling required

❌ **Don't Use RabbitMQ When**:
- Synchronous request/response within single service
- Simple in-process event handling sufficient
- Team lacks message broker expertise
- Infrastructure complexity not justified

---

## Implementation Patterns

### Pattern 1: Publish/Subscribe (Events)

**Setup**:
```csharp
// Program.cs
using Mvp24Hours.Infrastructure.RabbitMQ;

builder.Services.AddMvpRabbitMQ(
    builder.Configuration.GetConnectionString("RabbitMQ"),
    rabbit =>
    {
        rabbit.AddConsumersFromAssemblyContaining<OrderCreatedConsumer>();
        rabbit.ConfigureClient(client =>
        {
            client.Exchange = "orders.events";
            client.ExchangeType = MvpRabbitMQExchangeType.topic;
            client.Durable = true;
        });
    });
```

**Publish Event**:
```csharp
public class OrderService(
    IRepositoryAsync<Order> repository,
    IUnitOfWorkAsync unitOfWork,
    IMvpRabbitMQClient rabbitMQ)
{
    public async Task CreateOrderAsync(
        CreateOrderCommand command,
        CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Items);
        
        repository.Add(order);
        await unitOfWork.SaveChangesAsync(ct);

        // Publish event after successful save
        rabbitMQ.Publish(
            new OrderCreatedEvent(order.Id, order.CustomerId, order.TotalAmount),
            routingKey: "order.created");
    }
}

public record OrderCreatedEvent(Guid OrderId, Guid CustomerId, decimal TotalAmount);
```

**Typed Consumer**:
```csharp
public class OrderCreatedConsumer(
    IEmailService emailService,
    ILogger<OrderCreatedConsumer> logger)
    : IMessageConsumerAsync<OrderCreatedEvent>
{
    public string QueueName => "notifications.order-created";
    public string RoutingKey => "order.created";

    public async Task ConsumeAsync(
        OrderCreatedEvent message,
        ConsumeContext context)
    {
        logger.LogInformation("Processing order created: {OrderId}", message.OrderId);

        await emailService.SendOrderConfirmationAsync(
            message.CustomerId,
            message.OrderId);

        logger.LogInformation("Order confirmation sent: {OrderId}", message.OrderId);
    }
}
```

---

### Pattern 2: Request/Response (Async RPC)

**Setup**:
```csharp
// Request client registration
builder.Services.AddRequestClient<GetOrderRequest, GetOrderResponse>(request =>
{
    request.Exchange = "orders";
    request.RoutingKey = "orders.get";
    request.TimeoutMilliseconds = 5000;
});

// Response consumer registration
builder.Services.AddResponseConsumer<GetOrderRequest, GetOrderResponse, GetOrderConsumer>();
```

**Request/Response**:
```csharp
public record GetOrderRequest(Guid OrderId);
public record GetOrderResponse(Guid OrderId, decimal TotalAmount, string Status);

// Requestor (Service A)
public class OrderQueryService(IRequestClient<GetOrderRequest, GetOrderResponse> requestClient)
{
    public async Task<GetOrderResponse?> GetOrderDetailsAsync(
        Guid orderId,
        CancellationToken ct)
    {
        try
        {
            var response = await requestClient.RequestAsync(
                new GetOrderRequest(orderId),
                ct);
            return response;
        }
        catch (TimeoutException)
        {
            return null;
        }
    }
}

// Responder (Service B)
public class GetOrderConsumer(IRepositoryAsync<Order> repository)
    : IMessageConsumerAsync<GetOrderRequest>
{
    public string QueueName => "orders.get-request";
    public string RoutingKey => "orders.get";

    public async Task ConsumeAsync(
        GetOrderRequest message,
        ConsumeContext context)
    {
        var order = await repository.GetByIdAsync(message.OrderId);

        var response = order is not null
            ? new GetOrderResponse(order.Id, order.TotalAmount, order.Status.ToString())
            : null;

        // Reply to sender
        context.Reply(response);
    }
}
```

---

### Pattern 3: Outbox Pattern (Reliable Publishing)

**Outbox Table**:
```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
```

**Save to Outbox**:
```csharp
public class OrderService(
    IRepositoryAsync<Order> orderRepository,
    IRepositoryAsync<OutboxMessage> outboxRepository,
    IUnitOfWorkAsync unitOfWork)
{
    public async Task CreateOrderAsync(
        CreateOrderCommand command,
        CancellationToken ct)
    {
        var order = Order.Create(command.CustomerId, command.Items);
        
        // Create outbox message
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(OrderCreatedEvent),
            Payload = JsonSerializer.Serialize(
                new OrderCreatedEvent(order.Id, order.CustomerId, order.TotalAmount)),
            CreatedAt = DateTime.UtcNow
        };

        // Save both in same transaction
        orderRepository.Add(order);
        outboxRepository.Add(outboxMessage);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
```

**Outbox Processor**:
```csharp
public class OutboxProcessorService(
    IRepositoryAsync<OutboxMessage> outboxRepository,
    IMvpRabbitMQClient rabbitMQ,
    ILogger<OutboxProcessorService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingMessages = await outboxRepository.GetAsync(
                m => m.ProcessedAt == null,
                take: 100,
                ct: stoppingToken);

            foreach (var message in pendingMessages)
            {
                try
                {
                    // Publish to RabbitMQ
                    var eventData = JsonSerializer.Deserialize<object>(message.Payload);
                    rabbitMQ.Publish(eventData, message.EventType.ToLower());

                    // Mark as processed
                    message.ProcessedAt = DateTime.UtcNow;
                    outboxRepository.Update(message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process outbox message {Id}", message.Id);
                }
            }

            await outboxRepository.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

---

## Anti-Patterns & Pitfalls

### 1. No Outbox Pattern (Lost Messages)

**❌ WRONG**:
```csharp
// Database save and publish not atomic
public async Task CreateOrderAsync(CreateOrderCommand command)
{
    repository.Add(order);
    await unitOfWork.SaveChangesAsync(); // Success
    
    rabbitMQ.Publish(orderCreatedEvent); // If this fails, event lost!
}
```

**✅ CORRECT**:
```csharp
// Use outbox pattern for atomicity
public async Task CreateOrderAsync(CreateOrderCommand command, CancellationToken ct)
{
    repository.Add(order);
    outboxRepository.Add(outboxMessage);
    await unitOfWork.SaveChangesAsync(ct); // Both saved or both rolled back
    
    // Separate process publishes from outbox
}
```

---

### 2. Non-Idempotent Consumers

**❌ WRONG**:
```csharp
public async Task ConsumeAsync(OrderCreatedEvent message, ConsumeContext context)
{
    // No check if already processed
    await _emailService.SendOrderConfirmationAsync(message.OrderId);
    // Message redelivery sends duplicate emails!
}
```

**✅ CORRECT**:
```csharp
public async Task ConsumeAsync(OrderCreatedEvent message, ConsumeContext context)
{
    // Check if already processed (inbox pattern)
    var exists = await _inboxRepository.ExistsAsync(
        i => i.MessageId == context.MessageId);
    
    if (exists)
        return; // Already processed

    await _emailService.SendOrderConfirmationAsync(message.OrderId);
    
    // Mark as processed
    await _inboxRepository.AddAsync(new InboxMessage
    {
        MessageId = context.MessageId,
        ProcessedAt = DateTime.UtcNow
    });
}
```

---

### 3. Blocking Operations in Consumers

**❌ WRONG**:
```csharp
public async Task ConsumeAsync(OrderCreatedEvent message, ConsumeContext context)
{
    // Blocking synchronous call
    Thread.Sleep(10000); // Blocks consumer thread!
    
    // Heavy computation
    var result = ExpensiveOperation(); // Blocks queue processing
}
```

**✅ CORRECT**:
```csharp
public async Task ConsumeAsync(OrderCreatedEvent message, ConsumeContext context)
{
    // Use async properly
    await Task.Delay(10000, context.CancellationToken);
    
    // Offload heavy work
    await _backgroundJobQueue.EnqueueAsync(
        () => ExpensiveOperation(),
        context.CancellationToken);
}
```

---

## Testing Strategy

```csharp
public class OrderCreatedConsumerTests
{
    [Fact]
    public async Task ConsumeAsync_ValidMessage_SendsEmail()
    {
        // Arrange
        var emailService = Substitute.For<IEmailService>();
        var consumer = new OrderCreatedConsumer(emailService, Substitute.For<ILogger<OrderCreatedConsumer>>());
        
        var message = new OrderCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var context = Substitute.For<ConsumeContext>();

        // Act
        await consumer.ConsumeAsync(message, context);

        // Assert
        await emailService.Received(1).SendOrderConfirmationAsync(
            message.CustomerId,
            message.OrderId);
    }
}
```

---

## Best Practices Checklist

- [ ] Use outbox pattern for reliable event publishing
- [ ] Implement inbox pattern for idempotent consumers
- [ ] Configure dead letter queues for failed messages
- [ ] Use typed consumers with `IMessageConsumerAsync<T>`
- [ ] Set appropriate message TTL and queue limits
- [ ] Implement proper error handling and retry logic
- [ ] Monitor queue depths and consumer lag
- [ ] Use topic exchanges for flexible routing
- [ ] Test consumers independently with mocks
- [ ] Document message contracts and routing keys

---

## MCP Workflow Examples

```bash
# Get messaging documentation
get_doc "path": "docs/en-us/broker.md"
get_doc "path": "docs/en-us/broker-advanced.md"

# Explore samples
get_sample_tree "sampleId": "simple-rabbitmq-customer-api"
get_sample_tree "sampleId": "complex-event-driven-rabbitmq-customer-api"
```

---

## Samples (MCP `list_samples`)

Never infer tier from the sample id prefix. Event-driven is **Blueprint**; saga is **Capability**.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `simple-rabbitmq-customer-api` | Simple | Publish/consume baseline |
| `complex-event-driven-rabbitmq-customer-api` | Blueprint | Outbox/inbox event-driven |
| `complex-saga-rabbitmq-customer-api` | Capability | Saga orchestration |

---

## Further Resources

### Related Skills
- `rabbitmq-advanced-specialist.md` - Advanced RabbitMQ patterns
- `saga-orchestration-specialist.md` - Distributed transactions
- `event-driven-specialist.md` - Event-driven architecture
- `cqrs-architect.md` - CQRS with messaging integration

### NuGet Packages
- **Mvp24Hours.Infrastructure.RabbitMQ** - RabbitMQ integration
- **RabbitMQ.Client** - RabbitMQ .NET client

---

**Version**: Mvp24Hours 10.8.0+ (.NET 10)  
**Last Updated**: January 2025  
**Maintained By**: Mvp24Hours Community
