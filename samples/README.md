# Mvp24Hours samples for .NET 10

These samples demonstrate Mvp24Hours with relational and NoSQL databases, Redis, RabbitMQ, pipelines, validation, logging, identity, observability, and several application architecture tiers. All **32** runnable solutions target `net10.0` and use local project references by default.

Start with the [architecture guidance](../docs/en-us/guides/architecture/home.md) and [decision matrix](../docs/en-us/guides/architecture/decision-matrix.md) if you are unsure which tier fits your application.

## Which sample should I open first?

Use constraints, not labels. The table below maps common starting situations to a single recommended sample; open the linked README for run instructions and scope notes.

| Your situation | Open first | Why |
| --- | --- | --- |
| Smallest HTTP CRUD on SQL Server | [minimal-crud-ef-customer-api](src/minimal-crud-ef-customer-api/CustomerAPI/README.md) | Lean Minimal API host with EF Core, pagination, and TypedResults |
| Conventional layered CRUD (entities may appear in API) | [simple-crud-ef-customer-api](src/simple-crud-ef-customer-api/CustomerAPI.WebAPI/README.md) | Simple N-layers with explicit public-API trade-off guidance |
| Production-style CRUD with DTOs and validation | [complex-crud-ef-customer-api](src/complex-crud-ef-customer-api/CustomerAPI.WebAPI/README.md) | Canonical Complex EF reference: DTOs, specs, UoW, Facade |
| Commands and queries with mediator pipelines | [complex-cqrs-ef-customer-api](src/complex-cqrs-ef-customer-api/CustomerAPI.WebAPI/README.md) | Feature folders, behaviors, notifications — not MediatR |
| Rich domain invariants and domain events | [complex-ddd-ef-customer-api](src/complex-ddd-ef-customer-api/README.md) | Aggregates, value objects, specifications in one bounded context |
| Strict inward dependency rule | [complex-clean-architecture-customer-api](src/complex-clean-architecture-customer-api/README.md) | Domain ← Application ← Infrastructure with dependency diagram |
| Replaceable external adapters | [complex-hexagonal-customer-api](src/complex-hexagonal-customer-api/README.md) | Explicit inbound/outbound ports with EF and resilient HTTP |
| Guaranteed messaging with outbox/inbox | [complex-event-driven-rabbitmq-customer-api](src/complex-event-driven-rabbitmq-customer-api/README.md) | Durable outbox, RabbitMQ publish, consumer idempotency |
| Local multi-service orchestration | [microservices-aspire-customer](src/microservices-aspire-customer/README.md) | Customer API + Notification worker with Aspire AppHost |
| Pipeline / pipes-and-filters | [minimal-pipeline-customer-api](src/minimal-pipeline-customer-api/CustomerAPI/README.md) (minimal) or [complex-pipeline-builder-customer-api](src/complex-pipeline-builder-customer-api/CustomerAPI.WebAPI/README.md) (Complex) | Start minimal; use builder sample for DI-friendly composition |
| RabbitMQ basics (at-least-once) | [simple-rabbitmq-customer-api](src/simple-rabbitmq-customer-api/CustomerAPI.WebAPI/README.md) | Direct publish/consume with delivery-semantics notes |
| Health checks across dependencies | [simple-webstatus](src/simple-webstatus/WebStatus/README.md) | SQL, PostgreSQL, MySQL, Redis, MongoDB, RabbitMQ catalog |
| JWT + Keycloak Admin flows | [complex-keycloak-customer-api](src/complex-keycloak-customer-api/CustomerAPI.WebAPI/README.md) | Bearer validation and Admin API without Duende/IdentityModel |
| Scheduled background jobs | [simple-cronjob-worker](src/simple-cronjob-worker/README.md) | Basic and resilient CronJob services with health hooks |
| OpenTelemetry end-to-end | [simple-observability-customer-api](src/simple-observability-customer-api/README.md) | Logs, traces, metrics with OTLP exporters |
| HybridCache + rate limiting | [simple-hybridcache-rate-limit-api](src/simple-hybridcache-rate-limit-api/README.md) | Stampede-safe reads and sliding-window 429 protection |
| Saga with compensation | [complex-saga-rabbitmq-customer-api](src/complex-saga-rabbitmq-customer-api/README.md) | Orchestrated onboarding with compensating steps |
| Event sourcing fundamentals | [complex-event-sourcing-customer-api](src/complex-event-sourcing-customer-api/README.md) | In-memory store and projection (**preview** — no durable library store yet) |

