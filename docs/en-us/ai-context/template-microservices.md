# Microservices Architecture Template

> **AI Agent Instruction**: Use this template for distributed systems requiring independent deployment, scalability, and team autonomy. Each microservice owns its data and communicates through well-defined APIs or messaging.

---

## When to Use Microservices

### Recommended Scenarios
- Large-scale applications with multiple teams
- Systems requiring independent scaling per service
- Polyglot environments (different technologies per service)
- Complex domains with clear bounded contexts
- High availability and fault tolerance requirements

### Not Recommended
- Small applications or startups
- Teams without distributed systems experience
- Projects without clear domain boundaries
- Simple CRUD applications
- Tight deadlines with limited resources

---

## Core Principles

1. **Single Responsibility**: Each service does one thing well
2. **Data Ownership**: Each service owns its database
3. **Independent Deployment**: Services deploy independently
4. **Resilience**: Services handle failures gracefully
5. **Decentralized Governance**: Teams choose their technology
6. **Smart Endpoints, Dumb Pipes**: Logic in services, not middleware

---

## Solution Structure

```
MicroservicesProject/
├── MicroservicesProject.sln
├── src/
│   ├── Services/
│   │   ├── CustomerService/
│   │   │   ├── CustomerService.API/
│   │   │   │   ├── CustomerService.API.csproj
│   │   │   │   ├── Program.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   ├── Controllers/
│   │   │   │   │   └── CustomersController.cs
│   │   │   │   └── appsettings.json
│   │   │   ├── CustomerService.Domain/
│   │   │   │   ├── CustomerService.Domain.csproj
│   │   │   │   ├── Entities/
│   │   │   │   └── Events/
│   │   │   ├── CustomerService.Application/
│   │   │   │   ├── CustomerService.Application.csproj
│   │   │   │   ├── Commands/
│   │   │   │   └── Queries/
│   │   │   └── CustomerService.Infrastructure/
│   │   │       ├── CustomerService.Infrastructure.csproj
│   │   │       └── Persistence/
│   │   ├── OrderService/
│   │   │   ├── OrderService.API/
│   │   │   ├── OrderService.Domain/
│   │   │   ├── OrderService.Application/
│   │   │   └── OrderService.Infrastructure/
│   │   ├── ProductService/
│   │   │   └── ... (same structure)
│   │   └── NotificationService/
│   │       └── ... (same structure)
│   ├── ApiGateway/
│   │   ├── ApiGateway.csproj
│   │   ├── Program.cs
│   │   ├── Dockerfile
│   │   └── ocelot.json
│   └── Shared/
│       ├── Shared.Contracts/
│       │   ├── Shared.Contracts.csproj
│       │   ├── Events/
│       │   │   ├── CustomerCreatedEvent.cs
│       │   │   └── OrderPlacedEvent.cs
│       │   └── DTOs/
│       │       ├── CustomerDto.cs
│       │       └── OrderDto.cs
│       └── Shared.Infrastructure/
│           ├── Shared.Infrastructure.csproj
│           ├── Messaging/
│           │   └── RabbitMQPublisher.cs
│           └── ServiceDiscovery/
│               └── ConsulServiceDiscovery.cs
├── docker-compose.yml
├── docker-compose.override.yml
└── infrastructure/
    ├── kubernetes/
    │   ├── customer-service.yaml
    │   ├── order-service.yaml
    │   └── api-gateway.yaml
    └── terraform/
        └── main.tf
```

---

## Service Structure (Per Service)

```
CustomerService/
├── CustomerService.API/
│   ├── CustomerService.API.csproj
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   └── CustomersController.cs
│   ├── Consumers/
│   │   └── OrderPlacedConsumer.cs
│   ├── HealthChecks/
│   │   └── DatabaseHealthCheck.cs
│   └── Extensions/
│       └── ServiceBuilderExtensions.cs
├── CustomerService.Domain/
│   ├── CustomerService.Domain.csproj
│   ├── Entities/
│   │   └── Customer.cs
│   ├── ValueObjects/
│   │   └── Email.cs
│   ├── Events/
│   │   └── CustomerCreatedEvent.cs
│   └── Repositories/
│       └── ICustomerRepository.cs
├── CustomerService.Application/
│   ├── CustomerService.Application.csproj
│   ├── Commands/
│   │   ├── CreateCustomerCommand.cs
│   │   └── CreateCustomerHandler.cs
│   ├── Queries/
│   │   ├── GetCustomerQuery.cs
│   │   └── GetCustomerHandler.cs
│   ├── DTOs/
│   │   └── CustomerDto.cs
│   └── IntegrationEvents/
│       └── CustomerCreatedIntegrationEvent.cs
└── CustomerService.Infrastructure/
    ├── CustomerService.Infrastructure.csproj
    ├── Persistence/
    │   ├── CustomerDbContext.cs
    │   ├── CustomerRepository.cs
    │   └── Configurations/
    │       └── CustomerConfiguration.cs
    └── Messaging/
        └── IntegrationEventPublisher.cs
```

