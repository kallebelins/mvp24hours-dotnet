# Event-Driven Blueprint

Use events to notify interested components about facts that have already happened. Distinguish domain events inside one consistency boundary from integration events crossing process or service boundaries.

```text
Domain action
  -> commit local state
  -> durable outbox
  -> publish integration event
  -> consumer inbox/idempotency
  -> local transaction
```

## Rules

- Name events in the past tense and make contracts immutable.
- Do not publish externally before the related database commit is durable.
- Treat delivery as at-least-once: consumers must be idempotent.
- Version contracts compatibly and include correlation/causation identifiers.
- Define retries, dead-letter handling, timeouts, and ownership before production use.
- Use synchronous application calls where immediate consistency is required.

Use Mvp24Hours domain events for in-process decoupling, the mediator notification APIs for local dispatch, and RabbitMQ plus inbox/outbox for durable integration.

See [Domain Events](../../../cqrs/domain-events.md), [Integration Events](../../../cqrs/integration-events.md), [RabbitMQ Integration](../../../cqrs/integration-rabbitmq.md), [RabbitMQ Advanced](../../../broker-advanced.md), and [Inbox/Outbox](../../../cqrs/resilience/inbox-outbox.md).

> **Sample:** [`complex-event-driven-rabbitmq-customer-api`](../../../../../samples/src/complex-event-driven-rabbitmq-customer-api/README.md) — durable outbox, RabbitMQ publish, consumer inbox/idempotency, and correlation IDs. Simpler messaging baseline: [`simple-rabbitmq-customer-api`](../../../../../samples/src/simple-rabbitmq-customer-api/CustomerAPI.WebAPI/README.md).
