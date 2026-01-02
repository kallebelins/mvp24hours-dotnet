# Templates de Arquitetura

> **Instrução para Agente de IA**: Use estes templates como base para gerar aplicações .NET. Selecione o template apropriado com base nos requisitos e nível de complexidade do projeto.

---

## Template 1: Minimal API

**Complexidade**: Baixa  
**Casos de Uso**: CRUD simples, microsserviços, prototipagem rápida, aplicações pequenas

### Estrutura

```
NomeProjeto/
├── NomeProjeto.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Entities/
│   └── Entidade.cs
├── ValueObjects/
│   └── EntidadeDto.cs
├── Validators/
│   └── EntidadeValidator.cs
├── Data/
│   ├── DataContext.cs
│   └── EntidadeConfiguration.cs
└── Endpoints/
    └── EntidadeEndpoints.cs
```

### Template Program.cs

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Mvp24Hours
builder.Services.AddMvp24HoursDbContext<DataContext>();
builder.Services.AddMvp24HoursRepository(options => options.MaxQtyByQueryPage = 100);

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

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

// Map Endpoints
app.MapEntidadeEndpoints();

app.Run();
```

---

## Template 2: Simple N-Layers

**Complexidade**: Média  
**Casos de Uso**: Aplicações pequenas a médias com regras de negócio básicas

### Estrutura

```
Solution/
├── NomeProjeto.Core/
│   ├── NomeProjeto.Core.csproj
│   ├── Entities/
│   │   └── Entidade.cs
│   ├── ValueObjects/
│   │   └── EntidadeDto.cs
│   └── Validators/
│       └── EntidadeValidator.cs
├── NomeProjeto.Infrastructure/
│   ├── NomeProjeto.Infrastructure.csproj
│   ├── Data/
│   │   ├── DataContext.cs
│   │   └── EntidadeConfiguration.cs
│   └── Repositories/
│       └── EntidadeRepository.cs
└── NomeProjeto.WebAPI/
    ├── NomeProjeto.WebAPI.csproj
    ├── Program.cs
    ├── Startup.cs
    ├── appsettings.json
    ├── Controllers/
    │   └── EntidadeController.cs
    └── Extensions/
        └── ServiceBuilderExtensions.cs
```

### Camada Core - Entidade

```csharp
using Mvp24Hours.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace NomeProjeto.Core.Entities
{
    public class Entidade : EntityBase<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Nome { get; set; }

        [MaxLength(500)]
        public string Descricao { get; set; }

        public bool Ativo { get; set; } = true;
    }
}
```

### Camada Core - DTO

```csharp
namespace NomeProjeto.Core.ValueObjects
{
    public class EntidadeDto
    {
        public Guid? Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public bool Ativo { get; set; }
    }

    public class EntidadeFiltroDto
    {
        public string Nome { get; set; }
        public bool? Ativo { get; set; }
    }
}
```

### Camada Core - Validator

```csharp
using FluentValidation;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.Core.Validators
{
    public class EntidadeValidator : AbstractValidator<EntidadeDto>
    {
        public EntidadeValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório")
                .MaximumLength(100).WithMessage("Nome não pode exceder 100 caracteres");

            RuleFor(x => x.Descricao)
                .MaximumLength(500).WithMessage("Descrição não pode exceder 500 caracteres");
        }
    }
}
```

---

## Template 3: Complex N-Layers

**Complexidade**: Alta  
**Casos de Uso**: Aplicações corporativas com lógica de negócio complexa, requisitos de alta segurança, separação de responsabilidades

### Estrutura

```
Solution/
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
    │   └── ExceptionMiddleware.cs
    └── NLog.config
```

### Camada Core - Contrato de Serviço

```csharp
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.Core.Contract.Services
{
    public interface IClienteService
    {
        Task<IPagingResult<ClienteDto>> ObterTodosAsync(ClienteFiltroDto filtro, int pagina, int limite);
        Task<IBusinessResult<ClienteDto>> ObterPorIdAsync(Guid id);
        Task<IBusinessResult<ClienteDto>> CriarAsync(ClienteCreateDto dto);
        Task<IBusinessResult<ClienteDto>> AtualizarAsync(Guid id, ClienteUpdateDto dto);
        Task<IBusinessResult<bool>> ExcluirAsync(Guid id);
    }
}
```

### Camada Core - Specification

```csharp
using Mvp24Hours.Core.Contract.Domain;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.ValueObjects;
using System.Linq.Expressions;

