# Customer API — Simple EF Core Entity Logging

This .NET 10 sample demonstrates Mvp24Hours entity audit fields and logical deletion in a compact EF Core N-layer application.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Automatic created, modified, and removed audit fields through `EntityBaseLog`
- `TimeProvider` bridged to the audit interceptor for testable UTC timestamps
- Global EF Core filter that excludes logically removed records
- Paged Customer and Contact CRUD with FluentValidation and Unit of Work
- Native OpenAPI, RFC ProblemDetails, SQL Server health checks, and `ILogger<T>`

## Architecture

- Tier: `Simple`
- Shape: N-layers with Core, Infrastructure, and WebAPI projects
- Why this shape fits: the sample isolates audit-aware persistence while keeping entities as HTTP contracts

Entity leakage is deliberate for teaching. Public or externally versioned APIs should use Complex samples with DTO boundaries.

## Layers

- `CustomerAPI.Core` — `EntityBaseLog` entities and validators
- `CustomerAPI.Infrastructure` — audit-enabled context and EF Core mappings
- `CustomerAPI.WebAPI` — controllers, validated options, and HTTP composition

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:CustomerDbContext` | Yes | SQL Server database containing audit columns | `Server=localhost,1433;Database=MyTestLogDb;User Id=sa;Password=MyPass@word;TrustServerCertificate=True` |

Override credentials with environment variables, user secrets, or a secret store.

## Run

From `samples/src/simple-crud-ef-entitylog-customer-api`:

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


`EFDBContext.CanApplyEntityLog` enables the legacy entity-log rules and the `Removed == null` global filter. `AuditSaveChangesInterceptor` receives the `IClock` bridge registered by `AddTimeProvider`, so created and modified timestamps use the native time abstraction. Regular repository queries do not return soft-deleted rows.

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`

## Related documentation

- [Simple N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md)
- [Entity interfaces](../../../../docs/en-us/core/entity-interfaces.md)
- [Using entities](../../../../docs/en-us/database/use-entity.md)
- [EF Core query filters](https://learn.microsoft.com/ef/core/querying/filters)

## What this sample intentionally does not cover

- Per-tenant filters, audit history tables, or restoring removed rows
- An authenticated user provider; `EntityLogBy` uses the teaching value `SYSTEM`
- DTO isolation, production observability, or deployment hardening
