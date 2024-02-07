# Validação de Dados
Podemos usar dois métodos para validação de dados, usando Fluent Validation ou Data Annotations.
A validação é aplicada apenas no momento de persistir os dados.

## Fluent Validation

### Instalação
```csharp
/// Package Manager Console >
Install-Package FluentValidation -Version 10.3.5
```
### Configuração

```csharp
// CustomerValidator.cs
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Customer {PropertyName} is required.");
    }
}

/// Startup.cs
services.AddSingleton<IValidator<Customer>, CustomerValidator>();
```

## Data Annotations

### Configuração
```csharp
/// Customer.cs

// using
using System.ComponentModel.DataAnnotations;

// implementing
public class Customer : EntityBase<int>, IEntityBase
{
    public Customer()
    {
        Contacts = new List<Contact>();
    }

    [Required] // annotation
    public string Name { get; set; }

    [Required] // annotation
    public bool Active { get; set; }

    // collections

    public ICollection<Contact> Contacts { get; set; }
}

```

## Exemplo Uso

```csharp
// aplicar validação de dados ao modelo/entidade com FluentValidation ou DataAnnotation
var errors = entity.TryValidate(Validator);
if (errors.AnySafe())
{
    return errors.ToBusiness<int>();
}

// executar a ação de criação no banco de dados

```