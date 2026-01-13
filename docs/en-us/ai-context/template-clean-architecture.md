# Clean Architecture Template

> **AI Agent Instruction**: Use this template for enterprise applications requiring clear separation of concerns, testability, and independence from frameworks. Clean Architecture follows the Dependency Rule where dependencies point inward.

---

## When to Use Clean Architecture

### Recommended Scenarios
- Enterprise applications with complex business rules
- Long-term maintainable systems
- Projects requiring high testability
- Applications that need to be framework-agnostic
- Teams practicing TDD/BDD

### Not Recommended
- Simple CRUD applications
- Small projects with limited scope
- Tight deadlines with simple requirements
- Teams unfamiliar with layered architectures

---

## The Dependency Rule

Dependencies must point inward. Nothing in an inner circle can know anything about something in an outer circle.

```
┌──────────────────────────────────────────────────────────────────┐
│                    Frameworks & Drivers                          │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │                   Interface Adapters                       │ │
│  │  ┌──────────────────────────────────────────────────────┐ │ │
│  │  │                Application Business Rules            │ │ │
│  │  │  ┌────────────────────────────────────────────────┐ │ │ │
│  │  │  │            Enterprise Business Rules           │ │ │ │
│  │  │  │                   (Entities)                   │ │ │ │
│  │  │  └────────────────────────────────────────────────┘ │ │ │
│  │  │                    (Use Cases)                       │ │ │
│  │  └──────────────────────────────────────────────────────┘ │ │
│  │              (Controllers, Presenters, Gateways)          │ │
│  └────────────────────────────────────────────────────────────┘ │
│                      (Web, UI, DB, External)                     │
└──────────────────────────────────────────────────────────────────┘
```

---

## Directory Structure

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Domain/
    │   ├── ProjectName.Domain.csproj
    │   ├── Entities/
    │   │   ├── Customer.cs
    │   │   ├── Order.cs
    │   │   └── Product.cs
    │   ├── ValueObjects/
    │   │   ├── Email.cs
    │   │   ├── Money.cs
    │   │   └── Address.cs
    │   ├── Enums/
    │   │   ├── OrderStatus.cs
    │   │   └── CustomerType.cs
    │   ├── Events/
    │   │   ├── DomainEvent.cs
    │   │   ├── CustomerCreatedEvent.cs
    │   │   └── OrderPlacedEvent.cs
    │   ├── Exceptions/
    │   │   ├── DomainException.cs
    │   │   └── EntityNotFoundException.cs
    │   └── Common/
    │       ├── Entity.cs
    │       ├── AggregateRoot.cs
    │       └── IHasDomainEvents.cs
    ├── ProjectName.Application/
    │   ├── ProjectName.Application.csproj
    │   ├── Common/
    │   │   ├── Interfaces/
    │   │   │   ├── IApplicationDbContext.cs
    │   │   │   ├── ICurrentUserService.cs
    │   │   │   ├── IDateTimeService.cs
    │   │   │   └── IEmailService.cs
    │   │   ├── Behaviors/
    │   │   │   ├── ValidationBehavior.cs
    │   │   │   ├── LoggingBehavior.cs
    │   │   │   └── PerformanceBehavior.cs
    │   │   ├── Mappings/
    │   │   │   └── MappingProfile.cs
    │   │   └── Models/
    │   │       ├── Result.cs
    │   │       └── PaginatedList.cs
    │   ├── Customers/
    │   │   ├── Commands/
    │   │   │   ├── CreateCustomer/
    │   │   │   │   ├── CreateCustomerCommand.cs
    │   │   │   │   ├── CreateCustomerCommandHandler.cs
    │   │   │   │   └── CreateCustomerCommandValidator.cs
    │   │   │   ├── UpdateCustomer/
    │   │   │   │   ├── UpdateCustomerCommand.cs
    │   │   │   │   ├── UpdateCustomerCommandHandler.cs
    │   │   │   │   └── UpdateCustomerCommandValidator.cs
    │   │   │   └── DeleteCustomer/
    │   │   │       ├── DeleteCustomerCommand.cs
    │   │   │       └── DeleteCustomerCommandHandler.cs
    │   │   ├── Queries/
    │   │   │   ├── GetCustomerById/
    │   │   │   │   ├── GetCustomerByIdQuery.cs
    │   │   │   │   ├── GetCustomerByIdQueryHandler.cs
    │   │   │   │   └── CustomerDto.cs
    │   │   │   └── GetCustomers/
    │   │   │       ├── GetCustomersQuery.cs
    │   │   │       ├── GetCustomersQueryHandler.cs
    │   │   │       └── CustomerBriefDto.cs
    │   │   └── EventHandlers/
    │   │       └── CustomerCreatedEventHandler.cs
    │   └── Orders/
    │       ├── Commands/
    │       └── Queries/
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   ├── Persistence/
    │   │   ├── ApplicationDbContext.cs
    │   │   ├── ApplicationDbContextInitializer.cs
    │   │   └── Configurations/
    │   │       ├── CustomerConfiguration.cs
    │   │       └── OrderConfiguration.cs
    │   ├── Services/
    │   │   ├── DateTimeService.cs
    │   │   └── EmailService.cs
    │   └── DependencyInjection.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Controllers/
        │   ├── CustomersController.cs
        │   └── OrdersController.cs
        ├── Filters/
        │   └── ApiExceptionFilterAttribute.cs
        ├── Services/
        │   └── CurrentUserService.cs
        └── DependencyInjection.cs
