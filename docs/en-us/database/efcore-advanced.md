# EF Core Advanced Features

> The Mvp24Hours.Infrastructure.Data.EFCore module provides advanced features for enterprise applications including interceptors, bulk operations, multi-tenancy, resilience patterns, and observability.

## Installation

```bash
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 9.1.x
```

## Table of Contents

- [Interceptors](#interceptors)
- [Bulk Operations](#bulk-operations)
- [Multi-Tenancy](#multi-tenancy)
- [Specification Pattern](#specification-pattern)
- [Resilience](#resilience)
- [Streaming](#streaming)
- [Health Checks](#health-checks)
- [Performance Optimization](#performance-optimization)
- [Testing](#testing)

---

## Interceptors

EF Core interceptors allow you to automatically modify entity behavior during SaveChanges operations.

### Audit Interceptor

Automatically populates audit fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy) on entities implementing `IAuditableEntity`.

```csharp
// Register in Program.cs
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var currentUserProvider = sp.GetService<ICurrentUserProvider>();
    var clock = sp.GetService<IClock>(); // or TimeProvider for .NET 9+
    
    options.UseSqlServer(connectionString)
           .AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider, clock));
});

// Your entity
public class Product : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // These are automatically populated
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}
```

### Soft Delete Interceptor

Converts physical deletes to soft deletes for entities implementing `ISoftDeletable`.

```csharp
// Register interceptor
services.AddDbContext<AppDbContext>((sp, options) =>
{
    var currentUserProvider = sp.GetService<ICurrentUserProvider>();
    var clock = sp.GetService<IClock>();
    
    options.UseSqlServer(connectionString)
           .AddInterceptors(new SoftDeleteInterceptor(currentUserProvider, clock));
});

// Configure global query filter in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplySoftDeleteGlobalFilter();
}

// Your entity
public class Customer : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // These are automatically populated on delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; }
}
```

### Slow Query Interceptor

Logs queries that exceed a threshold time for performance monitoring.

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new SlowQueryInterceptor(
               thresholdMs: 500,  // Log queries slower than 500ms
               logger: loggerFactory.CreateLogger<SlowQueryInterceptor>()));
});
```

### Command Logging Interceptor

Logs all SQL commands with parameters for debugging.

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new CommandLoggingInterceptor(
               loggerFactory.CreateLogger<CommandLoggingInterceptor>(),
               enableSensitiveDataLogging: true)); // Only in development!
});
```

---

## Bulk Operations

High-performance bulk operations for large datasets.

### Setup

```csharp
services.AddMvp24HoursBulkOperationsRepositoryAsync(options =>
{
    options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
});
```

### Bulk Insert

```csharp
public class ImportService
{
    private readonly IBulkOperationsRepositoryAsync<Customer> _repository;

    public async Task ImportCustomers(IList<Customer> customers)
    {
        var result = await _repository.BulkInsertAsync(customers, new BulkOperationOptions
        {
            BatchSize = 5000,
            UseTransaction = true,
            ProgressCallback = (processed, total) => 
                Console.WriteLine($"Progress: {processed}/{total}")
        });
        
        Console.WriteLine($"Inserted {result.RowsAffected} rows in {result.ElapsedTime}");
    }
}
```

### Bulk Update

```csharp
// Update existing entities
var result = await _repository.BulkUpdateAsync(customersToUpdate, new BulkOperationOptions
{
    BatchSize = 1000
});
```

### Bulk Delete

```csharp
// Delete by entities
var result = await _repository.BulkDeleteAsync(customersToDelete);
```

### ExecuteUpdate and ExecuteDelete (.NET 7+)

```csharp
// Update all matching records in a single query (no entity loading)
var rowsUpdated = await _repository.ExecuteUpdateAsync(
    c => c.IsActive == false,           // Filter
    c => c.Status,                       // Property to update
    "Inactive"                           // New value
);

// Delete all matching records in a single query
var rowsDeleted = await _repository.ExecuteDeleteAsync(
    c => c.CreatedAt < DateTime.UtcNow.AddYears(-5)  // Delete old records
);
```

### BulkOperationOptions

| Property | Description | Default |
|----------|-------------|---------|
| BatchSize | Number of records per batch | 1000 |
| UseTransaction | Wrap in transaction | true |
| TimeoutSeconds | Operation timeout | 300 |
| ProgressCallback | Progress reporting callback | null |
| BypassChangeTracking | Skip EF change tracking | true |

---

## Multi-Tenancy

Automatic tenant isolation for SaaS applications.

### Setup

```csharp
// Register tenant provider
services.AddMvp24HoursMultiTenancy<HttpHeaderTenantProvider>(options =>
{
    options.RequireTenant = true;
    options.ValidateTenantOnModify = true;
});

// Register DbContext with tenant interceptor
services.AddDbContext<AppDbContext>((sp, options) =>
{
    var tenantInterceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();
    options.UseSqlServer(connectionString)
           .AddInterceptors(tenantInterceptor);
});
```

### Custom Tenant Provider

```csharp
public class HttpHeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpHeaderTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId => 
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

    public bool HasTenant => !string.IsNullOrEmpty(TenantId);
}
```

### Configure Query Filters

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply tenant filter to all tenant entities
    modelBuilder.ApplyTenantQueryFilters<IMultiTenant>(_tenantProvider);
}

// Your tenant entity
public class Order : IMultiTenant
{
    public int Id { get; set; }
    public string TenantId { get; set; }  // Automatically set on insert
    public decimal Total { get; set; }
}
```

---

## Specification Pattern

Expressive, reusable query specifications for clean architecture.

### Setup

```csharp
services.AddMvp24HoursReadOnlyRepositoryAsync(options =>
{
    options.MaxQtyByQueryPage = 100;
});
```

### Create a Specification

```csharp
public class ActiveCustomerSpecification : Specification<Customer>
{
    public ActiveCustomerSpecification()
    {
        // Filter
        AddCriteria(c => c.IsActive);
        
        // Ordering
        AddOrderBy(c => c.Name);
        
        // Include related data
        AddInclude(c => c.Orders);
    }
}
```

### Use with Repository

```csharp
public class CustomerQueryHandler
{
    private readonly IReadOnlyRepositoryAsync<Customer> _repository;

    public async Task<IList<Customer>> GetActiveCustomers()
    {
        var spec = new ActiveCustomerSpecification();
        return await _repository.GetBySpecificationAsync(spec);
    }

    public async Task<int> CountActiveCustomers()
    {
        var spec = new ActiveCustomerSpecification();
        return await _repository.CountBySpecificationAsync(spec);
    }

    public async Task<bool> HasActiveCustomers()
    {
        var spec = new ActiveCustomerSpecification();
        return await _repository.AnyBySpecificationAsync(spec);
    }
}
```

### Composing Specifications

```csharp
var activeSpec = new ActiveCustomerSpecification();
var premiumSpec = Specification<Customer>.Create(c => c.IsPremium);

// Combine with AND
var activePremiumSpec = activeSpec & premiumSpec;

// Combine with OR
var activeOrPremiumSpec = activeSpec | premiumSpec;

// Negate
var inactiveSpec = !activeSpec;
```

### Keyset (Cursor) Pagination

```csharp
// More efficient than OFFSET for large datasets
var page = await _repository.GetByKeysetPaginationAsync(
    clause: c => c.IsActive,
    keySelector: c => c.Id,
    lastKey: lastSeenId,      // null for first page
    pageSize: 20
);
```

---

## Resilience

Connection resiliency and circuit breaker patterns.

### Native Resilience (.NET 9+)

For .NET 9+, use `Microsoft.Extensions.Resilience` for native resilience patterns:

```csharp
// Program.cs
builder.Services.AddNativeDbResilience(options =>
{
    options.MaxRetryAttempts = 3;
    options.BaseDelay = TimeSpan.FromMilliseconds(100);
    options.UseExponentialBackoff = true;
    options.MaxDelay = TimeSpan.FromSeconds(30);
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});
```

> 📚 See [Native Resilience](../modernization/generic-resilience.md) for complete guide.

### Configure Resilience Options (Legacy)

```csharp
builder.Services.AddMvp24HoursDbContextWithResilience<AppDbContext>(options =>
{
    options.ConnectionString = connectionString;
    options.EnableRetryOnFailure = true;
    options.MaxRetryCount = 6;
    options.MaxRetryDelaySeconds = 30;
    options.CommandTimeoutSeconds = 60;
    options.EnableDbContextPooling = true;
    options.PoolSize = 1024;
});
```

### Preset Configurations

```csharp
// Production settings
var prodOptions = EFCoreResilienceOptions.Production();

// Development settings (more verbose logging)
var devOptions = EFCoreResilienceOptions.Development();

// Azure SQL optimized
var azureOptions = EFCoreResilienceOptions.AzureSql();

// No resilience (for testing)
var testOptions = EFCoreResilienceOptions.NoResilience();
```

### Circuit Breaker

```csharp
services.AddMvp24HoursDbContextCircuitBreaker(options =>
{
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 5;   // Opens after 5 failures
    options.CircuitBreakerDurationSeconds = 30;   // Stays open for 30s
});

// Usage in code
public class DataService
{
    private readonly DbContextCircuitBreaker _circuitBreaker;

    public async Task<List<Customer>> GetCustomers()
    {
        _circuitBreaker.EnsureCircuitClosed();  // Throws if circuit is open
        
        try
        {
            var result = await _dbContext.Customers.ToListAsync();
            _circuitBreaker.RecordSuccess();
            return result;
        }
        catch (Exception ex)
        {
            _circuitBreaker.RecordFailure();
            throw;
        }
    }
}
```

---

## Streaming

Memory-efficient data streaming with `IAsyncEnumerable`.

### Setup

```csharp
services.AddMvp24HoursStreamingRepositoryAsync(options =>
{
    options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
    options.StreamingBufferSize = 100;
});
```

### Usage

```csharp
public class ExportService
{
    private readonly IStreamingRepositoryAsync<Customer> _repository;

    public async Task ExportAllCustomers(StreamWriter writer)
    {
        // Streams data without loading everything into memory
        await foreach (var customer in _repository.StreamAllAsync())
        {
            await writer.WriteLineAsync($"{customer.Id},{customer.Name}");
        }
    }

    public async Task ProcessLargeDataset()
    {
        await foreach (var batch in _repository.StreamBatchesAsync(batchSize: 1000))
        {
            await ProcessBatchAsync(batch);
        }
    }
}
```

---

## Health Checks

Database health monitoring for Kubernetes and load balancers.

### Setup

```csharp
services.AddHealthChecks()
    // Generic DbContext check
    .AddMvp24HoursDbContextCheck<AppDbContext>("database", options =>
    {
        options.HealthQuery = "SELECT 1";
        options.CheckPendingMigrations = true;
        options.DegradedThresholdMs = 100;
        options.FailureThresholdMs = 500;
    })
    
    // SQL Server specific
    .AddMvp24HoursSqlServerCheck(connectionString, "sqlserver", options =>
    {
        options.CheckDatabaseState = true;
        options.CheckBlockingSessions = true;
    })
    
    // PostgreSQL specific
    .AddMvp24HoursPostgreSqlCheck<NpgsqlConnection>(connectionString, "postgresql", options =>
    {
        options.CheckConnectionUsage = true;
        options.CheckReplicationLag = true;
    })
    
    // MySQL specific
    .AddMvp24HoursMySqlCheck<MySqlConnection>(connectionString, "mysql", options =>
    {
        options.CheckConnectionUsage = true;
    });
```

### Liveness vs Readiness

```csharp
services.AddHealthChecks()
    // Liveness: Is the app alive? (minimal check)
    .AddMvp24HoursDbContextLivenessCheck<AppDbContext>()
    
    // Readiness: Is the app ready to serve traffic? (includes migrations check)
    .AddMvp24HoursDbContextReadinessCheck<AppDbContext>();
```

---

## Performance Optimization

### Read-Optimized Repository

```csharp
services.AddMvp24HoursReadOptimizedRepository(options =>
{
    options.MaxQtyByQueryPage = 200;
});

// Pre-configured with:
// - NoTracking by default
// - Split queries enabled
// - Query tags for profiling
```

### Write-Optimized Repository

```csharp
services.AddMvp24HoursWriteOptimizedRepository(options =>
{
    options.TransactionIsolationLevel = IsolationLevel.ReadCommitted;
});

// Pre-configured with:
// - Tracking enabled (required for updates)
// - Single queries
// - Minimal overhead
```

### Development Repository

```csharp
if (env.IsDevelopment())
{
    services.AddMvp24HoursDevRepository();
}

// Pre-configured with:
// - Detailed query tags
// - Sensitive data logging
// - Low slow query threshold (200ms)
```

### Query Tags

```csharp
var customers = await _dbContext.Customers
    .TagWith("GetActiveCustomers - CustomerService")
    .Where(c => c.IsActive)
    .ToListAsync();

// Output in SQL:
// -- GetActiveCustomers - CustomerService
// SELECT * FROM Customers WHERE IsActive = 1
```

---

## Testing

### In-Memory Testing

```csharp
public class CustomerServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    public CustomerServiceTests()
    {
        var services = new ServiceCollection();
        
        services.AddDbContext<DataContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
        });
        
        services.AddMvp24HoursRepositoryAsync();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateCustomer_ShouldSucceed()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Customer>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();

        var customer = new Customer { Name = "Test" };
        await repository.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();

        Assert.True(customer.Id > 0);
    }
}
```

### Repository Fake for Unit Tests

```csharp
public class CustomerServiceTests
{
    [Fact]
    public async Task GetActiveCustomers_ShouldReturnOnlyActive()
    {
        // Arrange
        var fakeRepository = new RepositoryFakeAsync<Customer>();
        fakeRepository.AddRange(new[]
        {
            new Customer { Id = 1, Name = "Active", IsActive = true },
            new Customer { Id = 2, Name = "Inactive", IsActive = false }
        });

        var service = new CustomerService(fakeRepository);

        // Act
        var result = await service.GetActiveCustomersAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }
}
```

### Data Seeder

```csharp
public class CustomerDataSeeder : IDataSeeder<Customer>
{
    public IEnumerable<Customer> Seed()
    {
        return new[]
        {
            new Customer { Name = "Customer 1", IsActive = true },
            new Customer { Name = "Customer 2", IsActive = true },
            new Customer { Name = "Customer 3", IsActive = false }
        };
    }
}

// Usage
services.AddTransient<IDataSeeder<Customer>, CustomerDataSeeder>();

// In test setup
var seeder = serviceProvider.GetRequiredService<IDataSeeder<Customer>>();
var customers = seeder.Seed();
await dbContext.Customers.AddRangeAsync(customers);
await dbContext.SaveChangesAsync();
```

---

## CQRS Integration

Complete setup for Command Query Responsibility Segregation.

```csharp
// Register both read and write repositories
builder.Services.AddMvp24HoursCqrsRepositories(options =>
{
    options.MaxQtyByQueryPage = 100;
});

// Query handler uses read-only repository
public class GetCustomerQueryHandler
{
    private readonly IReadOnlyRepositoryAsync<Customer> _repository;
    
    public async Task<Customer> Handle(GetCustomerQuery query)
    {
        return await _repository.GetByIdAsync(query.Id);
    }
}

// Command handler uses full repository with UnitOfWork
public class CreateCustomerCommandHandler
{
    private readonly IRepositoryAsync<Customer> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    
    public async Task Handle(CreateCustomerCommand command)
    {
        var customer = new Customer { Name = command.Name };
        await _repository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### Domain Events with SaveChanges

```csharp
// Use SaveChangesWithEventsAsync to dispatch domain events
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderResult>
{
    private readonly IRepositoryAsync<Order> _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    
    public async Task<OrderResult> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var order = new Order(command.CustomerId, command.Items);
        order.AddDomainEvent(new OrderCreatedEvent(order.Id));
        
        await _repository.AddAsync(order);
        
        // This dispatches domain events after commit
        await _unitOfWork.SaveChangesWithEventsAsync(ct);
        
        return new OrderResult(order.Id);
    }
}
```

> 📚 See [CQRS Documentation](../cqrs/home.md) for complete guide.

---

## See Also

- [Basic Database Configuration](relational.md)
- [Repository Pattern](use-repository.md)
- [Unit of Work](use-unitofwork.md)
- [CQRS Module](../cqrs/home.md)

