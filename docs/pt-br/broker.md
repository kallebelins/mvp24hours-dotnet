# Message Broker
>Corretor de mensagens (ou Message Broker) é um software que permite que aplicações, sistemas e serviços comuniquem entre si e troquem informações. [O que é um Corretor de Mensagens ( Message Broker ) ?](https://medium.com/@bookgrahms/o-que-%C3%A9-um-corretor-de-mensagens-message-broker-c9fbe219443b)

## RabbitMQ

### Instalação
```csharp
/// Package Manager Console >
Install-Package Mvp24Hours.Infrastructure.RabbitMQ -Version 9.1.x
```

> 📚 Para recursos avançados (Consumers Tipados, Request/Response, Scheduling, Sagas), veja [RabbitMQ Avançado](broker-advanced.md).

### Configuração Básica
Basicamente podemos registrar uma conexão com RabbitMQ levando em consideração todos os consumers de um projeto (assembly), execução assíncrona e retentativas caso ocorram falhas.

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

### Configuração Básica com Lista
Semelhante ao item anterior, com exceção dos itens registrados. Neste ponto, você poderá registrar consumers específicos para uma configuração exclusiva.

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

### Configuração Avançada
Caso seu projeto tenha a necessidade de executar passos de recuperação (SAGA) após falhas previstas ou não, você pode usar Dead Letter Queue configurando assim:

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

### Implementação de Producer

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

### Implementação de Consumer

```csharp
/// CustomerConsumer.cs
public class CustomerConsumer : IMvpRabbitMQConsumerAsync
{
    public string RoutingKey => typeof(CustomerDto).Name;

    public string QueueName => typeof(CustomerDto).Name;

    public async Task ReceivedAsync(object message, string token)
    {
        // tome uma atitude
        await Task.CompletedTask;
    }
}
```

### Implementação de Consumer com Recovery

```csharp
/// CustomerRecoveryConsumer.cs
public class CustomerConsumer : IMvpRabbitMQConsumerRecoveryAsync
{
    public string RoutingKey => typeof(CustomerDto).Name;

    public string QueueName => typeof(CustomerDto).Name;

    public async Task ReceivedAsync(object message, string token)
    {
        // tome uma atitude
        await Task.CompletedTask;
    }

    public async Task FailureAsync(Exception exception, string token)
    {
        // execute o tratamento de falhas de integração no RabbitMQ
        // escrever em uma tabela temporária, enviar e-mail, criar log especializado, etc.
        await Task.CompletedTask;
    }

    public async Task RejectedAsync(object message, string token)
    {
        // tentamos consumir o recurso por 3 vezes, neste caso como não tratamos, vamos desconsiderar
        // escrever em uma tabela temporária, enviar e-mail, criar log especializado, etc.
        await Task.CompletedTask;
    }
}
```

### Executando Consumidores

```csharp
/// HostService.cs
var source = new CancellationTokenSource(TimeSpan.FromSeconds(5));
while (!source.IsCancellationRequested)
{
    var client = serviceProvider.GetService<MvpRabbitMQClient>();
    client.Consume();
}

```

### Usando Docker
```
// Command
docker run -d --name my-rabbit -p 5672:5672 -p 5673:5673 -p 15672:15672 rabbitmq:3-management

// Connect
[127.0.0.1:6379](amqp://guest:guest@localhost:5672)

```

### Injeção vs Instância Padrão
É criada uma instância dinamicamente, com exceção das registradas na coleção de serviços para provedor (IServiceProvider).

---

## Consulte Também

- [Funcionalidades Avançadas do RabbitMQ](broker-advanced.md) - Consumers tipados, Request/Response, Sagas, Scheduling
- [Eventos de Integração CQRS](cqrs/events/integration-events.md) - Usando RabbitMQ com CQRS
- [Observabilidade de Mensagens](observability/messaging.md) - OpenTelemetry para rastreamento de mensagens