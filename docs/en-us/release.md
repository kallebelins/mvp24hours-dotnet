# What's new?

# NET9

## 9.1.200 (January 2026) 🚀 Major Release

### ⭐ Complete CQRS Library (Mvp24Hours.Infrastructure.Cqrs)
* Full CQRS pattern implementation with custom Mediator (MediatR replacement)
* `IMediator`, `ISender`, `IPublisher` - main interfaces
* `IMediatorCommand<T>` and `IMediatorQuery<T>` - typed commands and queries
* `IMediatorNotification` - in-process notification system
* Complete Pipeline Behaviors: Logging, Performance, Validation, Caching, Transaction, Authorization, Retry
* Domain Events and Integration Events with automatic dispatch
* Event Sourcing with `IEventStore`, `AggregateRoot<T>`, Snapshots and Projections
* Saga/Process Manager with compensation and timeout
* Command idempotency with `IIdempotentCommand`
* Scheduled Commands with background service
* Inbox/Outbox patterns for reliable messaging

### 🔄 .NET 9 Modernization
* **HybridCache**: Native hybrid cache (L1 + L2) with stampede protection
* **TimeProvider**: Time abstraction for deterministic testing
* **PeriodicTimer**: Modern timer in all background services
* **System.Threading.RateLimiting**: Native rate limiting (Fixed/Sliding Window, Token Bucket)
* **System.Threading.Channels**: High-performance Producer/Consumer
* **Microsoft.Extensions.Http.Resilience**: Native HTTP resilience
* **Microsoft.Extensions.Resilience**: Generic resilience for DB/messaging
* **ProblemDetails (RFC 7807)**: Standardized API errors
* **TypedResults (.NET 9)**: Minimal APIs with strong typing
* **Source Generators**: `[LoggerMessage]` and `[JsonSerializable]` for AOT
* **Native OpenAPI**: `Microsoft.AspNetCore.OpenAPI` (replaces Swashbuckle)
* **Keyed Services**: Dependency injection by key
* **Output Caching**: Native HTTP response caching
* **.NET Aspire 9**: Cloud-native stack integration

### 📊 Modern Observability (ILogger + OpenTelemetry)
* Complete migration from `TelemetryHelper` to `ILogger<T>`
* OpenTelemetry Tracing with `ActivitySource` in all modules
* OpenTelemetry Metrics with `Meter` (Counters, Histograms, Gauges)
* OpenTelemetry Logs integrated with `ILogger`
* Correlation ID and W3C Trace Context propagation
* `AddMvp24HoursObservability()` - all-in-one configuration
* Exporters: OTLP (Jaeger, Tempo), Console, Prometheus

### 🗄️ Advanced Entity Framework Core
* Interceptors: Audit, SoftDelete, Concurrency, CommandLogging, SlowQuery
* Multi-tenancy with automatic query filters and `ITenantProvider`
* Field encryption with value converters
* Row-level security helpers
* Bulk Operations: BulkInsert, BulkUpdate, BulkDelete
* Specification Pattern integrated with `GetBySpecificationAsync()`
* `IReadOnlyRepository<T>` for queries (no write methods)
* Cursor-based pagination (keyset)
* Connection resiliency with retry policies
* Health checks for SQL Server, PostgreSQL, MySQL
* Read/Write splitting for read replicas
* Separate DbContext for read vs write (CQRS)

### 🍃 Advanced MongoDB
* Interceptors: Audit, SoftDelete, CommandLogger
* Multi-tenancy with automatic filters
* Field-level encryption (CSFLE)
* Optimized bulk operations
* Change Streams for real-time events
* GridFS for large files
* Time Series Collections
* Geospatial queries
* Text search indexes
* Health checks and replica set monitoring
* Connection resiliency with circuit breaker

