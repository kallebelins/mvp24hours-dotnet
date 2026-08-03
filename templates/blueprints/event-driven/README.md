# Event-Driven template

Lean event-driven scaffold with a placeholder `Item` resource. **No RabbitMQ required at runtime** — integration events are published in-memory and logged.

## Architecture

- Tier: Complex
- Shape: Domain → Application (commands + integration contracts) → Infrastructure → WebAPI
- Persistence: EF Core **InMemory** by default
- Integration events: `IIntegrationEventPublisher` with `InMemoryIntegrationEventPublisher` (logs events)

## Layers

- `App.Domain` — `Item` entity
- `App.Application` — `CreateItemCommand`, `IIntegrationEventPublisher`, `ItemCreatedIntegrationEvent`
- `App.Infrastructure` — `EFDBContext`, `InMemoryIntegrationEventPublisher`
- `App.WebAPI` — Mediator controller, DI, OpenAPI, health
- `App.Test` — smoke tests

## Production path

For RabbitMQ + transactional outbox, see the full teaching sample:

[`samples/src/complex-event-driven-rabbitmq-customer-api`](../../../samples/src/complex-event-driven-rabbitmq-customer-api)

## Run

```bash
dotnet run --project App.WebAPI
```

- OpenAPI: `http://localhost:5103/openapi/v1.json`
- Health: `http://localhost:5103/hc`

Watch the console for `[IntegrationEvent] ItemCreatedIntegrationEvent` log entries after POST.

## Related

- Docs: [Event-driven architecture](../../../docs/en-us/guides/architecture/structures/)
