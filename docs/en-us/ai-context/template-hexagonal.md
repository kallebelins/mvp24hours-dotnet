# Hexagonal Architecture Template

> **AI Agent Instruction**: Use this template for applications requiring strong isolation between business logic and external systems. Hexagonal Architecture (Ports & Adapters) ensures the domain remains independent of infrastructure concerns.

---

## When to Use Hexagonal Architecture

### Recommended Scenarios
- Applications with multiple external integrations
- Systems requiring high testability
- Projects with evolving infrastructure needs
- Applications that need to swap implementations easily
- Long-term maintainable systems

### Not Recommended
- Simple CRUD applications
- Small projects with limited scope
- Teams unfamiliar with clean architecture patterns
- Projects with tight deadlines

---

## Core Concepts

### Ports
- **Inbound Ports (Driving)**: Interfaces that define how the outside world interacts with the application (use cases)
- **Outbound Ports (Driven)**: Interfaces that define how the application interacts with external systems

### Adapters
- **Inbound Adapters (Primary)**: Controllers, CLI, Message Consumers - implement driving side
- **Outbound Adapters (Secondary)**: Repositories, External APIs, Message Publishers - implement driven side

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
    │   │   └── Order.cs
    │   ├── ValueObjects/
    │   │   ├── Email.cs
    │   │   ├── Money.cs
    │   │   └── Address.cs
    │   ├── Aggregates/
    │   │   └── CustomerAggregate.cs
    │   ├── Events/
    │   │   ├── CustomerCreatedEvent.cs
    │   │   └── OrderPlacedEvent.cs
    │   └── Exceptions/
    │       ├── DomainException.cs
    │       └── CustomerNotFoundException.cs
    ├── ProjectName.Application/
    │   ├── ProjectName.Application.csproj
    │   ├── Ports/
    │   │   ├── Inbound/
    │   │   │   ├── ICreateCustomerUseCase.cs
    │   │   │   ├── IGetCustomerUseCase.cs
    │   │   │   ├── IUpdateCustomerUseCase.cs
    │   │   │   └── IDeleteCustomerUseCase.cs
    │   │   └── Outbound/
    │   │       ├── ICustomerRepository.cs
    │   │       ├── IEmailService.cs
    │   │       ├── IPaymentGateway.cs
    │   │       └── IEventPublisher.cs
    │   ├── UseCases/
    │   │   ├── CreateCustomerUseCase.cs
    │   │   ├── GetCustomerUseCase.cs
    │   │   ├── UpdateCustomerUseCase.cs
    │   │   └── DeleteCustomerUseCase.cs
    │   ├── DTOs/
    │   │   ├── Requests/
    │   │   │   ├── CreateCustomerRequest.cs
    │   │   │   └── UpdateCustomerRequest.cs
    │   │   └── Responses/
    │   │       ├── CustomerResponse.cs
    │   │       └── CustomerListResponse.cs
    │   └── Validators/
    │       ├── CreateCustomerValidator.cs
    │       └── UpdateCustomerValidator.cs
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   └── Adapters/
    │       ├── Outbound/
    │       │   ├── Persistence/
    │       │   │   ├── DataContext.cs
    │       │   │   ├── CustomerRepository.cs
    │       │   │   └── Configurations/
    │       │   │       └── CustomerConfiguration.cs
    │       │   ├── Email/
    │       │   │   └── SmtpEmailService.cs
    │       │   ├── Payment/
    │       │   │   └── StripePaymentGateway.cs
    │       │   └── Messaging/
    │       │       └── RabbitMQEventPublisher.cs
    │       └── Inbound/
    │           └── Messaging/
    │               └── OrderEventConsumer.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── Adapters/
        │   └── Inbound/
        │       └── Http/
        │           └── Controllers/
        │               └── CustomersController.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

---

## Namespaces

```csharp
// Domain (no external dependencies)
ProjectName.Domain.Entities
ProjectName.Domain.ValueObjects
ProjectName.Domain.Aggregates
ProjectName.Domain.Events
ProjectName.Domain.Exceptions

// Application (orchestration layer)
ProjectName.Application.Ports.Inbound
ProjectName.Application.Ports.Outbound
ProjectName.Application.UseCases
ProjectName.Application.DTOs.Requests
ProjectName.Application.DTOs.Responses
ProjectName.Application.Validators

// Infrastructure (external implementations)
ProjectName.Infrastructure.Adapters.Outbound.Persistence
ProjectName.Infrastructure.Adapters.Outbound.Email
ProjectName.Infrastructure.Adapters.Outbound.Payment
ProjectName.Infrastructure.Adapters.Outbound.Messaging

// WebAPI (HTTP adapter)
ProjectName.WebAPI.Adapters.Inbound.Http.Controllers
ProjectName.WebAPI.Extensions
```

---

## Project Files (.csproj)