### 🐇 Enterprise RabbitMQ
* Typed consumers with `IMessageConsumer<T>` (MassTransit replacement)
* Request/Response pattern with `IRequestClient<TRequest, TResponse>`
* Message Scheduling with delayed messages
* Consume and publish Pipeline/Middleware
* Automatic topology and naming conventions
* Batch consumers with `IBatchConsumer<T>`
* Transactional messaging with Outbox pattern
* Sagas integration with state machines
* Multi-tenancy with virtual hosts per tenant
* Fluent API `AddMvpRabbitMQ(cfg => {...})`
* Observability with OpenTelemetry and metrics

### 📦 Advanced Pipeline (Pipe and Filters)
* Typed pipeline `IPipeline<TInput, TOutput>`
* Fluent API `.Pipe<TIn, TOut>().Then<TNext>().Finally()`
* `IPipelineContext` with CorrelationId, Metadata, User
* Fork/Join pattern for parallel flows
* Dependency Graph between operations
* Saga Pattern with orchestrated compensation
* Checkpoint/Resume for long-running pipelines
* State Snapshots for debug/audit
* Detailed metrics per operation
* Integration with FluentValidation, Cache and OpenTelemetry

### 🌐 Enhanced WebAPI
* Exception mapping to ProblemDetails (RFC 7807)
* Native rate limiting with policies per IP, User, API Key
* Idempotency middleware for POST/PUT/PATCH
* Security headers (HSTS, CSP, X-Frame-Options)
* Request/Response logging with sensitive data masking
* API versioning (URL, Header, Query String)
* Unified health checks (/health, /health/ready, /health/live)
* Minimal APIs with `MapCommand<T>()` and `MapQuery<T>()`
* Model binders for DateOnly, TimeOnly, strongly-typed IDs

### 🏗️ Application Layer
* `IApplicationService<TEntity, TDto>` with integrated AutoMapper
* Separate `QueryService` and `CommandService` (CQRS light)
* Validation pipeline with FluentValidation
* Transaction scope with `[Transactional]` attribute
* Integrated Specification Pattern
* Configurable `ExceptionToResultMapper`
* Audit trail on command operations
* Cache with `[Cacheable]` attribute
* `PagedResult<T>` and cursor-based pagination
* Automatic soft delete

### 🔧 Base Infrastructure
* HTTP Client factory with Polly resilience
* Delegating handlers: Logging, Auth, Correlation, Telemetry, Retry, CircuitBreaker
* Distributed locking (Redis, SQL Server, PostgreSQL)
* File storage abstraction (Local, Azure Blob, S3)
* Email service (SMTP, SendGrid, Azure Communication)
* SMS service (Twilio, Azure Communication)
* Background jobs abstraction (Hangfire, Quartz)
* Secret providers (Azure KeyVault, AWS Secrets Manager)

### 💾 Advanced Caching
* `ICacheProvider` unified abstraction
* Cache patterns: Cache-Aside, Read-Through, Write-Through, Write-Behind
* Multi-level cache (L1 Memory + L2 Distributed)
* Cache tags for group invalidation
* Stampede prevention with locks
* Circuit breaker for remote cache
* Compression for large values
* `[Cacheable]` and `[CacheInvalidate]` attributes

### ⏰ Enhanced CronJob
* Configurable retry policy with circuit breaker
* Overlapping execution control
* Graceful shutdown with timeout
* Health checks per job
* Metrics: executions, duration, failures
* OpenTelemetry spans per execution
* Job dependencies (execute after another job)
* Distributed locking for clusters
* `ICronJobStateStore` for state persistence
* Pause/resume jobs at runtime
* 6-field CRON expressions (seconds)
* Configuration via appsettings.json

