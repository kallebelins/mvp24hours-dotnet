# Customer API — Minimal Pipeline

This focused .NET 10 sample uses Mvp24Hours Pipes and Filters to fetch customers from JSONPlaceholder and map the external contract into API-specific responses.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Asynchronous pipeline composition for list and get-by-ID use cases
- Named `HttpClient` with the standard HTTP resilience handler
- Request cancellation propagated to the outbound HTTP call
- Boundary mapping from the external JSON contract to local response models
- `TypedResults`, native OpenAPI, RFC ProblemDetails, and health checks
- Strongly typed integration options validated at startup

## Architecture

- Tier: `Minimal`
- Shape: one ASP.NET Core Minimal API host organized around pipeline operations
- Why this shape fits: the sample teaches Pipes and Filters without adding application and infrastructure projects

## Folders

- `Operations` — outbound HTTP and response-mapping pipeline steps
- `ValueObjects` — endpoint query and response contracts
- `Configuration` — startup-validated Typicode options
- `Extensions` — focused service and application composition

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Network access to the configured JSONPlaceholder-compatible endpoint

## Configuration

Use environment variables or another configuration provider when the integration URL differs by environment.

| Key | Required | Description | Default development value |
| --- | --- | --- | --- |
| `Settings:TypicodeCustomerUrl` | Yes | Source endpoint for external customer records | `https://jsonplaceholder.typicode.com/users` |

## Run

From `samples/src/minimal-pipeline-customer-api`:

```bash
dotnet restore
dotnet run --project CustomerAPI/CustomerAPI.csproj
```

## Explore the API

- OpenAPI document: `http://localhost:5159/openapi/v1.json`
- Swagger UI: `http://localhost:5159/swagger`
- Health endpoint: `http://localhost:5159/hc`
- Filtered customer list: `/customer`
- Customer details: `/customer/{id}`

The endpoint adds use-case-specific operations to a scoped asynchronous pipeline. Pipeline failures use Mvp24Hours business-result envelopes, while unexpected host failures are converted to ProblemDetails.

## Related documentation

- [Minimal API structure](../../../../docs/en-us/guides/architecture/structures/structure-minimal-api.md)
- [Pipeline](../../../../docs/en-us/pipeline.md)
- [HTTP resilience](../../../../docs/en-us/modernization/http-resilience.md)
- [Minimal APIs and TypedResults](../../../../docs/en-us/modernization/minimal-apis.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)

## What this sample intentionally does not cover

- Persistence, commands, transactions, or compensating operations
- Typed pipeline APIs; this sample retains message-based operations to demonstrate content shared between filters
- Authentication, authorization, production observability, or a private upstream service