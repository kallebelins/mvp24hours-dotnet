# Estrutura de Projeto Complex N-Layers

> **Instrução para Agente de IA**: Use esta estrutura para aplicações corporativas com lógica de negócio complexa. Quatro camadas: Core, Infrastructure, Application e WebAPI.

---

## Quando Usar

- Aplicações corporativas grandes (15+ entidades)
- Lógica de negócio complexa requerendo camada de aplicação dedicada
- Projetos com múltiplas fontes de dados
- Aplicações usando padrões como CQRS, DDD ou Ports & Adapters
- Projetos de longo prazo com múltiplas equipes
- Aplicações que requerem mapeamento e transformação extensivos

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
    │   │   ├── Clientes/
    │   │   │   ├── ClienteDto.cs
    │   │   │   ├── ClienteCreateDto.cs
    │   │   │   ├── ClienteUpdateDto.cs
    │   │   │   └── ClienteFiltroDto.cs
    │   │   └── Contatos/
    │   │       ├── ContatoDto.cs
    │   │       ├── ContatoCreateDto.cs
    │   │       └── ContatoUpdateDto.cs
    │   ├── Validators/
    │   │   ├── Clientes/
    │   │   │   ├── ClienteCreateValidator.cs
    │   │   │   └── ClienteUpdateValidator.cs
    │   │   └── Contatos/
    │   │       ├── ContatoCreateValidator.cs
    │   │       └── ContatoUpdateValidator.cs
    │   ├── Contract/
    │   │   ├── Services/
    │   │   │   ├── IClienteService.cs
    │   │   │   └── IContatoService.cs
    │   │   ├── Specifications/
    │   │   │   └── IClienteSpecification.cs
    │   │   └── Pipelines/
    │   │       └── IClientePipeline.cs
    │   ├── Specifications/
    │   │   ├── ClientePorFiltroSpec.cs
    │   │   └── ClienteAtivoSpec.cs
    │   └── Resources/
    │       └── Messages.resx
    ├── NomeProjeto.Infrastructure/
    │   ├── NomeProjeto.Infrastructure.csproj
    │   ├── Data/
    │   │   ├── EFCore/
    │   │   │   ├── DataContext.cs
    │   │   │   └── Configurations/
    │   │   │       ├── ClienteConfiguration.cs
    │   │   │       └── ContatoConfiguration.cs
    │   │   └── Dapper/
    │   │       └── ClienteDapperRepository.cs
    │   └── Migrations/
    │       └── (migrações do EF Core)
    ├── NomeProjeto.Application/
    │   ├── NomeProjeto.Application.csproj
    │   ├── Services/
    │   │   ├── ClienteService.cs
    │   │   └── ContatoService.cs
    │   ├── Mappings/
    │   │   ├── ClienteProfile.cs
    │   │   └── ContatoProfile.cs
    │   ├── Pipelines/
    │   │   ├── ClientePipeline.cs
    │   │   └── Operations/
    │   │       ├── ValidarClienteOperation.cs
    │   │       ├── CriarClienteOperation.cs
    │   │       └── NotificarClienteOperation.cs
    │   └── FacadeService.cs
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
        ├── Middlewares/
        │   ├── ExceptionMiddleware.cs
        │   └── CorrelationIdMiddleware.cs
        ├── Properties/
        │   └── launchSettings.json
        └── HealthChecks/
            └── CustomHealthCheck.cs
