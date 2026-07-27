# Options and DI Inventory

Status: **Working inventory for documentation v1**

This page freezes the Options, presets, and DI extension inventory for the English documentation overhaul. Use it to prioritize Phase 2–3 documentation work. When this inventory and source disagree, re-scan `src/**` and update this page.

Related:

- [Documentation Scope and Information Architecture](documentation-ia-policy.md)
- [Documentation Authoring Guide](documentation-authoring-guide.md)
- [AI Context Migration Map](ai-context-migration-map.md)

## Snapshot

| Metric | Count |
|--------|------:|
| Unique public `*Options` types | **250** |
| Declarations including cross-namespace collisions | **257** |
| Cross-namespace name collisions | **6 pairs** |
| Static presets / factory methods | **137** |
| `AddMvp*` / `AddMvp24Hours*` DI extensions | **213** |
| Primary DI methods outside the `AddMvp*` pattern | **~30** |
| `*Options*Test*.cs` files | **29** |
| Options types mentioned in `docs/en-us` | **~35** |
| Substantively documented | **~15** |
| Missing from `docs/en-us` | **~215** |

Classification used in this inventory:

| Status | Meaning |
|--------|---------|
| Documented | Canonical non–ai-context page names the type and covers properties, presets, or formal API reference |
| Partial | Mentioned without a complete Options table, only in ai-context/modernization, or ambiguous with BCL/ASP.NET names |
| Missing | No meaningful `docs/en-us` reference |

## Ambiguous APIs

Document these with full namespace and owning package. Do not rely on the short type name alone.

| Issue | Types / APIs | Notes |
|-------|--------------|-------|
| Name collision | `CircuitBreakerOptions` | Core Pipe contract vs Infrastructure Resilience |
| Name collision | `RetryOptions` | Core Pipe contract vs Infrastructure Resilience |
| Name collision | `NativeResilienceOptions` | Infrastructure HTTP vs Infrastructure Native Resilience |
| Name collision | `ObservabilityOptions` | Core vs Infrastructure |
| Name collision | `StructuredLoggingOptions` | EF Core observability vs Pipe middleware |
| Name collision | `CacheHealthCheckOptions` | Caching module vs WebAPI |
| BCL / ASP.NET collision | `HealthCheckOptions`, `AuthenticationOptions`, `CorsOptions`, `ProblemDetailsOptions` | Prefer Mvp24Hours-qualified names in docs |
| Not a user Options DTO | `ConfigureSwaggerGenOptions` | `IConfigureOptions<SwaggerGenOptions>` adapter |
| Interface | `IAdvancedCronJobOptions` | Interface, not a concrete Options class |
| Generics | `CronJobOptions<T>`, `AdvancedCronJobOptions<T>`, `SagaOptions<TData>` | Document generically |
| DI naming inconsistency | Email, SMS, File Storage, Secrets, Locking, Background Jobs, CronJob | Primary entry points use `AddEmailService`, `AddCronJob*`, and similar non-`AddMvp*` names |

## Documentation gap priorities

1. New module hubs: Email, SMS, File Storage, Secrets, Distributed Locking, Background Jobs — all Missing, with non-`AddMvp*` DI names.
2. WebAPI Options pack: ~45 types with solid configuration tests, thin docs.
3. RabbitMQ Options pack: ~44 types; broker pages show lambdas, not tables.
4. Resolve ambiguous duplicate names in every touched page.
5. Upgrade Partial → Documented for EF Core SchemaValidation/Migration/ReadWrite, CronJob advanced options, HybridCache/OutputCaching templates, and Mediator options already in good shape.

## Module coverage

### Core (`Mvp24Hours.Core`) — 22 types

Primary DI: `AddMvp24HoursObservability`, `AddMvp24HoursLogging`, `AddMvp24HoursTracing`, `AddMvp24HoursMetrics`, `AddMvp24HoursOpenTelemetry*`, `AddMvp24HoursAspireDefaults`, `AddMvpChannels`, Aspire component helpers.

