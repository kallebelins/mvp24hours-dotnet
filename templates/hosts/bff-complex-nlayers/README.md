# BFF host — Complex N-Layers

BFF-style Complex N-Layers scaffold **without a required DbContext**. Aggregates data from downstream APIs via gateway ports.

## Architecture

- Tier: Complex (BFF)
- Shape: Core → Application → Infrastructure → BFF (Facade + gateway ports)
- Persistence: **none in the BFF** — downstream calls via `IItemGateway` (in-memory stub by default)

## Layers

- `App.Core` — models, DTOs, validators, `IItemService`, `IItemGateway` port
- `App.Application` — `ItemService`, `FacadeService`
- `App.Infrastructure` — `InMemoryItemGateway` (stub) + `HttpItemGateway` (HttpClient)
- `App.BFF` — controllers, DI, OpenAPI, health
- `App.Test` — smoke tests

## Auth

This template already includes a Keycloak baseline (`AddKeycloakServices`, authentication middleware, current-user context, and authorization pipeline). Configure the `Keycloak` section in `appsettings*.json` for your realm/client.

## HTTP hardening

The BFF enables mvp24hours HTTP hardening middleware by default:

- Rate limiting
- Idempotency (disabled automatically in `Testing` environment)
- Output cache

Tune or disable these features through `HttpHardening` in `appsettings*.json`:

- `HttpHardening:RateLimiting` (`Enabled`, `PermitLimit`, `WindowSeconds`)
- `HttpHardening:Idempotency` (`Enabled`, `RequireKey`)
- `HttpHardening:OutputCache` (`Enabled`, `DefaultExpirationSeconds`)

## Downstream gateway mode

The BFF supports two gateway modes through `Downstream:ItemApi` options:

- `UseHttpGateway=false` (default): uses `InMemoryItemGateway` for local-first development.
- `UseHttpGateway=true`: uses `HttpItemGateway` with resilient HttpClient, response caching, and cache invalidation on writes.

Available options:

- `BaseAddress`
- `TimeoutSeconds`
- `ListCacheMinutes`
- `ItemCacheMinutes`

To wire a real downstream API, replace `InMemoryItemGateway` with `HttpItemGateway` in `ServiceBuilderExtensions` and set `Downstream:ItemApi:BaseAddress`.

## Run

```bash
dotnet run --project App.BFF
```

To start required local dependencies:

```bash
docker compose up -d
```

- OpenAPI: `http://localhost:5200/openapi/v1.json`
- Health: `http://localhost:5200/hc`

## Related

- API blueprint: [`blueprints/complex-nlayers`](../../blueprints/complex-nlayers)
- Docs: [Complex N-Layers](../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
