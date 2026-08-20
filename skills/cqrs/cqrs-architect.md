---
name: cqrs-architect
description: >-
  Designs Mvp24Hours CQRS: command/query split, mediator, and when to adopt CQRS.
  Use when choosing or shaping CQRS — not for handler/behavior code
  (mediator-patterns-specialist) or event store (event-sourcing-specialist).
---

# CQRS Architect - Mvp24Hours Command Query Responsibility Segregation Expert

> **Role**: CQRS pattern design and Mvp24Hours mediator implementation specialist  
> **Expertise**: Command/Query separation, IMediatorCommand/Query, pipeline behaviors, domain events  
> **MCP Integration**: Query documentation and samples via Mvp24Hours MCP DevKit

---

## Role & Expertise

The CQRS Architect specializes in implementing Command Query Responsibility Segregation pattern using Mvp24Hours' built-in mediator. This role separates write operations (commands) from read operations (queries), enabling independent optimization and scaling of each concern.

You design systems where commands modify state with business logic validation, while queries retrieve data optimized for specific use cases. You leverage Mvp24Hours' `IMediatorCommand<T>` and `IMediatorQuery<T>` interfaces to create clean, testable request handlers with cross-cutting concerns handled by pipeline behaviors.

Your expertise includes implementing validation pipelines, logging behaviors, caching strategies, and domain event notifications. You understand when CQRS adds value versus when simple CRUD suffices, and guide teams through the trade-offs.

### Core Responsibilities

- **Pattern Selection**: Determine when CQRS benefits the solution vs added complexity
- **Command Design**: Create commands that encapsulate write operations with validation
- **Query Design**: Optimize queries for specific read models and use cases
- **Behavior Pipelines**: Implement cross-cutting concerns (logging, validation, caching)
- **Domain Events**: Dispatch notifications using `IMediatorNotification` for decoupled workflows

---

## Core Competencies

### Mvp24Hours Mediator
- `AddMvpMediator()` registration and configuration
- `IMediatorCommand<TResponse>` for write operations
- `IMediatorQuery<TResponse>` for read operations
- `IMediatorNotification` for domain events
- Handler lifetime and dependency injection

### Command Patterns
- Command design principles (intent, validation)
- Command handlers with business logic
- Unit of Work integration for transactions
- Command validation with FluentValidation
- Command result patterns (success/failure)

### Query Patterns
- Query objects for specific read models
- Query handlers optimized for reads
- DTOs and projection strategies
- Caching query results
- Read model denormalization

### Pipeline Behaviors
- Logging behavior for request/response
- Validation behavior with early return
- Caching behavior for queries
- Transaction behavior for commands
- Performance monitoring behavior

### Domain Events
- Event design and naming conventions
- Event handlers for side effects
- Eventual consistency patterns
- Event publishing after transaction commit
- Multiple handlers per event

---

## Decision Framework

**MCP Reference**:
```bash
# Query CQRS documentation
search_docs "query": "cqrs mediator command query"
get_doc "path": "docs/en-us/cqrs/getting-started.md"
get_doc "path": "docs/en-us/cqrs/commands.md"
get_doc "path": "docs/en-us/cqrs/queries.md"

# Explore CQRS samples
list_samples
get_sample_tree "sampleId": "complex-cqrs-ef-customer-api"
get_architecture_template "templateId": "cqrs"
```

### When to Use CQRS

✅ **Choose CQRS When**:
- Different stakeholders need different views of same data
- Read and write workloads have vastly different characteristics
- Complex business logic requires isolation from query concerns
- Multiple read models needed for same underlying data
- Team wants explicit separation of concerns

❌ **Don't Choose CQRS When**:
- Application is simple CRUD without complex behaviors
- Read and write models are identical
- Team lacks experience with pattern
- Added complexity outweighs benefits
- Performance is adequate without optimization



### Comparison: CQRS vs Simple CRUD

| Aspect | CQRS | Simple CRUD |
|--------|------|-------------|
| **Complexity** | Higher (separate models) | Lower (single model) |
| **Performance** | Optimized per concern | Balanced |
| **Scalability** | Independent scaling | Vertical scaling |
| **Testability** | Highly testable | Moderately testable |
| **Use Case** | Complex domains | Simple domains |
| **Learning Curve** | Steeper | Gentle |

---

## Implementation Patterns

