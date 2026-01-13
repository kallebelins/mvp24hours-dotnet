# CQRS Architecture Template

> **AI Agent Instruction**: Use this template for applications that benefit from separating read and write operations. CQRS (Command Query Responsibility Segregation) is ideal for complex domains with different read/write patterns.

---

## When to Use CQRS

### Recommended Scenarios
- Complex business domains with different read/write models
- High-performance read operations with denormalized views
- Applications requiring audit trails and event history
- Systems with asymmetric read/write workloads
- Collaborative domains with conflict resolution needs

### Not Recommended
- Simple CRUD applications
- Small teams without CQRS experience
- Projects with tight deadlines and simple requirements
- Applications with similar read/write models

---

## Directory Structure

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   ├── ProjectName.Core.csproj
    │   ├── Entities/
    │   │   └── Customer.cs
    │   ├── ValueObjects/
    │   │   └── Email.cs
    │   ├── Commands/
    │   │   ├── Customers/
    │   │   │   ├── CreateCustomerCommand.cs
    │   │   │   ├── UpdateCustomerCommand.cs
    │   │   │   └── DeleteCustomerCommand.cs
    │   │   └── ICommand.cs
    │   ├── Queries/
    │   │   ├── Customers/
    │   │   │   ├── GetCustomerByIdQuery.cs
    │   │   │   ├── GetAllCustomersQuery.cs
    │   │   │   └── GetCustomersByFilterQuery.cs
    │   │   └── IQuery.cs
    │   ├── Results/
    │   │   ├── Customers/
    │   │   │   ├── CustomerResult.cs
    │   │   │   └── CustomerListResult.cs
    │   │   └── IQueryResult.cs
    │   ├── Validators/
    │   │   └── Customers/
    │   │       ├── CreateCustomerValidator.cs
    │   │       └── UpdateCustomerValidator.cs
    │   └── Contract/
    │       ├── Handlers/
    │       │   ├── ICommandHandler.cs
    │       │   └── IQueryHandler.cs
    │       └── Repositories/
    │           ├── ICustomerReadRepository.cs
    │           └── ICustomerWriteRepository.cs
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── Write/
    │   │   │   ├── WriteDbContext.cs
    │   │   │   └── Configurations/
    │   │   │       └── CustomerConfiguration.cs
    │   │   └── Read/
    │   │       ├── ReadDbContext.cs
    │   │       └── Views/
    │   │           └── CustomerReadModel.cs
    │   └── Repositories/
    │       ├── CustomerWriteRepository.cs
    │       └── CustomerReadRepository.cs
    ├── ProjectName.Application/
    │   ├── ProjectName.Application.csproj
    │   ├── Handlers/
    │   │   ├── Commands/
    │   │   │   ├── CreateCustomerHandler.cs
    │   │   │   ├── UpdateCustomerHandler.cs
    │   │   │   └── DeleteCustomerHandler.cs
    │   │   └── Queries/
    │   │       ├── GetCustomerByIdHandler.cs
    │   │       ├── GetAllCustomersHandler.cs
    │   │       └── GetCustomersByFilterHandler.cs
    │   ├── Behaviors/
    │   │   ├── ValidationBehavior.cs
    │   │   └── LoggingBehavior.cs
    │   └── Mappings/
    │       └── CustomerProfile.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── Controllers/
        │   └── CustomersController.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

---

## Core Concepts

### Commands (Write Operations)
- Represent intent to change state
- Should be immutable
- Named with verb in imperative form (Create, Update, Delete)
- Return void or simple acknowledgment

### Queries (Read Operations)
- Request for data without side effects
- Can return complex, denormalized results
- Named with verb "Get" + description
- Optimized for specific use cases

### Handlers
- Single responsibility: one handler per command/query
- Command handlers modify state
- Query handlers only read data

---

## Namespaces

