# Customer API — Complex Pipeline Builder

This .NET 10 sample shows the recommended Complex-tier pipeline style: each Customer use case is composed by a DI-registered builder that aggregates injectable operations.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Builder-registered asynchronous pipelines per Customer use case
- Constructor-injected builders and steps (unit-testable without a service locator)
- Named `HttpClient` with `IHttpClientFactory` and Microsoft.Extensions.Http.Resilience
- Request cancellation propagated to the outbound HTTP call
- Facade, DTOs/value objects, native OpenAPI, RFC ProblemDetails, health checks, and `ILogger<T>`

## Architecture

- Tier: `Complex`
- Shape: Core contracts, Application builders/operations, controller-based WebAPI
- Why this shape fits: builders keep use-case composition explicit and reusable while the host stays thin
- Dependency rule: **WebAPI → Application → Core**; Application must not reference WebAPI (no Infrastructure project in this sample)

FluentValidation and specifications are intentionally omitted: the sample focuses on builder-based pipeline composition over an external HTTP API with no local write model.

## Layers

- `CustomerAPI.Core` — contracts, builders interfaces, value objects, resources
- `CustomerAPI.Application` — facade, services, builders, and pipeline operations
- `CustomerAPI.WebAPI` — hosting, validated options, resilience, controllers, and middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Network access to the configured JSONPlaceholder-compatible endpoint

## Configuration

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `Settings:TypicodeCustomerUrl` | Yes | Source endpoint for external Customer records | `https://jsonplaceholder.typicode.com/users` |

## Run

From `samples/src/complex-pipeline-builder-customer-api`:

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

Builders compose the shared outbound step with a use-case-specific mapper. Pipeline failures retain Mvp24Hours business envelopes; unexpected host failures use ProblemDetails.

## Related documentation

- [Complex N-layers structure](../../../../docs/en-us/guides/architecture/structures/structure-complex-nlayers.md)
- [Pipeline](../../../../docs/en-us/pipeline.md)
- [HTTP resilience](../../../../docs/en-us/modernization/http-resilience.md)
- [ProblemDetails](../../../../docs/en-us/modernization/problem-details.md)
- [Dependency injection guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines)

## What this sample intentionally does not cover

- Persistence, Unit of Work, compensating operations, or messaging
- Ports-and-adapters project split (see `complex-pipeline-ports-adapters-customer-api`)
- Authentication, production observability, or a private upstream service
