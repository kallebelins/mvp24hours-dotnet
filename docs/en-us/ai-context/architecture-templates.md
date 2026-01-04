# Architecture Templates

> **AI Agent Instruction**: Use these templates as the foundation for generating .NET applications. Select the appropriate template based on project requirements and complexity level.

---

## Template 1: Minimal API

**Complexity**: Low  
**Use Cases**: Simple CRUD, microservices, rapid prototyping, small applications

### Structure

```
ProjectName/
├── ProjectName.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Entities/
│   └── Entity.cs
├── ValueObjects/
│   └── EntityDto.cs
├── Validators/
│   └── EntityValidator.cs
├── Data/
│   ├── DataContext.cs
│   └── EntityConfiguration.cs
└── Endpoints/
    └── EntityEndpoints.cs
```

### Program.cs Template

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
app.MapEntityEndpoints();

app.Run();
```

### Entity Endpoints Template

```csharp
public static class EntityEndpoints
{
    public static void MapEntityEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/entity").WithTags("Entity");

        group.MapGet("/", async (IUnitOfWorkAsync uow) =>
        {
            var repository = uow.GetRepository<Entity>();
            var result = await repository.ToBusinessPagingAsync();
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, IUnitOfWorkAsync uow) =>
        {
            var repository = uow.GetRepository<Entity>();
            var result = await repository.GetByIdAsync(id);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });

        group.MapPost("/", async (EntityDto dto, IValidator<EntityDto> validator, IUnitOfWorkAsync uow) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(validation.Errors);

            var repository = uow.GetRepository<Entity>();
            var entity = dto.ToEntity();
            await repository.AddAsync(entity);
            await uow.SaveChangesAsync();
            return Results.Created($"/api/entity/{entity.Id}", entity);
        });

        group.MapPut("/{id:guid}", async (Guid id, EntityDto dto, IValidator<EntityDto> validator, IUnitOfWorkAsync uow) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.BadRequest(validation.Errors);

            var repository = uow.GetRepository<Entity>();
            var entity = await repository.GetByIdAsync(id);
            if (entity == null)
                return Results.NotFound();

            entity.UpdateFrom(dto);
            await repository.ModifyAsync(entity);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, IUnitOfWorkAsync uow) =>
        {
            var repository = uow.GetRepository<Entity>();
            var entity = await repository.GetByIdAsync(id);
            if (entity == null)
                return Results.NotFound();

            await repository.RemoveAsync(entity);
            await uow.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
```

---

## Template 2: Simple N-Layers

**Complexity**: Medium  
**Use Cases**: Small to medium applications with basic business rules

### Structure

```
Solution/
├── ProjectName.Core/
│   ├── ProjectName.Core.csproj
│   ├── Entities/
│   │   └── Entity.cs
│   ├── ValueObjects/
│   │   └── EntityDto.cs
│   └── Validators/
│       └── EntityValidator.cs
├── ProjectName.Infrastructure/
│   ├── ProjectName.Infrastructure.csproj
│   ├── Data/
│   │   ├── DataContext.cs
│   │   └── EntityConfiguration.cs
│   └── Repositories/
│       └── EntityRepository.cs
└── ProjectName.WebAPI/
    ├── ProjectName.WebAPI.csproj
    ├── Program.cs
    ├── Startup.cs
    ├── appsettings.json
    ├── Controllers/
    │   └── EntityController.cs
    └── Extensions/
        └── ServiceBuilderExtensions.cs
```

### Core Layer - Entity

```csharp
using Mvp24Hours.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ProjectName.Core.Entities
{
    public class Entity : EntityBase<Guid>
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool Active { get; set; } = true;
    }
}
```

### Core Layer - DTO

```csharp
namespace ProjectName.Core.ValueObjects
{
    public class EntityDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
    }

    public class EntityFilterDto
    {
        public string Name { get; set; }
        public bool? Active { get; set; }
    }
}
```

### Core Layer - Validator

```csharp
using FluentValidation;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Core.Validators
{
    public class EntityValidator : AbstractValidator<EntityDto>
    {
        public EntityValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");
        }
    }
}
```

### Infrastructure Layer - DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore;
using ProjectName.Core.Entities;

namespace ProjectName.Infrastructure.Data
{
    public class DataContext : Mvp24HoursContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Entity> Entities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
        }
    }
}
```

