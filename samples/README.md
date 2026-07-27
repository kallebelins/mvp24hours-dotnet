# Mvp24Hours samples for .NET 10

These samples demonstrate Mvp24Hours with relational and NoSQL databases, Redis, RabbitMQ, pipelines, validation, logging, and several application architecture tiers. The catalog is being modernized for `net10.0`; use each entry's status to distinguish migrated examples from planned work.

Start with the [architecture guidance](../docs/en-us/guides/architecture/home.md) and [decision matrix](../docs/en-us/guides/architecture/decision-matrix.md) if you are unsure which tier fits your application.

## Package consumption

This repository uses the current projects under [`src/`](../src/) by default. [`Directory.Build.props`](Directory.Build.props) sets `Mvp24HoursUseProjectReferences=true`, and each sample maps its Mvp24Hours dependencies to those source projects.

For a standalone checkout after matching packages are published, switch to NuGet:

```bash
dotnet build -p:Mvp24HoursUseProjectReferences=false -p:Mvp24HoursPackageVersion=10.0.0
```

[`Directory.Packages.props`](Directory.Packages.props) imports shared versions from [`src/Directory.Packages.props`](../src/Directory.Packages.props) and adds sample-only packages. Never mix Mvp24Hours 4.x, 8.x, 9.x, and 10.x packages in one sample. The source and published-package modes are alternatives; do not enable both.

## Repository conventions

- All sample projects inherit `net10.0`, latest C#, nullable reference types, implicit usings, and the repository analyzers from the files in this directory.
- Follow the root [`.editorconfig`](../.editorconfig): use file-scoped namespaces, clear primary constructors where they improve readability, and the existing naming and formatting rules.
- Prefer validated options (`ValidateOnStart` or `IValidateOptions<T>`), `TimeProvider`, `ILogger<T>`, cancellation tokens on public asynchronous APIs, and ProblemDetails-friendly errors.
- Do not call `BuildServiceProvider()` from registration extensions, instantiate `HttpClient` directly, commit secrets, or introduce a separate sample style guide.
- New and migrated hosts should base their documentation on the [sample README template](templates/SAMPLE_README.template.md). Sample code, comments, and documentation are English.

Status: **Migrated** is ready on the .NET 10 patterns; **Planned** exists but still needs its catalog migration phase (or has not been created); **Deprecated** is retained only as a historical reference.

## Minimal

- **Planned** — [CRUD with EF Core](src/minimal-crud-ef-customer-api/CustomerAPI/): lean relational CRUD and pagination.
- **Planned** — [CRUD with MongoDB](src/minimal-crud-mongodb-customer-api/CustomerAPI/): lean document-database CRUD and pagination.
- **Planned** — [Pipeline](src/minimal-pipeline-customer-api/CustomerAPI/): pipeline operations in a Minimal API host.

## Simple

- **Planned** — [CRUD with EF Core](src/simple-crud-ef-customer-api/): simple N-layer relational CRUD.
- **Planned** — [CRUD with EF Core and Dapper](src/simple-crud-ef-dapper-customer-api/): EF writes with Dapper reads.
- **Planned** — [CRUD with EF Core entity logging](src/simple-crud-ef-entitylog-customer-api/): auditing and soft-delete fields.
- **Planned** — [CRUD with MongoDB](src/simple-crud-mongodb-customer-api/): simple N-layer MongoDB CRUD.
- **Planned** — [CRUD with Redis](src/simple-crud-redis-customer-api/): key-value persistence and caching concepts.
- **Planned** — [RabbitMQ](src/simple-rabbitmq-customer-api/): asynchronous customer operations through RabbitMQ.
- **Planned** — [Pipeline](src/simple-pipeline-customer-api/): layered pipeline operations.
- **Planned** — `simple-webstatus`: health monitoring catalog; the old external-only link remains removed until the sample is recreated.

## Complex

- **Planned** — [CRUD with EF Core](src/complex-crud-ef-customer-api/): DTO-based relational CRUD with stronger boundaries.
- **Planned** — [CRUD with EF Core and Dapper](src/complex-crud-ef-dapper-customer-api/): separated write and read persistence.
- **Deprecated** — [CRUD using EF entities as API contracts](src/complex-crud-ef-only-entity-customer-api/): retained to explain why public APIs should not leak persistence entities.
- **Planned** — [CRUD with EF Core entity logging](src/complex-crud-ef-entitylog-customer-api/): DTO boundaries, auditing, and soft delete.
- **Planned** — [CRUD with MongoDB](src/complex-crud-mongodb-customer-api/): complex-tier document persistence.
- **Planned** — [Pipeline](src/complex-pipeline-customer-api/): layered pipeline operations.
- **Planned** — [Pipeline builder](src/complex-pipeline-builder-customer-api/): constructor-composed use-case pipelines.
- **Planned** — [Pipeline with ports and adapters](src/complex-pipeline-ports-adapters-customer-api/): pipeline-centric hexagonal architecture.
- **Planned** — [Pipeline with EF Core](src/complex-pipeline-ef-customer-api/): integration pipeline with relational persistence.

## Architecture blueprints

- **Planned** — `complex-cqrs-ef-customer-api`: CQRS with the Mvp24Hours Mediator.
- **Planned** — `complex-ddd-ef-customer-api`: Domain-Driven Design with aggregates and domain events.
- **Planned** — `complex-clean-architecture-customer-api`: Clean Architecture dependency boundaries.
- **Planned** — `complex-hexagonal-customer-api`: first-class ports and adapters.
- **Planned** — `complex-event-driven-rabbitmq-customer-api`: event-driven processing with inbox and outbox.
- **Planned** — `microservices-aspire-customer`: multiple services composed with .NET Aspire.

## Capability samples

- **Planned** — `simple-webstatus`: health checks and monitoring.
- **Planned** — `complex-keycloak-customer-api`: Keycloak identity and admin operations.
- **Planned** — `simple-cronjob-worker`: scheduled hosted jobs.
- **Planned** — `simple-observability-customer-api`: OpenTelemetry logs, traces, and metrics.
- **Planned** — `simple-hybridcache-rate-limit-api`: HybridCache and rate limiting.
- **Planned** — `complex-saga-rabbitmq-customer-api`: saga coordination and compensation.
- **Planned** — `complex-event-sourcing-customer-api`: event sourcing reference, conditional on stable library APIs.

## Community

See the [documentation site](https://kallebelins.github.io/mvp24hours-dotnet/) to study, share feedback, and contribute.
