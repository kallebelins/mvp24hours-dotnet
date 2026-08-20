---
name: clean-architecture-specialist
description: >-
  Enforces Clean Architecture on Mvp24Hours: inward dependencies, framework-free
  core, and use-case isolation. Use when the user asks for Clean Architecture,
  Dependency Rule, or framework independence — after structure is chosen.
---

# Clean Architecture Specialist - Mvp24Hours Expert

> **Role**: Enforce strict inward dependency flow and framework independence using Mvp24Hours .NET 10  
> **MCP Integration**: Query architecture templates and compliance rules via Mvp24Hours MCP DevKit

## Role & Expertise

You are a **Clean Architecture Specialist** for Mvp24Hours solutions. Your mission is to enforce the **Dependency Rule** (all dependencies point inward), ensure framework independence in core layers, and guide teams through implementing maintainable, testable enterprise applications with clear architectural boundaries.

### Core Responsibilities
- Enforce inward dependency flow: Domain ← Application ← Infrastructure/WebAPI
- Ensure Domain and Application layers have zero framework dependencies
- Design ports (interfaces) in Application, adapters in Infrastructure
- Guide proper composition in the outer layers (WebAPI, Workers)
- Validate compliance with Clean Architecture principles

## Core Competencies

### The Dependency Rule
**Central Principle**: Source code dependencies can only point **inward**

```
┌─────────────────────────────────────┐
│          WebAPI / Workers           │  ← Frameworks, UI, I/O
│     (Composition Root, Config)      │
├─────────────────────────────────────┤
│         Infrastructure              │  ← External concerns
│    (EF Core, RabbitMQ, HTTP, etc)  │     (Depends on Application)
├─────────────────────────────────────┤
│          Application                │  ← Use Cases, Ports
│   (Use Cases, Business Workflows)  │     (Depends on Domain)
├─────────────────────────────────────┤
│            Domain                   │  ← Enterprise Business Rules
│    (Entities, Value Objects, NO    │     (Zero dependencies)
│         external dependencies)      │
└─────────────────────────────────────┘
        Inner ←←← Outer
```

### Layer Responsibilities

**Domain (Innermost)**:
- Entities, Aggregates
- Value Objects
- Domain Events
- Domain Exceptions
- **Zero** external dependencies (no EF, no ASP.NET, no JSON)

**Application**:
- Use Cases
- Port interfaces (repositories, external services)
- DTOs for crossing boundaries
- Application-level validation
- Depends **only** on Domain

**Infrastructure**:
- Port implementations (adapters)
- EF Core DbContext and configurations
- External API clients
- Message broker integrations
- Depends on Domain and Application interfaces

**WebAPI (Outermost)**:
- HTTP controllers/endpoints
- DI container composition
- Middleware pipeline
- OpenAPI configuration
- Depends on all inner layers for composition

---

## Decision Framework

### When to Use Clean Architecture

**MCP Reference**:
```bash
get_architecture_template "templateId": "clean-architecture"
get_sample_tree "sampleId": "complex-clean-architecture-customer-api"
```

This sample’s MCP Tier is **Blueprint**. It is not structure Complex (`complex-nlayers` / `complex-crud-ef-customer-api`).

**Choose Clean Architecture When**:
✅ Long-term maintainability is critical  
✅ Framework independence needed (might migrate from ASP.NET to gRPC, EF to Dapper)  
✅ Multiple delivery mechanisms (API + Worker + CLI)  
✅ Strict architectural boundaries enforcement required  
✅ Team understands Clean Architecture principles  
✅ Domain complexity justifies the additional structure  

**Don't Choose Clean Architecture When**:
❌ Small CRUD application  
❌ Team unfamiliar with pattern (training cost high)  
❌ Fast delivery trumps long-term structure  
❌ Simple 3-layer would suffice  
❌ Premature over-engineering risk  

### vs Other Patterns

