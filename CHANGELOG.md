# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [9.1.210] - 2026-01

### Corrigido

- **Pacotes NuGet**: Correção de versões de pacotes em todos os projetos
  - `Mvp24Hours.Core.csproj`
  - `Mvp24Hours.Application.csproj`
  - `Mvp24Hours.Infrastructure.csproj`
  - `Mvp24Hours.Infrastructure.Caching.csproj`
  - `Mvp24Hours.Infrastructure.Caching.Redis.csproj`
  - `Mvp24Hours.Infrastructure.Cqrs.csproj`
  - `Mvp24Hours.Infrastructure.CronJob.csproj`
  - `Mvp24Hours.Infrastructure.Data.EFCore.csproj`
  - `Mvp24Hours.Infrastructure.Data.MongoDb.csproj`
  - `Mvp24Hours.Infrastructure.Pipe.csproj`
  - `Mvp24Hours.Infrastructure.RabbitMQ.csproj`
  - `Mvp24Hours.WebAPI.csproj`

### Removido

- **Arquivos Obsoletos**: Remoção de arquivos de DelegatingHandlers e TypedHttpClient
  - `PropagationAuthorizationDelegatingHandler.cs`
  - `PropagationCorrelationIdDelegatingHandler.cs`
  - `PropagationHeaderDelegatingHandler.cs`
  - `TypedHttpClient.cs`

---

## [9.1.200] - 2026-01 🚀 Major Release

> **Migração para .NET 9** - Esta versão introduz mudanças significativas para adotar as APIs nativas do .NET 9.
> Consulte o guia de migração em `docs/pt-br/modernization/migration-guide.md` ou `docs/en-us/modernization/migration-guide.md`.

### Adicionado

#### Biblioteca CQRS Completa (Mvp24Hours.Infrastructure.Cqrs)
- `IMediator`, `ISender`, `IPublisher` - interfaces principais do Mediator
- `IMediatorCommand<TResponse>`, `IMediatorCommand` - commands CQRS
- `IMediatorQuery<TResponse>` - queries CQRS
- `IMediatorNotification` - sistema de notificações in-process
- `IMediatorRequestHandler<TRequest, TResponse>` - handlers genéricos
- Pipeline Behaviors:
  - `LoggingBehavior` - log de início, fim e tempo
  - `PerformanceBehavior` - alerta de requisições lentas
  - `UnhandledExceptionBehavior` - captura e log de exceções
  - `ValidationBehavior` - integração com FluentValidation
  - `CachingBehavior` - cache com IDistributedCache
  - `TransactionBehavior` - integração com IUnitOfWork
  - `AuthorizationBehavior` - autorização via policies
  - `RetryBehavior` - retry com backoff exponencial
  - `TimeoutBehavior` - timeout configurável por request
  - `CircuitBreakerBehavior` - circuit breaker para commands
  - `IdempotencyBehavior` - prevenção de duplicatas
- Domain Events:
  - `IDomainEvent` e `DomainEventBase`
  - `IDomainEventHandler<TEvent>` e `DomainEventDispatcher`
  - `IHasDomainEvents` para entidades/agregados
  - `SaveChangesWithEventsAsync` para EFCore e MongoDB
- Integration Events:
  - `IIntegrationEvent` e `IntegrationEventBase`
  - `IIntegrationEventHandler<TEvent>`
  - `IIntegrationEventOutbox` e `InMemoryIntegrationEventOutbox`
  - `RabbitMqIntegrationEventPublisher`
- Event Sourcing:
  - `IEventStore` e `EventStream`
  - `AggregateRoot<TId>` com Apply/Raise
  - `Snapshot` e `SnapshotStore`
  - `EventStoreRepository<T>`
  - `IProjection`, `IProjectionHandler<TEvent>`, `ProjectionManager`
- Saga/Process Manager:
  - `ISaga<TData>`, `SagaBase<TData>`
  - `ISagaOrchestrator` e `ISagaStateStore`
  - `CompensatingCommand` para rollback
  - Timeout e expiração de sagas
- Observabilidade CQRS:
  - `IRequestContext` com CorrelationId/CausationId
  - `RequestContextBehavior` para propagação de contexto
  - `AuditBehavior` e `IAuditStore`
- Multi-tenancy:
  - `ITenantContext`, `TenantBehavior`
  - `ICurrentUser`, `CurrentUserBehavior`
  - Filtros automáticos por tenant em queries
- Inbox/Outbox:
  - `InboxMessage`, `IInboxStore`, `InboxProcessor`
  - `OutboxProcessor` com retry e DLQ
