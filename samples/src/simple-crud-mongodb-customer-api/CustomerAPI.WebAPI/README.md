# Customer API — Simple MongoDB CRUD

This .NET 10 sample demonstrates paged Customer CRUD with Mvp24Hours MongoDB repositories in a small N-layer application.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Asynchronous MongoDB repositories with filtering and pagination
- Customer and Contact document mappings with FluentValidation
- Native OpenAPI, RFC ProblemDetails, MongoDB health checks, and `ILogger<T>`
- Startup-validated connection options and request cancellation

## Architecture

- Tier: `Simple`
- Shape: N-layers with Core, Infrastructure, and WebAPI projects
- Why this shape fits: it separates document mapping from HTTP composition without relational or Complex-tier ceremony

Entities intentionally cross the HTTP boundary. Prefer a Complex DTO-based sample for public or externally versioned APIs.

## Layers

- `CustomerAPI.Core` — entities and validators
- `CustomerAPI.Infrastructure` — MongoDB context and BSON mappings
- `CustomerAPI.WebAPI` — controllers, validated options, health checks, and HTTP middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- MongoDB reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:MongoDbContext` | Yes | MongoDB server used by the `simplecustomers` database | `mongodb://localhost:27017` |

Use environment variables or a secret store when credentials are required.

## Run

From `samples/src/simple-crud-mongodb-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

### Docker Compose

```bash
docker compose up -d
```

MongoDB listens on localhost port **27017**.


## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`

## Document modeling

MongoDB favors data shaped around access patterns. Embed small, bounded data that is normally read with its owner; reference independently changing or unbounded data. Denormalized copies improve reads but must be updated explicitly because there is no relational join or foreign-key enforcement.

## Related documentation

- [Simple N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md)
- [NoSQL databases](../../../../docs/en-us/database/nosql.md)
- [Advanced MongoDB](../../../../docs/en-us/database/mongodb-advanced.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Multi-document transactions, sharding, or replica-set operations
- Relational navigation assumptions or cross-collection joins
- DTO isolation, authentication, or production observability