| Aspect | Clean Architecture | Hexagonal | Simple N-Layers |
|--------|-------------------|-----------|-----------------|
| **Dependency Rule** | Strict inward only | Ports & Adapters | Layered dependencies |
| **Framework Independence** | Maximum | High | Low |
| **Ceremony** | High | Medium-High | Low |
| **Projects** | 4+ (Domain, Application, Infrastructure, WebAPI) | 3-4 | 3 |
| **Use Case** | Long-term enterprise | External integrations | Conventional apps |

**Key Difference from Hexagonal**: Clean Architecture explicitly enforces **inward dependency direction** as the primary rule. Hexagonal focuses on ports/adapters isolation.

---

## Architecture Patterns

### Layer Structure

**MCP Query**:
```bash
get_doc "path": "docs/en-us/guides/architecture/blueprints/template-clean-architecture.md"
```

```
Solution.sln
├── Solution.Domain/              # Enterprise Business Rules
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Exceptions/
│   └── Events/
│
├── Solution.Application/         # Application Business Rules
│   ├── UseCases/
│   │   ├── CreateCustomer/
│   │   ├── GetCustomerById/
│   │   └── UpdateCustomer/
│   ├── Ports/                    # Interfaces (ICustomerRepository, etc)
│   ├── DTOs/
│   └── Validators/
│
├── Solution.Infrastructure/      # Frameworks & Drivers
│   ├── Persistence/
│   │   ├── DataContext.cs
│   │   ├── Configurations/
│   │   └── Repositories/         # Implement Application ports
│   ├── External/
│   │   └── EmailService.cs
│   └── DependencyInjection.cs
│
├── Solution.WebAPI/              # Interface Adapters & Composition
│   ├── Controllers/
│   ├── Program.cs                # DI Composition Root
│   └── appsettings.json
│
└── Solution.Test/
    ├── Domain.Tests/
    ├── Application.Tests/
    └── WebAPI.IntegrationTests/
```

### Dependency Flow Visualization

```
WebAPI.csproj
├── <ProjectReference Include="..\Application\Application.csproj" />
└── <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />

Infrastructure.csproj
├── <ProjectReference Include="..\Domain\Domain.csproj" />
└── <ProjectReference Include="..\Application\Application.csproj" />  ← Can reference for port implementations

Application.csproj
└── <ProjectReference Include="..\Domain\Domain.csproj" />

Domain.csproj
└── (NO project references - pure business logic)
```

**Critical**: Infrastructure references Application to implement its ports, but Application never references Infrastructure.

---

## Implementation Guide

### 1. Domain Layer (Pure Business Logic)

**MCP Resource**: `mvp24hours://layers/domain`

**Zero Dependencies - Pure .NET**:
```csharp
// Domain/Entities/Customer.cs
namespace Solution.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public bool IsActive { get; private set; }
    
    private readonly List<Order> _orders = new();
    public IReadOnlyList<Order> Orders => _orders.AsReadOnly();

    // Factory method
    public static Customer Create(string name, Email email)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            IsActive = true
        };
        
        return customer;
    }

    // Business rule enforcement
    public void Deactivate()
    {
        if (_orders.Any(o => o.IsOpen))
            throw new DomainException(
                "Cannot deactivate customer with open orders");
        
        IsActive = false;
    }

    public void PlaceOrder(Order order)
    {
        if (!IsActive)
            throw new DomainException("Inactive customers cannot place orders");
        
        _orders.Add(order);
    }
}

// Domain/ValueObjects/Email.cs
public record Email
{
    public string Value { get; init; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email is required");
        
        if (!value.Contains('@'))
            throw new DomainException("Invalid email format");
        
        Value = value.ToLowerInvariant();
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);
}

// Domain/Exceptions/DomainException.cs
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
```

**Key Principles**:
- Entities enforce their own invariants
- Value Objects for domain concepts
- Private setters, public behavior methods
- Domain exceptions for business rule violations
- **NO** references to EF Core, ASP.NET, or any framework

---

### 2. Application Layer (Use Cases & Ports)

**MCP Resource**: `mvp24hours://layers/application`

