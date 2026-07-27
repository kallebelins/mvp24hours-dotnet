# Customer API — Complex Pipeline

This .NET 10 sample uses Mvp24Hours Pipes and Filters in a Complex N-layer application to fetch and map customers from JSONPlaceholder.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Scoped asynchronous pipelines composed in the Application layer per Customer use case
- DI-backed outbound and response-mapping operations with `IsBreakOnFail`
- Named `HttpClient` with the standard resilience handler
- Request cancellation propagated through the pipeline message bag to the outbound HTTP call
- Native OpenAPI, RFC ProblemDetails, health checks, and `ILogger<T>`
- Startup-validated Typicode options

## Architecture

- Tier: `Complex`
- Shape: Core contracts, Application pipeline operations and services, WebAPI controllers
- Why this shape fits: pipeline composition stays reusable behind a Facade while HTTP remains a thin adapter

## Layers

- `CustomerAPI.Core` — request and response contracts, resources, and enums
- `CustomerAPI.Application` — Facade, CustomerService, and pipe operations
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

From `samples/src/complex-pipeline-customer-api`:

```bash
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

Each request adds the integration and use-case-specific mapping steps to its scoped pipeline. Controllers pass `CancellationToken` into the Application service, which stores it on the pipeline message for the HTTP step. Pipeline failures retain Mvp24Hours business envelopes; unexpected host failures use ProblemDetails.

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer list: `/api/customer`
- Customer details: `/api/customer/{id}`

## Related documentation

- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Pipeline](../../../../docs/en-us/pipeline.md)
- [HTTP resilience](../../../../docs/en-us/modernization/http-resilience.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Persistence, transactions, compensating operations, or messaging
- Builder-based or ports-and-adapters pipeline composition
- Authentication, production observability, or a private upstream service
