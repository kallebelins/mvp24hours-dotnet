# Customer API — Simple RabbitMQ

This .NET 10 sample accepts Customer commands over HTTP, publishes them to RabbitMQ, and processes them asynchronously against an EF Core database.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- RabbitMQ producers and asynchronous Mvp24Hours consumers
- EF Core repositories and Unit of Work in scoped consumer services
- Retry limits and dead-letter exchange configuration
- Native OpenAPI, RFC ProblemDetails, SQL Server and RabbitMQ health checks, and `ILogger<T>`

## Architecture

- Tier: `Simple`
- Shape: N-layers with HTTP publishing and background message consumption
- Why this shape fits: it shows asynchronous command handling while keeping the broker and persistence responsibilities visible

## Layers

- `CustomerAPI.Core` — entities, message contracts, service contracts, and validation
- `CustomerAPI.Application` — Customer services, facade, and RabbitMQ consumers
- `CustomerAPI.Infrastructure` — EF Core context, mappings, migrations, and seed data
- `CustomerAPI.WebAPI` — HTTP producer, broker registration, hosted consumption, and middleware

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server
- RabbitMQ

## Configuration

| Key | Required | Description | Development example |
| --- | --- | --- | --- |
| `ConnectionStrings:EFDBContext` | Yes | SQL Server database updated by consumers | `Server=localhost,1433;Database=MyTestDbRabbitMQ;User Id=sa;Password=CHANGE_ME;TrustServerCertificate=True` |
| `ConnectionStrings:RabbitMQContext` | Yes | AMQP broker connection | `amqp://guest:guest@localhost:5672` |

Never commit production credentials. Override them through environment variables, user secrets, or a secret store.

## Run

From `samples/src/simple-rabbitmq-customer-api`:

```bash
docker compose up -d
dotnet restore
dotnet run --project CustomerAPI.WebAPI/CustomerAPI.WebAPI.csproj
```

### Docker Compose

```bash
docker compose up -d
```

- SQL Server: localhost **1433**
- RabbitMQ AMQP: **5672**; Management UI: **15672** (default `guest` / `guest`)

Set the same password in `docker-compose.yml` (`MSSQL_SA_PASSWORD`) and `ConnectionStrings:EFDBContext` in `appsettings.Development.json`.


## Explore the API

- OpenAPI document: `http://localhost:5000/openapi/v1.json`
- Swagger UI: `http://localhost:5000/swagger`
- Health endpoint: `http://localhost:5000/hc`
- Customer resources: `/api/customer`
- RabbitMQ management UI: `http://localhost:15672`

HTTP acceptance means the message was published, not that database processing has completed.

## Delivery semantics

RabbitMQ delivery is at least once: a consumer may receive the same command again after a connection or acknowledgement failure. Production handlers must be idempotent, normally by storing a stable message ID. This focused sample demonstrates retries and dead lettering but not a durable inbox or transactional outbox.

## Related documentation

- [Message broker](../../../../docs/en-us/broker.md)
- [Advanced RabbitMQ](../../../../docs/en-us/broker-advanced.md)
- [CQRS integration with RabbitMQ](../../../../docs/en-us/cqrs/integration-rabbitmq.md)
- [Inbox and outbox](../../../../docs/en-us/cqrs/resilience/inbox-outbox.md)

## What this sample intentionally does not cover

- Exactly-once delivery, durable inbox/outbox, or cross-resource transactions
- Production idempotency storage, schema evolution, or broker authorization
- Multi-service deployment and end-to-end distributed tracing
