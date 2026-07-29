# Customer API — Simple EF Core CRUD

This .NET 10 sample demonstrates relational CRUD in a small N-layer application. Persistence entities intentionally cross the HTTP boundary to keep the example compact.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Paged Customer and Contact CRUD with EF Core and SQL Server
- Repository and Unit of Work patterns with FluentValidation
- Native OpenAPI, RFC ProblemDetails, health checks, and NLog through `ILogger<T>`
- Startup-validated connection options and cancelable asynchronous operations

## Architecture

- Tier: `Simple`
- Shape: N-layers with Core, Infrastructure, and WebAPI projects
- Why this shape fits: it separates persistence from HTTP composition without DTO and application-service ceremony

Entities are API contracts in this teaching sample. For public or externally versioned APIs, use a Complex sample with dedicated request and response DTOs.

## Layers

- `CustomerAPI.Core` — entities and validators
- `CustomerAPI.Infrastructure` — EF Core context, mappings, and development seed data
- `CustomerAPI.WebAPI` — controllers, dependency injection, configuration, and HTTP middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server database used by EF Core | `Server=localhost,1433;Database=MyTestDb;User Id=sa;Password=MyPass@word;TrustServerCertificate=True` |

## Run

From `samples/src/simple-crud-ef-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

The host creates and seeds an empty development database.

### Docker Compose

```bash
docker compose up -d
```

SQL Server listens on localhost port **1433**. Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`. To use PostgreSQL or MySQL, add the provider through Central Package Management and replace `UseSqlServer` with `UseNpgsql` or `UseMySql`.

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`

Expected business failures retain Mvp24Hours business envelopes; unexpected host failures are rendered as ProblemDetails.

## Related documentation

- [Simple N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md)
- [Repository usage](../../../../docs/en-us/database/use-repository.md)
- [Relational databases](../../../../docs/en-us/database/relational.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- DTO isolation, mapping, or application services
- Authentication, production observability, or deployment hardening
- Production migration orchestration or multiple database providers in one build
