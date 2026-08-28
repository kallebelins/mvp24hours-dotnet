# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [10.8.0] - 2026-08 🚀 Major Release

> **Migration to .NET 10** — This release aligns the entire solution with .NET 10 / C# 14, enables
> Nullable Reference Types across all projects, and removes/replaces obsolete APIs. The quality gate
> (`TreatWarningsAsErrors`) is strict (0 warnings in Release; only intentional residual
> `NU1510`). NuGet package consumers should review **nullability signature**
> changes and members that now require `required` before upgrading.

### Changed

- **TargetFramework**: all projects (production and tests) migrated to `net10.0`. There is no more
  multi-targeting or remaining projects on `net9.0`.
- **LangVersion**: standardized to `latest` (C# 14) for the entire solution.
- **Nullable Reference Types enabled in all projects**: several public APIs had nullability
  annotations adjusted to reflect actual behavior (parameters/returns became
  `T?` where the value can be null). Consumer code that already compiled with `<Nullable>enable</Nullable>`
  may receive new nullability warnings — reviewing affected call sites is recommended.
- **Members that now require `required`** (breaking in object initialization scenarios):
  - `SetPropertyCall.Property`
  - `EncryptionOptions.Key`
- **Nullability signatures adjusted** in overrides and interface implementations:
  `EntityIdNewtonsoftConverters.ReadJson` (`TId? existingValue`), `Enumeration.Equals`/`CompareTo`/`object.Equals`,
  `IValueProvider.SetValue(..., object?)`, `AnonymousTypeContractResolver` and value object converters.
- **Centralized package management (CPM)**: package versions now live in
  `src/Directory.Packages.props`; common build properties (`TargetFramework`, `LangVersion`,
  `Nullable`, `ImplicitUsings`) were centralized in `src/Directory.Build.props`.
- **`Pipeline`/`PipelineAsync` — event handlers no longer fire-and-forget**: `RunEvents`/`RunEventsAsync`
  previously dispatched each event handler via `Task.Factory.StartNew` without awaiting it and without a
  `catch` block, so handlers could run after the pipeline had already continued (or completed) and any
  exception they threw was silently swallowed. Event handlers now execute synchronously with the pipeline
  flow, in registration order, before the pipeline proceeds. Exceptions raised by a handler are logged
  (`Pipeline`/`PipelineAsync: Event handler {HandlerName} failure`) and, when `AllowPropagateException` is
  `true`, rethrown — matching the behavior already used for pipeline operations and rollback. Consumers
  relying on the previous "detached" timing (e.g. tests using `Task.Delay`/`Thread.Sleep` to wait for an
  event handler) should no longer need that workaround.
- **Application service bases — single logging convention**: every base under
  `Mvp24Hours.Application.Logic` (and `Logic/Async`) now follows the same pattern: an optional
  `ILogger? logger = null` as the last constructor parameter, a non-nullable `protected virtual ILogger Logger`
  property, and a `NullLogger` fallback when no logger is supplied. This is **additive** — the new parameter
  is optional and appended, so existing derived classes keep compiling without changes.
  - `ApplicationServiceBaseWithDto`, `ApplicationServiceBaseWithSeparateDtos`,
    `BulkCommandServiceBaseAsync`, `BulkCommandServiceWithDtoBaseAsync`,
    `BulkCommandServiceWithSeparateDtosBaseAsync` (and the `Async` counterparts of the first two) had
    logging **permanently disabled** by a hardcoded `private readonly ILogger _logger = NullLogger.Instance;`.
    They now accept a logger and honor it.
  - `ApplicationServiceBase`, `QueryServiceBase`, `CommandServiceBase` (and the `Async` counterparts)
    changed `protected virtual ILogger? Logger` to `protected virtual ILogger Logger`; the value is never
    null, so overrides/consumers no longer need `?.`. No constructor parameter was removed or reordered.
  - `RepositoryPagingService`/`RepositoryPagingServiceAsync` now forward the injected logger to the base
    `RepositoryService`/`RepositoryServiceAsync`, which previously always fell back to `NullLogger`.
  - `RepositoryService`/`RepositoryServiceAsync` (and the paging subclasses) log messages migrated from
    fixed strings (`"application-repositoryservice-listany"`) to the structured template already used by
    the other bases (`"[{ServiceName}] Executing ListAny for {EntityType}"`). Consumers matching on the
    old literal message text must update their filters. The `application-*` message style remains in the
    DTO/Bulk bases and in the cache/event/validation support types — unchanged in this release.

### Security

- **NU1903 — `System.Security.Cryptography.Xml`**: pinned to `10.0.10` to mitigate advisories
  [GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx) and
  [GHSA-w3x6-4m5h-cxqf](https://github.com/advisories/GHSA-w3x6-4m5h-cxqf) (brought in transitively
  via `System.ServiceModel.*`). `dotnet list package --vulnerable --include-transitive` → 0 vulnerable
  projects.
- `security-scan` workflow fixed to point to the solution (`.sln`) and fail on advisories.

### Added

- **`Mvp24Hours.Infrastructure.Identity.Keycloak`**: new first-party package for Keycloak integration
  in ASP.NET Core, including JWT/OIDC authentication, role transformation, UMA authorization
  (decision and RPT), Admin REST API, local user synchronization, and health checks.
- The package does not depend on Duende IdentityServer, IdentityModel, or Keycloak.AuthServices; OIDC
  discovery and OAuth operations are implemented directly on the native ASP.NET Core stack.
- Complete NuGet metadata with README, icon, MIT license, discovery tags, and XML documentation.
- Dedicated pipeline for build, unit tests, integration tests with Keycloak in Docker, and
  NuGet package generation/validation.
- **`samples/` — .NET 10 migration and full catalog**: 32 executable solutions on `net10.0`
  (19 migrated + 6 architecture blueprints + 7 capability samples), local references to `src/`
  via `Mvp24HoursUseProjectReferences`, unified minimal hosting, native OpenAPI, ProblemDetails,
  `docker-compose.yml` for dependencies, test baseline (`samples/TESTING.md`), umbrella solution
  [`samples/Mvp24Hours.Samples.sln`](samples/Mvp24Hours.Samples.sln), dedicated CI
  [`.github/workflows/samples-ci.yml`](.github/workflows/samples-ci.yml), and catalog documented in
  [`samples/README.md`](samples/README.md) with decision matrix guidance.

### Replaced (modernized obsolete APIs)

- **`CircuitBreaker<T>` (internal, `[Obsolete]`)** → `NativeResiliencePipeline` (Polly v8 via
  `Microsoft.Extensions.Resilience`) in `ResilientCacheProvider`.
- **`SqlServerDistributedLockProvider`**: `System.Data.SqlClient` → `Microsoft.Data.SqlClient`.
- **`CertificateHelper`**: obsolete `X509Certificate2` constructors → `X509CertificateLoader`
  (`LoadCertificateFromFile`/`LoadPkcs12FromFile`/`LoadCertificate`/`LoadPkcs12`) — SYSLIB0057.
- **`SmtpEmailProvider`**: removed `ServicePointManager.ServerCertificateValidationCallback`
  (SYSLIB0014). **Behavior change**: the `ServerCertificateValidationCallback` configured in
  `SmtpEmailOptions` is no longer applied per connection (the `SmtpClient` does not expose that hook and the global
  callback was obsolete and also affected HTTP) — default OS certificate validation now applies,
  with a warning log when the callback is configured.
- **`FieldEncryption`/`EncryptionKeyHelper`**: `new Rfc2898DeriveBytes(...)` → `Rfc2898DeriveBytes.Pbkdf2(...)`
  (SYSLIB0060), with byte-for-byte equivalence validated.
- **`AwsSecretsManagerProvider`**: `FallbackCredentialsFactory` → `DefaultAWSCredentialsIdentityResolver`
  (AWSSDK.Core v4).

### Deprecated

- **Custom error-handling middlewares in `Mvp24Hours.WebAPI`**: `ExceptionMiddleware` and
  `ProblemDetailsMiddleware` — and the extension methods that configure and register them
  (`AddMvp24HoursWebExceptions`, `UseMvp24HoursExceptionHandling`, `UseMvp24HoursProblemDetails`) —
  are now `[Obsolete]`: *"Use `AddNativeProblemDetails()`/`UseNativeProblemDetailsHandling()`
  instead. Will be removed in v12."* The native path builds on the ASP.NET Core
  `IProblemDetailsService`, so it gets content negotiation, `UseExceptionHandler` and
  `UseStatusCodePages` integration for free. `ExceptionMiddleware` never produced RFC 7807 at
  all — it writes an `IBusinessResult` payload — so migrating away from it **changes the response
  body**, which is precisely the reason for the deprecation.
  **`AddMvp24HoursProblemDetails` (both overloads) and `AddMvp24HoursProblemDetailsAll` are NOT
  deprecated**: besides the exception mappers, they register the MVC filters
  `ModelStateValidationFilter` and `ProblemDetailsResultFilter`, which `AddNativeProblemDetails`
  does not register. Keep calling them next to the native path when you rely on those filters, and
  swap only the pipeline call (`app.UseMvp24HoursProblemDetails()` →
  `app.UseNativeProblemDetailsHandling()`). Nothing was removed and no behavior changed.
  See [WebAPI → Error handling](docs/en-us/webapi.md) and
  [Problem Details](docs/en-us/modernization/problem-details.md).
- **`ContantsHelper` → `ConstantsHelper`** (typo fix, `Mvp24Hours.Core.Helpers`). The correctly
  spelled `ConstantsHelper` is now the canonical type and is used across production code, tests,
  samples, and docs. `ContantsHelper` (with `ContantsHelper.Data`) remains as an `[Obsolete]` shim
  whose `MaxQtyByQueryPage` forwards to `ConstantsHelper.Data.MaxQtyByQueryPage`, so existing
  consumers keep compiling with a warning. The shim will be removed in v12 — replace
  `ContantsHelper.Data.MaxQtyByQueryPage` with `ConstantsHelper.Data.MaxQtyByQueryPage`.
  The constant value (300) and its visibility are unchanged, and it remains a default (not a cap),
  overridable per provider via `EFCoreRepositoryOptions.MaxQtyByQueryPage` /
  `MongoDbRepositoryOptions.MaxQtyByQueryPage`.
- **`IPipelineMessage.DynamicContents`** (`Mvp24Hours.Core.Contract.Infrastructure.Pipe`) and its
  implementation `PipelineMessage.DynamicContents` are now `[Obsolete]`: *"Use
  `GetContent<T>()`/`AddContent<T>()` for type-safe access. Will be removed in v12."* The property
  resolves members at runtime through `DynamicObject`, so a missing key throws
  `ArgumentOutOfRangeException` and a null assignment throws `ArgumentNullException` — both only
  when the line executes. The typed members (`AddContent<T>`, `GetContent<T>`, `HasContent<T>`,
  `GetContentAll`) provide the same capability with compile-time checking. Neither the property nor
  the `DynamicContents` class was removed, and behavior is unchanged. Types that implement
  `IPipelineMessage` directly must keep implementing the member until v12; annotate it with the same
  `[Obsolete]` attribute or suppress `CS0618` locally. See
  [Pipeline → Message contents](docs/en-us/pipeline.md).

### Fixed

- **MongoDB `Repository`/`RepositoryAsync` `Remove` — broken soft delete**: the type check
  `entity.GetType() == typeof(IEntityLog<>)` compared a closed runtime type against an open
  generic definition, which is never true. As a result, `Remove` always hard-deleted the
  document, even for entities implementing `IEntityLog<TForeignKey>`/`IEntityDateLog`.
  **Behavior change**: `Remove`/`RemoveAsync` now perform a soft delete (sets `Removed`, and
  `RemovedBy` when available) for any entity implementing `IEntityDateLog`, matching the
  EF Core provider and the `SoftDeleteInterceptor`. Entities without `IEntityDateLog` continue
  to be hard-deleted. `Modify`/`ModifyAsync` also had the equivalent invalid-cast fixed
  (`(IEntityLog<object>)entity` failed for any `TForeignKey` other than `object`) and now
  preserve `Created`/`CreatedBy`/`ModifiedBy` via reflection instead of `dynamic`.
  `Repository.EntityLogBy`/`RepositoryAsync.EntityLogBy` no longer throw `NotSupportedException`;
  they return `null` (no `RemovedBy` is set when unavailable, instead of blowing up the delete).
- **`LockHandleBase.Dispose` / `DisposeAsync`**: lock release now occurs before marking
  `_disposed` (previously `ReleaseAsync` returned early and the resource remained held until expiration).
- **Renamed `ServiceCollectionExtentions` → `ServiceCollectionExtensions`** (typo fix) in
  `Mvp24Hours.Core` and `Mvp24Hours.WebAPI`. No shim was introduced (the API is only consumed via
  extension-method syntax, so `services.AddX(...)` call sites are unaffected); only code that
  referenced the type statically by its old name would need updating, and no such usage was found
  in `samples/`, `templates/`, or `src/Tests/`.
- **Build warnings zeroed in Release:** **~4235 → 0** (−100%). The first modernization pass
  reduced to ~969 and accepted the residual (~948) for a hygiene round; that debt
  was eliminated (nullable CS86xx in production and tests, LOGGEN002, CS0618/`MvpExecutionStrategy`,
  CS0108, xUnit1031, and other gate codes). Baseline: `tasks/warnings-baseline-v2.json`
  (`total = 0`).
- Various quality warnings eliminated: CS0168 (unused exception variable), CS0219
  (variable never used), CS1718 (comparison with itself), CS0108 (inherited member hiding),
  CA2022 (incomplete `Stream` read), xUnit1031 (synchronous blocking in async test).
- Removed redundant `PackageReference` entries (NU1510) already provided via `FrameworkReference` (the intentional
  pin of `System.Security.Cryptography.Xml` in Infrastructure keeps `NoWarn=NU1510` on the
  `PackageReference` for consumers without AspNetCore.App).
- `.editorconfig` style rules elevated from `suggestion` → `warning` (`EnforceCodeStyleInBuild`);
  full `dotnet format` applied across the solution (file-scoped namespaces, primary constructors,
  collection expressions, usings, etc.).

### Tests

- Full suite revalidated on .NET 10 (Release + Docker): **2294 passed · 0 failed · 4 skipped**
  (unit + integration via Testcontainers for MongoDB, SQL Server, Redis, and RabbitMQ).
  Evidence: `tasks/test-final-report-net10-warnings.md`.
- **Coverage expansion (Phases 2–13):** **4,492 passed · 0 failed · 6 skipped** across 18 test projects
  (+~2,198 tests vs baseline). Consolidated Coverlet coverage: **37.7%** line (**+9.4 pp** vs baseline 28.3%);
  **12/12** production assemblies instrumented (baseline: 3/12). **>95%** target documented as pending —
  evidence: `tasks/coverage-final-tests.json`, `tasks/coverage-delta-tests.md`, `tasks/coverage-final-report.html`.
- Coverage anti-regression gate in CI (`scripts/check-coverage-gate.ps1`): **75%** line floor
  (Phase 3 roadmap gate); product target remains **95%**. Current consolidated baseline:
  **75.7%** — see `docs/en-us/testing/coverage-baseline.md`.
- **Coverage Phase 3 (≥75%):** expanded unit and integration tests across Infrastructure testing
  assertions, RabbitMQ, MongoDB, Pipe, WebAPI, EFCore, Application, Keycloak, and Caching
  (`CacheableRepository`, `EFCoreSagaRepository`, `XmlContentFormatter`, `CacheResultsMiddleware`, etc.).
  Per-project sequential coverage collection in `run-ci-local.ps1` improves stability on large suites.
- **Coverage roadmap (Phases 0–5):** CI split into unit + integration jobs with merged
  Cobertura; `reportgenerator` excludes test assemblies (`+Mvp24Hours*;-Mvp24Hours*.Test*`);
  `scripts/merge-coverage-report.ps1` for local/CI merge. Hundreds of new tests across Core metrics,
  WebAPI extensions/middlewares, Application service bases, Infrastructure HTTP, Pipe, EFCore,
  MongoDB/RabbitMQ Docker integration, SQL bulk operations, and PostgreSQL fixture.
- Coverlet instrumentation fixed for .NET 10 SDK (`src/Tests/Directory.Build.props`, `coverlet.runsettings`).
- Tests categorized with `[Trait("Category", "Unit")]` / `[Trait("Category", "Integration")]`
  for selective execution in CI and locally without Docker.
- `InMemory` available in any configuration in EF tests (MySql/PostgreSql/SQLServer), aligning
  Release/CI with local Debug behavior.
- **`Mvp24Hours.Infrastructure.Test` — DistributedLocking**: +97 tests (InMemory, Redis/Moq,
  SqlServer/PostgreSql guards, factory, options, metrics, DI extensions).
- **`Mvp24Hours.Infrastructure.Data.EFCore.Test` (new)**: dedicated EF Core test project —
  **175 passed · 2 skipped** (CRUD repos, read-only, bulk, streaming, UoW+events,
  interceptors, specifications, resilience, testing fakes, migrations, converters, CQRS,
  read/write splitting, schema validation). Async soft-delete aligned with sync in
  `RepositoryAsync.RemoveAsync` (`IEntityDateLog`).

### Added (incremental updates)

- **WebAPI request body tracing**: introduced `RequestBodyTracingMiddleware` with
  configurable capture controls (`RequestBodyTracingOptions`) including method/content-type filters,
  size limits, path exclusions, and JSON field redaction before writing Activity tags.
- **WebAPI idempotency atomic acquisition**: `DistributedCacheIdempotencyStore` now supports optional
  atomic acquisition using `IDistributedLockFactory` with configurable provider, timeout, and duration
  (`EnableAtomicAcquisitionUsingDistributedLock`, `DistributedLockProviderName`,
  `DistributedLockAcquisitionTimeout`, `DistributedLockDuration`).
- **EF Core observability central wiring**: `AddMvp24HoursEFCoreObservabilityInterceptors` now has
  explicit usage guidance and coverage for composing built-in interceptors with custom interceptors in
  `AddDbContext`.
- **CronJob resilient distributed locking**: `ResilientCronJobService<T>` now supports optional
  cluster-safe execution lock acquisition through `IDistributedCronJobLock`, with skip metrics/logging
  on contention and guaranteed async disposal of lock handles.

### Changed (incremental updates)

- `CronJobResilienceConfig<T>` and `ICronJobResilienceConfig<T>` expanded with distributed-lock settings:
  `EnableDistributedLocking`, `DistributedLockDuration`, `DistributedLockWaitTimeout`, and
  `DistributedLockInstanceId`.
- `CronJobOptions<T>` now maps distributed-lock fields in both directions
  (`ToResilienceConfig` and `FromResilienceConfig`) so configuration binding reflects runtime behavior.
- CronJob and EF Core advanced documentation updated with end-to-end usage examples for resilient
  distributed locking and custom interceptor composition.

### Tests (incremental updates)

- Added focused CronJob tests for resilient distributed lock behavior:
  lock-unavailable skip path, release-after-success, metrics skip reason (`distributed_lock`), and
  configured instance-id propagation.
- Added EF Core observability tests to validate custom interceptor composition with
  `AddMvp24HoursEFCoreObservabilityInterceptors` and structured logging opt-in behavior.
- Validation runs completed successfully:
  - `Mvp24Hours.Infrastructure.CronJob.Test`: **272 passed · 0 failed** (with coverage run artifact).
  - `EFCoreObservabilityExtensionsTest` subset: **9 passed · 0 failed**.

### CI/CD

- `ci.yml` and `codeql-analysis.yml` workflows updated for **.NET 10** SDK (`10.0.x`).
- Strict `TreatWarningsAsErrors=true` gate in the `code-quality` job: `MvpResidualWarnings` contains
  only `NU1510` (security pin). `dotnet build … /p:TreatWarningsAsErrors=true` →
  **0 error(s) / 0 warning(s)**.
- Formatting step: `dotnet format src/Mvp24Hours.sln --exclude-diagnostics IDE0130 IDE1006
  --verify-no-changes` (full scope, no `--severity error`).
- Coverage gate: split `build-and-test` (unit) and `build-and-test-integration` (Docker) jobs;
  merged gate via `merge-coverage-report.ps1` + `check-coverage-gate.ps1`. **75%** line floor;
  **95%** product target. Baseline documented in `docs/en-us/testing/coverage-baseline.md`.

---

## [9.1.210] - 2026-01

### Fixed

- **NuGet Packages**: Package version fixes across all projects
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

### Removed

- **Obsolete Files**: Removal of DelegatingHandlers and TypedHttpClient files
  - `PropagationAuthorizationDelegatingHandler.cs`
  - `PropagationCorrelationIdDelegatingHandler.cs`
  - `PropagationHeaderDelegatingHandler.cs`
  - `TypedHttpClient.cs`

---

## [9.1.200] - 2026-01 🚀 Major Release

> **Migration to .NET 9** - This release introduces significant changes to adopt .NET 9 native APIs.
> See the migration guide at `docs/en-us/modernization/migration-guide.md`.

### Added

#### Complete CQRS Library (Mvp24Hours.Infrastructure.Cqrs)
- `IMediator`, `ISender`, `IPublisher` - core Mediator interfaces
- `IMediatorCommand<TResponse>`, `IMediatorCommand` - CQRS commands
- `IMediatorQuery<TResponse>` - CQRS queries
- `IMediatorNotification` - in-process notification system
- `IMediatorRequestHandler<TRequest, TResponse>` - generic handlers
- Pipeline Behaviors:
  - `LoggingBehavior` - start, end, and duration logging
  - `PerformanceBehavior` - slow request alerts
  - `UnhandledExceptionBehavior` - exception capture and logging
  - `ValidationBehavior` - FluentValidation integration
  - `CachingBehavior` - cache with IDistributedCache
  - `TransactionBehavior` - IUnitOfWork integration
  - `AuthorizationBehavior` - authorization via policies
  - `RetryBehavior` - retry with exponential backoff
  - `TimeoutBehavior` - configurable timeout per request
  - `CircuitBreakerBehavior` - circuit breaker for commands
  - `IdempotencyBehavior` - duplicate prevention
- Domain Events:
  - `IDomainEvent` and `DomainEventBase`
  - `IDomainEventHandler<TEvent>` and `DomainEventDispatcher`
  - `IHasDomainEvents` for entities/aggregates
  - `SaveChangesWithEventsAsync` for EFCore and MongoDB
- Integration Events:
  - `IIntegrationEvent` and `IntegrationEventBase`
  - `IIntegrationEventHandler<TEvent>`
  - `IIntegrationEventOutbox` and `InMemoryIntegrationEventOutbox`
  - `RabbitMqIntegrationEventPublisher`
- Event Sourcing:
  - `IEventStore` and `EventStream`
  - `AggregateRoot<TId>` with Apply/Raise
  - `Snapshot` and `SnapshotStore`
  - `EventStoreRepository<T>`
  - `IProjection`, `IProjectionHandler<TEvent>`, `ProjectionManager`
- Saga/Process Manager:
  - `ISaga<TData>`, `SagaBase<TData>`
  - `ISagaOrchestrator` and `ISagaStateStore`
  - `CompensatingCommand` for rollback
  - Saga timeout and expiration
- CQRS Observability:
  - `IRequestContext` with CorrelationId/CausationId
  - `RequestContextBehavior` for context propagation
  - `AuditBehavior` and `IAuditStore`
- Multi-tenancy:
  - `ITenantContext`, `TenantBehavior`
  - `ICurrentUser`, `CurrentUserBehavior`
  - Automatic tenant filters in queries
- Inbox/Outbox:
  - `InboxMessage`, `IInboxStore`, `InboxProcessor`
  - `OutboxProcessor` with retry and DLQ
- Scheduled Commands:
  - `IScheduledCommand`, `ICommandScheduler`
  - `ScheduledCommandHostedService`
- Decorators and Extensibility:
  - `IPreProcessor<TRequest>`, `IPostProcessor<TRequest, TResponse>`
  - `IExceptionHandler<TRequest, TException>`
- Streaming: `IStreamRequest<T>`, `IStreamRequestHandler<T>` with IAsyncEnumerable

#### .NET 9 Modernization
- **HybridCache** (Microsoft.Extensions.Caching.Hybrid):
  - `AddMvpHybridCache()` for configuration
  - `HybridCacheProvider` as `ICacheProvider`
  - Tags for group invalidation
  - `InMemoryHybridCacheTagManager` and `RedisHybridCacheTagManager`
- **TimeProvider**:
  - `TimeProviderAdapter` (TimeProvider → IClock bridge)
  - `ClockAdapter` (IClock → TimeProvider bridge)
  - `AddTimeProvider()`, `AddClock()`, `ReplaceTimeProvider()`
  - `FakeTimeProviderHelper` for tests
- **PeriodicTimer**:
  - `PeriodicTimerHelper` with common patterns
  - Migration of all background services
- **System.Threading.RateLimiting**:
  - `IRateLimiterProvider`, `NativeRateLimiterProvider`
  - `RateLimitingPipelineMiddleware` for Pipeline
  - `RateLimitingConsumeFilter`, `RateLimitingPublishFilter` for RabbitMQ
- **System.Threading.Channels**:
  - `IChannel<T>`, `MvpChannel<T>`
  - `ChannelFactory`, `ProducerConsumer<T>`
  - `ChannelPipeline<TInput, TOutput>`
  - `ChannelBatchProcessor<T>`
- **Microsoft.Extensions.Http.Resilience**:
  - `AddHttpClientWithStandardResilience()`
  - `NativeResilienceOptions` with presets
  - `NativeResilienceBuilder` for custom configuration
- **Microsoft.Extensions.Resilience**:
  - `INativeResiliencePipeline`, `NativeResiliencePipeline`
  - `NativeDbResilienceExtensions` for EFCore
  - `NativeMongoDbResilienceExtensions` for MongoDB
  - `NativePipelineResilienceMiddleware` for Pipe
  - `NativeResilienceBehavior` for CQRS
- **ProblemDetails (RFC 7807)**:
  - `AddNativeProblemDetails()`, `AddNativeProblemDetailsAll()`
  - `UseNativeProblemDetailsHandling()`
  - Helpers: `NotFoundProblem()`, `ValidationProblem()`, `ConflictProblem()`, etc.
- **TypedResults (.NET 9)**:
  - `ToNativeTypedResult()` for `IBusinessResult<T>`
  - `MapNativeCommand<T>()`, `MapNativeQuery<T>()` for CQRS
  - Filters: NativeValidation, ExceptionHandling, Logging, CorrelationId, Idempotency, Timeout
- **Source Generators**:
  - `Mvp24HoursJsonSerializerContext` for AOT serialization
  - `[LoggerMessage]` across all modules (CoreLoggerMessages, PipelineLoggerMessages, etc.)
- **Native OpenAPI**:
  - `AddMvp24HoursNativeOpenApi()`, `MapMvp24HoursNativeOpenApi()`
  - `SecuritySchemeTransformer`, `OpenApiDocumentTransformers`
- **Keyed Services**:
  - `ServiceKeys.cs` with constants
  - `KeyedServiceExtensions.cs`
- **Output Caching**:
  - `AddMvp24HoursOutputCache()`, `AddMvp24HoursOutputCacheWithRedis()`
  - `IOutputCacheInvalidator`
  - Policies: Short, Medium, Long, VeryLong, NoCache, Authenticated, Api
- **.NET Aspire 9**:
  - `AddMvp24HoursAspireDefaults()`
  - `AddMvp24HoursRedisFromAspire()`, `AddMvp24HoursRabbitMQFromAspire()`
  - `AddMvp24HoursSqlServerFromAspire()`, `AddMvp24HoursMongoDbFromAspire()`
- **IOptions<T> Validation**:
  - `IOptionsValidator<T>`, `OptionsValidatorBase<T>`
  - `AddOptionsWithValidation<T>()`, `AddOptionsWithValidation<T, TValidator>()`
  - `AddOptionsValidatorsFromAssembly()`

#### Observability (ILogger + OpenTelemetry)
- OpenTelemetry Tracing:
  - `ActivitySources` for all modules (Core, Pipeline, Repository, Mediator, RabbitMQ, CronJob, HttpClient)
  - `ActivityHelper` with semantic conventions
  - `IActivityEnricher` for customizable enrichment
  - `TracePropagation` with W3C Trace Context
- OpenTelemetry Metrics:
  - `MetricSources` with Meters per module
  - `PipelineMetrics`, `RepositoryMetrics`, `MessagingMetrics`, `CqrsMetrics`, `CacheMetrics`, `HttpMetrics`, `CronJobMetrics`
  - `MetricNames.cs` with semantic conventions
- OpenTelemetry Logs:
  - `ILogger` ↔ OpenTelemetry Logs integration
  - Automatic logs ↔ traces correlation (TraceId, SpanId)
  - Log sampling for high-load environments
- Context and Correlation:
  - `ICorrelationIdAccessor`, `CorrelationIdAccessor`
  - `CorrelationIdMiddleware`, `RequestContextMiddleware`
  - `BaggagePropagation` for TenantId, UserId
  - `ILogEnricher` (UserContextLogEnricher, TenantContextLogEnricher)
- Configuration:
  - `AddMvp24HoursLogging()`, `AddMvp24HoursTracing()`, `AddMvp24HoursMetrics()`
  - `AddMvp24HoursObservability()` - all-in-one
  - `AddMvp24HoursOpenTelemetry()` with OTLP, Console, Prometheus exporters
  - Centralized `ObservabilityOptions`
- Testability:
  - `FakeLogger<T>`, `InMemoryLoggerProvider`
  - `FakeActivityListener`, `FakeMeterListener`
  - `LogAssertions`, `ActivityAssertions`, `MetricAssertions`
  - `ObservabilityTestFixture`

#### Advanced EFCore
- Interceptors:
  - `AuditSaveChangesInterceptor`
  - `SoftDeleteInterceptor`
  - `ConcurrencyInterceptor`
  - `CommandLoggingInterceptor`
  - `SlowQueryInterceptor`
  - `TenantSaveChangesInterceptor`
  - `StructuredLoggingInterceptor`
- Multi-tenancy:
  - `ITenantProvider`, automatic query filters
  - `TenantModelBuilderExtensions`
  - `RowLevelSecurityHelper`
- Performance:
  - Configurable `AsNoTracking()`, `AsNoTrackingWithIdentityResolution()`
  - Compiled queries, Split queries
  - `IAsyncEnumerable<T>` streaming
  - Query tags (`TagWith()`)
  - `ProjectTo<TDto>` with AutoMapper
- Bulk Operations:
  - `BulkInsertAsync()`, `BulkUpdateAsync()`, `BulkDeleteAsync()`
  - Progress callback
  - `ExecuteUpdate`, `ExecuteDelete` (.NET 7+)
- Specification Pattern:
  - `GetBySpecificationAsync()`, `CountBySpecificationAsync()`, `AnyBySpecificationAsync()`
  - `IReadOnlyRepository<T>`, `IReadOnlyRepositoryAsync<T>`
  - Cursor-based pagination (keyset)
- Resilience:
  - `EnableRetryOnFailure()`, retry policies per exception
  - Timeout per query
  - DbContext pooling
- Health Checks:
  - `SqlServerHealthCheck`, `PostgreSqlHealthCheck`, `MySqlHealthCheck`
- Read/Write Splitting:
  - `ConnectionResolver`, `ReplicaSelector`
  - Separate DbContext for reads
- Testability:
  - `UseInMemoryDatabase` helpers
  - `IDataSeeder<T>`, `DbContextFactory`
  - `IRepositoryFake<T>`

#### Advanced MongoDB
- Interceptors: `AuditInterceptor`, `SoftDeleteInterceptor`, `AuditTrailInterceptor`
- Multi-tenancy: Query filters, `ITenantProvider`, Row-level security
- Field-level encryption (CSFLE)
- Bulk operations: `BulkInsertAsync()`, `BulkUpdateAsync()`, `BulkDeleteAsync()`
- Change Streams for real-time events
- GridFS for large files
- Time Series Collections
- Geospatial queries
- Text search indexes
- Resilience: Connection resiliency, Circuit breaker, Retry policies
- Health checks: Connectivity, Replica set status, Indexes
- Configurable read preference
- Testability: In-memory provider, `IRepositoryFake<T>`, Testcontainers helpers

#### Enterprise RabbitMQ
- Typed consumers:
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
  - `ScheduleMessage<T>()` by DateTime or TimeSpan
  - `CancelScheduledMessage()`
  - Recurring messages
- Pipeline/Middleware:
  - `IConsumeFilter<TMessage>`, `IPublishFilter<TMessage>`, `ISendFilter`
  - Filters: Logging, ExceptionHandling, Correlation, Telemetry, Validation
- Topology:
  - `IEndpointNameFormatter`, `IMessageTopology<TMessage>`
  - Topic Exchange, Fanout Exchange
  - Auto-binding, Exchange-to-exchange bindings
- Batch Consumers:
  - `IBatchConsumer<TMessage>`, `BatchConsumeContext<TMessage>`
  - Batch size, timeout, parallel processing
- Transactional Messaging:
  - `ITransactionalBus`
  - Integration with `IUnitOfWork`
  - `InMemoryOutbox`
- Sagas:
  - `ISagaConsumer<TData, TMessage>`
  - `SagaStateMachine<TInstance>`
  - Saga persistence (Redis, SQL, MongoDB)
- Multi-tenancy:
  - Virtual hosts per tenant
  - `ITenantConsumeFilter`
  - Connection pool per tenant
- Fluent API: `AddMvpRabbitMQ(cfg => { cfg.Host(); cfg.AddConsumer<T>(); })`
- Observability: ActivitySource, Prometheus Metrics
- Testing: `InMemoryBus`, `TestHarness`, `TestConsumeContext<T>`

#### Advanced Pipeline
- Typing:
  - `IPipeline<TInput, TOutput>`, `ITypedOperation<TInput, TOutput>`
  - Fluent API `.Pipe<TIn, TOut>().Then<TNext>().Finally()`
  - `IOperationResult<T>`, `OperationChain<T>`
- Context:
  - `IPipelineContext` (CorrelationId, CausationId, Metadata, User)
  - State Snapshots
  - Activity spans
- Advanced Flow:
  - Fork/Join pattern
  - Dependency Graph
  - OperationPriority
  - Saga Pattern with compensation
  - Checkpoint/Resume
- Observability:
  - Metrics per operation (duration, memory, success rate)
  - Structured logging
  - Pipeline Visualization (flow diagram)
  - Aggregated Health Check
  - Events: OnOperationStart, OnOperationEnd, OnPipelineComplete
- Integration: FluentValidation, IAsyncEnumerable, IDistributedCache, OpenTelemetry

#### WebAPI
- Exception Handling:
  - ProblemDetails (RFC 7807)
  - `ExceptionToProblemDetailsMapper`
  - Domain exception mapping to HTTP status codes
- Rate Limiting:
  - Policies by IP, User, API Key
  - Fixed/Sliding Window, Token Bucket
  - X-RateLimit-* headers
  - Redis for distributed
- Idempotency:
  - `IdempotencyKeyMiddleware`
  - Integration with `IIdempotentCommand`
  - Retry-after headers
- Security:
  - Security headers (HSTS, CSP, X-Frame-Options)
  - API Key authentication
  - IP filtering
  - Input sanitization
- Observability:
  - Request/Response logging with masking
  - OpenTelemetry tracing
  - Endpoint metrics
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
- Integrated AutoMapper
- Validation pipeline with `IValidationService<T>`
- Transaction scope with `[Transactional]`
- Specification Pattern: `GetBySpecificationAsync<TSpec>()`
- Exception handling: `ExceptionToResultMapper`, Result status codes
- Observability: Logging, Audit trail, OpenTelemetry, Correlation ID
- Cache: `[Cacheable]`, automatic invalidation
- Pagination: `PagedResult<T>`, cursor-based
- Automatic soft delete

#### Base Infrastructure
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
- Health Checks for all subsystems

#### Advanced Caching
- `ICacheProvider`: Memory, Distributed, HybridCache
- Patterns: Cache-Aside, Read-Through, Write-Through, Write-Behind, Refresh-Ahead
- Multi-level cache (L1 + L2)
- Invalidation: Tags, Pub/sub, Dependency tracking
- Resilience: Circuit breaker, Fallback, Graceful degradation
- Performance: Compression, Batch operations, Prefetching, Warming
- Observability: Metrics, Tracing, Health checks

#### Improved CronJob
- Fixes: Memory leak, IAsyncDisposable, PeriodicTimer
- Resilience: Retry policy, Circuit breaker, Overlapping control, Graceful shutdown
- Observability: Health checks, Metrics, OpenTelemetry spans, Structured logging
- Features:
  - `ICronJobContext` (JobId, StartTime, Attempt)
  - 6-field CRON (seconds)
  - Job dependencies
  - Distributed locking
  - `ICronJobStateStore`
  - Pause/resume at runtime
  - Hooks: OnJobStarting, OnJobCompleted, OnJobFailed
- Configuration: `CronJobOptions<T>`, `CronJobGlobalOptions`, appsettings.json, startup validation

#### Core Fundamentals
- Guard clauses: `Guard.Against.Null`, `NullOrEmpty`, `OutOfRange`, `NegativeOrZero`, `InvalidEmail`, `InvalidCpf`, `InvalidCnpj`, `Default`, `InvalidFormat`
- ValueObjects: Email, Cpf, Cnpj, Money, Address, DateRange, Percentage, PhoneNumber (with TryParse and implicit operators)
- Strongly-typed IDs: `EntityId<T>` with EF Core and JSON converters
- Functional patterns: `Maybe<T>`, `Either<TLeft, TRight>` with Map, Bind, Match
- Smart Enums: `Enumeration<T>` with FromValue, FromName, GetAll
- Entity interfaces: `IEntity<TId>`, `IAuditableEntity`, `ISoftDeletable`, `ITenantEntity`, `IVersionedEntity`
- `IClock`, `SystemClock`, `TestClock`
- `IGuidGenerator`, `SequentialGuidGenerator`
- Nullable reference types

#### Exceptions
- `NotFoundException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `DomainException`
- Standardized ErrorCode

#### Improved BusinessResult
- `BusinessResult.Success<T>()`, `BusinessResult.Failure<T>()`, `BusinessResult.From<T>()`
- `Match<TResult>()`, `Bind<TNew>()`
- Implicit operators
- `BusinessResultFunctionalExtensions`: Map, Tap, Ensure
- `IStructuredMessageResult` with structured error code

### Deprecated

> **⚠️ APIs marked for removal in the next major version**

- **Legacy Telemetry**:
  - `TelemetryHelper` → Use `ILogger<T>`
  - `TelemetryLevels` → Use `LogLevel`
  - `ITelemetryService` → Use `ILogger<T>`
  - `AddMvp24HoursTelemetry()` → Use `AddMvp24HoursObservability()`
  - **Migration Guide**: `docs/en-us/observability/migration.md`

- **Legacy HTTP Resilience**:
  - `HttpClientExtensions` → Use `AddStandardResilienceHandler()`
  - `HttpPolicyHelper` → Use `Microsoft.Extensions.Http.Resilience`
  - `HttpClientResilienceExtensions` → Use native APIs
  - **Migration Guide**: `docs/en-us/modernization/http-resilience.md`

- **Legacy Generic Resilience**:
  - `MvpExecutionStrategy` → Use `ResiliencePipeline`
  - `MongoDbResiliencyPolicy` → Use `ResiliencePipeline`
  - `RetryPipelineMiddleware` → Use `NativePipelineResilienceMiddleware`
  - `CircuitBreakerPipelineMiddleware` → Use `NativePipelineResilienceMiddleware`
  - `RetryPolicy<T>`, `CircuitBreaker<T>` → Use `ResiliencePipeline`
  - **Migration Guide**: `docs/en-us/modernization/generic-resilience.md`

- **Legacy Cache**:
  - `MultiLevelCache` → Use `HybridCache`
  - **Migration Guide**: `docs/en-us/modernization/hybrid-cache.md`

### Documentation

#### New Structure
- `docs/en-us/cqrs/` - Complete CQRS documentation (20+ documents)
- `docs/en-us/core/` - Core documentation (10 documents)
- `docs/en-us/observability/` - Observability (6 documents)
- `docs/en-us/modernization/` - .NET 9 Modernization (15+ documents)

#### CQRS Documents
- home.md, getting-started.md, mediator.md, commands.md, queries.md
- notifications.md, behaviors.md, validation-behavior.md
- domain-events.md, integration-events.md
- integration-unitofwork.md, integration-repository.md, integration-rabbitmq.md, integration-caching.md
- concepts-comparison.md, migration-mediatr.md, best-practices.md, api-reference.md
- event-sourcing/*, saga/*, resilience/*, observability/*
- multi-tenancy.md, scheduled-commands.md, specifications.md

#### Core Documents
- home.md, guard-clauses.md, value-objects.md, strongly-typed-ids.md
- functional-patterns.md, smart-enums.md, infrastructure-abstractions.md, entity-interfaces.md

#### Observability Documents
- home.md, logging.md, tracing.md, metrics.md, migration.md, exporters.md

#### Modernization Documents
- dotnet9-features.md, migration-guide.md
- http-resilience.md, generic-resilience.md, rate-limiting.md
- time-provider.md, periodic-timer.md, options-configuration.md, channels.md
- hybrid-cache.md, output-caching.md, keyed-services.md
- problem-details.md, minimal-apis.md, source-generators.md
- native-openapi.md, aspire.md

### Tests

- 1099+ tasks completed (86.6% of the plan)
- 1000+ unit tests
- Integration tests with Testcontainers (SQL Server, MongoDB)
- Performance benchmarks vs MediatR
- Observability test helpers

### Improved

- Overall performance with .NET 9 and source generators
- Typing with nullable reference types
- XML documentation on all public APIs
- IntelliSense with practical examples

### Fixed

- Memory leak in CronJob `ResetServiceProvider`
- Various nullability warnings

---

## [8.3.261] - 2024

### Added
- **CronJob**: Scheduled task support implementation
  - Fluent schedule configuration
  - Cron expression support
  - Dependency injection for jobs
  - Integrated hosted service

### Details
This release introduces the `Mvp24Hours.Infrastructure.CronJob` module, enabling simple recurring task scheduling integrated with ASP.NET Core.

**Usage example:**
```csharp
services.AddMvp24HoursCronJob(config =>
{
    config.AddJob<MyScheduledJob>("0 */5 * * * *"); // Every 5 minutes
});
```

## [8.2.102] - 2024

### Added
- **Minimal API**: Route handlers for parameter conversion and binding
  - Custom binders for complex types
  - Converters for primitive types
  - Automatic validation support
  - Integration with BusinessResult pattern

### Improved
- Enhanced support for .NET 6+ Minimal APIs
- Automatic DTO binding in minimal routes
- Integrated validation in Minimal API endpoints

### Details
Makes it easier to use the Minimal API pattern while keeping the library's binders and validations robust.

## [8.2.101] - 2024

### Changed
- **Migration to .NET 8**: Complete refactoring for .NET 8
  - All dependencies updated for .NET 8
  - Use of modern C# 12 features
  - .NET 8 performance optimizations
  - Primary constructors where appropriate
  - Collection expressions

### Removed
- Support for versions prior to .NET 8
- Obsolete packages and replacements

### Details
Major milestone migration to .NET 8, bringing performance improvements and modern language features.

---

# .NET Core / .NET 6 History

## [4.1.191] - 2023

### Changed
- **Async Mapping**: Refactoring for asynchronous result mapping
  - New extension methods for `Task<T>`
  - Support for `ValueTask<T>`
  - Automatic mapping of async `IBusinessResult<T>`

### Improved
- Performance in async operations
- Memory allocation reduction

## [4.1.181] - 2023

### Removed
- **Anti-patterns**: Removal of identified problematic patterns
  - Elimination of unnecessary coupling
  - Removal of circular dependencies
  - Simplification of excessive abstractions

### Changed
- **Log Entities**: Separation of log entity contexts
  - Contract-only usage for better abstraction
  - Separate `IEntityLog<T>` and `IEntityDateLog` interfaces
  - Optional base implementations: `EntityBase` separated from `EntityBaseLog`

### Added
- Detailed architectural documentation
- Tests for database context with logging

### Fixed
- Dependency injection in RabbitMQ client
- Dependency injection in Pipeline
- Isolated consumers for RabbitMQ client

### Documentation
- Updated and detailed architectural resources
- Usage examples for entities with auditing

## [3.12.262] - 2022

### Changed
- Complete extension refactoring
  - Organization by namespace
  - Removal of duplications
  - Naming standardization

## [3.12.261] - 2022

### Added
- Tests for custom middlewares
- WebAPI test coverage

## [3.12.221] - 2022

### Added
- **Resilience**: Polly implementation for fault tolerance
  - Configurable retry policies
  - Circuit breaker pattern
  - Timeout policies
  - Fallback strategies

- **Delegation Handlers**: Header key propagation
  - Automatic Correlation ID
  - Authorization header propagation
  - Configurable custom headers
  - HTTP request logging

### Fixed
- Automatic loading of mapping classes with `IMapFrom`
- Reflection bug in dynamic assemblies

### Details
This release brought major improvements in resilience and observability for distributed applications.

## [3.12.151] - 2022

### Changed
- **IMapFrom**: Removal of redundant generic typing
  - Interface simplification
  - Automatic type detection
  - Cleaner configuration

### Added
- **Testcontainers**: Support for Docker container tests
  - RabbitMQ testcontainer
  - Redis testcontainer
  - MongoDB testcontainer
  - Automatic integration test configuration

### Details
Testcontainers revolutionized integration testing, enabling real tests against services in Docker containers.

## [3.2.241] - 2021

### Changed
- **Fluent Configuration**: Migration from JSON configurations to fluent extensions
  - Fluent API for DbContext
  - Fluent configuration for RabbitMQ
  - Fluent configuration for Redis
  - Fluent configuration for Pipeline

- **Notification Pattern**: Notification system replacement
  - New `BusinessResult<T>` with integrated messages
  - Removal of separate notification context
  - Typed messages (Info, Error, Warning, Success)

### Added
- **HealthCheck**: Full health check support
  - Health checks for SQL Server, PostgreSQL, MySQL
  - Health checks for MongoDB
  - Health checks for Redis
  - Health checks for RabbitMQ
  - WebStatus project with HealthCheckUI

- **Telemetry**: Customizable telemetry system
  - Trace/Verbose across all libraries
  - Configurable log levels
  - Filters by operation
  - Integration with logging providers

- **Advanced RabbitMQ**: Advanced messaging features
  - Configurable Dead Letter Queue
  - Persistent connection with Polly
  - Async consumer
  - Automatic retry

### Improved
- **Transaction Isolation**: Isolation level configuration for EF
  - Configurable transaction scope
  - Read committed by default
  - Read optimization

- **Pipeline**: Pipeline system improvements
  - Adding messages to the package during execution
  - Rollback operation support
  - Shared context between operations

- **Validation**: Enhanced validation system
  - FluentValidation with structured messages
  - DataAnnotations with structured messages
  - Consistent error returns

### Documentation
- Complete WebAPI documentation
- Updated configuration guides
- Usage examples for all features

### Details
Landmark release with major refactorings and fundamental new features. Introduction of the BusinessResult pattern and telemetry.

## [3.1.x and earlier] - 2020-2021

### Base Features Implemented
- ✅ **Relational Database**
  - SQL Server with Entity Framework Core
  - PostgreSQL with Npgsql
  - MySQL with Pomelo
  - Repository pattern
  - Unit of Work pattern
  - Automatic soft delete
  - Automatic auditing

- ✅ **NoSQL Database**
  - MongoDB with official driver
  - Redis with StackExchange.Redis
  - Repository pattern for NoSQL
  - Distributed cache

- ✅ **Message Broker**
  - RabbitMQ with official client
  - Publisher/Subscriber pattern
  - Work Queue pattern
  - Request/Reply pattern

- ✅ **Pipeline**
  - Pipe and Filters pattern
  - Sequential operations
  - Automatic rollback
  - Dependency injection

- ✅ **Documentation**
  - Swagger/OpenAPI integration
  - XML comments support
  - UI customization

- ✅ **Mapping**
  - AutoMapper integration
  - Automatic profiles
  - IMapFrom interface

- ✅ **Logging**
  - ILogger integration
  - Structured logging
  - Multiple providers

- ✅ **Validation**
  - FluentValidation support
  - Data Annotations support
  - Custom validations

- ✅ **Specification Pattern**
  - Reusable LINQ expressions
  - Specification composition
  - AND, OR, NOT operators

---

## Types of Changes

- `Added` for new features
- `Changed` for changes in existing features
- `Deprecated` for features that will be removed
- `Removed` for removed features
- `Fixed` for bug fixes
- `Security` for vulnerabilities

## Versioning Conventions

This project follows [Semantic Versioning](https://semver.org/):
- **MAJOR**: Incompatible API changes
- **MINOR**: Backward-compatible feature additions
- **PATCH**: Backward-compatible bug fixes

Format: `MAJOR.MINOR.PATCH` (e.g.: 8.3.261)

## Links

- [Documentation](https://kallebelins.github.io/mvp24hours-dotnet)
- [Repository](https://github.com/kallebelins/mvp24hours-dotnet)
- [Examples](https://github.com/kallebelins/mvp24hours-dotnet-samples)
- [Issues](https://github.com/kallebelins/mvp24hours-dotnet/issues)
- [Releases](https://github.com/kallebelins/mvp24hours-dotnet/releases)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for details on how to contribute to the project.

## Acknowledgments

Built with ❤️ by [Kallebe Lins](https://github.com/kallebelins).

**Want to contribute?** See [CONTRIBUTING.md](CONTRIBUTING.md) to get started!
