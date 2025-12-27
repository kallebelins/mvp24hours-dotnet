# MongoDB - Funcionalidades Avançadas

> O módulo Mvp24Hours.Infrastructure.Data.MongoDb fornece funcionalidades avançadas para aplicações enterprise incluindo interceptors, operações em lote, multi-tenancy, padrões de resiliência, observabilidade e recursos específicos do MongoDB como GridFS, Change Streams e queries geoespaciais.

## Instalação

```bash
Install-Package MongoDB.Driver -Version 2.13.2
Install-Package Mvp24Hours.Infrastructure.Data.MongoDb -Version 8.3.261
```

## Índice

- [Interceptors](#interceptors)
- [Operações em Lote (Bulk Operations)](#operações-em-lote-bulk-operations)
- [Multi-Tenancy](#multi-tenancy)
- [Specification Pattern](#specification-pattern)
- [Resiliência](#resiliência)
- [Health Checks](#health-checks)
- [Observabilidade](#observabilidade)
- [Funcionalidades Avançadas do MongoDB](#funcionalidades-avançadas-do-mongodb)
- [Testes](#testes)

---

## Interceptors

Interceptors do MongoDB permitem modificação automática de documentos durante operações CRUD.

### Configuração com Interceptors

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

Preenche automaticamente campos de auditoria em entidades que implementam `IAuditableEntity`.

```csharp
// Registrar interceptor de auditoria
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbAuditInterceptor();

// Sua entidade
public class Product : IAuditableEntity
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    
    // Preenchidos automaticamente
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}
```

### Soft Delete Interceptor

Converte exclusões físicas em soft deletes.

```csharp
// Registrar interceptor de soft delete
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbSoftDeleteInterceptor();

// Sua entidade
public class Customer : ISoftDeletable
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    
    // Preenchidos na exclusão
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; }
}
```

### Tenant Interceptor

Gerencia automaticamente TenantId para aplicações multi-tenant.

```csharp
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbTenantInterceptor();
```

### Command Logger

Registra todos os comandos MongoDB para depuração.

```csharp
services.AddMvp24HoursRepositoryAsyncWithInterceptors()
        .AddMongoDbCommandLogger(options =>
        {
            options.LogLevel = LogLevel.Debug;
            options.IncludeDocuments = true;  // Apenas em desenvolvimento!
        });
```

---

## Operações em Lote (Bulk Operations)

Operações de alta performance para grandes volumes de dados.

### Configuração

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
            IsOrdered = false,  // Não ordenado para melhor performance
            ProgressCallback = (processed, total) => 
                Console.WriteLine($"Progresso: {processed}/{total}")
        });
        
        Console.WriteLine($"Inseridos {result.InsertedCount} documentos em {result.ElapsedTime}");
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

Isolamento automático de tenant para aplicações SaaS.

### Configuração

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

### Tenant Provider Customizado

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

Especificações de query expressivas e reutilizáveis.

### Criar uma Especificação

```csharp
public class ActiveCustomerSpecification : MongoDbSpecification<Customer>
{
    public ActiveCustomerSpecification()
    {
        // Filtro
        AddCriteria(c => c.IsActive);
        
        // Ordenação
        AddOrderBy(c => c.Name);
        
        // Paginação
        ApplyPaging(skip: 0, take: 50);
    }
}
```

### Usar com Repository

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

## Resiliência

Resiliência de conexão e padrões de circuit breaker.

### Configurar Opções de Resiliência

```csharp
services.AddMvp24HoursDbContext(options =>
{
    options.DatabaseName = "MyDatabase";
    options.ConnectionString = connectionString;
})
.AddMvp24HoursMongoDbResiliency(options =>
{
    // Política de retry
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

O circuit breaker previne falhas em cascata bloqueando temporariamente requisições quando o banco está com problemas.

```csharp
// Estados do circuit breaker:
// - Closed: Operação normal, requisições passam
// - Open: Muitas falhas, requisições falham imediatamente
// - HalfOpen: Testando se o serviço recuperou

// Controle manual do circuit breaker
var circuitBreaker = serviceProvider.GetRequiredService<IMongoDbCircuitBreaker>();

// Verificar estado
if (circuitBreaker.AllowRequest())
{
    try
    {
        // Executar operação
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
    throw new CircuitBreakerOpenException("Circuit breaker do banco está aberto");
}

// Trip e reset manual
circuitBreaker.Trip();        // Forçar abertura
circuitBreaker.ResetState();  // Resetar para fechado

// Obter estatísticas
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

### Política de Retry

```csharp
// Configurar política de retry
services.AddMvp24HoursMongoDbRetryPolicy(options =>
{
    options.MaxRetryAttempts = 5;
    options.UseExponentialBackoff = true;
    options.RetryDelayMs = 100;
    options.MaxRetryDelayMs = 5000;
    
    // Retry apenas para exceções específicas
    options.RetryableExceptions.Add(typeof(MongoConnectionException));
    options.RetryableExceptions.Add(typeof(MongoNotPrimaryException));
});
```

---

## Health Checks

Monitoramento de saúde do banco para Kubernetes e load balancers.

### Configuração

```csharp
services.AddHealthChecks()
    // Health check básico do MongoDB
    .AddMvp24HoursMongoDbCheck(connectionString, "mongodb", options =>
    {
        options.HealthQuery = "{ ping: 1 }";
        options.DegradedThresholdMs = 100;
        options.FailureThresholdMs = 500;
    })
    
    // Health check de replica set
    .AddMvp24HoursMongoDbReplicaSetCheck(connectionString, "mongodb-replicaset", options =>
    {
        options.CheckReplicationLag = true;
        options.MaxReplicationLagSeconds = 10;
    });
```

---

## Observabilidade

### Integração com OpenTelemetry

```csharp
services.AddMvp24HoursMongoDbObservability(options =>
{
    options.EnableTracing = true;
    options.EnableMetrics = true;
    options.IncludeCommandText = false;  // true apenas em desenvolvimento
    options.ActivitySourceName = "Mvp24Hours.MongoDB";
});
```

### Slow Query Logger

```csharp
services.AddMvp24HoursMongoDbSlowQueryLogger(options =>
{
    options.ThresholdMs = 500;  // Registrar queries mais lentas que 500ms
    options.IncludeStackTrace = true;
    options.LogLevel = LogLevel.Warning;
});
```

### Métricas

```csharp
// Métricas do connection pool
services.AddMvp24HoursMongoDbConnectionPoolMetrics();

// Acessar métricas
var metrics = serviceProvider.GetRequiredService<IMongoDbMetrics>();
var poolStats = metrics.GetConnectionPoolStats();

Console.WriteLine($"Conexões ativas: {poolStats.ActiveConnections}");
Console.WriteLine($"Conexões disponíveis: {poolStats.AvailableConnections}");
Console.WriteLine($"Requisições aguardando: {poolStats.WaitingRequests}");
```

---

## Funcionalidades Avançadas do MongoDB

### GridFS (Armazenamento de Arquivos Grandes)

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbGridFS(options =>
        {
            options.BucketName = "files";
            options.ChunkSizeBytes = 261120;  // 255KB
        });

// Uso
var gridFsService = serviceProvider.GetRequiredService<IMongoDbGridFSService>();

// Upload de arquivo
var fileId = await gridFsService.UploadAsync(fileStream, "document.pdf", new GridFSMetadata
{
    ContentType = "application/pdf",
    CustomData = new { UserId = "user123" }
});

// Download de arquivo
using var downloadStream = await gridFsService.DownloadAsync(fileId);

// Excluir arquivo
await gridFsService.DeleteAsync(fileId);
```

### Change Streams (Eventos em Tempo Real)

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbChangeStreams<Customer>();

// Uso
var changeStreamService = serviceProvider.GetRequiredService<IMongoDbChangeStreamService<Customer>>();

// Observar mudanças
await foreach (var change in changeStreamService.WatchAsync())
{
    switch (change.OperationType)
    {
        case ChangeStreamOperationType.Insert:
            Console.WriteLine($"Novo cliente: {change.FullDocument.Name}");
            break;
        case ChangeStreamOperationType.Update:
            Console.WriteLine($"Cliente atualizado: {change.DocumentKey}");
            break;
        case ChangeStreamOperationType.Delete:
            Console.WriteLine($"Cliente excluído: {change.DocumentKey}");
            break;
    }
}
```

### Queries Geoespaciais

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbGeospatial();

// Criar índice geoespacial
await indexService.CreateGeoIndex<Store>(s => s.Location);

// Sua entidade com localização
public class Store
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Name { get; set; }
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
}

// Buscar lojas próximas a um ponto
var geoService = serviceProvider.GetRequiredService<IMongoDbGeospatialService>();
var nearbyStores = await geoService.FindNearAsync<Store>(
    longitude: -46.6333,
    latitude: -23.5505,
    maxDistanceMeters: 5000
);

// Buscar lojas dentro de um polígono
var storesInArea = await geoService.FindWithinPolygonAsync<Store>(polygonCoordinates);
```

### Pesquisa de Texto (Text Search)

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbTextSearch();

// Criar índice de texto
await indexService.CreateTextIndexAsync<Product>(
    p => p.Name,
    p => p.Description,
    options: new TextIndexOptions
    {
        DefaultLanguage = "portuguese",
        Weights = new { Name = 10, Description = 5 }
    }
);

// Pesquisar
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
            options.ExpireAfterSeconds = 86400 * 30;  // 30 dias
        });

// Seu documento de série temporal
public class SensorReading
{
    public DateTime Timestamp { get; set; }
    public SensorMetadata Metadata { get; set; }
    public double Value { get; set; }
}
```

### Transações

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbTransactions();

// Uso
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

### Validação de Schema

```csharp
services.AddMvp24HoursMongoDbAdvanced()
        .AddMongoDbSchemaValidation<Customer>(options =>
        {
            options.ValidationLevel = DocumentValidationLevel.Strict;
            options.ValidationAction = DocumentValidationAction.Error;
        });
```

---

## Testes

### Provider In-Memory para Testes Unitários

```csharp
public class CustomerServiceTests
{
    private readonly IServiceProvider _serviceProvider;

    public CustomerServiceTests()
    {
        var services = new ServiceCollection();
        
        // Usar provider in-memory do MongoDB
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

## Veja Também

- [Configuração Básica de NoSQL](nosql.md)
- [Padrão Repository](use-repository.md)
- [Unit of Work](use-unitofwork.md)
- [Módulo CQRS](../cqrs/home.md)

