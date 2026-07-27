# Customer API — Simple Pipeline

This .NET 10 sample uses Mvp24Hours Pipes and Filters in a layered controller application to fetch and map customers from JSONPlaceholder.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Scoped asynchronous pipelines composed per Customer use case
- DI-backed outbound and response-mapping operations
- Named `HttpClient` with the standard resilience handler
- Request cancellation propagated to the outbound HTTP call
- Native OpenAPI, RFC ProblemDetails, health checks, and `ILogger<T>`

## Architecture

- Tier: `Simple`
- Shape: Core contracts plus a controller-based WebAPI containing pipeline operations
- Why this shape fits: operations remain layered and reusable without the additional projects used by Complex pipeline samples

## Layers

- `CustomerAPI.Core` — request and response contracts, resources, and enums
- `CustomerAPI.WebAPI/Pipe` — outbound integration and mapping steps
- `CustomerAPI.WebAPI` — controllers, validated options, resilience, and middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Network access to the configured JSONPlaceholder-compatible endpoint

## Configuration

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `Settings:TypicodeCustomerUrl` | Yes | Source endpoint for external Customer records | `https://jsonplaceholder.typicode.com/users` |

Override the URL through environment-specific configuration when needed.

## Run

From `samples/src/simple-pipeline-customer-api`:

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

Each request adds the integration and use-case-specific mapping steps to its scoped pipeline. Pipeline failures retain Mvp24Hours business envelopes; unexpected host failures use ProblemDetails.

## Related documentation

- [Simple N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-simple-nlayers.md)
- [Pipeline](../../../../docs/en-us/pipeline.md)
- [HTTP resilience](../../../../docs/en-us/modernization/http-resilience.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Persistence, transactions, compensating operations, or messaging
- Builder-based or ports-and-adapters pipeline composition
- Authentication, production observability, or a private upstream service
