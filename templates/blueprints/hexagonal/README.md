# Hexagonal template

Compilable Hexagonal (Ports & Adapters) scaffold with a placeholder `Item` resource. Copy, rename `App` / `Item`, and implement your domain.

## Architecture

- Tier: Complex
- Shape: Core (entities + outbound ports) → Application (use cases + inbound ports) → Infrastructure (adapters) → WebAPI
- Persistence: EF Core **InMemory** by default (swap to SQL Server for production)
- **No repository/UoW** — application layer talks to outbound ports only; infrastructure provides EF adapters

## Layers

- `App.Core` — `Item` entity, `IItemReadPort`, `IItemWritePort`
- `App.Application` — DTOs, `IItemUseCase`, `ItemUseCase`
- `App.Infrastructure` — `EFDBContext`, `ItemEFAdapter` (implements both ports)
- `App.WebAPI` — DI wiring, controllers, OpenAPI, health
- `App.Test` — smoke tests

## Rename checklist

1. Rename projects/namespaces `App` → your service name
2. Rename `Item` → your entity
3. Add outbound ports per external system (email, cache, etc.)
4. Replace InMemory with SQL Server in `ServiceBuilderExtensions`

## Run

```bash
dotnet run --project App.WebAPI
```

- OpenAPI: `http://localhost:5101/openapi/v1.json`
- Health: `http://localhost:5101/hc`

## Related

- Docs: [Hexagonal architecture](../../../docs/en-us/guides/architecture/structures/)
