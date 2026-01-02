# Message Broker
>Message broker is software that allows applications, systems and services to communicate with each other and exchange information. [Message Broker](https://en.wikipedia.org/wiki/Message_broker)

## RabbitMQ

### Setup
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure.RabbitMQ -Version 9.1.x
```

> 📚 For advanced features (Typed Consumers, Request/Response, Scheduling, Sagas), see [RabbitMQ Advanced](broker-advanced.md).

### Basic Settings
Basically, we can register a connection with RabbitMQ taking into account all consumers of a project (assembly), asynchronous execution and retrying if failures occur.

```csharp
/// Startup.cs

services.AddMvp24HoursRabbitMQ(
    typeof(MyClassConsumer).Assembly,
    connectionOptions =>
    {
        connectionOptions.ConnectionString = configuration.GetConnectionString("RabbitMQContext");
        connectionOptions.DispatchConsumersAsync = true;
        connectionOptions.RetryCount = 3;
    }
);

```

### Basic Settings with List
Similar to the previous item, with the exception of registered items. At this point, you can register specific consumers for a unique configuration.

```csharp
/// Startup.cs

services.AddMvp24HoursRabbitMQ(
    new List<Type> { typeof(CustomerConsumer) },
    connectionOptions =>
    {
        connectionOptions.ConnectionString = _rabbitMqContainer.GetConnectionString();
        connectionOptions.DispatchConsumersAsync = true;
        connectionOptions.RetryCount = 3;
    },
    clientOptions =>
    {
        clientOptions.MaxRedeliveredCount = 1;
    }
);

```

### Advanced Setting
If your project needs to perform recovery steps (SAGA) after expected or unforeseen failures, you can use Dead Letter Queue by configuring it like this:

```csharp
/// Startup.cs

services.AddMvp24HoursRabbitMQ(
    typeof(MyClassConsumer).Assembly, // ou lista
    connectionOptions =>
    {
        connectionOptions.ConnectionString = configuration.GetConnectionString("RabbitMQContext");
        connectionOptions.DispatchConsumersAsync = true;
        connectionOptions.RetryCount = 3;
    },
    clientOptions =>
    {
        clientOptions.Exchange = "customer.direct";
        clientOptions.MaxRedeliveredCount = 1;
        clientOptions.QueueArguments = new System.Collections.Generic.Dictionary<string, object>
        {
            { "x-queue-mode", "lazy" },
            { "x-dead-letter-exchange", "dead-letter-customer.direct" }
        };

        // dead letter exchanges enabled
        clientOptions.DeadLetter = new RabbitMQOptions()
        {
            Exchange = "dead-letter-customer.direct",
            QueueArguments = new System.Collections.Generic.Dictionary<string, object>
            {
                { "x-queue-mode", "lazy" }
            }
        };
    }
);

```

### Producer Implementation

```csharp
/// CustomerService.cs
var client = serviceProvider.GetService<MvpRabbitMQClient>();
client.Publish(new CustomerDto
{
    Id = 99,
    Name = "Test 1",
    Active = true
}, typeof(CustomerDto).Name);

```

### Consumer Implementation

```csharp
/// CustomerConsumer.cs
public class CustomerConsumer : IMvpRabbitMQConsumerAsync
{
    public string RoutingKey => typeof(CustomerDto).Name;

    public string QueueName => typeof(CustomerDto).Name;

    public async Task ReceivedAsync(object message, string token)
    {
        // take action
        await Task.CompletedTask;
    }
}
```

### Implementation of Consumer with Recovery

```csharp
/// CustomerRecoveryConsumer.cs
public class CustomerConsumer : IMvpRabbitMQConsumerRecoveryAsync
{
    public string RoutingKey => typeof(CustomerDto).Name;

    public string QueueName => typeof(CustomerDto).Name;

    public async Task ReceivedAsync(object message, string token)
    {
        // take action
        await Task.CompletedTask;
    }

    public async Task FailureAsync(Exception exception, string token)
    {
        // perform integration failure handling in RabbitMQ
        // write to a temporary table, send email, create specialized log, etc.
        await Task.CompletedTask;
    }

    public async Task RejectedAsync(object message, string token)
    {
        // we tried to consume the resource 3 times, in this case as we did not treat it, we will disregard it
        // write to a temporary table, send email, create specialized log, etc.
        await Task.CompletedTask;
    }
}
```

### Running Consumers

```csharp
/// HostService.cs
var source = new CancellationTokenSource(TimeSpan.FromSeconds(5));
while (!source.IsCancellationRequested)
{
    var client = serviceProvider.GetService<MvpRabbitMQClient>();
    client.Consume();
}

```

### Using Docker
```
// Command
docker run -d --name my-rabbit -p 5672:5672 -p 5673:5673 -p 15672:15672 rabbitmq:3-management

// Connect
[127.0.0.1:6379](amqp://guest:guest@localhost:5672)

```

### Injection vs Standard Instance
An instance is created dynamically, with the exception of those registered in the service collection for the provider (IServiceProvider).

---

## See Also

- [RabbitMQ Advanced Features](broker-advanced.md) - Typed consumers, Request/Response, Sagas, Scheduling
- [CQRS Integration Events](cqrs/events/integration-events.md) - Using RabbitMQ with CQRS
- [Message Observability](observability/messaging.md) - OpenTelemetry for message tracing