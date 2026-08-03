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

In **Development**, authorization is not enforced by default (no auth middleware configured). Add JWT/OAuth and policy attributes before production.

To wire a real downstream API, replace `InMemoryItemGateway` with `HttpItemGateway` in `ServiceBuilderExtensions` and set `Downstream:ItemApi:BaseAddress`.

## Run

```bash
dotnet run --project App.BFF
```

- OpenAPI: `http://localhost:5200/openapi/v1.json`
- Health: `http://localhost:5200/hc`

## Related

- API blueprint: [`blueprints/complex-nlayers`](../../blueprints/complex-nlayers)
- Docs: [Complex N-Layers](../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
