# Estrutura de Projeto Minimal API

> **Instrução para Agente de IA**: Use esta estrutura ao criar APIs leves, de projeto único. Ideal para pequenos serviços, microservices ou aplicações CRUD simples.

---

## Quando Usar

- APIs CRUD simples com 1-5 entidades
- Microservices com responsabilidade única
- Protótipos e MVPs
- APIs sem lógica de negócio complexa
- Serviços backend leves

---

## Estrutura de Diretórios

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
        ├── NLog.config
        ├── Entities/
        │   ├── Cliente.cs
        │   └── Contato.cs
        ├── ValueObjects/
        │   ├── ClienteDto.cs
        │   ├── ClienteCreateDto.cs
        │   ├── ClienteUpdateDto.cs
        │   └── ContatoDto.cs
        ├── Validators/
        │   ├── ClienteCreateValidator.cs
        │   └── ClienteUpdateValidator.cs
        ├── Data/
        │   ├── DataContext.cs
        │   └── Configurations/
        │       ├── ClienteConfiguration.cs
        │       └── ContatoConfiguration.cs
        ├── Endpoints/
        │   ├── ClienteEndpoints.cs
        │   └── ContatoEndpoints.cs
        ├── Extensions/
        │   └── ServiceBuilderExtensions.cs
        └── Properties/
            └── launchSettings.json
```

---

## Descrição das Pastas

| Pasta | Propósito |
|-------|-----------|
| `Entities/` | Entidades de domínio herdando de `EntityBase` ou `EntityBaseLog` |
| `ValueObjects/` | DTOs para transferência de dados (Create, Update, Filter, Response) |
| `Validators/` | Validadores FluentValidation para DTOs |
| `Data/` | DbContext do EF Core e configurações de entidades |
| `Endpoints/` | Definições de endpoints Minimal API (métodos de extensão) |
| `Extensions/` | Extensões de registro de serviços e configuração |
| `Properties/` | Configurações de inicialização para desenvolvimento |

---

## Convenções de Nomenclatura

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Entidade | PascalCase, singular | `Cliente.cs` |
| DTO | PascalCase + sufixo Dto | `ClienteDto.cs` |
| DTO de criação | PascalCase + CreateDto | `ClienteCreateDto.cs` |
| DTO de atualização | PascalCase + UpdateDto | `ClienteUpdateDto.cs` |
| DTO de filtro | PascalCase + FiltroDto | `ClienteFiltroDto.cs` |
| Validator | Nome do DTO + Validator | `ClienteCreateValidator.cs` |
| Configuration | Entidade + Configuration | `ClienteConfiguration.cs` |
| Endpoints | Entidade + Endpoints | `ClienteEndpoints.cs` |

---

## Namespaces

```csharp
NomeProjeto.Entities
NomeProjeto.ValueObjects
NomeProjeto.Validators
NomeProjeto.Data
NomeProjeto.Data.Configurations
NomeProjeto.Endpoints
NomeProjeto.Extensions
```

---

## Arquivo de Projeto (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Mvp24Hours.Core" Version="9.*" />
    <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="9.*" />
    <PackageReference Include="Mvp24Hours.WebAPI" Version="9.*" />
    <PackageReference Include="FluentValidation" Version="11.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
    <PackageReference Include="NLog.Web.AspNetCore" Version="5.*" />
    <PackageReference Include="AspNetCore.HealthChecks.UI.Client" Version="9.*" />
    <PackageReference Include="AspNetCore.HealthChecks.SqlServer" Version="9.*" />
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

## Template Program.cs

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
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "NomeProjeto API", Version = "v1" });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "NomeProjeto API v1"));
    }

    app.UseHttpsRedirection();

    // Health Checks
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    // Endpoints
    app.MapClienteEndpoints();
    app.MapContatoEndpoints();

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

---

## Template de Extensões de Serviço

```csharp
// Extensions/ServiceBuilderExtensions.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore;
using NomeProjeto.Data;
using NomeProjeto.Validators;

namespace NomeProjeto.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Banco de dados
        services.AddDbContext<DataContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Serviços Mvp24Hours
        services.AddMvp24HoursDbContext<DataContext>();
        services.AddMvp24HoursRepository();

        // Validators
        services.AddValidatorsFromAssemblyContaining<ClienteCreateValidator>();

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

## Template de Endpoint

