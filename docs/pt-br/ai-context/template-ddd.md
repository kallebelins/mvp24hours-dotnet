# Template de Domain-Driven Design (DDD)

> **Instrução para Agente de IA**: Use este template para domínios de negócio complexos onde o modelo de domínio é central para a aplicação. DDD foca no domínio e lógica de domínio, usando linguagem ubíqua compartilhada por desenvolvedores e especialistas de domínio.

---

## Quando Usar DDD

### Cenários Recomendados
- Domínios de negócio complexos com regras intrincadas
- Projetos com envolvimento ativo de especialistas de domínio
- Aplicações corporativas de longo prazo
- Sistemas que requerem modelagem profunda de lógica de negócio
- Equipes colaborativas que valorizam linguagem ubíqua

### Não Recomendado
- Aplicações CRUD simples
- Projetos focados em tecnologia (sem domínio complexo)
- Projetos de curto prazo com escopo limitado
- Equipes sem acesso a especialistas de domínio

---

## Conceitos Principais

### Design Estratégico
- **Bounded Context**: Fronteira explícita dentro da qual um modelo de domínio existe
- **Linguagem Ubíqua**: Linguagem comum compartilhada pela equipe e especialistas
- **Context Map**: Representação visual dos relacionamentos entre contextos delimitados

### Design Tático
- **Entities**: Objetos com identidade que persiste ao longo do tempo
- **Value Objects**: Objetos imutáveis sem identidade
- **Aggregates**: Cluster de entidades e value objects com fronteira de consistência
- **Aggregate Root**: Ponto de entrada para um aggregate
- **Domain Services**: Operações que não pertencem a entidades
- **Domain Events**: Registro de algo significativo que aconteceu
- **Repositories**: Abstração para persistência de aggregates
- **Factories**: Criam aggregates complexos

---

## Estrutura de Diretórios

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Domain/
    │   ├── NomeProjeto.Domain.csproj
    │   ├── Aggregates/
    │   │   ├── ClienteAggregate/
    │   │   │   ├── Cliente.cs (Aggregate Root)
    │   │   │   ├── Contato.cs (Entity)
    │   │   │   ├── Endereco.cs (Value Object)
    │   │   │   └── TipoCliente.cs (Enum)
    │   │   └── PedidoAggregate/
    │   │       ├── Pedido.cs (Aggregate Root)
    │   │       ├── ItemPedido.cs (Entity)
    │   │       ├── StatusPedido.cs (Enum)
    │   │       └── Dinheiro.cs (Value Object)
    │   ├── Common/
    │   │   ├── Entity.cs
    │   │   ├── AggregateRoot.cs
    │   │   ├── ValueObject.cs
    │   │   └── DomainEvent.cs
    │   ├── DomainServices/
    │   │   ├── IClienteDomainService.cs
    │   │   ├── ClienteDomainService.cs
    │   │   └── IPrecificacaoService.cs
    │   ├── Events/
    │   │   ├── ClienteCriadoEvent.cs
    │   │   ├── ClienteDesativadoEvent.cs
    │   │   ├── PedidoRealizadoEvent.cs
    │   │   └── PedidoCanceladoEvent.cs
    │   ├── Exceptions/
    │   │   ├── DomainException.cs
    │   │   ├── EstadoClienteInvalidoException.cs
    │   │   └── PedidoNaoPodeCancelarException.cs
    │   ├── Repositories/
    │   │   ├── IClienteRepository.cs
    │   │   └── IPedidoRepository.cs
    │   ├── Factories/
    │   │   ├── IClienteFactory.cs
    │   │   └── IPedidoFactory.cs
    │   └── Specifications/
    │       ├── ISpecification.cs
    │       ├── ClienteAtivoSpec.cs
    │       └── PedidoPorPeriodoSpec.cs
    ├── NomeProjeto.Application/
    │   └── ...
    ├── NomeProjeto.Infrastructure/
    │   └── ...
    └── NomeProjeto.WebAPI/
        └── ...
```

---

## Blocos de Construção Comuns

### Entity Base

```csharp
// Domain/Common/Entity.cs
namespace NomeProjeto.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

### Aggregate Root

```csharp
// Domain/Common/AggregateRoot.cs
namespace NomeProjeto.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    public int Version { get; protected set; }

    protected void IncrementVersion() => Version++;
}
```

### Value Object Base

```csharp
// Domain/Common/ValueObject.cs
namespace NomeProjeto.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
```

---

## Aggregate Cliente

### Aggregate Root