```

---

## Responsabilidades das Camadas

### Camada Core

- **Entities**: Entidades de domínio (Aggregates, Value Objects)
- **ValueObjects**: DTOs organizados por feature/entidade
- **Validators**: Validadores FluentValidation por operação
- **Contract**: Interfaces para serviços, especificações, pipelines
- **Specifications**: Especificações de query para repositórios
- **Resources**: Arquivos de localização

### Camada Infrastructure

- **Data/EFCore**: Implementação Entity Framework Core
- **Data/Dapper**: Queries Dapper para consultas complexas/otimizadas
- **Migrations**: Migrações de banco de dados

### Camada Application

- **Services**: Implementação da lógica de negócio
- **Mappings**: Profiles do AutoMapper
- **Pipelines**: Implementações do padrão Pipeline
- **Operations**: Operações individuais de pipeline

### Camada WebAPI

- **Controllers**: Endpoints REST da API
- **Extensions**: Registro de serviços
- **Middlewares**: Componentes de middleware customizados
- **HealthChecks**: Health checks customizados

---

## Convenções de Nomenclatura

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Entidade | PascalCase, singular | `Cliente.cs` |
| DTO | PascalCase + sufixo Dto | `ClienteDto.cs` |
| DTO de criação | Entidade + CreateDto | `ClienteCreateDto.cs` |
| DTO de atualização | Entidade + UpdateDto | `ClienteUpdateDto.cs` |
| DTO de filtro | Entidade + FiltroDto | `ClienteFiltroDto.cs` |
| Validator | Nome do DTO + Validator | `ClienteCreateValidator.cs` |
| Interface de serviço | I + Entidade + Service | `IClienteService.cs` |
| Implementação de serviço | Entidade + Service | `ClienteService.cs` |
| Controller | Entidade + Controller | `ClienteController.cs` |
| Configuration | Entidade + Configuration | `ClienteConfiguration.cs` |
| Specification | Descritivo + Spec | `ClientePorFiltroSpec.cs` |
| Profile (AutoMapper) | Entidade + Profile | `ClienteProfile.cs` |
| Operation | Verbo + Entidade + Operation | `ValidarClienteOperation.cs` |
| Middleware | Feature + Middleware | `ExceptionMiddleware.cs` |

---

## Namespaces

```csharp
// Core
NomeProjeto.Core.Entities
NomeProjeto.Core.ValueObjects.Clientes
NomeProjeto.Core.ValueObjects.Contatos
NomeProjeto.Core.Validators.Clientes
NomeProjeto.Core.Validators.Contatos
NomeProjeto.Core.Contract.Services
NomeProjeto.Core.Contract.Specifications
NomeProjeto.Core.Contract.Pipelines
NomeProjeto.Core.Specifications
NomeProjeto.Core.Resources

// Infrastructure
NomeProjeto.Infrastructure.Data.EFCore
NomeProjeto.Infrastructure.Data.EFCore.Configurations
NomeProjeto.Infrastructure.Data.Dapper

// Application
NomeProjeto.Application.Services
NomeProjeto.Application.Mappings
NomeProjeto.Application.Pipelines
NomeProjeto.Application.Pipelines.Operations

// WebAPI
NomeProjeto.WebAPI.Controllers
NomeProjeto.WebAPI.Extensions
NomeProjeto.WebAPI.Middlewares
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
    <PackageReference Include="Dapper" Version="2.*" />
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

## Template de Extensões de Serviço

```csharp
// Extensions/ServiceBuilderExtensions.cs
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using NomeProjeto.Application.Mappings;
using NomeProjeto.Application.Services;
using NomeProjeto.Core.Contract.Services;
using NomeProjeto.Core.Validators.Clientes;
using NomeProjeto.Infrastructure.Data.EFCore;

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
        services.AddMvp24HoursPipeline();

        // AutoMapper
        services.AddAutoMapper(typeof(ClienteProfile).Assembly);

        // Serviços da aplicação
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IContatoService, ContatoService>();

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
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.Contract.Services;
using NomeProjeto.Core.ValueObjects.Clientes;

namespace NomeProjeto.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClienteController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IBusinessResult<IList<ClienteDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ClienteFiltroDto filtro)
    {
        var result = await _clienteService.GetAllAsync(filtro);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IBusinessResult<ClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _clienteService.GetByIdAsync(id);
        if (!result.HasData)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(IBusinessResult<ClienteDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ClienteCreateDto dto)
    {
        var result = await _clienteService.CreateAsync(dto);
        if (result.HasErrors)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(IBusinessResult<ClienteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ClienteUpdateDto dto)
    {
        var result = await _clienteService.UpdateAsync(id, dto);
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
        var result = await _clienteService.DeleteAsync(id);
        if (result.HasErrors)
            return NotFound();
        return NoContent();
    }
}
```

---

## Template de Interface de Serviço

