# Clean Architecture template

Compilable Clean Architecture scaffold with a placeholder `Item` resource. Copy, rename `App` / `Item`, and implement your domain.

## Architecture

- Tier: Complex
- Shape: Domain → Application → Infrastructure → WebAPI
- Dependency rule: **dependencies point inward** — Domain has no references to outer layers; WebAPI references Application and Infrastructure only (not Domain directly)
- Mediator: `Mvp24Hours.Infrastructure.Cqrs` with validation pipeline
- Persistence: EF Core **InMemory** by default (swap to SQL Server for production)

## Layers

- `App.Domain` — entities, domain validation
- `App.Application` — DTOs, commands, queries, handlers
- `App.Infrastructure` — `EFDBContext`, Fluent API configuration
- `App.WebAPI` — controllers, mediator DI, OpenAPI, health
- `App.Test` — smoke tests

## Production baseline included

The template includes a robust baseline using mvp24hours building blocks:

- Native OpenAPI
- FluentValidation + mediator validation behavior
- Keycloak authentication/authorization baseline
- HTTP request observability middleware
- Hybrid caching registration
- Resilient HttpClient registration
- HTTP middleware hardening: rate limiting, idempotency, and output cache
- Health checks (`self` + Keycloak)

These HTTP middleware features are configurable through `HttpHardening` in `App.WebAPI/appsettings*.json`.

Local dependencies can be started with:

```bash
docker compose up -d
```

## Inward dependency rule

```
WebAPI → Application, Infrastructure
Infrastructure → Domain
Application → Domain
Domain → (Mvp24Hours.Core only)
```

The WebAPI project must not reference `App.Domain` directly. Controllers depend on Application commands/queries and Infrastructure is wired through DI extension methods.

## Rename checklist

1. Rename projects/namespaces `App` → your service name
2. Rename `Item` → your entity
3. Replace InMemory with SQL Server (see ServiceBuilderExtensions)
4. Add real connection strings and health checks
5. Keep the inward dependency rule when adding new projects

## Run

```bash
dotnet run --project App.WebAPI
```

- OpenAPI: `http://localhost:5100/openapi/v1.json`
- Health: `http://localhost:5100/hc`

## Related

- CQRS variant: [`../cqrs`](../cqrs)
- Canonical N-Layers: [`../complex-nlayers`](../complex-nlayers)
- Docs: [Clean Architecture](../../../docs/en-us/guides/architecture/structures/structure-clean-architecture.md)
