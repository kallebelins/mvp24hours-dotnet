---
templateId: ddd
tier: Blueprint
shape: blueprint
layers: [Domain, Application, Infrastructure, WebAPI]
dependencyRule: Domain <- Application <- Infrastructure; one bounded context per model
samplePath: samples/src/complex-ddd-ef-customer-api
mvp24hoursModules: [core, core/value-objects, core/entity-interfaces, specification, cqrs/domain-events]
---

# Domain-Driven Design Blueprint

Use DDD when domain complexity—not technical complexity—is the primary risk. Build a shared language with domain experts and organize the model around bounded contexts.

## Building blocks

- Entities and strongly typed identifiers for identity.
- Value objects for immutable concepts and validation.
- Aggregates for consistency boundaries and invariant enforcement.
- Domain services only for behavior that does not naturally belong to an entity/value object.
- Domain events for facts inside a bounded context.
- Repositories per aggregate boundary, not one repository per table.
- Integration events or explicit application contracts between bounded contexts.

Do not share one entity model across contexts. A bounded context may use a simple CRUD model while another uses rich aggregates.

Mvp24Hours provides entity interfaces, value-object helpers, smart enums, specifications, repositories, unit of work, and domain-event integration. Use the canonical pages for exact APIs.

See [Core & Domain](../../../core/home.md), [Value Objects](../../../core/value-objects.md), [Entity Interfaces](../../../core/entity-interfaces.md), [Specification](../../../specification.md), and [Domain Events](../../../cqrs/domain-events.md).

> **Sample:** [`complex-ddd-ef-customer-api`](../../../../../samples/src/complex-ddd-ef-customer-api/README.md) — Customer aggregate with value objects, domain events, and specifications in one bounded context.