### Domain Project (No Dependencies)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Domain should have ZERO external dependencies -->
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
    <PackageReference Include="Mvp24Hours.Infrastructure.RabbitMQ" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
  </ItemGroup>
</Project>
```

### WebAPI Project

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\ProjectName.Infrastructure\ProjectName.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.WebAPI" Version="9.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
  </ItemGroup>
</Project>
```

---

## Domain Layer Templates

### Entity

```csharp
// Domain/Entities/Customer.cs
namespace ProjectName.Domain.Entities;

public class Customer
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public DateTime Created { get; private set; }

    // Private constructor for EF Core
    private Customer() { }

    public Customer(string name, string email)
    {
        SetName(name);
        SetEmail(email);
        Active = true;
        Created = DateTime.UtcNow;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name cannot be empty");
        
        Name = name;
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");
        
        // Simple email validation
        if (!email.Contains('@'))
            throw new DomainException("Invalid email format");
        
        Email = email;
    }

    public void Activate() => Active = true;
    public void Deactivate() => Active = false;
}
```

### Value Object

```csharp
// Domain/ValueObjects/Email.cs
namespace ProjectName.Domain.ValueObjects;

public record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));
        
        if (!value.Contains('@'))
            throw new ArgumentException("Invalid email format", nameof(value));
        
        Value = value.ToLowerInvariant();
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string value) => new(value);
}

// Domain/ValueObjects/Money.cs
namespace ProjectName.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract different currencies");
        
        return new Money(Amount - other.Amount, Currency);
    }
}
```

### Domain Exception

```csharp
// Domain/Exceptions/DomainException.cs
namespace ProjectName.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

// Domain/Exceptions/CustomerNotFoundException.cs
namespace ProjectName.Domain.Exceptions;

public class CustomerNotFoundException : DomainException
{
    public int CustomerId { get; }

    public CustomerNotFoundException(int customerId) 
        : base($"Customer with ID {customerId} was not found")
    {
        CustomerId = customerId;
    }
}
```

---

## Inbound Port Templates (Use Cases)

```csharp
// Application/Ports/Inbound/ICreateCustomerUseCase.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Application.DTOs.Requests;
using ProjectName.Application.DTOs.Responses;

namespace ProjectName.Application.Ports.Inbound;

public interface ICreateCustomerUseCase
{
    Task<IBusinessResult<CustomerResponse>> ExecuteAsync(
        CreateCustomerRequest request, 
        CancellationToken cancellationToken = default);
}

// Application/Ports/Inbound/IGetCustomerUseCase.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Application.DTOs.Responses;

namespace ProjectName.Application.Ports.Inbound;

public interface IGetCustomerUseCase
{
    Task<IBusinessResult<CustomerResponse>> ExecuteByIdAsync(
        int id, 
        CancellationToken cancellationToken = default);
    
    Task<IBusinessResult<CustomerListResponse>> ExecuteAllAsync(
        CancellationToken cancellationToken = default);
}

// Application/Ports/Inbound/IUpdateCustomerUseCase.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Application.DTOs.Requests;
using ProjectName.Application.DTOs.Responses;

namespace ProjectName.Application.Ports.Inbound;

public interface IUpdateCustomerUseCase
{
    Task<IBusinessResult<CustomerResponse>> ExecuteAsync(
        int id,
        UpdateCustomerRequest request, 
        CancellationToken cancellationToken = default);
}

// Application/Ports/Inbound/IDeleteCustomerUseCase.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace ProjectName.Application.Ports.Inbound;

public interface IDeleteCustomerUseCase
{
    Task<IBusinessResult<bool>> ExecuteAsync(
        int id, 
        CancellationToken cancellationToken = default);
}
```

---

## Outbound Port Templates (Driven Interfaces)

```csharp
// Application/Ports/Outbound/ICustomerRepository.cs
using ProjectName.Domain.Entities;

namespace ProjectName.Application.Ports.Outbound;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task DeleteAsync(Customer customer, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Application/Ports/Outbound/IEmailService.cs
namespace ProjectName.Application.Ports.Outbound;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string to, string customerName, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default);
}

// Application/Ports/Outbound/IPaymentGateway.cs
using ProjectName.Domain.ValueObjects;

namespace ProjectName.Application.Ports.Outbound;

public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(string customerId, Money amount, CancellationToken cancellationToken = default);
    Task<RefundResult> ProcessRefundAsync(string paymentId, Money amount, CancellationToken cancellationToken = default);
}

public record PaymentResult(bool Success, string TransactionId, string? ErrorMessage = null);
public record RefundResult(bool Success, string RefundId, string? ErrorMessage = null);

// Application/Ports/Outbound/IEventPublisher.cs
namespace ProjectName.Application.Ports.Outbound;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
```

---

## Use Case Implementation Templates