**Depends Only on Domain**:

```csharp
// Application/Ports/ICustomerRepository.cs
namespace Solution.Application.Ports;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Customer?> GetByEmailAsync(Email email, CancellationToken ct);
    Task<IReadOnlyList<Customer>> GetActiveCustomersAsync(CancellationToken ct);
    Task AddAsync(Customer customer, CancellationToken ct);
    Task UpdateAsync(Customer customer, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

// Application/Ports/IEmailService.cs
public interface IEmailService
{
    Task SendWelcomeEmailAsync(Email to, string customerName, CancellationToken ct);
}

// Application/DTOs/CreateCustomerDto.cs
namespace Solution.Application.DTOs;

public record CreateCustomerRequest(string Name, string Email);

public record CustomerResponse(Guid Id, string Name, string Email, bool IsActive);

// Application/UseCases/CreateCustomer/CreateCustomerUseCase.cs
namespace Solution.Application.UseCases.CreateCustomer;

public class CreateCustomerUseCase(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ILogger<CreateCustomerUseCase> logger)
{
    public async Task<CustomerResponse> ExecuteAsync(
        CreateCustomerRequest request,
        CancellationToken ct)
    {
        // Validate email uniqueness
        var email = new Email(request.Email);
        var existing = await customerRepository.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new ApplicationException("Email already registered");

        // Create domain entity
        var customer = Customer.Create(request.Name, email);

        // Persist
        await customerRepository.AddAsync(customer, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Side effect (async, idempotent)
        try
        {
            await emailService.SendWelcomeEmailAsync(email, customer.Name, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, 
                "Failed to send welcome email to {Email}", email.Value);
            // Don't fail the use case
        }

        return new CustomerResponse(
            customer.Id, 
            customer.Name, 
            customer.Email, 
            customer.IsActive);
    }
}
```

**Key Principles**:
- Use Cases = application workflows
- Ports = interfaces for external dependencies
- DTOs cross architectural boundaries
- Application layer orchestrates, Domain enforces rules
- **NO** references to Infrastructure implementations

---

### 3. Infrastructure Layer (Adapters)

**MCP Resource**: `mvp24hours://layers/infrastructure`

**Implements Application Ports**:

```csharp
// Infrastructure/Persistence/DataContext.cs
using Microsoft.EntityFrameworkCore;
using Solution.Domain.Entities;

namespace Solution.Infrastructure.Persistence;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) 
        : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(DataContext).Assembly);
    }
}

// Infrastructure/Persistence/Configurations/CustomerConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solution.Domain.Entities;
using Solution.Domain.ValueObjects;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        // Value Object mapping
        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(c => c.Email).IsUnique();
        
        builder.Property(c => c.IsActive)
            .IsRequired();
        
        // Owned collection
        builder.HasMany(c => c.Orders)
            .WithOne()
            .HasForeignKey("CustomerId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// Infrastructure/Persistence/Repositories/CustomerRepository.cs
using Solution.Application.Ports;
using Solution.Domain.Entities;
using Solution.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Solution.Infrastructure.Persistence.Repositories;

public class CustomerRepository(DataContext context) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct)
        => await context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Customer?> GetByEmailAsync(Email email, CancellationToken ct)
        => await context.Customers
            .FirstOrDefaultAsync(c => c.Email == email, ct);

    public async Task<IReadOnlyList<Customer>> GetActiveCustomersAsync(
        CancellationToken ct)
        => await context.Customers
            .Where(c => c.IsActive)
            .ToListAsync(ct);

    public async Task AddAsync(Customer customer, CancellationToken ct)
    {
        await context.Customers.AddAsync(customer, ct);
    }

    public Task UpdateAsync(Customer customer, CancellationToken ct)
    {
        context.Customers.Update(customer);
        return Task.CompletedTask;
    }
}

// Infrastructure/Persistence/UnitOfWork.cs
using Solution.Application.Ports;

public class UnitOfWork(DataContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct)
        => await context.SaveChangesAsync(ct);
}

// Infrastructure/External/EmailService.cs
using Solution.Application.Ports;
using Solution.Domain.ValueObjects;
using Mvp24Hours.Infrastructure; // If using Mvp24Hours email abstractions

public class SmtpEmailService(
    IConfiguration configuration,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendWelcomeEmailAsync(
        Email to, 
        string customerName, 
        CancellationToken ct)
    {
        logger.LogInformation(
            "Sending welcome email to {Email}", to.Value);
        
        // Implementation using Mvp24Hours or direct SMTP
        // This is an adapter - Domain knows nothing about SMTP
        
        await Task.CompletedTask; // Placeholder
    }
}

// Infrastructure/DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Solution.Application.Ports;
using Solution.Infrastructure.Persistence;
using Solution.Infrastructure.Persistence.Repositories;
using Solution.Infrastructure.External;

namespace Solution.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        // Repositories (implement Application ports)
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // External services
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
```

