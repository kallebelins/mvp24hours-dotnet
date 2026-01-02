# Template de Arquitetura Hexagonal

> **Instrução para Agente de IA**: Use este template para aplicações que requerem forte isolamento entre lógica de negócio e sistemas externos. Arquitetura Hexagonal (Ports & Adapters) garante que o domínio permaneça independente de preocupações de infraestrutura.

---

## Quando Usar Arquitetura Hexagonal

### Cenários Recomendados
- Aplicações com múltiplas integrações externas
- Sistemas que requerem alta testabilidade
- Projetos com necessidades de infraestrutura em evolução
- Aplicações que precisam trocar implementações facilmente
- Sistemas com manutenibilidade de longo prazo

### Não Recomendado
- Aplicações CRUD simples
- Projetos pequenos com escopo limitado
- Equipes sem familiaridade com padrões de arquitetura limpa
- Projetos com prazos apertados

---

## Conceitos Principais

### Ports (Portas)
- **Inbound Ports (Driving)**: Interfaces que definem como o mundo externo interage com a aplicação (casos de uso)
- **Outbound Ports (Driven)**: Interfaces que definem como a aplicação interage com sistemas externos

### Adapters (Adaptadores)
- **Inbound Adapters (Primary)**: Controllers, CLI, Message Consumers - implementam o lado de entrada
- **Outbound Adapters (Secondary)**: Repositórios, APIs Externas, Message Publishers - implementam o lado de saída

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
    │   │   └── Pedido.cs
    │   ├── ValueObjects/
    │   │   ├── Email.cs
    │   │   ├── Dinheiro.cs
    │   │   └── Endereco.cs
    │   ├── Aggregates/
    │   │   └── ClienteAggregate.cs
    │   ├── Events/
    │   │   ├── ClienteCriadoEvent.cs
    │   │   └── PedidoRealizadoEvent.cs
    │   └── Exceptions/
    │       ├── DomainException.cs
    │       └── ClienteNaoEncontradoException.cs
    ├── NomeProjeto.Application/
    │   ├── NomeProjeto.Application.csproj
    │   ├── Ports/
    │   │   ├── Inbound/
    │   │   │   ├── ICriarClienteUseCase.cs
    │   │   │   ├── IObterClienteUseCase.cs
    │   │   │   ├── IAtualizarClienteUseCase.cs
    │   │   │   └── IExcluirClienteUseCase.cs
    │   │   └── Outbound/
    │   │       ├── IClienteRepository.cs
    │   │       ├── IEmailService.cs
    │   │       ├── IPaymentGateway.cs
    │   │       └── IEventPublisher.cs
    │   ├── UseCases/
    │   │   ├── CriarClienteUseCase.cs
    │   │   ├── ObterClienteUseCase.cs
    │   │   ├── AtualizarClienteUseCase.cs
    │   │   └── ExcluirClienteUseCase.cs
    │   ├── DTOs/
    │   │   ├── Requests/
    │   │   │   ├── CriarClienteRequest.cs
    │   │   │   └── AtualizarClienteRequest.cs
    │   │   └── Responses/
    │   │       ├── ClienteResponse.cs
    │   │       └── ClienteListResponse.cs
    │   └── Validators/
    │       ├── CriarClienteValidator.cs
    │       └── AtualizarClienteValidator.cs
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   └── Adapters/
    │       ├── Outbound/
    │       │   ├── Persistence/
    │       │   │   ├── DataContext.cs
    │       │   │   ├── ClienteRepository.cs
    │       │   │   └── Configurations/
    │       │   │       └── ClienteConfiguration.cs
    │       │   ├── Email/
    │       │   │   └── SmtpEmailService.cs
    │       │   ├── Payment/
    │       │   │   └── StripePaymentGateway.cs
    │       │   └── Messaging/
    │       │       └── RabbitMQEventPublisher.cs
    │       └── Inbound/
    │           └── Messaging/
    │               └── PedidoEventConsumer.cs
    └── NomeProjeto.WebAPI/
        ├── NomeProjeto.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── Adapters/
        │   └── Inbound/
        │       └── Http/
        │           └── Controllers/
        │               └── ClientesController.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

---

## Namespaces

