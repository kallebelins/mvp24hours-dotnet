# MongoDB Advanced Features

> The Mvp24Hours.Infrastructure.Data.MongoDb module provides advanced features for enterprise applications including interceptors, bulk operations, multi-tenancy, resilience patterns, observability, and MongoDB-specific features like GridFS, Change Streams, and Geospatial queries.

## Installation

```bash
Install-Package MongoDB.Driver -Version 2.28.0
Install-Package Mvp24Hours.Infrastructure.Data.MongoDb -Version 9.1.x
```

## Table of Contents

- [Interceptors](#interceptors)
- [Bulk Operations](#bulk-operations)
- [Multi-Tenancy](#multi-tenancy)
- [Specification Pattern](#specification-pattern)
- [Resilience](#resilience)
- [Health Checks](#health-checks)
- [Observability](#observability)
- [Advanced MongoDB Features](#advanced-mongodb-features)
- [Testing](#testing)

---

## Interceptors

MongoDB interceptors allow automatic modification of documents during CRUD operations.

### Setup with Interceptors

```csharp
services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "MyDatabase";
    options.ConnectionString = "mongodb://localhost:27017";
})
.AddMvp24HoursRepositoryAsyncWithInterceptors()
.AddAllMongoDbInterceptors(options =>
{
    options.EnableAuditInterceptor = true;
    options.EnableSoftDelete = true;
    options.EnableCommandLogging = true;
});
```

### Audit Interceptor

Automatically populates audit fields on entities implementing `IAuditableEntity`.

```csharp
// Register audit interceptor
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbAuditInterceptor();

// Your entity
public class Product : IAuditableEntity
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    
    // Automatically populated
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}
```

### Soft Delete Interceptor

Converts physical deletes to soft deletes.

```csharp
// Register soft delete interceptor
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbSoftDeleteInterceptor();

// Your entity
public class Customer : ISoftDeletable
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    
    // Populated on delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; }
}
```

### Tenant Interceptor

Automatically manages TenantId for multi-tenant applications.

```csharp
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbTenantInterceptor();
```

### Command Logger

Logs all MongoDB commands for debugging.

```csharp
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbCommandLogger(options =>
        {
            options.LogLevel = LogLevel.Debug;
            options.IncludeDocuments = true;  // Only in development!
        });
```

---

## Bulk Operations

High-performance bulk operations for large datasets.

### Setup

```csharp
services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "MyDatabase";
    options.ConnectionString = connectionString;
})
.AddMvp24HoursBulkOperationsRepositoryAsync();
```

### Bulk Insert

```csharp
public class ImportService
{
    private readonly IBulkOperationsRepositoryAsync<Customer> _repository;

    public async Task ImportCustomers(IList<Customer> customers)
    {
        var result = await _repository.BulkInsertAsync(customers, new MongoDbBulkOperationOptions
        {
            BatchSize = 5000,
            IsOrdered = false,  // Unordered for better performance
            ProgressCallback = (processed, total) => 
                Console.WriteLine($"Progress: {processed}/{total}")
        });
        
        Console.WriteLine($"Inserted {result.InsertedCount} documents in {result.ElapsedTime}");
    }
}
```

### Bulk Update

```csharp
var result = await _repository.BulkUpdateAsync(customersToUpdate, new MongoDbBulkOperationOptions
{
    BatchSize = 1000,
    BypassDocumentValidation = false
});
```

### Bulk Delete

```csharp
var result = await _repository.BulkDeleteAsync(customersToDelete);
```

---

## Multi-Tenancy

Automatic tenant isolation for SaaS applications.

### Setup

```csharp
services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "MyDatabase";
    options.ConnectionString = connectionString;
})
.AddMvp24HoursRepositoryAsyncWithInterceptors()
.AddMongoDbMultiTenancy<HttpHeaderTenantProvider>(options =>
{
    options.RequireTenant = true;
    options.TenantIdField = "TenantId";
});
```

### Custom Tenant Provider

```csharp
public class HttpHeaderTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public string TenantId => 
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

    public bool HasTenant => !string.IsNullOrEmpty(TenantId);
}
```

---

## Specification Pattern

Expressive, reusable query specifications.

### Create a Specification

```csharp
public class ActiveCustomerSpecification : MongoDbSpecification<Customer>
{
    public ActiveCustomerSpecification()
    {
        // Filter
        AddCriteria(c => c.IsActive);
        
        // Sorting
        AddOrderBy(c => c.Name);
        
        // Pagination
        ApplyPaging(skip: 0, take: 50);
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
}
```

---

## Resilience

Connection resiliency and circuit breaker patterns.

### Native Resilience (.NET 9+)

For .NET 9+, use `Microsoft.Extensions.Resilience` for native resilience patterns:

```csharp
// Program.cs
builder.Services.AddNativeMongoDbResilience(options =>
{
    options.MaxRetryAttempts = 3;
    options.BaseDelay = TimeSpan.FromMilliseconds(100);
    options.UseExponentialBackoff = true;
    options.MaxDelay = TimeSpan.FromSeconds(30);
});

builder.Services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "MyDatabase";
    options.ConnectionString = connectionString;
});
```

> 📚 See [Native Resilience](../modernization/generic-resilience.md) for complete guide.

### Configure Resilience Options (Legacy)

```csharp
builder.Services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "MyDatabase";
    options.ConnectionString = connectionString;
})
.AddMvp24HoursMongoDbResiliency(options =>
{
    // Retry policy
    options.EnableRetry = true;
    options.MaxRetryAttempts = 5;
    options.RetryDelayMs = 200;
    options.MaxRetryDelayMs = 5000;
    
    // Circuit breaker
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 5;
    options.CircuitBreakerDurationSeconds = 30;
    options.CircuitBreakerSamplingDurationSeconds = 60;
    options.CircuitBreakerMinimumThroughput = 10;
    
    // Timeouts
    options.ConnectionTimeoutMs = 30000;
    options.SocketTimeoutMs = 30000;
    options.ServerSelectionTimeoutMs = 30000;
});
```

### Circuit Breaker

The circuit breaker prevents cascade failures by temporarily blocking requests when the database is experiencing issues.

```csharp
// Circuit breaker states:
// - Closed: Normal operation, requests pass through
// - Open: Too many failures, requests fail immediately
// - HalfOpen: Testing if service recovered

// Manual circuit breaker control
var circuitBreaker = serviceProvider.GetRequiredService<IMongoDbCircuitBreaker>();

// Check state
if (circuitBreaker.AllowRequest())
{
    try
    {
        // Execute operation
        circuitBreaker.RecordSuccess();
    }
    catch (Exception ex)
    {
        circuitBreaker.RecordFailure(ex);
        throw;
    }
}
else
{
    throw new CircuitBreakerOpenException("Database circuit breaker is open");
}

// Manual trip and reset
circuitBreaker.Trip();     // Force open
circuitBreaker.ResetState();  // Reset to closed

// Get statistics
var stats = new
{
    State = circuitBreaker.State,
    SuccessCount = circuitBreaker.TotalSuccessCount,
    FailureCount = circuitBreaker.TotalFailureCount,
    RejectedCount = circuitBreaker.TotalRejectedCount,
    FailureRate = circuitBreaker.CurrentFailureRate,
    TripCount = circuitBreaker.CircuitTripCount,
    RemainingDuration = circuitBreaker.GetRemainingOpenDuration()
};
```

### Retry Policy

```csharp
// Configure retry policy
services.AddMvp24HoursMongoDbRetryPolicy(options =>
{
    options.MaxRetryAttempts = 5;
    options.UseExponentialBackoff = true;
    options.RetryDelayMs = 100;
    options.MaxRetryDelayMs = 5000;
    
    // Only retry specific exceptions
    options.RetryableExceptions.Add(typeof(MongoConnectionException));
    options.RetryableExceptions.Add(typeof(MongoNotPrimaryException));
});
```

---

## Health Checks

Database health monitoring for Kubernetes and load balancers.

### Setup

```csharp
services.AddHealthChecks()
    // Basic MongoDB health check
    .AddMvp24HoursMongoDbCheck(connectionString, "mongodb", options =>
    {
        options.HealthQuery = "{ ping: 1 }";
        options.DegradedThresholdMs = 100;
        options.FailureThresholdMs = 500;
    })
    
    // Replica set health check
    .AddMvp24HoursMongoDbReplicaSetCheck(connectionString, "mongodb-replicaset", options =>
    {
        options.CheckReplicationLag = true;
        options.MaxReplicationLagSeconds = 10;
    });
```

---

## Observability

### OpenTelemetry Integration

```csharp
services.AddMvp24HoursMongoDbObservability(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.IncludeCommandText = false;  // Set true only in development
    options.ActivitySourceName = "Mvp24Hours.MongoDB";
});
```

### Slow Query Logger

```csharp
services.AddMvp24HoursMongoDbSlowQueryLogger(options =>
{
    options.ThresholdMs = 500;  // Log queries slower than 500ms
    options.IncludeStackTrace = true;
    options.LogLevel = LogLevel.Warning;
});
```

### Metrics

```csharp
// Connection pool metrics
services.AddMvp24HoursMongoDbConnectionPoolMetrics();

// Access metrics
var metrics = serviceProvider.GetRequiredService<IMongoDbMetrics>();
var poolStats = metrics.GetConnectionPoolStats();

Console.WriteLine($"Active connections: {poolStats.ActiveConnections}");
Console.WriteLine($"Available connections: {poolStats.AvailableConnections}");
Console.WriteLine($"Waiting requests: {poolStats.WaitingRequests}");
```

---

## Advanced MongoDB Features

### GridFS (Large File Storage)

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbGridFS(options =>
        {
            options.BucketName = "files";
            options.ChunkSizeBytes = 261120;  // 255KB
        });

// Usage
var gridFsService = serviceProvider.GetRequiredService<IMongoDbGridFSService>();

// Upload file
var fileId = await gridFsService.UploadAsync(fileStream, "document.pdf", new GridFSMetadata
{
    ContentType = "application/pdf",
    CustomData = new { UserId = "user123" }
});

// Download file
using var downloadStream = await gridFsService.DownloadAsync(fileId);

// Delete file
await gridFsService.DeleteAsync(fileId);
```

### Change Streams (Real-time Events)

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbChangeStreams<Customer>();

// Usage
var changeStreamService = serviceProvider.GetRequiredService<IMongoDbChangeStreamService<Customer>>();

// Watch for changes
await foreach (var change in changeStreamService.WatchAsync())
{
    switch (change.OperationType)
    {
        case ChangeStreamOperationType.Insert:
            Console.WriteLine($"New customer: {change.FullDocument.Name}");
            break;
        case ChangeStreamOperationType.Update:
            Console.WriteLine($"Updated customer: {change.DocumentKey}");
            break;
        case ChangeStreamOperationType.Delete:
            Console.WriteLine($"Deleted customer: {change.DocumentKey}");
            break;
    }
}
```

### Geospatial Queries

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbGeospatial();

// Create geospatial index
await indexService.CreateGeoIndex<Store>(s => s.Location);

// Your entity with location
public class Store
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
}

// Find stores near a point
var geoService = serviceProvider.GetRequiredService<IMongoDbGeospatialService>();
var nearbyStores = await geoService.FindNearAsync<Store>(
    longitude: -46.6333,
    latitude: -23.5505,
    maxDistanceMeters: 5000
);

// Find stores within polygon
var storesInArea = await geoService.FindWithinPolygonAsync<Store>(polygonCoordinates);
```

### Text Search

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbTextSearch();

// Create text index
await indexService.CreateTextIndexAsync<Product>(
    p => p.Name,
    p => p.Description,
    options: new TextIndexOptions
    {
        DefaultLanguage = "portuguese",
        Weights = new { Name = 10, Description = 5 }
    }
);

// Search
var textSearchService = serviceProvider.GetRequiredService<IMongoDbTextSearchService>();
var results = await textSearchService.SearchAsync<Product>("smartphone premium");
```

### Time Series Collections

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbTimeSeries<SensorReading>(options =>
        {
            options.TimeField = "timestamp";
            options.MetaField = "metadata";
            options.Granularity = TimeSeriesGranularity.Seconds;
            options.ExpireAfterSeconds = 86400 * 30;  // 30 days
        });

// Your time series document
public class SensorReading
{
    public DateTime Timestamp { get; set; }
    public SensorMetadata Metadata { get; set; }
    public double Value { get; set; }
}
```

### Transactions

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbTransactions();

// Usage
var transactionService = serviceProvider.GetRequiredService<IMongoDbTransactionService>();

await transactionService.ExecuteAsync(async session =>
{
    await _orderRepository.AddAsync(order, session);
    await _inventoryRepository.UpdateAsync(inventory, session);
    await _paymentRepository.AddAsync(payment, session);
});
```

### Capped Collections

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbCappedCollection<LogEntry>(options =>
        {
            options.MaxSize = 1024 * 1024 * 100;  // 100MB
            options.MaxDocuments = 100000;
        });
```

### Schema Validation

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbSchemaValidation<Customer>(options =>
        {
            options.ValidationLevel = DocumentValidationLevel.Strict;
            options.ValidationAction = DocumentValidationAction.Error;
        });
```

---

## Testing

### In-Memory Provider for Unit Tests

```csharp
public class CustomerServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    public CustomerServiceTests()
    {
        var services = new ServiceCollection();
        
        // Use in-memory MongoDB provider
        services.AddMvp24HoursMongoDbInMemory("TestDb");
        services.AddMvp24HoursRepositoryAsync();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task CreateCustomer_ShouldSucceed()
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<Customer>>();

        var customer = new Customer { Name = "Test" };
        await repository.AddAsync(customer);

        var saved = await repository.GetByIdAsync(customer.Id);
        Assert.NotNull(saved);
    }
}
```

### Repository Fake

```csharp
public class CustomerServiceTests
{
    [Fact]
    public async Task GetActiveCustomers_ShouldReturnOnlyActive()
    {
        // Arrange
        var fakeRepository = new MongoDbRepositoryFakeAsync<Customer>();
        fakeRepository.AddRange(new[]
        {
            new Customer { Id = ObjectId.GenerateNewId(), Name = "Active", IsActive = true },
            new Customer { Id = ObjectId.GenerateNewId(), Name = "Inactive", IsActive = false }
        });

        var service = new CustomerService(fakeRepository);

        // Act
        var result = await service.GetActiveCustomersAsync();

        // Assert
        Assert.Single(result);
    }
}
```

### Testcontainers

```csharp
public class IntegrationTests : IAsyncLifetime
{
    private MongoDbContainer _container;
    private IServiceProvider _serviceProvider;

    public async Task InitializeAsync()
    {
        _container = new MongoDbBuilder()
            .WithImage("mongo:6.0")
            .Build();
        
        await _container.StartAsync();
        
        var services = new ServiceCollection();
        services.AddMvp24HoursDbContext(options =>
        {
            options.ConnectionString = _container.GetConnectionString();
            options.DatabaseName = "TestDb";
        });
        services.AddMvp24HoursRepositoryAsync();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

---

## See Also

- [Basic NoSQL Configuration](nosql.md)
- [Repository Pattern](use-repository.md)
- [Unit of Work](use-unitofwork.md)
- [CQRS Module](../cqrs/home.md)

