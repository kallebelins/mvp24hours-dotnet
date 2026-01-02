# Template de Clean Architecture

> **Instrução para Agente de IA**: Use este template para aplicações corporativas que requerem clara separação de responsabilidades, testabilidade e independência de frameworks. Clean Architecture segue a Regra de Dependência onde dependências apontam para dentro.

---

## Quando Usar Clean Architecture

### Cenários Recomendados
- Aplicações corporativas com regras de negócio complexas
- Sistemas com manutenibilidade de longo prazo
- Projetos que requerem alta testabilidade
- Aplicações que precisam ser agnósticas a frameworks
- Equipes praticando TDD/BDD

### Não Recomendado
- Aplicações CRUD simples
- Projetos pequenos com escopo limitado
- Prazos apertados com requisitos simples
- Equipes sem familiaridade com arquiteturas em camadas

---

## A Regra de Dependência

Dependências devem apontar para dentro. Nada em um círculo interno pode saber qualquer coisa sobre algo em um círculo externo.

```
┌──────────────────────────────────────────────────────────────────┐
│                    Frameworks & Drivers                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                   Interface Adapters                       │ │
│  │  ┌──────────────────────────────────────────────────────┐ │ │
│  │  │          Application Business Rules                  │ │ │
│  │  │  ┌────────────────────────────────────────────────┐ │ │ │
│  │  │  │         Enterprise Business Rules              │ │ │ │
│  │  │  │                  (Entities)                    │ │ │ │
│  │  │  └────────────────────────────────────────────────┘ │ │ │
│  │  │                   (Use Cases)                        │ │ │
│  │  └──────────────────────────────────────────────────────┘ │ │
│  │              (Controllers, Presenters, Gateways)          │ │
│  └────────────────────────────────────────────────────────────┘ │
│                      (Web, UI, DB, External)                     │
└──────────────────────────────────────────────────────────────────┘
```

---

## Estrutura de Diretórios

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Domain/
    │   ├── NomeProjeto.Domain.csproj
    │   ├── Entities/
    │   │   ├── Cliente.cs
    │   │   ├── Pedido.cs
    │   │   └── Produto.cs
    │   ├── ValueObjects/
    │   │   ├── Email.cs
    │   │   ├── Dinheiro.cs
    │   │   └── Endereco.cs
    │   ├── Enums/
    │   │   ├── StatusPedido.cs
    │   │   └── TipoCliente.cs
    │   ├── Events/
    │   │   ├── DomainEvent.cs
    │   │   ├── ClienteCriadoEvent.cs
    │   │   └── PedidoRealizadoEvent.cs
    │   ├── Exceptions/
    │   │   ├── DomainException.cs
    │   │   └── EntityNotFoundException.cs
    │   └── Common/
    │       ├── Entity.cs
    │       ├── AggregateRoot.cs
    │       └── IHasDomainEvents.cs
    ├── NomeProjeto.Application/
    │   ├── NomeProjeto.Application.csproj
    │   ├── Common/
    │   │   ├── Interfaces/
    │   │   │   ├── IApplicationDbContext.cs
    │   │   │   ├── ICurrentUserService.cs
    │   │   │   ├── IDateTimeService.cs
    │   │   │   └── IEmailService.cs
    │   │   ├── Behaviors/
    │   │   │   ├── ValidationBehavior.cs
    │   │   │   ├── LoggingBehavior.cs
    │   │   │   └── PerformanceBehavior.cs
    │   │   ├── Mappings/
    │   │   │   └── MappingProfile.cs
    │   │   └── Models/
    │   │       ├── Result.cs
    │   │       └── PaginatedList.cs
    │   ├── Clientes/
    │   │   ├── Commands/
    │   │   │   ├── CriarCliente/
    │   │   │   │   ├── CriarClienteCommand.cs
    │   │   │   │   ├── CriarClienteCommandHandler.cs
    │   │   │   │   └── CriarClienteCommandValidator.cs
    │   │   │   └── AtualizarCliente/
    │   │   │       └── ...
    │   │   ├── Queries/
    │   │   │   ├── ObterClientePorId/
    │   │   │   │   ├── ObterClientePorIdQuery.cs
    │   │   │   │   ├── ObterClientePorIdQueryHandler.cs
    │   │   │   │   └── ClienteDto.cs
    │   │   │   └── ObterClientes/
    │   │   │       └── ...
    │   │   └── EventHandlers/
    │   │       └── ClienteCriadoEventHandler.cs
    │   └── Pedidos/
    │       ├── Commands/
    │       └── Queries/
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   ├── Persistence/
    │   │   ├── ApplicationDbContext.cs
    │   │   ├── ApplicationDbContextInitializer.cs
    │   │   └── Configurations/
    │   │       ├── ClienteConfiguration.cs
    │   │       └── PedidoConfiguration.cs
    │   ├── Services/
    │   │   ├── DateTimeService.cs
    │   │   └── EmailService.cs
    │   └── DependencyInjection.cs
    └── NomeProjeto.WebAPI/
        ├── NomeProjeto.WebAPI.csproj
        ├── Program.cs
        ├── Controllers/
        │   ├── ClientesController.cs
        │   └── PedidosController.cs
        ├── Filters/
        │   └── ApiExceptionFilterAttribute.cs
        ├── Services/
        │   └── CurrentUserService.cs
        └── DependencyInjection.cs
```

---

## Camada Domain

### Entidade Base

```csharp
// Domain/Common/Entity.cs
namespace NomeProjeto.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
        
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

