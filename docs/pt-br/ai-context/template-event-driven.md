# Template de Arquitetura Event-Driven

> **Instrução para Agente de IA**: Use este template para sistemas que precisam de acoplamento fraco, escalabilidade e processamento assíncrono. Arquitetura Event-Driven é ideal para sistemas distribuídos e comunicação entre microservices.

---

## Quando Usar Event-Driven

### Cenários Recomendados
- Comunicação entre microservices
- Requisitos de processamento assíncrono
- Sistemas que requerem trilhas de auditoria
- Necessidades de alta escalabilidade
- Acoplamento fraco entre componentes
- Processamento de dados em tempo real

### Não Recomendado
- Aplicações síncronas simples
- Sistemas que requerem consistência imediata
- Pequenas aplicações monolíticas
- Equipes sem experiência com mensageria

---

## Estrutura de Diretórios

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   ├── NomeProjeto.Core.csproj
    │   ├── Entities/
    │   │   └── Cliente.cs
    │   ├── Events/
    │   │   ├── Domain/
    │   │   │   ├── ClienteCriadoEvent.cs
    │   │   │   ├── ClienteAtualizadoEvent.cs
    │   │   │   └── ClienteExcluidoEvent.cs
    │   │   ├── Integration/
    │   │   │   ├── ClienteCriadoIntegrationEvent.cs
    │   │   │   └── PedidoRealizadoIntegrationEvent.cs
    │   │   └── Base/
    │   │       ├── IDomainEvent.cs
    │   │       └── IIntegrationEvent.cs
    │   ├── ValueObjects/
    │   │   └── ClienteDto.cs
    │   └── Contract/
    │       ├── Services/
    │       │   └── IClienteService.cs
    │       └── Events/
    │           ├── IDomainEventDispatcher.cs
    │           └── IIntegrationEventPublisher.cs
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── DataContext.cs
    │   │   └── Configurations/
    │   │       └── ClienteConfiguration.cs
    │   ├── Messaging/
    │   │   ├── RabbitMQ/
    │   │   │   ├── RabbitMQConnection.cs
    │   │   │   ├── RabbitMQPublisher.cs
    │   │   │   └── RabbitMQConsumer.cs
    │   │   └── EventBus/
    │   │       ├── InMemoryEventBus.cs
    │   │       └── RabbitMQEventBus.cs
    │   └── Events/
    │       ├── DomainEventDispatcher.cs
    │       └── IntegrationEventPublisher.cs
    ├── NomeProjeto.Application/
    │   ├── NomeProjeto.Application.csproj
    │   ├── Services/
    │   │   └── ClienteService.cs
    │   ├── EventHandlers/
    │   │   ├── Domain/
    │   │   │   ├── ClienteCriadoEventHandler.cs
    │   │   │   └── ClienteAtualizadoEventHandler.cs
    │   │   └── Integration/
    │   │       ├── PedidoRealizadoEventHandler.cs
    │   │       └── PagamentoConcluidoEventHandler.cs
    │   └── Mappings/
    │       └── ClienteProfile.cs
    └── NomeProjeto.WebAPI/
        ├── NomeProjeto.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── Controllers/
        │   └── ClientesController.cs
        ├── HostedServices/
        │   └── IntegrationEventConsumerService.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

---

## Tipos de Eventos

### Domain Events
- Representam algo que aconteceu dentro do domínio
- Usados para comunicação intra-serviço
- Processados síncrona ou assincronamente dentro do mesmo contexto delimitado

### Integration Events
- Usados para comunicação inter-serviços
- Publicados em broker de mensagens (RabbitMQ, Azure Service Bus, etc.)
- Consumidos por outros serviços

---

## Templates de Eventos

### Interface Base de Eventos

```csharp
// Core/Events/Base/IDomainEvent.cs
namespace NomeProjeto.Core.Events.Base;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

// Core/Events/Base/IIntegrationEvent.cs
namespace NomeProjeto.Core.Events.Base;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
    string CorrelationId { get; }
}
```

### Domain Events

```csharp
// Core/Events/Domain/ClienteCriadoEvent.cs
using NomeProjeto.Core.Events.Base;

namespace NomeProjeto.Core.Events.Domain;

public record ClienteCriadoEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => nameof(ClienteCriadoEvent);
    
    public int ClienteId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
```

### Integration Events

```csharp
// Core/Events/Integration/ClienteCriadoIntegrationEvent.cs
using NomeProjeto.Core.Events.Base;

namespace NomeProjeto.Core.Events.Integration;

public record ClienteCriadoIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string EventType => nameof(ClienteCriadoIntegrationEvent);
    public string CorrelationId { get; init; } = string.Empty;
    
    public int ClienteId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
```

---

