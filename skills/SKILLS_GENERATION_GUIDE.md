# Skills Generation Guide - Remaining Skills

> **Purpose**: Historical outlines used to generate specialist skills  
> **Status**: Catalog complete (35/35). Use this file only when revising a skill.  
> **Approach**: Follow SKILL_TEMPLATE.md and verify APIs via MCP (`get_doc`, `find_source_symbol`, `get_sample_tree`)

## Quick Reference

### Completed Skills (35)

Architecture: `demand-architect`, `solution-architect`, `clean-architecture-specialist`, `ddd-specialist`, `hexagonal-specialist`, `event-driven-specialist`, `microservices-specialist`

Data: `data-architect`, `efcore-specialist`, `dapper-specialist`, `mongodb-specialist`, `redis-specialist`

Messaging: `messaging-architect`, `rabbitmq-advanced-specialist`, `saga-orchestration-specialist`

CQRS: `cqrs-architect`, `event-sourcing-specialist`, `mediator-patterns-specialist`

Observability: `observability-architect`, `resilience-patterns-specialist`

Other: `pipeline-architect`, `caching-architect`, `infrastructure-architect`, `webapi-architect`, `api-contract-architect`, `testing-architect`, `identity-architect`, `security-architect`, `cronjob-architect`, `integration-architect`

Modernization: `architecture-analyst`, `architecture-proposal-architect`, `port-transpilation-specialist`, `architecture-rewrite-architect`, `dotnet-modernization-specialist`

See [COMPLETION_STATUS.md](COMPLETION_STATUS.md) and [README.md](README.md).

---

## Universal Template (All Skills)

Every skill follows this structure:

```markdown
# [Skill Name] - Mvp24Hours [Architect|Specialist]
> **Role**: [One sentence]
> **MCP Integration**: Query [docs/templates/samples] via MCP DevKit

## Role & Expertise
[2-3 paragraphs]

### Core Responsibilities
- [4-5 bullet points]

## Core Competencies
### [Category 1]
- [Items]

## Decision Framework
**MCP Reference**: [get_architecture_template / get_doc / get_sample_tree]

### When to Use [Pattern]
✅ [3-5 criteria]
❌ [3-5 anti-criteria]

### vs Alternative Approaches
[Comparison table]

## Architecture Patterns / Implementation Guide
[2-3 major patterns with code examples]

## Anti-Patterns & Pitfalls
[3-5 anti-patterns with wrong/correct code]

## Migration Paths
[1-2 progression paths]

## Integration Scenarios
[2-3 integration examples]

## Testing Strategy
[2-3 test examples]

## Best Practices Checklist
[10-15 checklist items]

## MCP Workflow Examples
[3-4 concrete MCP queries]

## Further Resources
[MCP resources, related skills, packages]
```

---

## Skill-Specific Outlines

### 1. data/data-architect.md

**Role**: Persistence technology selector for Mvp24Hours solutions

**Core Competencies**:
- EF Core (SQL Server, PostgreSQL, MySQL) selection
- MongoDB (document database) selection
- Redis (caching, pub/sub) selection
- Dapper (read optimization) selection
- Hybrid approaches (EF write + Dapper read)

**Decision Framework**:
```
Data Store Selection:
├─ Relational data with ACID → EF Core
├─ Schema flexibility, horizontal scaling → MongoDB
├─ High-speed caching, real-time → Redis
└─ Performance-critical reads → Dapper + EF Core
```

**Key Patterns**:
1. **Repository Pattern** with `IRepositoryAsync<T>`
2. **Unit of Work** with `IUnitOfWorkAsync`
3. **Specifications** for complex queries
4. **Hybrid Read/Write** (EF write, Dapper read)

**MCP Queries**:
```bash
search_docs "query": "repository pattern"
get_doc "path": "docs/en-us/database/relational.md"
get_sample_tree "sampleId": "simple-crud-ef-customer-api"
get_sample_tree "sampleId": "simple-crud-mongodb-customer-api"
get_sample_tree "sampleId": "simple-crud-ef-dapper-customer-api"
```

**Code Template**:
```csharp
// EF Core setup
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddMvp24HoursDbContext<DataContext>();
builder.Services.AddMvp24HoursRepositoryAsync();

// MongoDB setup
builder.Services.AddMvp24HoursMongoDbContext(options =>
{
    options.ConnectionString = mongoConnectionString;
    options.DatabaseName = "CustomersDb";
});
builder.Services.AddMvp24HoursMongoRepositoryAsync();

// Redis setup
builder.Services.AddMvp24HoursCaching();
builder.Services.AddMvp24HoursCachingRedis(redisConnectionString);
```

