# Complex N-Layers Project Structure

> **AI Agent Instruction**: Use this structure for enterprise-grade applications with complex business logic. Four layers: Core, Infrastructure, Application, and WebAPI.

---

## When to Use

- Large enterprise applications (15+ entities)
- Complex business logic requiring dedicated application layer
- Projects with multiple data sources
- Applications using CQRS, DDD, or Ports & Adapters patterns
- Long-term projects with multiple teams
- Applications requiring extensive mapping and transformation

---

## Directory Structure

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   ├── ProjectName.Core.csproj
    │   ├── Entities/
    │   │   ├── Customer.cs
    │   │   └── Contact.cs
    │   ├── ValueObjects/
    │   │   ├── Customers/
    │   │   │   ├── CustomerDto.cs
    │   │   │   ├── CustomerCreateDto.cs
    │   │   │   ├── CustomerUpdateDto.cs
    │   │   │   └── CustomerFilterDto.cs
    │   │   └── Contacts/
    │   │       ├── ContactDto.cs
    │   │       ├── ContactCreateDto.cs
    │   │       └── ContactUpdateDto.cs
    │   ├── Validators/
    │   │   ├── Customers/
    │   │   │   ├── CustomerCreateValidator.cs
    │   │   │   └── CustomerUpdateValidator.cs
    │   │   └── Contacts/
    │   │       ├── ContactCreateValidator.cs
    │   │       └── ContactUpdateValidator.cs
    │   ├── Contract/
    │   │   ├── Services/
    │   │   │   ├── ICustomerService.cs
    │   │   │   └── IContactService.cs
    │   │   ├── Specifications/
    │   │   │   └── ICustomerSpecification.cs
    │   │   └── Pipelines/
    │   │       └── ICustomerPipeline.cs
    │   ├── Specifications/
    │   │   ├── CustomerByFilterSpec.cs
    │   │   └── CustomerActiveSpec.cs
    │   └── Resources/
    │       └── Messages.resx
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── EFCore/
    │   │   │   ├── DataContext.cs
    │   │   │   └── Configurations/
    │   │   │       ├── CustomerConfiguration.cs
    │   │   │       └── ContactConfiguration.cs
    │   │   └── Dapper/
    │   │       └── CustomerDapperRepository.cs
    │   └── Migrations/
    │       └── (EF Core migrations)
    ├── ProjectName.Application/
    │   ├── ProjectName.Application.csproj
    │   ├── Services/
    │   │   ├── CustomerService.cs
    │   │   └── ContactService.cs
    │   ├── Mappings/
    │   │   ├── CustomerProfile.cs
    │   │   └── ContactProfile.cs
    │   ├── Pipelines/
    │   │   ├── CustomerPipeline.cs
    │   │   └── Operations/
    │   │       ├── ValidateCustomerOperation.cs
    │   │       ├── CreateCustomerOperation.cs
    │   │       └── NotifyCustomerOperation.cs
    │   └── FacadeService.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── appsettings.Staging.json
        ├── NLog.config
        ├── Controllers/
        │   ├── CustomerController.cs
        │   └── ContactController.cs
        ├── Extensions/
        │   └── ServiceBuilderExtensions.cs
        ├── Middlewares/
        │   ├── ExceptionMiddleware.cs
        │   └── CorrelationIdMiddleware.cs
        ├── Properties/
        │   └── launchSettings.json
        └── HealthChecks/
            └── CustomHealthCheck.cs
