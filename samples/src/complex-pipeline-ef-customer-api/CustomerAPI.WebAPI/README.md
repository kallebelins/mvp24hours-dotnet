# Customer API — Complex Pipeline EF Integration

This .NET 10 sample demonstrates an integration-style pipeline that fetches remote Customer data and persists it with EF Core. Pipeline steps are correlated and keep clear Unit of Work boundaries.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Integration pipeline: validate store → fetch remote → anti-corruption map → persist
- Correlation id carried on the pipeline message across steps
- Explicit persistence boundaries: only `CreateCustomerRepositoryStep` commits `IUnitOfWorkAsync.SaveChangesAsync`
- Named `HttpClient` with `IHttpClientFactory` and Microsoft.Extensions.Http.Resilience
- EF Core repository/UoW queries with specifications, DTOs, native OpenAPI, ProblemDetails, health checks, and `ILogger<T>`

## Architecture

- Tier: `Complex`
- Shape: Core + Application pipeline + Infrastructure EF + WebAPI
- Why this shape fits: remote fetch and local persistence share one correlated pipeline while write commits stay behind a single UoW boundary
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

FluentValidation is intentionally omitted: public endpoints are read-only or trigger integration seeding via the pipeline; there is no local write model accepting customer DTOs on the HTTP boundary.

## Layers

- `CustomerAPI.Core` — entities, value objects, specifications, contracts, resources
- `CustomerAPI.Application` — facade, services, and pipeline operations
- `CustomerAPI.Infrastructure` — EF Core context and Fluent API configuration
- `CustomerAPI.WebAPI` — hosting, validated options, resilience, controllers, and middleware

## Pipeline and Unit of Work boundaries

| Step | Boundary | Persistence |
| --- | --- | --- |
| `ValidateCustomerRepositoryStep` | Local read | Repository query only; no `SaveChanges` |
| `GetCustomerClientStep` | Remote integration | No database access |
| `GetByCustomerMapperResponseStep` | Anti-corruption layer | Maps remote payload to create DTOs; no UoW |
| `CreateCustomerRepositoryStep` | Local write | Adds entities and commits one `SaveChangesAsync` |

`RunDataSeed` assigns a `correlationId` and request `cancellationToken` on the pipeline message before execution.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (default) or another EF provider documented below
- Network access to the configured JSONPlaceholder-compatible endpoint

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | EF Core connection string | `Data Source=.,1433;Initial Catalog=MyTestDbPipelineEf;...;Password=CHANGE_ME;...` |
| `Settings:TypicodeCustomerUrl` | Yes | Source endpoint for external Customer records | `https://jsonplaceholder.typicode.com/users` |

## Run

From `samples/src/complex-pipeline-ef-customer-api`:

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


### Database providers

SQL Server is the default (`Microsoft.EntityFrameworkCore.SqlServer` via CPM). For another provider, add its package centrally and switch the `Use*` call:

- SQL Server: `UseSqlServer`
- PostgreSQL: `UseNpgsql` with `Npgsql.EntityFrameworkCore.PostgreSQL`
- MySQL: `UseMySql` with `Pomelo.EntityFrameworkCore.MySql`

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer list: `/api/customer`
- Customer details: `/api/customer/{id}`
- Integration seed: `POST /api/customer/RunDataSeed`

Expected validation and not-found outcomes keep Mvp24Hours business envelopes; unexpected host failures use ProblemDetails.

## Related documentation

- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Pipeline](../../../../docs/en-us/pipeline.md)
- [Unit of Work](../../../../docs/en-us/database/use-unitofwork.md)
- [Relational database](../../../../docs/en-us/database/relational.md)
- [HTTP resilience](../../../../docs/en-us/modernization/http-resilience.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Compensating transactions / saga rollback for partial remote+local failures
- Messaging, inbox/outbox, or event-driven integration
- Authentication, production observability, or multi-tenant data isolation
