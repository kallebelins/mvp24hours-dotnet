# Project Structure

Project boundaries should express dependency direction and ownership. Folder names are examples; keep a structure consistent within a solution.

## Recommended dependency flow

```text
WebAPI / Worker
        |
        v
Application
        |
        v
Core / Domain
        ^
        |
Infrastructure (implements ports and is composed at the host)
```

- **Core/Domain** owns entities, value objects, domain rules, specifications, and dependency-free contracts.
- **Application** owns use cases, mediator handlers, validation, mapping, and transaction coordination.
- **Infrastructure** owns EF Core/MongoDB, RabbitMQ, cache, files, email, SMS, locks, and external adapters.
- **Hosts** own composition, HTTP/background endpoints, configuration, middleware, and deployment.
- **Tests** mirror the boundary they verify and separate unit from integration scenarios.

## Conventions

- Target `net10.0` for the current source line and enable nullable reference types.
- Keep secrets out of source and bind validated options at the composition root.
- Prefer `Program.cs` and focused `IServiceCollection` extensions; a legacy `Startup.cs` is not required.
- Use `TimeProvider`, structured `ILogger<T>`, OpenTelemetry, health checks, and cancellation tokens.
- Keep contracts close to their owner. Share integration contracts deliberately; do not create a universal shared-domain project.
- Reference package versions through the solution's package-management policy. Do not copy stale `9.*` pins from old examples.

## Select a concrete structure

- [Architecture Guides overview](home.md)
- [Decision Matrix](decision-matrix.md)
- [Minimal API](structures/structure-minimal-api.md)
- [Simple N-Layers](structures/structure-simple-nlayers.md)
- [Complex N-Layers](structures/structure-complex-nlayers.md)
- [CQRS](blueprints/template-cqrs.md)
- [Event-Driven](blueprints/template-event-driven.md)
- [Hexagonal](blueprints/template-hexagonal.md)
- [Clean Architecture](blueprints/template-clean-architecture.md)
- [Domain-Driven Design](blueprints/template-ddd.md)
- [Microservices](blueprints/template-microservices.md)
- [Containerization](../deployment/containerization.md)

For exact entity, repository, unit-of-work, and service APIs, use the [Data & Persistence](../../database/relational.md) and [Application Services](../../application-services.md) documentation.
