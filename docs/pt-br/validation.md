# Validação de Dados
Podemos usar dois métodos para validação de dados, usando Fluent Validation ou Data Annotations.
A validação é aplicada apenas no momento de persistir os dados.

## Fluent Validation

### Instalação
```csharp
/// Package Manager Console >
Install-Package FluentValidation -Version 11.9.x
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

/// Program.cs
builder.Services.AddSingleton<IValidator<Customer>, CustomerValidator>();
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

## Exemplo de Uso

```csharp
// aplicar validação de dados ao modelo/entidade com FluentValidation ou DataAnnotation
var errors = entity.TryValidate(Validator);
if (errors.AnySafe())
{
    return errors.ToBusiness<int>();
}

// executar a ação de criação no banco de dados
```

---

## CQRS ValidationBehavior

Ao usar o padrão CQRS, você pode usar `ValidationBehavior` para validação automática de commands e queries:

### Configuração

```csharp
/// Program.cs
builder.Services.AddMvp24HoursCqrs(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddValidationBehavior(); // Habilita validação automática
});
```

### Validador de Command

```csharp
// CreateOrderCommand.cs
public record CreateOrderCommand(string CustomerId, List<OrderItem> Items) 
    : ICommand<OrderResult>;

// CreateOrderCommandValidator.cs
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required.");
            
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must have at least one item.");
            
        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}

// OrderItemValidator.cs
public class OrderItemValidator : AbstractValidator<OrderItem>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty();
            
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}
```

### Uso

```csharp
// A validação é automática ao enviar o command
var result = await _mediator.Send(new CreateOrderCommand(customerId, items));

// Se a validação falhar, ValidationException é lançada
// Configure o middleware de exceção para retornar ProblemDetails
```

> 📚 Consulte [CQRS Validation Behavior](cqrs/validation-behavior.md) para documentação completa.

---

## IValidationService

Para validação na camada de aplicação, use `IValidationService<T>`:

```csharp
public interface IValidationService<T>
{
    ValidationResult Validate(T instance);
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

// Uso em Application Service
public class CustomerApplicationService
{
    private readonly IValidationService<CreateCustomerDto> _validator;
    
    public CustomerApplicationService(IValidationService<CreateCustomerDto> validator)
    {
        _validator = validator;
    }
    
    public async Task<IBusinessResult<int>> CreateAsync(CreateCustomerDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return validationResult.Errors.ToBusiness<int>();
        }
        
        // ... criar cliente
    }
}
```

---

## Validação em Cascata

Para entidades aninhadas, use validação em cascata:

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Customer)
            .NotNull()
            .SetValidator(new CustomerValidator()); // Validação em cascata
            
        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator()); // Valida cada item
    }
}
```

---

## Documentação Relacionada

- [CQRS Validation Behavior](cqrs/validation-behavior.md) - Validação automática em CQRS
- [Application Services](application-services.md) - Validação na camada de aplicação
