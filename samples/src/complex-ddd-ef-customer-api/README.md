# Complex DDD EF Customer API

A **Domain-Driven Design** blueprint showing how to build a rich-domain Customer API with a single bounded context. The Customer is an **aggregate root** that enforces all business invariants through named domain methods, value objects validate inputs at construction time, and domain events are raised and dispatched in-process via the Mvp24Hours Mediator.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: project references by default; published packages are optional

## Features

- Rich aggregate root (`Customer`) with domain methods (`Create`, `Rename`, `Deactivate`, `AddContact`, `RemoveContact`)
- Value objects (`CustomerName`, `ContactDescription`) with invariant validation in constructors
- Domain events (`CustomerCreatedDomainEvent`, `ContactAddedDomainEvent`) raised by the aggregate, dispatched as in-process mediator notifications after persistence
- DDD specifications (`CustomerHasEmailContactSpec`, `CustomerHasCellContactSpec`, `CustomerHasNoContactSpec`, `CustomerIsProspectSpec`)
- CQRS-lite via Mvp24Hours Mediator (`IMediatorCommand`, `IMediatorQuery`, `IMediatorNotification`)
- EF Core SQL Server persistence with private-setter property mapping and private backing field for contacts collection
- Native OpenAPI, ProblemDetails, health checks (`/hc`), NLog, TimeProvider

## Architecture

- Tier: `Blueprint`
- Shape: DDD Aggregate + CQRS (command/query separation) + N-layers
- Why this shape fits: Teaches how a single bounded context is modeled with rich aggregates; all writes go through the aggregate root ensuring invariant consistency, while reads use efficient direct queries
- Dependency rule: **WebAPI → Application → Core**; **Infrastructure → Core**; composed at WebAPI. Application must not reference Infrastructure or WebAPI

## Layers

- `CustomerAPI.Core` — Domain layer: aggregate root, entities, value objects, domain events, specifications, resource messages
- `CustomerAPI.Application` — Application layer: CQRS commands/queries/handlers/notifications via Mvp24Hours Mediator
- `CustomerAPI.Infrastructure` — Infrastructure layer: EF Core DbContext, entity configurations, migrations, seed
- `CustomerAPI.WebAPI` — Presentation layer: ASP.NET Core API, controllers, DI composition, health checks, OpenAPI

## Key DDD Teaching Points

### Aggregate Root with Domain Methods

```csharp
// Customer aggregate — state changes go through domain methods only
public static Customer Create(CustomerName name, TimeProvider timeProvider, string? note = null) { ... }
public void Rename(CustomerName newName) { ... }
public void Deactivate() { ... }
public Contact AddContact(ContactType type, ContactDescription description, TimeProvider timeProvider) { ... }
public void RemoveContact(int contactId) { ... }
```

### Value Objects Validate at Construction

```csharp
// Throws ArgumentException on violation — no anemic models, no manual if-checks in handlers
var name = new CustomerName(""); // ArgumentException: Customer name cannot be empty
var desc = new ContactDescription("x" * 300); // ArgumentException: cannot exceed 255 chars
```

### Domain Events Raised by Aggregate, Dispatched After Persistence

```csharp
// Handler pattern: create → persist → dispatch
var customer = Customer.Create(name, timeProvider);
await repository.AddAsync(customer, cancellationToken);
await unitOfWork.SaveChangesAsync(cancellationToken);
await mediator.PublishAsync(new CustomerCreatedNotification(customer.Id, customer.Name), cancellationToken);
customer.ClearDomainEvents();
```

### Contact Owned by Aggregate

```csharp
// Contact can only be created through the aggregate — not directly
customer.AddContact(ContactType.Email, new ContactDescription("user@example.com"), timeProvider);
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server (local, Docker, or Azure SQL) — connection string in `appsettings.Development.json`

## Configuration

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server connection string | `Data Source=.;Initial Catalog=MyDddTestDb;...` |

Edit `CustomerAPI.WebAPI/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "EFDBContext": "Data Source=.,1433;Initial Catalog=MyDddTestDb;Persist Security Info=True;TrustServerCertificate=True;User ID=sa;Password=<secret>;Pooling=False;"
  }
}
```

## Run

From the solution directory:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

The application will:
1. Apply EF Core migrations automatically (`Database.MigrateAsync`)
2. Seed three sample customers (using aggregate factory methods)
3. Serve the API at `https://localhost:5001`

### Docker Compose

```bash
docker compose up -d
```

SQL Server listens on localhost port **1433**. Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`.

## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI (non-production): `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`

### Customer endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/Customer` | Paginated customer list with filter |
| `GET` | `/api/Customer/{id}` | Customer with contacts |
| `POST` | `/api/Customer` | Create customer (calls `Customer.Create()`) |
| `PUT` | `/api/Customer/{id}` | Rename + update note (calls `Rename()`, `UpdateNote()`) |
| `DELETE` | `/api/Customer/{id}` | Deactivate customer (calls `Deactivate()`) |

### Contact endpoints (nested under customer aggregate)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/Customer/{customerId}/Contact` | List contacts |
| `POST` | `/api/Customer/{customerId}/Contact` | Add contact (calls `AddContact()`) |
| `DELETE` | `/api/Customer/{customerId}/Contact/{id}` | Remove contact (calls `RemoveContact()`) |

## Related documentation

- [Architecture blueprints — DDD template](../../../docs/en-us/guides/architecture/blueprints/template-ddd.md)
- [Core home](../../../docs/en-us/core/home.md)
- [Value objects](../../../docs/en-us/core/value-objects.md)
- [Entity interfaces](../../../docs/en-us/core/entity-interfaces.md)
- [Strongly typed IDs](../../../docs/en-us/core/strongly-typed-ids.md)
- [Domain events](../../../docs/en-us/cqrs/domain-events.md)
- [Specifications](../../../docs/en-us/specification.md)
- [Getting started](../../../docs/en-us/getting-started.md)
- [Architecture guidance](../../../docs/en-us/guides/architecture/home.md)

## What this sample intentionally does not cover

- Strongly typed IDs (e.g., `CustomerId` as a struct) — see docs/en-us/core/strongly-typed-ids.md
- Event sourcing or snapshot aggregates (see `AggregateRoot<TId>` in Mvp24Hours.Infrastructure.Cqrs)
- Multi-aggregate / multiple bounded contexts
- Outbox pattern for durable domain event publishing across transactions
- Authentication, authorization, and multi-tenancy
- Read-model projections or separate read database
