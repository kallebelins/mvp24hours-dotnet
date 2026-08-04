# Complex N-Layers template

Compilable Complex N-Layers scaffold with a placeholder `Item` resource. Copy, rename `App` / `Item`, and implement your domain.

## Architecture

- Tier: Complex
- Shape: Core → Application → Infrastructure → WebAPI (Facade + application services)
- Persistence: EF Core **InMemory** by default (swap to SQL Server for production)

## Layers

- `App.Core` — entities, DTOs, validators, contracts
- `App.Application` — `ItemService`, `FacadeService`
- `App.Infrastructure` — `EFDBContext`, Fluent API configuration
- `App.WebAPI` — controllers, DI, OpenAPI, health
- `App.Test` — smoke tests

## Production baseline included

- Native OpenAPI
- FluentValidation registration
- Keycloak baseline (authentication and authorization pipeline)
- Request observability middleware
- Hybrid cache registration
- Resilient HttpClient defaults
- HTTP middleware hardening: rate limiting, idempotency, and output cache
- Health checks (`self` + Keycloak)

These HTTP middleware features are configurable through `HttpHardening` in `App.WebAPI/appsettings*.json`.

## Local dependencies

Start required services from this folder:

```bash
docker compose up -d
```

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

- Teaching sample: [`samples/src/complex-crud-ef-customer-api`](../../../samples/src/complex-crud-ef-customer-api)
- Docs: [Complex N-Layers](../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- Host shortcut: [`hosts/api-complex-nlayers`](../../hosts/api-complex-nlayers)
