# Data Validation
We can use two methods for data validation, using Fluent Validation or Data Annotations.
Validation is only applied when data is persisted.

## Fluent Validation

### Setup
```csharp
/// Package Manager Console >
Install-Package FluentValidation -Version 11.9.x
```

### Settings

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

### Settings
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

## Example Usage

```csharp
// apply data validation to model/entity with FluentValidation or DataAnnotation
var errors = entity.TryValidate(Validator);
if (errors.AnySafe())
{
    return errors.ToBusiness<int>();
}

// perform the create action on the database
```

---

## CQRS ValidationBehavior

When using CQRS pattern, you can use `ValidationBehavior` for automatic validation of commands and queries:

### Setup

```csharp
/// Program.cs
builder.Services.AddMvp24HoursCqrs(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddValidationBehavior(); // Enable automatic validation
});
```

### Command Validator

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

### Usage

```csharp
// Validation is automatic when sending command
var result = await _mediator.Send(new CreateOrderCommand(customerId, items));

// If validation fails, ValidationException is thrown
// Configure exception handling middleware to return ProblemDetails
```

> 📚 See [CQRS Validation Behavior](cqrs/validation-behavior.md) for complete documentation.

---

## IValidationService

For application layer validation, use `IValidationService<T>`:

```csharp
public interface IValidationService<T>
{
    ValidationResult Validate(T instance);
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}

// Usage in Application Service
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
        
        // ... create customer
    }
}
```

---

## Cascade Validation

For nested entities, use cascade validation:

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Customer)
            .NotNull()
            .SetValidator(new CustomerValidator()); // Cascade validation
            
        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator()); // Validate each item
    }
}
```

---

## Related Documentation

- [CQRS Validation Behavior](cqrs/validation-behavior.md) - Automatic validation in CQRS
- [Application Services](application-services.md) - Validation in application layer
