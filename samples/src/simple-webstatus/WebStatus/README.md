# Mvp24Hours WebStatus — Health Checks catalog

Dedicated .NET 10 host that registers and surfaces health checks for SQL Server, PostgreSQL, MySQL, Redis, MongoDB, and RabbitMQ using Mvp24Hours extensions (plus HealthChecks UI for a local status dashboard).

## Status

- Migration status: `migrated` (recreated capability sample)
- Target framework: `net10.0`
- Mvp24Hours consumption: project references by default; published packages are optional

## Features

- `AddMvp24HoursHealthChecks` / `UseMvp24HoursHealthChecks` JSON endpoints (`/health`, `/health/ready`, `/health/live`)
- Provider checks: `AddMvp24HoursSqlServerCheck`, `AddMvp24HoursPostgreSqlCheck`, `AddMvp24HoursMySqlCheck`, `AddMongoDbHealthCheck`, `AddMvp24HoursRabbitMQHealthCheck`, and Redis via AspNetCore.HealthChecks.Redis
- HealthChecks UI at `/healthchecks-ui` polling `/hc`
- Startup-validated connection string options, native OpenAPI, ProblemDetails, NLog through `ILogger<T>`

## Architecture

- Tier: `Simple`
- Shape: Single Web host focused on dependency monitoring (no business CRUD)
- Why this shape fits: a catalog sample should stay lean and demonstrate registration patterns only

## Layers

- `WebStatus` — host, options, health registration, OpenAPI, and HealthChecks UI

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (optional) for local SQL Server, PostgreSQL, MySQL, Redis, MongoDB, and RabbitMQ

## Configuration

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials. Align Docker Compose passwords with these values when you change them.

| Key | Required | Description | Example |
| --- | --- | --- | --- |
| `ConnectionStrings:SqlServer` | Yes | SQL Server connection | `Server=localhost,1433;Database=master;User Id=sa;Password=MyPass@word;TrustServerCertificate=True` |
| `ConnectionStrings:PostgreSql` | Yes | PostgreSQL connection | `Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=MyPass@word` |
| `ConnectionStrings:MySql` | Yes | MySQL connection | `Server=localhost;Port=3306;Database=mysql;User Id=root;Password=MyPass@word` |
| `ConnectionStrings:Redis` | Yes | Redis connection | `localhost:6379,abortConnect=false` |
| `ConnectionStrings:MongoDb` | Yes | MongoDB connection | `mongodb://localhost:27017` |
| `ConnectionStrings:RabbitMQ` | Yes | AMQP connection | `amqp://guest:guest@localhost:5672` |
| `HealthCatalog:MongoDatabaseName` | No | Mongo database probed by the check | `mvp24hours` |

## Run

From `samples/src/simple-webstatus`:

```bash
docker compose up -d
dotnet restore
dotnet run --project WebStatus/WebStatus.csproj
```

Update `appsettings.Development.json` so `SqlServer` uses the same SA password you set in `docker-compose.yml` (`MSSQL_SA_PASSWORD`).

### Docker Compose

```bash
docker compose up -d
```

Services expose localhost ports 1433 (SQL Server), 5432 (PostgreSQL), 3306 (MySQL), 6379 (Redis), 27017 (MongoDB), 5672/15672 (RabbitMQ).

## Explore the API

- HealthChecks UI: `http://localhost:5100/healthchecks-ui`
- Aggregated JSON (UI writer): `http://localhost:5100/hc`
- Mvp24Hours endpoints: `/health`, `/health/ready`, `/health/live`
- OpenAPI document: `http://localhost:5100/openapi/v1.json`

Unavailable dependencies report `Unhealthy` / `Degraded` without stopping the host — that is expected for a monitoring catalog.

## Related documentation

- [Health checks catalog](../../../docs/en-us/infrastructure/health-checks.md)
- [Web API advanced](../../../docs/en-us/webapi-advanced.md)
- [Relational databases](../../../docs/en-us/database/relational.md)
- [MongoDB](../../../docs/en-us/database/nosql.md)
- [Broker / RabbitMQ](../../../docs/en-us/broker.md)
- [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

## What this sample intentionally does not cover

- Business CRUD or domain logic
- Production authentication on health endpoints
- Distributed HealthChecks UI storage backends (uses in-memory storage)
- Replacing Kubernetes/probes with a full observability stack (see the observability sample)