```csharp
// Core/Contract/Services/IClienteService.cs
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.ValueObjects.Clientes;

namespace NomeProjeto.Core.Contract.Services;

public interface IClienteService
{
    Task<IBusinessResult<IList<ClienteDto>>> GetAllAsync(ClienteFiltroDto? filtro = null);
    Task<IBusinessResult<ClienteDto>> GetByIdAsync(int id);
    Task<IBusinessResult<ClienteDto>> CreateAsync(ClienteCreateDto dto);
    Task<IBusinessResult<ClienteDto>> UpdateAsync(int id, ClienteUpdateDto dto);
    Task<IBusinessResult<bool>> DeleteAsync(int id);
}
```

---

## Template de Implementação de Serviço

```csharp
// Application/Services/ClienteService.cs
using AutoMapper;
using FluentValidation;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Core.Contract.Services;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.Specifications;
using NomeProjeto.Core.ValueObjects.Clientes;

namespace NomeProjeto.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IUnitOfWorkAsync _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<ClienteCreateDto> _createValidator;
    private readonly IValidator<ClienteUpdateDto> _updateValidator;

    public ClienteService(
        IUnitOfWorkAsync uow,
        IMapper mapper,
        IValidator<ClienteCreateDto> createValidator,
        IValidator<ClienteUpdateDto> updateValidator)
    {
        _uow = uow;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IBusinessResult<IList<ClienteDto>>> GetAllAsync(ClienteFiltroDto? filtro = null)
    {
        var repository = _uow.GetRepository<Cliente>();
        
        IList<Cliente> clientes;
        if (filtro != null)
        {
            var spec = new ClientePorFiltroSpec(filtro);
            clientes = await repository.ToListAsync(spec);
        }
        else
        {
            clientes = await repository.ToListAsync();
        }

        var dtos = _mapper.Map<IList<ClienteDto>>(clientes);
        return dtos.ToBusiness();
    }

    public async Task<IBusinessResult<ClienteDto>> GetByIdAsync(int id)
    {
        var repository = _uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return default(ClienteDto).ToBusiness();

        var dto = _mapper.Map<ClienteDto>(cliente);
        return dto.ToBusiness();
    }

    public async Task<IBusinessResult<ClienteDto>> CreateAsync(ClienteCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return validation.Errors.ToBusiness<ClienteDto>();

        var cliente = _mapper.Map<Cliente>(dto);
        
        var repository = _uow.GetRepository<Cliente>();
        await repository.AddAsync(cliente);
        await _uow.SaveChangesAsync();

        var resultDto = _mapper.Map<ClienteDto>(cliente);
        return resultDto.ToBusiness();
    }

    public async Task<IBusinessResult<ClienteDto>> UpdateAsync(int id, ClienteUpdateDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return validation.Errors.ToBusiness<ClienteDto>();

        var repository = _uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return default(ClienteDto).ToBusiness();

        _mapper.Map(dto, cliente);
        
        await repository.ModifyAsync(cliente);
        await _uow.SaveChangesAsync();

        var resultDto = _mapper.Map<ClienteDto>(cliente);
        return resultDto.ToBusiness();
    }

    public async Task<IBusinessResult<bool>> DeleteAsync(int id)
    {
        var repository = _uow.GetRepository<Cliente>();
        var cliente = await repository.GetByIdAsync(id);
        
        if (cliente == null)
            return false.ToBusiness("Cliente não encontrado");

        await repository.RemoveAsync(cliente);
        await _uow.SaveChangesAsync();

        return true.ToBusiness();
    }
}
```

---

## Template de Profile do AutoMapper

```csharp
// Application/Mappings/ClienteProfile.cs
using AutoMapper;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.ValueObjects.Clientes;

namespace NomeProjeto.Application.Mappings;

public class ClienteProfile : Profile
{
    public ClienteProfile()
    {
        // Entidade -> DTO
        CreateMap<Cliente, ClienteDto>();

        // DTO -> Entidade
        CreateMap<ClienteCreateDto, Cliente>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Criado, opt => opt.Ignore())
            .ForMember(dest => dest.Ativo, opt => opt.MapFrom(src => true));

        CreateMap<ClienteUpdateDto, Cliente>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Criado, opt => opt.Ignore());
    }
}
```

---

## Template de Specification