```csharp
// Core
ProjectName.Core.Entities
ProjectName.Core.ValueObjects
ProjectName.Core.Commands
ProjectName.Core.Commands.Customers
ProjectName.Core.Queries
ProjectName.Core.Queries.Customers
ProjectName.Core.Results
ProjectName.Core.Results.Customers
ProjectName.Core.Validators.Customers
ProjectName.Core.Contract.Handlers
ProjectName.Core.Contract.Repositories

// Infrastructure
ProjectName.Infrastructure.Data.Write
ProjectName.Infrastructure.Data.Read
ProjectName.Infrastructure.Repositories

// Application
ProjectName.Application.Handlers.Commands
ProjectName.Application.Handlers.Queries
ProjectName.Application.Behaviors
ProjectName.Application.Mappings

// WebAPI
ProjectName.WebAPI.Controllers
ProjectName.WebAPI.Extensions
```

---

## Project Files (.csproj)

### Core Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Core" Version="9.*" />
    <PackageReference Include="FluentValidation" Version="11.*" />
    <PackageReference Include="MediatR.Contracts" Version="2.*" />
  </ItemGroup>
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
    <ProjectReference Include="..\ProjectName.Core\ProjectName.Core.csproj" />
    <ProjectReference Include="..\ProjectName.Infrastructure\ProjectName.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Application" Version="9.*" />
    <PackageReference Include="MediatR" Version="12.*" />
    <PackageReference Include="AutoMapper" Version="13.*" />
  </ItemGroup>
</Project>
```

---

## Command Templates

### Command Definition

```csharp
// Core/Commands/Customers/CreateCustomerCommand.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Core.Commands.Customers;

public record CreateCustomerCommand(
    string Name,
    string Email,
    string Phone
) : IRequest<IBusinessResult<CustomerResult>>;
```

### Command with No Return

```csharp
// Core/Commands/Customers/DeleteCustomerCommand.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace ProjectName.Core.Commands.Customers;

public record DeleteCustomerCommand(int Id) : IRequest<IBusinessResult<bool>>;
```

### Update Command

```csharp
// Core/Commands/Customers/UpdateCustomerCommand.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Core.Commands.Customers;

public record UpdateCustomerCommand(
    int Id,
    string Name,
    string Email,
    string Phone,
    bool Active
) : IRequest<IBusinessResult<CustomerResult>>;
```

---

## Query Templates

### Query Definition

```csharp
// Core/Queries/Customers/GetCustomerByIdQuery.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Core.Queries.Customers;

public record GetCustomerByIdQuery(int Id) : IRequest<IBusinessResult<CustomerResult>>;
```

### Query with Filter

```csharp
// Core/Queries/Customers/GetCustomersByFilterQuery.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Core.Queries.Customers;

public record GetCustomersByFilterQuery(
    string? Name,
    string? Email,
    bool? Active,
    int Page = 1,
    int PageSize = 10
) : IRequest<IBusinessResult<CustomerListResult>>;
```

### List Query

```csharp
// Core/Queries/Customers/GetAllCustomersQuery.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Core.Queries.Customers;

public record GetAllCustomersQuery : IRequest<IBusinessResult<CustomerListResult>>;
```

---

## Result Templates

```csharp
// Core/Results/Customers/CustomerResult.cs
namespace ProjectName.Core.Results.Customers;

public record CustomerResult(
    int Id,
    string Name,
    string Email,
    string Phone,
    bool Active,
    DateTime Created
);

// Core/Results/Customers/CustomerListResult.cs
namespace ProjectName.Core.Results.Customers;