**Anti-Patterns**:
1. Using EF Core for read-heavy high-performance queries → Use Dapper
2. Using MongoDB when ACID transactions critical → Use SQL
3. No repository abstraction → Hard to test and change providers

**Integration**: With CQRS (separate read/write stores), with Caching (query results)

**Testing**: InMemory provider for EF, Testcontainers for MongoDB/Redis

---

### 2. data/efcore-specialist.md

**Role**: EF Core advanced implementation specialist

**Core Competencies**:
- DbContext configuration and lifecycle
- Entity configurations (`IEntityTypeConfiguration<T>`)
- Migrations and seeding
- Query optimization (Include, AsNoTracking, split queries)
- Interceptors (audit, soft delete, multi-tenancy)
- Execution strategies (retry on failure)
- Value conversions (for Value Objects)

**Decision Framework**:
✅ Use EF Core for: ACID transactions, complex relationships, mature ecosystem
❌ Don't use for: Extremely high-performance reads (use Dapper), schema-less data (use MongoDB)

**Key Patterns**:
1. **Repository with Specifications**
2. **Audit Interceptor** (CreatedAt, UpdatedAt, CreatedBy)
3. **Soft Delete Global Filter**
4. **Value Object Conversions**

**MCP Queries**:
```bash
get_doc "path": "docs/en-us/database/relational.md"
get_doc "path": "docs/en-us/database/efcore-advanced.md"
get_doc "path": "docs/en-us/database/use-repository.md"
get_sample_tree "sampleId": "complex-crud-ef-customer-api"
get_sample_tree "sampleId": "simple-crud-ef-entitylog-customer-api"
```

**Code Template**:
```csharp
// Entity Configuration
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        
        // Value Object conversion
        builder.Property(c => c.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(100);
        
        // Soft delete filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

// Repository with Specification
public async Task<IEnumerable<Customer>> FindAsync(
    ISpecificationResult<Customer> spec,
    CancellationToken ct)
{
    return await _context.Customers
        .Where(spec.ToExpression())
        .ToListAsync(ct);
}

// Audit Interceptor
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        var entries = eventData.Context!.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        
        foreach (var entry in entries)
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                    auditable.CreatedAt = DateTime.UtcNow;
                auditable.UpdatedAt = DateTime.UtcNow;
            }
        }
        
        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

**Anti-Patterns**:
1. **N+1 queries** → Use `.Include()` or split queries
2. **Loading entire graphs** → Project to DTOs
3. **Not using AsNoTracking for reads** → Performance hit
4. **EF attributes in Domain** → Use `IEntityTypeConfiguration<T>`

**Testing**: InMemory provider for unit tests, real DB for integration

---

### 3. cqrs/cqrs-architect.md

**Role**: CQRS pattern design and Mvp24Hours mediator implementation

**Core Competencies**:
- Command/Query separation
- Mvp24Hours Mediator (`AddMvpMediator`)
- `IMediatorCommand<T>` / `IMediatorQuery<T>`
- Pipeline behaviors (logging, validation, caching)
- Domain events with `IMediatorNotification`
- Read/write model separation

**Decision Framework**:
✅ Use CQRS when: Different read/write models, complex behaviors, event-driven needs
❌ Don't use for: Simple CRUD, no cross-cutting concerns

**Key Patterns**:
1. **Commands** (write operations)
2. **Queries** (read operations)
3. **Handlers** (one per command/query)
4. **Behaviors** (cross-cutting pipeline)
5. **Notifications** (domain events)

**MCP Queries**:
```bash
get_architecture_template "templateId": "cqrs"
get_doc "path": "docs/en-us/cqrs/getting-started.md"
get_doc "path": "docs/en-us/cqrs/commands.md"
get_doc "path": "docs/en-us/cqrs/queries.md"
get_doc "path": "docs/en-us/cqrs/behaviors.md"
get_sample_tree "sampleId": "complex-cqrs-ef-customer-api"
```

**Code Template**:
```csharp
// Setup
builder.Services.AddMvpMediator(options =>
{
    options.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Command
public record CreateCustomerCommand(string Name, string Email) 
    : IMediatorCommand<Guid>;

// Handler
public class CreateCustomerHandler(
    IRepositoryAsync<Customer> repository,
    IUnitOfWorkAsync unitOfWork)
    : IMediatorCommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCustomerCommand command,
        CancellationToken ct)
    {
        var customer = Customer.Create(command.Name, command.Email);
        repository.Add(customer);
        await unitOfWork.SaveChangesAsync(ct);
        return customer.Id;
    }
}

