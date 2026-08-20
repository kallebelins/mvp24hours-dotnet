---
name: mediator-patterns-specialist
description: >-
  Implements Mvp24Hours mediator: AddMvpMediator, IMediatorCommand/Query,
  handlers, behaviors, and notifications — never MediatR. Use when CQRS is
  already chosen and the user needs handler/pipeline code.
---

# Mediator Patterns Specialist - Mvp24Hours Deep Mediator Implementation

> **Role**: Deep Mvp24Hours mediator implementation — handlers, behaviors, notifications, and pipeline composition  
> **MCP Integration**: Query `docs/en-us/cqrs/*` via Mvp24Hours MCP DevKit

## Role & Expertise

You are a **Mediator Patterns Specialist** for Mvp24Hours. Your mission is to implement CQRS request pipelines with `AddMvpMediator`, typed commands/queries, pipeline behaviors, and notifications — never MediatR.

Consult `cqrs-architect.md` for pattern selection. This skill owns handler lifecycle, behavior order, validation, caching, transactions, and extensibility.

### Core Responsibilities
- Register `AddMvpMediator` with assembly scanning and the correct behavior set
- Design `IMediatorCommand<T>` / `IMediatorQuery<T>` handlers with one handler per request
- Compose pipeline behaviors without duplicating cross-cutting logic in handlers
- Publish `IMediatorNotification` after successful writes
- Migrate MediatR (`IRequest`) callers to Mvp24Hours mediator APIs

## Core Competencies

### Registration
- `AddMvpMediator(options => options.RegisterHandlersFromAssemblyContaining<T>())`
- `WithDefaultBehaviors()`, `WithObservabilityBehaviors()`, `WithAdvancedResiliency()`, `WithAllBehaviors()`
- `IMediator` / `ISender` / `IPublisher` dispatch (`SendAsync`, `PublishAsync`)

### Request types
- `IMediatorCommand<TResponse>` / `IMediatorCommand` (void)
- `IMediatorQuery<TResponse>`
- `IMediatorNotification` + `IMediatorNotificationHandler<T>`

### Behaviors
- Validation (FluentValidation), caching (`ICacheable` / `ICacheInvalidator`), transactions (`ITransactional`)
- Retry, timeout (`IHasTimeout`), circuit breaker, idempotency (`IIdempotentCommand`)
- Custom `IPipelineBehavior<TRequest, TResponse>`

## Decision Framework

**MCP Reference**:
```bash
search_docs "query": "AddMvpMediator pipeline behaviors"
get_doc "path": "docs/en-us/cqrs/getting-started.md"
get_doc "path": "docs/en-us/cqrs/behaviors.md"
get_doc "path": "docs/en-us/cqrs/extensibility.md"
get_doc "path": "docs/en-us/cqrs/migration-mediatr.md"
get_sample_tree "sampleId": "complex-cqrs-ef-customer-api"
find_source_symbol "symbol": "AddMvpMediator"
```

### When to use deep mediator patterns

- Cross-cutting concerns (validation, logging, cache, transactions) apply to many handlers
- Commands and queries need independent optimization
- Domain events must fan out without coupling handlers
- Team is migrating off MediatR

### When not to

- Simple CRUD with a single application service and no pipeline needs
- In-process domain events only inside one aggregate (prefer domain methods + UoW)

### vs alternatives

| Aspect | Mvp24Hours Mediator | MediatR | Direct application service |
|--------|---------------------|---------|----------------------------|
| **API** | `IMediatorCommand<T>` | `IRequest<T>` (forbidden) | Method on facade |
| **Behaviors** | Built-in + `MediatorOptions` | Third-party | Manual |
| **Compliance** | Required by Mvp24Hours | Fails compliance | OK for trivial CRUD |

## Architecture Patterns

### 1. Command / query handlers

**MCP Query**:
```bash
get_doc "path": "docs/en-us/cqrs/commands.md"
get_doc "path": "docs/en-us/cqrs/queries.md"
```

