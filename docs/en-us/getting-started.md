# Getting Started

Each architectural solution must be built based on technical and/or business needs.
The objective of this library is to ensure agility in the construction of digital products through structures, mechanisms and tools that, when combined correctly, offer robustness, security, performance, monitoring, observability, resilience and consistency.

The feature map below will progressively align to the locked documentation structure defined in [Documentation Scope and Information Architecture](documentation-ia-policy.md).

Quick navigation:

- [Configuration Reference](configuration-reference.md)
- [Architecture Guides](guides/architecture/home.md)
- [Release Notes](release.md) · [Version Migration](migration.md)
- [AI & MCP Resources](ai-resources/home.md)

## 🚀 Quick Installation

```bash
# Core (required)
dotnet add package Mvp24Hours.Core

# Choose your data module
dotnet add package Mvp24Hours.Infrastructure.Data.EFCore    # SQL Server, PostgreSQL, MySQL
dotnet add package Mvp24Hours.Infrastructure.Data.MongoDb   # MongoDB

# CQRS and Mediator (recommended)
dotnet add package Mvp24Hours.Infrastructure.Cqrs

# WebAPI
dotnet add package Mvp24Hours.WebAPI

# Messaging
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ

# Caching
dotnet add package Mvp24Hours.Infrastructure.Caching
```

## 📋 Features Guide

### 🗄️ Relational Database
A database that allows you to create relationships between them to guarantee data consistency and integrity.

| Database | Link |
|----------|------|
| SQL Server | [Configuration](database/relational.md?id=sql-server) |
| PostgreSQL | [Configuration](database/relational.md?id=postgresql) |
| MySQL | [Configuration](database/relational.md?id=mysql) |

**Advanced features:**
- Interceptors (Audit, SoftDelete, Concurrency, SlowQuery)
- Multi-tenancy with automatic query filters
- Bulk Operations (Insert, Update, Delete)
- Integrated Specification Pattern
- Read/Write splitting for replicas

Also see [Core & Domain](core/home.md) and [EF Core Advanced](database/efcore-advanced.md).

### 🍃 NoSQL Database

#### Document-Oriented
> Database designed to store and query data as JSON documents.

[MongoDB](database/nosql.md?id=mongodb) - With Change Streams, GridFS, Geospatial queries

#### Key-Value Oriented
Map/dictionary data structure where we use a key as identifier.

[Redis](database/nosql.md?id=redis) - Distributed cache and locks

For application caching beyond Redis as a key-value store, use the
[Caching Advanced](caching-advanced.md) guide.

### ⭐ CQRS and Mediator
Command Query Responsibility Segregation pattern with custom Mediator.

[CQRS](cqrs/home.md) - Complete documentation

**Includes:**
- Typed Commands and Queries
- Pipeline Behaviors (Logging, Validation, Caching, Transaction, Retry)
- Domain Events and Integration Events
- Event Sourcing and Sagas
- Idempotency and Scheduled Commands

### 📨 Message Broker
Software that enables applications, systems and services to communicate.

[RabbitMQ](broker.md) - Enterprise messaging

**Features:**
- Typed consumers (`IMessageConsumer<T>`)
- Request/Response pattern
- Message Scheduling
- Batch consumers
- Sagas with state machines
- Multi-tenancy

### 📦 Pipeline
Pipe and Filters pattern representing a pipe with multiple operations executed sequentially.

[Pipeline](pipeline.md) - Complete documentation

**Features:**
- Typed pipeline (`IPipeline<TInput, TOutput>`)
- Fork/Join for parallel flows
- Saga Pattern with compensation
- Checkpoint/Resume for long-running pipelines

### 🧱 Application Layer

Use [Application Services](application-services.md) for service modules,
pagination, transactional scopes, and result mapping. Continue with
[Mapping](mapping.md) and [Validation](validation.md) for DTO conversion and
input rules.

### 🌐 Web API

Start with [Web API Basics](webapi.md) and the
[Web API Advanced](webapi-advanced.md) options matrix. Native OpenAPI support
was introduced in .NET 9 and remains current on .NET 10.

### 📊 Observability
Complete observability stack with OpenTelemetry.

[Observability](observability/home.md) - Complete documentation

**Includes:**
- Distributed tracing with Activities
- Metrics (Counters, Histograms, Gauges)
- Structured logs with ILogger
- Exporters: OTLP, Console, Prometheus

For area-specific resilience wrappers, begin with the
[Resilience Selection Guide](modernization/resilience-guide.md).

### ⏰ CronJob
Background task scheduling with CRON expressions.

[CronJob](cronjob.md) - Complete documentation

**Features:**
- Retry with circuit breaker
- Distributed locking
- Health checks
- Metrics and OpenTelemetry

### 📝 OpenAPI / Swagger
Document your RESTful API with Swagger/OpenAPI.