---

## Namespaces

```csharp
// Per Service
CustomerService.API.Controllers
CustomerService.API.Consumers
CustomerService.API.HealthChecks
CustomerService.API.Extensions

CustomerService.Domain.Entities
CustomerService.Domain.ValueObjects
CustomerService.Domain.Events
CustomerService.Domain.Repositories

CustomerService.Application.Commands
CustomerService.Application.Queries
CustomerService.Application.DTOs
CustomerService.Application.IntegrationEvents

CustomerService.Infrastructure.Persistence
CustomerService.Infrastructure.Messaging

// Shared
Shared.Contracts.Events
Shared.Contracts.DTOs
Shared.Infrastructure.Messaging
Shared.Infrastructure.ServiceDiscovery
```

---

## Project Files (.csproj)

### Service API Project

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\CustomerService.Application\CustomerService.Application.csproj" />
    <ProjectReference Include="..\..\Shared\Shared.Infrastructure\Shared.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.WebAPI" Version="8.*" />
    <PackageReference Include="Mvp24Hours.Infrastructure.RabbitMQ" Version="8.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.*" />
    <PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="8.*" />
    <PackageReference Include="AspNetCore.HealthChecks.RabbitMQ" Version="8.*" />
    <PackageReference Include="Microsoft.VisualStudio.Azure.Containers.Tools.Targets" Version="1.*" />
  </ItemGroup>
</Project>
```

### Shared Contracts Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- No dependencies - just DTOs and Event contracts -->
</Project>
```

---

## Shared Contracts (Integration Events)

```csharp
// Shared/Shared.Contracts/Events/CustomerCreatedEvent.cs
namespace Shared.Contracts.Events;

public record CustomerCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    
    public int CustomerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

// Shared/Shared.Contracts/Events/OrderPlacedEvent.cs
namespace Shared.Contracts.Events;

public record OrderPlacedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    
    public int OrderId { get; init; }
    public int CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public IList<OrderItemDto> Items { get; init; } = new List<OrderItemDto>();
}

public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);
```

---

## Service Implementation

### Controller

```csharp
// CustomerService.API/Controllers/CustomersController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CustomerService.Application.Commands;
using CustomerService.Application.Queries;

namespace CustomerService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _sender.Send(new GetCustomerQuery(id));
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await _sender.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }
}
```

### Command Handler with Event Publishing

```csharp
// CustomerService.Application/Commands/CreateCustomerHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using CustomerService.Application.DTOs;
using CustomerService.Application.IntegrationEvents;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Repositories;
using Shared.Infrastructure.Messaging;

namespace CustomerService.Application.Commands;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, IBusinessResult<CustomerDto>>
{
    private readonly ICustomerRepository _repository;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public CreateCustomerHandler(
        ICustomerRepository repository,
        IIntegrationEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<IBusinessResult<CustomerDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.Name, request.Email);

        await _repository.AddAsync(customer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Publish integration event for other services
        var integrationEvent = new CustomerCreatedIntegrationEvent
        {
            CustomerId = customer.Id,
            Name = customer.Name,
            Email = customer.Email
        };
        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        var dto = new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Active);
        return dto.ToBusiness();
    }
}
```

### Event Consumer

```csharp
// CustomerService.API/Consumers/OrderPlacedConsumer.cs
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using CustomerService.Domain.Repositories;

namespace CustomerService.API.Consumers;

public class OrderPlacedConsumer
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(
        ICustomerRepository customerRepository,
        ILogger<OrderPlacedConsumer> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderPlacedEvent. OrderId: {OrderId}, CustomerId: {CustomerId}",
            @event.OrderId,
            @event.CustomerId);

        var customer = await _customerRepository.GetByIdAsync(@event.CustomerId, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Customer {CustomerId} not found", @event.CustomerId);
            return;
        }

        // Update customer statistics, loyalty points, etc.
        customer.IncrementOrderCount();
        customer.AddToTotalSpent(@event.TotalAmount);

        _customerRepository.Update(customer);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Customer {CustomerId} statistics updated", @event.CustomerId);
    }
}
```

---

## API Gateway (Ocelot)

### Program.cs

```csharp
// ApiGateway/Program.cs
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot();
builder.Services.AddSwaggerForOcelot(builder.Configuration);

var app = builder.Build();

app.UseSwaggerForOcelotUI();
await app.UseOcelot();

app.Run();
```