```

---

## Layer Responsibilities

### Core Layer

- **Entities**: Domain entities (Aggregates, Value Objects)
- **ValueObjects**: DTOs organized by feature/entity
- **Validators**: FluentValidation validators per operation
- **Contract**: Interfaces for services, specifications, pipelines
- **Specifications**: Query specifications for repositories
- **Resources**: Localization files

### Infrastructure Layer

- **Data/EFCore**: Entity Framework Core implementation
- **Data/Dapper**: Dapper queries for complex/optimized queries
- **Migrations**: Database migrations

### Application Layer

- **Services**: Business logic implementation
- **Mappings**: AutoMapper profiles
- **Pipelines**: Pipeline pattern implementations
- **Operations**: Individual pipeline operations

### WebAPI Layer

- **Controllers**: REST API endpoints
- **Extensions**: Service registration
- **Middlewares**: Custom middleware components
- **HealthChecks**: Custom health checks

---

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Entity | PascalCase, singular | `Customer.cs` |
| DTO | PascalCase + Dto suffix | `CustomerDto.cs` |
| Create DTO | Entity + CreateDto | `CustomerCreateDto.cs` |
| Update DTO | Entity + UpdateDto | `CustomerUpdateDto.cs` |
| Filter DTO | Entity + FilterDto | `CustomerFilterDto.cs` |
| Validator | DTO name + Validator | `CustomerCreateValidator.cs` |
| Service Interface | I + Entity + Service | `ICustomerService.cs` |
| Service Implementation | Entity + Service | `CustomerService.cs` |
| Controller | Entity + Controller | `CustomerController.cs` |
| Configuration | Entity + Configuration | `CustomerConfiguration.cs` |
| Specification | Descriptive + Spec | `CustomerByFilterSpec.cs` |
| Profile (AutoMapper) | Entity + Profile | `CustomerProfile.cs` |
| Operation | Verb + Entity + Operation | `ValidateCustomerOperation.cs` |
| Middleware | Feature + Middleware | `ExceptionMiddleware.cs` |

---

## Namespaces

```csharp
// Core
ProjectName.Core.Entities
ProjectName.Core.ValueObjects.Customers
ProjectName.Core.ValueObjects.Contacts
ProjectName.Core.Validators.Customers
ProjectName.Core.Validators.Contacts
ProjectName.Core.Contract.Services
ProjectName.Core.Contract.Specifications
ProjectName.Core.Contract.Pipelines
ProjectName.Core.Specifications
ProjectName.Core.Resources

// Infrastructure
ProjectName.Infrastructure.Data.EFCore
ProjectName.Infrastructure.Data.EFCore.Configurations
ProjectName.Infrastructure.Data.Dapper

// Application
ProjectName.Application.Services
ProjectName.Application.Mappings
ProjectName.Application.Pipelines
ProjectName.Application.Pipelines.Operations

// WebAPI
ProjectName.WebAPI.Controllers
ProjectName.WebAPI.Extensions
ProjectName.WebAPI.Middlewares
ProjectName.WebAPI.HealthChecks
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
    <ProjectReference Include="..\ProjectName.Core\ProjectName.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Dapper" Version="2.*" />
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
    <PackageReference Include="AutoMapper" Version="13.*" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.*" />
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
    <ProjectReference Include="..\ProjectName.Application\ProjectName.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.WebAPI" Version="9.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
    <PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="9.*" />
    <PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="9.*" />
  </ItemGroup>
</Project>
```

---

## Configuration Files

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProjectDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;",
    "ReadOnlyConnection": "Server=localhost;Database=ProjectDb;User Id=readonly;Password=YourPassword;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Program.cs Template

```csharp
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var startup = new Startup(builder.Configuration);
    startup.ConfigureServices(builder.Services);

    var app = builder.Build();

    startup.Configure(app, app.Environment);

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped due to exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
```

---

## Startup.cs Template

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ProjectName.WebAPI.Extensions;
using ProjectName.WebAPI.Middlewares;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "ProjectName API", Version = "v1" });
        });

        services.AddFluentValidationAutoValidation();
        services.AddMyServices(Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectName API v1"));
        }

        app.UseCorrelationId();
        app.UseExceptionHandling();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        });
    }
}
```

---

## Service Extensions Template

```csharp
// Extensions/ServiceBuilderExtensions.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using ProjectName.Application.Mappings;
using ProjectName.Application.Services;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.Validators.Customers;
using ProjectName.Infrastructure.Data.EFCore;

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

        // Mvp24Hours Services
        services.AddMvp24HoursDbContext<DataContext>();
        services.AddMvp24HoursRepository();
        services.AddMvp24HoursPipeline();

        // AutoMapper
        services.AddAutoMapper(typeof(CustomerProfile).Assembly);

        // Application Services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IContactService, ContactService>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<CustomerCreateValidator>();

        // Health Checks
        services.AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "sqlserver",
                tags: new[] { "db", "sql" });

        return services;
    }
}
```

---

## Controller Template