```csharp
// Application/UseCases/CreateCustomerUseCase.cs
using FluentValidation;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Application.DTOs.Requests;
using ProjectName.Application.DTOs.Responses;
using ProjectName.Application.Ports.Inbound;
using ProjectName.Application.Ports.Outbound;
using ProjectName.Domain.Entities;
using ProjectName.Domain.Events;

namespace ProjectName.Application.UseCases;

public class CreateCustomerUseCase : ICreateCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailService _emailService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IValidator<CreateCustomerRequest> _validator;

    public CreateCustomerUseCase(
        ICustomerRepository customerRepository,
        IEmailService emailService,
        IEventPublisher eventPublisher,
        IValidator<CreateCustomerRequest> validator)
    {
        _customerRepository = customerRepository;
        _emailService = emailService;
        _eventPublisher = eventPublisher;
        _validator = validator;
    }

    public async Task<IBusinessResult<CustomerResponse>> ExecuteAsync(
        CreateCustomerRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validate
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.Errors.ToBusiness<CustomerResponse>();

        // Check if email exists
        var existingCustomer = await _customerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingCustomer != null)
            return default(CustomerResponse).ToBusiness("Email already registered");

        // Create domain entity
        var customer = new Customer(request.Name, request.Email);

        // Persist
        await _customerRepository.AddAsync(customer, cancellationToken);
        await _customerRepository.SaveChangesAsync(cancellationToken);

        // Send welcome email (fire and forget)
        _ = _emailService.SendWelcomeEmailAsync(customer.Email, customer.Name, cancellationToken);

        // Publish event
        await _eventPublisher.PublishAsync(new CustomerCreatedEvent(customer.Id, customer.Name, customer.Email), cancellationToken);

        // Return response
        var response = new CustomerResponse(customer.Id, customer.Name, customer.Email, customer.Active, customer.Created);
        return response.ToBusiness();
    }
}

// Application/UseCases/GetCustomerUseCase.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Application.DTOs.Responses;
using ProjectName.Application.Ports.Inbound;
using ProjectName.Application.Ports.Outbound;

namespace ProjectName.Application.UseCases;

public class GetCustomerUseCase : IGetCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IBusinessResult<CustomerResponse>> ExecuteByIdAsync(
        int id, 
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        
        if (customer == null)
            return default(CustomerResponse).ToBusiness("Customer not found");

        var response = new CustomerResponse(customer.Id, customer.Name, customer.Email, customer.Active, customer.Created);
        return response.ToBusiness();
    }

    public async Task<IBusinessResult<CustomerListResponse>> ExecuteAllAsync(
        CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);

        var items = customers.Select(c => 
            new CustomerResponse(c.Id, c.Name, c.Email, c.Active, c.Created)
        ).ToList();

        var response = new CustomerListResponse(items, items.Count);
        return response.ToBusiness();
    }
}
```

---

## DTOs

```csharp
// Application/DTOs/Requests/CreateCustomerRequest.cs
namespace ProjectName.Application.DTOs.Requests;

public record CreateCustomerRequest(string Name, string Email);

// Application/DTOs/Requests/UpdateCustomerRequest.cs
namespace ProjectName.Application.DTOs.Requests;

public record UpdateCustomerRequest(string Name, string Email, bool Active);

// Application/DTOs/Responses/CustomerResponse.cs
namespace ProjectName.Application.DTOs.Responses;

public record CustomerResponse(int Id, string Name, string Email, bool Active, DateTime Created);

// Application/DTOs/Responses/CustomerListResponse.cs
namespace ProjectName.Application.DTOs.Responses;

public record CustomerListResponse(IList<CustomerResponse> Items, int TotalCount);
```

---

## Outbound Adapter Templates (Infrastructure)

### Repository Adapter

```csharp
// Infrastructure/Adapters/Outbound/Persistence/CustomerRepository.cs
using Microsoft.EntityFrameworkCore;
using ProjectName.Application.Ports.Outbound;
using ProjectName.Domain.Entities;

namespace ProjectName.Infrastructure.Adapters.Outbound.Persistence;

public class CustomerRepository : ICustomerRepository
{
    private readonly DataContext _context;

    public CustomerRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<IList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers.ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }

    public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _context.Customers.Update(customer);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _context.Customers.Remove(customer);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### Email Service Adapter

```csharp
// Infrastructure/Adapters/Outbound/Email/SmtpEmailService.cs
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using ProjectName.Application.Ports.Outbound;

namespace ProjectName.Infrastructure.Adapters.Outbound.Email;

