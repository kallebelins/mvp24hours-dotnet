# Data Validation
We can use two methods for data validation, using Fluent Validation or Data Annotations.
Validation is only applied when data is persisted.

## Fluent Validation

### Setup
```csharp
/// Package Manager Console >
Install-Package FluentValidation -Version 10.3.5
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

/// Startup.cs
services.AddSingleton<IValidator<Customer>, CustomerValidator>();
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