```

---

## Namespaces

```csharp
// Domain (innermost - no dependencies)
ProjectName.Domain.Entities
ProjectName.Domain.ValueObjects
ProjectName.Domain.Enums
ProjectName.Domain.Events
ProjectName.Domain.Exceptions
ProjectName.Domain.Common

// Application (depends only on Domain)
ProjectName.Application.Common.Interfaces
ProjectName.Application.Common.Behaviors
ProjectName.Application.Common.Mappings
ProjectName.Application.Common.Models
ProjectName.Application.Customers.Commands.CreateCustomer
ProjectName.Application.Customers.Queries.GetCustomerById
ProjectName.Application.Customers.EventHandlers

// Infrastructure (implements Application interfaces)
ProjectName.Infrastructure.Persistence
ProjectName.Infrastructure.Services

// WebAPI (outermost)
ProjectName.WebAPI.Controllers
ProjectName.WebAPI.Filters
ProjectName.WebAPI.Services
```

---

## Project Files (.csproj)

### Domain Project (No External Dependencies)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Domain has NO external dependencies -->
</Project>
```

### Application Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProjectName.Domain\ProjectName.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Core" Version="9.*" />
    <PackageReference Include="FluentValidation" Version="11.*" />
    <PackageReference Include="MediatR" Version="12.*" />
    <PackageReference Include="AutoMapper" Version="13.*" />
  </ItemGroup>
</Project>
```

### Infrastructure Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProjectName.Application\ProjectName.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.*">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

---

## Domain Layer Templates

### Base Entity

```csharp
// Domain/Common/Entity.cs
namespace ProjectName.Domain.Common;

public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other)
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
        
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
```

### Aggregate Root with Domain Events

```csharp
// Domain/Common/IHasDomainEvents.cs
namespace ProjectName.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

// Domain/Common/AggregateRoot.cs
namespace ProjectName.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

### Domain Event Base

```csharp
// Domain/Events/DomainEvent.cs
using MediatR;

namespace ProjectName.Domain.Events;

public abstract record DomainEvent : INotification
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

### Customer Entity

```csharp
// Domain/Entities/Customer.cs
using ProjectName.Domain.Common;
using ProjectName.Domain.Events;
using ProjectName.Domain.ValueObjects;

namespace ProjectName.Domain.Entities;

public class Customer : AggregateRoot<int>
{
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public bool Active { get; private set; }
    public DateTime Created { get; private set; }

    private readonly List<Order> _orders = new();
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

    private Customer() { } // EF Core

    public static Customer Create(string name, string email)
    {
        var customer = new Customer
        {
            Name = name,
            Email = new Email(email),
            Active = true,
            Created = DateTime.UtcNow
        };

        customer.AddDomainEvent(new CustomerCreatedEvent(customer));

        return customer;
    }

    public void Update(string name, string email)
    {
        Name = name;
        Email = new Email(email);
    }

    public void Activate() => Active = true;
    public void Deactivate() => Active = false;
}
```

### Domain Event

```csharp
// Domain/Events/CustomerCreatedEvent.cs
using ProjectName.Domain.Entities;

namespace ProjectName.Domain.Events;

public record CustomerCreatedEvent(Customer Customer) : DomainEvent;
```

---

## Application Layer Templates

### Database Context Interface

```csharp
// Application/Common/Interfaces/IApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using ProjectName.Domain.Entities;