### WebAPI Layer - Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects;

namespace ProjectName.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntityController : ControllerBase
    {
        private readonly IUnitOfWorkAsync _uow;

        public EntityController(IUnitOfWorkAsync uow)
        {
            _uow = uow;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] EntityFilterDto filter, int page = 1, int limit = 10)
        {
            var repository = _uow.GetRepository<Entity>();
            var result = await repository.ToBusinessPagingAsync(page, limit);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var repository = _uow.GetRepository<Entity>();
            var entity = await repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound();
            return Ok(entity);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EntityDto dto)
        {
            var repository = _uow.GetRepository<Entity>();
            var entity = new Entity
            {
                Name = dto.Name,
                Description = dto.Description,
                Active = dto.Active
            };
            await repository.AddAsync(entity);
            await _uow.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EntityDto dto)
        {
            var repository = _uow.GetRepository<Entity>();
            var entity = await repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound();

            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.Active = dto.Active;

            await repository.ModifyAsync(entity);
            await _uow.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var repository = _uow.GetRepository<Entity>();
            var entity = await repository.GetByIdAsync(id);
            if (entity == null)
                return NotFound();

            await repository.RemoveAsync(entity);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}
```

### WebAPI Layer - ServiceBuilderExtensions

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using ProjectName.Infrastructure.Data;

namespace ProjectName.WebAPI.Extensions
{
    public static class ServiceBuilderExtensions
    {
        public static IServiceCollection AddMyServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database
            services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Mvp24Hours
            services.AddMvp24HoursDbContext<DataContext>();
            services.AddMvp24HoursRepository(options => options.MaxQtyByQueryPage = 100);

            // Validators
            services.AddValidatorsFromAssemblyContaining<DataContext>();

            // Health Checks
            services.AddHealthChecks()
                .AddSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    healthQuery: "SELECT 1;",
                    name: "SqlServer",
                    failureStatus: HealthStatus.Degraded);

            return services;
        }
    }
}
```

---

## Template 3: Complex N-Layers

**Complexity**: High  
**Use Cases**: Enterprise applications with complex business logic, high security requirements, separation of concerns

### Structure

```
Solution/
├── ProjectName.Core/
│   ├── ProjectName.Core.csproj
│   ├── Entities/
│   │   ├── Entity.cs
│   │   └── EntityRelated.cs
│   ├── ValueObjects/
│   │   ├── EntityDto.cs
│   │   ├── EntityCreateDto.cs
│   │   ├── EntityUpdateDto.cs
│   │   └── EntityFilterDto.cs
│   ├── Validators/
│   │   ├── EntityCreateValidator.cs
│   │   └── EntityUpdateValidator.cs
│   ├── Contract/
│   │   ├── Services/
│   │   │   └── IEntityService.cs
│   │   └── Specifications/
│   │       └── IEntitySpecification.cs
│   ├── Specifications/
│   │   └── EntityByFilterSpec.cs
│   └── Resources/
│       └── Messages.resx
├── ProjectName.Infrastructure/
│   ├── ProjectName.Infrastructure.csproj
│   ├── Data/
│   │   ├── DataContext.cs
│   │   └── Configurations/
│   │       ├── EntityConfiguration.cs
│   │       └── EntityRelatedConfiguration.cs
│   └── Migrations/
│       └── ...
├── ProjectName.Application/
│   ├── ProjectName.Application.csproj
│   ├── Services/
│   │   └── EntityService.cs
│   ├── Mappings/
│   │   └── EntityProfile.cs
│   └── FacadeService.cs
└── ProjectName.WebAPI/
    ├── ProjectName.WebAPI.csproj
    ├── Program.cs
    ├── Startup.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── appsettings.Production.json
    ├── Controllers/
    │   └── EntityController.cs
    ├── Extensions/
    │   └── ServiceBuilderExtensions.cs
    ├── Middlewares/
    │   └── ExceptionMiddleware.cs
    └── NLog.config
```