// Query
public record GetCustomerByIdQuery(Guid Id) : IMediatorQuery<CustomerDto?>;

public class GetCustomerByIdHandler(IRepositoryAsync<Customer> repository)
    : IMediatorQueryHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(
        GetCustomerByIdQuery query,
        CancellationToken ct)
    {
        var customer = await repository.GetByIdAsync(query.Id, ct);
        return customer is not null
            ? new CustomerDto(customer.Id, customer.Name, customer.Email)
            : null;
    }
}

// Behavior (logging)
public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}

// Controller dispatch
[HttpPost]
public async Task<IActionResult> Create(
    CreateCustomerCommand command,
    CancellationToken ct)
{
    var id = await _mediator.SendAsync(command, ct);
    return CreatedAtAction(nameof(GetById), new { id }, null);
}
```

**CRITICAL**: Use `IMediatorCommand<T>`, NOT `IRequest<T>` (MediatR)

**Anti-Patterns**:
1. Using MediatR instead of Mvp24Hours Mediator
2. CQRS for trivial CRUD
3. Commands returning full entities → Return IDs only
4. Queries with side effects → Commands only mutate

---

### 4. messaging/messaging-architect.md

**Role**: RabbitMQ integration and messaging pattern selector

**Core Competencies**:
- Publish/Subscribe patterns
- Typed consumers (`IMessageConsumerAsync<T>`)
- Request/Response patterns
- Topic exchange routing
- Dead letter queues
- Outbox/Inbox for reliability
- Message scheduling

**Decision Framework**:
✅ Use RabbitMQ for: Async workflows, service integration, event-driven architecture
❌ Don't use for: Synchronous request/response within bounded context, simple in-process events

**Key Patterns**:
1. **Pub/Sub** (fire-and-forget events)
2. **Request/Response** (async RPC)
3. **Outbox Pattern** (reliable publishing)
4. **Inbox Pattern** (idempotent consumption)
5. **Saga Orchestration** (distributed workflows)

**MCP Queries**:
```bash
get_doc "path": "docs/en-us/broker.md"
get_doc "path": "docs/en-us/broker-advanced.md"
get_doc "path": "docs/en-us/cqrs/integration-rabbitmq.md"
get_sample_tree "sampleId": "simple-rabbitmq-customer-api"
get_sample_tree "sampleId": "complex-event-driven-rabbitmq-customer-api"
```

**Code Template**:
```csharp
// Setup
builder.Services.AddMvpRabbitMQ(
    connectionString,
    rabbit =>
    {
        rabbit.AddConsumersFromAssemblyContaining<CustomerCreatedConsumer>();
        rabbit.ConfigureClient(client =>
        {
            client.Exchange = "orders.events";
            client.ExchangeType = MvpRabbitMQExchangeType.topic;
            client.Durable = true;
        });
    });

// Publish
public class OrderService(IMvpRabbitMQClient rabbitMQ)
{
    public async Task CreateOrderAsync(Order order)
    {
        // Save order...
        
        // Publish event
        rabbitMQ.Publish(
            new OrderCreated(order.Id, order.CustomerId),
            "order.created");
    }
}

// Typed Consumer
public class OrderCreatedConsumer : IMessageConsumerAsync<OrderCreated>
{
    public string QueueName => "notifications.order-created";
    public string RoutingKey => "order.created";

    public async Task ConsumeAsync(
        OrderCreated message,
        ConsumeContext context)
    {
        // Send email notification
        await _emailService.SendOrderConfirmationAsync(
            message.CustomerId,
            message.OrderId);
    }
}

// Request/Response
builder.Services.AddRequestClient<GetOrderRequest, GetOrderResponse>(request =>
{
    request.Exchange = "orders";
    request.RoutingKey = "orders.get";
    request.TimeoutMilliseconds = 5000;
});

public async Task<GetOrderResponse> GetOrderDetailsAsync(Guid orderId)
{
    var response = await _requestClient.RequestAsync(
        new GetOrderRequest(orderId),
        CancellationToken.None);
    return response;
}
```

**Anti-Patterns**:
1. No outbox → Lost messages on failure
2. Non-idempotent consumers → Duplicate processing
3. Synchronous calls disguised as messages → Use HTTP
4. Large message payloads → Use reference IDs

---

## Content Templates by Section

### Decision Framework Template

```markdown
## Decision Framework