```csharp
// Controllers/CustomerController.cs
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.ValueObjects.Customers;

namespace ProjectName.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IBusinessResult<IList<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] CustomerFilterDto filter)
    {
        var result = await _customerService.GetAllAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IBusinessResult<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _customerService.GetByIdAsync(id);
        if (!result.HasData)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(IBusinessResult<CustomerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
    {
        var result = await _customerService.CreateAsync(dto);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(IBusinessResult<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
    {
        var result = await _customerService.UpdateAsync(id, dto);
        if (result.HasErrors)
            return BadRequest(result);
        if (!result.HasData)
            return NotFound();
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customerService.DeleteAsync(id);
        if (result.HasErrors)
            return NotFound();
        return NoContent();
    }
}
```

---

## Service Interface Template

```csharp
// Core/Contract/Services/ICustomerService.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.ValueObjects.Customers;

namespace ProjectName.Core.Contract.Services;

public interface ICustomerService
{
    Task<IBusinessResult<IList<CustomerDto>>> GetAllAsync(CustomerFilterDto? filter = null);
    Task<IBusinessResult<CustomerDto>> GetByIdAsync(int id);
    Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto);
    Task<IBusinessResult<CustomerDto>> UpdateAsync(int id, CustomerUpdateDto dto);
    Task<IBusinessResult<bool>> DeleteAsync(int id);
}
```

---

## Service Implementation Template

```csharp
// Application/Services/CustomerService.cs
using AutoMapper;
using FluentValidation;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.Entities;
using ProjectName.Core.Specifications;
using ProjectName.Core.ValueObjects.Customers;

namespace ProjectName.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWorkAsync _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<CustomerCreateDto> _createValidator;
    private readonly IValidator<CustomerUpdateDto> _updateValidator;

    public CustomerService(
        IUnitOfWorkAsync uow,
        IMapper mapper,
        IValidator<CustomerCreateDto> createValidator,
        IValidator<CustomerUpdateDto> updateValidator)
    {
        _uow = uow;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IBusinessResult<IList<CustomerDto>>> GetAllAsync(CustomerFilterDto? filter = null)
    {
        var repository = _uow.GetRepository<Customer>();
        
        IList<Customer> customers;
        if (filter != null)
        {
            var spec = new CustomerByFilterSpec(filter);
            customers = await repository.ToListAsync(spec);
        }
        else
        {
            customers = await repository.ToListAsync();
        }

        var dtos = _mapper.Map<IList<CustomerDto>>(customers);
        return dtos.ToBusiness();
    }

    public async Task<IBusinessResult<CustomerDto>> GetByIdAsync(int id)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return default(CustomerDto).ToBusiness();

        var dto = _mapper.Map<CustomerDto>(customer);
        return dto.ToBusiness();
    }

    public async Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return validation.Errors.ToBusiness<CustomerDto>();

        var customer = _mapper.Map<Customer>(dto);
        
        var repository = _uow.GetRepository<Customer>();
        await repository.AddAsync(customer);
        await _uow.SaveChangesAsync();

        var resultDto = _mapper.Map<CustomerDto>(customer);
        return resultDto.ToBusiness();
    }

    public async Task<IBusinessResult<CustomerDto>> UpdateAsync(int id, CustomerUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return validation.Errors.ToBusiness<CustomerDto>();

        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return default(CustomerDto).ToBusiness();

        _mapper.Map(dto, customer);
        
        await repository.ModifyAsync(customer);
        await _uow.SaveChangesAsync();

        var resultDto = _mapper.Map<CustomerDto>(customer);
        return resultDto.ToBusiness();
    }

    public async Task<IBusinessResult<bool>> DeleteAsync(int id)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return false.ToBusiness("Customer not found");

        await repository.RemoveAsync(customer);
        await _uow.SaveChangesAsync();

        return true.ToBusiness();
    }
}
```

---

## AutoMapper Profile Template

```csharp
// Application/Mappings/CustomerProfile.cs
using AutoMapper;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects.Customers;

namespace ProjectName.Application.Mappings;

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        // Entity -> DTO
        CreateMap<Customer, CustomerDto>();

        // DTO -> Entity
        CreateMap<CustomerCreateDto, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Created, opt => opt.Ignore())
            .ForMember(dest => dest.Active, opt => opt.MapFrom(src => true));

        CreateMap<CustomerUpdateDto, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Created, opt => opt.Ignore());
    }
}
```

---

## Specification Template

