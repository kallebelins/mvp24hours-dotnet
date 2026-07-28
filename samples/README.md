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

- **Migrated** — [CRUD with EF Core](src/minimal-crud-ef-customer-api/CustomerAPI/): lean relational CRUD and pagination.
- **Migrated** — [CRUD with MongoDB](src/minimal-crud-mongodb-customer-api/CustomerAPI/): lean document-database CRUD and pagination.
- **Migrated** — [Pipeline](src/minimal-pipeline-customer-api/CustomerAPI/): pipeline operations in a Minimal API host.

## Simple

- **Migrated** — [CRUD with EF Core](src/simple-crud-ef-customer-api/CustomerAPI.WebAPI/): simple N-layer relational CRUD.
- **Migrated** — [CRUD with EF Core and Dapper](src/simple-crud-ef-dapper-customer-api/CustomerAPI.WebAPI/): EF writes with cancelable Dapper reads.
- **Migrated** — [CRUD with EF Core entity logging](src/simple-crud-ef-entitylog-customer-api/CustomerAPI.WebAPI/): auditing and soft-delete fields.
- **Migrated** — [CRUD with MongoDB](src/simple-crud-mongodb-customer-api/CustomerAPI.WebAPI/): simple N-layer document CRUD and modeling guidance.
- **Migrated** — [CRUD with Redis](src/simple-crud-redis-customer-api/CustomerAPI.WebAPI/): key-value persistence and cache-aside guidance.
- **Migrated** — [RabbitMQ](src/simple-rabbitmq-customer-api/CustomerAPI.WebAPI/): asynchronous customer operations with retry and delivery-semantics guidance.
- **Migrated** — [Pipeline](src/simple-pipeline-customer-api/CustomerAPI.WebAPI/): layered, resilient, cancelable pipeline operations.
- **Migrated** — [WebStatus / Health Checks](src/simple-webstatus/WebStatus/): multi-dependency health catalog with HealthChecks UI.

## Complex

- **Migrated** — [CRUD with EF Core](src/complex-crud-ef-customer-api/CustomerAPI.WebAPI/): DTO-based relational CRUD with stronger boundaries.
- **Migrated** — [CRUD with EF Core and Dapper](src/complex-crud-ef-dapper-customer-api/CustomerAPI.WebAPI/): EF writes with cancelable Dapper reads.
- **Deprecated** — [CRUD using EF entities as API contracts](src/complex-crud-ef-only-entity-customer-api/CustomerAPI.WebAPI/): migrated teaching sample retained to explain why public APIs should not leak persistence entities.
- **Migrated** — [CRUD with EF Core entity logging](src/complex-crud-ef-entitylog-customer-api/CustomerAPI.WebAPI/): DTO boundaries, auditing, and soft delete.
- **Migrated** — [CRUD with MongoDB](src/complex-crud-mongodb-customer-api/CustomerAPI.WebAPI/): complex-tier document persistence and modeling guidance.
- **Migrated** — [Pipeline](src/complex-pipeline-customer-api/CustomerAPI.WebAPI/): layered, cancelable pipeline operations.
- **Migrated** — [Pipeline builder](src/complex-pipeline-builder-customer-api/CustomerAPI.WebAPI/): constructor-composed, DI-friendly use-case pipelines.
- **Migrated** — [Pipeline with ports and adapters](src/complex-pipeline-ports-adapters-customer-api/CustomerAPI.WebAPI/): pipeline-centric hexagonal teaching sample with resilient HTTP.
- **Migrated** — [Pipeline with EF Core](src/complex-pipeline-ef-customer-api/CustomerAPI.WebAPI/): integration pipeline with relational persistence and clear UoW boundaries.

## Architecture blueprints

- **Migrated** — [CQRS with EF Core](src/complex-cqrs-ef-customer-api/CustomerAPI.WebAPI/): commands, queries, and notifications with the Mvp24Hours Mediator.
- **Migrated** — [Domain-Driven Design](src/complex-ddd-ef-customer-api/): aggregates, value objects, domain events, and specifications.
- **Migrated** — [Clean Architecture](src/complex-clean-architecture-customer-api/): inward dependency rule across Domain, Application, Infrastructure, and WebAPI.
- **Migrated** — [Hexagonal / ports and adapters](src/complex-hexagonal-customer-api/): inbound HTTP and outbound EF + resilient HTTP adapters (sibling of the pipeline ports-adapters sample).
- **Migrated** — [Event-driven with RabbitMQ](src/complex-event-driven-rabbitmq-customer-api/): durable outbox, publish, consumer inbox/idempotency, and correlation IDs.
- **Migrated** — [Microservices with .NET Aspire](src/microservices-aspire-customer/): Customer API + Notification worker composed by an Aspire AppHost.

## Capability samples

- **Migrated** — [WebStatus / Health Checks](src/simple-webstatus/WebStatus/): SQL Server, PostgreSQL, MySQL, Redis, MongoDB, and RabbitMQ health catalog.
- **Migrated** — [Keycloak identity](src/complex-keycloak-customer-api/CustomerAPI.WebAPI/): JWT bearer validation and Admin API flows (no Duende/IdentityModel).
- **Migrated** — [CronJob worker](src/simple-cronjob-worker/CronJobWorker/): scheduled jobs with resilience and health hooks.
- **Migrated** — [Observability](src/simple-observability-customer-api/CustomerAPI.WebAPI/): OpenTelemetry logs, traces, and metrics with OTLP exporters.
- **Migrated** — [HybridCache + Rate Limiting](src/simple-hybridcache-rate-limit-api/ProductAPI.WebAPI/): stampede-safe caching and abusive-client protection.
- **Migrated** — [Saga + compensation](src/complex-saga-rabbitmq-customer-api/): orchestrated multi-step process with compensating actions.
- **Migrated (preview)** — [Event Sourcing](src/complex-event-sourcing-customer-api/): aggregate, in-memory event store, projection, and snapshots (no durable store yet).

## Community

See the [documentation site](https://kallebelins.github.io/mvp24hours-dotnet/) to study, share feedback, and contribute.