[OpenAPI / Swagger](documentation.md) - Configuration

Native OpenAPI support was introduced in .NET 9 and remains current on .NET 10.

### 🔄 Mapping
AutoMapper for object mapping (Entity ↔ DTO).

[AutoMapper](mapping.md) - Configuration

### ✅ Validation
Data validation with FluentValidation or Data Annotations.

[Validation](validation.md) - Documentation

## 🏗️ Architectural Patterns

| Pattern | Description | Link |
|---------|-------------|------|
| **Unit of Work** | Manages transactions and persistence | [Documentation](database/use-unitofwork.md) |
| **Repository** | Data access abstraction | [Documentation](database/use-repository.md) |
| **Repository Service** | Business rules + repository | [Documentation](database/use-service.md) |
| **Specification** | Reusable filters | [Documentation](specification.md) |
| **CQRS** | Read/Write separation | [Documentation](cqrs/home.md) |
| **Event Sourcing** | Event-based persistence | [Documentation](cqrs/event-sourcing/home.md) |
| **Saga** | Distributed transactions | [Documentation](cqrs/saga/home.md) |

## 🧭 Choose an Architecture

Use the [Architecture Guides](guides/architecture/home.md) to compare solution
shapes, project boundaries, and Mvp24Hours-aligned blueprints. Keep
implementation details in the module guides above and use the
[Configuration Reference](configuration-reference.md) for the Options index.

| Need | Start here |
|------|------------|
| Compare solution shapes | [Decision Matrix](guides/architecture/decision-matrix.md) |
| Package and deploy the host | [Containerization](guides/deployment/containerization.md) |
| Release facts and upgrade path | [Release Notes](release.md) · [Version Migration](migration.md) |
| Machine downloads / MCP bridge | [AI & MCP Resources](ai-resources/home.md) |

External AI framework templates are not Mvp24Hours product documentation.

## 🔧 .NET 10 Modernization

Native APIs adopted in .NET 9 and retained by the .NET 10 source:

| Feature | Description | Link |
|---------|-------------|------|
| **HybridCache** | L1 + L2 cache with stampede protection | [Documentation](modernization/hybrid-cache.md) |
| **TimeProvider** | Time abstraction for testing | [Documentation](modernization/time-provider.md) |
| **Rate Limiting** | Native request limiting | [Documentation](modernization/rate-limiting.md) |
| **Channels** | High-performance Producer/Consumer | [Documentation](modernization/channels.md) |
| **TypedResults** | Typed Minimal APIs | [Documentation](modernization/minimal-apis.md) |

Also see the [.NET 10 Modernization Overview](modernization/dotnet9-features.md).

## 🧰 Infrastructure and Testing

The [Infrastructure overview](infrastructure/home.md) maps the cross-cutting modules and their provider-specific guides:

| Module | Guide |
|--------|-------|
| **Email and SMS** | [Email](infrastructure/email.md) · [SMS](infrastructure/sms.md) |
| **File Storage and Secrets** | [File Storage](infrastructure/file-storage.md) · [Secrets & Security](infrastructure/secrets-security.md) |
| **Identity** | [Keycloak](identity/keycloak.md) |
| **Distributed Locking** | [Distributed Locking](infrastructure/distributed-locking.md) |
| **Background Jobs** | [Background Jobs](infrastructure/background-jobs.md) · [CronJob](cronjob.md) |
| **HTTP clients and resilience** | [HTTP Clients & Resilience](infrastructure/http-resilience.md) |
| **Health Checks** | [Health Checks catalog](infrastructure/health-checks.md) |
| **Testing helpers** | [Testing cookbook](testing/home.md) |

## 🧪 Runnable Samples

All **32** sample solutions live in [`samples/`](../../samples/README.md) in this repository. They target **`net10.0`**, use local project references to `src/` by default, and ship English READMEs with run instructions and Docker Compose where applicable.

Build every sample at once with [`samples/Mvp24Hours.Samples.sln`](../../samples/Mvp24Hours.Samples.sln). After Mvp24Hours **10.0.0** is published, switch to NuGet mode with `-p:Mvp24HoursUseProjectReferences=false` — see [Using samples with published NuGet packages](migration.md?id=using-samples-with-published-nuget-packages).