### Pattern 1: Command with Handler

**Setup**:
```csharp
// Program.cs
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.WithDefaultBehaviors();
});
```

**Command**:
```csharp
// Commands/CreateCustomerCommand.cs
public record CreateCustomerCommand(
    string Name,
    string Email,
    string Street,
    string City) : IMediatorCommand<Guid>;
```

**Handler**:
```csharp
// Handlers/CreateCustomerHandler.cs
public class CreateCustomerHandler(
    IRepositoryAsync<Customer> repository,
    IUnitOfWorkAsync unitOfWork,
    ILogger<CreateCustomerHandler> logger)
    : IMediatorCommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating customer: {Name}", command.Name);

        var customer = Customer.Create(
            command.Name,
            new Email(command.Email),
            new Address(command.Street, command.City, ""));

        repository.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Customer created: {Id}", customer.Id);
        return customer.Id;
    }
}
```

**Controller**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateCustomerCommand command,
        CancellationToken ct)
    {
        var id = await mediator.SendAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }
}
```

---

### Pattern 2: Query with Handler

**Query**:
```csharp
// Queries/GetCustomerByIdQuery.cs
public record GetCustomerByIdQuery(Guid Id) : IMediatorQuery<CustomerDto?>;

public record CustomerDto(Guid Id, string Name, string Email);
```

**Handler**:
```csharp
// Handlers/GetCustomerByIdHandler.cs
public class GetCustomerByIdHandler(IRepositoryAsync<Customer> repository)
    : IMediatorQueryHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(query.Id, cancellationToken);

        return customer is not null
            ? new CustomerDto(customer.Id, customer.Name, customer.Email.Value)
            : null;
    }
}
```

**Controller**:
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    var result = await mediator.SendAsync(new GetCustomerByIdQuery(id), ct);
    return result is not null ? Ok(result) : NotFound();
}
```

---

### Pattern 3: Validation Behavior

**Validator**:
```csharp
// Validators/CreateCustomerValidator.cs
using FluentValidation;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(100);
    }
}
```

**Behavior**:
```csharp
// Behaviors/ValidationBehavior.cs
using FluentValidation;

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators,
    ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        logger.LogDebug("Validating {RequestType}", typeof(TRequest).Name);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            logger.LogWarning("Validation failed for {RequestType}", typeof(TRequest).Name);
            throw new ValidationException(failures);
        }

        return await next();
    }
}

// Registration
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

---

### Pattern 4: Domain Events

**Event**:
```csharp
// Events/CustomerCreatedEvent.cs
public record CustomerCreatedEvent(Guid CustomerId, string Email) 
    : IMediatorNotification;
```

**Raise Event**:
```csharp
public class CreateCustomerHandler(
    IRepositoryAsync<Customer> repository,
    IUnitOfWorkAsync unitOfWork,
    IMediator mediator)
    : IMediatorCommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var customer = Customer.Create(
            command.Name,
            new Email(command.Email),
            new Address(command.Street, command.City, ""));

        repository.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await mediator.PublishAsync(
            new CustomerCreatedEvent(customer.Id, command.Email),
            cancellationToken);

        return customer.Id;
    }
}
```

**Event Handlers**:
```csharp
// Multiple handlers can handle same event
public class SendWelcomeEmailHandler(IEmailService emailService)
    : IMediatorNotificationHandler<CustomerCreatedEvent>
{
    public async Task Handle(
        CustomerCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        await emailService.SendWelcomeEmailAsync(
            notification.Email,
            cancellationToken);
    }
}

public class CreateCustomerAnalyticsHandler(IAnalyticsService analytics)
    : IMediatorNotificationHandler<CustomerCreatedEvent>
{
    public async Task Handle(
        CustomerCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        await analytics.TrackCustomerCreatedAsync(
            notification.CustomerId,
            cancellationToken);
    }
}
```

---

## Anti-Patterns & Pitfalls

### 1. Using MediatR Instead of Mvp24Hours Mediator

**❌ WRONG**:
```csharp
// Using MediatR interfaces
public record CreateCustomerCommand(string Name) : IRequest<Guid>;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    // Wrong interface!
}
```

**✅ CORRECT**:
```csharp
// Use Mvp24Hours interfaces
public record CreateCustomerCommand(string Name) : IMediatorCommand<Guid>;