```csharp
// Core/Specifications/CustomerByFilterSpec.cs
using Mvp24Hours.Core.Contract.Domain;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects.Customers;
using System.Linq.Expressions;

namespace ProjectName.Core.Specifications;

public class CustomerByFilterSpec : ISpecificationQuery<Customer>
{
    private readonly CustomerFilterDto _filter;

    public CustomerByFilterSpec(CustomerFilterDto filter)
    {
        _filter = filter;
    }

    public Expression<Func<Customer, bool>> IsSatisfiedByExpression
    {
        get
        {
            return x =>
                (string.IsNullOrEmpty(_filter.Name) || x.Name.Contains(_filter.Name)) &&
                (string.IsNullOrEmpty(_filter.Email) || x.Email.Contains(_filter.Email)) &&
                (!_filter.Active.HasValue || x.Active == _filter.Active.Value);
        }
    }
}
```

---

## Pipeline Template

```csharp
// Application/Pipelines/CustomerPipeline.cs
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using ProjectName.Application.Pipelines.Operations;

namespace ProjectName.Application.Pipelines;

public class CustomerPipeline : Pipeline
{
    public CustomerPipeline(IServiceProvider serviceProvider)
    {
        Add<ValidateCustomerOperation>(serviceProvider);
        Add<CreateCustomerOperation>(serviceProvider);
        Add<NotifyCustomerOperation>(serviceProvider);
    }
}
```

---

## Pipeline Operation Template

```csharp
// Application/Pipelines/Operations/ValidateCustomerOperation.cs
using FluentValidation;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.DTOs;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using ProjectName.Core.ValueObjects.Customers;

namespace ProjectName.Application.Pipelines.Operations;

public class ValidateCustomerOperation : OperationBase<CustomerCreateDto>
{
    private readonly IValidator<CustomerCreateDto> _validator;

    public ValidateCustomerOperation(IValidator<CustomerCreateDto> validator)
    {
        _validator = validator;
    }

    public override async Task<IPipelineMessage> ExecuteAsync(IPipelineMessage input)
    {
        var dto = input.GetContent<CustomerCreateDto>();
        var validation = await _validator.ValidateAsync(dto);
        
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                input.Messages.Add(new MessageResult(error.ErrorMessage, Mvp24Hours.Core.Enums.MessageType.Error));
            }
            input.SetLock();
        }
        
        return input;
    }
}
```

---

## Middleware Templates

```csharp
// Middlewares/ExceptionMiddleware.cs
using System.Net;
using System.Text.Json;

namespace ProjectName.WebAPI.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Internal Server Error",
            Detail = exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}

public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionMiddleware>();
    }
}
```

```csharp
// Middlewares/CorrelationIdMiddleware.cs
namespace ProjectName.WebAPI.Middlewares;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();

        context.Items[CorrelationIdHeader] = correlationId;
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        await _next(context);
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
```

---

## With RabbitMQ Extension

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   ├── ... (same as above)
    │   └── Messages/
    │       ├── CustomerCreatedMessage.cs
    │       ├── CustomerUpdatedMessage.cs
    │       └── CustomerDeletedMessage.cs
    ├── ProjectName.Infrastructure/
    │   └── ... (same as above)
    ├── ProjectName.Application/
    │   ├── ... (same as above)
    │   └── Consumers/
    │       ├── CustomerCreatedConsumer.cs
    │       └── CustomerUpdatedConsumer.cs
    └── ProjectName.WebAPI/
        ├── ... (same as above)
        └── HostedServices/
            └── RabbitMQConsumerHostedService.cs
```

---

## With Ports & Adapters Pattern

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   ├── ... (same as above)
    │   └── Ports/
    │       ├── Inbound/
    │       │   └── ICustomerUseCase.cs
    │       └── Outbound/
    │           ├── ICustomerRepository.cs
    │           └── INotificationService.cs
    ├── ProjectName.Infrastructure/
    │   ├── ... (same as above)
    │   └── Adapters/
    │       └── NotificationServiceAdapter.cs
    ├── ProjectName.Application/
    │   └── ... (same as above)
    └── ProjectName.WebAPI/
        └── ... (same as above)
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Database Patterns](database-patterns.md)
- [Minimal API Structure](structure-minimal-api.md)
- [Simple N-Layers Structure](structure-simple-nlayers.md)

