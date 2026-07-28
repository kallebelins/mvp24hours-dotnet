# Customer API — Complex Pipeline Ports and Adapters

This .NET 10 sample is the transitional Hexagonal teaching sample for pipeline-centric outbound adapters. Core defines builder ports; `CustomerAPI.Typicode.Application` implements them with a resilient HTTP client. A dedicated Hexagonal blueprint sample (`complex-hexagonal-customer-api`, Phase 5.4) will cover inbound + outbound ports beyond pipelines.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Pipeline builders as application ports implemented by a Typicode outbound adapter project
- Named `HttpClient` via `IHttpClientFactory` with Microsoft.Extensions.Http.Resilience
- Request cancellation propagated to the outbound HTTP call
- Facade, DTOs/value objects, native OpenAPI, RFC ProblemDetails, health checks, and `ILogger<T>`

## Architecture

- Tier: `Complex`
- Shape: Hexagonal-leaning ports and adapters focused on pipeline composition
- Why this shape fits: Core stays free of HTTP SDK details while the Typicode project owns the outbound adapter
- Dependency rule: **WebAPI → Application → Core**; outbound adapter projects depend on Core ports only. Application must not reference WebAPI

FluentValidation and specifications are intentionally omitted: the sample focuses on pipeline composition over an external HTTP API with no local write model.

## Layers

- `CustomerAPI.Core` — contracts, builder ports, value objects, resources
- `CustomerAPI.Application` — facade and application services that depend only on Core ports
- `CustomerAPI.Typicode.Application` — outbound adapter (HTTP client step, mappers, builders)
- `CustomerAPI.WebAPI` — hosting, validated options, resilience registration, controllers, and middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Network access to the configured JSONPlaceholder-compatible endpoint

## Configuration

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `Settings:TypicodeCustomerUrl` | Yes | Source endpoint for external Customer records | `https://jsonplaceholder.typicode.com/users` |

## Run

From `samples/src/complex-pipeline-ports-adapters-customer-api`:

```bash
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer list: `/api/customer`
- Customer details: `/api/customer/{id}`

## Related documentation

- [Hexagonal blueprint](../../../../docs/en-us/guides/architecture/blueprints/template-hexagonal.md)
- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Pipeline](../../../../docs/en-us/pipeline.md)
- [HTTP resilience](../../../../docs/en-us/modernization/http-resilience.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

Sibling samples:

- Builder-only composition: `complex-pipeline-builder-customer-api`
- Planned first-class Hexagonal host: `complex-hexagonal-customer-api` (Phase 5.4)

## What this sample intentionally does not cover

- Full Hexagonal inbound adapters (messaging, multiple delivery mechanisms)
- Persistence, Unit of Work, compensating operations, or messaging
- Authentication, production observability, or a private upstream service
