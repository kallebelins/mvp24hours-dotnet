# DDD template

Compilable Domain-Driven Design scaffold with a placeholder `Item` aggregate. Copy, rename `App` / `Item`, and implement your bounded context.

## Architecture

- Tier: Complex
- Shape: Core (aggregates + value objects + domain events) → Application (CQRS/mediator) → Infrastructure → WebAPI
- Persistence: EF Core **InMemory** by default
- Writes use aggregate factory methods (`Item.Create`) — no anemic AutoMapper-to-entity mapping

## Layers

- `App.Core` — `Item` aggregate (`IAggregateRoot`, `IHasDomainEvents`), `ItemName` value object, `ItemCreatedDomainEvent`
- `App.Application` — `CreateItemCommand`, `GetItemsQuery`, Mvp24Hours Mediator handlers
- `App.Infrastructure` — `EFDBContext`, Fluent API configuration
- `App.WebAPI` — Mediator controllers, DI, OpenAPI, health
- `App.Test` — smoke tests

## Production baseline included

- Native OpenAPI
- Keycloak baseline (authentication and authorization pipeline)
- Request observability middleware
- Hybrid cache registration
- Resilient HttpClient defaults
- HTTP middleware hardening: rate limiting, idempotency, and output cache
- Health checks (`self` + Keycloak)
- Domain events ready for evolution to broker/outbox

These HTTP middleware features are configurable through `HttpHardening` in `App.WebAPI/appsettings*.json`.

## Local dependencies

Start required services from this folder:

```bash
docker compose up -d
```

## Rename checklist

1. Rename projects/namespaces `App` → your service name
2. Rename `Item` → your aggregate
3. Add domain methods and events on the aggregate
4. Replace InMemory with SQL Server in `ServiceBuilderExtensions`

## Run

```bash
dotnet run --project App.WebAPI
```

- OpenAPI: `http://localhost:5102/openapi/v1.json`
- Health: `http://localhost:5102/hc`

## Related

- Teaching sample: [`samples/src/complex-ddd-ef-customer-api`](../../../samples/src/complex-ddd-ef-customer-api)
- Docs: [DDD architecture](../../../docs/en-us/guides/architecture/structures/)
