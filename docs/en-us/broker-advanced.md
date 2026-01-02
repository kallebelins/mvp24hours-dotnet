# RabbitMQ Advanced Features

> The Mvp24Hours.Infrastructure.RabbitMQ module provides advanced messaging features including Saga pattern, message scheduling, batch processing, request/response, multi-tenancy, and comprehensive observability.

## Installation

```bash
Install-Package Mvp24Hours.Infrastructure.RabbitMQ -Version 9.1.x
```

## Table of Contents

- [Saga Pattern](#saga-pattern)
- [Message Scheduling](#message-scheduling)
- [Batch Processing](#batch-processing)
- [Request/Response Pattern](#requestresponse-pattern)
- [Multi-Tenancy](#multi-tenancy)
- [Pipeline Filters](#pipeline-filters)
- [Observability](#observability)
- [Health Checks](#health-checks)
- [Testing](#testing)

---

## Saga Pattern

Implement long-running business processes with compensating transactions.

### Define a Saga State Machine

```csharp
public class OrderSagaState
{
    public Guid OrderId { get; set; }
    public Guid CorrelationId { get; set; }
    public string CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderSagaStatus Status { get; set; }
    public List<string> CompensationHistory { get; set; } = new();
}

public enum OrderSagaStatus
{
    Started,
    PaymentProcessing,
    PaymentCompleted,
    InventoryReserving,
    InventoryReserved,
    ShippingScheduling,
    Completed,
    Failed,
    Compensating,
    Compensated
}
```

### Create the State Machine

```csharp
public class OrderSaga : SagaStateMachine<OrderSagaState>
{
    public OrderSaga()
    {
        // Define states
        Initially(
            When(OrderCreated)
                .Then(context => context.Instance.Status = OrderSagaStatus.Started)
                .TransitionTo(PaymentProcessing)
                .Publish(context => new ProcessPaymentCommand
                {
                    OrderId = context.Instance.OrderId,
                    Amount = context.Instance.TotalAmount
                })
        );

        During(PaymentProcessing,
            When(PaymentCompleted)
                .Then(context => context.Instance.Status = OrderSagaStatus.PaymentCompleted)
                .TransitionTo(InventoryReserving)
                .Publish(context => new ReserveInventoryCommand
                {
                    OrderId = context.Instance.OrderId
                }),
            When(PaymentFailed)
                .TransitionTo(Failed)
                .Finalize()
        );

        During(InventoryReserving,
            When(InventoryReserved)
                .Then(context => context.Instance.Status = OrderSagaStatus.Completed)
                .TransitionTo(Completed)
                .Finalize(),
            When(InventoryFailed)
                .TransitionTo(Compensating)
                .Publish(context => new RefundPaymentCommand
                {
                    OrderId = context.Instance.OrderId,
                    Amount = context.Instance.TotalAmount
                })
        );

        During(Compensating,
            When(PaymentRefunded)
                .Then(context => context.Instance.Status = OrderSagaStatus.Compensated)
                .TransitionTo(Compensated)
                .Finalize()
        );
    }

    // Event definitions
    public Event<OrderCreatedEvent> OrderCreated { get; private set; }
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; }
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; }
    public Event<InventoryReservedEvent> InventoryReserved { get; private set; }
    public Event<InventoryFailedEvent> InventoryFailed { get; private set; }
    public Event<PaymentRefundedEvent> PaymentRefunded { get; private set; }

    // State definitions
    public State PaymentProcessing { get; private set; }
    public State InventoryReserving { get; private set; }
    public State Completed { get; private set; }
    public State Failed { get; private set; }
    public State Compensating { get; private set; }
    public State Compensated { get; private set; }
}
```

### Register Saga

```csharp
services.AddMvp24HoursRabbitMQSaga<OrderSaga, OrderSagaState>(options =>
{
    options.PersistenceType = SagaPersistenceType.InMemory;  // or Redis, Database
    options.TimeoutMinutes = 30;
    options.RetryOnFailure = true;
});
```

### CQRS Integration

```csharp
services.AddMvp24HoursCqrsSagaIntegration(options =>
{
    options.PublishDomainEventsToSaga = true;
    options.CorrelationIdHeader = "X-Correlation-Id";
});
```

---

## Message Scheduling

Schedule messages for future delivery.

### Setup

```csharp
services.AddMvp24HoursMessageScheduler(options =>
{
    options.StoreType = ScheduledMessageStoreType.InMemory;  // or Redis
    options.PollingIntervalSeconds = 10;
});
```

### Schedule a Message

```csharp
var scheduler = serviceProvider.GetRequiredService<IMessageScheduler>();

// Schedule for a specific time
await scheduler.ScheduleAsync(
    new ReminderMessage { UserId = userId, Message = "Don't forget!" },
    scheduledTime: DateTime.UtcNow.AddHours(24)
);

// Schedule with delay
await scheduler.ScheduleAsync(
    new WelcomeEmail { UserId = userId },
    delay: TimeSpan.FromMinutes(30)
);

// Schedule recurring with cron expression
await scheduler.ScheduleRecurringAsync(
    new DailyReportMessage(),
    cronExpression: "0 8 * * *",  // Every day at 8 AM
    endTime: DateTime.UtcNow.AddMonths(12)
);
```

### Cancel Scheduled Messages

```csharp
// Cancel by message ID
await scheduler.CancelAsync(messageId);

// Cancel by correlation ID
await scheduler.CancelByCorrelationIdAsync(correlationId);
```

---

## Batch Processing

Process multiple messages efficiently in batches.

### Configure Batch Consumer

```csharp
services.AddMvp24HoursBatchConsumer<OrderMessage, OrderBatchConsumer>(options =>
{
    options.BatchSize = 100;
    options.BatchTimeoutSeconds = 30;
    options.MaxParallelBatches = 4;
    options.ProcessingOrder = BatchProcessingOrder.Sequential;
});
```

### Implement Batch Consumer

```csharp
public class OrderBatchConsumer : IBatchConsumer<OrderMessage>
{
    private readonly IOrderRepository _repository;

    public async Task<BatchMessageResult> ProcessBatchAsync(
        BatchConsumeContext<OrderMessage> context,
        CancellationToken cancellationToken)
    {
        var result = new BatchMessageResult();

        foreach (var item in context.Messages)
        {
            try
            {
                await _repository.ProcessOrderAsync(item.Message);
                result.Acknowledge(item);
            }
            catch (Exception ex)
            {
                result.Reject(item, ex.Message);
            }
        }

        return result;
    }
}
```

### Progress Tracking

```csharp
services.AddMvp24HoursBatchConsumer<OrderMessage, OrderBatchConsumer>(options =>
{
    options.ProgressCallback = (processed, total, elapsed) =>
    {
        Console.WriteLine($"Processed {processed}/{total} in {elapsed}");
    };
});
```

---

## Request/Response Pattern

Synchronous-style communication over messaging.

### Setup

```csharp
services.AddMvp24HoursRequestClient(options =>
{
    options.DefaultTimeoutSeconds = 30;
    options.RetryCount = 3;
});
```

### Make a Request

```csharp
var requestClient = serviceProvider.GetRequiredService<IRequestClient>();

// Send request and wait for response
var response = await requestClient.RequestAsync<GetCustomerRequest, CustomerResponse>(
    new GetCustomerRequest { CustomerId = customerId },
    timeout: TimeSpan.FromSeconds(10)
);

Console.WriteLine($"Customer: {response.Name}");
```

### Implement Response Handler

```csharp
public class GetCustomerRequestHandler : IRequestHandler<GetCustomerRequest, CustomerResponse>
{
    private readonly ICustomerRepository _repository;

    public async Task<CustomerResponse> HandleAsync(
        GetCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.CustomerId);
        return new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        };
    }
}
```

---

## Multi-Tenancy

Tenant-isolated messaging for SaaS applications.

### Setup

```csharp
services.AddMvp24HoursRabbitMQMultiTenancy<HttpHeaderTenantProvider>(options =>
{
    options.TenantHeaderName = "X-Tenant-Id";
    options.IsolationStrategy = TenantIsolationStrategy.VirtualHost;  // or Exchange, Queue
    options.CreateTenantResourcesOnDemand = true;
});
```

### Tenant Provider

```csharp
public class HttpHeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string TenantId => 
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
}
```

### Publish to Tenant

```csharp
// Tenant context is automatically propagated
await _publisher.PublishAsync(new OrderCreatedEvent { OrderId = orderId });

// Or explicitly specify tenant
await _publisher.PublishToTenantAsync(tenantId, new OrderCreatedEvent { OrderId = orderId });
```

### Tenant-Aware Dead Letter Queue

```csharp
services.AddMvp24HoursTenantDeadLetterQueue(options =>
{
    options.CreatePerTenantDLQ = true;
    options.DLQNamingPattern = "{tenant}.dead-letter";
});
```

---

## Pipeline Filters

Add cross-cutting concerns to message processing.

### Built-in Filters

```csharp
services.AddMvp24HoursRabbitMQ(...)
    .AddLoggingFilter()
    .AddRetryFilter(options =>
    {
        options.MaxRetries = 3;
        options.DelayMs = 1000;
        options.ExponentialBackoff = true;
    })
    .AddValidationFilter()
    .AddCircuitBreakerFilter(options =>
    {
        options.FailureThreshold = 5;
        options.DurationSeconds = 30;
    })
    .AddDeduplicationFilter(options =>
    {
        options.DeduplicationWindowMinutes = 60;
        options.StoreType = DeduplicationStoreType.InMemory;
    });
```

### Custom Filter

```csharp
public class AuditFilter : IConsumeFilter
{
    private readonly IAuditService _auditService;

    public async Task<bool> OnConsumeAsync(ConsumeFilterContext context)
    {
        await _auditService.LogMessageReceivedAsync(
            context.MessageType,
            context.CorrelationId,
            context.Timestamp
        );
        
        return true;  // Continue pipeline
    }

    public async Task OnConsumedAsync(ConsumeFilterContext context, bool success)
    {
        await _auditService.LogMessageProcessedAsync(
            context.MessageType,
            context.CorrelationId,
            success,
            context.ProcessingTime
        );
    }
}

// Register
services.AddMvp24HoursRabbitMQ(...)
    .AddFilter<AuditFilter>();
```

### Publish Filters

```csharp
public class CorrelationIdFilter : IPublishFilter
{
    public Task<bool> OnPublishAsync(PublishFilterContext context)
    {
        if (string.IsNullOrEmpty(context.CorrelationId))
        {
            context.CorrelationId = Guid.NewGuid().ToString();
        }
        
        return Task.FromResult(true);
    }
}
```

---

## Observability

### OpenTelemetry Integration

```csharp
services.AddMvp24HoursRabbitMQObservability(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.ActivitySourceName = "Mvp24Hours.RabbitMQ";
    options.PropagateCorrelationId = true;
});

// Configure OpenTelemetry
services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddSource("Mvp24Hours.RabbitMQ")
        .AddJaegerExporter());
```

### Prometheus Metrics

```csharp
services.AddMvp24HoursRabbitMQPrometheusMetrics(options =>
{
    options.MetricPrefix = "rabbitmq_";
    options.EnableConnectionMetrics = true;
    options.EnableChannelMetrics = true;
    options.EnableQueueMetrics = true;
});

// Available metrics:
// - rabbitmq_messages_published_total
// - rabbitmq_messages_consumed_total
// - rabbitmq_messages_failed_total
// - rabbitmq_message_processing_duration_seconds
// - rabbitmq_connection_pool_size
// - rabbitmq_channel_pool_size
```

### Structured Logging

```csharp
services.AddMvp24HoursRabbitMQStructuredLogging(options =>
{
    options.LogMessagePayload = false;  // true only in development
    options.LogLevel = LogLevel.Information;
    options.IncludeHeaders = true;
    options.IncludeRoutingKey = true;
});
```

### Get Metrics Snapshot

```csharp
var metrics = serviceProvider.GetRequiredService<IRabbitMQMetrics>();
var snapshot = metrics.GetSnapshot();

Console.WriteLine($"Published: {snapshot.TotalPublished}");
Console.WriteLine($"Consumed: {snapshot.TotalConsumed}");
Console.WriteLine($"Failed: {snapshot.TotalFailed}");
Console.WriteLine($"Avg Processing Time: {snapshot.AverageProcessingTimeMs}ms");
```

---

## Health Checks

### Setup

```csharp
services.AddHealthChecks()
    .AddMvp24HoursRabbitMQCheck("rabbitmq", options =>
    {
        options.ConnectionString = connectionString;
        options.HealthCheckTimeoutSeconds = 5;
        options.CheckQueues = new[] { "orders", "payments" };
    });
```

### Advanced Health Check

```csharp
services.AddHealthChecks()
    .AddMvp24HoursRabbitMQCheck("rabbitmq", options =>
    {
        options.CheckConnectionPool = true;
        options.CheckChannelPool = true;
        options.MaxAllowedQueueLength = 10000;
        options.DegradedThreshold = 5000;
    });
```

---

## Testing

### Test Harness

```csharp
public class OrderConsumerTests
{
    [Fact]
    public async Task OrderCreated_ShouldProcessSuccessfully()
    {
        // Arrange
        var harness = new RabbitMQTestHarness();
        harness.AddConsumer<OrderConsumer>();
        
        await harness.StartAsync();

        // Act
        await harness.PublishAsync(new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = "test@example.com"
        });

        // Assert
        var consumed = await harness.WaitForConsumeAsync<OrderCreatedEvent>(
            timeout: TimeSpan.FromSeconds(5)
        );
        
        Assert.True(consumed.Success);
        Assert.NotNull(consumed.Message);

        await harness.StopAsync();
    }

    [Fact]
    public async Task OrderCreated_WhenFails_ShouldRetry()
    {
        // Arrange
        var harness = new RabbitMQTestHarness();
        var failCount = 0;
        
        harness.AddConsumer<OrderConsumer>(consumer =>
        {
            consumer.OnConsume = ctx =>
            {
                if (++failCount < 3)
                    throw new Exception("Transient error");
            };
        });
        
        await harness.StartAsync();

        // Act
        await harness.PublishAsync(new OrderCreatedEvent { OrderId = Guid.NewGuid() });
        await Task.Delay(5000);  // Wait for retries

        // Assert
        Assert.Equal(3, failCount);  // Retried 3 times
    }
}
```

### In-Memory Bus

```csharp
public class IntegrationTests
{
    [Fact]
    public async Task EndToEnd_OrderFlow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMvp24HoursRabbitMQInMemory();  // Use in-memory bus
        services.AddTransient<OrderService>();
        
        var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IInMemoryBus>();
        var orderService = provider.GetRequiredService<OrderService>();

        var receivedMessages = new List<object>();
        bus.OnMessagePublished += (msg, type) => receivedMessages.Add(msg);

        // Act
        await orderService.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerEmail = "test@example.com",
            Items = new[] { new OrderItem { ProductId = 1, Quantity = 2 } }
        });

        // Assert
        Assert.Single(receivedMessages);
        Assert.IsType<OrderCreatedEvent>(receivedMessages[0]);
    }
}
```

### Testcontainers

```csharp
public class RabbitMQIntegrationTests : IAsyncLifetime
{
    private RabbitMqContainer _container;
    private IServiceProvider _serviceProvider;

    public async Task InitializeAsync()
    {
        _container = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .Build();
        
        await _container.StartAsync();
        
        var services = new ServiceCollection();
        services.AddMvp24HoursRabbitMQ(
            typeof(OrderConsumer).Assembly,
            options =>
            {
                options.ConnectionString = _container.GetConnectionString();
            }
        );
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

---

## Fluent Configuration

### Complete Example

```csharp
services.AddMvp24HoursRabbitMQ()
    .WithConnection(conn =>
    {
        conn.HostName = "localhost";
        conn.Port = 5672;
        conn.UserName = "guest";
        conn.Password = "guest";
        conn.VirtualHost = "/";
        conn.AutomaticRecoveryEnabled = true;
        conn.RequestedHeartbeat = TimeSpan.FromSeconds(60);
    })
    .WithExchange(exchange =>
    {
        exchange.Name = "myapp.events";
        exchange.Type = ExchangeType.Topic;
        exchange.Durable = true;
        exchange.AutoDelete = false;
    })
    .WithQueue(queue =>
    {
        queue.Name = "myapp.orders";
        queue.Durable = true;
        queue.Exclusive = false;
        queue.AutoDelete = false;
        queue.Arguments = new Dictionary<string, object>
        {
            { "x-queue-type", "quorum" },
            { "x-message-ttl", 86400000 }
        };
    })
    .WithDeadLetter(dlx =>
    {
        dlx.Exchange = "myapp.dlx";
        dlx.Queue = "myapp.dlq";
        dlx.RoutingKey = "dead-letter";
    })
    .WithConsumer<OrderConsumer>(consumer =>
    {
        consumer.PrefetchCount = 10;
        consumer.AutoAck = false;
    })
    .WithRetry(retry =>
    {
        retry.MaxRetries = 5;
        retry.InitialDelayMs = 1000;
        retry.MaxDelayMs = 30000;
        retry.ExponentialBackoff = true;
    })
    .Build();
```

---

## See Also

- [Basic RabbitMQ Configuration](broker.md)
- [CQRS RabbitMQ Integration](cqrs/integration-rabbitmq.md)
- [Outbox Pattern](cqrs/resilience/inbox-outbox.md)
- [Integration Events](cqrs/integration-events.md)

