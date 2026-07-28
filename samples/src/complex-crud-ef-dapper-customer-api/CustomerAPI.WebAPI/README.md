# Customer API — Complex EF Core and Dapper CRUD

This .NET 10 Complex sample uses EF Core for transactional writes and Dapper for focused SQL reads, while keeping DTO isolation, FluentValidation, AutoMapper, Facade, and Unit of Work boundaries.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- DTO and value-object request/response contracts (no entity leakage on the HTTP boundary)
- EF Core repositories and Unit of Work for create, update, and delete
- Cancelable Dapper pagination and multi-result queries on the EF-owned connection
- FluentValidation, AutoMapper, Facade, migrations, and development seed data
- Native OpenAPI, RFC ProblemDetails, health checks, and NLog through `ILogger<T>`

## Architecture

- Tier: `Complex`
- Shape: flat four-project N-layers (`Core`, `Application`, `Infrastructure`, `WebAPI`)
- Why this shape fits: Dapper optimizes read SQL while EF Core keeps change tracking and transaction boundaries behind Complex application services
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

Reads use hand-written SQL (not `ISpecification` composition). Writes stay on EF + UoW. This is a CQRS-lite teaching split, not a separately deployed read store.

`CustomerAPI.Core/Specifications/Customers/` documents the same filter rules that Dapper SQL applies in `CustomerService.GetBy` (for example `HasCellContact`, `HasEmailContact`). EF expression composition via `ISpecificationQuery` is not used on Dapper read paths.

## Layers

- `CustomerAPI.Core` — entities, DTOs/value objects, validators, specifications, contracts, and messages
- `CustomerAPI.Application` — application services, Facade, and Dapper paging extensions (`Extensions/DapperExtensions.cs`)
- `CustomerAPI.Infrastructure` — EF Core persistence, migrations, and seed
- `CustomerAPI.WebAPI` — controllers, configuration, and HTTP composition

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | Shared EF Core and Dapper SQL Server database | `Server=localhost,1433;Database=MyTestDb;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True` |

Supply credentials through environment variables, user secrets, or a secret store.

## Run

From `samples/src/complex-crud-ef-dapper-customer-api`:

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


On startup the host applies pending EF migrations and seeds an empty database. Dapper receives the connection owned by the scoped EF Core Unit of Work.

### Database providers

Default provider is SQL Server. CPM in `samples/Directory.Packages.props` controls package versions. For PostgreSQL or MySQL, switch the EF provider and adapt Dapper SQL (paging currently targets SQL Server).

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`
- Contact resources: `/api/customer/{customerId}/contact`

## When to choose Dapper reads

Use Dapper when a read path benefits from explicit SQL, multi-result queries, or projection control. Keep EF Core when LINQ, tracked aggregates, provider portability, and lower SQL maintenance are more valuable.

## Related documentation

- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Unit of Work](../../../../docs/en-us/database/use-unitofwork.md)
- [Advanced EF Core](../../../../docs/en-us/database/efcore-advanced.md)
- [Application services](../../../../docs/en-us/application-services.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- CQRS infrastructure or a separately deployed read store
- Provider-neutral Dapper SQL; the paging query targets SQL Server
- Authentication, authorization, or production observability hardening
