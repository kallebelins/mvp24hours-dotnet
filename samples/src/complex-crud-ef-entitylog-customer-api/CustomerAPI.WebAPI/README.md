# Customer API — Complex EF Core Entity Logging

This .NET 10 sample demonstrates Mvp24Hours entity audit fields and soft delete together with Complex-tier DTOs, FluentValidation, and specifications.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- `EntityBaseLog` audit columns (`Created`/`Modified`/`Removed` and actor fields) via `AuditSaveChangesInterceptor` and `TimeProvider`
- Global EF Core filter that excludes logically removed records (`Removed == null`)
- DTO traffic boundary with AutoMapper, FluentValidation, and query specifications
- Paged Customer and Contact CRUD through Facade and Application services
- Native OpenAPI, RFC ProblemDetails, SQL Server health checks, and `ILogger<T>`

## Architecture

- Tier: `Complex`
- Shape: N-layers with Core, Infrastructure, Application, and WebAPI projects
- Why this shape fits: public APIs keep DTO contracts while persistence demonstrates audit and soft-delete rules
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

## Layers

- `CustomerAPI.Core` — `EntityBaseLog` entities, DTOs, validators, and specifications
- `CustomerAPI.Infrastructure` — audit-enabled context, FluentAPI mappings, and seed data
- `CustomerAPI.Application` — Facade and application services
- `CustomerAPI.WebAPI` — controllers, validated options, and HTTP composition

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server database containing audit columns | `Server=localhost,1433;Database=MyTestLogDb;User Id=sa;Password=MyPass@word;TrustServerCertificate=True` |

Override credentials with environment variables, user secrets, or a secret store.

## Run

From `samples/src/complex-crud-ef-entitylog-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

### Docker Compose

```bash
docker compose up -d
```

SQL Server listens on localhost port **1433**. Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`.


Startup calls `EnsureCreatedAsync` then seeds sample rows when the database is empty. `EFDBContext.CanApplyEntityLog` enables entity-log rules and the soft-delete filter. `AuditSaveChangesInterceptor` receives the `IClock` bridge registered by `AddTimeProvider`, so created and modified timestamps use the native time abstraction. Regular repository queries do not return soft-deleted rows.

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`
- Contact resources: `/api/customer/{customerId}/contact`

## Related documentation

- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Entity interfaces](../../../../docs/en-us/core/entity-interfaces.md)
- [Using entities](../../../../docs/en-us/database/use-entity.md)
- [Specification](../../../../docs/en-us/specification.md)
- [Validation](../../../../docs/en-us/validation.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)
- [EF Core query filters](https://learn.microsoft.com/ef/core/querying/filters)

## What this sample intentionally does not cover

- Per-tenant filters, audit history tables, or restoring removed rows
- An authenticated user provider; `EntityLogBy` uses the teaching value `SYSTEM`
- Production observability, authentication, or deployment hardening