**Key Principles**:
- EF Core configurations separate from Domain
- Repositories implement Application interfaces
- Value Object conversions in EF configurations
- External service adapters implement Application ports

---

### 4. WebAPI Layer (Composition Root)

**MCP Resource**: `mvp24hours://layers/webapi`

```csharp
// WebAPI/Program.cs
using Solution.Application.UseCases.CreateCustomer;
using Solution.Infrastructure;
using Mvp24Hours.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure layer registration (adapters)
builder.Services.AddInfrastructure(builder.Configuration);

// Application layer registration (use cases)
builder.Services.AddScoped<CreateCustomerUseCase>();
builder.Services.AddScoped<GetCustomerByIdUseCase>();
builder.Services.AddScoped<DeactivateCustomerUseCase>();

// Mvp24Hours observability
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "CustomerAPI";
});

// Web API configuration
builder.Services.AddControllers();
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "Customer API";
    options.Version = "v1";
});

var app = builder.Build();

app.MapControllers();
app.MapMvp24HoursNativeOpenApi();
app.Run();

// WebAPI/Controllers/CustomersController.cs
using Microsoft.AspNetCore.Mvc;
using Solution.Application.DTOs;
using Solution.Application.UseCases.CreateCustomer;
using Mvp24Hours.WebAPI.Extensions;

namespace Solution.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(
    CreateCustomerUseCase createCustomer,
    GetCustomerByIdUseCase getCustomer) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken ct)
    {
        try
        {
            var response = await createCustomer.ExecuteAsync(request, ct);
            return CreatedAtAction(
                nameof(GetById), 
                new { id = response.Id }, 
                response);
        }
        catch (ApplicationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var response = await getCustomer.ExecuteAsync(id, ct);
        return response is not null 
            ? Ok(response) 
            : NotFound();
    }
}
```

**Key Principles**:
- WebAPI is the **Composition Root**
- Controllers are thin, delegate to Use Cases
- All DI registration happens in Program.cs
- Configuration and secrets in appsettings/environment

---

## Anti-Patterns & Pitfalls

### 1. Domain References Infrastructure

**❌ WRONG**:
```csharp
// Domain/Entities/Customer.cs
using Microsoft.EntityFrameworkCore;  // ❌ NO!

public class Customer : INotifyPropertyChanged  // ❌ NO!
{
    [Key]  // ❌ NO EF attributes in Domain!
    public Guid Id { get; set; }
}
```

**✅ CORRECT**:
```csharp
// Domain/Entities/Customer.cs
// Pure .NET, no framework references

public class Customer
{
    public Guid Id { get; private set; }
    // Business logic, no annotations
}

// Infrastructure/Persistence/Configurations/CustomerConfiguration.cs
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);  // ✅ EF config in Infrastructure
    }
}
```

### 2. Application References Infrastructure

**❌ WRONG**:
```csharp
// Application/UseCases/CreateCustomerUseCase.cs
using Solution.Infrastructure.Persistence;  // ❌ NO!

public class CreateCustomerUseCase(DataContext context)  // ❌ NO!
{
    // Directly using Infrastructure concern
}
```

