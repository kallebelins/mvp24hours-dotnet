# Customer API — Simple EF Core and Dapper CRUD

This .NET 10 sample uses EF Core for transactional writes and Dapper for focused SQL reads in a small N-layer application.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- EF Core repositories and Unit of Work for create, update, and delete operations
- Dapper pagination and multi-result queries on the EF-owned connection
- Cancelable Dapper commands with parameterized filters
- Native OpenAPI, RFC ProblemDetails, health checks, and NLog through `ILogger<T>`

## Architecture

- Tier: `Simple`
- Shape: N-layers with Core, Infrastructure, and WebAPI projects
- Why this shape fits: Dapper optimizes read SQL while EF Core retains change tracking and transaction boundaries

Entities cross the HTTP boundary to keep the sample small. Prefer a Complex DTO-based sample for public or externally versioned APIs.

## Layers

- `CustomerAPI.Core` — entities and validators
- `CustomerAPI.Infrastructure` — EF Core persistence, development seed data, and Dapper paging extensions
- `CustomerAPI.WebAPI` — controllers, configuration, and HTTP composition

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | Shared EF Core and Dapper SQL Server database | `Server=localhost,1433;Database=MyTestDb;User Id=sa;Password=MyPass@word;TrustServerCertificate=True` |

Supply credentials through environment variables, user secrets, or a secret store.

## Run

From `samples/src/simple-crud-ef-dapper-customer-api`:

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


Dapper receives the connection owned by the scoped EF Core Unit of Work. The query helpers do not create or retain independent connections, and Dapper opens and closes a previously closed connection around each command.

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`

## When to choose Dapper reads

Use Dapper when a read path benefits from explicit SQL, multi-result queries, or projection control. Keep EF Core when LINQ, tracked aggregates, provider portability, and lower SQL maintenance are more valuable.

## Related documentation

- [Simple N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md)
- [Advanced EF Core](../../../../docs/en-us/database/efcore-advanced.md)
- [Unit of Work](../../../../docs/en-us/database/use-unitofwork.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- CQRS infrastructure or a separately deployed read store
- DTO isolation, authentication, or production observability
- Provider-neutral Dapper SQL; the paging query targets SQL Server