```csharp
// Domain/Aggregates/ClienteAggregate/Cliente.cs
using NomeProjeto.Domain.Common;
using NomeProjeto.Domain.Events;
using NomeProjeto.Domain.Exceptions;

namespace NomeProjeto.Domain.Aggregates.ClienteAggregate;

public class Cliente : AggregateRoot<int>
{
    public string Nome { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public TipoCliente Tipo { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime Criado { get; private set; }
    public Endereco? EnderecoCobranca { get; private set; }

    private readonly List<Contato> _contatos = new();
    public IReadOnlyCollection<Contato> Contatos => _contatos.AsReadOnly();

    private Cliente() { } // EF Core

    // Factory method - encapsula lógica de criação
    public static Cliente Create(string nome, string email, TipoCliente tipo = TipoCliente.Regular)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente é obrigatório");

        var cliente = new Cliente
        {
            Nome = nome,
            Email = new Email(email),
            Tipo = tipo,
            Ativo = true,
            Criado = DateTime.UtcNow
        };

        cliente.AddDomainEvent(new ClienteCriadoEvent(cliente.Id, cliente.Nome, cliente.Email.Value));
        return cliente;
    }

    // Métodos de comportamento
    public void AtualizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome do cliente é obrigatório");
        
        Nome = nome;
        IncrementVersion();
    }

    public void AtualizarEmail(string email)
    {
        Email = new Email(email);
        IncrementVersion();
    }

    public void DefinirEnderecoCobranca(Endereco endereco)
    {
        EnderecoCobranca = endereco ?? throw new DomainException("Endereço não pode ser nulo");
        IncrementVersion();
    }

    public void PromoverParaPreferencial()
    {
        if (Tipo == TipoCliente.Preferencial)
            throw new EstadoClienteInvalidoException("Cliente já é preferencial");
        
        Tipo = TipoCliente.Preferencial;
        IncrementVersion();
    }

    public void Desativar()
    {
        if (!Ativo)
            throw new EstadoClienteInvalidoException("Cliente já está inativo");

        Ativo = false;
        AddDomainEvent(new ClienteDesativadoEvent(Id, Nome));
        IncrementVersion();
    }

    public void Ativar()
    {
        if (Ativo)
            throw new EstadoClienteInvalidoException("Cliente já está ativo");

        Ativo = true;
        IncrementVersion();
    }

    // Gerenciamento de contatos dentro da fronteira do aggregate
    public Contato AdicionarContato(string nome, string telefone, string email, TipoContato tipo)
    {
        if (_contatos.Count >= 10)
            throw new DomainException("Limite máximo de contatos atingido");

        var contato = new Contato(nome, telefone, email, tipo);
        _contatos.Add(contato);
        IncrementVersion();
        return contato;
    }

    public void RemoverContato(int contatoId)
    {
        var contato = _contatos.FirstOrDefault(c => c.Id == contatoId);
        if (contato == null)
            throw new DomainException($"Contato {contatoId} não encontrado");

        _contatos.Remove(contato);
        IncrementVersion();
    }
}
```

### Entidade dentro do Aggregate

```csharp
// Domain/Aggregates/ClienteAggregate/Contato.cs
using NomeProjeto.Domain.Common;

namespace NomeProjeto.Domain.Aggregates.ClienteAggregate;

public class Contato : Entity<int>
{
    public string Nome { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public TipoContato Tipo { get; private set; }

    private Contato() { } // EF Core

    internal Contato(string nome, string telefone, string email, TipoContato tipo)
    {
        Nome = nome;
        Telefone = telefone;
        Email = email;
        Tipo = tipo;
    }

    public void Atualizar(string nome, string telefone, string email, TipoContato tipo)
    {
        Nome = nome;
        Telefone = telefone;
        Email = email;
        Tipo = tipo;
    }
}

public enum TipoContato
{
    Principal,
    Cobranca,
    Entrega,
    Tecnico
}
```

### Value Objects

```csharp
// Domain/Aggregates/ClienteAggregate/Email.cs
using NomeProjeto.Domain.Common;
using NomeProjeto.Domain.Exceptions;

namespace NomeProjeto.Domain.Aggregates.ClienteAggregate;

public class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email é obrigatório");
        
        if (!value.Contains('@'))
            throw new DomainException("Formato de email inválido");

        Value = value.ToLowerInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(Email email) => email.Value;
}

// Domain/Aggregates/ClienteAggregate/Endereco.cs
using NomeProjeto.Domain.Common;

namespace NomeProjeto.Domain.Aggregates.ClienteAggregate;

public class Endereco : ValueObject
{
    public string Rua { get; }
    public string Cidade { get; }
    public string Estado { get; }
    public string Pais { get; }
    public string CEP { get; }

    public Endereco(string rua, string cidade, string estado, string pais, string cep)
    {
        Rua = rua ?? throw new ArgumentNullException(nameof(rua));
        Cidade = cidade ?? throw new ArgumentNullException(nameof(cidade));
        Estado = estado ?? throw new ArgumentNullException(nameof(estado));
        Pais = pais ?? throw new ArgumentNullException(nameof(pais));
        CEP = cep ?? throw new ArgumentNullException(nameof(cep));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Rua;
        yield return Cidade;
        yield return Estado;
        yield return Pais;
        yield return CEP;
    }
}

// Domain/Aggregates/ClienteAggregate/TipoCliente.cs
namespace NomeProjeto.Domain.Aggregates.ClienteAggregate;

public enum TipoCliente
{
    Regular,
    Preferencial,
    VIP
}
```

