# Application Layer Template

The Application project orchestrates use cases, coordinates transactions, and exposes application services or mediator handlers.

## Folder layout

```text
{Product}.Application/
├── Services/
│   └── {Entity}Facade.cs
├── DTOs/
├── Mapping/
├── Validation/
└── Features/          # CQRS: Commands/, Queries/, Behaviors/
    └── {Feature}/
        ├── Commands/
        └── Queries/
```

## Naming conventions

- Facade: `{Entity}Facade.cs` for service-style APIs
- Commands: `{Verb}{Entity}Command.cs` with `{Verb}{Entity}CommandHandler.cs`
- Queries: `Get{Entity}By{Criteria}Query.cs`
- Behaviors: `{Name}Behavior.cs` implementing pipeline behaviors

## Dependency rule

Application depends on **Core or Domain only**. It must **not** reference Infrastructure or WebAPI.

## DI registration snippet

```csharp
services.AddScoped<I{Entity}Facade, {Entity}Facade>();
// CQRS:
services.AddMvpMediator(typeof({Product}ApplicationAssemblyMarker).Assembly);
```

## Canonical sample

[`complex-crud-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-crud-ef-customer-api) — `CustomerAPI.Application` with Facade and application services.

CQRS reference: [`complex-cqrs-ef-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-cqrs-ef-customer-api).

## Related documentation

- [Application Services](../../application-services.md)
- [CQRS Getting Started](../../cqrs/getting-started.md)
