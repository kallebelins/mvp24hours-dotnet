# Event-Driven Architecture Template

> **AI Agent Instruction**: Use this template for systems that need loose coupling, scalability, and asynchronous processing. Event-Driven Architecture is ideal for distributed systems and microservices communication.

---

## When to Use Event-Driven

### Recommended Scenarios
- Microservices communication
- Asynchronous processing requirements
- Systems requiring audit trails
- High scalability needs
- Loose coupling between components
- Real-time data processing

### Not Recommended
- Simple synchronous applications
- Systems requiring immediate consistency
- Small monolithic applications
- Teams without messaging experience

---

## Directory Structure

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   ├── ProjectName.Core.csproj
    │   ├── Entities/
    │   │   └── Customer.cs
    │   ├── Events/
    │   │   ├── Domain/
    │   │   │   ├── CustomerCreatedEvent.cs
    │   │   │   ├── CustomerUpdatedEvent.cs
    │   │   │   └── CustomerDeletedEvent.cs
    │   │   ├── Integration/
    │   │   │   ├── CustomerCreatedIntegrationEvent.cs
    │   │   │   └── OrderPlacedIntegrationEvent.cs
    │   │   └── Base/
    │   │       ├── IDomainEvent.cs
    │   │       └── IIntegrationEvent.cs
    │   ├── ValueObjects/
    │   │   └── CustomerDto.cs
    │   └── Contract/
    │       ├── Services/
    │       │   └── ICustomerService.cs
    │       └── Events/
    │           ├── IDomainEventDispatcher.cs
    │           └── IIntegrationEventPublisher.cs
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── DataContext.cs
    │   │   └── Configurations/
    │   │       └── CustomerConfiguration.cs
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
    ├── ProjectName.Application/
    │   ├── ProjectName.Application.csproj
    │   ├── Services/
    │   │   └── CustomerService.cs
    │   ├── EventHandlers/
    │   │   ├── Domain/
    │   │   │   ├── CustomerCreatedEventHandler.cs
    │   │   │   └── CustomerUpdatedEventHandler.cs
    │   │   └── Integration/
    │   │       ├── OrderPlacedEventHandler.cs
    │   │       └── PaymentCompletedEventHandler.cs
    │   └── Mappings/
    │       └── CustomerProfile.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── Controllers/
        │   └── CustomersController.cs
        ├── HostedServices/
        │   └── IntegrationEventConsumerService.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

---

## Event Types

### Domain Events
- Represent something that happened within the domain
- Used for intra-service communication
- Processed synchronously or asynchronously within the same bounded context

### Integration Events
- Used for inter-service communication
- Published to message broker (RabbitMQ, Azure Service Bus, etc.)
- Consumed by other services

---

## Namespaces

```csharp
// Core
ProjectName.Core.Entities
ProjectName.Core.Events.Domain
ProjectName.Core.Events.Integration
ProjectName.Core.Events.Base
ProjectName.Core.ValueObjects
ProjectName.Core.Contract.Services
ProjectName.Core.Contract.Events

// Infrastructure
ProjectName.Infrastructure.Data
ProjectName.Infrastructure.Messaging.RabbitMQ
ProjectName.Infrastructure.Messaging.EventBus
ProjectName.Infrastructure.Events

// Application
ProjectName.Application.Services
ProjectName.Application.EventHandlers.Domain
ProjectName.Application.EventHandlers.Integration
ProjectName.Application.Mappings

// WebAPI
ProjectName.WebAPI.Controllers
ProjectName.WebAPI.HostedServices
ProjectName.WebAPI.Extensions
```

---

## Project Files (.csproj)

### Infrastructure Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProjectName.Core\ProjectName.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="9.*" />
    <PackageReference Include="Mvp24Hours.Infrastructure.RabbitMQ" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageReference Include="RabbitMQ.Client" Version="6.*" />
  </ItemGroup>
</Project>
```

---

## Event Base Interfaces

```csharp
// Core/Events/Base/IDomainEvent.cs
namespace ProjectName.Core.Events.Base;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

// Core/Events/Base/IIntegrationEvent.cs
namespace ProjectName.Core.Events.Base;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
    string CorrelationId { get; }
}
```

---

## Domain Event Templates

```csharp
// Core/Events/Domain/CustomerCreatedEvent.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Events.Domain;

public record CustomerCreatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => nameof(CustomerCreatedEvent);
    
    public int CustomerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

// Core/Events/Domain/CustomerUpdatedEvent.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Events.Domain;

public record CustomerUpdatedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => nameof(CustomerUpdatedEvent);
    
    public int CustomerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool Active { get; init; }
}

// Core/Events/Domain/CustomerDeletedEvent.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Events.Domain;