- Scheduled Commands:
  - `IScheduledCommand`, `ICommandScheduler`
  - `ScheduledCommandHostedService`
- Decorators e Extensibilidade:
  - `IPreProcessor<TRequest>`, `IPostProcessor<TRequest, TResponse>`
  - `IExceptionHandler<TRequest, TException>`
- Streaming: `IStreamRequest<T>`, `IStreamRequestHandler<T>` com IAsyncEnumerable

#### Modernização .NET 9
- **HybridCache** (Microsoft.Extensions.Caching.Hybrid):
  - `AddMvpHybridCache()` para configuração
  - `HybridCacheProvider` como `ICacheProvider`
  - Tags para invalidação em grupo
  - `InMemoryHybridCacheTagManager` e `RedisHybridCacheTagManager`
- **TimeProvider**:
  - `TimeProviderAdapter` (ponte TimeProvider → IClock)
  - `ClockAdapter` (ponte IClock → TimeProvider)
  - `AddTimeProvider()`, `AddClock()`, `ReplaceTimeProvider()`
  - `FakeTimeProviderHelper` para testes
- **PeriodicTimer**:
  - `PeriodicTimerHelper` com padrões comuns
  - Migração de todos os background services
- **System.Threading.RateLimiting**:
  - `IRateLimiterProvider`, `NativeRateLimiterProvider`
  - `RateLimitingPipelineMiddleware` para Pipeline
  - `RateLimitingConsumeFilter`, `RateLimitingPublishFilter` para RabbitMQ
- **System.Threading.Channels**:
  - `IChannel<T>`, `MvpChannel<T>`
  - `ChannelFactory`, `ProducerConsumer<T>`
  - `ChannelPipeline<TInput, TOutput>`
  - `ChannelBatchProcessor<T>`
- **Microsoft.Extensions.Http.Resilience**:
  - `AddHttpClientWithStandardResilience()`
  - `NativeResilienceOptions` com presets
  - `NativeResilienceBuilder` para configuração customizada
- **Microsoft.Extensions.Resilience**:
  - `INativeResiliencePipeline`, `NativeResiliencePipeline`
  - `NativeDbResilienceExtensions` para EFCore
  - `NativeMongoDbResilienceExtensions` para MongoDB
  - `NativePipelineResilienceMiddleware` para Pipe
  - `NativeResilienceBehavior` para CQRS
- **ProblemDetails (RFC 7807)**:
  - `AddNativeProblemDetails()`, `AddNativeProblemDetailsAll()`
  - `UseNativeProblemDetailsHandling()`
  - Helpers: `NotFoundProblem()`, `ValidationProblem()`, `ConflictProblem()`, etc.
- **TypedResults (.NET 9)**:
  - `ToNativeTypedResult()` para `IBusinessResult<T>`
  - `MapNativeCommand<T>()`, `MapNativeQuery<T>()` para CQRS
  - Filtros: NativeValidation, ExceptionHandling, Logging, CorrelationId, Idempotency, Timeout
- **Source Generators**:
  - `Mvp24HoursJsonSerializerContext` para serialização AOT
  - `[LoggerMessage]` em todos os módulos (CoreLoggerMessages, PipelineLoggerMessages, etc.)
- **OpenAPI Nativo**:
  - `AddMvp24HoursNativeOpenApi()`, `MapMvp24HoursNativeOpenApi()`
  - `SecuritySchemeTransformer`, `OpenApiDocumentTransformers`
- **Keyed Services**:
  - `ServiceKeys.cs` com constantes
  - `KeyedServiceExtensions.cs`
- **Output Caching**:
  - `AddMvp24HoursOutputCache()`, `AddMvp24HoursOutputCacheWithRedis()`
  - `IOutputCacheInvalidator`
  - Políticas: Short, Medium, Long, VeryLong, NoCache, Authenticated, Api
- **.NET Aspire 9**:
  - `AddMvp24HoursAspireDefaults()`
  - `AddMvp24HoursRedisFromAspire()`, `AddMvp24HoursRabbitMQFromAspire()`
  - `AddMvp24HoursSqlServerFromAspire()`, `AddMvp24HoursMongoDbFromAspire()`
- **IOptions<T> Validation**:
  - `IOptionsValidator<T>`, `OptionsValidatorBase<T>`
  - `AddOptionsWithValidation<T>()`, `AddOptionsWithValidation<T, TValidator>()`
  - `AddOptionsValidatorsFromAssembly()`

