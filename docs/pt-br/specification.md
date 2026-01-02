# Especificação (Specification Pattern)
O padrão de especificação usamos como requisitos para filtros de pesquisa. Cada especificação deve ser criada com o objetivo de definir casos concretos. Cada especificação poderá fazer parte de uma composição ou ser aplicada individualmente.

>Na programação de computadores, o padrão de especificação é um padrão de design de software específico, por meio do qual as regras de negócios podem ser recombinadas encadeando as regras de negócios usando a lógica booleana. O padrão é freqüentemente usado no contexto de design orientado a domínio. [Wikipédia](https://en.wikipedia.org/wiki/Specification_pattern)

## Exemplo Básico
Neste exemplo criamos uma especificação que filtra pessoas que tenham número de celular no registro de contato.

```csharp
/// CustomerHasCellContactSpec.cs

public class CustomerHasCellContactSpec : ISpecificationQuery<Customer>
{
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => x => x.Contacts.Any(y => y.Type == ContactType.CellPhone);
}

/// CustomerService.cs -> Get Method

Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter.And<Customer, CustomerHasCellContactSpec>();
var paging = new PagingCriteriaExpression<Customer>(3, 0);
paging.NavigationExpr.Add(x => x.Contacts);
var boResult = service.GetBy(filter, paging);
```

## Composição de Especificações

Especificações podem ser compostas usando operadores lógicos `And`, `Or` e `Not`:

### Usando Operador And

```csharp
// Combinar múltiplas especificações com AND
Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter
    .And<Customer, CustomerHasCellContactSpec>()
    .And<Customer, CustomerHasEmailSpec>();
```

### Usando Operador Or

```csharp
// Combinar especificações com OR
Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter.Or<Customer, CustomerIsVIPSpec>();
```

### Usando Operador Not

```csharp
// Negar uma especificação
Expression<Func<Customer, bool>> filter = x => x.Active;
filter = filter.Not<Customer, CustomerIsBlockedSpec>();
```

## Classe Base Specification

Para especificações mais complexas, use a classe base `Specification<T>`:

```csharp
public class ActiveCustomerWithContactsSpec : Specification<Customer>
{
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Active && customer.Contacts.Any();
    }
}

// Uso
var spec = new ActiveCustomerWithContactsSpec();
var customers = await repository.GetByAsync(spec.ToExpression());
```

## SpecificationEvaluator para EF Core

Use `SpecificationEvaluator<T>` para consultas avançadas com EF Core:

```csharp
public class CustomerByStatusSpec : Specification<Customer>
{
    private readonly CustomerStatus _status;
    
    public CustomerByStatusSpec(CustomerStatus status)
    {
        _status = status;
    }
    
    public override Expression<Func<Customer, bool>> ToExpression()
    {
        return customer => customer.Status == _status;
    }
}

// Com SpecificationEvaluator
var spec = new CustomerByStatusSpec(CustomerStatus.Active);
var query = SpecificationEvaluator<Customer>.GetQuery(dbContext.Customers, spec);
var results = await query.ToListAsync();
```

## Especificações Parametrizadas

Crie especificações reutilizáveis com parâmetros:

```csharp
public class CustomerByNameSpec : ISpecificationQuery<Customer>
{
    private readonly string _name;
    
    public CustomerByNameSpec(string name)
    {
        _name = name;
    }
    
    public Expression<Func<Customer, bool>> IsSatisfiedByExpression => 
        x => x.Name.Contains(_name);
}

// Uso
var spec = new CustomerByNameSpec("John");
var filter = Expression<Func<Customer, bool>>.And(
    x => x.Active,
    spec.IsSatisfiedByExpression);
```

---

## Documentação Relacionada

- [CQRS Specifications](cqrs/specifications.md) - Usando especificações com padrão CQRS
- [Repository Pattern](database/use-repository.md) - Repositório com especificações