public record CustomerDeletedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType => nameof(CustomerDeletedEvent);
    
    public int CustomerId { get; init; }
}
```

---

## Integration Event Templates

```csharp
// Core/Events/Integration/CustomerCreatedIntegrationEvent.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Events.Integration;

public record CustomerCreatedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string EventType => nameof(CustomerCreatedIntegrationEvent);
    public string CorrelationId { get; init; } = string.Empty;
    
    public int CustomerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

// Core/Events/Integration/OrderPlacedIntegrationEvent.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Events.Integration;

public record OrderPlacedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string EventType => nameof(OrderPlacedIntegrationEvent);
    public string CorrelationId { get; init; } = string.Empty;
    
    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public IList<OrderItemDto> Items { get; init; } = new List<OrderItemDto>();
}

public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
```

---

## Event Dispatcher Interface

```csharp
// Core/Contract/Events/IDomainEventDispatcher.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Contract.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
    
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

// Core/Contract/Events/IIntegrationEventPublisher.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Contract.Events;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
    
    Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
```

---

## Event Handler Interfaces

```csharp
// Core/Contract/Events/IDomainEventHandler.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Contract.Events;

public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

// Core/Contract/Events/IIntegrationEventHandler.cs
using ProjectName.Core.Events.Base;

namespace ProjectName.Core.Contract.Events;

public interface IIntegrationEventHandler<TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
```

---

## Domain Event Dispatcher Implementation

```csharp
// Infrastructure/Events/DomainEventDispatcher.cs
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Core.Contract.Events;
using ProjectName.Core.Events.Base;

namespace ProjectName.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                if (method != null)
                {
                    await (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                }
            }
        }
    }
}
```

---

## Integration Event Publisher (RabbitMQ)

```csharp
// Infrastructure/Events/IntegrationEventPublisher.cs
using System.Text;
using System.Text.Json;
using ProjectName.Core.Contract.Events;
using ProjectName.Core.Events.Base;
using RabbitMQ.Client;

namespace ProjectName.Infrastructure.Events;

public class IntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IConnection _connection;
    private readonly string _exchangeName;

    public IntegrationEventPublisher(IConnection connection, string exchangeName = "integration_events")
    {
        _connection = connection;
        _exchangeName = exchangeName;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        await PublishAsync(integrationEvent, integrationEvent.EventType, cancellationToken);
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, string routingKey, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        using var channel = _connection.CreateModel();
        
        channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false
        );

        var message = JsonSerializer.Serialize(integrationEvent);
        var body = Encoding.UTF8.GetBytes(message);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = integrationEvent.EventId.ToString();
        properties.CorrelationId = integrationEvent.CorrelationId;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.Type = integrationEvent.EventType;

        channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body
        );

        return Task.CompletedTask;
    }
}
```

---

## Domain Event Handler Template

```csharp
// Application/EventHandlers/Domain/CustomerCreatedEventHandler.cs
using Microsoft.Extensions.Logging;
using ProjectName.Core.Contract.Events;
using ProjectName.Core.Events.Domain;
using ProjectName.Core.Events.Integration;

namespace ProjectName.Application.EventHandlers.Domain;

public class CustomerCreatedEventHandler : IDomainEventHandler<CustomerCreatedEvent>
{
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<CustomerCreatedEventHandler> _logger;

    public CustomerCreatedEventHandler(
        IIntegrationEventPublisher eventPublisher,
        ILogger<CustomerCreatedEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CustomerCreatedEvent for CustomerId: {CustomerId}", domainEvent.CustomerId);

        // Publish integration event for other services
        var integrationEvent = new CustomerCreatedIntegrationEvent
        {
            CustomerId = domainEvent.CustomerId,
            Name = domainEvent.Name,
            Email = domainEvent.Email,
            CorrelationId = domainEvent.EventId.ToString()
        };

        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation("CustomerCreatedIntegrationEvent published for CustomerId: {CustomerId}", domainEvent.CustomerId);
    }
}
```

---

## Integration Event Handler Template

```csharp
// Application/EventHandlers/Integration/OrderPlacedEventHandler.cs
using Microsoft.Extensions.Logging;
using ProjectName.Core.Contract.Events;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.Events.Integration;

namespace ProjectName.Application.EventHandlers.Integration;

