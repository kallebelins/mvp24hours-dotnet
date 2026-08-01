# Domain Layer Template

Use a Domain project instead of Core when applying Clean Architecture or DDD blueprints. The Domain layer is the innermost ring with zero outward dependencies.

## Folder layout

```text
{Product}.Domain/
├── Entities/
├── Aggregates/
├── ValueObjects/
├── Events/
├── Specifications/
└── Exceptions/
```

## Naming conventions

- Aggregates: `{Entity}Aggregate.cs` or `{Entity}.cs` with explicit aggregate root marker
- Domain events: `{Entity}{PastTense}Event.cs` (e.g. `CustomerRegisteredEvent.cs`)
- Domain exceptions: `{Entity}{Reason}Exception.cs`

## Dependency rule

Domain references **nothing** from Application, Infrastructure, WebAPI, or delivery frameworks.

## DI registration

Domain types are not registered directly; Application handlers and Infrastructure adapters consume them.

## Canonical samples

- [`complex-clean-architecture-customer-api`](../../../../samples/src/complex-clean-architecture-customer-api) — inward dependency rule
- [`complex-ddd-ef-customer-api`](../../../../samples/src/complex-ddd-ef-customer-api) — aggregates, value objects, domain events

## Related documentation

- [Core & Domain](../../core/home.md)
- [DDD Blueprint](../../guides/architecture/blueprints/template-ddd.md)
- [Clean Architecture Blueprint](../../guides/architecture/blueprints/template-clean-architecture.md)
