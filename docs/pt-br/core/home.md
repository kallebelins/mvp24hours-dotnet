# Mvp24Hours.Core

O módulo Core é a base fundamental do framework Mvp24Hours. Ele contém abstrações essenciais, contratos, Value Objects, padrões DDD e utilitários utilizados por todos os outros módulos.

## O Que Está Incluído

| Categoria | Componentes |
|-----------|-------------|
| **Value Objects** | Email, Cpf, Cnpj, Money, Address, DateRange, Percentage, PhoneNumber |
| **Guard Clauses** | Utilitários de programação defensiva para validação de argumentos |
| **IDs Fortemente Tipados** | EntityId, GuidEntityId, IntEntityId, LongEntityId, StringEntityId |
| **Padrões Funcionais** | Maybe&lt;T&gt;, Either&lt;TLeft, TRight&gt; |
| **Smart Enums** | Classe base Enumeration&lt;T&gt; para enumerações ricas |
| **Interfaces de Entidade** | IEntity, IAuditableEntity, ISoftDeletable, ITenantEntity, IVersionedEntity |
| **Infraestrutura** | IClock, IGuidGenerator com implementações amigáveis para testes |
| **Exceções** | BusinessException, ValidationException, NotFoundException, e mais |

## Instalação

O módulo Core é incluído quando você instala qualquer pacote Mvp24Hours:

```bash
dotnet add package Mvp24Hours.Core
```

## Exemplos Rápidos

### Guard Clauses

```csharp
using Mvp24Hours.Core.Helpers;

public class OrderService
{
    public void CreateOrder(string customerId, decimal amount)
    {
        Guard.Against.NullOrEmpty(customerId, nameof(customerId));
        Guard.Against.NegativeOrZero(amount, nameof(amount));
        
        // Sua lógica aqui...
    }
}
```

### Value Objects

```csharp
using Mvp24Hours.Core.ValueObjects;

// Email com validação
var email = Email.Create("usuario@exemplo.com");
Console.WriteLine(email.Domain); // exemplo.com

// CPF com validação
var cpf = Cpf.Create("123.456.789-09");
Console.WriteLine(cpf.Formatted); // 123.456.789-09

// Money com moeda
var preco = Money.Create(99.99m, "BRL");
var total = preco * 2; // R$ 199,98
```

### Maybe&lt;T&gt; para Segurança contra Null

```csharp
using Mvp24Hours.Core.ValueObjects.Functional;

public Maybe<Customer> FindCustomer(string id)
{
    var customer = _repository.GetById(id);
    return Maybe.From(customer);
}

// Uso
var resultado = FindCustomer("123")
    .Map(c => c.Name)
    .ValueOr("Desconhecido");
```

### Exceções

```csharp
using Mvp24Hours.Core.Exceptions;

if (conta.Saldo < valor)
{
    throw new BusinessException(
        "Saldo insuficiente", 
        "SALDO_INSUFICIENTE"
    );
}

if (pedido == null)
{
    throw new NotFoundException("Pedido", pedidoId);
}
```

## Documentação

- [Guard Clauses](guard-clauses.md) - Programação defensiva
- [Value Objects](value-objects.md) - Email, CPF, CNPJ, Money, etc.
- [IDs Fortemente Tipados](strongly-typed-ids.md) - Identificadores type-safe
- [Padrões Funcionais](functional-patterns.md) - Maybe, Either
- [Smart Enums](smart-enums.md) - Padrão Enumeration
- [Interfaces de Entidade](entity-interfaces.md) - IEntity, IAuditable, etc.
- [Infraestrutura](infrastructure-abstractions.md) - IClock, IGuidGenerator
- [Exceções](exceptions.md) - Hierarquia de exceções