**MCP Reference**:
\`\`\`bash
get_doc "path": "docs/en-us/[area]/[topic].md"
get_sample_tree "sampleId": "[sample-id]"
\`\`\`

### When to Use [Technology/Pattern]

✅ **Choose [Name] When**:
- [Business criterion 1]
- [Technical criterion 2]
- [Team criterion 3]
- [Domain criterion 4]

❌ **Don't Choose [Name] When**:
- [Anti-criterion 1]
- [Anti-criterion 2]
- [Anti-criterion 3]

### vs Alternative Approaches

| Aspect | [This Choice] | [Alternative 1] | [Alternative 2] |
|--------|--------------|-----------------|-----------------|
| **Performance** | [Rating/Details] | [Rating/Details] | [Rating/Details] |
| **Complexity** | [Rating/Details] | [Rating/Details] | [Rating/Details] |
| **Scalability** | [Rating/Details] | [Rating/Details] | [Rating/Details] |
| **Team Expertise** | [Rating/Details] | [Rating/Details] | [Rating/Details] |
| **Use Case** | [Description] | [Description] | [Description] |
```

### Anti-Pattern Template

```markdown
### [N]. [Anti-Pattern Name]

**Problem**: [Brief description of the issue]

**❌ WRONG**:
\`\`\`csharp
// [Comment explaining why this is wrong]
[Code showing anti-pattern]
\`\`\`

**Symptoms**:
- [Symptom 1]
- [Symptom 2]

**✅ CORRECT**:
\`\`\`csharp
// [Comment explaining correct approach]
[Code showing correct pattern]
\`\`\`

**Why**: [2-3 sentences explaining benefits of correct approach]

**Related**: Consult `[related-skill].md` for [topic]
```

### Testing Strategy Template

```markdown
## Testing Strategy

### [Test Type] ([Unit|Integration|E2E])

**Scope**: [What this test covers]

**Setup**:
\`\`\`csharp
// Test setup code
using NSubstitute; // or xUnit, FluentAssertions

public class [TestClass]
{
    private readonly [IDependency] _dependency;
    private readonly [SystemUnderTest] _sut;

    public [TestClass]()
    {
        _dependency = Substitute.For<[IDependency]>();
        _sut = new [SystemUnderTest](_dependency);
    }

    [Fact]
    public async Task [MethodName]_[Scenario]_[ExpectedResult]()
    {
        // Arrange
        [Setup test data and mocks]

        // Act
        [Execute system under test]

        // Assert
        [Verify expected outcome]
        [Assertions using FluentAssertions]
    }
}
\`\`\`

**Key Points**:
- [Testing principle 1]
- [Testing principle 2]
- Use Mvp24Hours test helpers: [specific helpers]

**MCP Reference**:
\`\`\`bash
get_test_scaffold "tier": "[minimal|simple|complex]", "dataStore": "[efcore|mongodb]"
get_doc "path": "docs/en-us/testing/home.md"
\`\`\`
```

---

## Quick Skill Creation Workflow

For each skill:

1. **Research** (20-30 min):
   - Run MCP queries for docs/samples
   - Review completed skills for pattern
   - Note key Mvp24Hours APIs

2. **Outline** (10-15 min):
   - Copy template structure
   - Fill role & expertise
   - List main patterns (3-5)
   - Identify anti-patterns (3-5)

3. **Content** (90-120 min):
   - Write decision framework
   - Add implementation code (Mvp24Hours APIs)
   - Document anti-patterns
   - Write integration scenarios
   - Add testing examples
   - Create MCP workflow examples

4. **Quality Check** (10-15 min):
   - Verify 300-500 lines
   - Check all MCP references
   - Ensure no local paths
   - Validate code compiles conceptually

5. **Finalize** (5 min):
   - Update COMPLETION_STATUS.md
   - Commit with clear message

**Total**: 2.5-3 hours per skill

---

## Remaining Skills Quick Outlines

### 5. cqrs/mediator-patterns-specialist.md
- **Focus**: Deep Mvp24Hours mediator implementation
- **Key Topics**: Command/query handlers, behaviors, notifications, streaming
- **Code**: Handler lifecycle, behavior pipeline, notification fanout
- **Anti-Patterns**: Using MediatR, handlers with side effects

### 6. messaging/rabbitmq-advanced-specialist.md
- **Focus**: Advanced RabbitMQ features
- **Key Topics**: Scheduling, filters, priority queues, headers exchange
- **Code**: Delayed messages, consumer middleware, batch publishing
- **Anti-Patterns**: Blocking operations in consumers