### Aggregate Root com Domain Events

```csharp
// Domain/Common/AggregateRoot.cs
namespace NomeProjeto.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### Entidade Cliente

```csharp
// Domain/Entities/Cliente.cs
using NomeProjeto.Domain.Common;
using NomeProjeto.Domain.Events;
using NomeProjeto.Domain.ValueObjects;

namespace NomeProjeto.Domain.Entities;

public class Cliente : AggregateRoot<int>
{
    public string Nome { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public bool Ativo { get; private set; }
    public DateTime Criado { get; private set; }

    private readonly List<Pedido> _pedidos = new();
    public IReadOnlyCollection<Pedido> Pedidos => _pedidos.AsReadOnly();

    private Cliente() { } // EF Core

    public static Cliente Create(string nome, string email)
    {
        var cliente = new Cliente
        {
            Nome = nome,
            Email = new Email(email),
            Ativo = true,
            Criado = DateTime.UtcNow
        };

        cliente.AddDomainEvent(new ClienteCriadoEvent(cliente));

        return cliente;
    }

    public void Update(string nome, string email)
    {
        Nome = nome;
        Email = new Email(email);
    }

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}
```

---

## Camada Application

### Interface de DbContext

```csharp
// Application/Common/Interfaces/IApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using NomeProjeto.Domain.Entities;

namespace NomeProjeto.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Cliente> Clientes { get; }
    DbSet<Pedido> Pedidos { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

### Command com Handler e Validator

```csharp
// Application/Clientes/Commands/CriarCliente/CriarClienteCommand.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace NomeProjeto.Application.Clientes.Commands.CriarCliente;

public record CriarClienteCommand(string Nome, string Email) : IRequest<IBusinessResult<int>>;

// Application/Clientes/Commands/CriarCliente/CriarClienteCommandValidator.cs
using FluentValidation;

namespace NomeProjeto.Application.Clientes.Commands.CriarCliente;

public class CriarClienteCommandValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteCommandValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Formato de email inválido");
    }
}

// Application/Clientes/Commands/CriarCliente/CriarClienteCommandHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Application.Common.Interfaces;
using NomeProjeto.Domain.Entities;

namespace NomeProjeto.Application.Clientes.Commands.CriarCliente;

public class CriarClienteCommandHandler : IRequestHandler<CriarClienteCommand, IBusinessResult<int>>
{
    private readonly IApplicationDbContext _context;

    public CriarClienteCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IBusinessResult<int>> Handle(CriarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = Cliente.Create(request.Nome, request.Email);

        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(cancellationToken);

        return cliente.Id.ToBusiness();
    }
}
```

### Query com Handler

```csharp
// Application/Clientes/Queries/ObterClientePorId/ObterClientePorIdQuery.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace NomeProjeto.Application.Clientes.Queries.ObterClientePorId;

public record ObterClientePorIdQuery(int Id) : IRequest<IBusinessResult<ClienteDto>>;

// Application/Clientes/Queries/ObterClientePorId/ClienteDto.cs
namespace NomeProjeto.Application.Clientes.Queries.ObterClientePorId;

public record ClienteDto(int Id, string Nome, string Email, bool Ativo, DateTime Criado);

// Application/Clientes/Queries/ObterClientePorId/ObterClientePorIdQueryHandler.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Application.Common.Interfaces;

namespace NomeProjeto.Application.Clientes.Queries.ObterClientePorId;

public class ObterClientePorIdQueryHandler : IRequestHandler<ObterClientePorIdQuery, IBusinessResult<ClienteDto>>
{
    private readonly IApplicationDbContext _context;

    public ObterClientePorIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IBusinessResult<ClienteDto>> Handle(ObterClientePorIdQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _context.Clientes
            .Where(c => c.Id == request.Id)
            .Select(c => new ClienteDto(c.Id, c.Nome, c.Email.Value, c.Ativo, c.Criado))
            .FirstOrDefaultAsync(cancellationToken);

        if (cliente == null)
            return default(ClienteDto).ToBusiness("Cliente não encontrado");

        return cliente.ToBusiness();
    }
}
```

---

## Camada Infrastructure

### Application DbContext

```csharp
// Infrastructure/Persistence/ApplicationDbContext.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using NomeProjeto.Application.Common.Interfaces;
using NomeProjeto.Domain.Common;
using NomeProjeto.Domain.Entities;

namespace NomeProjeto.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IMediator _mediator;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Disparar domain events antes de salvar
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entities = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
```

---

## Camada WebAPI

### Controller

```csharp
// WebAPI/Controllers/ClientesController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NomeProjeto.Application.Clientes.Commands.CriarCliente;
using NomeProjeto.Application.Clientes.Commands.AtualizarCliente;
using NomeProjeto.Application.Clientes.Commands.ExcluirCliente;
using NomeProjeto.Application.Clientes.Queries.ObterClientePorId;
using NomeProjeto.Application.Clientes.Queries.ObterClientes;

namespace NomeProjeto.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ISender _sender;

    public ClientesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ObterClientesQuery query)
    {
        var result = await _sender.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _sender.Send(new ObterClientePorIdQuery(id));
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriarClienteCommand command)
    {
        var result = await _sender.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result);
    }
}
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Template Hexagonal](template-hexagonal.md)
- [Template DDD](template-ddd.md)
- [Template CQRS](template-cqrs.md)

