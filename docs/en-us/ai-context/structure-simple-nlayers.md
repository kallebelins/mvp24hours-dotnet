# Simple N-Layers Project Structure

> **AI Agent Instruction**: Use this structure for medium-sized applications requiring separation of concerns. Three layers: Core, Infrastructure, and WebAPI.

---

## When to Use

- Medium-complexity applications (5-15 entities)
- Projects requiring clear separation of concerns
- Applications with business logic in services
- CRUD operations with some business rules
- Teams with multiple developers
- Projects that may grow over time

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
    │   │   ├── CustomerDto.cs
    │   │   ├── CustomerCreateDto.cs
    │   │   ├── CustomerUpdateDto.cs
    │   │   ├── CustomerFilterDto.cs
    │   │   └── ContactDto.cs
    │   ├── Validators/
    │   │   ├── CustomerCreateValidator.cs
    │   │   ├── CustomerUpdateValidator.cs
    │   │   └── ContactValidator.cs
    │   ├── Contract/
    │   │   └── Services/
    │   │       ├── ICustomerService.cs
    │   │       └── IContactService.cs
    │   └── Resources/
    │       └── Messages.resx
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── DataContext.cs
    │   │   └── Configurations/
    │   │       ├── CustomerConfiguration.cs
    │   │       └── ContactConfiguration.cs
    │   └── Migrations/
    │       └── (EF Core migrations)
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
        └── Properties/
            └── launchSettings.json
```

---

## Layer Responsibilities

### Core Layer

- **Entities**: Domain entities inheriting from `EntityBase` or `EntityBaseLog`
- **ValueObjects**: DTOs for data transfer
- **Validators**: FluentValidation validators
- **Contract**: Service interfaces (abstraction)
- **Resources**: Localization files

### Infrastructure Layer

- **Data**: EF Core DbContext and configurations
- **Migrations**: Database migration files
- **Repositories**: Custom repository implementations (if needed)

### WebAPI Layer

- **Controllers**: API endpoints (REST controllers)
- **Extensions**: Service registration
- **Middlewares**: Custom middleware (optional)

---

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Entity | PascalCase, singular | `Customer.cs` |
| DTO | PascalCase + Dto suffix | `CustomerDto.cs` |
| Create DTO | PascalCase + CreateDto | `CustomerCreateDto.cs` |
| Update DTO | PascalCase + UpdateDto | `CustomerUpdateDto.cs` |
| Filter DTO | PascalCase + FilterDto | `CustomerFilterDto.cs` |
| Validator | DTO name + Validator | `CustomerCreateValidator.cs` |
| Service Interface | I + Entity + Service | `ICustomerService.cs` |
| Controller | Entity + Controller | `CustomerController.cs` |
| Configuration | Entity + Configuration | `CustomerConfiguration.cs` |

---

## Namespaces

```csharp
// Core
ProjectName.Core.Entities
ProjectName.Core.ValueObjects
ProjectName.Core.Validators
ProjectName.Core.Contract.Services
ProjectName.Core.Resources

// Infrastructure
ProjectName.Infrastructure.Data
ProjectName.Infrastructure.Data.Configurations

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
    "DefaultConnection": "Server=localhost;Database=ProjectDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
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

### launchSettings.json

