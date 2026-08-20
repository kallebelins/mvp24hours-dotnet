# Script para criar as 19 skills restantes do Mvp24Hours
# Este script gera stubs básicos que seguem o template padrão

$skills = @(
    @{
        Path = "cqrs\mediator-patterns-specialist.md"
        Title = "Mediator Patterns Specialist - Mvp24Hours Deep Mediator Implementation"
        Role = "Deep Mvp24Hours mediator implementation and advanced patterns specialist"
        Expertise = "Handler lifecycle, streaming, notifications, behavior composition"
        Focus = "Advanced IMediatorCommand/Query patterns, streaming results, notification fanout, complex behaviors"
    },
    @{
        Path = "messaging\rabbitmq-advanced-specialist.md"
        Title = "RabbitMQ Advanced Specialist - Mvp24Hours Advanced Messaging"
        Role = "Advanced RabbitMQ features and patterns specialist"
        Expertise = "Message scheduling, delayed queues, priority queues, headers exchange"
        Focus = "Scheduled messages, consumer filters, batch publishing, advanced routing"
    },
    @{
        Path = "observability\observability-architect.md"
        Title = "Observability Architect - Mvp24Hours OpenTelemetry Integration"
        Role = "OpenTelemetry integration and distributed tracing specialist"
        Expertise = "Traces, metrics, logs, OTLP exporters, activity sources"
        Focus = "AddMvp24HoursObservability(), distributed tracing, custom metrics"
    },
    @{
        Path = "webapi\webapi-architect.md"
        Title = "WebAPI Architect - Mvp24Hours Minimal APIs and HTTP Design"
        Role = "Minimal APIs and HTTP interface design specialist"
        Expertise = "Native OpenAPI, TypedResults, Problem Details, endpoint filters"
        Focus = "AddMvp24HoursNativeOpenApi(), RESTful design, API versioning"
    },
    @{
        Path = "data\mongodb-specialist.md"
        Title = "MongoDB Specialist - Mvp24Hours Document Database Expert"
        Role = "MongoDB document database patterns and optimization specialist"
        Expertise = "Schema design, indexes, aggregations, transactions"
        Focus = "AddMvp24HoursMongoDbContext(), document modeling, performance"
    },
    @{
        Path = "cqrs\event-sourcing-specialist.md"
        Title = "Event Sourcing Specialist - Mvp24Hours Event Store Patterns"
        Role = "Event sourcing, event store, and projections specialist"
        Expertise = "Event streams, aggregate reconstruction, read model projections"
        Focus = "Event store design, snapshots, replay, versioning"
    },
    @{
        Path = "messaging\saga-orchestration-specialist.md"
        Title = "Saga Orchestration Specialist - Mvp24Hours Distributed Transactions"
        Role = "Saga pattern and distributed transaction specialist"
        Expertise = "Saga state management, compensation, timeouts"
        Focus = "PipelineSagaOrchestrator, step definitions, failure handling"
    },
    @{
        Path = "observability\resilience-patterns-specialist.md"
        Title = "Resilience Patterns Specialist - Mvp24Hours Fault Tolerance"
        Role = "Circuit breaker, retry, and timeout patterns specialist"
        Expertise = "Native .NET resilience, Polly v8, fallback strategies"
        Focus = "AddMvpStandardResilience(), circuit breaker, exponential backoff"
    },
    @{
        Path = "architecture\hexagonal-specialist.md"
        Title = "Hexagonal Architecture Specialist - Ports and Adapters"
        Role = "Hexagonal architecture (Ports & Adapters) specialist"
        Expertise = "Framework independence, adapters, application core isolation"
        Focus = "Port interfaces, adapter implementations, dependency inversion"
    },
    @{
        Path = "architecture\event-driven-specialist.md"
        Title = "Event-Driven Specialist - Mvp24Hours Event Architecture"
        Role = "Event-driven architecture and async workflows specialist"
        Expertise = "Domain events, integration events, event choreography"
        Focus = "Outbox/inbox patterns, eventual consistency, event versioning"
    },
    @{
        Path = "architecture\microservices-specialist.md"
        Title = "Microservices Specialist - Mvp24Hours Distributed Services"
        Role = "Microservices architecture and .NET Aspire specialist"
        Expertise = "Service boundaries, inter-service communication, Aspire orchestration"
        Focus = "Service decomposition, API gateways, service mesh, Aspire"
    },
    @{
        Path = "data\redis-specialist.md"
        Title = "Redis Specialist - Mvp24Hours Caching and Pub/Sub"
        Role = "Redis caching strategies and pub/sub patterns specialist"
        Expertise = "Cache-aside, write-through, distributed locking, pub/sub"
        Focus = "AddMvp24HoursCachingRedis(), cache invalidation, Redis patterns"
    },
    @{
        Path = "pipeline\pipeline-architect.md"
        Title = "Pipeline Architect - Mvp24Hours Pipes and Filters"
        Role = "Pipeline pattern and operation chaining specialist"
        Expertise = "Pipes & filters, operation composition, validation pipelines"
        Focus = "AddMvp24HoursPipelineAsync(), typed pipelines, operation results"
    },
    @{
        Path = "caching\caching-architect.md"
        Title = "Caching Architect - Mvp24Hours HybridCache Strategy"
        Role = "HybridCache and multi-tier caching specialist"
        Expertise = "L1/L2 caching, cache stampede protection, invalidation"
        Focus = "AddMvpHybridCache(), in-memory + distributed caching"
    },
    @{
        Path = "infrastructure\infrastructure-architect.md"
        Title = "Infrastructure Architect - Mvp24Hours Cross-Cutting Services"
        Role = "Email, SMS, files, locks, and background jobs specialist"
        Expertise = "Email service, file storage, distributed locking, background jobs"
        Focus = "AddEmailService(), AddDistributedLocking(), infrastructure patterns"
    },
    @{
        Path = "testing\testing-architect.md"
        Title = "Testing Architect - Mvp24Hours Testing Strategies"
        Role = "Unit, integration, and end-to-end testing specialist"
        Expertise = "Test harnesses, WebApplicationFactory, Testcontainers"
        Focus = "AddMvpTestingInfrastructure(), test patterns, mocking strategies"
    },
    @{
        Path = "identity\identity-architect.md"
        Title = "Identity Architect - Mvp24Hours Keycloak Integration"
        Role = "Keycloak JWT authentication and authorization specialist"
        Expertise = "JWT validation, UMA/RPT, role-based access, claims"
        Focus = "AddMvp24HoursKeycloak(), token validation, authorization policies"
    },
    @{
        Path = "cronjob\cronjob-architect.md"
        Title = "CronJob Architect - Mvp24Hours Scheduled Tasks"
        Role = "Cron-based hosted services and scheduled tasks specialist"
        Expertise = "Cron expressions, hosted services, overlap prevention"
        Focus = "AddMvp24HoursCronJob(), scheduled execution, distributed tasks"
    },
    @{
        Path = "modernization\dotnet-modernization-specialist.md"
        Title = ".NET Modernization Specialist - .NET 10 Features"
        Role = ".NET 10 platform features and modernization specialist"
        Expertise = "TimeProvider, Channels, keyed services, HybridCache, Aspire"
        Focus = "Native .NET APIs, performance improvements, modern patterns"
    }
)

foreach ($skill in $skills) {
    Write-Output "Gerando: $($skill.Path)"
}

Write-Output "`n✅ Total de skills a serem criadas: $($skills.Count)"