| Type | Presets / notes | Docs | Tests |
|------|-----------------|------|-------|
| `AspireOptions` + nested telemetry/health/resilience | nested options | Partial — `modernization/aspire.md` | `AspireOptionsTest.cs` |
| `AspireRedisOptions`, `AspireRabbitMQOptions`, `AspireDatabaseOptions` | Aspire component options | Missing | — |
| `BulkheadOptions` | Default, Narrow, Wide, NoQueue | Missing | `PipeOptionsContractTest.cs` |
| `CircuitBreakerOptions` (Pipe) | Default, Sensitive, Tolerant | Missing | `PipeOptionsContractTest.cs` |
| `RetryOptions` (Pipe) | Default, NoRetry, Aggressive, Conservative | Partial | `PipeOptionsContractTest.cs` |
| `FallbackOptions` | Default | Missing | `PipeOptionsContractTest.cs` |
| `CacheEntryOptions` | FromDuration, WithSlidingExpiration, WithBothExpirations | Partial | `CacheEntryOptionsTest.cs` |
| `MvpChannelOptions` | Unbounded, Bounded, HighThroughput, Drop* | Missing | — |
| `ProducerConsumerOptions` | — | Partial | — |
| `BulkOperationOptions` | — | Partial | — |
| `EncryptionOptions` | `Key` required in 10.0.0 | Missing | — |
| `LoggingOptions`, `MetricsOptions`, `TracingOptions` | — | Partial / Missing | Observability tests |
| `ObservabilityOptions` + nested | — | Missing | Observability tests |
| `OpenTelemetryExporterOptions`, `OtlpExporterOptions`, `ConsoleExporterOptions`, `PrometheusExporterOptions` | Development/Production defaults | Partial — `observability/exporters.md` | — |
| `OpenTelemetryLoggingOptions` | — | Missing | — |
| `NativeRateLimiterOptions` | FixedWindow, SlidingWindow, TokenBucket, Concurrency | Documented — `modernization/rate-limiting.md` | — |

### Application (`Mvp24Hours.Application`) — 13 types, all Missing

Primary DI: `AddMvp24HoursApplication*`, `AddMvp24HoursValidation`, `AddMvp24HoursPagination`, `AddMvpApplicationQueryCache*`, `AddMvpResilience`, specification helpers.

Types: `ApplicationModuleOptions`, `ApplicationObservabilityOptions`, `ApplicationEventDispatcherOptions`, `ApplicationEventOutboxProcessorOptions`, `ExceptionMappingOptions`, `OperationMetricsOptions`, `PaginationOptions`, `QueryCacheOptions`, `QueryCacheEntryOptions`, `TransactionScopeOptions`, `ValidationOptions`, `ValidationServiceOptions`.

Presets: `ValidationOptions.Default`, `WithCascadeValidation`, `FastValidation`.

Tests: Application service/module/validation/pagination/resilience/transaction extension tests. Docs mention some DI modules in `application-services.md` without Options tables.

### EF Core (`Mvp24Hours.Infrastructure.Data.EFCore`) — 18 types

Primary DI: `AddMvp24HoursDbContext*`, repository helpers, schema validation, migrations, read/write splitting, observability, health checks, CQRS integration, encryption, tenant, testing fakes.

| Type | Presets | Docs | Tests |
|------|---------|------|-------|
| `EFCoreResilienceOptions` | Production, Development, AzureSql, NoResilience | Documented — `database/efcore-advanced.md` | `EFCoreResilienceOptionsTest.cs` |
| `EFCoreRepositoryOptions` | — | Missing | `EFCoreRepositoryOptionsTest.cs` |
| `EFCoreObservabilityOptions`, `SlowQueryInterceptorOptions`, `StructuredLoggingOptions` (EF) | — | Missing | observability extension tests |
| `EFCoreCqrsOptions` | — | Missing | CQRS integration tests |
| `SchemaValidationOptions` | Development, Staging, Production, ContinuousIntegration | Missing | `SchemaValidationOptionsTest.cs` |
| `MigrationOptions` | Development, Staging, Production, LogOnly | Missing | `MigrationOptionsTest.cs` |
| `ReadWriteOptions` | SimpleSetup, AzureSqlGeoReplica, PostgreSqlStreaming | Missing | `ReadWriteOptionsTest.cs` |
| `NativeDbResilienceOptions` | SqlServer, PostgreSql, MySql | Partial — modernization | native resilience tests |
| `DbContextHealthCheckOptions` | SqlServer, PostgreSql, MySql, Strict, Liveness | Missing | `DbContextHealthCheckOptionsTest.cs` |
| `SqlServerHealthCheckOptions`, `PostgreSqlHealthCheckOptions`, `MySqlHealthCheckOptions` | — | Missing | health check extension tests |
| `TenantInterceptorOptions` | — | Missing | — |
| `InMemoryDbContextOptions`, `TestDbContextFactoryOptions` | — | Missing | testing extension tests |
| `BulkOperationOptions` | shared Core contract | Partial | — |

