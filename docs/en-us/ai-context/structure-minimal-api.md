# Minimal API Project Structure

> **AI Agent Instruction**: Use this structure when creating lightweight, single-project APIs. Ideal for small services, microservices, or simple CRUD applications.

---

## When to Use

- Simple CRUD APIs with 1-5 entities
- Microservices with single responsibility
- Prototypes and MVPs
- APIs without complex business logic
- Lightweight backend services

---

## Directory Structure

```
ProjectName/
├── ProjectName.sln
└── src/
    └── ProjectName/
        ├── ProjectName.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── NLog.config
        ├── Entities/
        │   ├── Customer.cs
        │   └── Contact.cs
        ├── ValueObjects/
        │   ├── CustomerDto.cs
        │   ├── CustomerCreateDto.cs
        │   ├── CustomerUpdateDto.cs
        │   └── ContactDto.cs
        ├── Validators/
        │   ├── CustomerCreateValidator.cs
        │   └── CustomerUpdateValidator.cs
        ├── Data/
        │   ├── DataContext.cs
        │   └── Configurations/
        │       ├── CustomerConfiguration.cs
        │       └── ContactConfiguration.cs
        ├── Endpoints/
        │   ├── CustomerEndpoints.cs
        │   └── ContactEndpoints.cs
        ├── Extensions/
        │   └── ServiceBuilderExtensions.cs
        └── Properties/
            └── launchSettings.json
```

---

## Folder Descriptions

| Folder | Purpose |
|--------|---------|
| `Entities/` | Domain entities inheriting from `EntityBase` or `EntityBaseLog` |
| `ValueObjects/` | DTOs for data transfer (Create, Update, Filter, Response) |
| `Validators/` | FluentValidation validators for DTOs |
| `Data/` | EF Core DbContext and entity configurations |
| `Endpoints/` | Minimal API endpoint definitions (extension methods) |
| `Extensions/` | Service registration and configuration extensions |
| `Properties/` | Launch settings for development |

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
| Configuration | Entity + Configuration | `CustomerConfiguration.cs` |
| Endpoints | Entity + Endpoints | `CustomerEndpoints.cs` |

---

## Namespaces

```csharp
ProjectName.Entities
ProjectName.ValueObjects
ProjectName.Validators
ProjectName.Data
ProjectName.Data.Configurations
ProjectName.Endpoints
ProjectName.Extensions
```

---

## Project File (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Core" Version="8.*" />
    <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="8.*" />
    <PackageReference Include="Mvp24Hours.WebAPI" Version="8.*" />
    <PackageReference Include="FluentValidation" Version="11.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
    <PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="8.*" />
    <PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="8.*" />
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

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProjectDb_Dev;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
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
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using NLog;
using NLog.Web;
using ProjectName.Data;
using ProjectName.Extensions;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    // Logging
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Services
    builder.Services.AddMyServices(builder.Configuration);

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "ProjectName API", Version = "v1" });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProjectName API v1"));
    }

    app.UseHttpsRedirection();

    // Health Checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Endpoints
    app.MapCustomerEndpoints();
    app.MapContactEndpoints();

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

## Service Extensions Template

```csharp
// Extensions/ServiceBuilderExtensions.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore;
using ProjectName.Data;
using ProjectName.Validators;

namespace ProjectName.Extensions;

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

## Endpoint Template

```csharp
// Endpoints/CustomerEndpoints.cs
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore;
using ProjectName.Entities;
using ProjectName.ValueObjects;

namespace ProjectName.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .WithOpenApi();

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);
    }

    private static async Task<IResult> GetAllAsync(
        [FromServices] IUnitOfWorkAsync uow)
    {
        var repository = uow.GetRepository<Customer>();
        var customers = await repository.ToListAsync();
        return Results.Ok(customers.ToBusinessPaging());
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var repository = uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return Results.NotFound();
            
        return Results.Ok(customer.ToBusiness());
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CustomerCreateDto dto,
        [FromServices] IValidator<CustomerCreateDto> validator,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Results.BadRequest(validation.Errors.ToBusiness<CustomerDto>());

        var customer = new Customer
        {
            Name = dto.Name,
            Email = dto.Email
        };

        var repository = uow.GetRepository<Customer>();
        await repository.AddAsync(customer);
        await uow.SaveChangesAsync();

        return Results.Created($"/api/customers/{customer.Id}", customer.ToBusiness());
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        [FromBody] CustomerUpdateDto dto,
        [FromServices] IValidator<CustomerUpdateDto> validator,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Results.BadRequest(validation.Errors.ToBusiness<CustomerDto>());

        var repository = uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return Results.NotFound();

        customer.Name = dto.Name;
        customer.Email = dto.Email;

        await repository.ModifyAsync(customer);
        await uow.SaveChangesAsync();

        return Results.Ok(customer.ToBusiness());
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var repository = uow.GetRepository<Customer>();
        var customer = await repository.GetByIdAsync(id);
        
        if (customer == null)
            return Results.NotFound();

        await repository.RemoveAsync(customer);
        await uow.SaveChangesAsync();

        return Results.NoContent();
    }
}
```

---

## Entity Template

```csharp
// Entities/Customer.cs
using Mvp24Hours.Core.Entities;

namespace ProjectName.Entities;

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
// ValueObjects/CustomerDto.cs
namespace ProjectName.ValueObjects;

public record CustomerDto(
    int Id,
    string Name,
    string Email,
    bool Active,
    DateTime Created
);

// ValueObjects/CustomerCreateDto.cs
namespace ProjectName.ValueObjects;

public record CustomerCreateDto(
    string Name,
    string Email
);

// ValueObjects/CustomerUpdateDto.cs
namespace ProjectName.ValueObjects;

public record CustomerUpdateDto(
    string Name,
    string Email,
    bool Active
);
```

---

## Validator Template

```csharp
// Validators/CustomerCreateValidator.cs
using FluentValidation;
using ProjectName.ValueObjects;

namespace ProjectName.Validators;

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

// Validators/CustomerUpdateValidator.cs
using FluentValidation;
using ProjectName.ValueObjects;

namespace ProjectName.Validators;

public class CustomerUpdateValidator : AbstractValidator<CustomerUpdateDto>
{
    public CustomerUpdateValidator()
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

## DataContext Template

```csharp
// Data/DataContext.cs
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using ProjectName.Entities;

namespace ProjectName.Data;

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
// Data/Configurations/CustomerConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Entities;

namespace ProjectName.Data.Configurations;

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

        // Relationships
        builder.HasMany(x => x.Contacts)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
```

---

## With Pipeline Pattern

For operations requiring multiple steps, use the Pipeline Pattern:

```
ProjectName/
├── ...
├── Pipelines/
│   ├── CustomerPipeline.cs
│   └── Operations/
│       ├── ValidateCustomerOperation.cs
│       ├── CreateCustomerOperation.cs
│       └── SendNotificationOperation.cs
└── ...
```

```csharp
// Pipelines/CustomerPipeline.cs
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using ProjectName.Pipelines.Operations;

namespace ProjectName.Pipelines;

public class CustomerPipeline : Pipeline
{
    public CustomerPipeline(IServiceProvider serviceProvider)
    {
        Add<ValidateCustomerOperation>(serviceProvider);
        Add<CreateCustomerOperation>(serviceProvider);
        Add<SendNotificationOperation>(serviceProvider);
    }
}
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Database Patterns](database-patterns.md)
- [Simple N-Layers Structure](structure-simple-nlayers.md)
- [Complex N-Layers Structure](structure-complex-nlayers.md)