```csharp
// Endpoints/ClienteEndpoints.cs
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore;
using NomeProjeto.Entities;
using NomeProjeto.ValueObjects;

namespace NomeProjeto.Endpoints;

public static class ClienteEndpoints
{
    public static void MapClienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clientes")
            .WithTags("Clientes")
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
        var repository = uow.GetRepository<Cliente>();
        var clientes = await repository.ToListAsync();
        return Results.Ok(clientes.ToBusinessPaging());
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var repository = uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return Results.NotFound();
            
        return Results.Ok(cliente.ToBusiness());
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] ClienteCreateDto dto,
        [FromServices] IValidator<ClienteCreateDto> validator,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Results.BadRequest(validation.Errors.ToBusiness<ClienteDto>());

        var cliente = new Cliente
        {
            Nome = dto.Nome,
            Email = dto.Email
        };

        var repository = uow.GetRepository<Cliente>();
        await repository.AddAsync(cliente);
        await uow.SaveChangesAsync();

        return Results.Created($"/api/clientes/{cliente.Id}", cliente.ToBusiness());
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        [FromBody] ClienteUpdateDto dto,
        [FromServices] IValidator<ClienteUpdateDto> validator,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return Results.BadRequest(validation.Errors.ToBusiness<ClienteDto>());

        var repository = uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return Results.NotFound();

        cliente.Nome = dto.Nome;
        cliente.Email = dto.Email;

        await repository.ModifyAsync(cliente);
        await uow.SaveChangesAsync();

        return Results.Ok(cliente.ToBusiness());
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        [FromServices] IUnitOfWorkAsync uow)
    {
        var repository = uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return Results.NotFound();

        await repository.RemoveAsync(cliente);
        await uow.SaveChangesAsync();

        return Results.NoContent();
    }
}
```

---

## Template de Entidade

```csharp
// Entities/Cliente.cs
using Mvp24Hours.Core.Entities;

namespace NomeProjeto.Entities;

public class Cliente : EntityBase<int>
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DateTime Criado { get; set; } = DateTime.UtcNow;

    // Propriedades de navegação
    public virtual ICollection<Contato> Contatos { get; set; } = new List<Contato>();
}
```

---

## Templates de DTO

```csharp
// ValueObjects/ClienteDto.cs
namespace NomeProjeto.ValueObjects;

public record ClienteDto(
    int Id,
    string Nome,
    string Email,
    bool Ativo,
    DateTime Criado
);

// ValueObjects/ClienteCreateDto.cs
namespace NomeProjeto.ValueObjects;

public record ClienteCreateDto(
    string Nome,
    string Email
);

// ValueObjects/ClienteUpdateDto.cs
namespace NomeProjeto.ValueObjects;

public record ClienteUpdateDto(
    string Nome,
    string Email,
    bool Ativo
);
```

---

## Template de Validator

```csharp
// Validators/ClienteCreateValidator.cs
using FluentValidation;
using NomeProjeto.ValueObjects;

namespace NomeProjeto.Validators;

public class ClienteCreateValidator : AbstractValidator<ClienteCreateDto>
{
    public ClienteCreateValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Formato de email inválido")
            .MaximumLength(255).WithMessage("Email não pode exceder 255 caracteres");
    }
}

// Validators/ClienteUpdateValidator.cs
using FluentValidation;
using NomeProjeto.ValueObjects;

namespace NomeProjeto.Validators;

public class ClienteUpdateValidator : AbstractValidator<ClienteUpdateDto>
{
    public ClienteUpdateValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Formato de email inválido")
            .MaximumLength(255).WithMessage("Email não pode exceder 255 caracteres");
    }
}
```

---

## Template DataContext

```csharp
// Data/DataContext.cs
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using NomeProjeto.Entities;

namespace NomeProjeto.Data;

public class DataContext : Mvp24HoursContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Contato> Contatos => Set<Contato>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
```

---

## Template de Configuração de Entidade

```csharp
// Data/Configurations/ClienteConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NomeProjeto.Entities;

namespace NomeProjeto.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Ativo)
            .HasDefaultValue(true);

        builder.Property(x => x.Criado)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relacionamentos
        builder.HasMany(x => x.Contatos)
            .WithOne(x => x.Cliente)
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índices
        builder.HasIndex(x => x.Email).IsUnique();
    }
}
```

---

## Com Padrão Pipeline

Para operações que requerem múltiplos passos, use o Padrão Pipeline:

```
NomeProjeto/
├── ...
├── Pipelines/
│   ├── ClientePipeline.cs
│   └── Operations/
│       ├── ValidarClienteOperation.cs
│       ├── CriarClienteOperation.cs
│       └── EnviarNotificacaoOperation.cs
└── ...
```

```csharp
// Pipelines/ClientePipeline.cs
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using NomeProjeto.Pipelines.Operations;

namespace NomeProjeto.Pipelines;

public class ClientePipeline : Pipeline
{
    public ClientePipeline(IServiceProvider serviceProvider)
    {
        Add<ValidarClienteOperation>(serviceProvider);
        Add<CriarClienteOperation>(serviceProvider);
        Add<EnviarNotificacaoOperation>(serviceProvider);
    }
}
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Padrões de Banco de Dados](database-patterns.md)
- [Estrutura Simple N-Layers](structure-simple-nlayers.md)
- [Estrutura Complex N-Layers](structure-complex-nlayers.md)