### MongoDB (`Mvp24Hours.Infrastructure.Data.MongoDb`) — 27 types

Primary DI: `AddMvp24HoursDbContext` / repository helpers, `AddMvpMongoDb*`, CQRS integration, testing infrastructure.

Documented coverage is thin: `MongoDbOptions` and `MongoDbBulkOperationOptions` are Partial; `MongoDbResiliencyOptions` and most advanced Options are Missing. Important presets include:

- `MongoDbResiliencyOptions`: CreateProduction, CreateDevelopment
- `NativeMongoDbResilienceOptions`: ReplicaSet, ShardedCluster, Standalone
- `MongoDbBulkOperationOptions`: Default, HighThroughput, HighIntegrity
- `MongoDbConcernOptions`: MaxDurability, MaxConsistency, MaxPerformance, Balanced, FireAndForget, Analytics
- `MongoDbCollationOptions`: English/Portuguese/Spanish case-insensitive, NumericOrdered, CaseInsensitiveNumeric, SimpleBinary
- Testing: `MongoDbInMemoryOptions` and `MongoDbTestcontainersOptions` factories

Tests: `MongoDbResiliencyOptionsTests.cs`, `AdvancedOptionsTest.cs`, native resilience extension tests.

### CQRS (`Mvp24Hours.Infrastructure.Cqrs`) — 11 types

Primary DI: `AddMvpMediator`, `AddMvpInbox`, `AddMvpOutbox`, `AddMvpInboxOutbox`, `AddMvpScheduledCommands`, `AddMvpCommandSchedulerOnly`.

| Type | Presets | Docs | Tests |
|------|---------|------|-------|
| `MediatorOptions` | fluent `With*Behaviors()` | Documented — `cqrs/api-reference.md` | many Cqrs tests |
| `MediatorCacheOptions` | — | Documented | `MediatorCachingExtensionsTest.cs` |
| `InboxOutboxOptions` | — | Documented — `cqrs/resilience/inbox-outbox.md` | inbox/outbox extension tests |
| `NativeCqrsResilienceOptions` | Default, ForCommands, ForQueries | Partial | behavior tests |
| `ScheduleOptions` | Now, After, At | Documented — `cqrs/scheduled-commands.md` | scheduled command tests |
| `ScheduledCommandOptions` | — | Partial | — |
| `SagaOrchestrationOptions`, `SagaExecutionOptions`, `SagaHostedServiceOptions` | — | Missing | saga tests |
| `ProjectionOptions`, `EventSourcingOptions` | — | Missing | — |

### RabbitMQ (`Mvp24Hours.Infrastructure.RabbitMQ`) — 44 types

Primary DI: `AddMvpRabbitMQ`, `AddMvp24HoursRabbitMQ*`, topology/filters/scheduler/multi-tenancy/observability/transactional helpers.

Most Options are Missing. Broker docs show DI lambdas without property tables. High-priority types:

- Connection/client: `RabbitMQOptions`, `RabbitMQConnectionOptions`, `RabbitMQClientOptions`, `RabbitMQHostedOptions`, `RabbitMQAdvancedOptions`
- Messaging: `OutboxOptions` (Default, HighThroughput, HighReliability, LowLatency), `BatchConsumerOptions`, `BatchPublishOptions`, `ConsumerPrefetchOptions`, `PublisherConfirmOptions`, `PriorityQueueOptions`, `MessageTtlOptions`, `MessageDeduplicationOptions`, `MessageSchedulerOptions`, `RequestClientOptions`
- Pipeline/topology/tenancy/testing: `FilterPipelineOptions`, filter Options, topology Options, `TenantRabbitMQOptions`, `TestHarnessOptions`

Tests: Application RabbitMQ integration coverage; few dedicated Options unit tests.

### Pipe (`Mvp24Hours.Infrastructure.Pipe`) — 24 types

Primary DI: `AddMvp24HoursPipeline`, `AddMvp24HoursPipelineAsync`, `AddMvpPipelineResiliency`, retry/circuit-breaker/fallback/bulkhead/dead-letter helpers.

