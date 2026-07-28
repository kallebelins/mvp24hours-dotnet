# Customer API — Complex MongoDB CRUD

This .NET 10 sample demonstrates Complex-tier Customer CRUD with DTOs, FluentValidation, and specifications on MongoDB document storage.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Asynchronous MongoDB repositories with filtering, pagination, retry reads/writes, and page-size limits
- DTO traffic boundary with AutoMapper, FluentValidation, and query specifications
- Embedded Contact documents on the Customer document
- Development seed with varied contacts and notes to exercise specification filters
- Native OpenAPI, RFC ProblemDetails, MongoDB health checks, and `ILogger<T>`
- Startup-validated connection options and request cancellation

## Architecture

- Tier: `Complex`
- Shape: N-layers with Core, Infrastructure, Application, and WebAPI projects
- Why this shape fits: public APIs keep DTO contracts while Mongo modeling stays focused on access patterns
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

## Layers

- `CustomerAPI.Core` — entities, DTOs, validators, and specifications
- `CustomerAPI.Infrastructure` — MongoDB context, BSON mappings, and seed data
- `CustomerAPI.Application` — Facade and application services
- `CustomerAPI.WebAPI` — controllers, validated options, health checks, and HTTP middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- MongoDB reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:MongoDbContext` | Yes | MongoDB server used by the `complexcustomers` database | `mongodb://localhost:27017` |

Use environment variables or a secret store when credentials are required.

## Run

From `samples/src/complex-crud-mongodb-customer-api`:

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

## Document modeling and consistency

- Contacts are **embedded** in the Customer document because they share the Customer lifecycle and are read with the owner.
- A single Customer write is atomic at the document boundary. There is no relational foreign-key enforcement across collections.
- Denormalized fields must be updated explicitly; MongoDB does not provide joins for this sample.
- Identifiers use MongoDB `ObjectId` string values (`EntityBase<string>`), assigned with `TimeProvider` timestamps on create.
- This sample does not use multi-document transactions. Cross-document consistency is intentionally out of scope.

## Related documentation

- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [NoSQL databases](../../../../docs/en-us/database/nosql.md)
- [Advanced MongoDB](../../../../docs/en-us/database/mongodb-advanced.md)
- [Specification](../../../../docs/en-us/specification.md)
- [Validation](../../../../docs/en-us/validation.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Multi-document transactions, sharding, or replica-set operations
- Separate Contact collection CRUD endpoints
- Authentication, production observability, or deployment hardening