public class SmtpEmailService : IEmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;

    public SmtpEmailService(IConfiguration configuration)
    {
        _fromEmail = configuration["Email:From"] ?? "noreply@example.com";
        _smtpClient = new SmtpClient(configuration["Email:SmtpHost"])
        {
            Port = int.Parse(configuration["Email:SmtpPort"] ?? "587"),
            Credentials = new NetworkCredential(
                configuration["Email:Username"],
                configuration["Email:Password"]
            ),
            EnableSsl = true
        };
    }

    public async Task SendWelcomeEmailAsync(string to, string customerName, CancellationToken cancellationToken = default)
    {
        var message = new MailMessage(_fromEmail, to)
        {
            Subject = "Welcome to Our Service!",
            Body = $"Hello {customerName},\n\nWelcome to our service!",
            IsBodyHtml = false
        };

        await _smtpClient.SendMailAsync(message, cancellationToken);
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
    {
        var message = new MailMessage(_fromEmail, to)
        {
            Subject = "Password Reset Request",
            Body = $"Click the following link to reset your password: {resetLink}",
            IsBodyHtml = false
        };

        await _smtpClient.SendMailAsync(message, cancellationToken);
    }
}
```

---

## Inbound Adapter (HTTP Controller)

```csharp
// WebAPI/Adapters/Inbound/Http/Controllers/CustomersController.cs
using Microsoft.AspNetCore.Mvc;
using ProjectName.Application.DTOs.Requests;
using ProjectName.Application.Ports.Inbound;

namespace ProjectName.WebAPI.Adapters.Inbound.Http.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICreateCustomerUseCase _createCustomerUseCase;
    private readonly IGetCustomerUseCase _getCustomerUseCase;
    private readonly IUpdateCustomerUseCase _updateCustomerUseCase;
    private readonly IDeleteCustomerUseCase _deleteCustomerUseCase;

    public CustomersController(
        ICreateCustomerUseCase createCustomerUseCase,
        IGetCustomerUseCase getCustomerUseCase,
        IUpdateCustomerUseCase updateCustomerUseCase,
        IDeleteCustomerUseCase deleteCustomerUseCase)
    {
        _createCustomerUseCase = createCustomerUseCase;
        _getCustomerUseCase = getCustomerUseCase;
        _updateCustomerUseCase = updateCustomerUseCase;
        _deleteCustomerUseCase = deleteCustomerUseCase;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _getCustomerUseCase.ExecuteAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _getCustomerUseCase.ExecuteByIdAsync(id, cancellationToken);
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _createCustomerUseCase.ExecuteAsync(request, cancellationToken);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await _updateCustomerUseCase.ExecuteAsync(id, request, cancellationToken);
        if (result.HasErrors)
            return BadRequest(result);
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _deleteCustomerUseCase.ExecuteAsync(id, cancellationToken);
        if (result.HasErrors)
            return NotFound(result);
        return NoContent();
    }
}
```

---

## Service Registration

```csharp
// Extensions/ServiceBuilderExtensions.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectName.Application.Ports.Inbound;
using ProjectName.Application.Ports.Outbound;
using ProjectName.Application.UseCases;
using ProjectName.Application.Validators;
using ProjectName.Infrastructure.Adapters.Outbound.Email;
using ProjectName.Infrastructure.Adapters.Outbound.Messaging;
using ProjectName.Infrastructure.Adapters.Outbound.Persistence;

namespace ProjectName.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();

        // Outbound Adapters (Secondary/Driven)
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();

        // Inbound Ports (Use Cases)
        services.AddScoped<ICreateCustomerUseCase, CreateCustomerUseCase>();
        services.AddScoped<IGetCustomerUseCase, GetCustomerUseCase>();
        services.AddScoped<IUpdateCustomerUseCase, UpdateCustomerUseCase>();
        services.AddScoped<IDeleteCustomerUseCase, DeleteCustomerUseCase>();

        return services;
    }
}
```

---

## Dependency Rule

```
┌─────────────────────────────────────────────────────────────┐
│                        WebAPI                               │
│  (Inbound Adapter: HTTP Controllers)                        │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Application                             │
│  ┌─────────────────┐     ┌─────────────────┐               │
│  │ Inbound Ports   │     │ Outbound Ports  │               │
│  │ (Use Cases)     │     │ (Interfaces)    │               │
│  └────────┬────────┘     └────────┬────────┘               │
│           │                       │                         │
│           └───────────┬───────────┘                         │
│                       │                                     │
│                 Use Cases                                   │
└───────────────────────┼─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│                       Domain                                │
│  (Entities, Value Objects, Domain Events, Exceptions)       │
│  NO EXTERNAL DEPENDENCIES                                   │
└─────────────────────────────────────────────────────────────┘
                        ▲
                        │
┌───────────────────────┴─────────────────────────────────────┐
│                    Infrastructure                           │
│  (Outbound Adapters: Repositories, External Services)       │
└─────────────────────────────────────────────────────────────┘
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Clean Architecture Template](template-clean-architecture.md)
- [DDD Template](template-ddd.md)
- [Complex N-Layers Structure](structure-complex-nlayers.md)

