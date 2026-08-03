# Architecture Guides

Use these guides to choose a solution shape before selecting implementation details. They cover architecture decisions, project boundaries, and Mvp24Hours-aligned blueprints; canonical module pages remain the source of truth for packages, APIs, options, defaults, and DI registration.

## Choose a starting point

| Need | Start here |
|------|------------|
| Compare solution shapes | [Decision Matrix](decision-matrix.md) |
| Define projects and dependency direction | [Project Structure](project-structure.md) |
| Build a small HTTP service | [Minimal API](structures/structure-minimal-api.md) |
| Separate a conventional application into layers | [Simple N-Layers](structures/structure-simple-nlayers.md) |
| Organize a large modular application | [Complex N-Layers](structures/structure-complex-nlayers.md) |
| Separate commands and queries | [CQRS](blueprints/template-cqrs.md) |
| Publish and consume business events | [Event-Driven](blueprints/template-event-driven.md) |
| Isolate external systems behind ports | [Hexagonal](blueprints/template-hexagonal.md) |
| Enforce inward dependency flow | [Clean Architecture](blueprints/template-clean-architecture.md) |
| Model a complex business domain | [Domain-Driven Design](blueprints/template-ddd.md) |
| Split independently deployable services | [Microservices](blueprints/template-microservices.md) |
| Package and deploy the host | [Containerization](../deployment/containerization.md) |
| Copy a compilable scaffold | [Scaffolding templates](scaffolding-templates.md) |

## Scaffolding templates

Use [Scaffolding templates](scaffolding-templates.md) when you need a **compilable** starting solution (placeholder `Item`, `App.*` projects). Use [`samples/`](../../../samples/README.md) when you need a full teaching scenario.

- Blueprints: Complex N-Layers, Clean Architecture, Hexagonal, CQRS, DDD, Event-Driven
- Hosts: API (via Complex N-Layers), BFF, Azure Functions (minimal/simple/complex), Workers (minimal/simple/complex)

## Apply a guide

1. Start with the simplest structure that satisfies the deployment and domain constraints.
2. Add CQRS, messaging, or domain patterns only when their costs solve a concrete problem.
3. Follow [Getting Started](../../getting-started.md) for package setup.
4. Use the [Configuration Reference](../../configuration-reference.md) and module pages for exact APIs.
5. Verify the result with the [Testing Cookbook](../../testing/home.md).

Architecture names do not prescribe every folder. Dependency direction, ownership, transaction boundaries, and deployment boundaries matter more than naming.

## Canonical implementation documentation

- [Core & Domain](../../core/home.md)
- [Data & Persistence](../../database/relational.md)
- [Application Services](../../application-services.md)
- [CQRS & Mediator](../../cqrs/home.md)
- [RabbitMQ](../../broker.md)
- [Infrastructure Modules](../../infrastructure/home.md)
- [Web API](../../webapi.md)
- [Observability](../../observability/home.md)
- [Resilience Selection Guide](../../modernization/resilience-guide.md)
- [Release Notes](../../release.md)
- [Version Migration](../../migration.md)

Machine-oriented downloads and external AI-framework material are listed separately in [AI & MCP Resources](../../ai-resources/home.md).
