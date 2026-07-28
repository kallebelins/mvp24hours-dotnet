# Customer API — Simple Redis Key-Value CRUD

This .NET 10 sample demonstrates Mvp24Hours Redis cache repositories as a small key-value Customer API.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Get, set, and remove operations through `IRepositoryCacheAsync<T>`
- Sliding expiration configured for Customer values
- FluentValidation, startup-validated Redis options, and request cancellation
- Native OpenAPI, RFC ProblemDetails, Redis health checks, and `ILogger<T>`

## Architecture

- Tier: `Simple`
- Shape: Core contracts plus a WebAPI host backed directly by Redis
- Why this shape fits: the API teaches key-value storage without pretending Redis is a relational repository

## Layers

- `CustomerAPI.Core` — Customer cache contract and validation
- `CustomerAPI.WebAPI` — Redis repository registration, controllers, configuration, and HTTP middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Redis reachable by the configured endpoint

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:RedisDbContext` | Yes | Redis endpoint used by the cache repository | `127.0.0.1:6379` |

Use environment variables or a secret store when the endpoint includes credentials.

## Run

From `samples/src/simple-crud-redis-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

### Docker Compose

```bash
docker compose up -d
```

Redis listens on localhost port **6379**.


## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer/{key}`

## Cache-aside versus this sample

This API treats Redis as the key-value store of record. A cache-aside application keeps an authoritative database, reads the cache first, fills it on misses, and invalidates it after writes. Use `HybridCache` for coordinated in-memory and distributed caching; that broader pattern belongs in the dedicated capability sample.

## Related documentation

- [Simple N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md)
- [Advanced caching](../../../../docs/en-us/caching-advanced.md)
- [HybridCache modernization](../../../../docs/en-us/modernization/hybrid-cache.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- An authoritative relational or document database
- Cache stampede protection, distributed locking, or HybridCache
- Authentication, production observability, or Redis cluster operations
