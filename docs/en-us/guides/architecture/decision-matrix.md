# Architecture Decision Matrix

Choose from constraints, not labels. Start small and introduce additional boundaries only when they have an owner and a measurable purpose.

## Primary decision

| Situation | Suggested starting point | Main trade-off |
|-----------|--------------------------|----------------|
| Small CRUD API, one team, one data store | [Minimal API](structures/structure-minimal-api.md) | Fast delivery; fewer enforced boundaries |
| Conventional business application | [Simple N-Layers](structures/structure-simple-nlayers.md) | Clear separation with moderate ceremony |
| Large modular monolith | [Complex N-Layers](structures/structure-complex-nlayers.md) | Stronger boundaries; more projects and coordination |
| Different read/write models or cross-cutting request pipeline | [CQRS](blueprints/template-cqrs.md) | Explicit handlers; higher conceptual and operational cost |
| Rich domain language and invariants | [DDD](blueprints/template-ddd.md) | Better domain model; requires domain expertise |
| Many external adapters or replaceable delivery mechanisms | [Hexagonal](blueprints/template-hexagonal.md) | Isolation; more interfaces and mapping |
| Strict framework independence | [Clean Architecture](blueprints/template-clean-architecture.md) | Enforced dependency rule; additional indirection |
| Asynchronous workflows and integration events | [Event-Driven](blueprints/template-event-driven.md) | Loose coupling; eventual consistency and delivery concerns |
| Independent deployment and team ownership are required | [Microservices](blueprints/template-microservices.md) | Autonomy; distributed-system complexity |

## Cross-cutting choices

- **Persistence:** begin with [relational EF Core](../../database/relational.md) or [MongoDB](../../database/nosql.md); use read/write splitting only for a demonstrated need.
- **In-process requests:** use the [Mvp24Hours Mediator](../../cqrs/getting-started.md), not MediatR APIs.
- **Messaging:** use [RabbitMQ](../../broker.md) for asynchronous service integration and account for retries, idempotency, and inbox/outbox.
- **Observability:** use the [OpenTelemetry-first observability stack](../../observability/home.md).
- **Resilience:** select policies by dependency type in the [Resilience Selection Guide](../../modernization/resilience-guide.md).
- **Deployment:** use [Containerization](../deployment/containerization.md) and consider [.NET Aspire](../../modernization/aspire.md) for local orchestration and service defaults.

## Avoid premature escalation

Do not choose microservices only for code organization, CQRS only to wrap CRUD, or event-driven integration where a transactionally consistent call is required. A modular monolith can preserve boundaries while avoiding distributed transactions, message delivery, and multi-service operations.

## Runnable samples

Each row in the [decision matrix](decision-matrix.md) maps to a runnable sample under [`samples/`](../../../../samples/README.md). Start with the [“Which sample should I open first?”](../../../../samples/README.md#which-sample-should-i-open-first) table when you want a concrete project to clone rather than a blueprint alone.