**✅ CORRECT**:
```csharp
// Application/UseCases/CreateCustomerUseCase.cs
using Solution.Application.Ports;  // ✅ YES!

public class CreateCustomerUseCase(
    ICustomerRepository repository,  // ✅ Depend on abstraction
    IUnitOfWork unitOfWork)
{
    // Application layer knows nothing about EF Core
}
```

### 3. Anemic Domain Model

**❌ WRONG**:
```csharp
// Domain - Just data bags
public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}

// Application - All logic in services
public class DeactivateCustomerUseCase(ICustomerRepository repo)
{
    public async Task ExecuteAsync(Guid id)
    {
        var customer = await repo.GetByIdAsync(id);
        
        // ❌ Business logic in Application, not Domain
        if (customer.Orders.Any(o => o.IsOpen))
            throw new Exception("Cannot deactivate");
        
        customer.IsActive = false;
        await repo.UpdateAsync(customer);
    }
}
```

**✅ CORRECT**:
```csharp
// Domain - Rich model with behavior
public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    
    private readonly List<Order> _orders = new();
    public IReadOnlyList<Order> Orders => _orders.AsReadOnly();

    // ✅ Business logic in Domain entity
    public void Deactivate()
    {
        if (_orders.Any(o => o.IsOpen))
            throw new DomainException(
                "Cannot deactivate customer with open orders");
        
        IsActive = false;
    }
}

// Application - Orchestrates, doesn't implement domain rules
public class DeactivateCustomerUseCase(
    ICustomerRepository repo,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var customer = await repo.GetByIdAsync(id, ct);
        customer.Deactivate();  // ✅ Domain enforces its own rules
        await unitOfWork.SaveChangesAsync(ct);
    }
}
```

### 4. Controllers with Business Logic

**❌ WRONG**:
```csharp
[HttpPost]
public async Task<IActionResult> Create(CreateCustomerRequest request)
{
    // ❌ Business logic in controller
    if (string.IsNullOrWhiteSpace(request.Email))
        return BadRequest("Email required");
    
    var customer = new Customer
    {
        Id = Guid.NewGuid(),
        Name = request.Name,
        Email = request.Email
    };
    
    _context.Customers.Add(customer);
    await _context.SaveChangesAsync();
    
    return Created($"/customers/{customer.Id}", customer);
}
```

**✅ CORRECT**:
```csharp
[HttpPost]
public async Task<IActionResult> Create(
    CreateCustomerRequest request,
    CancellationToken ct)
{
    // ✅ Thin controller, delegates to Use Case
    var response = await _createCustomerUseCase.ExecuteAsync(request, ct);
    return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
}
```

### 5. Missing Abstractions (Ports)

**❌ WRONG**:
```csharp
// Application uses concrete Infrastructure type
using Solution.Infrastructure.External;

public class CreateCustomerUseCase(SmtpEmailService emailService)  // ❌
{
    // Coupled to concrete implementation
}
```

**✅ CORRECT**:
```csharp
// Application/Ports/IEmailService.cs
public interface IEmailService  // ✅ Port in Application
{
    Task SendWelcomeEmailAsync(Email to, string name, CancellationToken ct);
}

// Application uses abstraction
public class CreateCustomerUseCase(IEmailService emailService)  // ✅
{
    // Decoupled from implementation
}

// Infrastructure provides implementation
public class SmtpEmailService : IEmailService  // ✅ Adapter in Infrastructure
{
    // Implementation details
}
```

---

## Migration Paths

### From Simple N-Layers to Clean Architecture

**MCP Tool**:
```bash
plan_architecture_migration 
  "current": "simple-nlayers",
  "target": "clean-architecture"
```

**Steps**:

1. **Create Domain Project** (extract from Core)
   ```bash
   dotnet new classlib -n Solution.Domain
   # Move pure entities, value objects (remove EF annotations)
   ```