### Core Layer - Service Contract

```csharp
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Core.Contract.Services
{
    public interface IEntityService
    {
        Task<IPagingResult<EntityDto>> GetAllAsync(EntityFilterDto filter, int page, int limit);
        Task<IBusinessResult<EntityDto>> GetByIdAsync(Guid id);
        Task<IBusinessResult<EntityDto>> CreateAsync(EntityCreateDto dto);
        Task<IBusinessResult<EntityDto>> UpdateAsync(Guid id, EntityUpdateDto dto);
        Task<IBusinessResult<bool>> DeleteAsync(Guid id);
    }
}
```

### Core Layer - Specification

```csharp
using Mvp24Hours.Core.Contract.Domain;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects;
using System.Linq.Expressions;

namespace ProjectName.Core.Specifications
{
    public class EntityByFilterSpec : ISpecificationQuery<Entity>
    {
        private readonly EntityFilterDto _filter;

        public EntityByFilterSpec(EntityFilterDto filter)
        {
            _filter = filter;
        }

        public Expression<Func<Entity, bool>> IsSatisfiedByExpression
        {
            get
            {
                return entity =>
                    (string.IsNullOrEmpty(_filter.Name) || entity.Name.Contains(_filter.Name)) &&
                    (!_filter.Active.HasValue || entity.Active == _filter.Active.Value);
            }
        }
    }
}
```

### Application Layer - Service

```csharp
using AutoMapper;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.Entities;
using ProjectName.Core.Specifications;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Application.Services
{
    public class EntityService : RepositoryPagingServiceAsync<Entity, IUnitOfWorkAsync>, IEntityService
    {
        private readonly IMapper _mapper;

        public EntityService(IUnitOfWorkAsync unitOfWork, IMapper mapper) : base(unitOfWork)
        {
            _mapper = mapper;
        }

        public async Task<IPagingResult<EntityDto>> GetAllAsync(EntityFilterDto filter, int page, int limit)
        {
            var spec = new EntityByFilterSpec(filter);
            var entities = await Repository.ToBusinessPagingAsync(spec.IsSatisfiedByExpression, page, limit);
            return entities.MapPagingTo<Entity, EntityDto>(_mapper);
        }

        public async Task<IBusinessResult<EntityDto>> GetByIdAsync(Guid id)
        {
            var entity = await Repository.GetByIdAsync(id);
            if (entity == null)
                return new BusinessResult<EntityDto>().AddMessage("Entity not found");

            return new BusinessResult<EntityDto>(_mapper.Map<EntityDto>(entity));
        }

        public async Task<IBusinessResult<EntityDto>> CreateAsync(EntityCreateDto dto)
        {
            var entity = _mapper.Map<Entity>(dto);
            await Repository.AddAsync(entity);
            await UnitOfWork.SaveChangesAsync();
            return new BusinessResult<EntityDto>(_mapper.Map<EntityDto>(entity));
        }

        public async Task<IBusinessResult<EntityDto>> UpdateAsync(Guid id, EntityUpdateDto dto)
        {
            var entity = await Repository.GetByIdAsync(id);
            if (entity == null)
                return new BusinessResult<EntityDto>().AddMessage("Entity not found");

            _mapper.Map(dto, entity);
            await Repository.ModifyAsync(entity);
            await UnitOfWork.SaveChangesAsync();
            return new BusinessResult<EntityDto>(_mapper.Map<EntityDto>(entity));
        }

        public async Task<IBusinessResult<bool>> DeleteAsync(Guid id)
        {
            var entity = await Repository.GetByIdAsync(id);
            if (entity == null)
                return new BusinessResult<bool>(false).AddMessage("Entity not found");

            await Repository.RemoveAsync(entity);
            await UnitOfWork.SaveChangesAsync();
            return new BusinessResult<bool>(true);
        }
    }
}
```

### Application Layer - AutoMapper Profile

```csharp
using AutoMapper;
using ProjectName.Core.Entities;
using ProjectName.Core.ValueObjects;

namespace ProjectName.Application.Mappings
{
    public class EntityProfile : Profile
    {
        public EntityProfile()
        {
            CreateMap<Entity, EntityDto>().ReverseMap();
            CreateMap<EntityCreateDto, Entity>();
            CreateMap<EntityUpdateDto, Entity>();
        }
    }
}
```

