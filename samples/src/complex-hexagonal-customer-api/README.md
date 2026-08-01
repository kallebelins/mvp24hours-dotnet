# Complex Hexagonal Customer API

First-class **Hexagonal / Ports & Adapters** blueprint for a Customer API built with Mvp24Hours and **.NET 10**. Explicit inbound HTTP adapters and outbound EF Core + resilient HTTP adapters map at the boundaries without leaking infrastructure types into Application.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Outbound ports in Core; use cases in Application with no Infrastructure references
- EF Core and Typicode HTTP outbound adapters with `IHttpClientFactory` + standard HTTP resilience
- Native OpenAPI, ProblemDetails, health checks, and Docker Compose for SQL Server

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│  Inbound Adapters (CustomerAPI.WebAPI)                   │
│  HTTP Controllers → ICustomerUseCase / IExternalProfile  │
└───────────────────────────┬──────────────────────────────┘
                            │  Inbound (driving) ports
                            ▼
┌──────────────────────────────────────────────────────────┐
│  Application (CustomerAPI.Application)                   │
│  CustomerUseCase / ExternalProfileUseCase                │
│  depends only on Core port interfaces                    │
└──────┬──────────────────────────────────┬────────────────┘
       │  Outbound (driven) ports         │
       ▼                                  ▼
┌────────────────────┐        ┌──────────────────────────┐
│  EF Core Adapter   │        │  HTTP Outbound Adapter   │
│  CustomerEFAdapter │        │  TypicodeProfileAdapter  │
│  ContactEFAdapter  │        │  (JSONPlaceholder API)   │
│  (SQL Server)      │        │  IHttpClientFactory +    │
│                    │        │  AddStandardResilience   │
└─────────┬──────────┘        └─────────────┬────────────┘
          └──── CustomerAPI.Infrastructure ──┘
                        ▼
┌──────────────────────────────────────────────────────────┐
│  Core (CustomerAPI.Core)                                 │
│  Entities · Enums · Outbound Port Interfaces             │
│  ICustomerReadPort, ICustomerWritePort                   │
│  IContactReadPort, IContactWritePort                     │
│  IExternalProfilePort                                    │
└──────────────────────────────────────────────────────────┘
```

> **Key rule:** `CustomerAPI.Application` references only `CustomerAPI.Core`.
> It never references `CustomerAPI.Infrastructure` or any EF/HTTP type.

## Projects

| Project | Role |
|---------|------|
| `CustomerAPI.Core` | Domain entities, enums, outbound port interfaces, value objects |
| `CustomerAPI.Application` | Use cases (`CustomerUseCase`, `ExternalProfileUseCase`), inbound port interfaces, DTOs |
| `CustomerAPI.Infrastructure` | EF Core adapters (`CustomerEFAdapter`, `ContactEFAdapter`), HTTP adapter (`TypicodeProfileAdapter`) |
| `CustomerAPI.WebAPI` | HTTP controllers (inbound adapters), composition root, NLog, OpenAPI |

## Ports

### Outbound Ports (defined in Core)

| Interface | Implemented by |
|-----------|----------------|
| `ICustomerReadPort` | `CustomerEFAdapter` |
| `ICustomerWritePort` | `CustomerEFAdapter` |
| `IContactReadPort` | `ContactEFAdapter` |
| `IContactWritePort` | `ContactEFAdapter` |
| `IExternalProfilePort` | `TypicodeProfileAdapter` |

### Inbound Ports (defined in Application)

| Interface | Implemented by |
|-----------|----------------|
| `ICustomerUseCase` | `CustomerUseCase` |
| `IExternalProfileUseCase` | `ExternalProfileUseCase` |

## Outbound HTTP Adapter

`TypicodeProfileAdapter` fetches user profiles from the [JSONPlaceholder](https://jsonplaceholder.typicode.com/users)
public API. The HTTP client is registered with `AddStandardResilienceHandler` (retry + circuit-breaker) at the
composition root — the adapter itself only receives `IHttpClientFactory`:

```csharp
// WebAPI/Extensions/ServiceBuilderExtensions.cs
services.AddHttpClientWithStandardResilience(TypicodeProfileAdapter.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

| Key | Description |
|-----|-------------|
| `ConnectionStrings:EFDBContext` | SQL Server connection for EF Core adapters |

## Getting Started

```bash
# From samples/src/complex-hexagonal-customer-api
docker compose up -d
dotnet run --project CustomerAPI.WebAPI

# Open Swagger UI
open http://localhost:5150/swagger
```

Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`.

### Docker Compose

```bash
docker compose up -d
```

SQL Server listens on localhost port **1433**.

### Database

Catalog: `MyHexTestDb`. EF migrations are applied automatically on startup in non-production environments.

## Related Samples

- [`complex-pipeline-ports-adapters-customer-api`](../complex-pipeline-ports-adapters-customer-api/README.md) —
  **sibling sample** focused on the pipeline-centric Ports & Adapters pattern using Mvp24Hours pipelines.
  This hexagonal sample uses explicit use-case services instead of pipeline builders.

## Build

```bash
dotnet build samples/src/complex-hexagonal-customer-api/Complex-Hexagonal-CustomerAPI.sln
```

## Related documentation

- [Hexagonal blueprint](../../../docs/en-us/guides/architecture/blueprints/template-hexagonal.md)
- [Core abstractions](../../../docs/en-us/core/infrastructure-abstractions.md)
- [HTTP resilience](../../../docs/en-us/infrastructure/http-resilience.md)
- [Getting started](../../../docs/en-us/getting-started.md)

## What this sample intentionally does not cover

- Message-driven inbound adapters (HTTP only in this host)
- Multiple bounded contexts or shared databases across services
- Production-grade adapter substitution beyond the teaching EF and Typicode HTTP examples