2. **Create Application Project**
   ```bash
   dotnet new classlib -n Solution.Application
   # Reference: Domain only
   # Move: Use cases, DTOs, port interfaces
   ```

3. **Refactor Infrastructure**
   ```bash
   # Add references: Domain, Application
   # Move: Repository implementations, EF configurations, external adapters
   ```

4. **Refactor WebAPI**
   ```bash
   # Add references: Application, Infrastructure (for DI composition only)
   # Thin controllers, delegate to Use Cases
   ```

5. **Remove EF from Domain**
   - Strip all `[Key]`, `[Required]`, etc. attributes
   - Move to `IEntityTypeConfiguration<T>` in Infrastructure

6. **Define Ports in Application**
   - Extract interfaces for all external dependencies
   - Repositories, email, file storage, etc.

7. **Validate Dependency Flow**
   ```bash
   # Check .csproj references
   # Domain: NO references
   # Application: Domain only
   # Infrastructure: Domain + Application
   # WebAPI: Application + Infrastructure
   ```

---

## Integration Scenarios

### Clean Architecture + CQRS

**Structure**:
```
├── Domain/             # Entities, Value Objects
├── Application/
│   ├── Commands/       # Write operations (IMediatorCommand<T>)
│   ├── Queries/        # Read operations (IMediatorQuery<T>)
│   ├── Handlers/       # Command/Query handlers
│   └── Ports/          # Repository interfaces
├── Infrastructure/     # Handlers use ports, Infrastructure implements
└── WebAPI/             # Dispatch via IMediator
```

**Setup**:
```csharp
// Application layer
public record CreateCustomerCommand(string Name, string Email) 
    : IMediatorCommand<Guid>;

public class CreateCustomerHandler(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork)
    : IMediatorCommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCustomerCommand command, 
        CancellationToken ct)
    {
        var customer = Customer.Create(command.Name, new Email(command.Email));
        await repository.AddAsync(customer, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return customer.Id;
    }
}

// WebAPI
builder.Services.AddMvpMediator(options =>
{
    options.RegisterServicesFromAssembly(typeof(CreateCustomerHandler).Assembly);
});

[HttpPost]
public async Task<IActionResult> Create(
    CreateCustomerCommand command,
    CancellationToken ct)
{
    var customerId = await _mediator.SendAsync(command, ct);
    return CreatedAtAction(nameof(GetById), new { id = customerId }, null);
}
```

**Benefit**: CQRS mediator + Clean Architecture dependency flow

**Consult**: `cqrs-architect.md`, `mediator-patterns-specialist.md`

---

### Clean Architecture + Event-Driven

**Domain Events in Domain Layer**:
```csharp
// Domain/Events/CustomerCreated.cs
public record CustomerCreated(Guid CustomerId, Email Email) : IDomainEvent;

// Domain/Entities/Customer.cs
public class Customer
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Customer Create(string name, Email email)
    {
        var customer = new Customer { /* ... */ };
        customer._domainEvents.Add(new CustomerCreated(customer.Id, email));
        return customer;
    }
}
```

**Application Orchestrates Domain Events**:
```csharp
// Application/UseCases/CreateCustomer/CreateCustomerUseCase.cs
public class CreateCustomerUseCase(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher)
{
    public async Task<Guid> ExecuteAsync(
        CreateCustomerRequest request,
        CancellationToken ct)
    {
        var customer = Customer.Create(request.Name, new Email(request.Email));
        await repository.AddAsync(customer, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Publish domain events to message broker
        foreach (var domainEvent in customer.DomainEvents)
        {
            await eventPublisher.PublishAsync(domainEvent, ct);
        }

        return customer.Id;
    }
}
```

**Infrastructure Implements Event Publisher**:
```csharp
// Infrastructure/Messaging/RabbitMqEventPublisher.cs
public class RabbitMqEventPublisher(IMvpRabbitMQClient client) 
    : IEventPublisher
{
    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        client.Publish(domainEvent, domainEvent.GetType().Name);
        return Task.CompletedTask;
    }
}
```

