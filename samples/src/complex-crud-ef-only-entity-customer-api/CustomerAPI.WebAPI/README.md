# Customer API — Complex EF Core with Entity Traffic

This .NET 10 Complex sample keeps entities as HTTP request and response contracts on purpose. Use it only to compare trade-offs against the DTO-based Complex EF sample.

## Status

- Migration status: `migrated` (teaching / contrast sample)
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional
- Recommendation: do **not** expose this shape on the public internet

## Features

- Persistence entities intentionally cross the HTTP boundary
- FluentValidation, specifications, Facade, repository, and Unit of Work
- EF Core migrations and development seed data
- Native OpenAPI, RFC ProblemDetails, health checks, and NLog through `ILogger<T>`
- Startup-validated connection options and cancelable asynchronous operations

## Architecture

- Tier: `Complex`
- Shape: flat four-project N-layers (`Core`, `Application`, `Infrastructure`, `WebAPI`)
- Why this shape fits: it demonstrates Complex layering without DTO/mapping ceremony so readers can compare coupling costs
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

### Trade-offs vs DTO Complex sample

| Concern | This sample (entity traffic) | `complex-crud-ef-customer-api` (DTO traffic) |
| --- | --- | --- |
| HTTP contract stability | Tightly coupled to persistence model | Versionable request/response shapes |
| Over-posting / under-exposure | Harder to control | Explicit fields per operation |
| Mapping cost | None | AutoMapper / value objects |
| Public API readiness | Discouraged | Preferred |

Prefer the DTO Complex sample for public or externally versioned APIs. See the [decision matrix](../../../../docs/en-us/guides/architecture/decision-matrix.md).

## Layers

- `CustomerAPI.Core` — entities, query filters, validators, specifications, contracts, and messages
- `CustomerAPI.Application` — application services and Facade
- `CustomerAPI.Infrastructure` — EF Core context, configurations, migrations, and seed
- `CustomerAPI.WebAPI` — controllers, dependency injection, configuration, and HTTP middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

Override credentials with environment variables, user secrets, or a secret store. Never commit real passwords.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server database used by EF Core | `Server=localhost,1433;Database=MyTestDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True` |

## Run

From `samples/src/complex-crud-ef-only-entity-customer-api`:

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


On startup the host applies pending EF migrations and seeds an empty database.

### Database providers

Default provider is SQL Server via CPM. Switch to PostgreSQL (`UseNpgsql`) or MySQL (`UseMySql`) by adding the provider package centrally.

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`
- Contact resources: `/api/customer/{customerId}/contact`

Expected business failures retain Mvp24Hours business envelopes; unexpected host failures are rendered as ProblemDetails.

## Related documentation

- [Decision matrix](../../../../docs/en-us/guides/architecture/decision-matrix.md)
- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Application services](../../../../docs/en-us/application-services.md)
- [Validation](../../../../docs/en-us/validation.md)
- [Specification](../../../../docs/en-us/specification.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- DTO isolation, AutoMapper profiles, or field-level API versioning
- Safe public-internet exposure patterns
- Authentication, authorization, or production observability hardening
