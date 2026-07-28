# Complex Hexagonal Customer API

First-class **Hexagonal / Ports & Adapters** blueprint for a Customer API built with [Mvp24Hours](https://mvp24hours.dev) and **.NET 10**.

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

## Getting Started

```bash
# Requires SQL Server accessible at localhost,1433 with the credentials in appsettings.Development.json.
# Update the connection string before running.

dotnet run --project samples/src/complex-hexagonal-customer-api/CustomerAPI.WebAPI

# Open Swagger UI
open http://localhost:5150/swagger
```

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