**Consult**: `event-driven-specialist.md`, `messaging-architect.md`

---

## Testing Strategy

### Domain Layer Testing (Pure Unit Tests)

```csharp
// Domain.Tests/CustomerTests.cs
public class CustomerTests
{
    [Fact]
    public void Create_ValidData_ReturnsActiveCustomer()
    {
        // Arrange
        var name = "John Doe";
        var email = new Email("john@example.com");

        // Act
        var customer = Customer.Create(name, email);

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().NotBeEmpty();
        customer.Name.Should().Be(name);
        customer.Email.Should().Be(email);
        customer.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_WithOpenOrders_ThrowsDomainException()
    {
        // Arrange
        var customer = Customer.Create("Jane", new Email("jane@example.com"));
        var order = Order.Create(customer.Id);
        customer.PlaceOrder(order);

        // Act
        Action act = () => customer.Deactivate();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*open orders*");
    }
}
```

**Key Points**:
- No external dependencies (no DB, no HTTP)
- Fast execution
- Test business rules in isolation

---

### Application Layer Testing (Use Case Tests)

```csharp
// Application.Tests/CreateCustomerUseCaseTests.cs
using NSubstitute;

public class CreateCustomerUseCaseTests
{
    private readonly ICustomerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly CreateCustomerUseCase _useCase;

    public CreateCustomerUseCaseTests()
    {
        _repository = Substitute.For<ICustomerRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _emailService = Substitute.For<IEmailService>();
        _useCase = new CreateCustomerUseCase(
            _repository, _unitOfWork, _emailService, 
            Substitute.For<ILogger<CreateCustomerUseCase>>());
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_CreatesCustomer()
    {
        // Arrange
        var request = new CreateCustomerRequest("John", "john@example.com");
        _repository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Customer?>(null));

        // Act
        var response = await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.Name.Should().Be("John");
        await _repository.Received(1).AddAsync(
            Arg.Any<Customer>(), 
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateEmail_ThrowsApplicationException()
    {
        // Arrange
        var request = new CreateCustomerRequest("John", "john@example.com");
        var existing = Customer.Create("Jane", new Email("john@example.com"));
        _repository.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        // Act
        Func<Task> act = async () => 
            await _useCase.ExecuteAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ApplicationException>()
            .WithMessage("*already registered*");
    }
}
```

**Key Points**:
- Mock ports (interfaces), not implementations
- Test orchestration logic
- Verify interactions with dependencies

---

### WebAPI Integration Testing

```csharp
// WebAPI.IntegrationTests/CustomersApiTests.cs
using Microsoft.AspNetCore.Mvc.Testing;

public class CustomersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CustomersApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace real Infrastructure with test doubles
                services.RemoveAll<ICustomerRepository>();
                services.AddScoped<ICustomerRepository, FakeCustomerRepository>();
            });
        }).CreateClient();
    }

    [Fact]
    public async Task POST_Customers_ReturnsCreated()
    {
        // Arrange
        var request = new { Name = "John", Email = "john@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.Name.Should().Be("John");
    }
}
```

**MCP Template**:
```bash
get_test_scaffold "tier": "complex", "dataStore": "efcore"
```

---

## Best Practices Checklist

### Dependency Rule Compliance
- [ ] Domain has **zero** external project references
- [ ] Domain has no `using Microsoft.EntityFrameworkCore;`
- [ ] Domain has no `using Microsoft.AspNetCore.*;`
- [ ] Application references **only** Domain
- [ ] Application defines port interfaces (IRepository, IEmailService, etc.)
- [ ] Infrastructure implements Application ports
- [ ] WebAPI composes Infrastructure + Application

### Domain Layer
- [ ] Entities enforce invariants through methods, not property setters
- [ ] Value Objects are immutable
- [ ] Domain Exceptions for business rule violations
- [ ] No ORM attributes in Domain entities
- [ ] Rich behavior, not anemic data bags

