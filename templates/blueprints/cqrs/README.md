# CQRS template

Compilable CQRS scaffold with a placeholder `Item` resource. Copy, rename `App` / `Item`, and implement your domain.

## Architecture

- Tier: Complex
- Shape: Core → Application (commands/queries) → Infrastructure → WebAPI
- Mediator: `Mvp24Hours.Infrastructure.Cqrs` with validation pipeline
- Persistence: EF Core **InMemory** by default (swap to SQL Server for production)

## Layers

- `App.Core` — entity, DTOs, entity validator
- `App.Application` — commands, queries, handlers, command validators
- `App.Infrastructure` — `EFDBContext`, Fluent API configuration
- `App.WebAPI` — controllers, mediator DI, OpenAPI, health
- `App.Test` — smoke tests

## Rename checklist

1. Rename projects/namespaces `App` → your service name
2. Rename `Item` → your entity
3. Replace InMemory with SQL Server (see ServiceBuilderExtensions)
4. Add real connection strings and health checks

## Run

```bash
dotnet run --project App.WebAPI
```

- OpenAPI: `http://localhost:5100/openapi/v1.json`
- Health: `http://localhost:5100/hc`

## Related

- Canonical N-Layers: [`../complex-nlayers`](../complex-nlayers)
- Docs: [CQRS](../../../docs/en-us/guides/architecture/structures/structure-cqrs.md)