namespace NomeProjeto.Core.Specifications
{
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
                return cliente =>
                    (string.IsNullOrEmpty(_filtro.Nome) || cliente.Nome.Contains(_filtro.Nome)) &&
                    (!_filtro.Ativo.HasValue || cliente.Ativo == _filtro.Ativo.Value);
            }
        }
    }
}
```

### Camada Application - Serviço

```csharp
using AutoMapper;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using NomeProjeto.Core.Contract.Services;
using NomeProjeto.Core.Entities;
using NomeProjeto.Core.Specifications;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.Application.Services
{
    public class ClienteService : RepositoryPagingServiceAsync<Cliente, IUnitOfWorkAsync>, IClienteService
    {
        private readonly IMapper _mapper;

        public ClienteService(IUnitOfWorkAsync unitOfWork, IMapper mapper) : base(unitOfWork)
        {
            _mapper = mapper;
        }

        public async Task<IPagingResult<ClienteDto>> ObterTodosAsync(ClienteFiltroDto filtro, int pagina, int limite)
        {
            var spec = new ClientePorFiltroSpec(filtro);
            var entidades = await Repository.ToBusinessPagingAsync(spec.IsSatisfiedByExpression, pagina, limite);
            return entidades.MapPagingTo<Cliente, ClienteDto>(_mapper);
        }

        public async Task<IBusinessResult<ClienteDto>> ObterPorIdAsync(Guid id)
        {
            var entidade = await Repository.GetByIdAsync(id);
            if (entidade == null)
                return new BusinessResult<ClienteDto>().AddMessage("Cliente não encontrado");

            return new BusinessResult<ClienteDto>(_mapper.Map<ClienteDto>(entidade));
        }

        public async Task<IBusinessResult<ClienteDto>> CriarAsync(ClienteCreateDto dto)
        {
            var entidade = _mapper.Map<Cliente>(dto);
            await Repository.AddAsync(entidade);
            await UnitOfWork.SaveChangesAsync();
            return new BusinessResult<ClienteDto>(_mapper.Map<ClienteDto>(entidade));
        }

        public async Task<IBusinessResult<ClienteDto>> AtualizarAsync(Guid id, ClienteUpdateDto dto)
        {
            var entidade = await Repository.GetByIdAsync(id);
            if (entidade == null)
                return new BusinessResult<ClienteDto>().AddMessage("Cliente não encontrado");

            _mapper.Map(dto, entidade);
            await Repository.ModifyAsync(entidade);
            await UnitOfWork.SaveChangesAsync();
            return new BusinessResult<ClienteDto>(_mapper.Map<ClienteDto>(entidade));
        }

        public async Task<IBusinessResult<bool>> ExcluirAsync(Guid id)
        {
            var entidade = await Repository.GetByIdAsync(id);
            if (entidade == null)
                return new BusinessResult<bool>(false).AddMessage("Cliente não encontrado");

            await Repository.RemoveAsync(entidade);
            await UnitOfWork.SaveChangesAsync();
            return new BusinessResult<bool>(true);
        }
    }
}
```

### Camada WebAPI - Controller (Complex)

```csharp
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using NomeProjeto.Core.Contract.Services;
using NomeProjeto.Core.ValueObjects;