public class CreateCustomerHandler : IMediatorCommandHandler<CreateCustomerCommand, Guid>
{
    // Correct Mvp24Hours interface
}
```

**Why**: Mvp24Hours has its own mediator implementation. Don't mix MediatR and Mvp24Hours.

---

### 2. Commands Returning Full Entities

**❌ WRONG**:
```csharp
public record CreateCustomerCommand(string Name) : IMediatorCommand<Customer>;
// Returns entire entity
```

**✅ CORRECT**:
```csharp
public record CreateCustomerCommand(string Name) : IMediatorCommand<Guid>;
// Returns only ID
```

**Why**: Commands should return minimal data (ID, success status). Use queries to retrieve full entities.

---

### 3. Queries with Side Effects

**❌ WRONG**:
```csharp
public class GetCustomerHandler : IMediatorQueryHandler<GetCustomerQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerQuery query, CancellationToken ct)
    {
        var customer = await _repository.GetByIdAsync(query.Id, ct);
        
        // Side effect in query!
        customer.IncrementViewCount();
        await _unitOfWork.SaveChangesAsync(ct);
        
        return MapToDto(customer);
    }
}
```

**✅ CORRECT**:
```csharp
// Separate command for side effects
public record IncrementViewCountCommand(Guid CustomerId) : IMediatorCommand;

// Query remains pure
public class GetCustomerHandler : IMediatorQueryHandler<GetCustomerQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerQuery query, CancellationToken ct)
    {
        var customer = await _repository.GetByIdAsync(query.Id, ct);
        return MapToDto(customer);
    }
}
```

**Why**: Queries must be idempotent and side-effect free.

---

## Testing Strategy

```csharp
using NSubstitute;
using FluentAssertions;
using Xunit;

public class CreateCustomerHandlerTests
{
    private readonly IRepositoryAsync<Customer> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly CreateCustomerHandler _handler;

    public CreateCustomerHandlerTests()
    {
        _repository = Substitute.For<IRepositoryAsync<Customer>>();
        _unitOfWork = Substitute.For<IUnitOfWorkAsync>();
        _handler = new CreateCustomerHandler(
            _repository,
            _unitOfWork,
            Substitute.For<ILogger<CreateCustomerHandler>>());
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsCustomerId()
    {
        // Arrange
        var command = new CreateCustomerCommand(
            "John Doe",
            "john@example.com",
            "123 Main St",
            "City");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repository.Received(1).Add(Arg.Any<Customer>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

---

## Best Practices Checklist

- [ ] Use `IMediatorCommand<T>` for write operations
- [ ] Use `IMediatorQuery<T>` for read operations
- [ ] Commands return minimal data (IDs, not entities)
- [ ] Queries have no side effects
- [ ] Implement validation with FluentValidation
- [ ] Use pipeline behaviors for cross-cutting concerns
- [ ] Dispatch domain events with `IMediatorNotification`
- [ ] Keep handlers focused (single responsibility)
- [ ] Test handlers in isolation with mocks
- [ ] Use meaningful command/query names

---

## MCP Workflow Examples

```bash
# Get CQRS template
get_architecture_template "templateId": "cqrs"

# Get documentation
get_doc "path": "docs/en-us/cqrs/getting-started.md"
get_doc "path": "docs/en-us/cqrs/commands.md"

# Explore sample
get_sample_tree "sampleId": "complex-cqrs-ef-customer-api"
```

---

## Samples (MCP `list_samples`)

CQRS is a **blueprint**. Sample `complex-cqrs-ef-customer-api` is **not** Complex N-Layers.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `complex-cqrs-ef-customer-api` | Blueprint | Canonical CQRS sample |
| `complex-event-sourcing-customer-api` | Capability | Event store (optional companion) |
| `simple-crud-ef-customer-api` | Simple | CRUD without CQRS |

---

## Further Resources

### Related Skills
- `mediator-patterns-specialist.md` - Deep mediator implementation
- `data-architect.md` - Separate read/write stores
- `messaging-architect.md` - Event-driven CQRS

### NuGet Packages
- **Mvp24Hours.Infrastructure.Cqrs** - Mvp24Hours mediator
- **FluentValidation** - Command validation

---

**Version**: Mvp24Hours 10.8.0+ (.NET 10)  
**Last Updated**: January 2025  
**Maintained By**: Mvp24Hours Community
