---
name: testing-architect
description: >-
  Designs Mvp24Hours test strategy: unit vs integration, in-package fakes,
  WebApplicationFactory, Testcontainers, and get_test_scaffold. Use when
  planning tests — not for implementing a single feature without a test plan.
---

# Testing Architect - Mvp24Hours Testing Strategies

> **Role**: Unit vs integration boundaries using in-package test helpers — there is no `Mvp24Hours.*.Testing` NuGet  
> **MCP Integration**: `docs/en-us/testing/home.md` and `get_test_scaffold`

## Role & Expertise

You are a **Testing Architect**. Helpers ship **in the same runtime packages** (Infrastructure, EF Core, MongoDB, RabbitMQ). Tests use xUnit `[Trait("Category", "Unit"|"Integration")]`.

### Core Responsibilities
- Choose unit (fakes/in-memory) vs integration (SQL translation, real broker, full host)
- Register `AddMvpTestingInfrastructure` and provider helpers
- Use `WebApplicationFactory` for HTTP pipeline
- Do not treat in-memory RabbitMQ as protocol coverage
- Prefer Mvp24Hours fakes before NSubstitute for infrastructure ports

## Core Competencies

- `AddMvpTestingInfrastructure()`, `MockClock`, email/SMS/file fakes, `TestHttpMessageHandler`
- EF: `AddMvp24HoursTestInfrastructure<TContext>`, `RepositoryFake`
- Mongo: fakes vs `AddMvp24HoursMongoTestInfrastructure(connectionString)`
- RabbitMQ: `AddRabbitMQTestHarness`, `IInMemoryBus`
- Observability: `AddObservabilityTesting`, dispose listeners
- Scaffold: MCP `get_test_scaffold`

## Decision Framework

**MCP Reference**:
```bash
get_doc "path": "docs/en-us/testing/home.md"
get_test_scaffold "templateId": "simple-nlayers"
get_doc "path": "docs/en-us/ai-resources/layers/layer-tests.md"
```

### Unit when

- Domain methods, handlers with fakes, `InMemoryBus`

### Integration when

- EF SQL, Mongo transactions, real RabbitMQ, `WebApplicationFactory`

## Architecture Patterns

```csharp
services.AddMvpTestingInfrastructure()
    .AddObservabilityTesting("Mvp24Hours.*");
```

```csharp
services.AddMvp24HoursTestInfrastructure<AppDbContext>("OrdersTest");
```

```csharp
services.AddTestConsumer<OrderCreatedConsumer>();
services.AddRabbitMQTestHarness();
await harness.StartAsync();
await harness.PublishAndWaitAsync(new OrderCreated(orderId));
```

Host tests:

```csharp
factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureTestServices(services =>
    {
        services.ReplaceWithTestInfrastructure(options =>
        {
            options.InitialClockTime = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        });
    });
});
```

Naming: `Method_Scenario_Expected`. Layout: UnitTests / IntegrationTests / TestSupport.

## Implementation Guide

Packages on test projects: same Infrastructure/Data/RabbitMQ packages as production plus `Microsoft.AspNetCore.Mvc.Testing`.

Mongo Testcontainers helper **does not start Docker** — the test creates the container then passes the connection string.

Dispose `FakeActivityListener` / `FakeMeterListener` (global diagnostics).

## Anti-Patterns & Pitfalls

### 1. Integration tests without Docker when Category=Integration needs it

**CORRECT**: Split CI like the library (unit vs integration).

### 2. Using EF InMemory to prove relational constraints

**CORRECT**: Real provider / Testcontainers; InMemory does not enforce FKs.

### 3. In-memory bus as RabbitMQ certification

**CORRECT**: Container tests for topology, confirms, DLQ.

### 4. Undisposed activity listeners in parallel tests

**CORRECT**: `using` listeners.

### 5. Inventing `AddMvpTestingInfrastructure` package

**CORRECT**: Extensions in `Mvp24Hours.Infrastructure.Testing.Extensions`.

## Migration Paths

1. Domain unit tests
2. Fake repositories
3. WebApplicationFactory + fakes
4. Container-backed data/broker
5. Coverage baseline doc if required

## Integration Scenarios

Every specialist skill’s Testing section defers here for helpers.

## Testing Strategy

This skill **is** the strategy. Example:

```csharp
[Trait("Category", "Unit")]
public class OrderTests
{
    [Fact]
    public void AddItem_IncreasesTotal()
    {
        var order = new Order(customerId: 10);
        order.AddItem(3, 2, 12.5m);
        Assert.Equal(25m, order.Total);
    }
}
```

## Best Practices Checklist

- [ ] Traits on tests
- [ ] Fakes for infrastructure
- [ ] Clock via `TimeProvider`/`MockClock`
- [ ] No production secrets in tests
- [ ] Unique Mongo/Redis resource names
- [ ] Scaffold from MCP when generating projects

## MCP Workflow Examples

```bash
get_doc "path": "docs/en-us/testing/home.md"
get_test_scaffold "templateId": "minimal-api"
find_source_symbol "symbol": "AddMvpTestingInfrastructure"
```

## Samples (MCP `list_samples`)

Use `get_test_scaffold` with **`templateId`** (`minimal-api`, `simple-nlayers`, `complex-nlayers`). HTTP smoke tests use `WebApplicationFactory<Program>` on the WebAPI host (`webapi-architect.md`). Blueprint samples still have tests; their `.Tier` is Blueprint.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `minimal-crud-ef-customer-api` | Minimal | Scaffold `tier: minimal` |
| `simple-crud-ef-customer-api` | Simple | Layered test host |
| `complex-crud-ef-customer-api` | Complex | Modular test host |
| `complex-cqrs-ef-customer-api` | Blueprint | Test handlers, not Complex N-Layers |

## Further Resources

- Related: all architect skills
- Docs: `testing/coverage-baseline.md`
- Sample testing baseline mentioned in layer-tests template