```csharp
// Core/Specifications/ClientePorFiltroSpec.cs
using Mvp24Hours.Core.Contract.Domain;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.ValueObjects.Clientes;
using System.Linq.Expressions;

namespace NomeProjeto.Core.Specifications;

public class ClientePorFiltroSpec : ISpecificationQuery<Cliente>
{
    private readonly ClienteFiltroDto _filtro;

    public ClientePorFiltroSpec(ClienteFiltroDto filtro)
    {
        _filtro = filtro;
    }

    public Expression<Func<Cliente, bool>> IsSatisfiedByExpression
    {
        get
        {
            return x =>
                (string.IsNullOrEmpty(_filtro.Nome) || x.Nome.Contains(_filtro.Nome)) &&
                (string.IsNullOrEmpty(_filtro.Email) || x.Email.Contains(_filtro.Email)) &&
                (!_filtro.Ativo.HasValue || x.Ativo == _filtro.Ativo.Value);
        }
    }
}
```

---

## Template de Pipeline

```csharp
// Application/Pipelines/ClientePipeline.cs
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using NomeProjeto.Application.Pipelines.Operations;

namespace NomeProjeto.Application.Pipelines;

public class ClientePipeline : Pipeline
{
    public ClientePipeline(IServiceProvider serviceProvider)
    {
        Add<ValidarClienteOperation>(serviceProvider);
        Add<CriarClienteOperation>(serviceProvider);
        Add<NotificarClienteOperation>(serviceProvider);
    }
}
```

---

## Template de Operação de Pipeline

```csharp
// Application/Pipelines/Operations/ValidarClienteOperation.cs
using FluentValidation;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.DTOs;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using NomeProjeto.Core.ValueObjects.Clientes;

namespace NomeProjeto.Application.Pipelines.Operations;

public class ValidarClienteOperation : OperationBase<ClienteCreateDto>
{
    private readonly IValidator<ClienteCreateDto> _validator;

    public ValidarClienteOperation(IValidator<ClienteCreateDto> validator)
    {
        _validator = validator;
    }

    public override async Task<IPipelineMessage> ExecuteAsync(IPipelineMessage input)
    {
        var dto = input.GetContent<ClienteCreateDto>();
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

## Templates de Middleware

```csharp
// Middlewares/ExceptionMiddleware.cs
using System.Net;
using System.Text.Json;

namespace NomeProjeto.WebAPI.Middlewares;

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
            _logger.LogError(ex, "Ocorreu uma exceção não tratada");
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
            Message = "Erro Interno do Servidor",
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
namespace NomeProjeto.WebAPI.Middlewares;

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

## Com Extensão RabbitMQ

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   ├── ... (mesmo acima)
    │   └── Messages/
    │       ├── ClienteCriadoMessage.cs
    │       ├── ClienteAtualizadoMessage.cs
    │       └── ClienteRemovidoMessage.cs
    ├── NomeProjeto.Infrastructure/
    │   └── ... (mesmo acima)
    ├── NomeProjeto.Application/
    │   ├── ... (mesmo acima)
    │   └── Consumers/
    │       ├── ClienteCriadoConsumer.cs
    │       └── ClienteAtualizadoConsumer.cs
    └── NomeProjeto.WebAPI/
        ├── ... (mesmo acima)
        └── HostedServices/
            └── RabbitMQConsumerHostedService.cs
```

---

## Com Padrão Ports & Adapters

```
NomeProjeto/
├── NomeProjeto.sln
└── src/
    ├── NomeProjeto.Core/
    │   ├── ... (mesmo acima)
    │   └── Ports/
    │       ├── Inbound/
    │       │   └── IClienteUseCase.cs
    │       └── Outbound/
    │           ├── IClienteRepository.cs
    │           └── INotificationService.cs
    ├── NomeProjeto.Infrastructure/
    │   ├── ... (mesmo acima)
    │   └── Adapters/
    │       └── NotificationServiceAdapter.cs
    ├── NomeProjeto.Application/
    │   └── ... (mesmo acima)
    └── NomeProjeto.WebAPI/
        └── ... (mesmo acima)
```

---

## Documentação Relacionada

- [Templates de Arquitetura](architecture-templates.md)
- [Matriz de Decisão](decision-matrix.md)
- [Padrões de Banco de Dados](database-patterns.md)
- [Estrutura Minimal API](structure-minimal-api.md)
- [Estrutura Simple N-Layers](structure-simple-nlayers.md)