#### Observabilidade (ILogger + OpenTelemetry)
- OpenTelemetry Tracing:
  - `ActivitySources` para todos os módulos (Core, Pipeline, Repository, Mediator, RabbitMQ, CronJob, HttpClient)
  - `ActivityHelper` com convenções semânticas
  - `IActivityEnricher` para enriquecimento customizável
  - `TracePropagation` com W3C Trace Context
- OpenTelemetry Metrics:
  - `MetricSources` com Meters por módulo
  - `PipelineMetrics`, `RepositoryMetrics`, `MessagingMetrics`, `CqrsMetrics`, `CacheMetrics`, `HttpMetrics`, `CronJobMetrics`
  - `MetricNames.cs` com convenções semânticas
- OpenTelemetry Logs:
  - Integração `ILogger` ↔ OpenTelemetry Logs
  - Correlação automática logs ↔ traces (TraceId, SpanId)
  - Log sampling para ambientes de alta carga
- Contexto e Correlação:
  - `ICorrelationIdAccessor`, `CorrelationIdAccessor`
  - `CorrelationIdMiddleware`, `RequestContextMiddleware`
  - `BaggagePropagation` para TenantId, UserId
  - `ILogEnricher` (UserContextLogEnricher, TenantContextLogEnricher)
- Configuração:
  - `AddMvp24HoursLogging()`, `AddMvp24HoursTracing()`, `AddMvp24HoursMetrics()`
  - `AddMvp24HoursObservability()` - all-in-one
  - `AddMvp24HoursOpenTelemetry()` com OTLP, Console, Prometheus exporters
  - `ObservabilityOptions` centralizado
- Testabilidade:
  - `FakeLogger<T>`, `InMemoryLoggerProvider`
  - `FakeActivityListener`, `FakeMeterListener`
  - `LogAssertions`, `ActivityAssertions`, `MetricAssertions`
  - `ObservabilityTestFixture`

#### EFCore Avançado
- Interceptors:
  - `AuditSaveChangesInterceptor`
  - `SoftDeleteInterceptor`
  - `ConcurrencyInterceptor`
  - `CommandLoggingInterceptor`
  - `SlowQueryInterceptor`
  - `TenantSaveChangesInterceptor`
  - `StructuredLoggingInterceptor`
- Multi-tenancy:
  - `ITenantProvider`, query filters automáticos
  - `TenantModelBuilderExtensions`
  - `RowLevelSecurityHelper`
- Performance:
  - `AsNoTracking()` configurável, `AsNoTrackingWithIdentityResolution()`
  - Compiled queries, Split queries
  - `IAsyncEnumerable<T>` streaming
  - Query tags (`TagWith()`)
  - `ProjectTo<TDto>` com AutoMapper
- Bulk Operations:
  - `BulkInsertAsync()`, `BulkUpdateAsync()`, `BulkDeleteAsync()`
  - Progress callback
  - `ExecuteUpdate`, `ExecuteDelete` (.NET 7+)
- Specification Pattern:
  - `GetBySpecificationAsync()`, `CountBySpecificationAsync()`, `AnyBySpecificationAsync()`
  - `IReadOnlyRepository<T>`, `IReadOnlyRepositoryAsync<T>`
  - Cursor-based pagination (keyset)
- Resiliência:
  - `EnableRetryOnFailure()`, retry policies por exceção
  - Timeout por query
  - DbContext pooling
- Health Checks:
  - `SqlServerHealthCheck`, `PostgreSqlHealthCheck`, `MySqlHealthCheck`
- Read/Write Splitting:
  - `ConnectionResolver`, `ReplicaSelector`
  - DbContext separado para leitura
- Testabilidade:
  - `UseInMemoryDatabase` helpers
  - `IDataSeeder<T>`, `DbContextFactory`
  - `IRepositoryFake<T>`

#### MongoDB Avançado
- Interceptors: `AuditInterceptor`, `SoftDeleteInterceptor`, `AuditTrailInterceptor`
- Multi-tenancy: Query filters, `ITenantProvider`, Row-level security
- Field-level encryption (CSFLE)
- Bulk operations: `BulkInsertAsync()`, `BulkUpdateAsync()`, `BulkDeleteAsync()`
- Change Streams para eventos real-time
- GridFS para arquivos grandes
- Time Series Collections
- Geospatial queries
- Text search indexes
- Resiliência: Connection resiliency, Circuit breaker, Retry policies
- Health checks: Conectividade, Replica set status, Índices
- Read preference configurável
- Testabilidade: In-memory provider, `IRepositoryFake<T>`, Testcontainers helpers

