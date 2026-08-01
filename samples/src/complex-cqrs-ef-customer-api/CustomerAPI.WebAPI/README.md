# Customer API — CQRS + Mediator (EF Core)

This .NET 10 architecture blueprint demonstrates Command Query Responsibility Segregation with the Mvp24Hours Mediator. Controllers stay thin: they map HTTP to `IMediatorCommand` / `IMediatorQuery` and call `SendAsync`. Application feature folders own handlers, FluentValidation validators, and an in-process `IMediatorNotification` example. Persistence remains EF Core with Unit of Work—without premature event sourcing.

## Status

- Migration status: `migrated` (new blueprint sample)
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Feature folders: `Application/Customers|Contacts/Commands|Queries|Notifications`
- `AddMvpMediator` with `WithDefaultBehaviors()` and `RegisterValidationBehavior`
- Command/query separation via `IMediatorCommand` / `IMediatorQuery` (not MediatR)
- At least one notification (`CustomerCreatedNotification`) after a successful create
- DTO/value-object HTTP contracts, specifications on queries, AutoMapper, EF migrations and seed
- Native OpenAPI, RFC ProblemDetails, health checks, and NLog through `ILogger<T>`

## Architecture

- Tier: `Blueprint`
- Shape: CQRS + Mediator on Complex-quality boundaries (DTO traffic, validation, UoW)
- Why this shape fits: write and read models, validation, and cross-cutting behaviors differ enough to justify mediator pipelines without splitting databases yet
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

## Layers

- `CustomerAPI.Core` — entities, DTOs/value objects, specifications, and messages
- `CustomerAPI.Application` — commands, queries, handlers, validators, notifications
- `CustomerAPI.Infrastructure` — EF Core context, Fluent API configurations, migrations, and seed
- `CustomerAPI.WebAPI` — controllers (`IMediator`), dependency injection, configuration, and HTTP middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server reachable by the configured connection string

## Configuration

Configure secrets with environment variables, user secrets (`dotnet user-secrets`), or a secret store. Never commit real credentials.

Override credentials with environment variables, user secrets, or a secret store. Never commit real passwords.

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server database used by EF Core | `Server=localhost,1433;Database=MyCqrsTestDb;User Id=sa;Password=<secret>;TrustServerCertificate=True` |

## Run

From `samples/src/complex-cqrs-ef-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

### Docker Compose

```bash
docker compose up -d
```

SQL Server listens on localhost port **1433**. Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`.


On startup the host applies pending EF migrations and seeds an empty database. For production, prefer running migrations out-of-band.

### Database providers

These samples use SQL Server by default. Central Package Management in `samples/Directory.Packages.props` controls package versions.

- SQL Server: `Microsoft.EntityFrameworkCore.SqlServer` with `UseSqlServer`
- PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL` with `UseNpgsql`
- MySQL: `Pomelo.EntityFrameworkCore.MySql` with `UseMySql`

## Explore the API

- OpenAPI document: `http://localhost:5001/openapi/v1.json`
- Swagger UI: `http://localhost:5001/swagger`
- Health endpoint: `http://localhost:5001/hc`
- Customer resources: `/api/customer`
- Contact resources: `/api/customer/{customerId}/contact`

Expected business failures retain Mvp24Hours business envelopes; FluentValidation failures from the mediator validation behavior and unexpected host failures are rendered as ProblemDetails.

## Related documentation

- [CQRS blueprint](../../../../docs/en-us/guides/architecture/blueprints/template-cqrs.md)
- [CQRS getting started](../../../../docs/en-us/cqrs/getting-started.md)
- [Commands](../../../../docs/en-us/cqrs/commands.md)
- [Queries](../../../../docs/en-us/cqrs/queries.md)
- [Behaviors](../../../../docs/en-us/cqrs/behaviors.md)
- [Validation behavior](../../../../docs/en-us/cqrs/validation-behavior.md)
- [Notifications](../../../../docs/en-us/cqrs/notifications.md)
- [Complex EF CRUD sibling](../complex-crud-ef-customer-api/CustomerAPI.WebAPI/)

## What this sample intentionally does not cover

- Event sourcing, saga orchestration, or inbox/outbox (see Event-Driven and Event Sourcing samples)
- Separate read database or materialized views
- MediatR APIs (`IRequest`, `AddMediatR`) — use Mvp24Hours Mediator only