namespace ProjectName.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<Order> Orders { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

### Command with Handler and Validator

```csharp
// Application/Customers/Commands/CreateCustomer/CreateCustomerCommand.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace ProjectName.Application.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(string Name, string Email) : IRequest<IBusinessResult<int>>;

// Application/Customers/Commands/CreateCustomer/CreateCustomerCommandValidator.cs
using FluentValidation;

namespace ProjectName.Application.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

// Application/Customers/Commands/CreateCustomer/CreateCustomerCommandHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Application.Common.Interfaces;
using ProjectName.Domain.Entities;

namespace ProjectName.Application.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, IBusinessResult<int>>
{
    private readonly IApplicationDbContext _context;

    public CreateCustomerCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IBusinessResult<int>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.Name, request.Email);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        return customer.Id.ToBusiness();
    }
}
```

### Query with Handler

```csharp
// Application/Customers/Queries/GetCustomerById/GetCustomerByIdQuery.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace ProjectName.Application.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(int Id) : IRequest<IBusinessResult<CustomerDto>>;

// Application/Customers/Queries/GetCustomerById/CustomerDto.cs
namespace ProjectName.Application.Customers.Queries.GetCustomerById;

public record CustomerDto(int Id, string Name, string Email, bool Active, DateTime Created);

// Application/Customers/Queries/GetCustomerById/GetCustomerByIdQueryHandler.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Application.Common.Interfaces;

namespace ProjectName.Application.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, IBusinessResult<CustomerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCustomerByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IBusinessResult<CustomerDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _context.Customers
            .Where(c => c.Id == request.Id)
            .Select(c => new CustomerDto(c.Id, c.Name, c.Email.Value, c.Active, c.Created))
            .FirstOrDefaultAsync(cancellationToken);

        if (customer == null)
            return default(CustomerDto).ToBusiness("Customer not found");

        return customer.ToBusiness();
    }
}
```

### Validation Behavior

```csharp
// Application/Common/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace ProjectName.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Domain Event Handler

```csharp
// Application/Customers/EventHandlers/CustomerCreatedEventHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using ProjectName.Application.Common.Interfaces;
using ProjectName.Domain.Events;

namespace ProjectName.Application.Customers.EventHandlers;

public class CustomerCreatedEventHandler : INotificationHandler<CustomerCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<CustomerCreatedEventHandler> _logger;

    public CustomerCreatedEventHandler(
        IEmailService emailService,
        ILogger<CustomerCreatedEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(CustomerCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer created: {CustomerId}", notification.Customer.Id);

        await _emailService.SendWelcomeEmailAsync(
            notification.Customer.Email.Value,
            notification.Customer.Name,
            cancellationToken);
    }
}
```

---

## Infrastructure Layer Templates

### Application DbContext

```csharp
// Infrastructure/Persistence/ApplicationDbContext.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectName.Application.Common.Interfaces;
using ProjectName.Domain.Common;
using ProjectName.Domain.Entities;

namespace ProjectName.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IMediator _mediator;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMediator mediator)
        : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entities = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}
```

### Dependency Injection

```csharp
// Infrastructure/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Application.Common.Interfaces;
using ProjectName.Infrastructure.Persistence;
using ProjectName.Infrastructure.Services;

namespace ProjectName.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddTransient<IDateTimeService, DateTimeService>();
        services.AddTransient<IEmailService, EmailService>();

        return services;
    }
}
```

---

## WebAPI Layer Templates

### Controller

```csharp
// WebAPI/Controllers/CustomersController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Application.Customers.Commands.CreateCustomer;
using ProjectName.Application.Customers.Commands.UpdateCustomer;
using ProjectName.Application.Customers.Commands.DeleteCustomer;
using ProjectName.Application.Customers.Queries.GetCustomerById;
using ProjectName.Application.Customers.Queries.GetCustomers;

namespace ProjectName.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetCustomersQuery query)
    {
        var result = await _sender.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _sender.Send(new GetCustomerByIdQuery(id));
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await _sender.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _sender.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _sender.Send(new DeleteCustomerCommand(id));
        if (result.HasErrors)
            return NotFound(result);
        return NoContent();
    }
}
```

### WebAPI Dependency Injection

```csharp
// WebAPI/DependencyInjection.cs
using FluentValidation;
using MediatR;
using ProjectName.Application.Common.Behaviors;
using ProjectName.Application.Customers.Commands.CreateCustomer;
using ProjectName.WebAPI.Services;

namespace ProjectName.WebAPI;

public static class DependencyInjection
{
    public static IServiceCollection AddWebAPIServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(CreateCustomerCommand).Assembly);
        });

        // Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Validators
        services.AddValidatorsFromAssembly(typeof(CreateCustomerCommandValidator).Assembly);

        // Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
```

---

## Program.cs

```csharp
using ProjectName.Infrastructure;
using ProjectName.WebAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWebAPIServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Hexagonal Architecture Template](template-hexagonal.md)
- [DDD Template](template-ddd.md)
- [CQRS Template](template-cqrs.md)