#### RabbitMQ Enterprise
- Consumers tipados:
  - `IMessageConsumer<TMessage>`, `ConsumeContext<TMessage>`
  - `IMessage<TPayload>`, `IMessageSerializer`
  - `ConsumerDefinition<TConsumer>`
  - `IFaultConsumer<TMessage>`
- Request/Response:
  - `IRequestClient<TRequest, TResponse>`
  - `Response<T>` wrapper
  - `IRequestHandler<TRequest, TResponse>`
  - `RequestTimeoutException`
- Message Scheduling:
  - `IMessageScheduler`
  - `ScheduleMessage<T>()` por DateTime ou TimeSpan
  - `CancelScheduledMessage()`
  - Recurring messages
- Pipeline/Middleware:
  - `IConsumeFilter<TMessage>`, `IPublishFilter<TMessage>`, `ISendFilter`
  - Filtros: Logging, ExceptionHandling, Correlation, Telemetry, Validation
- Topologia:
  - `IEndpointNameFormatter`, `IMessageTopology<TMessage>`
  - Topic Exchange, Fanout Exchange
  - Auto-binding, Exchange-to-exchange bindings
- Batch Consumers:
  - `IBatchConsumer<TMessage>`, `BatchConsumeContext<TMessage>`
  - Batch size, timeout, parallel processing
- Transactional Messaging:
  - `ITransactionalBus`
  - Integração com `IUnitOfWork`
  - `InMemoryOutbox`
- Sagas:
  - `ISagaConsumer<TData, TMessage>`
  - `SagaStateMachine<TInstance>`
  - Saga persistence (Redis, SQL, MongoDB)
- Multi-tenancy:
  - Virtual hosts por tenant
  - `ITenantConsumeFilter`
  - Connection pool por tenant
- API Fluente: `AddMvpRabbitMQ(cfg => { cfg.Host(); cfg.AddConsumer<T>(); })`
- Observabilidade: ActivitySource, Métricas Prometheus
- Testing: `InMemoryBus`, `TestHarness`, `TestConsumeContext<T>`

#### Pipeline Avançado
- Tipagem:
  - `IPipeline<TInput, TOutput>`, `ITypedOperation<TInput, TOutput>`
  - API fluente `.Pipe<TIn, TOut>().Then<TNext>().Finally()`
  - `IOperationResult<T>`, `OperationChain<T>`
- Contexto:
  - `IPipelineContext` (CorrelationId, CausationId, Metadata, User)
  - State Snapshots
  - Activity spans
- Fluxo Avançado:
  - Fork/Join pattern
  - Dependency Graph
  - OperationPriority
  - Saga Pattern com compensação
  - Checkpoint/Resume
- Observabilidade:
  - Métricas por operação (duration, memory, success rate)
  - Logging estruturado
  - Pipeline Visualization (diagrama de fluxo)
  - Health Check agregado
  - Eventos: OnOperationStart, OnOperationEnd, OnPipelineComplete
- Integração: FluentValidation, IAsyncEnumerable, IDistributedCache, OpenTelemetry

#### WebAPI
- Exception Handling:
  - ProblemDetails (RFC 7807)
  - `ExceptionToProblemDetailsMapper`
  - Mapeamento de exceções de domínio para HTTP status codes
- Rate Limiting:
  - Políticas por IP, User, API Key
  - Fixed/Sliding Window, Token Bucket
  - Headers X-RateLimit-*
  - Redis para distribuído
- Idempotência:
  - `IdempotencyKeyMiddleware`
  - Integração com `IIdempotentCommand`
  - Retry-after headers
- Segurança:
  - Security headers (HSTS, CSP, X-Frame-Options)
  - API Key authentication
  - IP filtering
  - Input sanitization
- Observabilidade:
  - Request/Response logging com masking
  - OpenTelemetry tracing
  - Métricas de endpoints
  - Correlation ID propagation
- API Versioning: URL, Header, Query String
- Health Checks: `/health`, `/health/ready`, `/health/live`
- Minimal APIs:
  - `MapCommand<T>()`, `MapQuery<T>()`
  - Endpoint filters
  - `TypedResults` helpers

#### Application Layer
- Services:
  - `IApplicationService<TEntity, TDto>`, `ApplicationServiceBase<T>`
  - `IApplicationService<TEntity, TDto, TCreateDto, TUpdateDto>`
  - `QueryService`, `CommandService` (CQRS light)
  - `IReadOnlyApplicationService<T>`