`PipelineOptions` and `PipelineAsyncOptions` are Partial in `pipeline.md`. Missing includes resiliency Options, observability/health/visualization Options, fork/join, checkpoint, dependency graph, saga, FluentValidation, OpenTelemetry, and cache-operation Options. Presets include `NativePipelineResilienceOptions` Default/LongRunning/QuickOperations and `DeadLetterOptions`/`RateLimitingPipelineOptions` Default.

Tests: `PipelineOptionsTest.cs`, message extension tests.

### Caching (`Mvp24Hours.Infrastructure.Caching` + Redis) — 12 types

Primary DI: `AddMvp24HoursCaching`, `AddMvpCaching`, `AddMvpHybridCache`, `AddMvpHybridCacheWithRedis`, `AddMvp24HoursCachingRedis`.

| Type | Docs |
|------|------|
| `MvpHybridCacheOptions` | Documented — `modernization/hybrid-cache.md` |
| `CacheEntryOptions`, `RedisHybridCacheTagManagerOptions` | Partial |
| `CacheOptions`, `MvpCachingOptions`, `MultiLevelCacheOptions`, `CacheResilienceOptions`, `EfCoreCacheOptions`, `WriteBehindOptions`, `CacheHealthCheckOptions`, `ObservableCacheProviderOptions`, `CacheableRepositoryOptions` | Missing |

### CronJob (`Mvp24Hours.Infrastructure.CronJob`) — 5 types

Primary DI uses **`AddCronJob*`**, `AddResilientCronJob*`, `AddAdvancedCronJob*`, global options, observability, metrics, and health checks — not `AddMvp24Hours*`.

| Type | Docs | Tests |
|------|------|-------|
| `CronJobOptions<T>` | Documented | — |
| `CronJobGlobalOptions` | Documented | `CronJobGlobalOptionsTest.cs` |
| `CronJobHealthCheckOptions` | Partial | — |
| `CronJobAdvancedOptions`, `AdvancedCronJobOptions<T>` / `IAdvancedCronJobOptions` | Missing | CronJob extension tests |

### Infrastructure modules (`Mvp24Hours.Infrastructure`) — 38 types, all Missing

These are the largest documentation gaps and often use non-`AddMvp*` entry points.

| Module | Primary DI | Options | Presets | Tests |
|--------|------------|---------|---------|-------|
| Email | `AddEmailService`, `AddSmtpEmailService`, `AddSendGridEmailService`, `AddAzureCommunicationEmailService`, `AddInMemoryEmailService`, queue/template/rate-limit helpers | `EmailOptions`, `SmtpEmailOptions`, `SendGridEmailOptions`, `AzureCommunicationEmailOptions`, `TemplateOptions`, `BulkSendOptions`, `RateLimitOptions`, `EmailQueueProcessorOptions`, `EmailServiceHealthCheckOptions` | `EmailOptions.Default` | `EmailOptionsTest.cs`, `EmailServiceExtensionsTest.cs`, health-check Options tests |
| SMS | `AddSmsService`, Twilio/Azure/InMemory providers, rate limiter, templates | `SmsOptions`, `TwilioSmsOptions`, `AzureCommunicationSmsOptions`, `SmsRateLimitOptions`, `SmsServiceHealthCheckOptions` | `SmsOptions.Default` | `SmsOptionsTest.cs`, `SmsRateLimitOptionsTest.cs`, extension tests |
| File Storage | `AddFileStorage`, Local/InMemory/Azure Blob/AWS S3 | `FileStorageOptions`, `FileStorageHealthCheckOptions` | Default, ForImages, ForDocuments, ForSecureUploads | `FileStorageOptionsTest.cs`, extension tests |
| Secrets | Environment/Azure Key Vault/AWS Secrets Manager providers, rotation helper | `SecretProviderOptions`, `EnvironmentVariableOptions`, `AwsSecretsManagerOptions`, `AzureKeyVaultOptions` | — | `SecretProviderOptionsTest.cs`, security extension tests |
| Distributed Locking | `AddDistributedLocking` + InMemory/Redis/RedLock/SQL Server/PostgreSQL providers | `DistributedLockOptions`, `DistributedLockHealthCheckOptions` | Default, ShortOperation, LongOperation, CriticalOperation, HighContention | `DistributedLockOptionsTest.cs`, extension tests |
| Background Jobs | `AddBackgroundJobs`, InMemory/Hangfire/Quartz | `JobOptions`, `HangfireJobOptions`, `QuartzJobOptions`, `ContinuationOptions`, `BatchOptions`, `ParentChildJobOptions`, `BackgroundJobHealthCheckOptions` | `JobOptions.Default` | `JobOptionsTest.cs`, `QuartzJobOptionsTest.cs`, extension tests |
| HTTP / Resilience / Health / Testing | `AddMvpInfrastructure`, `AddMvpHttpClient*`, `AddMvpResilience`, `AddMvpTestingInfrastructure`, health helpers | `InfrastructureOptions`, `HttpClientOptions` + nested certificate/retry/circuit-breaker/timeout/logging/proxy Options, `NativeResilienceOptions` (two namespaces), handler Options, health-check Options, `TestInfrastructureOptions` | HTTP/Native resilience presets | Native resilience Options tests, health-check extension tests, HTTP/resilience tests |