public class OrderPlacedEventHandler : IIntegrationEventHandler<OrderPlacedIntegrationEvent>
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(
        ICustomerService customerService,
        ILogger<OrderPlacedEventHandler> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderPlacedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling OrderPlacedIntegrationEvent. OrderId: {OrderId}, CustomerId: {CustomerId}",
            integrationEvent.OrderId,
            integrationEvent.CustomerId
        );

        // Update customer statistics, send notifications, etc.
        await _customerService.UpdateOrderStatisticsAsync(
            integrationEvent.CustomerId,
            integrationEvent.TotalAmount,
            cancellationToken
        );

        _logger.LogInformation("OrderPlacedIntegrationEvent handled successfully");
    }
}
```

---

## Service with Domain Events

```csharp
// Application/Services/CustomerService.cs
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Contract.Events;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.Entities;
using ProjectName.Core.Events.Domain;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWorkAsync _uow;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CustomerService(IUnitOfWorkAsync uow, IDomainEventDispatcher eventDispatcher)
    {
        _uow = uow;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            Email = dto.Email,
            Active = true,
            Created = DateTime.UtcNow
        };

        var repository = _uow.GetRepository<Customer>();
        await repository.AddAsync(customer);
        await _uow.SaveChangesAsync();

        // Dispatch domain event
        var domainEvent = new CustomerCreatedEvent
        {
            CustomerId = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        };
        await _eventDispatcher.DispatchAsync(domainEvent);

        var result = new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Active);
        return result.ToBusiness();
    }

    public async Task<IBusinessResult<CustomerDto>> UpdateAsync(int id, CustomerUpdateDto dto)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);

        if (customer == null)
            return default(CustomerDto).ToBusiness("Customer not found");

        customer.Name = dto.Name;
        customer.Email = dto.Email;
        customer.Active = dto.Active;

        await repository.ModifyAsync(customer);
        await _uow.SaveChangesAsync();

        // Dispatch domain event
        var domainEvent = new CustomerUpdatedEvent
        {
            CustomerId = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Active = customer.Active
        };
        await _eventDispatcher.DispatchAsync(domainEvent);

        var result = new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Active);
        return result.ToBusiness();
    }

    public async Task<IBusinessResult<bool>> DeleteAsync(int id)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);

        if (customer == null)
            return false.ToBusiness("Customer not found");

        await repository.RemoveAsync(customer);
        await _uow.SaveChangesAsync();

        // Dispatch domain event
        var domainEvent = new CustomerDeletedEvent { CustomerId = id };
        await _eventDispatcher.DispatchAsync(domainEvent);

        return true.ToBusiness();
    }
}
```

---

## Integration Event Consumer (Hosted Service)

```csharp
// WebAPI/HostedServices/IntegrationEventConsumerService.cs
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectName.Core.Contract.Events;
using ProjectName.Core.Events.Integration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ProjectName.WebAPI.HostedServices;

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
        var queueName = "customer_service_queue";

        _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: "order.*");

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
                _logger.LogError(ex, "Error processing event: {EventType}", eventType);
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
            case nameof(OrderPlacedIntegrationEvent):
                var orderEvent = JsonSerializer.Deserialize<OrderPlacedIntegrationEvent>(message);
                if (orderEvent != null)
                {
                    var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<OrderPlacedIntegrationEvent>>();
                    await handler.HandleAsync(orderEvent, cancellationToken);
                }
                break;
            
            default:
                _logger.LogWarning("Unknown event type: {EventType}", eventType);
                break;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        base.Dispose();
    }
}
```

---

## Service Registration

```csharp
// Extensions/ServiceBuilderExtensions.cs
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using ProjectName.Application.EventHandlers.Domain;
using ProjectName.Application.EventHandlers.Integration;
using ProjectName.Application.Services;
using ProjectName.Core.Contract.Events;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.Events.Domain;
using ProjectName.Core.Events.Integration;
using ProjectName.Infrastructure.Data;
using ProjectName.Infrastructure.Events;
using RabbitMQ.Client;

namespace ProjectName.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddMvp24HoursDbContext<DataContext>();
        services.AddMvp24HoursRepository();

        // RabbitMQ Connection
        services.AddSingleton<IConnection>(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/"
            };
            return factory.CreateConnection();
        });

        // Event Infrastructure
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

        // Domain Event Handlers
        services.AddScoped<IDomainEventHandler<CustomerCreatedEvent>, CustomerCreatedEventHandler>();
        services.AddScoped<IDomainEventHandler<CustomerUpdatedEvent>, CustomerUpdatedEventHandler>();

        // Integration Event Handlers
        services.AddScoped<IIntegrationEventHandler<OrderPlacedIntegrationEvent>, OrderPlacedEventHandler>();

        // Services
        services.AddScoped<ICustomerService, CustomerService>();

        // Hosted Services
        services.AddHostedService<IntegrationEventConsumerService>();

        return services;
    }
}
```

---

## Configuration (appsettings.json)

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

## Integration with Mvp24Hours RabbitMQ

You can use Mvp24Hours built-in RabbitMQ support:

```csharp
// Using Mvp24Hours.Infrastructure.RabbitMQ
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

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [CQRS Template](template-cqrs.md)
- [Messaging Patterns](messaging-patterns.md)
- [Microservices Template](template-microservices.md)

