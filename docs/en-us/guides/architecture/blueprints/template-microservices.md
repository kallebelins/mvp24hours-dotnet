---
templateId: microservices
tier: Blueprint
shape: blueprint
layers: [Domain, Application, Infrastructure, API, Worker, AppHost]
dependencyRule: One team owns service data; no shared database tables
samplePath: samples/src/microservices-aspire-customer
mvp24hoursModules: [modernization/aspire, broker, infrastructure/http-resilience, observability]
---

# Microservices Blueprint

Use microservices only when independent deployment, scaling, ownership, or regulatory isolation outweighs distributed-system cost. For code organization alone, prefer a modular monolith.

## Boundaries

- One team owns a service lifecycle and data.
- Services do not share database tables.
- Integration contracts are explicit and versioned.
- Synchronous calls use timeouts, cancellation, and dependency-specific resilience.
- Asynchronous consumers are idempotent and use inbox/outbox where consistency matters.
- Each service publishes logs, traces, metrics, and health signals.

```text
Service/
├── Domain/
├── Application/
├── Infrastructure/
├── API or Worker host/
└── Tests/
```

Use the Mvp24Hours Mediator inside a service and RabbitMQ for durable asynchronous integration. Do not copy MediatR examples. Prefer .NET Aspire for local composition/service defaults where it fits, and use .NET 10 container images.

See [Event-Driven](template-event-driven.md), [RabbitMQ](../../../broker.md), [HTTP Resilience](../../../infrastructure/http-resilience.md), [Observability](../../../observability/home.md), [.NET Aspire](../../../modernization/aspire.md), and [Containerization](../../deployment/containerization.md).

> **Sample:** [`microservices-aspire-customer`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/microservices-aspire-customer/README.md) — Customer API + Notification worker with separate data stores, RabbitMQ, and an Aspire AppHost for local orchestration.
