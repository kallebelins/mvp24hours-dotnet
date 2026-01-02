# Estrutura de Projetos para Agentes de IA

> **Instrução para Agente de IA**: Siga estas convenções ao gerar estrutura de projeto. Mantenha consistência em todo o código gerado.

---

## Estrutura da Solução

### Minimal API

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    └── NomeProjeto/
        ├── NomeProjeto.csproj
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── Entities/
        │   ├── Cliente.cs
        │   └── Contato.cs
        ├── ValueObjects/
        │   ├── ClienteDto.cs
        │   └── ContatoDto.cs
        ├── Validators/
        │   └── ClienteValidator.cs
        ├── Data/
        │   ├── DataContext.cs
        │   └── Configurations/
        │       ├── ClienteConfiguration.cs
        │       └── ContatoConfiguration.cs
        ├── Endpoints/
        │   ├── ClienteEndpoints.cs
        │   └── ContatoEndpoints.cs
        └── Extensions/
            └── ServiceBuilderExtensions.cs
```

### Simple N-Layers

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   ├── NomeProjeto.Core.csproj
    │   ├── Entities/
    │   │   ├── Cliente.cs
    │   │   └── Contato.cs
    │   ├── ValueObjects/
    │   │   ├── ClienteDto.cs
    │   │   ├── ClienteFiltroDto.cs
    │   │   └── ContatoDto.cs
    │   └── Validators/
    │       ├── ClienteValidator.cs
    │       └── ContatoValidator.cs
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   └── Data/
    │       ├── DataContext.cs
    │       └── Configurations/
    │           ├── ClienteConfiguration.cs
    │           └── ContatoConfiguration.cs
    └── NomeProjeto.WebAPI/
        ├── NomeProjeto.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── appsettings.Staging.json
        ├── Controllers/
        │   ├── ClienteController.cs
        │   └── ContatoController.cs
        ├── Extensions/
        │   └── ServiceBuilderExtensions.cs
        ├── Properties/
        │   └── launchSettings.json
        └── NLog.config
```

### Complex N-Layers

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   ├── NomeProjeto.Core.csproj
    │   ├── Entities/
    │   │   ├── Cliente.cs
    │   │   └── Contato.cs
    │   ├── ValueObjects/
    │   │   ├── Clientes/
    │   │   │   ├── ClienteDto.cs
    │   │   │   ├── ClienteCreateDto.cs
    │   │   │   ├── ClienteUpdateDto.cs
    │   │   │   └── ClienteFiltroDto.cs
    │   │   └── Contatos/
    │   │       └── ContatoDto.cs
    │   ├── Validators/
    │   │   └── Clientes/
    │   │       ├── ClienteCreateValidator.cs
    │   │       └── ClienteUpdateValidator.cs
    │   ├── Contract/
    │   │   ├── Services/
    │   │   │   └── IClienteService.cs
    │   │   └── Specifications/
    │   │       └── IClienteSpecification.cs
    │   ├── Specifications/
    │   │   └── ClientePorFiltroSpec.cs
    │   └── Resources/
    │       └── Messages.resx
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── DataContext.cs
    │   │   └── Configurations/
    │   │       └── ClienteConfiguration.cs
    │   └── Migrations/
    │       └── ...
    ├── NomeProjeto.Application/
    │   ├── NomeProjeto.Application.csproj
    │   ├── Services/
    │   │   └── ClienteService.cs
    │   ├── Mappings/
    │   │   └── ClienteProfile.cs
    │   └── FacadeService.cs
    └── NomeProjeto.WebAPI/
        ├── NomeProjeto.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── appsettings.json
        ├── Controllers/
        │   └── ClienteController.cs
        ├── Extensions/
        │   └── ServiceBuilderExtensions.cs
        ├── Middlewares/
        │   ├── ExceptionMiddleware.cs
        │   └── CorrelationIdMiddleware.cs
        └── NLog.config
```

### Com RabbitMQ

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   ├── ... (mesmo que Complex)
    │   └── Messages/
    │       ├── ClienteCriadoMessage.cs
    │       └── ClienteAtualizadoMessage.cs
    ├── NomeProjeto.Infrastructure/
    │   └── ... (mesmo que Complex)
    ├── NomeProjeto.Application/
    │   ├── ... (mesmo que Complex)
    │   └── Consumers/
    │       └── ClienteCriadoConsumer.cs
    └── NomeProjeto.WebAPI/
        ├── ... (mesmo que Complex)
        └── HostedServices/
            └── RabbitMQConsumerHostedService.cs
```

