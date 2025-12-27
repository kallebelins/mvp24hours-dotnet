# EF Core - Funcionalidades Avançadas

> O módulo Mvp24Hours.Infrastructure.Data.EFCore fornece funcionalidades avançadas para aplicações enterprise incluindo interceptors, operações em lote, multi-tenancy, padrões de resiliência e observabilidade.

## Instalação

```bash
Install-Package Mvp24Hours.Infrastructure.Data.EFCore -Version 8.3.261
```

## Índice

- [Interceptors](#interceptors)
- [Operações em Lote (Bulk Operations)](#operações-em-lote-bulk-operations)
- [Multi-Tenancy](#multi-tenancy)
- [Specification Pattern](#specification-pattern)
- [Resiliência](#resiliência)
- [Streaming](#streaming)
- [Health Checks](#health-checks)
- [Otimização de Performance](#otimização-de-performance)
- [Testes](#testes)

---

## Interceptors

Interceptors do EF Core permitem modificar automaticamente o comportamento das entidades durante operações de SaveChanges.

### Audit Interceptor

Preenche automaticamente campos de auditoria (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy) em entidades que implementam `IAuditableEntity`.

```csharp
// Registrar em Startup.cs
services.AddDbContext<AppDbContext>((sp, options) =>
{
    var currentUserProvider = sp.GetService<ICurrentUserProvider>();
    var clock = sp.GetService<IClock>();
    
    options.UseSqlServer(connectionString)
           .AddInterceptors(new AuditSaveChangesInterceptor(currentUserProvider, clock));
});

// Sua entidade
public class Product : IAuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Estes campos são preenchidos automaticamente
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string ModifiedBy { get; set; }
}
```

### Soft Delete Interceptor

Converte exclusões físicas em soft deletes para entidades que implementam `ISoftDeletable`.

```csharp
// Registrar interceptor
services.AddDbContext<AppDbContext>((sp, options) =>
{
    var currentUserProvider = sp.GetService<ICurrentUserProvider>();
    var clock = sp.GetService<IClock>();
    
    options.UseSqlServer(connectionString)
           .AddInterceptors(new SoftDeleteInterceptor(currentUserProvider, clock));
});

// Configurar filtro global no DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplySoftDeleteGlobalFilter();
}

// Sua entidade
public class Customer : ISoftDeletable
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Estes campos são preenchidos automaticamente na exclusão
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; }
}
```

### Slow Query Interceptor

Registra queries que excedem um tempo limite para monitoramento de performance.

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new SlowQueryInterceptor(
               thresholdMs: 500,  // Registrar queries mais lentas que 500ms
               logger: loggerFactory.CreateLogger<SlowQueryInterceptor>()));
});
```

### Command Logging Interceptor

Registra todos os comandos SQL com parâmetros para depuração.

```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(new CommandLoggingInterceptor(
               loggerFactory.CreateLogger<CommandLoggingInterceptor>(),
               enableSensitiveDataLogging: true)); // Apenas em desenvolvimento!
});
```

---

## Operações em Lote (Bulk Operations)

Operações de alta performance para grandes volumes de dados.

### Configuração

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
                Console.WriteLine($"Progresso: {processed}/{total}")
        });
        
        Console.WriteLine($"Inseridos {result.RowsAffected} registros em {result.ElapsedTime}");
    }
}
```

### Bulk Update

```csharp
// Atualizar entidades existentes
var result = await _repository.BulkUpdateAsync(customersToUpdate, new BulkOperationOptions
{
    BatchSize = 1000
});
```

### Bulk Delete

```csharp
// Excluir por entidades
var result = await _repository.BulkDeleteAsync(customersToDelete);
```

### ExecuteUpdate e ExecuteDelete (.NET 7+)

```csharp
// Atualizar todos os registros correspondentes em uma única query (sem carregar entidades)
var rowsUpdated = await _repository.ExecuteUpdateAsync(
    c => c.IsActive == false,           // Filtro
    c => c.Status,                       // Propriedade a atualizar
    "Inactive"                           // Novo valor
);

// Excluir todos os registros correspondentes em uma única query
var rowsDeleted = await _repository.ExecuteDeleteAsync(
    c => c.CreatedAt < DateTime.UtcNow.AddYears(-5)  // Excluir registros antigos
);
```

### BulkOperationOptions

| Propriedade | Descrição | Padrão |
|-------------|-----------|--------|
| BatchSize | Número de registros por lote | 1000 |
| UseTransaction | Envolver em transação | true |
| TimeoutSeconds | Timeout da operação | 300 |
| ProgressCallback | Callback de progresso | null |
| BypassChangeTracking | Ignorar change tracking do EF | true |

---

## Multi-Tenancy

Isolamento automático de tenant para aplicações SaaS.

### Configuração

```csharp
// Registrar tenant provider
services.AddMvp24HoursMultiTenancy<HttpHeaderTenantProvider>(options =>
{
    options.RequireTenant = true;
    options.ValidateTenantOnModify = true;
});

// Registrar DbContext com interceptor de tenant
services.AddDbContext<AppDbContext>((sp, options) =>
{
    var tenantInterceptor = sp.GetRequiredService<TenantSaveChangesInterceptor>();
    options.UseSqlServer(connectionString)
           .AddInterceptors(tenantInterceptor);
});
```

### Tenant Provider Customizado

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

### Configurar Query Filters

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Aplicar filtro de tenant a todas as entidades multi-tenant
    modelBuilder.ApplyTenantQueryFilters<IMultiTenant>(_tenantProvider);
}

// Sua entidade multi-tenant
public class Order : IMultiTenant
{
    public int Id { get; set; }
    public string TenantId { get; set; }  // Definido automaticamente no insert
    public decimal Total { get; set; }
}
```

---

## Specification Pattern

Especificações de query expressivas e reutilizáveis para arquitetura limpa.

### Configuração

```csharp
services.AddMvp24HoursReadOnlyRepositoryAsync(options =>
{
    options.MaxQtyByQueryPage = 100;
});
```

### Criar uma Especificação

```csharp
public class ActiveCustomerSpecification : Specification<Customer>
{
    public ActiveCustomerSpecification()
    {
        // Filtro
        AddCriteria(c => c.IsActive);
        
        // Ordenação
        AddOrderBy(c => c.Name);
        
        // Incluir dados relacionados
        AddInclude(c => c.Orders);
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

### Compondo Especificações

```csharp
var activeSpec = new ActiveCustomerSpecification();
var premiumSpec = Specification<Customer>.Create(c => c.IsPremium);

// Combinar com AND
var activePremiumSpec = activeSpec & premiumSpec;

// Combinar com OR
var activeOrPremiumSpec = activeSpec | premiumSpec;

// Negar
var inactiveSpec = !activeSpec;
```

### Paginação por Keyset (Cursor)

```csharp
// Mais eficiente que OFFSET para grandes datasets
var page = await _repository.GetByKeysetPaginationAsync(
    clause: c => c.IsActive,
    keySelector: c => c.Id,
    lastKey: lastSeenId,      // null para primeira página
    pageSize: 20
);
```

---

## Resiliência

Resiliência de conexão e padrões de circuit breaker.

### Configurar Opções de Resiliência

```csharp
services.AddMvp24HoursDbContextWithResilience<AppDbContext>(options =>
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

### Configurações Pré-definidas

```csharp
// Configurações de produção
var prodOptions = EFCoreResilienceOptions.Production();

// Configurações de desenvolvimento (log mais verboso)
var devOptions = EFCoreResilienceOptions.Development();

// Otimizado para Azure SQL
var azureOptions = EFCoreResilienceOptions.AzureSql();

// Sem resiliência (para testes)
var testOptions = EFCoreResilienceOptions.NoResilience();
```

### Circuit Breaker

```csharp
services.AddMvp24HoursDbContextCircuitBreaker(options =>
{
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 5;   // Abre após 5 falhas
    options.CircuitBreakerDurationSeconds = 30;   // Permanece aberto por 30s
});

// Uso no código
public class DataService
{
    private readonly DbContextCircuitBreaker _circuitBreaker;

    public async Task<List<Customer>> GetCustomers()
    {
        _circuitBreaker.EnsureCircuitClosed();  // Lança exceção se circuito estiver aberto
        
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

Streaming de dados eficiente em memória com `IAsyncEnumerable`.

### Configuração

```csharp
services.AddMvp24HoursStreamingRepositoryAsync(options =>
{
    options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
    options.StreamingBufferSize = 100;
});
```

### Uso

```csharp
public class ExportService
{
    private readonly IStreamingRepositoryAsync<Customer> _repository;

    public async Task ExportAllCustomers(StreamWriter writer)
    {
        // Transmite dados sem carregar tudo na memória
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

Monitoramento de saúde do banco de dados para Kubernetes e load balancers.

### Configuração

```csharp
services.AddHealthChecks()
    // Check genérico de DbContext
    .AddMvp24HoursDbContextCheck<AppDbContext>("database", options =>
    {
        options.HealthQuery = "SELECT 1";
        options.CheckPendingMigrations = true;
        options.DegradedThresholdMs = 100;
        options.FailureThresholdMs = 500;
    })
    
    // Específico para SQL Server
    .AddMvp24HoursSqlServerCheck(connectionString, "sqlserver", options =>
    {
        options.CheckDatabaseState = true;
        options.CheckBlockingSessions = true;
    })
    
    // Específico para PostgreSQL
    .AddMvp24HoursPostgreSqlCheck<NpgsqlConnection>(connectionString, "postgresql", options =>
    {
        options.CheckConnectionUsage = true;
        options.CheckReplicationLag = true;
    })
    
    // Específico para MySQL
    .AddMvp24HoursMySqlCheck<MySqlConnection>(connectionString, "mysql", options =>
    {
        options.CheckConnectionUsage = true;
    });
```

### Liveness vs Readiness

```csharp
services.AddHealthChecks()
    // Liveness: A aplicação está viva? (check mínimo)
    .AddMvp24HoursDbContextLivenessCheck<AppDbContext>()
    
    // Readiness: A aplicação está pronta para receber tráfego? (inclui check de migrations)
    .AddMvp24HoursDbContextReadinessCheck<AppDbContext>();
```

---

## Otimização de Performance

### Repository Otimizado para Leitura

```csharp
services.AddMvp24HoursReadOptimizedRepository(options =>
{
    options.MaxQtyByQueryPage = 200;
});

// Pré-configurado com:
// - NoTracking por padrão
// - Split queries habilitadas
// - Query tags para profiling
```

### Repository Otimizado para Escrita

```csharp
services.AddMvp24HoursWriteOptimizedRepository(options =>
{
    options.TransactionIsolationLevel = IsolationLevel.ReadCommitted;
});

// Pré-configurado com:
// - Tracking habilitado (necessário para updates)
// - Single queries
// - Overhead mínimo
```

### Repository para Desenvolvimento

```csharp
if (env.IsDevelopment())
{
    services.AddMvp24HoursDevRepository();
}

// Pré-configurado com:
// - Query tags detalhadas
// - Logging de dados sensíveis
// - Threshold baixo para slow queries (200ms)
```

### Query Tags

```csharp
var customers = await _dbContext.Customers
    .TagWith("GetActiveCustomers - CustomerService")
    .Where(c => c.IsActive)
    .ToListAsync();

// Output no SQL:
// -- GetActiveCustomers - CustomerService
// SELECT * FROM Customers WHERE IsActive = 1
```

---

## Testes

### Testes In-Memory

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

### Repository Fake para Testes Unitários

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

// Uso
services.AddTransient<IDataSeeder<Customer>, CustomerDataSeeder>();

// No setup do teste
var seeder = serviceProvider.GetRequiredService<IDataSeeder<Customer>>();
var customers = seeder.Seed();
await dbContext.Customers.AddRangeAsync(customers);
await dbContext.SaveChangesAsync();
```

---

## Integração CQRS

Configuração completa para Command Query Responsibility Segregation.

```csharp
// Registrar repositórios de leitura e escrita
services.AddMvp24HoursCqrsRepositories(options =>
{
    options.MaxQtyByQueryPage = 100;
});

// Query handler usa repositório somente leitura
public class GetCustomerQueryHandler
{
    private readonly IReadOnlyRepositoryAsync<Customer> _repository;
    
    public async Task<Customer> Handle(GetCustomerQuery query)
    {
        return await _repository.GetByIdAsync(query.Id);
    }
}

// Command handler usa repositório completo com UnitOfWork
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

---

## Veja Também

- [Configuração Básica de Banco de Dados](relational.md)
- [Padrão Repository](use-repository.md)
- [Unit of Work](use-unitofwork.md)
- [Módulo CQRS](../cqrs/home.md)