### 🧱 Core Fundamentals
* Guard clauses (`Guard.Against.Null`, `Guard.Against.NullOrEmpty`, etc.)
* ValueObjects: Email, Cpf, Cnpj, Money, Address, DateRange, Percentage, PhoneNumber
* Strongly-typed IDs: `EntityId<T>` with EF Core and JSON converters
* Functional patterns: `Maybe<T>`, `Either<TLeft, TRight>`
* Smart Enums: `Enumeration<T>` base class
* Entity interfaces: `IEntity<TId>`, `IAuditableEntity`, `ISoftDeletable`, `ITenantEntity`
* `IClock` and `IGuidGenerator` for testability
* Nullable reference types throughout the framework

### 📚 Complete Bilingual Documentation
* 50+ documents in PT-BR and EN-US
* Sections: CQRS, Core, Observability, Modernization
* Migration guides from MediatR and TelemetryHelper
* Architecture diagrams
* Practical code examples

### 🧪 Tests
* 1000+ unit tests
* Integration tests with Testcontainers (SQL Server, MongoDB)
* Performance benchmarks
* Test helpers: FakeLogger, FakeActivityListener, FakeMeterListener

### ⚠️ Deprecated (Will be removed in next major)
* `TelemetryHelper` - Use `ILogger<T>`
* `TelemetryLevels` - Use `LogLevel`
* `ITelemetryService` - Use `ILogger<T>`
* `AddMvp24HoursTelemetry()` - Use `AddMvp24HoursObservability()`
* `HttpClientExtensions` - Use `AddStandardResilienceHandler()`
* `MvpExecutionStrategy` - Use `ResiliencePipeline`
* `MultiLevelCache` - Use `HybridCache`

---

# NET8

## 8.3.261
* CronJob implementation.

## 8.2.102
* Implementation of route handlers for conversion and binding of parameters for Minimal API.

## 8.2.101
* Migration and refactoring of evolution to NET8.

# NETCORE

## 4.1.191
* Refactoring for asynchronous result mapping;

## 4.1.181
* Anti-pattern removal;
* Separation of log entity contexts for contract use only;
* Update and detail architectural resources in documentation;
* Correction of dependency injection in the RabbitMQ and Pipeline client;
* Configuration of isolated consumers for RabbitMQ client;
* Implementation of tests for database context with log;

## 3.12.262
* Refactoring extensions.

## 3.12.261
* Middleware test implementation.

## 3.12.221
* Implementation of Delegation Handlers to propagate keys in the Header (correlation-id, authorization, etc);
* Implementation of Polly to apply concepts of resilience and fault tolerance;
* Correction of automatic loading of mapping classes with IMapFrom;

## 3.12.151
* Removed generic typing from the IMapFrom class;
* Implementation of Testcontainers for RabbitMQ, Redis and MongoDb projects;

## 3.2.241
* Refactoring to migrate json file settings to fluent extensions;
* Replacement of the notification pattern;
* Review of templates;
* Addition of HealthCheck to all samples;
* Creation of a basic WebStatus project with HealthCheckUI;
* Replacement of logging dependencies for trace injection through actions;
* Trace/Verbose in all main libraries and layers;
* Configuration of transaction isolation level for queries with EF;
* Refactoring of the RabbitMQ library for consumer injection and fluid configuration for "DeadLetterQueue";
* Persistent connection and resilience with Polly for RabbitMQ;
* Implementation of asynchronous consumer for RabbitMQ;
* Pipeline adjustment to allow adding messages to the package (info, error, warning, success) - replacement of the notification pattern;
* Validation change (FluentValidation or DataAnnotations) to return list of messages - replacement of notification pattern;
* Changed documentation and added configuration for WebAPI;
* Refactoring of library testing;
* Refactoring for migration from Core to .NET 6.

## Other versions...
* Relational database (SQL Server, PostgreSql and MySql)
* NoSql database (MongoDb and Redis)
* Message Broker (RabbitMQ)
* Pipeline (Pipe and Filters pattern)
* Documentation (Swagger)
* Mapping (AutoMapper)
* Logging
* Standards for data validation (FluentValidation and Data Annotations), specifications (Specification pattern), work unit, repository, among others.