# Serviços de Aplicação

Esta documentação cobre o módulo Mvp24Hours.Application, que implementa a camada de serviços de aplicação seguindo DDD, Clean Architecture e melhores práticas enterprise .NET.

## Sumário

1. [Visão Geral](#visão-geral)
2. [Application Service Base](#application-service-base)
3. [Serviços Baseados em DTO](#serviços-baseados-em-dto)
4. [Serviços de Query e Command](#serviços-de-query-e-command)
5. [Validação](#validação)
6. [Transações](#transações)
7. [Cache](#cache)
8. [Paginação](#paginação)
9. [Eventos](#eventos)
10. [Observabilidade](#observabilidade)
11. [Resiliência](#resiliência)
12. [Injeção de Dependência](#injeção-de-dependência)

---

## Visão Geral

O módulo Application fornece abstrações e implementações para a camada de serviços de aplicação:

- Operações **Query + Command** unificadas
- **Mapeamento DTO** com integração AutoMapper
- Suporte a **FluentValidation**
- **Gerenciamento de transações** via Unit of Work
- Suporte ao **Specification Pattern**
- **Cache** para queries
- **Paginação** com suporte a cursor e offset
- **Eventos de Aplicação** com padrão outbox
- **Observabilidade** com telemetria e auditoria

### Instalação do Pacote

```bash
dotnet add package Mvp24Hours.Application
```

---

## Application Service Base

### Serviço Básico

A forma mais simples de criar um serviço de aplicação:

```csharp
public class CustomerService : ApplicationServiceBase<Customer, MyDbContext>
{
    public CustomerService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    // Adicione lógica de negócio personalizada
    public IBusinessResult<Customer> FindByEmail(string email)
    {
        return GetBy(c => c.Email == email);
    }
}
```

### Com Validação

Adicione suporte a FluentValidation:

```csharp
public class CustomerService : ApplicationServiceBase<Customer, MyDbContext>
{
    public CustomerService(
        MyDbContext unitOfWork, 
        IValidator<Customer> validator) 
        : base(unitOfWork, validator) 
    { 
    }
}

// Validador
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
```

### Serviço Assíncrono

Para operações assíncronas:

```csharp
public class CustomerService : ApplicationServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    public async Task<IBusinessResult<Customer>> FindByEmailAsync(
        string email, 
        CancellationToken ct = default)
    {
        return await GetByAsync(c => c.Email == email, cancellationToken: ct);
    }
}
```

### Serviço Somente Leitura

Para serviços que precisam apenas de operações de consulta:

```csharp
public class CustomerQueryService : IReadOnlyApplicationService<Customer>
{
    // Apenas métodos de consulta disponíveis: List, GetBy, GetById, etc.
}
```

---

## Serviços Baseados em DTO

### Mapeamento Automático

Mapeie entre entidades e DTOs automaticamente:

```csharp
public class CustomerService 
    : ApplicationServiceBaseWithDtoAsync<Customer, CustomerDto, MyDbContext>
{
    public CustomerService(MyDbContext unitOfWork, IMapper mapper) 
        : base(unitOfWork, mapper) 
    { 
    }
}

// DTO
public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Perfil AutoMapper
public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>().ReverseMap();
    }
}
```

### DTOs Separados para Create/Update

Use DTOs diferentes para operações de criação e atualização:

```csharp
public class CustomerService 
    : ApplicationServiceBaseWithSeparateDtosAsync<
        Customer, 
        CustomerDto, 
        CreateCustomerDto, 
        UpdateCustomerDto, 
        MyDbContext>
{
    public CustomerService(MyDbContext unitOfWork, IMapper mapper) 
        : base(unitOfWork, mapper) 
    { 
    }
}

// DTOs
public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCustomerDto
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UpdateCustomerDto
{
    public string Name { get; set; }
}
```

---

## Serviços de Query e Command

### Serviço de Query Separado

Para o padrão CQRS-light:

```csharp
public class CustomerQueryService : QueryServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerQueryService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    // Apenas operações de consulta: List, GetBy, GetById, etc.
}
```

### Serviço de Command Separado

```csharp
public class CustomerCommandService : CommandServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerCommandService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    // Apenas operações de comando: Add, Modify, Remove, etc.
}
```

### Operações em Lote (Bulk)

Para operações em batch:

```csharp
public class CustomerBulkService 
    : BulkCommandServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerBulkService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    public async Task<IBusinessResult<int>> ImportCustomersAsync(
        IList<Customer> customers,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        return await BulkAddAsync(customers, progress, ct);
    }
}
```

Uso:

```csharp
var progress = new Progress<int>(count => 
    Console.WriteLine($"Processados {count} clientes"));

var result = await bulkService.BulkAddAsync(customers, progress);
```

---

## Validação

### Serviço de Validação

```csharp
// Registro
services.AddMvp24HoursValidation(options =>
{
    options.ValidateOnAdd = true;
    options.ValidateOnModify = true;
    options.ThrowOnValidationError = false;
});

// Injetar e usar
public class CustomerService
{
    private readonly IValidationService<Customer> _validationService;

    public CustomerService(IValidationService<Customer> validationService)
    {
        _validationService = validationService;
    }

    public async Task<IBusinessResult<int>> CreateAsync(Customer customer)
    {
        var validationResult = await _validationService.ValidateAsync(customer);
        if (!validationResult.IsValid)
        {
            return validationResult.Errors.ToBusiness<int>();
        }
        // prosseguir...
    }
}
```

### Validação em Cascata

Valide entidades aninhadas:

```csharp
services.AddMvp24HoursCascadeValidation();

public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator(IValidator<OrderItem> itemValidator)
    {
        RuleFor(o => o.CustomerId).NotEmpty();
        RuleForEach(o => o.Items).SetValidator(itemValidator);
    }
}
```

### Pipeline de Validação

```csharp
services.AddMvp24HoursValidationPipeline(options =>
{
    options.AddStep<DataAnnotationValidationStep>();
    options.AddStep<FluentValidationStep>();
    options.AddStep<BusinessRuleValidationStep>();
});
```

---

## Transações

### Transaction Scope

```csharp
// Registro
services.AddMvp24HoursTransactionScope<MyDbContext>();

// Uso
public class OrderService
{
    private readonly ITransactionScopeFactory _transactionFactory;
    private readonly IOrderRepository _orderRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public async Task<IBusinessResult<Order>> CreateOrderAsync(Order order)
    {
        using var transaction = await _transactionFactory.CreateAsync();
        
        try
        {
            await _orderRepo.AddAsync(order);
            await _inventoryRepo.ReserveAsync(order.Items);
            
            await transaction.CommitAsync();
            return order.ToBusiness();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### Atributo Transactional

```csharp
public class OrderService
{
    [Transactional]
    public virtual async Task<IBusinessResult<Order>> CreateOrderAsync(Order order)
    {
        // Automaticamente envolvido em transação
        await _orderRepo.AddAsync(order);
        await _inventoryRepo.ReserveAsync(order.Items);
        return order.ToBusiness();
    }
}
```

---

## Cache

### Queries com Cache

```csharp
// Registro
services.AddMvp24HoursQueryCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.UseDistributedCache = true;
});

// Serviço com cache
public class CustomerQueryService 
    : CacheableQueryServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerQueryService(
        MyDbContext unitOfWork, 
        IQueryCacheProvider cacheProvider) 
        : base(unitOfWork, cacheProvider) 
    { 
    }

    [Cacheable(Duration = 300)] // 5 minutos
    public async Task<IBusinessResult<CustomerDto>> GetByIdCachedAsync(int id)
    {
        return await GetByIdAsync(id);
    }
}
```

### Invalidação de Cache

```csharp
public class CustomerCommandService 
    : CacheableApplicationServiceBaseAsync<Customer, MyDbContext>
{
    [CacheInvalidate("customers:*")]
    public async Task<IBusinessResult<int>> UpdateAsync(Customer customer)
    {
        return await ModifyAsync(customer);
    }
}

// Ou invalidação manual
public class CustomerService
{
    private readonly ICacheInvalidator _cacheInvalidator;

    public async Task<IBusinessResult<int>> UpdateAsync(Customer customer)
    {
        var result = await ModifyAsync(customer);
        
        if (result.HasData)
        {
            await _cacheInvalidator.InvalidateAsync($"customers:{customer.Id}");
            await _cacheInvalidator.InvalidateByPatternAsync("customers:list:*");
        }
        
        return result;
    }
}
```

---

## Paginação

### Paginação por Offset

```csharp
public class CustomerService
{
    public async Task<IPagedResult<CustomerDto>> GetPagedAsync(
        int page, 
        int pageSize)
    {
        var criteria = new PagingCriteria(page, pageSize);
        var result = await ListAsync(criteria);
        
        return result.ToPagedResult(page, pageSize, totalCount);
    }
}

// IPagedResult<T> contém:
// - Items: IReadOnlyList<T>
// - Page: int
// - PageSize: int
// - TotalCount: int
// - TotalPages: int
// - HasPreviousPage: bool
// - HasNextPage: bool
```

### Paginação por Cursor

Para grandes conjuntos de dados:

```csharp
public class CustomerService
{
    public async Task<ICursorPagedResult<CustomerDto>> GetByCursorAsync(
        string? cursor, 
        int pageSize)
    {
        var result = await _paginationService.GetByCursorAsync<Customer, CustomerDto>(
            query => query.OrderBy(c => c.Id),
            cursor,
            pageSize,
            c => c.Id.ToString());
        
        return result;
    }
}

// ICursorPagedResult<T> contém:
// - Items: IReadOnlyList<T>
// - NextCursor: string?
// - PreviousCursor: string?
// - HasMore: bool
```

### Registro

```csharp
services.AddMvp24HoursPagination(options =>
{
    options.DefaultPageSize = 20;
    options.MaxPageSize = 100;
    options.EnableCursorPagination = true;
});
```

---

## Eventos

### Eventos de Aplicação

```csharp
// Definir eventos
public record CustomerCreatedEvent(int CustomerId, string Email) : IApplicationEvent;
public record CustomerUpdatedEvent(int CustomerId) : IApplicationEvent;

// Handlers de eventos
public class CustomerCreatedHandler : IApplicationEventHandler<CustomerCreatedEvent>
{
    public async Task HandleAsync(CustomerCreatedEvent @event, CancellationToken ct)
    {
        // Enviar email de boas-vindas, atualizar analytics, etc.
    }
}

// Disparar eventos
public class CustomerService
{
    private readonly IApplicationEventDispatcher _eventDispatcher;

    public async Task<IBusinessResult<int>> CreateAsync(Customer customer)
    {
        var result = await AddAsync(customer);
        
        if (result.HasData)
        {
            await _eventDispatcher.DispatchAsync(
                new CustomerCreatedEvent(customer.Id, customer.Email));
        }
        
        return result;
    }
}
```

### Padrão Outbox

Para entrega confiável de eventos:

```csharp
// Registro
services.AddMvp24HoursApplicationEvents(options =>
{
    options.UseOutbox = true;
    options.OutboxRetryInterval = TimeSpan.FromSeconds(30);
    options.OutboxBatchSize = 100;
});

// Eventos são armazenados no outbox antes do envio
public class CustomerService
{
    private readonly IApplicationEventOutbox _outbox;

    public async Task<IBusinessResult<int>> CreateAsync(Customer customer)
    {
        var result = await AddAsync(customer);
        
        if (result.HasData)
        {
            // Evento armazenado no outbox, processado depois
            await _outbox.AddAsync(
                new CustomerCreatedEvent(customer.Id, customer.Email));
        }
        
        return result;
    }
}
```

### Registro

```csharp
services.AddMvp24HoursApplicationEvents(options =>
{
    options.ScanAssemblies = new[] { typeof(CustomerService).Assembly };
    options.UseOutbox = true;
});
```

---

## Observabilidade

### Audit Trail

```csharp
// Registro
services.AddMvp24HoursAudit(options =>
{
    options.AuditCommands = true;
    options.IncludeEntityData = true;
    options.ExcludedProperties = new[] { "Password", "SecretKey" };
});

// Serviço auditável
public class CustomerService : IAuditableOperation
{
    public string OperationName => "CustomerService";
    public string UserId => _currentUser.Id;
    public IDictionary<string, object> AuditData { get; } = new Dictionary<string, object>();

    public async Task<IBusinessResult<int>> CreateAsync(Customer customer)
    {
        AuditData["CustomerEmail"] = customer.Email;
        return await AddAsync(customer);
    }
}
```

### Telemetria

```csharp
// Registro
services.AddMvp24HoursOperationMetrics(options =>
{
    options.MeterName = "meuapp.application";
    options.RecordDuration = true;
    options.RecordSuccessRate = true;
});

// Métricas são gravadas automaticamente para todas as operações
```

### Correlation ID

```csharp
// Acessar correlation ID
public class CustomerService
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public async Task<IBusinessResult<int>> CreateAsync(Customer customer)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        _logger.LogInformation(
            "Criando cliente {Email} com CorrelationId {CorrelationId}",
            customer.Email, correlationId);
            
        return await AddAsync(customer);
    }
}
```

---

## Resiliência

### Códigos de Status de Resultado

```csharp
public class CustomerService
{
    public async Task<IBusinessResultWithStatus<CustomerDto>> GetByIdAsync(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        
        if (customer == null)
        {
            return ResultStatusCode.NotFound
                .ToBusinessResult<CustomerDto>("Cliente não encontrado");
        }
        
        return _mapper.Map<CustomerDto>(customer)
            .ToBusinessResultWithStatus(ResultStatusCode.Success);
    }
}

// Códigos de status disponíveis:
// - Success
// - Created
// - NotFound
// - ValidationFailed
// - Conflict
// - Forbidden
// - Error
```

### Códigos de Erro para I18n

```csharp
// Definir códigos de erro
public static class CustomerErrorCodes
{
    public const string NotFound = "CUSTOMER_NOT_FOUND";
    public const string EmailExists = "CUSTOMER_EMAIL_EXISTS";
    public const string InvalidEmail = "CUSTOMER_INVALID_EMAIL";
}

// Usar no serviço
public async Task<IBusinessResultWithStatus<CustomerDto>> CreateAsync(
    CreateCustomerDto dto)
{
    if (await EmailExistsAsync(dto.Email))
    {
        return ResultStatusCode.Conflict
            .ToBusinessResult<CustomerDto>(
                CustomerErrorCodes.EmailExists,
                "Email já cadastrado");
    }
    // ...
}

// Localizar no cliente
public class ErrorLocalizer : IErrorMessageLocalizer
{
    public string Localize(string errorCode, string defaultMessage)
    {
        return _localizer[errorCode] ?? defaultMessage;
    }
}
```

### Mapeamento de Exceções

```csharp
services.AddMvp24HoursExceptionToResultMapping(options =>
{
    options.Map<EntityNotFoundException>(ResultStatusCode.NotFound);
    options.Map<ConcurrencyException>(ResultStatusCode.Conflict);
    options.Map<ValidationException>(ResultStatusCode.ValidationFailed);
    options.MapDefault(ResultStatusCode.Error);
});
```

---

## Injeção de Dependência

### Configuração Completa

```csharp
// Program.cs
services.AddMvp24HoursApplication(
    typeof(CustomerService).Assembly,
    typeof(CustomerProfile).Assembly);

// Isso registra:
// - AutoMapper com scanning de profiles
// - Todos os serviços de aplicação
```

### Registro Modular

```csharp
// Apenas AutoMapper
services.AddMvp24HoursAutoMapper(typeof(CustomerProfile).Assembly);

// Apenas serviços de aplicação
services.AddMvp24HoursApplicationServices(typeof(CustomerService).Assembly);

// Apenas validadores
services.AddMvp24HoursValidators(typeof(CustomerValidator).Assembly);
```

### Registro Baseado em Convenção

```csharp
services.AddMvp24HoursConventionBasedServices(options =>
{
    options.ScanAssemblies = new[] { typeof(CustomerService).Assembly };
    
    // Registrar por marcador de interface
    options.RegisterByMarker<IScopedService>(ServiceLifetime.Scoped);
    options.RegisterByMarker<ISingletonService>(ServiceLifetime.Singleton);
    options.RegisterByMarker<ITransientService>(ServiceLifetime.Transient);
    
    // Registrar por convenção de nome
    options.RegisterBySuffix("Service", ServiceLifetime.Scoped);
    options.RegisterBySuffix("Repository", ServiceLifetime.Scoped);
});
```

### Todas as Funcionalidades

```csharp
services.AddMvp24HoursApplicationModule(options =>
{
    // Core
    options.Assemblies = new[] { typeof(CustomerService).Assembly };
    
    // Validação
    options.Validation.Enabled = true;
    options.Validation.UseCascadeValidation = true;
    
    // Cache
    options.Cache.Enabled = true;
    options.Cache.DefaultExpiration = TimeSpan.FromMinutes(5);
    
    // Transações
    options.Transaction.Enabled = true;
    
    // Eventos
    options.Events.Enabled = true;
    options.Events.UseOutbox = true;
    
    // Observabilidade
    options.Observability.EnableAudit = true;
    options.Observability.EnableMetrics = true;
    
    // Resiliência
    options.Resilience.UseExceptionMapping = true;
});
```

---

## Specification Pattern

Use specifications para queries complexas:

```csharp
// Definir specification
public class ActiveCustomersSpec : SpecificationBase<Customer>
{
    public ActiveCustomersSpec()
    {
        AddCriteria(c => c.IsActive);
        AddOrderBy(c => c.Name);
    }
}

public class CustomersByCountrySpec : SpecificationBase<Customer>
{
    public CustomersByCountrySpec(string country)
    {
        AddCriteria(c => c.Country == country);
    }
}

// Usar no serviço
public class CustomerService : ApplicationServiceBase<Customer, MyDbContext>
{
    public async Task<IBusinessResult<IList<Customer>>> GetActiveAsync()
    {
        return await GetBySpecificationAsync(new ActiveCustomersSpec());
    }

    public async Task<IBusinessResult<IList<Customer>>> GetByCountryAsync(string country)
    {
        return await GetBySpecificationAsync(new CustomersByCountrySpec(country));
    }
}

// Combinar specifications
var spec = new ActiveCustomersSpec()
    .And(new CustomersByCountrySpec("Brasil"));

var result = await service.GetBySpecificationAsync(spec);
```

---

## Veja Também

- [Padrões de Database](database/use-service.md)
- [Padrão CQRS](cqrs/home.md)
- [Validação](validation.md)
- [Integração WebAPI](webapi.md)