### Ocelot Configuration

```json
// ApiGateway/ocelot.json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/customers/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "customer-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/customers/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "RateLimitOptions": {
        "EnableRateLimiting": true,
        "Period": "1m",
        "PeriodTimespan": 60,
        "Limit": 100
      }
    },
    {
      "DownstreamPathTemplate": "/api/orders/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "order-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/orders/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
    },
    {
      "DownstreamPathTemplate": "/api/products/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "product-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/products/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://localhost:5000"
  }
}
```

---

## Docker Configuration

### Service Dockerfile

```dockerfile
# CustomerService.API/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/CustomerService/CustomerService.API/CustomerService.API.csproj", "Services/CustomerService/CustomerService.API/"]
COPY ["src/Services/CustomerService/CustomerService.Application/CustomerService.Application.csproj", "Services/CustomerService/CustomerService.Application/"]
COPY ["src/Services/CustomerService/CustomerService.Domain/CustomerService.Domain.csproj", "Services/CustomerService/CustomerService.Domain/"]
COPY ["src/Services/CustomerService/CustomerService.Infrastructure/CustomerService.Infrastructure.csproj", "Services/CustomerService/CustomerService.Infrastructure/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/CustomerService/CustomerService.API/CustomerService.API.csproj"
COPY src/ .
WORKDIR "/src/Services/CustomerService/CustomerService.API"
RUN dotnet build "CustomerService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CustomerService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CustomerService.API.dll"]
```

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  api-gateway:
    build:
      context: .
      dockerfile: src/ApiGateway/Dockerfile
    ports:
      - "5000:80"
    depends_on:
      - customer-service
      - order-service
      - product-service
    networks:
      - microservices-network

  customer-service:
    build:
      context: .
      dockerfile: src/Services/CustomerService/CustomerService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=customer-db;Database=CustomerDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
      - RabbitMQ__Host=rabbitmq
    depends_on:
      - customer-db
      - rabbitmq
    networks:
      - microservices-network

  order-service:
    build:
      context: .
      dockerfile: src/Services/OrderService/OrderService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=order-db;Database=OrderDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
      - RabbitMQ__Host=rabbitmq
    depends_on:
      - order-db
      - rabbitmq
    networks:
      - microservices-network

  product-service:
    build:
      context: .
      dockerfile: src/Services/ProductService/ProductService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=product-db;Database=ProductDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
    depends_on:
      - product-db
    networks:
      - microservices-network

  customer-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
    volumes:
      - customer-db-data:/var/opt/mssql
    networks:
      - microservices-network

  order-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
    volumes:
      - order-db-data:/var/opt/mssql
    networks:
      - microservices-network

  product-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
    volumes:
      - product-db-data:/var/opt/mssql
    networks:
      - microservices-network

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    networks:
      - microservices-network

networks:
  microservices-network:
    driver: bridge

volumes:
  customer-db-data:
  order-db-data:
  product-db-data:
```

---

## Health Checks

```csharp
// CustomerService.API/Extensions/ServiceBuilderExtensions.cs
public static IServiceCollection AddHealthChecks(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddHealthChecks()
        .AddSqlServer(
            configuration.GetConnectionString("DefaultConnection")!,
            name: "database",
            tags: new[] { "db", "sql", "ready" })
        .AddRabbitMQ(
            $"amqp://{configuration["RabbitMQ:Host"]}",
            name: "rabbitmq",
            tags: new[] { "messaging", "ready" });

    return services;
}
```

---

## Service Communication Patterns

### Synchronous (HTTP)

```csharp
// Calling another service via HTTP
public class ProductServiceClient
{
    private readonly HttpClient _httpClient;

    public ProductServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/api/products/{productId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
    }
}

// Registration with resilience
services.AddHttpClient<ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Services:ProductService"]!);
})
.AddStandardResilienceHandler();
```

### Asynchronous (Messaging)

```csharp
// Publishing integration events
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : class;
}

// Consuming integration events
public interface IIntegrationEventHandler<TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

---

## Resilience Patterns

```csharp
// Using Microsoft.Extensions.Http.Resilience
services.AddHttpClient<ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Services:ProductService"]!);
})
.AddResilienceHandler("product-pipeline", builder =>
{
    builder
        .AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(500),
            BackoffType = DelayBackoffType.Exponential
        })
        .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(30),
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});
```

---

## Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CustomerDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "Services": {
    "ProductService": "http://product-service",
    "OrderService": "http://order-service"
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

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Event-Driven Architecture](template-event-driven.md)
- [CQRS Template](template-cqrs.md)
- [Containerization Patterns](containerization-patterns.md)