- AutoMapper integrado
- Validation pipeline com `IValidationService<T>`
- Transaction scope com `[Transactional]`
- Specification Pattern: `GetBySpecificationAsync<TSpec>()`
- Exception handling: `ExceptionToResultMapper`, Result status codes
- Observabilidade: Logging, Audit trail, OpenTelemetry, Correlation ID
- Cache: `[Cacheable]`, invalidação automática
- Pagination: `PagedResult<T>`, cursor-based
- Soft delete automático

#### Infrastructure Base
- HTTP Client:
  - `ITypedHttpClient<TApi>`, `HttpClientBuilder`
  - Delegating handlers: Logging, Auth, Correlation, Telemetry, Retry, CircuitBreaker, Timeout, Compression
  - Polly resilience
- Distributed Locking:
  - `IDistributedLock`, `IDistributedLockFactory`
  - Providers: Redis (RedLock), SQL Server, PostgreSQL, InMemory
- File Storage:
  - `IFileStorage`
  - Providers: Local, Azure Blob, AWS S3, InMemory
  - Presigned URLs, versioning, soft delete
- Email Service:
  - `IEmailService`, `EmailMessage`
  - Providers: SMTP, SendGrid, Azure Communication, InMemory
  - Template engine (Razor, Scriban)
- SMS Service:
  - `ISmsService`, `SmsMessage`
  - Providers: Twilio, Azure Communication, InMemory
- Background Jobs:
  - `IJobScheduler`, `IBackgroundJob`
  - Providers: Hangfire, Quartz, InMemory
  - Fire-and-forget, Delayed, Recurring, Continuations, Batches
- Secret Providers:
  - `ISecretProvider`
  - Azure KeyVault, AWS Secrets Manager, Environment Variables
- Health Checks para todos os subsistemas

#### Caching Avançado
- `ICacheProvider`: Memory, Distributed, HybridCache
- Patterns: Cache-Aside, Read-Through, Write-Through, Write-Behind, Refresh-Ahead
- Multi-level cache (L1 + L2)
- Invalidação: Tags, Pub/sub, Dependency tracking
- Resiliência: Circuit breaker, Fallback, Graceful degradation
- Performance: Compression, Batch operations, Prefetching, Warming
- Observabilidade: Metrics, Tracing, Health checks

#### CronJob Melhorado
- Correções: Memory leak, IAsyncDisposable, PeriodicTimer
- Resiliência: Retry policy, Circuit breaker, Overlapping control, Graceful shutdown
- Observabilidade: Health checks, Métricas, OpenTelemetry spans, Logging estruturado
- Funcionalidades:
  - `ICronJobContext` (JobId, StartTime, Attempt)
  - CRON de 6 campos (segundos)
  - Job dependencies
  - Distributed locking
  - `ICronJobStateStore`
  - Pausar/resumir em runtime
  - Hooks: OnJobStarting, OnJobCompleted, OnJobFailed
- Configuração: `CronJobOptions<T>`, `CronJobGlobalOptions`, appsettings.json, validação no startup

#### Core Fundamentals
- Guard clauses: `Guard.Against.Null`, `NullOrEmpty`, `OutOfRange`, `NegativeOrZero`, `InvalidEmail`, `InvalidCpf`, `InvalidCnpj`, `Default`, `InvalidFormat`
- ValueObjects: Email, Cpf, Cnpj, Money, Address, DateRange, Percentage, PhoneNumber (com TryParse e implicit operators)
- Strongly-typed IDs: `EntityId<T>` com conversores EF Core e JSON
- Functional patterns: `Maybe<T>`, `Either<TLeft, TRight>` com Map, Bind, Match
- Smart Enums: `Enumeration<T>` com FromValue, FromName, GetAll
- Entity interfaces: `IEntity<TId>`, `IAuditableEntity`, `ISoftDeletable`, `ITenantEntity`, `IVersionedEntity`
- `IClock`, `SystemClock`, `TestClock`
- `IGuidGenerator`, `SequentialGuidGenerator`
- Nullable reference types

