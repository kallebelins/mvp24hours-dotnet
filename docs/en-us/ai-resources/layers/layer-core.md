# Core Layer Template

The Core project owns domain-neutral business concepts that do not depend on persistence, HTTP, or external SDKs.

## Folder layout

```text
{Product}.Core/
├── Entities/
├── ValueObjects/
├── Specifications/
├── Validations/
└── Contracts/
    ├── Repositories/
    └── Services/
```

## Naming conventions

- Entities: `{Entity}.cs` (e.g. `Customer.cs`)
- Value objects: `{Name}ValueObject.cs` or record types in `ValueObjects/`
- Specifications: `{Entity}By{Criteria}Specification.cs`
- Repository contracts: `I{Entity}Repository.cs` in `Contracts/Repositories/`

## Dependency rule

Core references **no other solution projects**. It may reference Mvp24Hours core packages only.

## DI registration

Core types are typically registered from Infrastructure or Application; Core itself does not call `Add*` on the service collection.

## Canonical sample

[`complex-crud-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-crud-ef-customer-api) — DTOs, validators, specifications, and repository contracts in `CustomerAPI.Core`.

## Related documentation

- [Core & Domain](../../core/home.md)
- [Specification pattern](../../core/specification.md)
- [Entity interfaces](../../core/entity-interfaces.md)
