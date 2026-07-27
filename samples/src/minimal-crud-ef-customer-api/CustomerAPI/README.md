# Customer API — Minimal API with EF Core

This focused .NET 10 sample implements paged Customer CRUD with Minimal APIs, EF Core, and the Mvp24Hours repository and unit-of-work abstractions.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Paged list, get by ID, create, update, and delete endpoints
- SQL Server persistence through EF Core, repository, and unit of work
- FluentValidation and Mvp24Hours business-result envelopes
- `TypedResults`, native OpenAPI, RFC ProblemDetails, and health checks
- Strongly typed connection-string options validated at startup

## Architecture

- Tier: `Minimal`
- Shape: one ASP.NET Core Minimal API host
- Why this shape fits: the service is small and cohesive, so folders provide enough separation without introducing artificial project boundaries

## Folders

- `Entities` — Customer persistence model
- `Data` — EF Core context, mapping, and development seed
- `Validations` — request/entity validation rules
- `Extensions` — focused service and application composition
- `Configuration` — startup-validated options

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Keep credentials outside committed files by using environment variables, user secrets, or a secret store.

| Key | Required | Description | Safe example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server connection used by EF Core and health checks | `Server=localhost,1433;Database=CustomerDb;User Id=sa;Password=<secret>;TrustServerCertificate=True` |

The default provider is SQL Server. To use PostgreSQL or MySQL, add the provider package to Central Package Management and replace `UseSqlServer` with `UseNpgsql` or `UseMySql`.

## Run

From `samples/src/minimal-crud-ef-customer-api`:

```bash
dotnet restore
dotnet run --project CustomerAPI/CustomerAPI.csproj
```

In Development, the application creates the database when needed and adds sample records.

## Explore the API

- OpenAPI document: `http://localhost:5159/openapi/v1.json`
- Swagger UI: `http://localhost:5159/swagger`
- Health endpoint: `http://localhost:5159/hc`
- Customer collection: `/customer`
- Customer resource: `/customer/{id}`

Expected validation and not-found outcomes use Mvp24Hours business-result envelopes. Unexpected exceptions are converted to ProblemDetails.

## Related documentation

- [Minimal API structure](../../../../docs/en-us/guides/architecture/structures/structure-minimal-api.md)
- [Minimal APIs and TypedResults](../../../../docs/en-us/modernization/minimal-apis.md)
- [Relational data](../../../../docs/en-us/database/relational.md)
- [Repository usage](../../../../docs/en-us/database/use-repository.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- DTO boundaries or multiple projects; use a Complex sample for a public integration API
- Authentication, authorization, production migrations, or secret provisioning
- Docker orchestration and production observability
