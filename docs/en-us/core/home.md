# Mvp24Hours.Core

The Core module is the foundation of the Mvp24Hours framework. It contains essential abstractions, contracts, Value Objects, DDD patterns, and utilities used by all other modules.

## What's Included

| Category | Components |
|----------|------------|
| **Value Objects** | Email, Cpf, Cnpj, Money, Address, DateRange, Percentage, PhoneNumber |
| **Guard Clauses** | Defensive programming utilities for argument validation |
| **Strongly-Typed IDs** | EntityId, GuidEntityId, IntEntityId, LongEntityId, StringEntityId |
| **Functional Patterns** | Maybe&lt;T&gt;, Either&lt;TLeft, TRight&gt; |
| **Smart Enums** | Enumeration&lt;T&gt; base class for rich enumerations |
| **Entity Interfaces** | IEntity, IAuditableEntity, ISoftDeletable, ITenantEntity, IVersionedEntity |
| **Infrastructure** | IClock, IGuidGenerator with test-friendly implementations |
| **Exceptions** | BusinessException, ValidationException, NotFoundException, and more |

## Installation

The Core module is included when you install any Mvp24Hours package:

```bash
dotnet add package Mvp24Hours.Core
```

## Quick Examples

### Guard Clauses

```csharp
using Mvp24Hours.Core.Helpers;

public class OrderService
{
    public void CreateOrder(string customerId, decimal amount)
    {
        Guard.Against.NullOrEmpty(customerId, nameof(customerId));
        Guard.Against.NegativeOrZero(amount, nameof(amount));
        
        // Your logic here...
    }
}
```

### Value Objects

```csharp
using Mvp24Hours.Core.ValueObjects;

// Email with validation
var email = Email.Create("user@example.com");
Console.WriteLine(email.Domain); // example.com

// CPF (Brazilian document) with validation
var cpf = Cpf.Create("123.456.789-09");
Console.WriteLine(cpf.Formatted); // 123.456.789-09

// Money with currency
var price = Money.Create(99.99m, "USD");
var total = price * 2; // $199.98
```

### Maybe&lt;T&gt; for Null Safety

```csharp
using Mvp24Hours.Core.ValueObjects.Functional;

public Maybe<Customer> FindCustomer(string id)
{
    var customer = _repository.GetById(id);
    return Maybe.From(customer);
}

// Usage
var result = FindCustomer("123")
    .Map(c => c.Name)
    .ValueOr("Unknown");
```

### Exceptions

```csharp
using Mvp24Hours.Core.Exceptions;

if (account.Balance < amount)
{
    throw new BusinessException(
        "Insufficient balance", 
        "INSUFFICIENT_BALANCE"
    );
}

if (order == null)
{
    throw new NotFoundException("Order", orderId);
}
```

## Documentation

- [Guard Clauses](en-us/core/guard-clauses.md) - Defensive programming
- [Value Objects](en-us/core/value-objects.md) - Email, CPF, CNPJ, Money, etc.
- [Strongly-Typed IDs](en-us/core/strongly-typed-ids.md) - Type-safe identifiers
- [Functional Patterns](en-us/core/functional-patterns.md) - Maybe, Either
- [Smart Enums](en-us/core/smart-enums.md) - Enumeration pattern
- [Entity Interfaces](en-us/core/entity-interfaces.md) - IEntity, IAuditable, etc.
- [Infrastructure](en-us/core/infrastructure-abstractions.md) - IClock, IGuidGenerator
- [Exceptions](en-us/core/exceptions.md) - Exception hierarchy