#### Exceções
- `NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `DomainException`
- ErrorCode padronizado

#### BusinessResult Melhorado
- `BusinessResult.Success<T>()`, `BusinessResult.Failure<T>()`, `BusinessResult.From<T>()`
- `Match<TResult>()`, `Bind<TNew>()`
- Implicit operators
- `BusinessResultFunctionalExtensions`: Map, Tap, Ensure
- `IStructuredMessageResult` com código de erro estruturado

### Descontinuado (Deprecated)

> **⚠️ APIs marcadas para remoção na próxima major version**

- **Telemetria Legada**:
  - `TelemetryHelper` → Use `ILogger<T>`
  - `TelemetryLevels` → Use `LogLevel`
  - `ITelemetryService` → Use `ILogger<T>`
  - `AddMvp24HoursTelemetry()` → Use `AddMvp24HoursObservability()`
  - **Guia de Migração**: `docs/*/observability/migration.md`

- **Resiliência HTTP Legada**:
  - `HttpClientExtensions` → Use `AddStandardResilienceHandler()`
  - `HttpPolicyHelper` → Use `Microsoft.Extensions.Http.Resilience`
  - `HttpClientResilienceExtensions` → Use APIs nativas
  - **Guia de Migração**: `docs/*/modernization/http-resilience.md`

- **Resiliência Genérica Legada**:
  - `MvpExecutionStrategy` → Use `ResiliencePipeline`
  - `MongoDbResiliencyPolicy` → Use `ResiliencePipeline`
  - `RetryPipelineMiddleware` → Use `NativePipelineResilienceMiddleware`
  - `CircuitBreakerPipelineMiddleware` → Use `NativePipelineResilienceMiddleware`
  - `RetryPolicy<T>`, `CircuitBreaker<T>` → Use `ResiliencePipeline`
  - **Guia de Migração**: `docs/*/modernization/generic-resilience.md`

- **Cache Legado**:
  - `MultiLevelCache` → Use `HybridCache`
  - **Guia de Migração**: `docs/*/modernization/hybrid-cache.md`

### Documentação

#### Nova Estrutura
- `docs/pt-br/cqrs/` - Documentação CQRS completa (20+ documentos)
- `docs/en-us/cqrs/` - CQRS documentation (20+ documents)
- `docs/pt-br/core/` - Documentação Core (10 documentos)
- `docs/en-us/core/` - Core documentation (10 documents)
- `docs/pt-br/observability/` - Observabilidade (6 documentos)
- `docs/en-us/observability/` - Observability (6 documents)
- `docs/pt-br/modernization/` - Modernização .NET 9 (15+ documentos)
- `docs/en-us/modernization/` - .NET 9 Modernization (15+ documents)

#### Documentos CQRS
- home.md, getting-started.md, mediator.md, commands.md, queries.md
- notifications.md, behaviors.md, validation-behavior.md
- domain-events.md, integration-events.md
- integration-unitofwork.md, integration-repository.md, integration-rabbitmq.md, integration-caching.md
- concepts-comparison.md, migration-mediatr.md, best-practices.md, api-reference.md
- event-sourcing/*, saga/*, resilience/*, observability/*
- multi-tenancy.md, scheduled-commands.md, specifications.md

#### Documentos Core
- home.md, guard-clauses.md, value-objects.md, strongly-typed-ids.md
- functional-patterns.md, smart-enums.md, infrastructure-abstractions.md, entity-interfaces.md

#### Documentos Observability
- home.md, logging.md, tracing.md, metrics.md, migration.md, exporters.md

#### Documentos Modernization
- dotnet9-features.md, migration-guide.md
- http-resilience.md, generic-resilience.md, rate-limiting.md
- time-provider.md, periodic-timer.md, options-configuration.md, channels.md
- hybrid-cache.md, output-caching.md, keyed-services.md
- problem-details.md, minimal-apis.md, source-generators.md
- native-openapi.md, aspire.md

### Testes

- 1099+ tarefas concluídas (86.6% do plano)
- 1000+ testes unitários
- Testes de integração com Testcontainers (SQL Server, MongoDB)
- Benchmarks de performance vs MediatR
- Helpers de teste para observabilidade

### Melhorado

- Performance geral com .NET 9 e source generators
- Tipagem com nullable reference types
- Documentação XML em todas as APIs públicas
- IntelliSense com exemplos práticos

### Corrigido

- Memory leak em CronJob `ResetServiceProvider`
- Diversos warnings de nullability

---

## [8.3.261] - 2024

### Adicionado
- **CronJob**: Implementação de suporte a tarefas agendadas
  - Configuração fluente de schedules
  - Suporte a expressões cron
  - Injeção de dependência para jobs
  - Hosted service integrado

### Detalhes
Esta versão introduz o módulo `Mvp24Hours.Infrastructure.CronJob` permitindo agendar tarefas recorrentes de forma simples e integrada ao ASP.NET Core.

**Exemplo de uso:**
```csharp
services.AddMvp24HoursCronJob(config =>
{
    config.AddJob<MyScheduledJob>("0 */5 * * * *"); // A cada 5 minutos
});
```

## [8.2.102] - 2024

### Adicionado
- **Minimal API**: Manipuladores de rotas para conversão e vinculação de parâmetros
  - Binders customizados para tipos complexos
  - Conversores para tipos primitivos
  - Suporte a validação automática
  - Integração com BusinessResult pattern

### Melhorado
- Suporte aprimorado para Minimal APIs do .NET 6+
- Binding automático de DTOs em rotas mínimas
- Validação integrada em endpoints Minimal API

### Detalhes
Facilita o uso do padrão Minimal API mantendo a robustez dos binders e validações da biblioteca.

## [8.2.101] - 2024

### Mudado
- **Migração para .NET 8**: Refatoração completa para .NET 8
  - Atualização de todas as dependências para .NET 8
  - Uso de recursos modernos do C# 12
  - Otimizações de performance do .NET 8
  - Primary constructors onde apropriado
  - Collection expressions

### Removido
- Suporte a versões anteriores ao .NET 8
- Pacotes obsoletos e substituídos

### Detalhes
Grande marco de migração para .NET 8, trazendo melhorias de performance e recursos modernos da linguagem.

---

# Histórico .NET Core / .NET 6

## [4.1.191] - 2023

### Mudado
- **Mapeamento Assíncrono**: Refatoração para mapeamento de resultados assíncronos
  - Novos métodos de extensão para `Task<T>`
  - Suporte a `ValueTask<T>`
  - Mapeamento automático de `IBusinessResult<T>` assíncrono

### Melhorado
- Performance em operações assíncronas
- Redução de alocações de memória

## [4.1.181] - 2023

### Removido
- **Anti-patterns**: Remoção de padrões problemáticos identificados
  - Eliminação de acoplamentos desnecessários
  - Remoção de dependências circulares
  - Simplificação de abstrações excessivas

### Mudado
- **Entidades de Log**: Separação de contextos de entidade de log
  - Uso apenas de contratos para melhor abstração
  - Interfaces `IEntityLog<T>` e `IEntityDateLog` separadas
  - Implementações base opcionaliz`EntityBase` separadas de `EntityBaseLog`

### Adicionado
- Documentação arquitetural detalhada
- Testes para contexto de banco de dados com log

### Corrigido
- Injeção de dependência no client do RabbitMQ
- Injeção de dependência no Pipeline
- Consumers isolados para client do RabbitMQ

### Documentação
- Atualização e detalhamento de recursos arquiteturais
- Exemplos de uso de entidades com auditoria

## [3.12.262] - 2022

### Mudado
- Refatoração completa de extensões
  - Organização por namespace
  - Remoção de duplicações
  - Padronização de nomenclatura

## [3.12.261] - 2022

### Adicionado
- Testes para middlewares customizados
- Cobertura de testes para WebAPI

## [3.12.221] - 2022

### Adicionado
- **Resiliência**: Implementação de Polly para tolerância a falhas
  - Retry policies configuráveis
  - Circuit breaker pattern
  - Timeout policies
  - Fallback strategies

- **Delegation Handlers**: Propagação de chaves no Header
  - Correlation ID automático
  - Propagação de Authorization header
  - Headers customizados configuráveis
  - Logging de requisições HTTP

### Corrigido
- Carregamento automático de classes de mapeamento com `IMapFrom`
- Bug de reflexão em assemblies dinâmicos

### Detalhes
Esta versão trouxe grande melhoria em resiliência e observabilidade de aplicações distribuídas.

## [3.12.151] - 2022

### Mudado
- **IMapFrom**: Remoção de tipagem genérica redundante
  - Simplificação da interface
  - Detecção automática de tipos
  - Configuração mais limpa

### Adicionado
- **Testcontainers**: Suporte para testes com containers Docker
  - RabbitMQ testcontainer
  - Redis testcontainer
  - MongoDB testcontainer
  - Configuração automática de testes de integração

### Detalhes
Testcontainers revolucionaram os testes de integração, permitindo testes reais contra serviços em containers Docker.

## [3.2.241] - 2021

### Mudado
- **Configuração Fluente**: Migração de configurações JSON para extensões fluentes
  - API fluente para DbContext
  - Configuração fluente para RabbitMQ
  - Configuração fluente para Redis
  - Configuração fluente para Pipeline

- **Padrão de Notificação**: Substituição do sistema de notificações
  - Novo `BusinessResult<T>` com mensagens integradas
  - Remoção de notification context separado
  - Mensagens tipadas (Info, Error, Warning, Success)

### Adicionado
- **HealthCheck**: Suporte completo a health checks
  - Health checks para SQL Server, PostgreSQL, MySQL
  - Health checks para MongoDB
  - Health checks para Redis
  - Health checks para RabbitMQ
  - WebStatus project com HealthCheckUI

- **Telemetria**: Sistema de telemetria customizável
  - Trace/Verbose em todas as bibliotecas
  - Níveis de log configuráveis
  - Filtros por operação
  - Integração com providers de logging

- **RabbitMQ Avançado**: Recursos avançados de mensageria
  - Dead Letter Queue configurável
  - Conexão persistente com Polly
  - Consumer assíncrono
  - Retry automático

### Melhorado
- **Transaction Isolation**: Configuração de nível de isolamento para EF
  - Transaction scope configurável
  - Read committed por padrão
  - Otimização para leituras

- **Pipeline**: Melhorias no sistema de pipeline
  - Adição de mensagens ao pacote durante execução
  - Suporte a operações de rollback
  - Context compartilhado entre operações

- **Validação**: Sistema de validação aprimorado
  - FluentValidation com mensagens estruturadas
  - DataAnnotations com mensagens estruturadas
  - Retorno consistente de erros

### Documentação
- Documentação completa de WebAPI
- Guias de configuração atualizados
- Exemplos de uso de todos os recursos

### Detalhes
Versão marco com grandes refatorações e novos recursos fundamentais. Introdução do padrão BusinessResult e telemetria.

## [3.1.x e anteriores] - 2020-2021

### Funcionalidades Base Implementadas
- ✅ **Banco de Dados Relacional**
  - SQL Server com Entity Framework Core
  - PostgreSQL com Npgsql
  - MySQL com Pomelo
  - Repository pattern
  - Unit of Work pattern
  - Soft delete automático
  - Auditoria automática

- ✅ **Banco de Dados NoSQL**
  - MongoDB com driver oficial
  - Redis com StackExchange.Redis
  - Repository pattern para NoSQL
  - Cache distribuído

- ✅ **Message Broker**
  - RabbitMQ com cliente oficial
  - Publisher/Subscriber pattern
  - Work Queue pattern
  - Request/Reply pattern

- ✅ **Pipeline**
  - Pipe and Filters pattern
  - Operações sequenciais
  - Rollback automático
  - Injeção de dependência

- ✅ **Documentação**
  - Swagger/OpenAPI integration
  - XML comments support
  - Customização de UI

- ✅ **Mapeamento**
  - AutoMapper integration
  - Profiles automáticos
  - IMapFrom interface

- ✅ **Logging**
  - Integração com ILogger
  - Structured logging
  - Múltiplos providers

- ✅ **Validação**
  - FluentValidation support
  - Data Annotations support
  - Validações customizadas

- ✅ **Specification Pattern**
  - Expressões LINQ reutilizáveis
  - Composição de specifications
  - AND, OR, NOT operators

---

## Tipos de Mudanças

- `Adicionado` para novos recursos
- `Mudado` para mudanças em recursos existentes
- `Descontinuado` para recursos que serão removidos
- `Removido` para recursos removidos
- `Corrigido` para correções de bugs
- `Segurança` para vulnerabilidades

## Convenções de Versionamento

Este projeto segue o [Versionamento Semântico](https://semver.org/lang/pt-BR/):
- **MAJOR**: Mudanças incompatíveis na API
- **MINOR**: Funcionalidades adicionadas de forma retrocompatível
- **PATCH**: Correções de bugs retrocompatíveis

Formato: `MAJOR.MINOR.PATCH` (ex: 8.3.261)

## Links

- [Documentação](https://mvp24hours.dev)
- [Repositório](https://github.com/kallebelins/mvp24hours-dotnet)
- [Exemplos](https://github.com/kallebelins/mvp24hours-dotnet-samples)
- [Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues)
- [Releases](https://github.com/kallebelins/mvp24hours-dotnet/releases)

## Contribuindo

Veja [CONTRIBUTING.md](CONTRIBUTING.md) para detalhes sobre como contribuir com o projeto.

## Agradecimentos

Desenvolvido com ❤️ por [Kallebe Lins](https://github.com/kallebelins).

**Quer contribuir?** Veja [CONTRIBUTING.md](CONTRIBUTING.md) para começar!