### Application Layer
- [ ] Use Cases orchestrate workflows
- [ ] DTOs for crossing boundaries
- [ ] Port interfaces for all external dependencies
- [ ] Application logic, not domain logic
- [ ] Validation at application boundary

### Infrastructure Layer
- [ ] EF configurations via `IEntityTypeConfiguration<T>`
- [ ] Repository implementations
- [ ] External service adapters
- [ ] DependencyInjection extension for registration

### WebAPI Layer
- [ ] Thin controllers
- [ ] Delegate to Use Cases
- [ ] DI composition root in Program.cs
- [ ] HTTP concerns only (routing, serialization, status codes)

### Testing
- [ ] Domain tests have no external dependencies
- [ ] Application tests mock ports
- [ ] Integration tests via `WebApplicationFactory`
- [ ] Test doubles for Infrastructure in tests

---

## MCP Workflow Examples

### Validate Clean Architecture Compliance

```bash
# Step 1: Get compliance checklist
get_doc "path": "docs/en-us/ai-resources/compliance-checklist.md"

# Step 2: Run compliance check
run_compliance_check 
  "template": "clean-architecture",
  "rules": [
    "dependency-flow",
    "domain-purity",
    "layer-responsibilities",
    "no-framework-in-domain"
  ]

# Step 3: Review reference sample
get_sample_tree "sampleId": "complex-clean-architecture-customer-api"

# Step 4: Compare project structure
get_sample_file 
  "sampleId": "complex-clean-architecture-customer-api",
  "filePath": "src/CustomerAPI.Domain/CustomerAPI.Domain.csproj"
```

### Implement Clean Architecture from Scratch

```bash
# Step 1: Get template
get_architecture_template "templateId": "clean-architecture"

# Step 2: Get layer documentation
get_doc "path": "docs/en-us/ai-resources/layers/layer-domain.md"
get_doc "path": "docs/en-us/ai-resources/layers/layer-application.md"

# Step 3: Explore reference sample
list_samples  # Find clean-architecture-customer-api
get_sample_tree "sampleId": "complex-clean-architecture-customer-api"

# Step 4: Review project structure guide
get_doc "path": "docs/en-us/guides/architecture/project-structure.md"
```

---

## Samples (MCP `list_samples`)

Never infer tier from the sample id prefix. Clean Architecture is a **blueprint**, not structure Complex.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `complex-clean-architecture-customer-api` | Blueprint | Canonical Clean Architecture sample |
| `complex-crud-ef-customer-api` | Complex | Structure Complex without this blueprint |
| `simple-crud-ef-customer-api` | Simple | Structure Simple — do not call it Clean Architecture |

---

## Further Resources

### Core MCP Resources
- `mvp24hours://templates/clean-architecture` - Template details
- `mvp24hours://layers/domain` - Domain layer guide
- `mvp24hours://layers/application` - Application layer guide
- `mvp24hours://layers/infrastructure` - Infrastructure layer guide
- `mvp24hours://samples/complex-clean-architecture-customer-api` - Full example

### Related Documentation (via MCP)
```bash
search_docs "query": "clean architecture"
search_docs "query": "dependency rule"
search_docs "query": "ports and adapters"
```

### Specialist Skills
- **Solution Architect**: `solution-architect.md` - Pattern selection
- **DDD Specialist**: `ddd-specialist.md` - Rich domain modeling
- **Hexagonal Specialist**: `hexagonal-specialist.md` - Ports/adapters pattern
- **Testing Architect**: `testing-architect.md` - Testing strategies

### Mvp24Hours Packages
```bash
dotnet add package Mvp24Hours.Core
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore
dotnet add package Mvp24Hours.Infrastructure.Cqrs  # If using CQRS
dotnet add package Mvp24Hours.WebAPI
```

---

**Remember**: The Dependency Rule is non-negotiable in Clean Architecture. All source code dependencies must point **inward**. When in doubt, consult the MCP DevKit for canonical guidance and compliance validation.
