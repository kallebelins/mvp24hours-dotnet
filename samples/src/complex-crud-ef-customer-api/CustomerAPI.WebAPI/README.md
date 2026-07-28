# Customer API — Complex EF Core CRUD

This .NET 10 sample is the canonical Complex N-layers Customer API. It isolates HTTP traffic behind DTOs and value objects, validates with FluentValidation, filters with specifications, persists through EF Core repositories and Unit of Work, maps with AutoMapper, and exposes application services through a Facade.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- DTO and value-object request/response contracts (no entity leakage on the HTTP boundary)
- FluentValidation, specifications, AutoMapper, Facade, repository, and Unit of Work
- EF Core migrations and development seed data
- Native OpenAPI, RFC ProblemDetails, health checks, and NLog through `ILogger<T>`
- Startup-validated connection options and cancelable asynchronous operations

## Architecture

- Tier: `Complex`
- Shape: flat four-project N-layers (`Core`, `Application`, `Infrastructure`, `WebAPI`) as a teaching simplification of the modular Complex layout in `structure-complex-nlayers.md`
- Why this shape fits: Complex rules, validation, mapping, and application boundaries without premature multi-module ceremony
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

Prefer this sample for public or externally versioned APIs. The modular `Modules/` layout in the Complex structure guide is the scale-up path when multiple bounded contexts appear.

## Layers

- `CustomerAPI.Core` — entities, DTOs/value objects, validators, specifications, contracts, and messages
- `CustomerAPI.Application` — application services and Facade
- `CustomerAPI.Infrastructure` — EF Core context, Fluent API configurations, migrations, and seed
- `CustomerAPI.WebAPI` — controllers, dependency injection, configuration, and HTTP middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server database used by EF Core | `Server=localhost,1433;Database=MyTestDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True` |

## Run

From `samples/src/complex-crud-ef-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

On startup the host applies pending EF migrations and seeds an empty database. For production, prefer running migrations out-of-band.

### Docker Compose

```bash
docker compose up -d
```

SQL Server listens on localhost port **1433**. Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`.

### Database providers

These samples use SQL Server by default. Central Package Management in `samples/Directory.Packages.props` controls package versions.

- SQL Server: `Microsoft.EntityFrameworkCore.SqlServer` with `UseSqlServer`
- PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL` with `UseNpgsql`
- MySQL: `Pomelo.EntityFrameworkCore.MySql` with `UseMySql`

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`
- Contact resources: `/api/customer/{customerId}/contact`

Expected business failures retain Mvp24Hours business envelopes; unexpected host failures are rendered as ProblemDetails.

## Related documentation

- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Application services](../../../../docs/en-us/application-services.md)
- [Validation](../../../../docs/en-us/validation.md)
- [Specification](../../../../docs/en-us/specification.md)
- [Mapping](../../../../docs/en-us/mapping.md)
- [Unit of Work](../../../../docs/en-us/database/use-unitofwork.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Multi-module modular monolith packaging (`Modules/Sales`, `Modules/Billing`)
- Authentication, authorization, or production observability hardening
- CQRS mediator dispatch or a separately deployed read store
- Provider-neutral SQL beyond the documented EF provider switch
