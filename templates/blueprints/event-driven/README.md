# Event-Driven template

Event-driven scaffold with a placeholder `Item` resource and production-oriented messaging path.

## Architecture

- Tier: Complex
- Shape: Domain → Application (commands + integration contracts) → Infrastructure → WebAPI
- Persistence: EF Core **InMemory** by default
- Integration events: `IIntegrationEventPublisher` with RabbitMQ-enabled publisher by default and in-memory fallback

## Layers

- `App.Domain` — `Item` entity
- `App.Application` — `CreateItemCommand`, `IIntegrationEventPublisher`, `ItemCreatedIntegrationEvent`
- `App.Infrastructure` — `EFDBContext`, integration publishers (`RabbitMqIntegrationEventPublisher`, `InMemoryIntegrationEventPublisher`)
- `App.WebAPI` — Mediator controller, DI, OpenAPI, health
- `App.Test` — smoke tests

## Production baseline included

- Native OpenAPI
- Keycloak baseline (authentication and authorization pipeline)
- Request observability middleware
- Hybrid cache registration
- Resilient HttpClient defaults
- HTTP middleware hardening: rate limiting, idempotency, and output cache
- Health checks (`self` + Keycloak)
- RabbitMQ publisher enabled via `RabbitMQ:Enabled=true`

These HTTP middleware features are configurable through `HttpHardening` in `App.WebAPI/appsettings*.json`.

## Messaging mode

- RabbitMQ mode (default): `RabbitMQ:Enabled=true`
- In-memory fallback: `RabbitMQ:Enabled=false`

## Local dependencies

Start required services from this folder:

```bash
docker compose up -d
```

## Advanced production path

For RabbitMQ + transactional outbox, see the full teaching sample:

[`samples/src/complex-event-driven-rabbitmq-customer-api`](../../../samples/src/complex-event-driven-rabbitmq-customer-api)

## Run

```bash
dotnet run --project App.WebAPI
```

- OpenAPI: `http://localhost:5103/openapi/v1.json`
- Health: `http://localhost:5103/hc`

Watch the logs for integration event publish entries after POST.

## Related

- Docs: [Event-driven architecture](../../../docs/en-us/guides/architecture/structures/)
