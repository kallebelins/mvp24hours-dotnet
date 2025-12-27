# RabbitMQ - Funcionalidades Avançadas

> O módulo Mvp24Hours.Infrastructure.RabbitMQ fornece funcionalidades avançadas de mensageria incluindo padrão Saga, agendamento de mensagens, processamento em lote, request/response, multi-tenancy e observabilidade.

## Instalação

```bash
Install-Package Mvp24Hours.Infrastructure.RabbitMQ -Version 8.3.261
```

## Índice

- [Padrão Saga](#padrão-saga)
- [Agendamento de Mensagens](#agendamento-de-mensagens)
- [Processamento em Lote](#processamento-em-lote)
- [Padrão Request/Response](#padrão-requestresponse)
- [Multi-Tenancy](#multi-tenancy)
- [Pipeline de Filtros](#pipeline-de-filtros)
- [Observabilidade](#observabilidade)
- [Health Checks](#health-checks)
- [Testes](#testes)

---

## Padrão Saga

Implemente processos de negócio de longa duração com transações compensatórias.

### Definir Estado da Saga

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

### Criar a Máquina de Estados

```csharp
public class OrderSaga : SagaStateMachine<OrderSagaState>
{
    public OrderSaga()
    {
        // Definir estados
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

    // Definição de eventos
    public Event<OrderCreatedEvent> OrderCreated { get; private set; }
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; }
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; }
    public Event<InventoryReservedEvent> InventoryReserved { get; private set; }
    public Event<InventoryFailedEvent> InventoryFailed { get; private set; }
    public Event<PaymentRefundedEvent> PaymentRefunded { get; private set; }

    // Definição de estados
    public State PaymentProcessing { get; private set; }
    public State InventoryReserving { get; private set; }
    public State Completed { get; private set; }
    public State Failed { get; private set; }
    public State Compensating { get; private set; }
    public State Compensated { get; private set; }
}
```

### Registrar Saga

```csharp
services.AddMvp24HoursRabbitMQSaga<OrderSaga, OrderSagaState>(options =>
{
    options.PersistenceType = SagaPersistenceType.InMemory;  // ou Redis, Database
    options.TimeoutMinutes = 30;
    options.RetryOnFailure = true;
});
```

---

## Agendamento de Mensagens

Agende mensagens para entrega futura.

### Configuração

```csharp
services.AddMvp24HoursMessageScheduler(options =>
{
    options.StoreType = ScheduledMessageStoreType.InMemory;  // ou Redis
    options.PollingIntervalSeconds = 10;
});
```

### Agendar uma Mensagem

```csharp
var scheduler = serviceProvider.GetRequiredService<IMessageScheduler>();

// Agendar para um horário específico
await scheduler.ScheduleAsync(
    new ReminderMessage { UserId = userId, Message = "Não esqueça!" },
    scheduledTime: DateTime.UtcNow.AddHours(24)
);

// Agendar com delay
await scheduler.ScheduleAsync(
    new WelcomeEmail { UserId = userId },
    delay: TimeSpan.FromMinutes(30)
);

// Agendar recorrente com expressão cron
await scheduler.ScheduleRecurringAsync(
    new DailyReportMessage(),
    cronExpression: "0 8 * * *",  // Todo dia às 8h
    endTime: DateTime.UtcNow.AddMonths(12)
);
```

---

## Processamento em Lote

Processe múltiplas mensagens eficientemente em lotes.

### Configurar Consumer de Lote

```csharp
services.AddMvp24HoursBatchConsumer<OrderMessage, OrderBatchConsumer>(options =>
{
    options.BatchSize = 100;
    options.BatchTimeoutSeconds = 30;
    options.MaxParallelBatches = 4;
    options.ProcessingOrder = BatchProcessingOrder.Sequential;
});
```

### Implementar Consumer de Lote

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

---

## Padrão Request/Response

Comunicação estilo síncrono sobre mensageria.

### Configuração

```csharp
services.AddMvp24HoursRequestClient(options =>
{
    options.DefaultTimeoutSeconds = 30;
    options.RetryCount = 3;
});
```

### Fazer uma Requisição

```csharp
var requestClient = serviceProvider.GetRequiredService<IRequestClient>();

// Enviar requisição e aguardar resposta
var response = await requestClient.RequestAsync<GetCustomerRequest, CustomerResponse>(
    new GetCustomerRequest { CustomerId = customerId },
    timeout: TimeSpan.FromSeconds(10)
);

Console.WriteLine($"Cliente: {response.Name}");
```

---

## Multi-Tenancy

Mensageria isolada por tenant para aplicações SaaS.

### Configuração

```csharp
services.AddMvp24HoursRabbitMQMultiTenancy<HttpHeaderTenantProvider>(options =>
{
    options.TenantHeaderName = "X-Tenant-Id";
    options.IsolationStrategy = TenantIsolationStrategy.VirtualHost;  // ou Exchange, Queue
    options.CreateTenantResourcesOnDemand = true;
});
```

### Publicar para Tenant

```csharp
// Contexto do tenant é propagado automaticamente
await _publisher.PublishAsync(new OrderCreatedEvent { OrderId = orderId });

// Ou especificar tenant explicitamente
await _publisher.PublishToTenantAsync(tenantId, new OrderCreatedEvent { OrderId = orderId });
```

---

## Pipeline de Filtros

Adicione funcionalidades transversais ao processamento de mensagens.

### Filtros Integrados

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

### Filtro Customizado

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
        
        return true;  // Continuar pipeline
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

// Registrar
services.AddMvp24HoursRabbitMQ(...)
    .AddFilter<AuditFilter>();
```

---

## Observabilidade

### Integração com OpenTelemetry

```csharp
services.AddMvp24HoursRabbitMQObservability(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.ActivitySourceName = "Mvp24Hours.RabbitMQ";
    options.PropagateCorrelationId = true;
});
```

### Métricas Prometheus

```csharp
services.AddMvp24HoursRabbitMQPrometheusMetrics(options =>
{
    options.MetricPrefix = "rabbitmq_";
    options.EnableConnectionMetrics = true;
    options.EnableChannelMetrics = true;
    options.EnableQueueMetrics = true;
});

// Métricas disponíveis:
// - rabbitmq_messages_published_total
// - rabbitmq_messages_consumed_total
// - rabbitmq_messages_failed_total
// - rabbitmq_message_processing_duration_seconds
```

---

## Health Checks

### Configuração

```csharp
services.AddHealthChecks()
    .AddMvp24HoursRabbitMQCheck("rabbitmq", options =>
    {
        options.ConnectionString = connectionString;
        options.HealthCheckTimeoutSeconds = 5;
        options.CheckQueues = new[] { "orders", "payments" };
    });
```

---

## Testes

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

        await harness.StopAsync();
    }
}
```

### In-Memory Bus

```csharp
var services = new ServiceCollection();
services.AddMvp24HoursRabbitMQInMemory();  // Usar bus in-memory
services.AddTransient<OrderService>();

var provider = services.BuildServiceProvider();
var bus = provider.GetRequiredService<IInMemoryBus>();

var receivedMessages = new List<object>();
bus.OnMessagePublished += (msg, type) => receivedMessages.Add(msg);
```

---

## Veja Também

- [Configuração Básica do RabbitMQ](broker.md)
- [Integração CQRS com RabbitMQ](cqrs/integration-rabbitmq.md)
- [Padrão Outbox](cqrs/resilience/inbox-outbox.md)
- [Eventos de Integração](cqrs/integration-events.md)