---

## Convenções de Nomenclatura

### Arquivos e Pastas

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Entidade | PascalCase, singular | `Cliente.cs` |
| DTO | PascalCase + sufixo Dto | `ClienteDto.cs` |
| DTO de criação | PascalCase + CreateDto | `ClienteCreateDto.cs` |
| DTO de atualização | PascalCase + UpdateDto | `ClienteUpdateDto.cs` |
| DTO de filtro | PascalCase + FiltroDto | `ClienteFiltroDto.cs` |
| Validator | PascalCase + Validator | `ClienteValidator.cs` |
| Service | PascalCase + Service | `ClienteService.cs` |
| Repository | PascalCase + Repository | `ClienteRepository.cs` |
| Controller | PascalCase + Controller | `ClienteController.cs` |
| Configuration | PascalCase + Configuration | `ClienteConfiguration.cs` |
| Specification | Descritivo + Spec | `ClientePorFiltroSpec.cs` |
| Consumer | Mensagem + Consumer | `ClienteCriadoConsumer.cs` |
| Operation | Verbo + Entidade + Operation | `ValidarClienteOperation.cs` |
| Profile (AutoMapper) | PascalCase + Profile | `ClienteProfile.cs` |

### Namespaces

```csharp
// Core
NomeProjeto.Core.Entities
NomeProjeto.Core.ValueObjects
NomeProjeto.Core.ValueObjects.Clientes
NomeProjeto.Core.Validators
NomeProjeto.Core.Validators.Clientes
NomeProjeto.Core.Contract.Services
NomeProjeto.Core.Contract.Specifications
NomeProjeto.Core.Specifications
NomeProjeto.Core.Messages

// Infrastructure
NomeProjeto.Infrastructure.Data
NomeProjeto.Infrastructure.Data.Configurations
NomeProjeto.Infrastructure.Repositories

// Application
NomeProjeto.Application.Services
NomeProjeto.Application.Mappings
NomeProjeto.Application.Consumers
NomeProjeto.Application.Pipelines
NomeProjeto.Application.Pipelines.Operations

// WebAPI
NomeProjeto.WebAPI.Controllers
NomeProjeto.WebAPI.Extensions
NomeProjeto.WebAPI.Middlewares
NomeProjeto.WebAPI.HostedServices
NomeProjeto.WebAPI.HealthChecks
```

---

## Arquivos de Projeto (.csproj)

### Projeto Core

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

### Projeto Infrastructure

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\NomeProjeto.Core\NomeProjeto.Core.csproj" />
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

### Projeto Application

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\NomeProjeto.Core\NomeProjeto.Core.csproj" />
    <ProjectReference Include="..\NomeProjeto.Infrastructure\NomeProjeto.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Application" Version="8.*" />
    <PackageReference Include="AutoMapper" Version="13.*" />
    <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.*" />
  </ItemGroup>
</Project>
```

### Projeto WebAPI

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\NomeProjeto.Application\NomeProjeto.Application.csproj" />
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

## Arquivos de Configuração

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

## Templates Program.cs

### Minimal API

```csharp
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using NLog;
using NLog.Web;
using NomeProjeto.Data;
using NomeProjeto.Extensions;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Iniciando aplicação");

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
    app.MapClienteEndpoints();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Aplicação parou devido a exceção");
    throw;
}
finally
{
    LogManager.Shutdown();
}
```

### N-Layers (com Startup.cs)

```csharp
// Program.cs
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Iniciando aplicação");

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
    logger.Error(ex, "Aplicação parou devido a exceção");
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
using NomeProjeto.WebAPI.Extensions;
using NomeProjeto.WebAPI.Middlewares;

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
            c.SwaggerDoc("v1", new() { Title = "NomeProjeto API", Version = "v1" });
        });

        services.AddFluentValidationAutoValidation();
        services.AddMyServices(Configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NomeProjeto API v1"));
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

## Documentação Relacionada

- [Templates de Arquitetura](ai-context/architecture-templates.md)
- [Matriz de Decisão](ai-context/decision-matrix.md)
- [Padrões de Banco de Dados](ai-context/database-patterns.md)