namespace NomeProjeto.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ClienteController : ControllerBase
    {
        private readonly IClienteService _service;

        public ClienteController(IClienteService service)
        {
            _service = service;
        }

        /// <summary>
        /// Obtém todos os clientes com paginação
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IPagingResult<ClienteDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObterTodos([FromQuery] ClienteFiltroDto filtro, int pagina = 1, int limite = 10)
        {
            var resultado = await _service.ObterTodosAsync(filtro, pagina, limite);
            return Ok(resultado);
        }

        /// <summary>
        /// Obtém cliente por ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(IBusinessResult<ClienteDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var resultado = await _service.ObterPorIdAsync(id);
            if (!resultado.HasData)
                return NotFound(resultado);
            return Ok(resultado);
        }

        /// <summary>
        /// Cria novo cliente
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(IBusinessResult<ClienteDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Criar([FromBody] ClienteCreateDto dto)
        {
            var resultado = await _service.CriarAsync(dto);
            if (!resultado.HasData)
                return BadRequest(resultado);
            return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Data.Id }, resultado);
        }

        /// <summary>
        /// Atualiza cliente
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Atualizar(Guid id, [FromBody] ClienteUpdateDto dto)
        {
            var resultado = await _service.AtualizarAsync(id, dto);
            if (!resultado.HasData)
                return NotFound(resultado);
            return NoContent();
        }

        /// <summary>
        /// Exclui cliente
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var resultado = await _service.ExcluirAsync(id);
            if (!resultado.Data)
                return NotFound(resultado);
            return NoContent();
        }
    }
}
```

---

## Variações de Template

### Com Entity Log (Auditoria)

Adicione à entidade:

```csharp
using Mvp24Hours.Core.Entities;

public class Cliente : EntityBase<Guid>, IEntityLog
{
    // Propriedades...
    
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? Modified { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? Removed { get; set; }
    public string RemovedBy { get; set; }
}
```

### Com Dapper (Híbrido)

Adicione à Infrastructure:

```csharp
using Dapper;
using Mvp24Hours.Infrastructure.Data.EFCore;

public class ClienteDapperRepository
{
    private readonly DataContext _context;

    public ClienteDapperRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClienteDto>> ObterTodosOtimizadoAsync()
    {
        var connection = _context.Database.GetDbConnection();
        return await connection.QueryAsync<ClienteDto>(
            "SELECT Id, Nome, Email, Ativo FROM Clientes WHERE Ativo = 1");
    }
}
```

### Com Pipeline Pattern

Veja [Documentação de Pipeline](../pipeline.md) para implementação detalhada.

### Com RabbitMQ

Veja [Padrões de Mensageria](ai-context/messaging-patterns.md) para implementação detalhada.

---

## Templates Avançados

Para padrões arquiteturais mais complexos, veja a documentação dedicada:

| Template | Caso de Uso | Documentação |
|----------|-------------|--------------|
| **CQRS** | Command Query Responsibility Segregation | [template-cqrs.md](template-cqrs.md) |
| **Event-Driven** | Event Sourcing, Domain Events | [template-event-driven.md](template-event-driven.md) |
| **Hexagonal** | Ports & Adapters, Separação limpa | [template-hexagonal.md](template-hexagonal.md) |
| **Clean Architecture** | Centrado em domínio, Regra de Dependência | [template-clean-architecture.md](template-clean-architecture.md) |
| **DDD** | Aggregates, Value Objects, Domain Services | [template-ddd.md](template-ddd.md) |
| **Microservices** | Decomposição de serviços, API Gateway | [template-microservices.md](template-microservices.md) |

---

## Documentação Complementar

| Tópico | Caso de Uso | Documentação |
|--------|-------------|--------------|
| **Padrões de Testes** | Unitários, Integração, Mocking | [testing-patterns.md](testing-patterns.md) |
| **Padrões de Segurança** | JWT, OAuth2, API Keys | [security-patterns.md](security-patterns.md) |
| **Tratamento de Erros** | Exceções, ProblemDetails, Padrão Result | [error-handling-patterns.md](error-handling-patterns.md) |
| **Versionamento de API** | URL Path, Query String, Header | [api-versioning-patterns.md](api-versioning-patterns.md) |
| **Containerização** | Docker, Docker Compose, Health Checks | [containerization-patterns.md](containerization-patterns.md) |

---

## Próximos Passos

- [Matriz de Decisão](decision-matrix.md) - Ajuda a escolher o template correto
- [Padrões de Banco de Dados](database-patterns.md) - Configurações específicas de banco
- [Estrutura de Projetos](project-structure.md) - Convenções detalhadas de estrutura