```csharp
// Domain (sem dependências externas)
NomeProjeto.Domain.Entities
NomeProjeto.Domain.ValueObjects
NomeProjeto.Domain.Aggregates
NomeProjeto.Domain.Events
NomeProjeto.Domain.Exceptions

// Application (camada de orquestração)
NomeProjeto.Application.Ports.Inbound
NomeProjeto.Application.Ports.Outbound
NomeProjeto.Application.UseCases
NomeProjeto.Application.DTOs.Requests
NomeProjeto.Application.DTOs.Responses
NomeProjeto.Application.Validators

// Infrastructure (implementações externas)
NomeProjeto.Infrastructure.Adapters.Outbound.Persistence
NomeProjeto.Infrastructure.Adapters.Outbound.Email
NomeProjeto.Infrastructure.Adapters.Outbound.Payment
NomeProjeto.Infrastructure.Adapters.Outbound.Messaging

// WebAPI (adaptador HTTP)
NomeProjeto.WebAPI.Adapters.Inbound.Http.Controllers
NomeProjeto.WebAPI.Extensions
```

---

## Camada Domain

### Entidade

```csharp
// Domain/Entities/Cliente.cs
namespace NomeProjeto.Domain.Entities;

public class Cliente
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool Ativo { get; private set; }
    public DateTime Criado { get; private set; }

    private Cliente() { }

    public Cliente(string nome, string email)
    {
        SetNome(nome);
        SetEmail(email);
        Ativo = true;
        Criado = DateTime.UtcNow;
    }

    public void SetNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome não pode ser vazio");
        
        Nome = nome;
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email não pode ser vazio");
        
        if (!email.Contains('@'))
            throw new DomainException("Formato de email inválido");
        
        Email = email;
    }

    public void Ativar() => Ativo = true;
    public void Desativar() => Ativo = false;
}
```

### Value Object

```csharp
// Domain/ValueObjects/Email.cs
namespace NomeProjeto.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email não pode ser vazio", nameof(value));
        
        if (!value.Contains('@'))
            throw new ArgumentException("Formato de email inválido", nameof(value));
        
        Value = value.ToLowerInvariant();
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string value) => new(value);
}
```

---

## Templates de Inbound Ports (Use Cases)

```csharp
// Application/Ports/Inbound/ICriarClienteUseCase.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Application.DTOs.Requests;
using NomeProjeto.Application.DTOs.Responses;

namespace NomeProjeto.Application.Ports.Inbound;

public interface ICriarClienteUseCase
{
    Task<IBusinessResult<ClienteResponse>> ExecuteAsync(
        CriarClienteRequest request, 
        CancellationToken cancellationToken = default);
}

// Application/Ports/Inbound/IObterClienteUseCase.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Application.DTOs.Responses;

namespace NomeProjeto.Application.Ports.Inbound;

public interface IObterClienteUseCase
{
    Task<IBusinessResult<ClienteResponse>> ExecuteByIdAsync(
        int id, 
        CancellationToken cancellationToken = default);
    
    Task<IBusinessResult<ClienteListResponse>> ExecuteAllAsync(
        CancellationToken cancellationToken = default);
}
```

---

## Templates de Outbound Ports (Interfaces Driven)

```csharp
// Application/Ports/Outbound/IClienteRepository.cs
using NomeProjeto.Domain.Entities;

namespace NomeProjeto.Application.Ports.Outbound;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Cliente?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task UpdateAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task DeleteAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Application/Ports/Outbound/IEmailService.cs
namespace NomeProjeto.Application.Ports.Outbound;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string to, string customerName, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default);
}
```

---

## Implementação de Use Case