```json
{
  "$schema": "https://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
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
using ProjectName.Core.Validators;
using ProjectName.Infrastructure.Data;

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
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects;

namespace ProjectName.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly IUnitOfWorkAsync _uow;
    private readonly IValidator<CustomerCreateDto> _createValidator;
    private readonly IValidator<CustomerUpdateDto> _updateValidator;

    public CustomerController(
        IUnitOfWorkAsync uow,
        IValidator<CustomerCreateDto> createValidator,
        IValidator<CustomerUpdateDto> updateValidator)
    {
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var repository = _uow.GetRepository<Customer>();
        var customers = await repository.ToListAsync();
        return Ok(customers.ToBusinessPaging());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return NotFound();
            
        return Ok(customer.ToBusiness());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.ToBusiness<CustomerDto>());

        var customer = new Customer
        {
            Name = dto.Name,
            Email = dto.Email
        };

        var repository = _uow.GetRepository<Customer>();
        await repository.AddAsync(customer);
        await _uow.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer.ToBusiness());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.ToBusiness<CustomerDto>());

        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return NotFound();

        customer.Name = dto.Name;
        customer.Email = dto.Email;
        customer.Active = dto.Active;

        await repository.ModifyAsync(customer);
        await _uow.SaveChangesAsync();

        return Ok(customer.ToBusiness());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var repository = _uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return NotFound();

        await repository.RemoveAsync(customer);
        await _uow.SaveChangesAsync();

        return NoContent();
    }
}
```

---

## Entity Template

```csharp
// Core/Entities/Customer.cs
using Mvp24Hours.Core.Entities;

namespace ProjectName.Core.Entities;

public class Customer : EntityBase<int>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime Created { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
```

---

## DTO Templates

```csharp
// Core/ValueObjects/CustomerDto.cs
namespace ProjectName.Core.ValueObjects;

public record CustomerDto(
    int Id,
    string Name,
    string Email,
    bool Active,
    DateTime Created
);

// Core/ValueObjects/CustomerCreateDto.cs
namespace ProjectName.Core.ValueObjects;

public record CustomerCreateDto(
    string Name,
    string Email
);

// Core/ValueObjects/CustomerUpdateDto.cs
namespace ProjectName.Core.ValueObjects;

public record CustomerUpdateDto(
    string Name,
    string Email,
    bool Active
);

// Core/ValueObjects/CustomerFilterDto.cs
namespace ProjectName.Core.ValueObjects;

public record CustomerFilterDto(
    string? Name,
    string? Email,
    bool? Active
);
```

---

## Validator Template

```csharp
// Core/Validators/CustomerCreateValidator.cs
using FluentValidation;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Core.Validators;

public class CustomerCreateValidator : AbstractValidator<CustomerCreateDto>
{
    public CustomerCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
    }
}
```

---

## Service Interface Template

```csharp
// Core/Contract/Services/ICustomerService.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Core.Contract.Services;

public interface ICustomerService
{
    Task<IBusinessResult<IList<CustomerDto>>> GetAllAsync();
    Task<IBusinessResult<CustomerDto>> GetByIdAsync(int id);
    Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto);
    Task<IBusinessResult<CustomerDto>> UpdateAsync(int id, CustomerUpdateDto dto);
    Task<IBusinessResult<bool>> DeleteAsync(int id);
}
```

---

## DataContext Template

```csharp
// Infrastructure/Data/DataContext.cs
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using ProjectName.Core.Entities;

namespace ProjectName.Infrastructure.Data;

public class DataContext : Mvp24HoursContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
```

---

## Entity Configuration Template

```csharp
// Infrastructure/Data/Configurations/CustomerConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Core.Entities;

namespace ProjectName.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Active)
            .HasDefaultValue(true);

        builder.Property(x => x.Created)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasMany(x => x.Contacts)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Email).IsUnique();
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
    │       └── CustomerUpdatedMessage.cs
    ├── ProjectName.Infrastructure/
    │   └── ... (same as above)
    └── ProjectName.WebAPI/
        ├── ... (same as above)
        ├── Consumers/
        │   └── CustomerCreatedConsumer.cs
        └── HostedServices/
            └── RabbitMQConsumerHostedService.cs
```

---

## With MongoDB Extension

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   └── ... (same as above, entities inherit from EntityBase)
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   └── Data/
    │       └── MongoDbContext.cs
    └── ProjectName.WebAPI/
        └── ... (same as above)
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Database Patterns](database-patterns.md)
- [Minimal API Structure](structure-minimal-api.md)
- [Complex N-Layers Structure](structure-complex-nlayers.md)

