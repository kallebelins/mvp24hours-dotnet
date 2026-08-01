# Microservices + .NET Aspire — Customer Sample

This sample demonstrates a minimal **microservices architecture** orchestrated locally with **.NET Aspire 13.x**, built on **Mvp24Hours** and targeting **net10.0**.

## Status

- Migration status: `migrated`
- Target framework: `net10.0`
- Mvp24Hours consumption: local project references by default; matching published packages are optional

## Features

- Customer API and Notification worker with separate data stores and RabbitMQ integration
- Aspire AppHost for local SQL Server + RabbitMQ orchestration and dashboard
- Shared ServiceDefaults for health checks and HTTP resilience
- Standalone run mode for each service without AppHost

## Architecture

- Tier: `Blueprint`
- Shape: two services (Customer API + Notification worker) composed locally with .NET Aspire AppHost
- Why this shape fits: demonstrates service boundaries, separate data stores, and async messaging without production orchestration complexity

See [Solution overview](#solution-overview) below for project layout and communication flow.

---

## ⚠️ When NOT to choose microservices

Microservices solve distribution and scale problems, but they introduce **operational complexity** that is rarely justified for small teams or early-stage systems.

**Do not choose microservices if:**

- Your team is small (< 5–8 engineers sharing the codebase)
- You are in the discovery / MVP phase — your bounded contexts are not yet stable
- Your data is highly relational — cross-service joins become expensive
- You lack container orchestration maturity (Kubernetes, Docker Swarm, etc.)
- Distributed tracing, service mesh, and multi-database ops are new to your team

**Read before deciding:**

- [Architecture Decision Matrix](../../docs/en-us/guides/architecture/decision-matrix.md)
- [Microservices Blueprint / Template](../../docs/en-us/guides/architecture/blueprints/template-microservices.md)

Related docs:

- [.NET Aspire Integration](../../docs/en-us/modernization/aspire.md)
- [Containerization Guide](../../docs/en-us/modernization/containerization.md)
- [Observability Home](../../docs/en-us/observability/home.md)

---

## Solution overview

```
microservices-aspire-customer/
├── AppHost/             # .NET Aspire AppHost — local orchestrator
├── ServiceDefaults/     # Shared health checks + HTTP resilience defaults
├── CustomerAPI/         # HTTP API: create/get customers, publish events to RabbitMQ
└── NotificationWorker/  # Background worker: consume events, persist notification log
```

### How the services communicate

```
┌──────────────┐  POST /api/customers  ┌───────────────────────────────────────────┐
│   Client     │ ─────────────────────►│  CustomerAPI (HTTP, port 5100)            │
└──────────────┘                       │  ┌─────────────────────────────────────┐   │
                                       │  │  CustomerDbContext (SQL Server)     │   │
                                       │  │  MyAspireCustomerDb                 │   │
                                       │  └─────────────────────────────────────┘   │
                                       │  Publishes CustomerCreatedEvent            │
                                       └───────────────────────┬───────────────────┘
                                                               │ amq.direct
                                                               ▼
                                                       ┌───────────────┐
                                                       │   RabbitMQ    │
                                                       └───────┬───────┘
                                                               │ CustomerCreatedEvent queue
                                                               ▼
                                       ┌───────────────────────────────────────────┐
                                       │  NotificationWorker (HTTP, port 5101)     │
                                       │  ┌─────────────────────────────────────┐   │
                                       │  │  NotificationDbContext (In-Memory)  │   │
                                       │  │  MyAspireNotificationDb             │   │
                                       │  └─────────────────────────────────────┘   │
                                       └───────────────────────────────────────────┘
```

Each service has its **own data store** — the core microservices principle. The two stores are intentionally decoupled: one persists customers, the other persists notification audit logs.

---

## Configuration

When you run through **AppHost**, Aspire injects SQL Server and RabbitMQ connection strings automatically — no manual `appsettings` edits are required.

For **standalone** runs, each service accepts standard configuration:

| Key | Required | Description |
| --- | --- | --- |
| `ConnectionStrings:CustomerDb` | No | SQL Server for CustomerAPI; omitted → in-memory EF |
| `ConnectionStrings:RabbitMQ` | No | Broker for publish/consume; omitted → messaging skipped with warning logs |

Configure secrets with environment variables, user secrets, or a secret store. Never commit credentials.

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0 |
| Docker Desktop (or compatible runtime) | 24+ |
| .NET Aspire workload | 13.x |

Install the Aspire workload once:

```bash
dotnet workload install aspire
```

Verify:

```bash
dotnet workload list
```

---

## Run

### With Aspire (recommended)

```bash
dotnet run --project samples/src/microservices-aspire-customer/AppHost/AppHost.csproj
```

Aspire starts:

- **SQL Server** container → `MyAspireCustomerDb` (for CustomerAPI)
- **RabbitMQ** container with management UI → `amq.direct` exchange
- **CustomerAPI** → `http://localhost:5100` (port auto-assigned by Aspire; check dashboard)
- **NotificationWorker** → health endpoint at `/health`
- **Aspire Dashboard** → `http://localhost:15888` for traces, logs, and resource health

Connection strings are injected automatically by Aspire — no manual configuration needed.

### Standalone (no AppHost)

Each service compiles and runs without the AppHost. Use this for CI or focused development.

#### CustomerAPI

```bash
dotnet build samples/src/microservices-aspire-customer/CustomerAPI/CustomerAPI.csproj
dotnet run  --project samples/src/microservices-aspire-customer/CustomerAPI/CustomerAPI.csproj
```

Without a SQL Server connection string, CustomerAPI falls back to an **in-memory database**.
Without a RabbitMQ connection string, publishing is skipped with a warning log.

Health: `GET http://localhost:5000/health`

Swagger UI (Development): `http://localhost:5000/swagger`

#### NotificationWorker

```bash
dotnet build samples/src/microservices-aspire-customer/NotificationWorker/NotificationWorker.csproj
dotnet run  --project samples/src/microservices-aspire-customer/NotificationWorker/NotificationWorker.csproj
```

Without a RabbitMQ connection string, the worker starts but does not consume.

Health: `GET http://localhost:5001/health`

---

## Full solution build

```bash
dotnet build samples/src/microservices-aspire-customer/Microservices-Aspire-Customer.sln
```

> **Note:** The `AppHost` project requires the Aspire workload (`dotnet workload install aspire`).
> If the workload is absent, build the service projects individually as shown above — they compile independently.

---

## API quick reference

### Create a customer (triggers RabbitMQ publish)

```http
POST /api/customers
Content-Type: application/json

{
  "name": "Ada Lovelace",
  "email": "ada@example.com"
}
```

Response `201 Created`:

```json
{
  "id": "...",
  "name": "Ada Lovelace",
  "email": "ada@example.com",
  "createdAt": "2026-07-28T12:00:00Z",
  "isActive": true
}
```

### Get all customers

```http
GET /api/customers
```

### Get customer by ID

```http
GET /api/customers/{id}
```

---

## Health endpoints

| Service | Endpoint | Description |
|---|---|---|
| CustomerAPI | `/health/live` | Liveness probe |
| CustomerAPI | `/health/ready` | Readiness probe (SQL + RabbitMQ) |
| CustomerAPI | `/health` | All checks |
| CustomerAPI | `/hc` | HealthChecks UI format |
| NotificationWorker | `/health/live` | Liveness probe |
| NotificationWorker | `/health/ready` | Readiness probe (RabbitMQ) |
| NotificationWorker | `/health` | All checks |
| NotificationWorker | `/hc` | HealthChecks UI format |

---

## Package notes

- **Aspire 13.4.6** — AppHost uses `Sdk="Aspire.AppHost.Sdk/13.4.6"` (no explicit `Aspire.Hosting.AppHost` package needed since Aspire 13.0).
- **Aspire.Hosting.RabbitMQ / SqlServer 13.4.6** — declared in `samples/Directory.Packages.props`.
- **Mvp24Hours.Infrastructure.RabbitMQ** — used for both publishing (CustomerAPI) and consuming (NotificationWorker).
- **ServiceDefaults** — a lightweight shared project (no Aspire SDK dependency) providing health check registration and HTTP resilience defaults.

---

## Data stores

| Store | Service | Technology | Notes |
|---|---|---|---|
| `MyAspireCustomerDb` | CustomerAPI | SQL Server (Aspire) / In-Memory | SQL Server when orchestrated, in-memory standalone |
| `MyAspireNotificationDb` | NotificationWorker | EF Core In-Memory | Teaching scope — resets on restart |

---

## Related documentation

- [Microservices blueprint](../../../docs/en-us/guides/architecture/blueprints/template-microservices.md)
- [Architecture decision matrix](../../../docs/en-us/guides/architecture/decision-matrix.md)
- [.NET Aspire integration](../../../docs/en-us/modernization/aspire.md)
- [Containerization guide](../../../docs/en-us/modernization/containerization.md)
- [Observability home](../../../docs/en-us/observability/home.md)
- [Getting started](../../../docs/en-us/getting-started.md)

## Learning path

1. Understand why a monolith may be right → [Decision Matrix](../../docs/en-us/guides/architecture/decision-matrix.md)
2. Study the microservices blueprint → [Template Microservices](../../docs/en-us/guides/architecture/blueprints/template-microservices.md)
3. Explore Aspire integration → [Aspire Guide](../../docs/en-us/modernization/aspire.md)
4. Study simpler messaging → [simple-rabbitmq-customer-api](../simple-rabbitmq-customer-api/)
5. Study advanced patterns → [complex-clean-architecture-customer-api](../complex-clean-architecture-customer-api/)

## What this sample intentionally does not cover

- Kubernetes or production service mesh deployment
- Shared databases, two-phase commit, or synchronous cross-service transactions
- Full observability stack beyond Aspire dashboard defaults and health endpoints
