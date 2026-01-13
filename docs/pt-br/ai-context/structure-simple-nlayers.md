# Estrutura de Projeto Simple N-Layers

> **Instrução para Agente de IA**: Use esta estrutura para aplicações de médio porte que requerem separação de responsabilidades. Três camadas: Core, Infrastructure e WebAPI.

---

## Quando Usar

- Aplicações de média complexidade (5-15 entidades)
- Projetos que requerem clara separação de responsabilidades
- Aplicações com lógica de negócio em serviços
- Operações CRUD com algumas regras de negócio
- Equipes com múltiplos desenvolvedores
- Projetos que podem crescer ao longo do tempo

---

## Estrutura de Diretórios

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
    │   │   ├── ClienteCreateDto.cs
    │   │   ├── ClienteUpdateDto.cs
    │   │   ├── ClienteFiltroDto.cs
    │   │   └── ContatoDto.cs
    │   ├── Validators/
    │   │   ├── ClienteCreateValidator.cs
    │   │   ├── ClienteUpdateValidator.cs
    │   │   └── ContatoValidator.cs
    │   ├── Contract/
    │   │   └── Services/
    │   │       ├── IClienteService.cs
    │   │       └── IContatoService.cs
    │   └── Resources/
    │       └── Messages.resx
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── DataContext.cs
    │   │   └── Configurations/
    │   │       ├── ClienteConfiguration.cs
    │   │       └── ContatoConfiguration.cs
    │   └── Migrations/
    │       └── (migrações do EF Core)
    └── NomeProjeto.WebAPI/
        ├── NomeProjeto.WebAPI.csproj
        ├── Program.cs
        ├── Startup.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        ├── appsettings.Production.json
        ├── appsettings.Staging.json
        ├── NLog.config
        ├── Controllers/
        │   ├── ClienteController.cs
        │   └── ContatoController.cs
        ├── Extensions/
        │   └── ServiceBuilderExtensions.cs
        └── Properties/
            └── launchSettings.json
```

---

## Responsabilidades das Camadas

### Camada Core

- **Entities**: Entidades de domínio herdando de `EntityBase` ou `EntityBaseLog`
- **ValueObjects**: DTOs para transferência de dados
- **Validators**: Validadores FluentValidation
- **Contract**: Interfaces de serviços (abstração)
- **Resources**: Arquivos de localização

### Camada Infrastructure

- **Data**: DbContext do EF Core e configurações
- **Migrations**: Arquivos de migração de banco
- **Repositories**: Implementações de repositórios customizados (se necessário)

### Camada WebAPI

- **Controllers**: Endpoints da API (controladores REST)
- **Extensions**: Registro de serviços
- **Middlewares**: Middleware customizado (opcional)

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
| Interface de serviço | I + Entidade + Service | `IClienteService.cs` |
| Controller | Entidade + Controller | `ClienteController.cs` |
| Configuration | Entidade + Configuration | `ClienteConfiguration.cs` |

---

## Namespaces

```csharp
// Core
NomeProjeto.Core.Entities
NomeProjeto.Core.ValueObjects
NomeProjeto.Core.Validators
NomeProjeto.Core.Contract.Services
NomeProjeto.Core.Resources

// Infrastructure
NomeProjeto.Infrastructure.Data
NomeProjeto.Infrastructure.Data.Configurations

// WebAPI
NomeProjeto.WebAPI.Controllers
NomeProjeto.WebAPI.Extensions
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
    <PackageReference Include="Mvp24Hours.Core" Version="9.*" />
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
    <PackageReference Include="Mvp24Hours.Infrastructure.Data.EFCore" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
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
    <ProjectReference Include="..\NomeProjeto.Infrastructure\NomeProjeto.Infrastructure.csproj" />
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
```

---

## Template Startup.cs

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NomeProjeto.WebAPI.Extensions;

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

## Template de Extensões de Serviço

```csharp
// Extensions/ServiceBuilderExtensions.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using NomeProjeto.Core.Validators;
using NomeProjeto.Infrastructure.Data;

namespace NomeProjeto.WebAPI.Extensions;

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

## Template de Controller