Use the [sample catalog](../../samples/README.md#complete-catalog) for the full list, or the [“Which sample should I open first?”](../../samples/README.md#which-sample-should-i-open-first) table when you are choosing an architecture shape.

| Sample project | Tier | Primary documentation |
| --- | --- | --- |
| `minimal-crud-ef-customer-api` | Minimal | [Minimal API structure](guides/architecture/structures/structure-minimal-api.md), [Relational database](database/relational.md) |
| `minimal-crud-mongodb-customer-api` | Minimal | [Minimal API structure](guides/architecture/structures/structure-minimal-api.md), [MongoDB](database/nosql.md) |
| `minimal-pipeline-customer-api` | Minimal | [Pipeline](pipeline.md) |
| `simple-crud-ef-customer-api` | Simple | [Simple N-Layers](guides/architecture/structures/structure-simple-nlayers.md), [Repository](database/use-repository.md) |
| `simple-crud-ef-dapper-customer-api` | Simple | [Simple N-Layers](guides/architecture/structures/structure-simple-nlayers.md), [EF Core Advanced](database/efcore-advanced.md) |
| `simple-crud-ef-entitylog-customer-api` | Simple | [Entity interfaces](core/entity-interfaces.md), [Use entity](database/use-entity.md) |
| `simple-crud-mongodb-customer-api` | Simple | [MongoDB](database/nosql.md) |
| `simple-crud-redis-customer-api` | Simple | [Caching advanced](caching-advanced.md) |
| `simple-rabbitmq-customer-api` | Simple | [RabbitMQ](broker.md), [CQRS/RabbitMQ integration](cqrs/integration-rabbitmq.md) |
| `simple-pipeline-customer-api` | Simple | [Pipeline](pipeline.md) |
| `simple-webstatus` | Simple | [Health Checks catalog](infrastructure/health-checks.md) |
| `complex-crud-ef-customer-api` | Complex | [Complex N-Layers](guides/architecture/structures/structure-complex-nlayers.md), [Relational database](database/relational.md) |
| `complex-crud-ef-dapper-customer-api` | Complex | [Complex N-Layers](guides/architecture/structures/structure-complex-nlayers.md), [Unit of Work](database/use-unitofwork.md) |
| `complex-crud-ef-only-entity-customer-api` | Complex (deprecated) | [Decision matrix](guides/architecture/decision-matrix.md) |
| `complex-crud-ef-entitylog-customer-api` | Complex | [Entity interfaces](core/entity-interfaces.md) |
| `complex-crud-mongodb-customer-api` | Complex | [MongoDB Advanced](database/mongodb-advanced.md) |
| `complex-pipeline-customer-api` | Complex | [Pipeline](pipeline.md) |
| `complex-pipeline-builder-customer-api` | Complex | [Pipeline](pipeline.md) |
| `complex-pipeline-ports-adapters-customer-api` | Complex | [Hexagonal blueprint](guides/architecture/blueprints/template-hexagonal.md), [Pipeline](pipeline.md) |
| `complex-pipeline-ef-customer-api` | Complex | [Pipeline](pipeline.md), [Relational database](database/relational.md) |
| `complex-cqrs-ef-customer-api` | Blueprint | [CQRS blueprint](guides/architecture/blueprints/template-cqrs.md), [CQRS getting started](cqrs/getting-started.md) |
| `complex-ddd-ef-customer-api` | Blueprint | [DDD blueprint](guides/architecture/blueprints/template-ddd.md) |
| `complex-clean-architecture-customer-api` | Blueprint | [Clean Architecture blueprint](guides/architecture/blueprints/template-clean-architecture.md) |
| `complex-hexagonal-customer-api` | Blueprint | [Hexagonal blueprint](guides/architecture/blueprints/template-hexagonal.md) |
| `complex-event-driven-rabbitmq-customer-api` | Blueprint | [Event-driven blueprint](guides/architecture/blueprints/template-event-driven.md), [Inbox/Outbox](cqrs/resilience/inbox-outbox.md) |
| `microservices-aspire-customer` | Blueprint | [Microservices blueprint](guides/architecture/blueprints/template-microservices.md), [Aspire](modernization/aspire.md) |
| `complex-keycloak-customer-api` | Capability | [Keycloak identity](identity/keycloak.md) |
| `simple-cronjob-worker` | Capability | [CronJob](cronjob.md) |
| `simple-observability-customer-api` | Capability | [Observability](observability/home.md) |
| `simple-hybridcache-rate-limit-api` | Capability | [HybridCache](modernization/hybrid-cache.md), [Rate limiting](modernization/rate-limiting.md) |
| `complex-saga-rabbitmq-customer-api` | Capability | [Saga](cqrs/saga/home.md) |
| `complex-event-sourcing-customer-api` | Capability (preview) | [Event sourcing](cqrs/event-sourcing/home.md) |

Blueprint and capability samples include xUnit test projects. See the [sample testing baseline](../../samples/TESTING.md) before adding integration tests.

## 📚 Next Steps

1. **Choose your database** and configure following the documentation
2. **Configure CQRS** if you need structured Commands/Queries
3. **Add observability** for production monitoring
4. **Choose an architecture** from the [Architecture Guides](guides/architecture/home.md)
5. **Validate the application** with the [Testing cookbook](testing/home.md)
6. **Explore runnable samples** in [`samples/`](../../samples/README.md) using the catalog and decision-matrix tables