Also note: `AddMvp24HoursMapService`, `AddMvp24HoursTimeZone`.

### WebAPI (`Mvp24Hours.WebAPI`) — 45 types

Primary DI: 60+ `AddMvp24Hours*` methods for essentials, security, caching, rate limiting, OpenAPI, idempotency, and health checks.

Documented or Partial today: `OutputCachingOptions`, `OutputCachePolicyOptions`, `NativeRateLimiterOptions`, and partial coverage for `NativeOpenApiOptions`, `MvpProblemDetailsOptions` / `ProblemDetailsOptions`, `IdempotencyOptions`, `RateLimitingOptions`, plus unnamed lambda usage for Exception/CorrelationId/SecurityHeaders/ETag/ApiVersioning/HealthCheck.

Missing majority includes ApiKey auth and nested rate limit, request context/logging/telemetry, IP filtering, input sanitization, anti-forgery, size limits, compression/decompression, content negotiation, JSON/XML serialization, CORS, Swagger, request timeout, and WebAPI `CacheHealthCheckOptions`.

Tests: `ConfigurationOptionsTest.cs`, `MoreConfigurationOptionsTest.cs`, WebAPI extension smoke tests.

## Preset catalog highlights

Document presets wherever the owning Options type is documented.

| Area | Representative presets |
|------|------------------------|
| EF Core | `EFCoreResilienceOptions.Production/Development/AzureSql/NoResilience`; Migration/SchemaValidation environment presets; ReadWrite SimpleSetup/AzureSqlGeoReplica/PostgreSqlStreaming; DbContext health presets |
| MongoDB | resiliency CreateProduction/CreateDevelopment; concern and collation presets; bulk Default/HighThroughput/HighIntegrity |
| Resilience | HTTP and Native `NativeResilienceOptions` HighAvailability/LowLatency/BatchProcessing (+ Database/Messaging/Default on Native); CQRS Default/ForCommands/ForQueries; Pipe Default/LongRunning/QuickOperations |
| Infrastructure modules | DistributedLock Short/Long/Critical/HighContention; FileStorage ForImages/ForDocuments/ForSecureUploads; Outbox/BatchConsumer throughput presets |
| Channels / rate limiting | `MvpChannelOptions` Unbounded/Bounded/HighThroughput/Drop*; `NativeRateLimiterOptions` FixedWindow/SlidingWindow/TokenBucket/Concurrency |

## DI search rule for agents

When documenting a module, search both:

1. `AddMvp*` / `AddMvp24Hours*`
2. Module-specific entry points such as `AddEmailService`, `AddSmsService`, `AddFileStorage`, `AddDistributedLocking`, `AddBackgroundJobs`, `AddCronJob*`, `AddEnvironmentVariableSecretProvider`

An `AddMvp*`-only inventory misses several well-tested Infrastructure modules.

## Test coverage notes

- Dedicated Options unit tests cover ~70 types across 29 files.
- Roughly 180 Options types have no dedicated Options test file and may only appear in extension or integration tests.
- Prefer `*OptionsTest.cs`, `*Extensions*Test*.cs`, and module integration tests as evidence before writing defaults or DI examples.

## Acceptance criteria

- Public Options inventory is grouped by product module with documentation status.
- Presets and primary DI entry points, including non-`AddMvp*` names, are captured.
- Ambiguous colliding type names are flagged for namespaced documentation.
- Priority gaps for Phase 2–3 are explicit.
- Later documentation tasks can consume this inventory without reopening the scan methodology.