### 7. observability/observability-architect.md
- **Focus**: OpenTelemetry integration
- **Key Topics**: Traces, metrics, logs, OTLP exporters
- **Code**: `AddMvp24HoursObservability()`, activity sources, meters
- **Anti-Patterns**: Over-instrumentation, missing correlation IDs

### 8. webapi/webapi-architect.md
- **Focus**: HTTP composition root on Minimal, Simple, and Complex (ASP.NET Minimal APIs ≠ structure Minimal)
- **Key Topics**: `AddMvp24HoursWebEssential`, Native OpenAPI, Problem Details, controllers vs Map*
- **Code**: `AddMvp24HoursNativeOpenApi()`, `MapControllers` (Simple/Complex samples), TypedResults (Minimal sample)
- **Anti-Patterns**: Treating the skill as structure Minimal only; Swashbuckle on new work; inventing `AddMvp24HoursWebApi`

### 9. data/mongodb-specialist.md
- **Focus**: Document database patterns
- **Key Topics**: Schema design, indexes, transactions
- **Code**: `AddMvp24HoursMongoDbContext()`, repository patterns
- **Anti-Patterns**: Using Mongo when ACID critical

### 10. cqrs/event-sourcing-specialist.md
- **Focus**: Event store, projections, snapshots
- **Key Topics**: Event streams, read models, replay
- **Code**: Aggregate reconstruction, projection handlers
- **Anti-Patterns**: Mutable events, missing versioning

### 11. messaging/saga-orchestration-specialist.md
- **Focus**: Distributed transaction patterns
- **Key Topics**: Saga state, compensation, timeouts
- **Code**: `PipelineSagaOrchestrator`, step definitions
- **Anti-Patterns**: No compensation logic

### 12. observability/resilience-patterns-specialist.md
- **Focus**: Circuit breaker, retry, timeout
- **Key Topics**: Native .NET resilience, Polly v8
- **Code**: `AddMvpStandardResilience()`, policy configuration
- **Anti-Patterns**: Retry without backoff

### 13-15. architecture/[hexagonal|event-driven|microservices]-specialist.md
- **Hex**: Ports/adapters, framework independence
- **Event**: Outbox/inbox, async workflows
- **Micro**: Aspire, service boundaries

### 16. data/redis-specialist.md
- **Focus**: Caching strategies, pub/sub
- **Code**: `AddMvp24HoursCachingRedis()`, cache-aside pattern

### 17. pipeline/pipeline-architect.md
- **Focus**: Pipes & filters, operations, validation
- **Code**: `AddMvp24HoursPipelineAsync()`, typed pipelines

### 18. caching/caching-architect.md
- **Focus**: HybridCache, L1/L2 tiers
- **Code**: `AddMvpHybridCache()`, stampede protection

### 19. infrastructure/infrastructure-architect.md
- **Focus**: Email, SMS, files, locks, jobs
- **Code**: `AddEmailService()`, `AddDistributedLocking()`

### 20. testing/testing-architect.md
- **Focus**: Unit, integration, test harnesses
- **Code**: `AddMvpTestingInfrastructure()`, WebApplicationFactory

### 21. identity/identity-architect.md
- **Focus**: Keycloak JWT authentication
- **Code**: `AddMvp24HoursKeycloak()`, UMA/RPT patterns

### 22. cronjob/cronjob-architect.md
- **Focus**: Cron-based hosted services
- **Code**: `AddMvp24HoursCronJob()`, overlap prevention

### 23. modernization/dotnet-modernization-specialist.md
- **Focus**: .NET 10 features (TimeProvider, Channels, Aspire)
- **Code**: Native APIs, HybridCache, keyed services

---

## Automation Script Template

```powershell
# Create skill from outline
param(
    [string]$SkillName,
    [string]$Category
)

$template = Get-Content "skills/SKILL_TEMPLATE.md" -Raw
$outline = Get-Content "skills/SKILLS_GENERATION_GUIDE.md" -Raw

# Extract outline for specific skill
# Generate skill content following template
# Validate against quality checklist
# Write to skills/$Category/$SkillName.md
```

---

## Next Actions

1. Create Phase 1 skills (4 HIGH priority)
2. Validate against template & checklist
3. Update COMPLETION_STATUS.md
4. Continue with Phase 2-4

**Estimated Remaining Time**: 23 skills × 2.5h = ~58 hours

---

**Note**: This guide provides the framework to create all remaining skills consistently. Each skill outline includes the specific MCP queries, code templates, and anti-patterns needed. Follow the template structure strictly for consistency across the entire skills ecosystem.
