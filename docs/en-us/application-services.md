# Application Services

This documentation covers the Mvp24Hours.Application module, which implements the application service layer following DDD, Clean Architecture, and enterprise .NET best practices.

## Table of Contents

1. [Overview](#overview)
2. [Application Service Base](#application-service-base)
3. [DTO-Based Services](#dto-based-services)
4. [Query and Command Services](#query-and-command-services)
5. [Validation](#validation)
6. [Transactions](#transactions)
7. [Caching](#caching)
8. [Pagination](#pagination)
9. [Events](#events)
10. [Observability](#observability)
11. [Resilience](#resilience)
12. [Dependency Injection](#dependency-injection)

---

## Overview

The Application module provides abstractions and implementations for the application service layer:

- **Unified Query + Command** operations
- **DTO mapping** with AutoMapper integration
- **FluentValidation** support
- **Transaction management** via Unit of Work
- **Specification Pattern** support
- **Caching** for queries
- **Pagination** with cursor and offset support
- **Application Events** with outbox pattern
- **Observability** with telemetry and audit

### Package Installation

```bash
dotnet add package Mvp24Hours.Application
```

---

## Application Service Base

### Basic Service

The simplest way to create an application service:

```csharp
public class CustomerService : ApplicationServiceBase<Customer, MyDbContext>
{
    public CustomerService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    // Add custom business logic
    public IBusinessResult<Customer> FindByEmail(string email)
    {
        return GetBy(c => c.Email == email);
    }
}
```

### With Validation

Add FluentValidation support:

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

// Validator
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
```

### Async Service

For async operations:

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

### Read-Only Service

For services that only need query operations:

```csharp
public class CustomerQueryService : IReadOnlyApplicationService<Customer>
{
    // Only query methods available: List, GetBy, GetById, etc.
}
```

---

## DTO-Based Services

### Automatic Mapping

Map between entities and DTOs automatically:

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

// AutoMapper Profile
public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>().ReverseMap();
    }
}
```

### Separate Create/Update DTOs

Use different DTOs for create and update operations:

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

## Query and Command Services

### Separate Query Service

For CQRS-light pattern:

```csharp
public class CustomerQueryService : QueryServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerQueryService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    // Only query operations: List, GetBy, GetById, etc.
}
```

### Separate Command Service

```csharp
public class CustomerCommandService : CommandServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerCommandService(MyDbContext unitOfWork) 
        : base(unitOfWork) 
    { 
    }

    // Only command operations: Add, Modify, Remove, etc.
}
```

### Bulk Operations

For batch operations:

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

Usage:

```csharp
var progress = new Progress<int>(count => 
    Console.WriteLine($"Processed {count} customers"));

var result = await bulkService.BulkAddAsync(customers, progress);
```

---

## Validation

### Validation Service

```csharp
// Registration
services.AddMvp24HoursValidation(options =>
{
    options.ValidateOnAdd = true;
    options.ValidateOnModify = true;
    options.ThrowOnValidationError = false;
});

// Inject and use
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
        // proceed...
    }
}
```

### Cascade Validation

Validate nested entities:

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

### Validation Pipeline

```csharp
services.AddMvp24HoursValidationPipeline(options =>
{
    options.AddStep<DataAnnotationValidationStep>();
    options.AddStep<FluentValidationStep>();
    options.AddStep<BusinessRuleValidationStep>();
});
```

---

## Transactions

### Transaction Scope

```csharp
// Registration
services.AddMvp24HoursTransactionScope<MyDbContext>();

// Usage
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

### Transactional Attribute

```csharp
public class OrderService
{
    [Transactional]
    public virtual async Task<IBusinessResult<Order>> CreateOrderAsync(Order order)
    {
        // Automatically wrapped in transaction
        await _orderRepo.AddAsync(order);
        await _inventoryRepo.ReserveAsync(order.Items);
        return order.ToBusiness();
    }
}
```

---

## Caching

### Cacheable Queries

```csharp
// Registration
services.AddMvp24HoursQueryCache(options =>
{
    options.DefaultExpiration = TimeSpan.FromMinutes(5);
    options.UseDistributedCache = true;
});

// Service with caching
public class CustomerQueryService 
    : CacheableQueryServiceBaseAsync<Customer, MyDbContext>
{
    public CustomerQueryService(
        MyDbContext unitOfWork, 
        IQueryCacheProvider cacheProvider) 
        : base(unitOfWork, cacheProvider) 
    { 
    }

    [Cacheable(Duration = 300)] // 5 minutes
    public async Task<IBusinessResult<CustomerDto>> GetByIdCachedAsync(int id)
    {
        return await GetByIdAsync(id);
    }
}
```

### Cache Invalidation

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

// Or manual invalidation
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

## Pagination

### Offset-Based Pagination

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

// IPagedResult<T> contains:
// - Items: IReadOnlyList<T>
// - Page: int
// - PageSize: int
// - TotalCount: int
// - TotalPages: int
// - HasPreviousPage: bool
// - HasNextPage: bool
```

### Cursor-Based Pagination

For large datasets:

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

// ICursorPagedResult<T> contains:
// - Items: IReadOnlyList<T>
// - NextCursor: string?
// - PreviousCursor: string?
// - HasMore: bool
```

### Registration

```csharp
services.AddMvp24HoursPagination(options =>
{
    options.DefaultPageSize = 20;
    options.MaxPageSize = 100;
    options.EnableCursorPagination = true;
});
```

---

## Events

### Application Events

```csharp
// Define events
public record CustomerCreatedEvent(int CustomerId, string Email) : IApplicationEvent;
public record CustomerUpdatedEvent(int CustomerId) : IApplicationEvent;

// Event handlers
public class CustomerCreatedHandler : IApplicationEventHandler<CustomerCreatedEvent>
{
    public async Task HandleAsync(CustomerCreatedEvent @event, CancellationToken ct)
    {
        // Send welcome email, update analytics, etc.
    }
}

// Dispatch events
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

### Outbox Pattern

For reliable event delivery:

```csharp
// Registration
services.AddMvp24HoursApplicationEvents(options =>
{
    options.UseOutbox = true;
    options.OutboxRetryInterval = TimeSpan.FromSeconds(30);
    options.OutboxBatchSize = 100;
});

// Events are stored in outbox before dispatching
public class CustomerService
{
    private readonly IApplicationEventOutbox _outbox;

    public async Task<IBusinessResult<int>> CreateAsync(Customer customer)
    {
        var result = await AddAsync(customer);
        
        if (result.HasData)
        {
            // Event stored in outbox, processed later
            await _outbox.AddAsync(
                new CustomerCreatedEvent(customer.Id, customer.Email));
        }
        
        return result;
    }
}
```

### Registration

```csharp
services.AddMvp24HoursApplicationEvents(options =>
{
    options.ScanAssemblies = new[] { typeof(CustomerService).Assembly };
    options.UseOutbox = true;
});
```

---

## Observability

### Audit Trail

```csharp
// Registration
services.AddMvp24HoursAudit(options =>
{
    options.AuditCommands = true;
    options.IncludeEntityData = true;
    options.ExcludedProperties = new[] { "Password", "SecretKey" };
});

// Auditable service
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

### Telemetry

```csharp
// Registration
services.AddMvp24HoursOperationMetrics(options =>
{
    options.MeterName = "myapp.application";
    options.RecordDuration = true;
    options.RecordSuccessRate = true;
});

// Metrics are automatically recorded for all operations
```

### Correlation ID

```csharp
// Access correlation ID
public class CustomerService
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public async Task<IBusinessResult<int>> CreateAsync(Customer customer)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        _logger.LogInformation(
            "Creating customer {Email} with CorrelationId {CorrelationId}",
            customer.Email, correlationId);
            
        return await AddAsync(customer);
    }
}
```

---

## Resilience

### Result Status Codes

```csharp
public class CustomerService
{
    public async Task<IBusinessResultWithStatus<CustomerDto>> GetByIdAsync(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        
        if (customer == null)
        {
            return ResultStatusCode.NotFound
                .ToBusinessResult<CustomerDto>("Customer not found");
        }
        
        return _mapper.Map<CustomerDto>(customer)
            .ToBusinessResultWithStatus(ResultStatusCode.Success);
    }
}

// Available status codes:
// - Success
// - Created
// - NotFound
// - ValidationFailed
// - Conflict
// - Forbidden
// - Error
```

### Error Codes for I18n

```csharp
// Define error codes
public static class CustomerErrorCodes
{
    public const string NotFound = "CUSTOMER_NOT_FOUND";
    public const string EmailExists = "CUSTOMER_EMAIL_EXISTS";
    public const string InvalidEmail = "CUSTOMER_INVALID_EMAIL";
}

// Use in service
public async Task<IBusinessResultWithStatus<CustomerDto>> CreateAsync(
    CreateCustomerDto dto)
{
    if (await EmailExistsAsync(dto.Email))
    {
        return ResultStatusCode.Conflict
            .ToBusinessResult<CustomerDto>(
                CustomerErrorCodes.EmailExists,
                "Email already registered");
    }
    // ...
}

// Localize in client
public class ErrorLocalizer : IErrorMessageLocalizer
{
    public string Localize(string errorCode, string defaultMessage)
    {
        return _localizer[errorCode] ?? defaultMessage;
    }
}
```

### Exception Mapping

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

## Dependency Injection

### Complete Setup

```csharp
// Program.cs
services.AddMvp24HoursApplication(
    typeof(CustomerService).Assembly,
    typeof(CustomerProfile).Assembly);

// This registers:
// - AutoMapper with profile scanning
// - All application services
```

### Modular Registration

```csharp
// AutoMapper only
services.AddMvp24HoursAutoMapper(typeof(CustomerProfile).Assembly);

// Application services only
services.AddMvp24HoursApplicationServices(typeof(CustomerService).Assembly);

// Validators only
services.AddMvp24HoursValidators(typeof(CustomerValidator).Assembly);
```

### Convention-Based Registration

```csharp
services.AddMvp24HoursConventionBasedServices(options =>
{
    options.ScanAssemblies = new[] { typeof(CustomerService).Assembly };
    
    // Register by interface marker
    options.RegisterByMarker<IScopedService>(ServiceLifetime.Scoped);
    options.RegisterByMarker<ISingletonService>(ServiceLifetime.Singleton);
    options.RegisterByMarker<ITransientService>(ServiceLifetime.Transient);
    
    // Register by naming convention
    options.RegisterBySuffix("Service", ServiceLifetime.Scoped);
    options.RegisterBySuffix("Repository", ServiceLifetime.Scoped);
});
```

### All Features

```csharp
services.AddMvp24HoursApplicationModule(options =>
{
    // Core
    options.Assemblies = new[] { typeof(CustomerService).Assembly };
    
    // Validation
    options.Validation.Enabled = true;
    options.Validation.UseCascadeValidation = true;
    
    // Caching
    options.Cache.Enabled = true;
    options.Cache.DefaultExpiration = TimeSpan.FromMinutes(5);
    
    // Transactions
    options.Transaction.Enabled = true;
    
    // Events
    options.Events.Enabled = true;
    options.Events.UseOutbox = true;
    
    // Observability
    options.Observability.EnableAudit = true;
    options.Observability.EnableMetrics = true;
    
    // Resilience
    options.Resilience.UseExceptionMapping = true;
});
```

---

## Specification Pattern

Use specifications for complex queries:

```csharp
// Define specification
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

// Use in service
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

// Combine specifications
var spec = new ActiveCustomersSpec()
    .And(new CustomersByCountrySpec("Brazil"));

var result = await service.GetBySpecificationAsync(spec);
```

---

## See Also

- [Database Patterns](database/use-service.md)
- [CQRS Pattern](cqrs/home.md)
- [Validation](validation.md)
- [WebAPI Integration](webapi.md)

