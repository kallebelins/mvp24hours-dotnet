# Template de Arquitetura CQRS

> **Instrução para Agente de IA**: Use este template para aplicações que se beneficiam da separação de operações de leitura e escrita. CQRS (Command Query Responsibility Segregation) é ideal para domínios complexos com diferentes padrões de leitura/escrita.

---

## Quando Usar CQRS

### Cenários Recomendados
- Domínios de negócio complexos com diferentes modelos de leitura/escrita
- Operações de leitura de alta performance com views desnormalizadas
- Aplicações que requerem trilhas de auditoria e histórico de eventos
- Sistemas com cargas de trabalho assimétricas de leitura/escrita
- Domínios colaborativos com necessidades de resolução de conflitos

### Não Recomendado
- Aplicações CRUD simples
- Equipes pequenas sem experiência com CQRS
- Projetos com prazos apertados e requisitos simples
- Aplicações com modelos de leitura/escrita similares

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
    │   ├── ValueObjects/
    │   │   └── Email.cs
    │   ├── Commands/
    │   │   ├── Clientes/
    │   │   │   ├── CriarClienteCommand.cs
    │   │   │   ├── AtualizarClienteCommand.cs
    │   │   │   └── ExcluirClienteCommand.cs
    │   │   └── ICommand.cs
    │   ├── Queries/
    │   │   ├── Clientes/
    │   │   │   ├── ObterClientePorIdQuery.cs
    │   │   │   ├── ObterTodosClientesQuery.cs
    │   │   │   └── ObterClientesPorFiltroQuery.cs
    │   │   └── IQuery.cs
    │   ├── Results/
    │   │   ├── Clientes/
    │   │   │   ├── ClienteResult.cs
    │   │   │   └── ClienteListResult.cs
    │   │   └── IQueryResult.cs
    │   ├── Validators/
    │   │   └── Clientes/
    │   │       ├── CriarClienteValidator.cs
    │   │       └── AtualizarClienteValidator.cs
    │   └── Contract/
    │       ├── Handlers/
    │       │   ├── ICommandHandler.cs
    │       │   └── IQueryHandler.cs
    │       └── Repositories/
    │           ├── IClienteReadRepository.cs
    │           └── IClienteWriteRepository.cs
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── Write/
    │   │   │   ├── WriteDbContext.cs
    │   │   │   └── Configurations/
    │   │   │       └── ClienteConfiguration.cs
    │   │   └── Read/
    │   │       ├── ReadDbContext.cs
    │   │       └── Views/
    │   │           └── ClienteReadModel.cs
    │   └── Repositories/
    │       ├── ClienteWriteRepository.cs
    │       └── ClienteReadRepository.cs
    ├── NomeProjeto.Application/
    │   ├── NomeProjeto.Application.csproj
    │   ├── Handlers/
    │   │   ├── Commands/
    │   │   │   ├── CriarClienteHandler.cs
    │   │   │   ├── AtualizarClienteHandler.cs
    │   │   │   └── ExcluirClienteHandler.cs
    │   │   └── Queries/
    │   │       ├── ObterClientePorIdHandler.cs
    │   │       ├── ObterTodosClientesHandler.cs
    │   │       └── ObterClientesPorFiltroHandler.cs
    │   ├── Behaviors/
    │   │   ├── ValidationBehavior.cs
    │   │   └── LoggingBehavior.cs
    │   └── Mappings/
    │       └── ClienteProfile.cs
    └── NomeProjeto.WebAPI/
        ├── NomeProjeto.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── Controllers/
        │   └── ClientesController.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

---

## Conceitos Principais

### Commands (Operações de Escrita)
- Representam intenção de alterar estado
- Devem ser imutáveis
- Nomeados com verbo no imperativo (Criar, Atualizar, Excluir)
- Retornam void ou confirmação simples

### Queries (Operações de Leitura)
- Solicitação de dados sem efeitos colaterais
- Podem retornar resultados complexos e desnormalizados
- Nomeados com verbo "Obter" + descrição
- Otimizados para casos de uso específicos

### Handlers
- Responsabilidade única: um handler por command/query
- Command handlers modificam estado
- Query handlers apenas leem dados

---

## Namespaces

```csharp
// Core
NomeProjeto.Core.Entities
NomeProjeto.Core.ValueObjects
NomeProjeto.Core.Commands
NomeProjeto.Core.Commands.Clientes
NomeProjeto.Core.Queries
NomeProjeto.Core.Queries.Clientes
NomeProjeto.Core.Results
NomeProjeto.Core.Results.Clientes
NomeProjeto.Core.Validators.Clientes
NomeProjeto.Core.Contract.Handlers
NomeProjeto.Core.Contract.Repositories

// Infrastructure
NomeProjeto.Infrastructure.Data.Write
NomeProjeto.Infrastructure.Data.Read
NomeProjeto.Infrastructure.Repositories

// Application
NomeProjeto.Application.Handlers.Commands
NomeProjeto.Application.Handlers.Queries
NomeProjeto.Application.Behaviors
NomeProjeto.Application.Mappings

// WebAPI
NomeProjeto.WebAPI.Controllers
NomeProjeto.WebAPI.Extensions
```

---

## Templates de Command

### Definição de Command

```csharp
// Core/Commands/Clientes/CriarClienteCommand.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.Results.Clientes;

namespace NomeProjeto.Core.Commands.Clientes;

public record CriarClienteCommand(
    string Nome,
    string Email,
    string Telefone
) : IRequest<IBusinessResult<ClienteResult>>;
```