---

## Domain Events

```csharp
// Domain/Events/ClienteCriadoEvent.cs
using NomeProjeto.Domain.Common;

namespace NomeProjeto.Domain.Events;

public record ClienteCriadoEvent(int ClienteId, string Nome, string Email) : DomainEvent;

// Domain/Events/ClienteDesativadoEvent.cs
using NomeProjeto.Domain.Common;

namespace NomeProjeto.Domain.Events;

public record ClienteDesativadoEvent(int ClienteId, string Nome) : DomainEvent;

// Domain/Events/PedidoRealizadoEvent.cs
using NomeProjeto.Domain.Common;

namespace NomeProjeto.Domain.Events;

public record PedidoRealizadoEvent(int PedidoId, int ClienteId, decimal ValorTotal) : DomainEvent;
```

---

## Domain Services

```csharp
// Domain/DomainServices/IClienteDomainService.cs
using NomeProjeto.Domain.Aggregates.ClienteAggregate;

namespace NomeProjeto.Domain.DomainServices;

public interface IClienteDomainService
{
    Task<bool> IsEmailUnicoAsync(string email, int? excluirClienteId = null);
    Task<Cliente?> GetByEmailAsync(string email);
}

// Domain/DomainServices/IPrecificacaoService.cs
using NomeProjeto.Domain.Aggregates.ClienteAggregate;
using NomeProjeto.Domain.Aggregates.PedidoAggregate;

namespace NomeProjeto.Domain.DomainServices;

public interface IPrecificacaoService
{
    Dinheiro CalcularDesconto(Pedido pedido, TipoCliente tipoCliente);
    Dinheiro AplicarImposto(Dinheiro valor, string pais);
}
```

---

## Interfaces de Repositório

```csharp
// Domain/Repositories/IClienteRepository.cs
using NomeProjeto.Domain.Aggregates.ClienteAggregate;

namespace NomeProjeto.Domain.Repositories;

public interface IClienteRepository
{
    Task<Cliente?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Cliente?> GetByIdWithContatosAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Cliente>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Cliente cliente, CancellationToken cancellationToken = default);
    void Update(Cliente cliente);
    void Remove(Cliente cliente);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## Padrão Specification

```csharp
// Domain/Specifications/ISpecification.cs
using System.Linq.Expressions;

namespace NomeProjeto.Domain.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
}

// Domain/Specifications/ClienteAtivoSpec.cs
using System.Linq.Expressions;
using NomeProjeto.Domain.Aggregates.ClienteAggregate;

namespace NomeProjeto.Domain.Specifications;

public class ClienteAtivoSpec : ISpecification<Cliente>
{
    public Expression<Func<Cliente, bool>> ToExpression()
    {
        return cliente => cliente.Ativo;
    }

    public bool IsSatisfiedBy(Cliente entity)
    {
        return entity.Ativo;
    }
}
```

---

## Configuração de Entidade (EF Core)

```csharp
// Infrastructure/Persistence/Configurations/ClienteConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NomeProjeto.Domain.Aggregates.ClienteAggregate;

namespace NomeProjeto.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(100);

        // Value Object como Owned Type
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(255);
        });

        builder.OwnsOne(c => c.EnderecoCobranca, endereco =>
        {
            endereco.Property(a => a.Rua).HasColumnName("CobrancaRua").HasMaxLength(200);
            endereco.Property(a => a.Cidade).HasColumnName("CobrancaCidade").HasMaxLength(100);
            endereco.Property(a => a.Estado).HasColumnName("CobrancaEstado").HasMaxLength(50);
            endereco.Property(a => a.Pais).HasColumnName("CobrancaPais").HasMaxLength(50);
            endereco.Property(a => a.CEP).HasColumnName("CobrancaCEP").HasMaxLength(20);
        });

        builder.Property(c => c.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Entidades filhas dentro do aggregate
        builder.HasMany(c => c.Contatos)
            .WithOne()
            .HasForeignKey("ClienteId")
            .OnDelete(DeleteBehavior.Cascade);

        // Ignorar domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Template Clean Architecture](template-clean-architecture.md)
- [Template Hexagonal](template-hexagonal.md)
- [Template CQRS](template-cqrs.md)

