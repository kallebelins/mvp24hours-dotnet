# Template de Arquitetura Microservices

> **Instrução para Agente de IA**: Use este template para sistemas distribuídos que requerem deploy independente, escalabilidade e autonomia de equipes. Cada microservice possui seus próprios dados e se comunica através de APIs bem definidas ou mensageria.

---

## Quando Usar Microservices

### Cenários Recomendados
- Aplicações de grande escala com múltiplas equipes
- Sistemas que requerem escalabilidade independente por serviço
- Ambientes poliglotas (diferentes tecnologias por serviço)
- Domínios complexos com bounded contexts claros
- Requisitos de alta disponibilidade e tolerância a falhas

### Não Recomendado
- Aplicações pequenas ou startups
- Equipes sem experiência em sistemas distribuídos
- Projetos sem fronteiras de domínio claras
- Aplicações CRUD simples
- Prazos apertados com recursos limitados

---

## Princípios Fundamentais

1. **Responsabilidade Única**: Cada serviço faz uma coisa bem
2. **Propriedade de Dados**: Cada serviço possui seu banco de dados
3. **Deploy Independente**: Serviços deployam independentemente
4. **Resiliência**: Serviços tratam falhas graciosamente
5. **Governança Descentralizada**: Equipes escolhem sua tecnologia
6. **Smart Endpoints, Dumb Pipes**: Lógica nos serviços, não no middleware

---

## Estrutura da Solução

```
ProjetoMicroservices/
├── ProjetoMicroservices.sln
├── src/
│   ├── Services/
│   │   ├── ClienteService/
│   │   │   ├── ClienteService.API/
│   │   │   │   ├── ClienteService.API.csproj
│   │   │   │   ├── Program.cs
│   │   │   │   ├── Dockerfile
│   │   │   │   ├── Controllers/
│   │   │   │   │   └── ClientesController.cs
│   │   │   │   └── appsettings.json
│   │   │   ├── ClienteService.Domain/
│   │   │   ├── ClienteService.Application/
│   │   │   └── ClienteService.Infrastructure/
│   │   ├── PedidoService/
│   │   │   └── ... (mesma estrutura)
│   │   ├── ProdutoService/
│   │   │   └── ... (mesma estrutura)
│   │   └── NotificacaoService/
│   │       └── ... (mesma estrutura)
│   ├── ApiGateway/
│   │   ├── ApiGateway.csproj
│   │   ├── Program.cs
│   │   ├── Dockerfile
│   │   └── ocelot.json
│   └── Shared/
│       ├── Shared.Contracts/
│       │   ├── Shared.Contracts.csproj
│       │   ├── Events/
│       │   │   ├── ClienteCriadoEvent.cs
│       │   │   └── PedidoRealizadoEvent.cs
│       │   └── DTOs/
│       │       ├── ClienteDto.cs
│       │       └── PedidoDto.cs
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
    │   ├── cliente-service.yaml
    │   ├── pedido-service.yaml
    │   └── api-gateway.yaml
    └── terraform/
        └── main.tf
```

---

## Estrutura do Serviço (Por Serviço)

```
ClienteService/
├── ClienteService.API/
│   ├── ClienteService.API.csproj
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   └── ClientesController.cs
│   ├── Consumers/
│   │   └── PedidoRealizadoConsumer.cs
│   ├── HealthChecks/
│   │   └── DatabaseHealthCheck.cs
│   └── Extensions/
│       └── ServiceBuilderExtensions.cs
├── ClienteService.Domain/
│   ├── Entities/
│   │   └── Cliente.cs
│   ├── ValueObjects/
│   │   └── Email.cs
│   ├── Events/
│   │   └── ClienteCriadoEvent.cs
│   └── Repositories/
│       └── IClienteRepository.cs
├── ClienteService.Application/
│   ├── Commands/
│   │   ├── CriarClienteCommand.cs
│   │   └── CriarClienteHandler.cs
│   ├── Queries/
│   │   ├── ObterClienteQuery.cs
│   │   └── ObterClienteHandler.cs
│   ├── DTOs/
│   │   └── ClienteDto.cs
│   └── IntegrationEvents/
│       └── ClienteCriadoIntegrationEvent.cs
└── ClienteService.Infrastructure/
    ├── Persistence/
    │   ├── ClienteDbContext.cs
    │   ├── ClienteRepository.cs
    │   └── Configurations/
    │       └── ClienteConfiguration.cs
    └── Messaging/
        └── IntegrationEventPublisher.cs
```

---

## Contratos Compartilhados (Integration Events)

```csharp
// Shared/Shared.Contracts/Events/ClienteCriadoEvent.cs
namespace Shared.Contracts.Events;

public record ClienteCriadoEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    
    public int ClienteId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

// Shared/Shared.Contracts/Events/PedidoRealizadoEvent.cs
namespace Shared.Contracts.Events;

public record PedidoRealizadoEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    
    public int PedidoId { get; init; }
    public int ClienteId { get; init; }
    public decimal ValorTotal { get; init; }
    public IList<ItemPedidoDto> Items { get; init; } = new List<ItemPedidoDto>();
}

public record ItemPedidoDto(int ProdutoId, string NomeProduto, int Quantidade, decimal PrecoUnitario);
```

---

## Implementação do Serviço

### Controller

```csharp
// ClienteService.API/Controllers/ClientesController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ClienteService.Application.Commands;
using ClienteService.Application.Queries;

namespace ClienteService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ISender _sender;

    public ClientesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _sender.Send(new ObterClienteQuery(id));
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CriarClienteCommand command)
    {
        var result = await _sender.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }
}
```

### Command Handler com Publicação de Evento