### Command sem Retorno

```csharp
// Core/Commands/Clientes/ExcluirClienteCommand.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace NomeProjeto.Core.Commands.Clientes;

public record ExcluirClienteCommand(int Id) : IRequest<IBusinessResult<bool>>;
```

---

## Templates de Query

### Definição de Query

```csharp
// Core/Queries/Clientes/ObterClientePorIdQuery.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.Results.Clientes;

namespace NomeProjeto.Core.Queries.Clientes;

public record ObterClientePorIdQuery(int Id) : IRequest<IBusinessResult<ClienteResult>>;
```

### Query com Filtro

```csharp
// Core/Queries/Clientes/ObterClientesPorFiltroQuery.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.Results.Clientes;

namespace NomeProjeto.Core.Queries.Clientes;

public record ObterClientesPorFiltroQuery(
    string? Nome,
    string? Email,
    bool? Ativo,
    int Pagina = 1,
    int TamanhoPagina = 10
) : IRequest<IBusinessResult<ClienteListResult>>;
```

---

## Templates de Result

```csharp
// Core/Results/Clientes/ClienteResult.cs
namespace NomeProjeto.Core.Results.Clientes;

public record ClienteResult(
    int Id,
    string Nome,
    string Email,
    string Telefone,
    bool Ativo,
    DateTime Criado
);

// Core/Results/Clientes/ClienteListResult.cs
namespace NomeProjeto.Core.Results.Clientes;

public record ClienteListResult(
    IList<ClienteResult> Items,
    int TotalCount,
    int Pagina,
    int TamanhoPagina
);
```

---

## Templates de Command Handler

### Handler de Criação

```csharp
// Application/Handlers/Commands/CriarClienteHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Core.Commands.Clientes;
using NomeProjeto.Core.Contract.Repositories;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.Results.Clientes;

namespace NomeProjeto.Application.Handlers.Commands;

public class CriarClienteHandler : IRequestHandler<CriarClienteCommand, IBusinessResult<ClienteResult>>
{
    private readonly IClienteWriteRepository _repository;

    public CriarClienteHandler(IClienteWriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<ClienteResult>> Handle(
        CriarClienteCommand request,
        CancellationToken cancellationToken)
    {
        var cliente = new Cliente
        {
            Nome = request.Nome,
            Email = request.Email,
            Telefone = request.Telefone,
            Ativo = true,
            Criado = DateTime.UtcNow
        };

        await _repository.AddAsync(cliente, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var result = new ClienteResult(
            cliente.Id,
            cliente.Nome,
            cliente.Email,
            cliente.Telefone,
            cliente.Ativo,
            cliente.Criado
        );

        return result.ToBusiness();
    }
}
```

---

## Templates de Query Handler

### Handler de Obter por Id

```csharp
// Application/Handlers/Queries/ObterClientePorIdHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Core.Contract.Repositories;
using NomeProjeto.Core.Queries.Clientes;
using NomeProjeto.Core.Results.Clientes;

namespace NomeProjeto.Application.Handlers.Queries;

public class ObterClientePorIdHandler : IRequestHandler<ObterClientePorIdQuery, IBusinessResult<ClienteResult>>
{
    private readonly IClienteReadRepository _repository;

    public ObterClientePorIdHandler(IClienteReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<ClienteResult>> Handle(
        ObterClientePorIdQuery request,
        CancellationToken cancellationToken)
    {
        var cliente = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (cliente == null)
            return default(ClienteResult).ToBusiness("Cliente não encontrado");

        var result = new ClienteResult(
            cliente.Id,
            cliente.Nome,
            cliente.Email,
            cliente.Telefone,
            cliente.Ativo,
            cliente.Criado
        );

        return result.ToBusiness();
    }
}
```

---

## Interfaces de Repositório

```csharp
// Core/Contract/Repositories/IClienteWriteRepository.cs
using NomeProjeto.Core.Entities;

namespace NomeProjeto.Core.Contract.Repositories;

public interface IClienteWriteRepository
{
    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task DeleteAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Core/Contract/Repositories/IClienteReadRepository.cs
using NomeProjeto.Core.Entities;

namespace NomeProjeto.Core.Contract.Repositories;

public interface IClienteReadRepository
{
    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IList<Cliente> Items, int TotalCount)> GetByFilterAsync(
        string? nome,
        string? email,
        bool? ativo,
        int pagina,
        int tamanhoPagina,
        CancellationToken cancellationToken = default
    );
}
```

---

## Template de Controller

```csharp
// Controllers/ClientesController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NomeProjeto.Core.Commands.Clientes;
using NomeProjeto.Core.Queries.Clientes;

namespace NomeProjeto.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new ObterTodosClientesQuery());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new ObterClientePorIdQuery(id));
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriarClienteCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AtualizarClienteCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID inconsistente");

        var result = await _mediator.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new ExcluirClienteCommand(id));
        if (result.HasErrors)
            return NotFound(result);
        return NoContent();
    }
}
```

---

## Configuração (appsettings.json)

```json
{
  "ConnectionStrings": {
    "WriteConnection": "Server=localhost;Database=ProjectDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;",
    "ReadConnection": "Server=localhost;Database=ProjectDb_Read;User Id=readonly;Password=YourPassword;TrustServerCertificate=True;"
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
- [Clean Architecture](template-clean-architecture.md)
- [Estrutura Complex N-Layers](structure-complex-nlayers.md)