### WebAPI Layer - Controller (Complex)

```csharp
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using ProjectName.Core.Contract.Services;
using ProjectName.Core.ValueObjects;

namespace ProjectName.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EntityController : ControllerBase
    {
        private readonly IEntityService _service;

        public EntityController(IEntityService service)
        {
            _service = service;
        }

        /// <summary>
        /// Get all entities with pagination
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IPagingResult<EntityDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] EntityFilterDto filter, int page = 1, int limit = 10)
        {
            var result = await _service.GetAllAsync(filter, page, limit);
            return Ok(result);
        }

        /// <summary>
        /// Get entity by ID
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(IBusinessResult<EntityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (!result.HasData)
                return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Create new entity
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(IBusinessResult<EntityDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] EntityCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            if (!result.HasData)
                return BadRequest(result);
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
        }

        /// <summary>
        /// Update entity
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] EntityUpdateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            if (!result.HasData)
                return NotFound(result);
            return NoContent();
        }

        /// <summary>
        /// Delete entity
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result.Data)
                return NotFound(result);
            return NoContent();
        }
    }
}
```

---

## Template Variations

### With Entity Log (Audit)

Add to entity:

```csharp
using Mvp24Hours.Core.Entities;

public class Entity : EntityBase<Guid>, IEntityLog
{
    // Properties...
    
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? Modified { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? Removed { get; set; }
    public string RemovedBy { get; set; }
}
```

### With Dapper (Hybrid)

Add to Infrastructure:

```csharp
using Dapper;
using Mvp24Hours.Infrastructure.Data.EFCore;

public class EntityDapperRepository
{
    private readonly DataContext _context;

    public EntityDapperRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EntityDto>> GetAllOptimizedAsync()
    {
        var connection = _context.Database.GetDbConnection();
        return await connection.QueryAsync<EntityDto>(
            "SELECT Id, Name, Description, Active FROM Entities WHERE Active = 1");
    }
}
```

### With Pipeline Pattern

See [Pipeline Documentation](../pipeline.md) for detailed implementation.

### With RabbitMQ

See [Messaging Patterns](messaging-patterns.md) for detailed implementation.

---

## Advanced Templates

For more complex architectural patterns, see the dedicated documentation:

| Template | Use Case | Documentation |
|----------|----------|---------------|
| **CQRS** | Command Query Responsibility Segregation | [template-cqrs.md](template-cqrs.md) |
| **Event-Driven** | Event Sourcing, Domain Events | [template-event-driven.md](template-event-driven.md) |
| **Hexagonal** | Ports & Adapters, Clean separation | [template-hexagonal.md](template-hexagonal.md) |
| **Clean Architecture** | Domain-centric, Dependency Rule | [template-clean-architecture.md](template-clean-architecture.md) |
| **DDD** | Aggregates, Value Objects, Domain Services | [template-ddd.md](template-ddd.md) |
| **Microservices** | Service decomposition, API Gateway | [template-microservices.md](template-microservices.md) |

---

## Complementary Documentation

| Topic | Use Case | Documentation |
|-------|----------|---------------|
| **Testing Patterns** | Unit, Integration, Mocking | [testing-patterns.md](testing-patterns.md) |
| **Security Patterns** | JWT, OAuth2, API Keys | [security-patterns.md](security-patterns.md) |
| **Error Handling** | Exceptions, ProblemDetails, Result Pattern | [error-handling-patterns.md](error-handling-patterns.md) |
| **API Versioning** | URL Path, Query String, Header | [api-versioning-patterns.md](api-versioning-patterns.md) |
| **Containerization** | Docker, Docker Compose, Health Checks | [containerization-patterns.md](containerization-patterns.md) |

---

## Next Steps

- [Decision Matrix](decision-matrix.md) - Help choosing the right template
- [Database Patterns](database-patterns.md) - Database-specific configurations
- [Project Structure](project-structure.md) - Detailed structure conventions

