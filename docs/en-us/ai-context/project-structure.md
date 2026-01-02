# Project Structure for AI Agents

> **AI Agent Instruction**: Follow these conventions when generating project structure. Maintain consistency across all generated code.

---

## Solution Structure

### Minimal API

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
        ├── Entities/
        │   ├── Customer.cs
        │   └── Contact.cs
        ├── ValueObjects/
        │   ├── CustomerDto.cs
        │   └── ContactDto.cs
        ├── Validators/
        │   └── CustomerValidator.cs
        ├── Data/
        │   ├── DataContext.cs
        │   └── Configurations/
        │       ├── CustomerConfiguration.cs
        │       └── ContactConfiguration.cs
        ├── Endpoints/
        │   ├── CustomerEndpoints.cs
        │   └── ContactEndpoints.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

### Simple N-Layers

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
    │   │   ├── CustomerFilterDto.cs
    │   │   └── ContactDto.cs
    │   └── Validators/
    │       ├── CustomerValidator.cs
    │       └── ContactValidator.cs
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   └── Data/
    │       ├── DataContext.cs
    │       └── Configurations/
    │           ├── CustomerConfiguration.cs
    │           └── ContactConfiguration.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── appsettings.Staging.json
        ├── Controllers/
        │   ├── CustomerController.cs
        │   └── ContactController.cs
        ├── Extensions/
        │   └── ServiceBuilderExtensions.cs
        ├── Properties/
        │   └── launchSettings.json
        └── NLog.config
```

### Complex N-Layers

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
    │   │   └── Specifications/
    │   │       └── ICustomerSpecification.cs
    │   ├── Specifications/
    │   │   ├── CustomerByFilterSpec.cs
    │   │   └── CustomerActiveSpec.cs
    │   └── Resources/
    │       └── Messages.resx
    ├── ProjectName.Infrastructure/
    │   ├── ProjectName.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── DataContext.cs
    │   │   └── Configurations/
    │   │       ├── CustomerConfiguration.cs
    │   │       └── ContactConfiguration.cs
    │   ├── Repositories/
    │   │   └── CustomerDapperRepository.cs (optional)
    │   └── Migrations/
    │       └── 20240101000000_InitialCreate.cs
    ├── ProjectName.Application/
    │   ├── ProjectName.Application.csproj
    │   ├── Services/
    │   │   ├── CustomerService.cs
    │   │   └── ContactService.cs
    │   ├── Mappings/
    │   │   ├── CustomerProfile.cs
    │   │   └── ContactProfile.cs
    │   └── FacadeService.cs
    └── ProjectName.WebAPI/
        ├── ProjectName.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── appsettings.Staging.json
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
        ├── HealthChecks/
        │   └── CustomHealthCheck.cs
        └── NLog.config
```

### With RabbitMQ

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   ├── ... (same as Complex)
    │   └── Messages/
    │       ├── CustomerCreatedMessage.cs
    │       ├── CustomerUpdatedMessage.cs
    │       └── CustomerDeletedMessage.cs
    ├── ProjectName.Infrastructure/
    │   └── ... (same as Complex)
    ├── ProjectName.Application/
    │   ├── ... (same as Complex)
    │   └── Consumers/
    │       ├── CustomerCreatedConsumer.cs
    │       └── CustomerUpdatedConsumer.cs
    └── ProjectName.WebAPI/
        ├── ... (same as Complex)
        └── HostedServices/
            └── RabbitMQConsumerHostedService.cs
```

### With Pipeline Pattern

```
ProjectName/
├── ProjectName.sln
└── src/
    ├── ProjectName.Core/
    │   ├── ... (same as Complex)
    │   └── Contract/
    │       └── Pipelines/
    │           └── ICustomerPipeline.cs
    ├── ProjectName.Infrastructure/
    │   └── ... (same as Complex)
    ├── ProjectName.Application/
    │   ├── ... (same as Complex)
    │   └── Pipelines/
    │       ├── CustomerPipeline.cs
    │       └── Operations/
    │           ├── ValidateCustomerOperation.cs
    │           ├── CreateCustomerOperation.cs
    │           └── NotifyCustomerOperation.cs
    └── ProjectName.WebAPI/
        └── ... (same as Complex)
```

---

## Naming Conventions

### Files and Folders

| Type | Convention | Example |
|------|------------|---------|
| Entity | PascalCase, singular | `Customer.cs` |
| DTO | PascalCase + Dto suffix | `CustomerDto.cs` |
| Create DTO | PascalCase + CreateDto | `CustomerCreateDto.cs` |
| Update DTO | PascalCase + UpdateDto | `CustomerUpdateDto.cs` |
| Filter DTO | PascalCase + FilterDto | `CustomerFilterDto.cs` |
| Validator | PascalCase + Validator | `CustomerValidator.cs` |
| Service | PascalCase + Service | `CustomerService.cs` |
| Repository | PascalCase + Repository | `CustomerRepository.cs` |
| Controller | PascalCase + Controller | `CustomerController.cs` |
| Configuration | PascalCase + Configuration | `CustomerConfiguration.cs` |
| Specification | Descriptive + Spec | `CustomerByFilterSpec.cs` |
| Consumer | Message + Consumer | `CustomerCreatedConsumer.cs` |
| Operation | Verb + Entity + Operation | `ValidateCustomerOperation.cs` |
| Profile (AutoMapper) | PascalCase + Profile | `CustomerProfile.cs` |

### Namespaces

```csharp
// Core
ProjectName.Core.Entities
ProjectName.Core.ValueObjects
ProjectName.Core.ValueObjects.Customers
ProjectName.Core.Validators
ProjectName.Core.Validators.Customers
ProjectName.Core.Contract.Services
ProjectName.Core.Contract.Specifications
ProjectName.Core.Specifications
ProjectName.Core.Messages

// Infrastructure
ProjectName.Infrastructure.Data
ProjectName.Infrastructure.Data.Configurations
ProjectName.Infrastructure.Repositories

// Application
ProjectName.Application.Services
ProjectName.Application.Mappings
ProjectName.Application.Consumers
ProjectName.Application.Pipelines
ProjectName.Application.Pipelines.Operations

// WebAPI
ProjectName.WebAPI.Controllers
ProjectName.WebAPI.Extensions
ProjectName.WebAPI.Middlewares
ProjectName.WebAPI.HostedServices
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
    <PackageReference Include="Mvp24Hours.Core" Version="8.*" />
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
    <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="8.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
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
    <PackageReference Include="Mvp24Hours.Application" Version="8.*" />
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
    <PackageReference Include="Mvp24Hours.WebAPI" Version="8.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
    <PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
    <PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
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

## Program.cs Templates

### Minimal API

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
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Health Checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Endpoints
    app.MapCustomerEndpoints();

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

### N-Layers (with Startup.cs)

```csharp
// Program.cs
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

// Startup.cs
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

## Related Documentation

- [Architecture Templates](ai-context/architecture-templates.md)
- [Decision Matrix](ai-context/decision-matrix.md)
- [Database Patterns](ai-context/database-patterns.md)

