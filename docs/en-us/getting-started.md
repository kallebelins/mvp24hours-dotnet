# Getting Started

Each architectural solution must be built based on technical and/or business needs.
The objective of this library is to ensure agility in the construction of digital products through structures, mechanisms and tools that, when combined correctly, offer robustness, security, performance, monitoring, observability, resilience and consistency.

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
| SQL Server | [Configuration](database/relational?id=sql-server) |
| PostgreSQL | [Configuration](database/relational?id=postgresql) |
| MySQL | [Configuration](database/relational?id=mysql) |

**Advanced features:**
- Interceptors (Audit, SoftDelete, Concurrency, SlowQuery)
- Multi-tenancy with automatic query filters
- Bulk Operations (Insert, Update, Delete)
- Integrated Specification Pattern
- Read/Write splitting for replicas

### 🍃 NoSQL Database

#### Document-Oriented
> Database designed to store and query data as JSON documents.

[MongoDB](database/nosql?id=mongodb) - With Change Streams, GridFS, Geospatial queries

#### Key-Value Oriented
Map/dictionary data structure where we use a key as identifier.

[Redis](database/nosql?id=redis) - Distributed cache and locks

### ⭐ CQRS and Mediator (New!)
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

### 📊 Observability (New!)
Complete observability stack with OpenTelemetry.

[Observability](observability/home.md) - Complete documentation

**Includes:**
- Distributed tracing with Activities
- Metrics (Counters, Histograms, Gauges)
- Structured logs with ILogger
- Exporters: OTLP, Console, Prometheus

### ⏰ CronJob
Background task scheduling with CRON expressions.

[CronJob](cronjob.md) - Complete documentation

**Features:**
- Retry with circuit breaker
- Distributed locking
- Health checks
- Metrics and OpenTelemetry

### 📝 Documentation
Document your RESTful API with Swagger/OpenAPI.

[Swagger](swagger.md) - Configuration

**New:** Native OpenAPI support (.NET 9)

### 🔄 Mapping
AutoMapper for object mapping (Entity ↔ DTO).

[AutoMapper](automapper.md) - Configuration

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

## 🔧 .NET 9 Modernization

Native .NET 9 features integrated:

| Feature | Description | Link |
|---------|-------------|------|
| **HybridCache** | L1 + L2 cache with stampede protection | [Documentation](modernization/hybrid-cache.md) |
| **TimeProvider** | Time abstraction for testing | [Documentation](modernization/time-provider.md) |
| **Rate Limiting** | Native request limiting | [Documentation](modernization/rate-limiting.md) |
| **Channels** | High-performance Producer/Consumer | [Documentation](modernization/channels.md) |
| **TypedResults** | Typed Minimal APIs | [Documentation](modernization/minimal-apis.md) |

## 📚 Next Steps

1. **Choose your database** and configure following the documentation
2. **Configure CQRS** if you need structured Commands/Queries
3. **Add observability** for production monitoring
4. **Explore examples** at [mvp24hours-dotnet-samples](https://github.com/kallebelins/mvp24hours-dotnet-samples)