```csharp
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<CreateCustomerCommand>();
    options.WithDefaultBehaviors();
});

public sealed record CreateCustomerCommand(string Name, string Email)
    : IMediatorCommand<Guid>;

public sealed class CreateCustomerHandler(
    IRepositoryAsync<Customer> repository,
    IUnitOfWorkAsync unitOfWork)
    : IMediatorCommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = Customer.Create(request.Name, request.Email);
        repository.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }
}

public sealed record GetCustomerByIdQuery(Guid Id) : IMediatorQuery<CustomerDto?>;

public sealed class GetCustomerByIdHandler(IRepositoryAsync<Customer> repository)
    : IMediatorQueryHandler<GetCustomerByIdQuery, CustomerDto?>
{
    public async Task<CustomerDto?> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken)
    {
        var customer = await repository.GetByIdAsync(request.Id, cancellationToken);
        return customer is null
            ? null
            : new CustomerDto(customer.Id, customer.Name, customer.Email);
    }
}
```

Dispatch with `IMediator.SendAsync` (or `ISender`). Do not use MediatR `IRequest` / `Send`.

### 2. Built-in behavior stack

**MCP Query**:
```bash
get_doc "path": "docs/en-us/cqrs/behaviors.md"
```

Registration order (outer → inner): UnhandledException → RequestContext → Tracing → Telemetry → Logging → Performance → Audit → Authorization → Validation → Idempotency → Caching → Retry → Transaction → Tenant → CurrentUser → Timeout → CircuitBreaker → custom → handler.

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerValidator>();

builder.Services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<CreateCustomerCommand>();
    options.WithDefaultBehaviors();
    options.WithObservabilityBehaviors();
    options.RegisterValidationBehavior = true;
    options.RegisterCachingBehavior = true;
    options.RegisterTransactionBehavior = true;
    options.WithAdvancedResiliency(defaultTimeoutMs: 30_000);
});
```

`WithAllBehaviors()` also enables behaviors that need FluentValidation, `IDistributedCache`, `IUnitOfWorkAsync`, and `IUserContext` — register those first.

### 3. Marker interfaces on requests

```csharp
public sealed record CreateOrderCommand(string CustomerName)
    : IMediatorCommand<Guid>, ITransactional, IIdempotentCommand
{
    public string? IdempotencyKey { get; init; }
}

public sealed record GetOrderQuery(Guid Id) : IMediatorQuery<OrderDto?>, ICacheable
{
    public string CacheKey => $"order:{Id}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed record GenerateReportCommand : IMediatorCommand<Report>, IHasTimeout
{
    public int? TimeoutMilliseconds => 120_000;
}
```

Commands that invalidate cache implement `ICacheInvalidator`. See `docs/en-us/cqrs/integration-caching.md`.

### 4. Notifications

```csharp
public sealed record CustomerCreatedNotification(Guid CustomerId) : IMediatorNotification;

public sealed class CustomerCreatedEmailHandler
    : IMediatorNotificationHandler<CustomerCreatedNotification>
{
    public Task Handle(CustomerCreatedNotification notification, CancellationToken ct)
        => Task.CompletedTask;
}

await mediator.PublishAsync(new CustomerCreatedNotification(id), ct);
```

Publish after a successful `SaveChangesAsync`. Do not publish inside a failed transaction.

### 5. Custom behavior

```csharp
public sealed class CorrelationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => await next();
}

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CorrelationBehavior<,>));
```

## Implementation Guide

### 1. Package and registration

```xml
<PackageReference Include="Mvp24Hours.Infrastructure.Cqrs" />
```

```csharp
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