```csharp
// Controllers/ClienteController.cs
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    private readonly IUnitOfWorkAsync _uow;
    private readonly IValidator<ClienteCreateDto> _createValidator;
    private readonly IValidator<ClienteUpdateDto> _updateValidator;

    public ClienteController(
        IUnitOfWorkAsync uow,
        IValidator<ClienteCreateDto> createValidator,
        IValidator<ClienteUpdateDto> updateValidator)
    {
        _uow = uow;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var repository = _uow.GetRepository<Cliente>();
        var clientes = await repository.ToListAsync();
        return Ok(clientes.ToBusinessPaging());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var repository = _uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return NotFound();
            
        return Ok(cliente.ToBusiness());
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.ToBusiness<ClienteDto>());

        var cliente = new Cliente
        {
            Nome = dto.Nome,
            Email = dto.Email
        };

        var repository = _uow.GetRepository<Cliente>();
        await repository.AddAsync(cliente);
        await _uow.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente.ToBusiness());
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ClienteUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.ToBusiness<ClienteDto>());

        var repository = _uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return NotFound();

        cliente.Nome = dto.Nome;
        cliente.Email = dto.Email;
        cliente.Ativo = dto.Ativo;

        await repository.ModifyAsync(cliente);
        await _uow.SaveChangesAsync();

        return Ok(cliente.ToBusiness());
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var repository = _uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return NotFound();

        await repository.RemoveAsync(cliente);
        await _uow.SaveChangesAsync();

        return NoContent();
    }
}
```

---

## Template de Entidade

```csharp
// Core/Entities/Cliente.cs
using Mvp24Hours.Core.Entities;

namespace NomeProjeto.Core.Entities;

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
// Core/ValueObjects/ClienteDto.cs
namespace NomeProjeto.Core.ValueObjects;

public record ClienteDto(
    int Id,
    string Nome,
    string Email,
    bool Ativo,
    DateTime Criado
);

// Core/ValueObjects/ClienteCreateDto.cs
namespace NomeProjeto.Core.ValueObjects;

public record ClienteCreateDto(
    string Nome,
    string Email
);

// Core/ValueObjects/ClienteUpdateDto.cs
namespace NomeProjeto.Core.ValueObjects;

public record ClienteUpdateDto(
    string Nome,
    string Email,
    bool Ativo
);

// Core/ValueObjects/ClienteFiltroDto.cs
namespace NomeProjeto.Core.ValueObjects;

public record ClienteFiltroDto(
    string? Nome,
    string? Email,
    bool? Ativo
);
```

---

## Template de Validator

```csharp
// Core/Validators/ClienteCreateValidator.cs
using FluentValidation;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.Core.Validators;

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
```

---

## Template de Interface de Serviço

```csharp
// Core/Contract/Services/IClienteService.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.Core.Contract.Services;

public interface IClienteService
{
    Task<IBusinessResult<IList<ClienteDto>>> GetAllAsync();
    Task<IBusinessResult<ClienteDto>> GetByIdAsync(int id);
    Task<IBusinessResult<ClienteDto>> CreateAsync(ClienteCreateDto dto);
    Task<IBusinessResult<ClienteDto>> UpdateAsync(int id, ClienteUpdateDto dto);
    Task<IBusinessResult<bool>> DeleteAsync(int id);
}
```

---

## Template DataContext

```csharp
// Infrastructure/Data/DataContext.cs
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using NomeProjeto.Core.Entities;

namespace NomeProjeto.Infrastructure.Data;

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
// Infrastructure/Data/Configurations/ClienteConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NomeProjeto.Core.Entities;

namespace NomeProjeto.Infrastructure.Data.Configurations;

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

        builder.HasMany(x => x.Contatos)
            .WithOne(x => x.Cliente)
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
```

---

## Com Extensão RabbitMQ

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   ├── ... (mesmo acima)
    │   └── Messages/
    │       ├── ClienteCriadoMessage.cs
    │       └── ClienteAtualizadoMessage.cs
    ├── NomeProjeto.Infrastructure/
    │   └── ... (mesmo acima)
    └── NomeProjeto.WebAPI/
        ├── ... (mesmo acima)
        ├── Consumers/
        │   └── ClienteCriadoConsumer.cs
        └── HostedServices/
            └── RabbitMQConsumerHostedService.cs
```

---

## Com Extensão MongoDB

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   └── ... (mesmo acima, entidades herdam de EntityBase)
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   └── Data/
    │       └── MongoDbContext.cs
    └── NomeProjeto.WebAPI/
        └── ... (mesmo acima)
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Padrões de Banco de Dados](database-patterns.md)
- [Estrutura Minimal API](structure-minimal-api.md)
- [Estrutura Complex N-Layers](structure-complex-nlayers.md)