```csharp
// ClienteService.Application/Commands/CriarClienteHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ClienteService.Application.DTOs;
using ClienteService.Application.IntegrationEvents;
using ClienteService.Domain.Entities;
using ClienteService.Domain.Repositories;
using Shared.Infrastructure.Messaging;

namespace ClienteService.Application.Commands;

public class CriarClienteHandler : IRequestHandler<CriarClienteCommand, IBusinessResult<ClienteDto>>
{
    private readonly IClienteRepository _repository;
    private readonly IIntegrationEventPublisher _eventPublisher;

    public CriarClienteHandler(
        IClienteRepository repository,
        IIntegrationEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }

    public async Task<IBusinessResult<ClienteDto>> Handle(
        CriarClienteCommand request,
        CancellationToken cancellationToken)
    {
        var cliente = Cliente.Create(request.Nome, request.Email);

        await _repository.AddAsync(cliente, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        // Publicar evento de integração para outros serviços
        var integrationEvent = new ClienteCriadoIntegrationEvent
        {
            ClienteId = cliente.Id,
            Nome = cliente.Nome,
            Email = cliente.Email
        };
        await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);

        var dto = new ClienteDto(cliente.Id, cliente.Nome, cliente.Email, cliente.Ativo);
        return dto.ToBusiness();
    }
}
```

### Consumer de Evento

```csharp
// ClienteService.API/Consumers/PedidoRealizadoConsumer.cs
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using ClienteService.Domain.Repositories;

namespace ClienteService.API.Consumers;

public class PedidoRealizadoConsumer
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ILogger<PedidoRealizadoConsumer> _logger;

    public PedidoRealizadoConsumer(
        IClienteRepository clienteRepository,
        ILogger<PedidoRealizadoConsumer> logger)
    {
        _clienteRepository = clienteRepository;
        _logger = logger;
    }

    public async Task HandleAsync(PedidoRealizadoEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processando PedidoRealizadoEvent. PedidoId: {PedidoId}, ClienteId: {ClienteId}",
            @event.PedidoId,
            @event.ClienteId);

        var cliente = await _clienteRepository.GetByIdAsync(@event.ClienteId, cancellationToken);
        if (cliente == null)
        {
            _logger.LogWarning("Cliente {ClienteId} não encontrado", @event.ClienteId);
            return;
        }

        // Atualizar estatísticas do cliente, pontos de fidelidade, etc.
        cliente.IncrementarContadorPedidos();
        cliente.AdicionarAoTotalGasto(@event.ValorTotal);

        _clienteRepository.Update(cliente);
        await _clienteRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Estatísticas do cliente {ClienteId} atualizadas", @event.ClienteId);
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

### Configuração Ocelot

```json
// ApiGateway/ocelot.json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/clientes/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "cliente-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/clientes/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "RateLimitOptions": {
        "EnableRateLimiting": true,
        "Period": "1m",
        "PeriodTimespan": 60,
        "Limit": 100
      }
    },
    {
      "DownstreamPathTemplate": "/api/pedidos/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "pedido-service",
          "Port": 80
        }
      ],
      "UpstreamPathTemplate": "/api/pedidos/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://localhost:5000"
  }
}
```

---

## Configuração Docker

### Dockerfile do Serviço

```dockerfile
# ClienteService.API/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Services/ClienteService/ClienteService.API/ClienteService.API.csproj", "Services/ClienteService/ClienteService.API/"]
COPY ["src/Services/ClienteService/ClienteService.Application/ClienteService.Application.csproj", "Services/ClienteService/ClienteService.Application/"]
COPY ["src/Services/ClienteService/ClienteService.Domain/ClienteService.Domain.csproj", "Services/ClienteService/ClienteService.Domain/"]
COPY ["src/Services/ClienteService/ClienteService.Infrastructure/ClienteService.Infrastructure.csproj", "Services/ClienteService/ClienteService.Infrastructure/"]
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/ClienteService/ClienteService.API/ClienteService.API.csproj"
COPY src/ .
WORKDIR "/src/Services/ClienteService/ClienteService.API"
RUN dotnet build "ClienteService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ClienteService.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ClienteService.API.dll"]
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
      - cliente-service
      - pedido-service
    networks:
      - microservices-network

  cliente-service:
    build:
      context: .
      dockerfile: src/Services/ClienteService/ClienteService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=cliente-db;Database=ClienteDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
      - RabbitMQ__Host=rabbitmq
    depends_on:
      - cliente-db
      - rabbitmq
    networks:
      - microservices-network

  pedido-service:
    build:
      context: .
      dockerfile: src/Services/PedidoService/PedidoService.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=pedido-db;Database=PedidoDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True
      - RabbitMQ__Host=rabbitmq
    depends_on:
      - pedido-db
      - rabbitmq
    networks:
      - microservices-network

  cliente-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
    volumes:
      - cliente-db-data:/var/opt/mssql
    networks:
      - microservices-network

  pedido-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
    volumes:
      - pedido-db-data:/var/opt/mssql
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
  cliente-db-data:
  pedido-db-data:
```

---

## Padrões de Resiliência

```csharp
// Usando Microsoft.Extensions.Http.Resilience
services.AddHttpClient<ProdutoServiceClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Services:ProdutoService"]!);
})
.AddResilienceHandler("produto-pipeline", builder =>
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

## Configuração (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ClienteDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "Services": {
    "ProdutoService": "http://produto-service",
    "PedidoService": "http://pedido-service"
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

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Arquitetura Event-Driven](template-event-driven.md)
- [Template CQRS](template-cqrs.md)
- [Padrões de Containerização](containerization-patterns.md)