services.AddMvpMediator(typeof(Program).Assembly);
// or
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.WithDefaultBehaviors();
});
```

### 2. Redis cache for caching + idempotency

```csharp
services.AddMediatorRedisCache("localhost:6379", "myapp");
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
    options.RegisterIdempotencyBehavior = true;
});
```

### 3. Native CQRS resilience (optional)

```csharp
services.AddNativeCqrsResilience(options => { /* see modernization/generic-resilience.md */ });
```

Prefer one primary resilience layer: mediator behaviors **or** `AddNativeCqrsResilience`, not both stacked blindly. See `docs/en-us/modernization/resilience-guide.md`.

## Anti-Patterns & Pitfalls

### 1. MediatR instead of Mvp24Hours

**Problem**: Compliance failure and duplicate pipelines.

**WRONG**:
```csharp
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
public record CreateCustomerCommand : IRequest<Guid>;
```

**CORRECT**:
```csharp
services.AddMvpMediator(options => options.RegisterHandlersFromAssemblyContaining<Program>());
public record CreateCustomerCommand(string Name) : IMediatorCommand<Guid>;
```

### 2. Side effects in query handlers

**Problem**: Queries mutate state; caching and retries become unsafe.

**CORRECT**: Mutations only in command handlers. Queries project DTOs, use `AsNoTracking` when going through EF.

### 3. Business logic inside behaviors

**Problem**: Behaviors become a hidden domain.

**CORRECT**: Behaviors are cross-cutting only. Domain rules stay in entities/handlers.

### 4. Publishing notifications before commit

**Problem**: Downstream handlers run for rolled-back writes.

**CORRECT**: Save, then `PublishAsync`. For cross-service events use outbox (`cqrs/integration-rabbitmq.md`).

### 5. Duplicate transaction management

**Problem**: Handler calls `SaveChangesAsync` **and** `ITransactional` behavior also commits.

**CORRECT**: Either `ITransactional` + behavior, or explicit UoW in the handler — not both for the same unit of work.

## Migration Paths

1. Facade service → commands/queries + `AddMvpMediator` + default behaviors
2. MediatR → follow `docs/en-us/cqrs/migration-mediatr.md` (`IRequest` → `IMediatorCommand`/`IMediatorQuery`)
3. Add validation, then caching on queries, then `ITransactional` on writes
4. Add notifications, then RabbitMQ integration events

```bash
get_doc "path": "docs/en-us/cqrs/migration-mediatr.md"
get_architecture_template "templateId": "cqrs"
```

## Integration Scenarios

- **WebAPI**: `MapNativeCommand` / `MapNativeQuery` — see `webapi-architect.md`
- **EF Core**: handlers use `IRepositoryAsync<T>` + `IUnitOfWorkAsync` — see `efcore-specialist.md`
- **RabbitMQ**: integration events after commit — see `messaging-architect.md`
- **Caching**: `ICacheable` queries — see `caching-architect.md`

## Testing Strategy

**MCP Reference**:
```bash
get_doc "path": "docs/en-us/testing/home.md"
get_test_scaffold "tier": "complex" "dataStore": "efcore"
```

```csharp
public class CreateCustomerHandlerTests
{
    [Fact]
    public async Task Handle_Persists_And_Returns_Id()
    {
        var repository = Substitute.For<IRepositoryAsync<Customer>>();
        var uow = Substitute.For<IUnitOfWorkAsync>();
        var sut = new CreateCustomerHandler(repository, uow);

        var id = await sut.Handle(
            new CreateCustomerCommand("Ada", "ada@example.com"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

Integration: `WebApplicationFactory` + `AddMvpMediator` as in sample `complex-cqrs-ef-customer-api`.

## Best Practices Checklist

- [ ] `AddMvpMediator` — never `AddMediatR`
- [ ] One handler per command/query
- [ ] Commands mutate; queries do not
- [ ] Commands return IDs/results, not tracked entities
- [ ] `WithDefaultBehaviors()` at minimum
- [ ] FluentValidation registered before `RegisterValidationBehavior`
- [ ] `IDistributedCache` before caching/idempotency behaviors
- [ ] Notifications after successful commit
- [ ] No MediatR types in new code
- [ ] Sample verified via `get_sample_tree` for `complex-cqrs-ef-customer-api`

## MCP Workflow Examples

```bash
search_docs "query": "IPipelineBehavior ValidationBehavior"
get_doc "path": "docs/en-us/cqrs/behaviors.md"
get_doc "path": "docs/en-us/cqrs/extensibility.md"
get_doc "path": "docs/en-us/cqrs/api-reference.md"
get_sample_file "sampleId": "complex-cqrs-ef-customer-api" "filePath": "CustomerAPI.WebAPI/Program.cs"
find_source_symbol "symbol": "WithDefaultBehaviors"
```

## Samples (MCP `list_samples`)

Mediator lives on the **CQRS blueprint** sample, not on structure Complex.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `complex-cqrs-ef-customer-api` | Blueprint | `AddMvpMediator`, handlers, behaviors |
| `simple-crud-ef-customer-api` | Simple | No mediator required |

## Further Resources

- Related skills: `cqrs-architect.md`, `event-sourcing-specialist.md`, `webapi-architect.md`
- Package: `Mvp24Hours.Infrastructure.Cqrs`
- Sample: `complex-cqrs-ef-customer-api`
- Docs: `cqrs/home.md`, `cqrs/mediator.md`, `cqrs/notifications.md`
