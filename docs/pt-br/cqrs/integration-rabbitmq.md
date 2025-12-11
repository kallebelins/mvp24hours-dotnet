# Integration Events via RabbitMQ

## Visão Geral

O Mediator integra-se com o `Mvp24Hours.Infrastructure.RabbitMQ` existente para publicar e consumir Integration Events entre bounded contexts.

## Configuração

### Instalação

```bash
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ
dotnet add package Mvp24Hours.Infrastructure.Cqrs
```

### Registro de Serviços

```csharp
// Configurar RabbitMQ
services.AddMvpRabbitMQ(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672";
    options.Exchange = "myapp.events";
    options.ExchangeType = ExchangeType.Topic;
    options.QueueName = "myapp.orders";
});

// Registrar publisher
services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

// Registrar outbox (opcional mas recomendado)
services.AddScoped<IIntegrationEventOutbox, InMemoryIntegrationEventOutbox>();

// Registrar Mediator
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});
```

## Publicando Integration Events

### Event Definition

```csharp
public record OrderCreatedIntegrationEvent : IntegrationEventBase
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
}
```

### Publicação Direta

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IIntegrationEventPublisher _publisher;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail, request.Items);
        await _repository.AddAsync(order);

        // Publicar evento diretamente
        await _publisher.PublishAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        }, cancellationToken);

        return OrderDto.FromEntity(order);
    }
}
```

### Publicação via Outbox (Recomendado)

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IIntegrationEventOutbox _outbox;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail, request.Items);
        await _repository.AddAsync(order);

        // Adicionar ao outbox (mesma transação)
        await _outbox.AddAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount
        }, cancellationToken);

        // Salvar tudo atomicamente
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderDto.FromEntity(order);
    }
}
```

## Processador de Outbox

```csharp
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var messages = await outbox.GetPendingAsync(100, cancellationToken);
        
        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishFromOutboxAsync(message, cancellationToken);
                await outbox.MarkAsPublishedAsync(message.Id, cancellationToken);
                
                _logger.LogInformation("Published message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message {MessageId}", message.Id);
                await outbox.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}

// Registro
services.AddHostedService<OutboxProcessor>();
```

## Consumindo Integration Events

### Consumer Handler

```csharp
public class OrderCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderCreatedIntegrationEventHandler> _logger;

    public async Task HandleAsync(
        OrderCreatedIntegrationEvent @event, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderCreated event for order {OrderId}",
            @event.OrderId);

        // Reservar estoque
        foreach (var item in @event.Items)
        {
            await _inventoryService.ReserveAsync(
                item.ProductId, 
                item.Quantity, 
                cancellationToken);
        }

        // Enviar notificação
        await _notificationService.SendAsync(
            @event.CustomerEmail,
            "Order Confirmed",
            $"Your order {@event.OrderId} has been confirmed.",
            cancellationToken);
    }
}
```

### Configuração do Consumer

```csharp
// No startup
services.AddMvpRabbitMQConsumer<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>(
    options =>
    {
        options.QueueName = "inventory.order-created";
        options.RoutingKey = "orders.created";
        options.AutoAck = false;
        options.PrefetchCount = 10;
    });
```

## Routing Keys

### Convenção de Nomenclatura

```
{bounded-context}.{aggregate}.{event}

Exemplos:
- orders.order.created
- orders.order.cancelled
- inventory.stock.reserved
- payments.payment.processed
```

### Configuração de Routing

```csharp
services.AddMvpRabbitMQ(options =>
{
    options.Exchange = "myapp.events";
    options.ExchangeType = ExchangeType.Topic;
});

// Publisher com routing key
await _publisher.PublishAsync(
    @event, 
    routingKey: "orders.order.created",
    cancellationToken);
```

## Tratamento de Erros

### Retry Policy

```csharp
services.AddMvpRabbitMQConsumer<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>(
    options =>
    {
        options.MaxRetryAttempts = 3;
        options.RetryDelayMilliseconds = 1000;
        options.ExponentialBackoff = true;
        options.DeadLetterExchange = "myapp.dlx";
    });
```

### Dead Letter Queue

```csharp
// Mensagens que falharam após todos os retries
// são enviadas para a DLQ para análise
services.AddMvpRabbitMQDeadLetterConsumer(options =>
{
    options.QueueName = "myapp.dlq";
    options.OnDeadLetter = async (message, exception) =>
    {
        _logger.LogError(exception, 
            "Message {MessageId} moved to DLQ", 
            message.Id);
    };
});
```

## Boas Práticas

1. **Outbox Pattern**: Sempre use para garantir consistência
2. **Idempotência**: Handlers devem ser idempotentes
3. **Correlation ID**: Propague para tracing distribuído
4. **Routing Keys**: Use convenção consistente
5. **Dead Letter**: Configure para mensagens problemáticas
6. **Monitoring**: Monitore filas e latência
7. **Serialização**: Use JSON com versionamento de schema