For a side-by-side comparison of architecture shapes, see the [decision matrix](../docs/en-us/guides/architecture/decision-matrix.md).

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
- Phase 5–6 blueprint and capability samples ship with xUnit + FluentAssertions test projects. See [Sample testing baseline](TESTING.md) (including [Testcontainers guides](TESTING.md#testcontainers)) and copy from [`templates/SAMPLE_TEST*.template`](templates/).

Status: **Migrated** is ready on the .NET 10 patterns; **Deprecated** is retained only as a historical reference; **Preview** documents library gaps explicitly.

## Complete catalog

| Sample | Tier | Status | Purpose | Primary documentation |
| --- | --- | --- | --- | --- |
| [minimal-crud-ef-customer-api](src/minimal-crud-ef-customer-api/CustomerAPI/README.md) | Minimal | Migrated | Paged Customer CRUD with EF Core in a single Minimal API host | [Minimal API structure](../docs/en-us/guides/architecture/structures/structure-minimal-api.md), [Relational database](../docs/en-us/database/relational.md) |
| [minimal-crud-mongodb-customer-api](src/minimal-crud-mongodb-customer-api/CustomerAPI/README.md) | Minimal | Migrated | Paged document CRUD with MongoDB repository patterns | [Minimal API structure](../docs/en-us/guides/architecture/structures/structure-minimal-api.md), [MongoDB](../docs/en-us/database/nosql.md) |
| [minimal-pipeline-customer-api](src/minimal-pipeline-customer-api/CustomerAPI/README.md) | Minimal | Migrated | Cancelable pipeline operations in a Minimal API host | [Pipeline](../docs/en-us/pipeline.md) |
| [simple-crud-ef-customer-api](src/simple-crud-ef-customer-api/CustomerAPI.WebAPI/README.md) | Simple | Migrated | Simple N-layer relational CRUD with entity-as-contract teaching boundary | [Simple N-Layers](../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md), [Repository](../docs/en-us/database/use-repository.md) |
| [simple-crud-ef-dapper-customer-api](src/simple-crud-ef-dapper-customer-api/CustomerAPI.WebAPI/README.md) | Simple | Migrated | EF writes with cancelable Dapper reads on a shared connection | [Simple N-Layers](../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md), [EF Core Advanced](../docs/en-us/database/efcore-advanced.md) |
| [simple-crud-ef-entitylog-customer-api](src/simple-crud-ef-entitylog-customer-api/CustomerAPI.WebAPI/README.md) | Simple | Migrated | Auditing fields and soft-delete filters with `TimeProvider` | [Entity interfaces](../docs/en-us/core/entity-interfaces.md), [Use entity](../docs/en-us/database/use-entity.md) |
| [simple-crud-mongodb-customer-api](src/simple-crud-mongodb-customer-api/CustomerAPI.WebAPI/README.md) | Simple | Migrated | Simple N-layer MongoDB CRUD and modeling guidance | [MongoDB](../docs/en-us/database/nosql.md) |
| [simple-crud-redis-customer-api](src/simple-crud-redis-customer-api/CustomerAPI.WebAPI/README.md) | Simple | Migrated | Key-value persistence and cache-aside guidance | [Caching advanced](../docs/en-us/caching-advanced.md) |
| [simple-rabbitmq-customer-api](src/simple-rabbitmq-customer-api/CustomerAPI.WebAPI/README.md) | Simple | Migrated | Asynchronous customer operations with at-least-once delivery notes | [RabbitMQ](../docs/en-us/broker.md), [Integration](../docs/en-us/cqrs/integration-rabbitmq.md) |
| [simple-pipeline-customer-api](src/simple-pipeline-customer-api/CustomerAPI.WebAPI/README.md) | Simple | Migrated | Layered, resilient, cancelable pipeline operations | [Pipeline](../docs/en-us/pipeline.md) |
| [simple-webstatus](src/simple-webstatus/WebStatus/README.md) | Simple | Migrated | Multi-dependency health catalog with HealthChecks UI | [Health checks](../docs/en-us/infrastructure/health-checks.md) |
| [complex-crud-ef-customer-api](src/complex-crud-ef-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | **Canonical Complex EF** — DTOs, validation, specs, UoW, Facade | [Complex N-Layers](../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md), [Application services](../docs/en-us/application-services.md) |
| [complex-crud-ef-dapper-customer-api](src/complex-crud-ef-dapper-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | Complex boundaries with EF writes and Dapper reads | [Unit of Work](../docs/en-us/database/use-unitofwork.md) |
| [complex-crud-ef-only-entity-customer-api](src/complex-crud-ef-only-entity-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Deprecated | Teaching sample: why public APIs should not leak persistence entities | [Decision matrix](../docs/en-us/guides/architecture/decision-matrix.md) |
| [complex-crud-ef-entitylog-customer-api](src/complex-crud-ef-entitylog-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | DTO boundaries with audit columns and soft-delete filters | [Entity interfaces](../docs/en-us/core/entity-interfaces.md) |
| [complex-crud-mongodb-customer-api](src/complex-crud-mongodb-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | Complex-tier MongoDB with DTOs, validators, and specifications | [MongoDB advanced](../docs/en-us/database/mongodb-advanced.md) |
| [complex-pipeline-customer-api](src/complex-pipeline-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | Layered pipeline operations with break-on-fail | [Pipeline](../docs/en-us/pipeline.md) |
| [complex-pipeline-builder-customer-api](src/complex-pipeline-builder-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | Constructor-composed, DI-friendly use-case pipelines | [Pipeline](../docs/en-us/pipeline.md) |
| [complex-pipeline-ports-adapters-customer-api](src/complex-pipeline-ports-adapters-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | Pipeline-centric hexagonal teaching sample with resilient HTTP | [Hexagonal blueprint](../docs/en-us/guides/architecture/blueprints/template-hexagonal.md) |
| [complex-pipeline-ef-customer-api](src/complex-pipeline-ef-customer-api/CustomerAPI.WebAPI/README.md) | Complex | Migrated | Integration pipeline fetching remote data and persisting with EF + UoW | [Pipeline](../docs/en-us/pipeline.md), [Relational database](../docs/en-us/database/relational.md) |
| [complex-cqrs-ef-customer-api](src/complex-cqrs-ef-customer-api/CustomerAPI.WebAPI/README.md) | Blueprint | Migrated | CQRS commands, queries, behaviors, and in-process notifications | [CQRS blueprint](../docs/en-us/guides/architecture/blueprints/template-cqrs.md), [CQRS getting started](../docs/en-us/cqrs/getting-started.md) |
| [complex-ddd-ef-customer-api](src/complex-ddd-ef-customer-api/README.md) | Blueprint | Migrated | Rich Customer aggregate with value objects and domain events | [DDD blueprint](../docs/en-us/guides/architecture/blueprints/template-ddd.md) |
| [complex-clean-architecture-customer-api](src/complex-clean-architecture-customer-api/README.md) | Blueprint | Migrated | Inward dependency rule across Domain, Application, Infrastructure, WebAPI | [Clean Architecture blueprint](../docs/en-us/guides/architecture/blueprints/template-clean-architecture.md) |
| [complex-hexagonal-customer-api](src/complex-hexagonal-customer-api/README.md) | Blueprint | Migrated | Explicit inbound/outbound ports with EF and Typicode HTTP adapters | [Hexagonal blueprint](../docs/en-us/guides/architecture/blueprints/template-hexagonal.md) |
| [complex-event-driven-rabbitmq-customer-api](src/complex-event-driven-rabbitmq-customer-api/README.md) | Blueprint | Migrated | Durable outbox, RabbitMQ publish, inbox idempotency, correlation IDs | [Event-driven blueprint](../docs/en-us/guides/architecture/blueprints/template-event-driven.md), [Inbox/Outbox](../docs/en-us/cqrs/resilience/inbox-outbox.md) |
| [microservices-aspire-customer](src/microservices-aspire-customer/README.md) | Blueprint | Migrated | Customer API + Notification worker composed by Aspire AppHost | [Microservices blueprint](../docs/en-us/guides/architecture/blueprints/template-microservices.md), [Aspire](../docs/en-us/modernization/aspire.md) |
| [complex-keycloak-customer-api](src/complex-keycloak-customer-api/CustomerAPI.WebAPI/README.md) | Capability | Migrated | JWT bearer validation and Keycloak Admin API flows | [Keycloak identity](../docs/en-us/identity/keycloak.md) |
| [simple-cronjob-worker](src/simple-cronjob-worker/README.md) | Capability | Migrated | Scheduled jobs with resilience and observability hooks | [CronJob](../docs/en-us/cronjob.md) |
| [simple-observability-customer-api](src/simple-observability-customer-api/README.md) | Capability | Migrated | OpenTelemetry logs, traces, and metrics with OTLP exporters | [Observability](../docs/en-us/observability/home.md) |
| [simple-hybridcache-rate-limit-api](src/simple-hybridcache-rate-limit-api/README.md) | Capability | Migrated | HybridCache stampede protection and sliding-window rate limiting | [HybridCache](../docs/en-us/modernization/hybrid-cache.md), [Rate limiting](../docs/en-us/modernization/rate-limiting.md) |
| [complex-saga-rabbitmq-customer-api](src/complex-saga-rabbitmq-customer-api/README.md) | Capability | Migrated | Orchestrated onboarding saga with compensating steps | [Saga](../docs/en-us/cqrs/saga/home.md) |
| [complex-event-sourcing-customer-api](src/complex-event-sourcing-customer-api/README.md) | Capability | Preview | In-memory aggregate, event store, projection, and rehydration endpoint | [Event sourcing](../docs/en-us/cqrs/event-sourcing/home.md) |

## Community

See the [documentation site](https://kallebelins.github.io/mvp24hours-dotnet/) to study, share feedback, and contribute.