```csharp
// Application/UseCases/CriarClienteUseCase.cs
using FluentValidation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Application.DTOs.Requests;
using NomeProjeto.Application.DTOs.Responses;
using NomeProjeto.Application.Ports.Inbound;
using NomeProjeto.Application.Ports.Outbound;
using NomeProjeto.Domain.Entities;
using NomeProjeto.Domain.Events;

namespace NomeProjeto.Application.UseCases;

public class CriarClienteUseCase : ICriarClienteUseCase
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IEmailService _emailService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IValidator<CriarClienteRequest> _validator;

    public CriarClienteUseCase(
        IClienteRepository clienteRepository,
        IEmailService emailService,
        IEventPublisher eventPublisher,
        IValidator<CriarClienteRequest> validator)
    {
        _clienteRepository = clienteRepository;
        _emailService = emailService;
        _eventPublisher = eventPublisher;
        _validator = validator;
    }

    public async Task<IBusinessResult<ClienteResponse>> ExecuteAsync(
        CriarClienteRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validar
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.ToBusiness<ClienteResponse>();

        // Verificar se email existe
        var existingCliente = await _clienteRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingCliente != null)
            return default(ClienteResponse).ToBusiness("Email já cadastrado");

        // Criar entidade de domínio
        var cliente = new Cliente(request.Nome, request.Email);

        // Persistir
        await _clienteRepository.AddAsync(cliente, cancellationToken);
        await _clienteRepository.SaveChangesAsync(cancellationToken);

        // Enviar email de boas-vindas (fire and forget)
        _ = _emailService.SendWelcomeEmailAsync(cliente.Email, cliente.Nome, cancellationToken);

        // Publicar evento
        await _eventPublisher.PublishAsync(new ClienteCriadoEvent(cliente.Id, cliente.Nome, cliente.Email), cancellationToken);

        // Retornar resposta
        var response = new ClienteResponse(cliente.Id, cliente.Nome, cliente.Email, cliente.Ativo, cliente.Criado);
        return response.ToBusiness();
    }
}
```

---

## Inbound Adapter (HTTP Controller)

```csharp
// WebAPI/Adapters/Inbound/Http/Controllers/ClientesController.cs
using Microsoft.AspNetCore.Mvc;
using NomeProjeto.Application.DTOs.Requests;
using NomeProjeto.Application.Ports.Inbound;

namespace NomeProjeto.WebAPI.Adapters.Inbound.Http.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ICriarClienteUseCase _criarClienteUseCase;
    private readonly IObterClienteUseCase _obterClienteUseCase;
    private readonly IAtualizarClienteUseCase _atualizarClienteUseCase;
    private readonly IExcluirClienteUseCase _excluirClienteUseCase;

    public ClientesController(
        ICriarClienteUseCase criarClienteUseCase,
        IObterClienteUseCase obterClienteUseCase,
        IAtualizarClienteUseCase atualizarClienteUseCase,
        IExcluirClienteUseCase excluirClienteUseCase)
    {
        _criarClienteUseCase = criarClienteUseCase;
        _obterClienteUseCase = obterClienteUseCase;
        _atualizarClienteUseCase = atualizarClienteUseCase;
        _excluirClienteUseCase = excluirClienteUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _obterClienteUseCase.ExecuteAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _obterClienteUseCase.ExecuteByIdAsync(id, cancellationToken);
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CriarClienteRequest request, CancellationToken cancellationToken)
    {
        var result = await _criarClienteUseCase.ExecuteAsync(request, cancellationToken);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }
}
```

---

## Regra de Dependência

```
┌─────────────────────────────────────────────────────────────┐
│                        WebAPI                               │
│  (Inbound Adapter: HTTP Controllers)                        │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Application                             │
│  ┌─────────────────┐     ┌─────────────────┐               │
│  │ Inbound Ports   │     │ Outbound Ports  │               │
│  │ (Use Cases)     │     │ (Interfaces)    │               │
│  └────────┬────────┘     └────────┬────────┘               │
│           │                       │                         │
│           └───────────┬───────────┘                         │
│                       │                                     │
│                 Use Cases                                   │
└───────────────────────┼─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                       Domain                                │
│  (Entities, Value Objects, Domain Events, Exceptions)       │
│  SEM DEPENDÊNCIAS EXTERNAS                                  │
└─────────────────────────────────────────────────────────────┘
                        ▲
                        │
┌───────────────────────┴─────────────────────────────────────┐
│                    Infrastructure                           │
│  (Outbound Adapters: Repositories, External Services)       │
└─────────────────────────────────────────────────────────────┘
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Template Clean Architecture](template-clean-architecture.md)
- [Template DDD](template-ddd.md)
- [Estrutura Complex N-Layers](structure-complex-nlayers.md)