public record CustomerListResult(
    IList<CustomerResult> Items,
    int TotalCount,
    int Page,
    int PageSize
);
```

---

## Command Handler Templates

### Create Handler

```csharp
// Application/Handlers/Commands/CreateCustomerHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Commands.Customers;
using ProjectName.Core.Contract.Repositories;
using ProjectName.Core.Entities;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Application.Handlers.Commands;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, IBusinessResult<CustomerResult>>
{
    private readonly ICustomerWriteRepository _repository;

    public CreateCustomerHandler(ICustomerWriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<CustomerResult>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Active = true,
            Created = DateTime.UtcNow
        };

        await _repository.AddAsync(customer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var result = new CustomerResult(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Active,
            customer.Created
        );

        return result.ToBusiness();
    }
}
```

### Update Handler

```csharp
// Application/Handlers/Commands/UpdateCustomerHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Commands.Customers;
using ProjectName.Core.Contract.Repositories;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Application.Handlers.Commands;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, IBusinessResult<CustomerResult>>
{
    private readonly ICustomerWriteRepository _repository;

    public UpdateCustomerHandler(ICustomerWriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<CustomerResult>> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (customer == null)
            return default(CustomerResult).ToBusiness("Customer not found");

        customer.Name = request.Name;
        customer.Email = request.Email;
        customer.Phone = request.Phone;
        customer.Active = request.Active;

        await _repository.UpdateAsync(customer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var result = new CustomerResult(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Active,
            customer.Created
        );

        return result.ToBusiness();
    }
}
```

### Delete Handler

```csharp
// Application/Handlers/Commands/DeleteCustomerHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Commands.Customers;
using ProjectName.Core.Contract.Repositories;

namespace ProjectName.Application.Handlers.Commands;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, IBusinessResult<bool>>
{
    private readonly ICustomerWriteRepository _repository;

    public DeleteCustomerHandler(ICustomerWriteRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<bool>> Handle(
        DeleteCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (customer == null)
            return false.ToBusiness("Customer not found");

        await _repository.DeleteAsync(customer, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return true.ToBusiness();
    }
}
```

---

## Query Handler Templates

### Get By Id Handler

```csharp
// Application/Handlers/Queries/GetCustomerByIdHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Contract.Repositories;
using ProjectName.Core.Queries.Customers;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Application.Handlers.Queries;

public class GetCustomerByIdHandler : IRequestHandler<GetCustomerByIdQuery, IBusinessResult<CustomerResult>>
{
    private readonly ICustomerReadRepository _repository;

    public GetCustomerByIdHandler(ICustomerReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<CustomerResult>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        if (customer == null)
            return default(CustomerResult).ToBusiness("Customer not found");

        var result = new CustomerResult(
            customer.Id,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Active,
            customer.Created
        );

        return result.ToBusiness();
    }
}
```

### Get All Handler

```csharp
// Application/Handlers/Queries/GetAllCustomersHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Contract.Repositories;
using ProjectName.Core.Queries.Customers;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Application.Handlers.Queries;

public class GetAllCustomersHandler : IRequestHandler<GetAllCustomersQuery, IBusinessResult<CustomerListResult>>
{
    private readonly ICustomerReadRepository _repository;

    public GetAllCustomersHandler(ICustomerReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<CustomerListResult>> Handle(
        GetAllCustomersQuery request,
        CancellationToken cancellationToken)
    {
        var customers = await _repository.GetAllAsync(cancellationToken);

        var items = customers.Select(c => new CustomerResult(
            c.Id,
            c.Name,
            c.Email,
            c.Phone,
            c.Active,
            c.Created
        )).ToList();

        var result = new CustomerListResult(items, items.Count, 1, items.Count);

        return result.ToBusiness();
    }
}
```

### Filter Handler with Pagination

```csharp
// Application/Handlers/Queries/GetCustomersByFilterHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Contract.Repositories;
using ProjectName.Core.Queries.Customers;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Application.Handlers.Queries;

public class GetCustomersByFilterHandler : IRequestHandler<GetCustomersByFilterQuery, IBusinessResult<CustomerListResult>>
{
    private readonly ICustomerReadRepository _repository;

    public GetCustomersByFilterHandler(ICustomerReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<IBusinessResult<CustomerListResult>> Handle(
        GetCustomersByFilterQuery request,
        CancellationToken cancellationToken)
    {
        var (customers, totalCount) = await _repository.GetByFilterAsync(
            request.Name,
            request.Email,
            request.Active,
            request.Page,
            request.PageSize,
            cancellationToken
        );

        var items = customers.Select(c => new CustomerResult(
            c.Id,
            c.Name,
            c.Email,
            c.Phone,
            c.Active,
            c.Created
        )).ToList();

        var result = new CustomerListResult(items, totalCount, request.Page, request.PageSize);

        return result.ToBusiness();
    }
}
```

---

## Repository Interfaces

```csharp
// Core/Contract/Repositories/ICustomerWriteRepository.cs
using ProjectName.Core.Entities;

namespace ProjectName.Core.Contract.Repositories;

public interface ICustomerWriteRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task DeleteAsync(Customer customer, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Core/Contract/Repositories/ICustomerReadRepository.cs
using ProjectName.Core.Entities;

namespace ProjectName.Core.Contract.Repositories;

public interface ICustomerReadRepository
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IList<Customer> Items, int TotalCount)> GetByFilterAsync(
        string? name,
        string? email,
        bool? active,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}
```

---

## Validation Behavior (Cross-Cutting)

```csharp
// Application/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace ProjectName.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
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
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Return business result with errors
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && 
                responseType.GetGenericTypeDefinition() == typeof(IBusinessResult<>))
            {
                var dataType = responseType.GetGenericArguments()[0];
                var method = typeof(BusinessExtensions)
                    .GetMethod(nameof(BusinessExtensions.ToBusiness), new[] { typeof(IList<FluentValidation.Results.ValidationFailure>) })
                    ?.MakeGenericMethod(dataType);
                
                if (method != null)
                    return (TResponse)method.Invoke(null, new object[] { failures })!;
            }
            
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

---

## Controller Template

```csharp
// Controllers/CustomersController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Core.Commands.Customers;
using ProjectName.Core.Queries.Customers;

namespace ProjectName.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllCustomersQuery());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _mediator.Send(new GetCustomerByIdQuery(id));
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpGet("filter")]
    public async Task<IActionResult> GetByFilter(
        [FromQuery] string? name,
        [FromQuery] string? email,
        [FromQuery] bool? active,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetCustomersByFilterQuery(name, email, active, page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await _mediator.Send(command);
        if (result.HasErrors)
            return BadRequest(result);
        if (!result.HasData)
            return NotFound(result);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediator.Send(new DeleteCustomerCommand(id));
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
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProjectName.Application.Behaviors;
using ProjectName.Application.Handlers.Commands;
using ProjectName.Core.Contract.Repositories;
using ProjectName.Core.Validators.Customers;
using ProjectName.Infrastructure.Data.Read;
using ProjectName.Infrastructure.Data.Write;
using ProjectName.Infrastructure.Repositories;

namespace ProjectName.WebAPI.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Write Database
        services.AddDbContext<WriteDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("WriteConnection")));

        // Read Database (can be same or different)
        services.AddDbContext<ReadDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ReadConnection")));

        // MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(CreateCustomerHandler).Assembly);
        });

        // Behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Validators
        services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();

        // Repositories
        services.AddScoped<ICustomerWriteRepository, CustomerWriteRepository>();
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();

        return services;
    }
}
```

---

## Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "WriteConnection": "Server=localhost;Database=ProjectDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;",
    "ReadConnection": "Server=localhost;Database=ProjectDb_Read;User Id=readonly;Password=YourPassword;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## Integration with Mvp24Hours Pipeline

You can combine CQRS with Mvp24Hours Pipeline for complex command operations:

```csharp
// Application/Handlers/Commands/CreateCustomerWithPipelineHandler.cs
using MediatR;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Commands.Customers;
using ProjectName.Core.Results.Customers;

namespace ProjectName.Application.Handlers.Commands;

public class CreateCustomerWithPipelineHandler : IRequestHandler<CreateCustomerCommand, IBusinessResult<CustomerResult>>
{
    private readonly IPipelineAsync _pipeline;

    public CreateCustomerWithPipelineHandler(IPipelineAsync pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task<IBusinessResult<CustomerResult>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var message = await _pipeline
            .Add<ValidateCustomerOperation>()
            .Add<CreateCustomerOperation>()
            .Add<SendWelcomeEmailOperation>()
            .ExecuteAsync(request);

        return message.GetContent<CustomerResult>().ToBusiness();
    }
}
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Event-Driven Architecture](template-event-driven.md)
- [Clean Architecture](template-clean-architecture.md)
- [Complex N-Layers Structure](structure-complex-nlayers.md)