## Interfaces de Dispatcher/Publisher

```csharp
// Core/Contract/Events/IDomainEventDispatcher.cs
using NomeProjeto.Core.Events.Base;

namespace NomeProjeto.Core.Contract.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
    
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

// Core/Contract/Events/IIntegrationEventPublisher.cs
using NomeProjeto.Core.Events.Base;

namespace NomeProjeto.Core.Contract.Events;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
    
    Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
```

---

## Handler de Domain Event

```csharp
// Application/EventHandlers/Domain/ClienteCriadoEventHandler.cs
using Microsoft.Extensions.Logging;
using NomeProjeto.Core.Contract.Events;
using NomeProjeto.Core.Events.Domain;
using NomeProjeto.Core.Events.Integration;

namespace NomeProjeto.Application.EventHandlers.Domain;

public class ClienteCriadoEventHandler : IDomainEventHandler<ClienteCriadoEvent>
{
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<ClienteCriadoEventHandler> _logger;

    public ClienteCriadoEventHandler(
        IIntegrationEventPublisher eventPublisher,
        ILogger<ClienteCriadoEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(ClienteCriadoEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processando ClienteCriadoEvent para ClienteId: {ClienteId}", domainEvent.ClienteId);

        // Publicar evento de integração para outros serviços
        var integrationEvent = new ClienteCriadoIntegrationEvent
        {
            ClienteId = domainEvent.ClienteId,
            Nome = domainEvent.Nome,
            Email = domainEvent.Email,
            CorrelationId = domainEvent.EventId.ToString()
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation("ClienteCriadoIntegrationEvent publicado para ClienteId: {ClienteId}", domainEvent.ClienteId);
    }
}
```

---

## Serviço com Domain Events

```csharp
// Application/Services/ClienteService.cs
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Core.Contract.Events;
using NomeProjeto.Core.Contract.Services;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.Events.Domain;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IUnitOfWorkAsync _uow;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ClienteService(IUnitOfWorkAsync uow, IDomainEventDispatcher eventDispatcher)
    {
        _uow = uow;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<IBusinessResult<ClienteDto>> CreateAsync(ClienteCreateDto dto)
    {
        var cliente = new Cliente
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Ativo = true,
            Criado = DateTime.UtcNow
        };

        var repository = _uow.GetRepository<Cliente>();
        await repository.AddAsync(cliente);
        await _uow.SaveChangesAsync();

        // Disparar domain event
        var domainEvent = new ClienteCriadoEvent
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email
        };
        await _eventDispatcher.DispatchAsync(domainEvent);

        var result = new ClienteDto(cliente.Id, cliente.Nome, cliente.Email, cliente.Ativo);
        return result.ToBusiness();
    }
}
```

---

## Consumer de Integration Event (Hosted Service)

```csharp
// WebAPI/HostedServices/IntegrationEventConsumerService.cs
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NomeProjeto.Core.Contract.Events;
using NomeProjeto.Core.Events.Integration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NomeProjeto.WebAPI.HostedServices;

public class IntegrationEventConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventConsumerService> _logger;
    private IModel? _channel;

    public IntegrationEventConsumerService(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<IntegrationEventConsumerService> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        
        var exchangeName = "integration_events";
        var queueName = "cliente_service_queue";

        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "pedido.*");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventType = ea.BasicProperties.Type;

            try
            {
                await ProcessEventAsync(eventType, message, stoppingToken);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento: {EventType}", eventType);
                _channel.BasicNack(ea.DeliveryTag, false, true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private async Task ProcessEventAsync(string eventType, string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        switch (eventType)
        {
            case nameof(PedidoRealizadoIntegrationEvent):
                var orderEvent = JsonSerializer.Deserialize<PedidoRealizadoIntegrationEvent>(message);
                if (orderEvent != null)
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<PedidoRealizadoIntegrationEvent>>();
                    await handler.HandleAsync(orderEvent, cancellationToken);
                }
                break;
            
            default:
                _logger.LogWarning("Tipo de evento desconhecido: {EventType}", eventType);
                break;
        }
    }
}
```

---

## Configuração (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProjectDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## Integração com Mvp24Hours RabbitMQ

Você pode usar o suporte nativo do Mvp24Hours para RabbitMQ:

```csharp
// Usando Mvp24Hours.Infrastructure.RabbitMQ
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;

services.AddMvp24HoursRabbitMQ(options =>
{
    options.ConnectionString = configuration["RabbitMQ:ConnectionString"];
    options.DispatchConsumersAsync = true;
    options.MaxRetryCount = 3;
});
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Template CQRS](template-cqrs.md)
- [Padrões de Mensageria](messaging-patterns.md)
- [Template Microservices](template-microservices.md)

