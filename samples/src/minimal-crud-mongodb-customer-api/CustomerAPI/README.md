# Customer API — Minimal API with MongoDB

This focused .NET 10 sample implements paged Customer CRUD with Minimal APIs, MongoDB, and the Mvp24Hours document repository abstraction.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Paged list, get by ID, create, update, and delete endpoints
- MongoDB document persistence through the Mvp24Hours repository
- FluentValidation and Mvp24Hours business-result envelopes
- `TypedResults`, native OpenAPI, RFC ProblemDetails, and health checks
- Strongly typed connection-string options validated at startup

## Architecture

- Tier: `Minimal`
- Shape: one ASP.NET Core Minimal API host
- Why this shape fits: the sample demonstrates document CRUD and paging without adding Complex-tier project ceremony

## Folders

- `Entities` — Customer document model
- `Data` — MongoDB context, collection mapping, and development seed
- `Validations` — request/document validation rules
- `Extensions` — focused service and application composition
- `Configuration` — startup-validated options

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- MongoDB reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

Keep credentials outside committed files by using environment variables, user secrets, or a secret store.

| Key | Required | Description | Safe example |
| --- | --- | --- | --- |
| `ConnectionStrings:MongoDbContext` | Yes | MongoDB connection used by the repository and health check | `mongodb://localhost:27017` |

The sample stores Customer documents in the `simplecustomers` database. Contacts and other customer-owned data should normally be embedded when they share the same lifecycle; independently queried data may warrant references or a separate collection.

## Run

From `samples/src/minimal-crud-mongodb-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI/CustomerAPI.csproj
```

### Docker Compose

```bash
docker compose up -d
```

MongoDB listens on localhost port **27017**.


In Development, the application seeds sample documents when needed.

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
- [NoSQL data](../../../../docs/en-us/database/nosql.md)
- [MongoDB advanced guidance](../../../../docs/en-us/database/mongodb-advanced.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Relational joins, cross-document transactions, or relational modeling assumptions
- DTO boundaries or multiple projects; use a Complex sample for a public integration API
- Authentication, production clustering, backups, or secret provisioning