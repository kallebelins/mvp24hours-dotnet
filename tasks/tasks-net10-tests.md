# Tasks — Cobertura de Testes > 95% para Mvp24Hours .NET

> Gerado em 18/07/2026 a partir de uma análise completa da solução.
> **Status inicial:** Build com **0 erro(s)**, **2282 testes aprovados**, **1 falha** (flaky), **4 ignorados**.
> **Cobertura atual:** **28.3%** (4524 linhas cobertas / 15973 linhas totais).
> **Meta:** **>95%** de cobertura de linha.
>
> **Diagnóstico:** Apenas 3 assemblies têm cobertura mensurável:
> - `Mvp24Hours.Infrastructure.Caching.Redis` — 40.9%
> - `Mvp24Hours.Infrastructure.Cqrs` — 28%
> - `Mvp24Hours.Infrastructure.CronJob` — 28.9%
>
> **Projetos de produção SEM projeto de testes dedicado:**
> - `Mvp24Hours.Infrastructure` (~232 arquivos)
> - `Mvp24Hours.Infrastructure.Data.EFCore` (~83 arquivos)
>
> **Projetos com cobertura muito baixa (<10 testes ou <20% cobertura):**
> - `Mvp24Hours.WebAPI` (apenas 5 testes)
> - `Mvp24Hours.Application.RabbitMQ.Test` (apenas 6 testes)
> - `Mvp24Hours.Infrastructure.Caching.Test` (apenas 38 testes)
>
> **Convenção de status:** `[ ]` pendente · `[x]` concluído · `[~]` em andamento/bloqueado.

---

## FASE 1 — Diagnóstico e Baseline de Cobertura

> **Objetivo:** Estabelecer um baseline mensurável e identificar os gaps de cobertura.

[x] 1.1 - Corrigir teste flaky `NotificationTest.PublishAsync_ShouldExecuteHandlersSequentially`
- O teste `Mvp24Hours.Infrastructure.Cqrs.Test.NotificationTest.PublishAsync_ShouldExecuteHandlersSequentially` estava falhando com `Assert.Equal() Failure: Values differ Expected: 5 Actual: 15` por condição de corrida: handlers usavam `List` estático compartilhado, poluído por `BenchmarkTest`/`MediatorTest` em paralelo. Corrigido com captura via `AsyncLocal` (BeginCapture/EndCapture). Mesmo padrão aplicado em handlers de Domain Events.
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/NotificationTest.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/Support/TestNotification.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/Support/TestDomainEvent.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/DomainEventTest.cs`
- https://xunit.net/docs/running-tests-in-parallel
- Verificado: 347 testes Cqrs aprovados em 3 execuções consecutivas.

[x] 1.2 - Corrigir erro de carregamento do `Mvp24Hours.Application.MongoDb.Test`
- O projeto de testes `Mvp24Hours.Application.MongoDb.Test` falhava na descoberta com `System.IO.FileLoadException: Could not load file or assembly 'Mvp24Hours.Application.dll'. Acesso negado.` durante `dotnet test --list-tests` / execução paralela da solution.
- **Causa:** `GeneratePackageOnBuild=true` nos projetos de produção empacotava NuGet em paralelo com a descoberta/execução dos testes; o pack travava a DLL no Windows e o xUnit não conseguia carregar a cópia em `Application.MongoDb.Test\bin`.
- **Correção:** `src/Directory.Build.targets` força `GeneratePackageOnBuild=false` (override pós-csproj). CI já empacota via `dotnet pack` em `.github/workflows/ci.yml`.
- Verificado: `dotnet test --list-tests` lista os 11 testes MongoDb sem FileLoadException; execução isolada **11 aprovados**; sem mensagens `Pacote criado` no build Debug.
- `src/Directory.Build.targets`
- `src/Tests/Mvp24Hours.Application.MongoDb.Test/Mvp24Hours.Application.MongoDb.Test.csproj`
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices
- `src/Tests/Mvp24Hours.Application.MongoDb.Test/**/*.cs`

[x] 1.3 - Gerar relatório de cobertura detalhado por projeto
- Executado `dotnet test src/Mvp24Hours.sln --collect:"XPlat Code Coverage" --results-directory ./test-results` (16 projetos · **2294** aprovados · **0** falhas · **4** ignorados).
- Relatório HTML + JsonSummary + Cobertura mesclado via `reportgenerator` em `test-results/coverage-report/` (filtro `+Mvp24Hours*`).
- Baseline versionado em `tasks/coverage-baseline-tests.json` com cobertura por assembly, classe e método.
- **Baseline:** linha **28.3%** (4524/15973) · branch **25.8%** · método **47.8%**. Apenas **3/12** assemblies de produção com dados Coverlet: Caching.Redis **40.9%**, Cqrs **28.1%**, CronJob **29%**. Os outros 9 projetos aparecem sem instrumentação mensurável neste run.
- `dotnet test src/Mvp24Hours.sln --collect:"XPlat Code Coverage" --results-directory ./test-results`
- `reportgenerator -reports:"test-results/**/coverage.cobertura.xml" -targetdir:"./test-results/coverage-report" -reporttypes:"Html;JsonSummary;Cobertura"`
- https://github.com/coverlet-coverage/coverlet
- https://github.com/danielpalme/ReportGenerator
- `tasks/coverage-baseline-tests.json`
- `test-results/coverage-report/index.html` (local, não versionado)

[x] 1.4 - Inventariar todas as classes de produção sem cobertura
- Inventário gerado a partir do baseline Coverlet (1.3) + varredura de tipos públicos (`class`/`interface`/`enum`/`record`/`struct`) em `src/Mvp24Hours.*`.
- **Resultado:** 2525 tipos públicos · **2393 sem cobertura** · 132 com alguma cobertura Coverlet. Prioridade: P1 lógica **1546** · P2 helpers **40** · P3 extensions **217** · P4 contratos **590**.
- Apenas 3/12 assemblies com dados Coverlet; 9 projetos entram 100% no inventário até instrumentação. **95** tipos com `lineCoverage = 0` em Cqrs/CronJob (ROI imediato).
- Sem projeto de teste dedicado: `Mvp24Hours.Infrastructure` (351 tipos) e `Mvp24Hours.Infrastructure.Data.EFCore` (141, cobertura indireta via Application.*Sql.Test).
- https://learn.microsoft.com/dotnet/core/testing/
- `tasks/classes-without-tests.md`
- `tasks/classes-without-tests.raw.json`

---

## FASE 2 — Criar Projeto de Testes para `Mvp24Hours.Infrastructure`

> **Objetivo:** O projeto `Mvp24Hours.Infrastructure` (~232 arquivos) é um dos maiores e não possui projeto de testes dedicado.

[x] 2.1 - Criar projeto `Mvp24Hours.Infrastructure.Test`
- Criado projeto de testes xUnit em `src/Tests/Mvp24Hours.Infrastructure.Test/` no padrão de `Caching.Test` / `Cqrs.Test`: `xunit`, `Xunit.Priority`, `coverlet.collector`, `FluentAssertions`, `Moq`, `Microsoft.NET.Test.Sdk`, mais DI/Logging/Options. Referência a `Mvp24Hours.Infrastructure`. Incluído na solution sob a pasta `Tests`. `GlobalUsings.cs` com FluentAssertions/Xunit. Build 0 erro(s)/0 aviso(s); sem testes ainda (vêm em 2.2+).
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Mvp24Hours.Infrastructure.Caching.Test.csproj` (referência de estrutura)
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-with-dotnet-test
- `src/Tests/Mvp24Hours.Infrastructure.Test/Mvp24Hours.Infrastructure.Test.csproj`
- `src/Tests/Mvp24Hours.Infrastructure.Test/GlobalUsings.cs`
- `src/Mvp24Hours.sln`

[x] 2.2 - Testes para `Email/Providers/*`
- Criados testes unitários para os 4 providers existentes (`InMemoryEmailProvider`, `SmtpEmailProvider`, `AzureCommunicationEmailProvider`, `SendGridEmailProvider`) + cobertura da lógica de `BaseEmailProvider` via InMemory (validação, defaults, batch, limites). `MailgunEmailProvider` **não existe** no código — omitido. HTTP via `TestHttpMessageHandler`; SMTP com host/porta inacessíveis (falha controlada). **36** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Email/Providers/*.cs` (5 arquivos; sem Mailgun)
- https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices
- `src/Tests/Mvp24Hours.Infrastructure.Test/Email/Providers/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/EmailTestHelpers.cs`

[x] 2.3 - Testes para `Email/Templates/*`
- Criados testes para `RazorEmailTemplateRenderer`, `ScribanEmailTemplateRenderer` + `TemplateValidationResult`/`TemplateRenderException`/`TemplateOptions`. `HandlebarsEmailTemplateRenderer` e `DefaultEmailTemplateService` **não existem** no código — o engine Liquid-like é Scriban. Cobertura: render (object/dictionary/fields/null), arquivo, validação, cancelamento e erros de parse. **55** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Email/Templates/*.cs` (3 arquivos; Scriban em vez de Handlebars)
- https://learn.microsoft.com/aspnet/core/mvc/views/razor
- https://github.com/scriban/scriban
- `src/Tests/Mvp24Hours.Infrastructure.Test/Email/Templates/*Test.cs`

[x] 2.4 - Testes para `Sms/Providers/*`
- Criados testes unitários para `InMemorySmsProvider`, `TwilioSmsProvider`, `AzureCommunicationSmsProvider` + cobertura da lógica de `BaseSmsProvider` via InMemory (validação de telefone/tamanho, defaults, batch, MMS, cancelamento). HTTP via `TestHttpMessageHandler`; Twilio mapeia status e Basic Auth; Azure cobre HMAC headers, connection string e respostas `value[]`. **40** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Sms/Providers/*.cs` (4 arquivos incl. Base)
- https://www.twilio.com/docs/sms
- `src/Tests/Mvp24Hours.Infrastructure.Test/Sms/Providers/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/SmsTestHelpers.cs`

[x] 2.5 - Testes para `FileStorage/Providers/*`
- Criados testes unitários para `InMemoryFileStorageProvider`, `LocalFileStorageProvider`, `AzureBlobStorageProvider`, `AwsS3StorageProvider` (task citava `S3FileStorageProvider` — nome real é `AwsS3`). InMemory/Local cobrem upload (bytes/stream/chunks), download, listagem, copy/move, validação, overwrite e path normalization; Local usa pasta temp + content-type por extensão + bloqueio de path traversal. Azure/S3 são stubs (`NotImplementedException` até pacotes Azure.Storage.Blobs / AWSSDK.S3) — testes cobrem guards do construtor e NIE em todas as operações. **97** testes aprovados.
- `src/Mvp24Hours.Infrastructure/FileStorage/Providers/*.cs` (5 arquivos incl. FileMetadata)
- https://learn.microsoft.com/azure/storage/blobs/
- `src/Tests/Mvp24Hours.Infrastructure.Test/FileStorage/Providers/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/FileStorageTestHelpers.cs`

[x] 2.6 - Testes para `Http/Resilience/*`
- Criados testes para `RetryPolicy`, `CircuitBreakerPolicy`, `TimeoutPolicy`, `PolicyWrap`, `BulkheadPolicy`, `FallbackPolicy`, `NativeResilienceBuilder` (+ extensions), `NativeResilienceOptions` e `NativeHttpResilienceExtensions`. Cobertura: retry (backoff, Retry-After, exaustão), circuit breaker (open/half-open/isolate/reset), timeout (optimistic/pessimistic), composition, bulkhead rejection, fallback e registro DI do handler nativo. **101** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Http/Resilience/*.cs` (11 arquivos)
- https://learn.microsoft.com/dotnet/core/resilience/
- `src/Tests/Mvp24Hours.Infrastructure.Test/Http/Resilience/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/ResilienceTestHelpers.cs`

[x] 2.7 - Testes para `Http/DelegatingHandlers/*`
- Criados testes para os 10 handlers: `TelemetryDelegatingHandler`, `RetryDelegatingHandler`, `CircuitBreakerDelegatingHandler`, `TimeoutDelegatingHandler`, `LoggingDelegatingHandler`, `CompressionDelegatingHandler`, `AuthenticationDelegatingHandler`, `PropagationCorrelationIdDelegatingHandler` (task citava `CorrelationIdDelegatingHandler`), `PropagationAuthorizationDelegatingHandler`, `PropagationHeaderDelegatingHandler`. Cobertura: telemetria (Activity/tags/events), retry (backoff, Retry-After, exaustão), circuit breaker (open/half-open/isolate/reset), timeout por request, auth (Bearer/ApiKey/Basic), compressão (Gzip/Brotli/Deflate), logging (connection refused → 502) e propagação de headers via `IHttpContextAccessor`. **91** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Http/DelegatingHandlers/*.cs` (10 arquivos)
- https://learn.microsoft.com/dotnet/api/system.net.http.delegatinghandler
- `src/Tests/Mvp24Hours.Infrastructure.Test/Http/DelegatingHandlers/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/DelegatingHandlerTestHelpers.cs`

[x] 2.8 - Testes para `BackgroundJobs/*`
- Criados testes para `InMemoryDeadLetterQueue`, `InMemoryJobMetrics`, `PriorityQueueManager`, `InMemoryJobHistoryStore`, `HangfireJobProvider`. DLQ/metrics/history cobrem CRUD, filtros, paginação, agregações e limpeza; PriorityQueue cobre prioridade Critical→Low, FIFO, clear e stats (`InternalsVisibleTo` no Infrastructure). Hangfire é stub (`NotSupportedException` até pacotes Hangfire) — testes cobrem guards do construtor e NSE em todas as operações. **92** testes aprovados.
- `src/Mvp24Hours.Infrastructure/BackgroundJobs/**/*.cs` (15+ arquivos)
- https://www.hangfire.io/
- `src/Tests/Mvp24Hours.Infrastructure.Test/BackgroundJobs/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/BackgroundJobsTestHelpers.cs`

[x] 2.9 - Testes para `DistributedLocking/*`
- Criados testes para `InMemoryDistributedLockProvider`, `RedisDistributedLockProvider`, `SqlServerDistributedLockProvider`, `PostgreSqlDistributedLockProvider` + `DistributedLockFactory`, `DistributedLockOptions`, `LockAcquisitionResult`, `DistributedLockMetrics`, `DistributedLockAcquisitionException` e `DistributedLockingServiceExtensions`. InMemory cobre acquire/release/renew/expiry/contenção/auto-renewal/métricas (via `BaseDistributedLockProvider`); Redis via Moq (`StringSet` NX + Lua scripts + RedLock quorum); SQL/PostgreSQL cobrem guards do construtor e falha com host inacessível. Corrigido bug em `LockHandleBase.Dispose`/`DisposeAsync` que marcava `_disposed` antes de liberar o lock. **97** testes aprovados.
- `src/Mvp24Hours.Infrastructure/DistributedLocking/**/*.cs` (15 arquivos)
- https://learn.microsoft.com/azure/architecture/patterns/leader-election
- `src/Tests/Mvp24Hours.Infrastructure.Test/DistributedLocking/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/DistributedLockingTestHelpers.cs`
- `src/Mvp24Hours.Infrastructure/DistributedLocking/Providers/LockHandleBase.cs`

[x] 2.10 - Testes para `Security/*`
- Criados testes para `EncryptionHelper` (em Helpers), `SecretRotationHelper`, `SensitiveDataMasker`, `EnvironmentVariableSecretProvider`, `AwsSecretsManagerProvider`, `AzureKeyVaultSecretProvider` (task citava `AzureKeyVaultProvider`) + options, `SecurityServiceExtensions` e `LoggingExtensions`. Env provider cobre prefix/CRUD/existência; AWS/Azure cobrem guards do construtor e validação de args (SDK não injetável); EncryptionHelper cobre round-trip AES, IV único e erros. **144** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Security/**/*.cs` (13 arquivos) + `Helpers/EncryptionHelper.cs`
- https://learn.microsoft.com/azure/key-vault/
- `src/Tests/Mvp24Hours.Infrastructure.Test/Security/**/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/SecurityTestHelpers.cs`

[x] 2.11 - Testes para `Resilience/*`
- Criados testes para `Bulkhead`/`Bulkhead<T>`, `CircuitBreaker`/`CircuitBreaker<T>` (wrapper legado), `RetryPolicy`/`RetryPolicy<T>`, `RetryHelper`, `RetryWithJitter`, options, exceptions e `NativeResiliencePipeline` (+ DI via `NativeResilienceServiceExtensions`). Cobertura: limite de concorrência/fila, open/half-open/isolate/reset, retry com backoff (constant/linear/exponential/jitter), timeout Polly, presets e registro keyed. `ResilientOperationExecutor` **não existe** no código — omitido. **106** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Resilience/**/*.cs` (16 arquivos)
- https://learn.microsoft.com/dotnet/core/resilience/
- `src/Tests/Mvp24Hours.Infrastructure.Test/Resilience/**/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/GenericResilienceTestHelpers.cs`

[x] 2.12 - Testes para `HealthChecks/*`
- Criados testes para `FileStorageHealthCheck`, `EmailServiceHealthCheck`, `SmsServiceHealthCheck` (task citava `EmailHealthCheck`/`SmsHealthCheck`) + `HttpClientHealthCheck`, `DistributedLockHealthCheck`, `BackgroundJobHealthCheck`, options e `InfrastructureHealthCheckExtensions`. Cobertura: upload/exists/download/delete, send opcional (email/SMS), HEAD/GET HTTP, acquire/release de lock, registro DI condicional via `AddInfrastructureHealthChecks`, thresholds Healthy/Degraded/Unhealthy. **82** testes aprovados.
- `src/Mvp24Hours.Infrastructure/HealthChecks/*.cs` (7 arquivos)
- https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks
- `src/Tests/Mvp24Hours.Infrastructure.Test/HealthChecks/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/HealthChecksTestHelpers.cs`

[x] 2.13 - Testes para `Helpers/*`
- Criados testes para `DirectoryHelper`/`DirectoryService`, `FileLogHelper`, `TimeZoneHelper`, `ConfigurationHelper`, `WebRequestHelper` (`ToQueryString`), `HttpPolicyHelper` (smoke) e `CertificateHelper` (em `Http/Helpers`). `EncryptionHelper` já coberto em 2.10 (`Security/Helpers/EncryptionHelperTest`). Certificados self-signed em fixture; `TempDirectory` cria pasta real. **~70** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Helpers/*.cs` (7 arquivos) + `Http/Helpers/CertificateHelper.cs`
- https://learn.microsoft.com/dotnet/standard/security/cryptography-model
- `src/Tests/Mvp24Hours.Infrastructure.Test/Helpers/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/HelpersTestHelpers.cs`

[x] 2.14 - Testes para `Observability/*`
- Criados testes para `InfrastructureDiagnostics`, `ObservabilityServiceExtensions`/`ObservabilityOptions`, `CorrelationIdPropagation`, `ObservabilityHelper`, `ActivitySources`, `InfrastructureMetrics` e `StructuredLoggingExtensions`. Cobertura: agregação de health/metrics/errors com providers Moq, baggage/header de correlation ID, wrapper async/sync com `FakeActivityListener`, métricas via `FakeMeterListener`, registro DI. **~63** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Observability/**/*.cs` (8 arquivos)
- https://learn.microsoft.com/dotnet/core/diagnostics/
- `src/Tests/Mvp24Hours.Infrastructure.Test/Observability/*Test.cs`
- `src/Tests/Mvp24Hours.Infrastructure.Test/Support/ObservabilityTestHelpers.cs`

[x] 2.15 - Testes para `Testing/*`
- Criados testes para `FakeEmailService`, `FakeSmsService`, `FakeFileStorage`, `TestHttpMessageHandler`, `FakeTimeProviderHelper`, `MockClock`, `FakeMeterListener`, `FakeActivityListener`, `EmailAssertions`, `SmsAssertions`, `ActivityAssertions` e `HttpClientTestFixture`. Cobertura: fakes (sucesso/falha/batch/queries), HTTP matchers/recording, listeners de Activity/Meter, asserções pass/fail (`AssertionException`) e fixture HTTP. **~124** testes aprovados.
- `src/Mvp24Hours.Infrastructure/Testing/**/*.cs` (25 arquivos)
- https://learn.microsoft.com/dotnet/core/testing/
- `src/Tests/Mvp24Hours.Infrastructure.Test/Testing/**/*Test.cs`

---

## FASE 3 — Criar Projeto de Testes para `Mvp24Hours.Infrastructure.Data.EFCore`

> **Objetivo:** O projeto de EF Core (~83 arquivos) é crítico para persistência e não possui projeto de testes dedicado.

[x] 3.1 - Criar projeto `Mvp24Hours.Infrastructure.Data.EFCore.Test`
- Criado projeto xUnit em `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/` no padrão de `Infrastructure.Test` / `SQLServer.Test`: FluentAssertions, Moq, xunit, coverlet, EF InMemory, Hosting. Referências a Core, Infrastructure e Data.EFCore. Incluído na solution sob `Tests`. Support: `TestDbContext` + entidades (audit/soft-delete/tenant/versioned/domain events) e `EfCoreTestHelpers`.
- `src/Tests/Mvp24Hours.Application.SQLServer.Test/Mvp24Hours.Application.SQLServer.Test.csproj` (referência)
- https://learn.microsoft.com/ef/core/testing/
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Mvp24Hours.Infrastructure.Data.EFCore.Test.csproj`
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Support/*`

[x] 3.2 - Testes para `Repository.cs` e `RepositoryAsync.cs`
- CRUD sync/async: Add/Modify/Remove/GetById/List/GetBy/Any/Count, paging (`PagingCriteria`), hard-delete vs soft-delete (`IEntityDateLog`). Corrigido `RepositoryAsync.RemoveAsync` para soft-delete de `IEntityDateLog` (alinhado ao sync). **18** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Repository.cs`, `Async/RepositoryAsync.cs`
- https://learn.microsoft.com/ef/core/querying/
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/RepositoryTest.cs`, `RepositoryAsyncTest.cs`

[x] 3.3 - Testes para `ReadOnlyRepository.cs` e `ReadOnlyRepositoryAsync.cs`
- Consultas somente-leitura sync/async, `AsNoTracking` (entidades detached), `GetBySpecification`. **6** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/ReadOnlyRepository.cs`, `Async/ReadOnlyRepositoryAsync.cs`
- https://learn.microsoft.com/ef/core/querying/tracking
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/ReadOnlyRepositoryTest.cs`

[x] 3.4 - Testes para `BulkOperationsRepositoryAsync.cs`
- BulkInsert/BulkUpdate/BulkDelete (lista vazia, sucesso, progresso por batch). `ExecuteUpdate`/`ExecuteDelete` **ignorados** (InMemory não suporta; trait `RequiresRealDatabase`). **7** aprovados · **2** ignorados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Async/BulkOperationsRepositoryAsync.cs`
- https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/BulkOperationsRepositoryAsyncTest.cs`

[x] 3.5 - Testes para `StreamingRepositoryAsync.cs`
- `StreamAllAsync`, `StreamByAsync`, `StreamBatchesAsync`, `StreamProjectedAsync`, `StreamAndProcessAsync` via InMemory. **5** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Async/StreamingRepositoryAsync.cs`
- https://learn.microsoft.com/dotnet/csharp/whats-new/tutorials/generate-consume-asynchronous-stream
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/StreamingRepositoryAsyncTest.cs`

[x] 3.6 - Testes para `UnitOfWorkWithEventsAsync.cs`
- `SaveChangesWithEvents`/`SaveChangesWithEventsAsync`, dispatcher capturando eventos, limpeza de domain events, `GetEntitiesWithEvents` (sync + async). **6** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Async/UnitOfWorkWithEventsAsync.cs`
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/UnitOfWorkWithEventsAsyncTest.cs`, `UnitOfWorkWithEventsTest.cs`

[x] 3.7 - Testes para `Interceptors/*`
- Audit, SoftDelete, Tenant (`PreventTenantIdChange`), Concurrency (version counter), DomainEvent dispatch, CommandLogging/StructuredLogging/SlowQuery (smoke). **13** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Interceptors/*.cs` (8 arquivos)
- https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Interceptors/*Test.cs`

[x] 3.8 - Testes para `Specifications/SpecificationEvaluator.cs`
- Filtro via `ISpecificationQuery` + OrderBy/Skip/Take via `ISpecificationQueryEnhanced`. **2** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Specifications/SpecificationEvaluator.cs`
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Specifications/SpecificationEvaluatorTest.cs`

[x] 3.9 - Testes para `Resilience/*`
- `DbContextCircuitBreaker` (open/half-open/reset), `DbContextPoolMonitor` stats, `AddNativeDbResilience` + presets SqlServer/PostgreSql/MySql, `MvpExecutionStrategy` metadata (Obsolete). **13** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Resilience/*.cs`
- https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Resilience/*Test.cs`

[x] 3.10 - Testes para `Testing/*`
- `RepositoryFake`/`RepositoryFakeAsync`, `TestDbContextFactory`/`InMemoryDbContextFactory`, `TestingExtensions` DI, `IDataSeeder`. **26** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Testing/*.cs`
- https://learn.microsoft.com/ef/core/testing/
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Testing/*Test.cs`

[x] 3.11 - Testes para `Migrations/*`
- `MigrationService` (EnsureCreated/Delete/pending no InMemory), options/result, hosted service smoke, DI extensions. APIs relacionais assertam `InvalidOperationException` no InMemory. **22** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Migrations/*.cs`
- https://learn.microsoft.com/ef/core/managing-schemas/migrations/
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Migrations/*Test.cs`

[x] 3.12 - Testes para `Converters/*`
- `EntityIdValueConverters` (Guid/Int/Long/String) e `EncryptedValueConverters` (round-trip com provider fake). **14** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Converters/*.cs`
- https://learn.microsoft.com/ef/core/modeling/value-conversions
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Converters/*Test.cs`

[x] 3.13 - Testes para `Cqrs/*`
- `NoOpDomainEventDispatcher`, `DomainEventDispatcherAdapter`, `ReadDbContextBase` (SaveChanges lança) / write context. **12** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/Cqrs/*.cs`
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/Cqrs/*Test.cs`

[x] 3.14 - Testes para `ReadWriteSplitting/*`
- `ReplicaSelector` (RoundRobin/Random/Weighted), `ConnectionResolver` (read/write + sticky pós-write), options, DI extensions. **15** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/ReadWriteSplitting/*.cs`
- https://learn.microsoft.com/azure/azure-sql/database/read-scale-out
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/ReadWriteSplitting/*Test.cs`

[x] 3.15 - Testes para `SchemaValidation/*`
- Options, `SchemaValidator` (connectivity/summary no InMemory), extensions DI, hosted service smoke. **12** testes aprovados.
- `src/Mvp24Hours.Infrastructure.Data.EFCore/SchemaValidation/*.cs`
- https://learn.microsoft.com/ef/core/modeling/
- `src/Tests/Mvp24Hours.Infrastructure.Data.EFCore.Test/SchemaValidation/*Test.cs`

> **Resultado Fase 3:** **175 aprovados · 0 falhas · 2 ignorados** (`ExecuteUpdate`/`ExecuteDelete` — InMemory).

---

## FASE 4 — Expandir Testes de `Mvp24Hours.WebAPI`

> **Objetivo:** O projeto WebAPI (~115 arquivos) tem apenas 5 testes. Precisa de cobertura massiva.

[x] 4.1 - Testes para `Middlewares/*`
- Criados testes unitários para 12 middlewares: `ExceptionMiddleware`, `CorrelationIdMiddleware`, `RateLimitingMiddleware`, `RequestLoggingMiddleware`, `SecurityHeadersMiddleware`, `ETagMiddleware`, `ProblemDetailsMiddleware`, `RequestContextMiddleware`, `IpFilteringMiddleware`, `RequestSizeLimitMiddleware`, `RequestTelemetryMiddleware`, `AntiForgeryMiddleware`. Pipeline via `DefaultHttpContext` + Moq para `IRequestLogger`/`IExceptionToProblemDetailsMapper`. **15** testes aprovados.
- `src/Mvp24Hours.WebAPI/Middlewares/*.cs` (12+ arquivos)
- https://learn.microsoft.com/aspnet/core/fundamentals/middleware/
- `src/Tests/Mvp24Hours.WebAPI.Test/Middlewares/MiddlewaresTest.cs`

[x] 4.2 - Testes para `Filters/*`
- Criados testes para `ModelStateValidationFilter`, `ProblemDetailsResultFilter`, `ContentNegotiationResultFilter` + filtros Swagger (`CustomSwaggerFilter`, `ExamplesOperationFilter`, `AuthResponsesOperationFilter`, `DeprecationOperationFilter`). **12** testes aprovados.
- `src/Mvp24Hours.WebAPI/Filters/*.cs`, `src/Mvp24Hours.WebAPI/Filters/Swagger/*.cs` (10+ arquivos)
- https://learn.microsoft.com/aspnet/core/mvc/controllers/filters
- `src/Tests/Mvp24Hours.WebAPI.Test/Filters/FiltersTest.cs`, `Filters/SwaggerFiltersTest.cs`

[x] 4.3 - Testes para `Binders/*`
- Criados testes para `TimeOnlyModelBinder`, `DateOnlyModelBinder`, `Mvp24HoursModelBinderProvider` (routing para DateOnly/TimeOnly/EntityId/PagingCriteria). **7** testes aprovados.
- `src/Mvp24Hours.WebAPI/Binders/*.cs` (5+ arquivos)
- https://learn.microsoft.com/aspnet/core/mvc/advanced/custom-model-binding
- `src/Tests/Mvp24Hours.WebAPI.Test/Binders/BindersTest.cs`

[x] 4.4 - Testes para `RateLimiting/*`
- Criados testes para `DefaultRateLimitKeyGenerator` (IP/user/API key/tenant), `RateLimitPartitionResolver` (bypass/whitelist), `InMemoryRateLimiter` e `RedisDistributedRateLimiter` com `MemoryDistributedCache`. **8** testes aprovados.
- `src/Mvp24Hours.WebAPI/RateLimiting/*.cs` (5+ arquivos)
- https://learn.microsoft.com/aspnet/core/performance/rate-limit
- `src/Tests/Mvp24Hours.WebAPI.Test/RateLimiting/RateLimitingTest.cs`

[x] 4.5 - Testes para `Idempotency/*`
- Criados testes para `DefaultIdempotencyKeyGenerator` (header/body/hash), `CqrsIdempotencyKeyGenerator` (JSON body), `DistributedCacheIdempotencyStore` (lock/replay/complete via `MemoryDistributedCache`). **6** testes aprovados.
- `src/Mvp24Hours.WebAPI/Idempotency/*.cs` (4 arquivos)
- https://learn.microsoft.com/azure/architecture/patterns/idempotent-operations
- `src/Tests/Mvp24Hours.WebAPI.Test/Idempotency/IdempotencyTest.cs`

[x] 4.6 - Testes para `ContentNegotiation/*`
- Criados testes para `AcceptHeaderNegotiator` (Accept header, format param, URL suffix), `ContentFormatterRegistry`, `JsonContentFormatter`/`XmlContentFormatter`, `ProblemDetailsJsonFormatter`/`ProblemDetailsXmlFormatter`. **6** testes aprovados.
- `src/Mvp24Hours.WebAPI/ContentNegotiation/*.cs` (6 arquivos)
- https://learn.microsoft.com/aspnet/core/web-api/advanced/formatting
- `src/Tests/Mvp24Hours.WebAPI.Test/ContentNegotiation/ContentNegotiationTest.cs`

[x] 4.7 - Testes para `Exceptions/*`
- Criados testes para `DefaultExceptionToProblemDetailsMapper` (NotFound/Validation/Argument), `ValidationProblemDetailsMapper`, `CompositeExceptionToProblemDetailsMapper` (chain delegation). **5** testes aprovados.
- `src/Mvp24Hours.WebAPI/Exceptions/*.cs` (4 arquivos)
- https://learn.microsoft.com/aspnet/core/web-api/handle-errors
- `src/Tests/Mvp24Hours.WebAPI.Test/Exceptions/ExceptionMappersTest.cs`

[x] 4.8 - Testes para `Endpoints/*`
- Criados testes para `ValidationEndpointFilter`, `NativeValidationEndpointFilter`, `ExceptionHandlingEndpointFilter`, `LoggingEndpointFilter`, `CorrelationIdEndpointFilter`, `TimeoutEndpointFilter`, `NativeTypedResultsExtensions`/`TypedResultsExtensions`. **8** testes aprovados.
- `src/Mvp24Hours.WebAPI/Endpoints/*.cs`, `src/Mvp24Hours.WebAPI/Endpoints/Filters/*.cs` (8+ arquivos)
- https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/
- `src/Tests/Mvp24Hours.WebAPI.Test/Endpoints/EndpointsTest.cs`

[x] 4.9 - Testes para `Configuration/*`
- Criados testes para options: `ExceptionOptions`, `CorrelationIdOptions`, `SecurityHeadersOptions`, `ETagOptions`, `RateLimitingOptions`, `ApiVersioningOptions`, `HealthCheckOptions`, `MvpProblemDetailsOptions`, `IdempotencyOptions`, `RequestLoggingOptions`, `IpFilteringOptions`, `AntiForgeryOptions`. **12** testes aprovados.
- `src/Mvp24Hours.WebAPI/Configuration/*.cs` (20+ arquivos)
- https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options
- `src/Tests/Mvp24Hours.WebAPI.Test/Configuration/ConfigurationOptionsTest.cs`

[x] 4.10 - Testes para `OpenApi/*`
- Criados testes para transformers: `SecuritySchemeTransformer`, `CustomHeadersTransformer`, `CommonResponsesTransformer`, `DeprecationTransformer`, `TagFilterTransformer`, `ProblemDetailsTransformer`, `RateLimitHeadersTransformer`. **6** testes aprovados.
- `src/Mvp24Hours.WebAPI/OpenApi/*.cs` (1+ arquivos)
- https://learn.microsoft.com/aspnet/core/tutorials/getting-started-with-swashbuckle
- `src/Tests/Mvp24Hours.WebAPI.Test/OpenApi/OpenApiTransformersTest.cs`

[x] 4.11 - Testes para `Http/*`
- Criados testes para `CorrelationIdHandler` (propagação de correlation ID via `IHttpContextAccessor`) e `AsyncLocalCorrelationContextProvider`. **2** testes aprovados.
- `src/Mvp24Hours.WebAPI/Http/*.cs` (1+ arquivo)
- https://learn.microsoft.com/aspnet/core/fundamentals/http-requests
- `src/Tests/Mvp24Hours.WebAPI.Test/Http/CorrelationIdHandlerTest.cs`

[x] 4.12 - Testes para `Services/*`
- Criados testes para `DefaultRequestLogger` (request/response/exception logging com masking). **3** testes aprovados.
- `src/Mvp24Hours.WebAPI/Services/*.cs` (1+ arquivo)
- https://learn.microsoft.com/aspnet/core/fundamentals/logging/
- `src/Tests/Mvp24Hours.WebAPI.Test/Services/DefaultRequestLoggerTest.cs`

[x] 4.13 - Testes para `HealthChecks/*`
- Criados testes para `BaseHealthCheck` (via subclass de teste) e `CacheHealthCheck` (MemoryCache + MemoryDistributedCache). **4** testes aprovados.
- `src/Mvp24Hours.WebAPI/HealthChecks/*.cs` (1+ arquivo)
- https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks
- `src/Tests/Mvp24Hours.WebAPI.Test/HealthChecks/HealthChecksTest.cs`

[x] 4.14 - Testes para `Extensions/*`
- Criados smoke tests para `ServiceCollectionExtentions` (`AddMvp24HoursWeb*`) e `ApplicationBuilderExtensions` (`UseMvp24Hours*`) + 5 testes TestHost existentes em `ApplicationBuilderTest` (exceptions, CORS, correlation ID). Namespace alinhado para `Mvp24Hours.WebAPI.Test`. **13** testes aprovados (8 novos + 5 existentes).
- `src/Mvp24Hours.WebAPI/Extensions/*.cs` (5+ arquivos)
- https://learn.microsoft.com/dotnet/core/extensions/dependency-injection
- `src/Tests/Mvp24Hours.WebAPI.Test/Extensions/ExtensionsSmokeTest.cs`, `ApplicationBuilderTest.cs`

> **Resultado Fase 4:** **107 aprovados · 0 falhas · 0 ignorados**. Infraestrutura: `GlobalUsings.cs`, `Support/WebApiTestHelpers.cs`, csproj atualizado (FluentAssertions, FrameworkReference).

---

## FASE 5 — Expandir Testes de `Mvp24Hours.Infrastructure.RabbitMQ`

> **Objetivo:** O projeto RabbitMQ (~175 arquivos) tem apenas 6 testes. Precisa de cobertura massiva.

[x] 5.1 - Testes para `Core/Contract/*`
- Criados testes para `Response<T>`, `ResponseStatus` e `ScheduleMessageOptions` (Success/Timeout/Failure/Cancelled, defaults). **6** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Core/Contract/*.cs` (10+ arquivos)
- https://www.rabbitmq.com/dotnet-api-guide.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Core/CoreContractTest.cs`

[x] 5.2 - Testes para `MvpRabbitMQClient.cs`
- Criados testes unitários com mock de `IMvpRabbitMQConnection`/`IModel`: routing key obrigatório, publish async/batch/TTL/priority/headers, register/unregister, consume sem consumers. Batch via `InMemoryBus`. **13** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/MvpRabbitMQClient.cs`
- https://www.rabbitmq.com/tutorials
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/MvpRabbitMQClientTest.cs`

[x] 5.3 - Testes para `Consumers/*`
- Criados testes para `BatchMessageResult`, `BatchProcessingHelper` (parallel/sequential/retry/transaction/group), `ConsumeContext`, `BatchConsumeContext`. **12** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Consumers/*.cs` (4 arquivos)
- https://www.rabbitmq.com/consumers.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Consumers/ConsumersTest.cs`

[x] 5.4 - Testes para `Transactional/*`
- Criados testes para `InMemoryTransactionalOutbox`, `TransactionalBus`, `OutboxPublisher`, `TransactionalConsumeContext`. **8** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Transactional/*.cs` (6 arquivos)
- https://www.rabbitmq.com/confirms.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Transactional/TransactionalTest.cs`

[x] 5.5 - Testes para `Saga/*`
- Criados testes para `SagaInstance`, `InMemorySagaRepository`, `SagaStateMachine` (máquina de teste com eventos/transições). **8** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Saga/*.cs` (6+ arquivos)
- https://microservices.io/patterns/data/saga.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Saga/SagaTest.cs`

[x] 5.6 - Testes para `Scheduling/*`
- Criados testes para `CronExpressionHelper`, `InMemoryScheduledMessageStore`, `MessageScheduler`, `RedisScheduledMessageStore` (MemoryDistributedCache). **9** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Scheduling/*.cs` (4 arquivos)
- https://www.rabbitmq.com/ttl.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Scheduling/SchedulingTest.cs`

[x] 5.7 - Testes para `RequestResponse/*`
- Criados testes para `RequestClient` (options/guards) e `TestHarness.RequestAsync` com fake client. **3** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/RequestResponse/*.cs` (1+ arquivo)
- https://www.rabbitmq.com/tutorials/tutorial-six-dotnet.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/RequestResponse/RequestResponseTest.cs`

[x] 5.8 - Testes para `Pipeline/Filters/*`
- Criados testes para `CorrelationConsumeFilter`, `TelemetryConsumeFilter`/`TelemetryPublishFilter`, `ExceptionHandlingConsumeFilter`, `FilterPipelineExecutor`, `ConsumeFilterContext`. **6** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Pipeline/Filters/*.cs` (3+ arquivos)
- https://masstransit.io/documentation/configuration/middleware
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Pipeline/PipelineFiltersTest.cs`

[x] 5.9 - Testes para `MultiTenancy/*`
- Criados testes para `InMemoryTenantRabbitMQResolver`, `TenantRabbitMQOptions`, `TenantConsumeFilter`, `TenantPublishFilter`. **7** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/MultiTenancy/*.cs` (6 arquivos)
- https://docs.microsoft.com/azure/architecture/guide/multitenant/overview
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/MultiTenancy/MultiTenancyTest.cs`

[x] 5.10 - Testes para `Topology/*`
- Criados testes para `RoutingKeyConvention`, `EndpointNameFormatter`, `EndpointConvention`, `MessageTopologyRegistry`. **6** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Topology/*.cs` (5 arquivos)
- https://www.rabbitmq.com/tutorials/tutorial-four-dotnet.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Topology/TopologyTest.cs`

[x] 5.11 - Testes para `Serialization/*`
- Criados testes para `JsonMessageSerializer` (round-trip) e `MessageTypeResolver` (register/headers). **4** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Serialization/*.cs` (2 arquivos)
- https://www.rabbitmq.com/queues.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Serialization/SerializationTest.cs`

[x] 5.12 - Testes para `Testing/*`
- Criados testes meta para `InMemoryBus`, `TestConsumeContextBuilder`, `ConsumedMessage`, `TestMessageHelpers`. **6** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Testing/*.cs` (5+ arquivos)
- https://masstransit.io/documentation/concepts/testing
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Testing/TestingInfrastructureTest.cs`

[x] 5.13 - Testes para `Observability/*`
- Criados testes para `RabbitMQDiagnostics`, `RabbitMQMetrics`, `RabbitMQStructuredLogger`, `BaggagePropagation`. **5** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Observability/*.cs` (4+ arquivos)
- https://www.rabbitmq.com/monitoring.html
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Observability/ObservabilityTest.cs`

[x] 5.14 - Testes para `Hosted/*`
- Criados smoke tests para `MvpRabbitMQHostedService` (Start/Stop, null guard, options mapping). **4** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Hosted/*.cs` (1+ arquivo)
- https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Hosted/HostedTest.cs`

[x] 5.15 - Testes para `Configuration/*`
- Criados testes para `BatchConsumerOptions.Validate`, presets, `RabbitMQClientOptions`, `MessageDeduplicationOptions`, `RequestClientOptions`, `MessageSchedulerOptions`, `PublisherConfirmOptions`, etc. **12** testes aprovados.
- `src/Mvp24Hours.Infrastructure.RabbitMQ/Configuration/*.cs` (20+ arquivos)
- https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options
- `src/Tests/Mvp24Hours.Application.RabbitMQ.Test/Configuration/ConfigurationTest.cs`

> **Resultado Fase 5:** **122 aprovados · 0 falhas · 0 ignorados** (116 unitários + 6 integração Testcontainers existentes). Infraestrutura: `GlobalUsings.cs`, `Support/RabbitMQTestHelpers.cs`, `Support/TestMessages.cs`, csproj atualizado (FluentAssertions, Moq, DI/Logging/Options).

---

## FASE 6 — Expandir Testes de `Mvp24Hours.Application`

> **Objetivo:** O projeto Application (~98 arquivos) tem testes parciais. Expandir cobertura para classes de serviço e resiliência.

[ ] 6.1 - Testes para `Logic/Async/*`
- Criar testes para `QueryServiceBaseAsync`, `CommandServiceBaseAsync`, `RepositoryServiceAsync`, `RepositoryPagingServiceAsync`, `BulkCommandServiceWithDtoBaseAsync`, `BulkCommandServiceWithSeparateDtosBaseAsync`.
- `src/Mvp24Hours.Application/Logic/Async/*.cs` (7 arquivos)
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/
- `src/Tests/Mvp24Hours.Application.Test/Logic/Async/*Test.cs`

[ ] 6.2 - Testes para `Logic/Cache/*`
- Criar testes para `CacheableApplicationServiceBaseAsync`, `CacheableQueryServiceBaseAsync`, `QueryCacheProvider`, `QueryCacheKeyGenerator`, `CacheInvalidator`.
- `src/Mvp24Hours.Application/Logic/Cache/*.cs` (5 arquivos)
- https://learn.microsoft.com/aspnet/core/performance/caching/
- `src/Tests/Mvp24Hours.Application.Test/Logic/Cache/*Test.cs`

[ ] 6.3 - Testes para `Logic/Events/*`
- Criar testes para `ApplicationEventDispatcher`, `EventAwareCommandServiceBaseAsync`, `MediatorApplicationEventAdapter`, `InMemoryApplicationEventOutbox`.
- `src/Mvp24Hours.Application/Logic/Events/*.cs` (4 arquivos)
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation
- `src/Tests/Mvp24Hours.Application.Test/Logic/Events/*Test.cs`

[ ] 6.4 - Testes para `Logic/Validation/*`
- Criar testes para `ValidationPipeline`, `ValidationService`, `DataAnnotationValidationStep`, `CustomValidationStep`.
- `src/Mvp24Hours.Application/Logic/Validation/*.cs` (4+ arquivos)
- https://learn.microsoft.com/aspnet/core/mvc/models/validation
- `src/Tests/Mvp24Hours.Application.Test/Logic/Validation/*Test.cs`

[ ] 6.5 - Testes para `Logic/Observability/*`
- Criar testes para `ApplicationActivitySource`, `CorrelationIdAccessor`, `InMemoryApplicationAuditStore`, `ApplicationOperationMetrics`.
- `src/Mvp24Hours.Application/Logic/Observability/*.cs` (4 arquivos)
- https://learn.microsoft.com/dotnet/core/diagnostics/
- `src/Tests/Mvp24Hours.Application.Test/Logic/Observability/*Test.cs`

[ ] 6.6 - Testes para `Logic/Resilience/*`
- Criar/expandir testes para `ResultMessage`, `DefaultErrorMessageLocalizer`.
- `src/Mvp24Hours.Application/Logic/Resilience/*.cs` (4 arquivos)
- https://learn.microsoft.com/dotnet/core/resilience/
- `src/Tests/Mvp24Hours.Application.Test/Logic/Resilience/*Test.cs`

[ ] 6.7 - Testes para `Specifications/*`
- Criar/expandir testes para `SpecificationCombinators`.
- `src/Mvp24Hours.Application/Specifications/*.cs` (1 arquivo)
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core
- `src/Tests/Mvp24Hours.Application.Test/Specifications/*Test.cs`

---

## FASE 7 — Expandir Testes de `Mvp24Hours.Infrastructure.Pipe`

> **Objetivo:** O projeto Pipe (~104 arquivos) tem testes parciais. Expandir cobertura para operações avançadas.

[ ] 7.1 - Testes para `Typed/*`
- Criar testes para `TypedPipeline`, `TypedPipelineAsync`, `TypedOperationBase`, `TypedOperationBaseAsync`.
- `src/Mvp24Hours.Infrastructure.Pipe/Typed/*.cs` (4 arquivos)
- Referência: `src/Tests/Mvp24Hours.Application.Pipe.Test/PipelineTest.cs`
- `src/Tests/Mvp24Hours.Application.Pipe.Test/Typed/*Test.cs`

[ ] 7.2 - Testes para `AdvancedFlow/DependencyGraph/*`
- Criar testes para `DependencyGraphExecutor`, `DependencyGraphNode`.
- `src/Mvp24Hours.Infrastructure.Pipe/AdvancedFlow/DependencyGraph/*.cs` (2 arquivos)
- https://en.wikipedia.org/wiki/Directed_acyclic_graph
- `src/Tests/Mvp24Hours.Application.Pipe.Test/AdvancedFlow/DependencyGraph/*Test.cs`

[ ] 7.3 - Testes para `AdvancedFlow/Saga/*`
- Criar testes para `PipelineSagaOrchestrator`.
- `src/Mvp24Hours.Infrastructure.Pipe/AdvancedFlow/Saga/*.cs` (2 arquivos)
- https://microservices.io/patterns/data/saga.html
- `src/Tests/Mvp24Hours.Application.Pipe.Test/AdvancedFlow/Saga/*Test.cs`

[ ] 7.4 - Testes para `AdvancedFlow/Checkpoint/*`
- Criar testes para `InMemoryCheckpointStore`.
- `src/Mvp24Hours.Infrastructure.Pipe/AdvancedFlow/Checkpoint/*.cs` (2 arquivos)
- https://learn.microsoft.com/azure/architecture/patterns/compensating-transaction
- `src/Tests/Mvp24Hours.Application.Pipe.Test/AdvancedFlow/Checkpoint/*Test.cs`

[ ] 7.5 - Testes para `AdvancedFlow/Priority/*`
- Criar testes para `PriorityPipeline`, `OperationPriority`.
- `src/Mvp24Hours.Infrastructure.Pipe/AdvancedFlow/Priority/*.cs` (2 arquivos)
- https://learn.microsoft.com/dotnet/api/system.collections.generic.priorityqueue-2
- `src/Tests/Mvp24Hours.Application.Pipe.Test/AdvancedFlow/Priority/*Test.cs`

[ ] 7.6 - Testes para `Resiliency/*`
- Criar testes para `DeadLetterPipelineMiddleware`, `BulkheadPipelineMiddleware`, `RateLimitingPipelineMiddleware`, `InMemoryDeadLetterStore`.
- `src/Mvp24Hours.Infrastructure.Pipe/Resiliency/*.cs` (8+ arquivos)
- https://learn.microsoft.com/dotnet/core/resilience/
- `src/Tests/Mvp24Hours.Application.Pipe.Test/Resiliency/*Test.cs`

[ ] 7.7 - Testes para `Middleware/*`
- Criar testes para `TimeoutPipelineMiddleware`, `PipelineMiddlewareExecutor`.
- `src/Mvp24Hours.Infrastructure.Pipe/Middleware/*.cs` (2 arquivos)
- https://learn.microsoft.com/aspnet/core/fundamentals/middleware/
- `src/Tests/Mvp24Hours.Application.Pipe.Test/Middleware/*Test.cs`

[ ] 7.8 - Testes para `Observability/*`
- Criar testes para `PipelineMetrics`, `PipelineHealthCheck`, `PipelineVisualizer`.
- `src/Mvp24Hours.Infrastructure.Pipe/Observability/*.cs` (5+ arquivos)
- https://learn.microsoft.com/dotnet/core/diagnostics/
- `src/Tests/Mvp24Hours.Application.Pipe.Test/Observability/*Test.cs`

[ ] 7.9 - Testes para `Builders/*`
- Criar testes para `ParallelOperationBuilder`.
- `src/Mvp24Hours.Infrastructure.Pipe/Builders/*.cs` (1+ arquivo)
- Referência: `src/Tests/Mvp24Hours.Application.Pipe.Test/PipelineTest.cs`
- `src/Tests/Mvp24Hours.Application.Pipe.Test/Builders/*Test.cs`

[ ] 7.10 - Testes para `ExceptionMapping/*`
- Criar testes para `DefaultPipelineExceptionMapper`.
- `src/Mvp24Hours.Infrastructure.Pipe/ExceptionMapping/*.cs` (1 arquivo)
- https://learn.microsoft.com/aspnet/core/web-api/handle-errors
- `src/Tests/Mvp24Hours.Application.Pipe.Test/ExceptionMapping/*Test.cs`

[ ] 7.11 - Testes para `Validation/*`
- Criar testes para `DefaultPipelineValidator`.
- `src/Mvp24Hours.Infrastructure.Pipe/Validation/*.cs` (1 arquivo)
- https://fluentvalidation.net/
- `src/Tests/Mvp24Hours.Application.Pipe.Test/Validation/*Test.cs`

---

## FASE 8 — Expandir Testes de `Mvp24Hours.Infrastructure.Caching`

> **Objetivo:** O projeto Caching (~59 arquivos) tem apenas 38 testes. Precisa de cobertura mais ampla.

[ ] 8.1 - Testes para `Providers/*`
- Criar/expandir testes para `MemoryCacheProvider`, `DistributedCacheProvider`, `MultiLevelCache`.
- `src/Mvp24Hours.Infrastructure.Caching/Providers/*.cs` (3 arquivos)
- https://learn.microsoft.com/aspnet/core/performance/caching/
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Providers/*Test.cs`

[ ] 8.2 - Testes para `Patterns/*`
- Criar testes para `ReadThroughCache`, `WriteThroughCache`, `WriteBehindCache`, `RefreshAheadCache`, `CachePatternExtensions`, `CacheAsideExtensions`.
- `src/Mvp24Hours.Infrastructure.Caching/Patterns/*.cs` (7 arquivos)
- https://learn.microsoft.com/azure/architecture/patterns/cache-aside
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Patterns/*Test.cs`

[ ] 8.3 - Testes para `Serializers/*`
- Criar testes para `JsonCacheSerializer`, `CompressedCacheSerializer`, `MessagePackCacheSerializer`.
- `src/Mvp24Hours.Infrastructure.Caching/Serializers/*.cs` (3 arquivos)
- https://learn.microsoft.com/dotnet/standard/serialization/
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Serializers/*Test.cs`

[ ] 8.4 - Testes para `Invalidation/*`
- Criar testes para `CacheDependencyManager`, `CacheTagManager`, `CacheStampedePrevention`, `RedisCacheInvalidationEventPublisher`, `InMemoryCacheInvalidationEventPublisher`.
- `src/Mvp24Hours.Infrastructure.Caching/Invalidation/*.cs` (5 arquivos)
- https://learn.microsoft.com/azure/architecture/best-practices/caching
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Invalidation/*Test.cs`

[ ] 8.5 - Testes para `Warming/*`
- Criar testes para `CacheWarmer`, `CacheWarmupHostedService`.
- `src/Mvp24Hours.Infrastructure.Caching/Warming/*.cs` (2 arquivos)
- https://learn.microsoft.com/azure/architecture/best-practices/caching
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Warming/*Test.cs`

[ ] 8.6 - Testes para `Prefetching/*`
- Criar testes para `CachePrefetcher`.
- `src/Mvp24Hours.Infrastructure.Caching/Prefetching/*.cs` (1 arquivo)
- https://learn.microsoft.com/azure/architecture/best-practices/caching
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Prefetching/*Test.cs`

[ ] 8.7 - Testes para `Resilience/*`
- Criar testes para `ResilientCacheProvider`, `CacheResilienceOptions`.
- `src/Mvp24Hours.Infrastructure.Caching/Resilience/*.cs` (3 arquivos)
- https://learn.microsoft.com/dotnet/core/resilience/
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Resilience/*Test.cs`

[ ] 8.8 - Testes para `Compression/*`
- Criar testes para `CacheCompressor`.
- `src/Mvp24Hours.Infrastructure.Caching/Compression/*.cs` (1 arquivo)
- https://learn.microsoft.com/dotnet/api/system.io.compression
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Compression/*Test.cs`

[ ] 8.9 - Testes para `Synchronization/*`
- Criar testes para `InMemoryCacheSynchronizer`.
- `src/Mvp24Hours.Infrastructure.Caching/Synchronization/*.cs` (1 arquivo)
- https://learn.microsoft.com/azure/architecture/best-practices/caching
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Synchronization/*Test.cs`

[ ] 8.10 - Testes para `Repository/*`
- Criar testes para `CacheableRepository`.
- `src/Mvp24Hours.Infrastructure.Caching/Repository/*.cs` (1 arquivo)
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Repository/*Test.cs`

[ ] 8.11 - Testes para `EFCore/*`
- Criar testes para `EfCoreCacheInterceptor`.
- `src/Mvp24Hours.Infrastructure.Caching/EFCore/*.cs` (1 arquivo)
- https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/EFCore/*Test.cs`

[ ] 8.12 - Testes para `Observability/*`
- Criar testes para `CacheHealthCheck`, `CacheMetrics`, `CacheActivitySource`, `ObservableCacheProvider`.
- `src/Mvp24Hours.Infrastructure.Caching/Observability/*.cs` (4 arquivos)
- https://learn.microsoft.com/dotnet/core/diagnostics/
- `src/Tests/Mvp24Hours.Infrastructure.Caching.Test/Observability/*Test.cs`

---

## FASE 9 — Expandir Testes de `Mvp24Hours.Infrastructure.Cqrs`

> **Objetivo:** O projeto CQRS tem boa cobertura (28%) mas muitas classes têm 0%. Preencher os gaps.

[ ] 9.1 - Testes para classes com 0% de cobertura
- Criar testes para todas as classes listadas no relatório de cobertura com 0%: `ParallelNotificationPublisher`, `ParallelNoWaitNotificationPublisher`, `PipelineHookBase`, `SequentialContinueOnExceptionPublisher`, `SequentialNotificationPublisher`, `AuthorizationBehavior`, `CacheInvalidationBehavior`, `CachingBehavior`, `RetryBehavior`, `NativeResilienceBehavior`, `IdempotencyBehavior`, etc.
- Ver relatório de cobertura: `test-results/coverage-report/Summary.txt`
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/**/*Test.cs`

[ ] 9.2 - Testes para `Projections/*`
- Expandir testes para `ProjectionHostedService`, `ProjectionManager`, `ProjectionRebuildService`, `AggregatingProjectionHandler`, `IncrementalProjection`, `BatchProjection`, `ReadModelProjectionHandler`.
- `src/Mvp24Hours.Infrastructure.Cqrs/Projections/*.cs` (15+ arquivos)
- https://learn.microsoft.com/azure/architecture/patterns/cqrs
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/Projections/*Test.cs`

[ ] 9.3 - Testes para `EventSourcing/*`
- Expandir testes para `SnapshotAggregateRoot`, `CompositeSnapshotStrategy`, `DefaultEventTypeResolver`, `EventSourcingExtensions`, `EventSourcingOptions`.
- `src/Mvp24Hours.Infrastructure.Cqrs/EventSourcing/*.cs` (20+ arquivos)
- https://learn.microsoft.com/azure/architecture/patterns/event-sourcing
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/EventSourcing/*Test.cs`

[ ] 9.4 - Testes para `Messaging/*`
- Criar testes para `InboxCleanupService`, `OutboxCleanupService`, `OutboxProcessor`, `RabbitMQOutboxAdapter`.
- `src/Mvp24Hours.Infrastructure.Cqrs/Messaging/*.cs` (8 arquivos)
- https://microservices.io/patterns/data/transactional-outbox.html
- `src/Tests/Mvp24Hours.Infrastructure.Cqrs.Test/Messaging/*Test.cs`

---

## FASE 10 — Expandir Testes de `Mvp24Hours.Infrastructure.CronJob`

> **Objetivo:** O projeto CronJob tem cobertura de 28.9%. Preencher os gaps.

[ ] 10.1 - Testes para classes com 0% de cobertura
- Criar testes para: `CronJobConfigurationExtensions`, `CronJobGlobalOptions`, `CronJobContext`, `CronJobContextAccessor`, `CronJobController`, `CronJobDependency`, `CronJobDependencyBuilder`, `InMemoryCronJobDependencyTracker`, `CronJobEventDispatcher`, `CronExpressionParser`, `AdvancedCronJobService`, `InMemoryCronJobStateStore`.
- Ver relatório de cobertura: `test-results/coverage-report/Summary.txt`
- https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services
- `src/Tests/Mvp24Hours.Infrastructure.CronJob.Test/**/*Test.cs`

[ ] 10.2 - Testes para `Extensions/*`
- Criar testes para `ScheduledServiceExtensions`, `CronJobAdvancedExtensions`.
- `src/Mvp24Hours.Infrastructure.CronJob/Extensions/*.cs` (2 arquivos)
- https://learn.microsoft.com/dotnet/core/extensions/dependency-injection
- `src/Tests/Mvp24Hours.Infrastructure.CronJob.Test/Extensions/*Test.cs`

[ ] 10.3 - Testes para `Scheduling/*`
- Criar testes para `CronExpressionParser`.
- `src/Mvp24Hours.Infrastructure.CronJob/Scheduling/*.cs` (2 arquivos)
- https://en.wikipedia.org/wiki/Cron
- `src/Tests/Mvp24Hours.Infrastructure.CronJob.Test/Scheduling/*Test.cs`

---

## FASE 11 — Expandir Testes de `Mvp24Hours.Infrastructure.Data.MongoDb`

> **Objetivo:** O projeto MongoDb tem testes parciais. Expandir cobertura.

[ ] 11.1 - Testes para `Advanced/*`
- Criar testes para `MongoDbTextSearchService`, `MongoDbGeospatialService`, `MongoDbShardingService`, `MongoDbGridFsService`, `MongoDbCappedCollectionService`, `MongoDbChangeStreamService`, `MongoDbSchemaValidationService`.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Advanced/**/*.cs` (15+ arquivos)
- https://www.mongodb.com/docs/manual/
- `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Advanced/*Test.cs`

[ ] 11.2 - Testes para `Performance/*`
- Criar testes para `MongoDbIndexManager`, `MongoDbQueryProfiler`, `MongoDbProjection`, `MongoDbConnectionPoolOptions`.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Performance/**/*.cs` (6 arquivos)
- https://www.mongodb.com/docs/manual/indexes/
- `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Performance/*Test.cs`

[ ] 11.3 - Testes para `Security/*`
- Criar testes para `FieldEncryption`, `MongoDbAuthenticationOptions`.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Security/*.cs` (2 arquivos)
- https://www.mongodb.com/docs/manual/core/security-encryption-at-rest/
- `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Security/*Test.cs`

[ ] 11.4 - Testes para `Interceptors/*`
- Criar testes para `MongoDbInterceptorPipeline`, `CommandLogger`, `TenantInterceptor`.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Interceptors/*.cs` (5 arquivos)
- https://www.mongodb.com/docs/manual/reference/command/
- `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Interceptors/*Test.cs`

[ ] 11.5 - Testes para `Observability/*`
- Criar testes para `MongoDbMetrics`, `MongoDbOpenTelemetryInstrumentation`, `MongoDbSlowQueryLogger`, `MongoDbDurationTracker`, `MongoDbStructuredLogger`.
- `src/Mvp24Hours.Infrastructure.Data.MongoDb/Observability/*.cs` (6 arquivos)
- https://learn.microsoft.com/dotnet/core/diagnostics/
- `src/Tests/Mvp24Hours.Infrastructure.Data.MongoDb.Test/Observability/*Test.cs`

---

## FASE 12 — Expandir Testes de `Mvp24Hours.Core`

> **Objetivo:** Garantir >95% de cobertura no projeto Core, que é a base da solução.

[ ] 12.1 - Revisar cobertura atual do Core
- Analisar relatório de cobertura específico para `Mvp24Hours.Core` e identificar classes/métodos não cobertos.
- `test-results/coverage-report/` (relatório HTML)
- https://github.com/coverlet-coverage/coverlet
- `src/Tests/Mvp24Hours.Core.Test/**/*Test.cs`

[ ] 12.2 - Testes para `Contract/**/*`
- Criar testes para interfaces e contratos que possuem implementações default ou métodos de extensão.
- `src/Mvp24Hours.Core/Contract/**/*.cs` (50+ arquivos)
- https://learn.microsoft.com/dotnet/csharp/fundamentals/types/interfaces
- `src/Tests/Mvp24Hours.Core.Test/Contract/*Test.cs`

[ ] 12.3 - Testes para `Domain/**/*`
- Expandir testes para `Specification`, `CompositeSpecifications`, `Enumeration`, `EntityBase`, `AuditableEntity`.
- `src/Mvp24Hours.Core/Domain/**/*.cs` (10+ arquivos)
- https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/
- `src/Tests/Mvp24Hours.Core.Test/Domain/*Test.cs`

[ ] 12.4 - Testes para `Infrastructure/**/*`
- Expandir testes para `ChannelFactory`, `NativeRateLimiterProvider`, geradores de GUID.
- `src/Mvp24Hours.Core/Infrastructure/**/*.cs` (10+ arquivos)
- https://learn.microsoft.com/dotnet/standard/parallel-programming/dataflow-task-parallel-library
- `src/Tests/Mvp24Hours.Core.Test/Infrastructure/*Test.cs`

[ ] 12.5 - Testes para `Serialization/**/*`
- Criar testes para `JsonCoreSerializerContext`, `PropertyAndFieldsSerializerResolver`, `CompositeContractResolver`, `ValueObjectConverter`.
- `src/Mvp24Hours.Core/Serialization/**/*.cs` (6 arquivos)
- https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/
- `src/Tests/Mvp24Hours.Core.Test/Serialization/*Test.cs`

[ ] 12.6 - Testes para `Exceptions/*`
- Criar testes para `NotFoundException`, `UnauthorizedException`, e outras exceções customizadas.
- `src/Mvp24Hours.Core/Exceptions/*.cs` (2+ arquivos)
- https://learn.microsoft.com/dotnet/standard/exceptions/
- `src/Tests/Mvp24Hours.Core.Test/Exceptions/*Test.cs`

---

## FASE 13 — Validação Final e Gate de Cobertura

> **Objetivo:** Validar que a meta de >95% foi atingida.

[ ] 13.1 - Gerar relatório de cobertura final
- Executar `dotnet test` com coleta de cobertura completa e gerar relatório consolidado.
- `dotnet test src/Mvp24Hours.sln --collect:"XPlat Code Coverage" --results-directory ./test-results`
- https://github.com/coverlet-coverage/coverlet
- `tasks/coverage-final-tests.json`, `tasks/coverage-final-report.html`

[ ] 13.2 - Comparar com baseline
- Comparar cobertura final com o baseline da Fase 1 e documentar o delta por projeto.
- `tasks/coverage-baseline-tests.json` (Fase 1), `tasks/coverage-final-tests.json` (Fase 13)
- https://github.com/danielpalme/ReportGenerator
- `tasks/coverage-delta-tests.md`

[ ] 13.3 - Configurar gate de cobertura no CI
- Adicionar step no workflow de CI para falhar o build se a cobertura cair abaixo de 95%.
- `.github/workflows/ci.yml`
- https://learn.microsoft.com/azure/devops/pipelines/test/codecoverage-for-pullrequests
- `.github/workflows/ci.yml` (step de coverage gate)

[ ] 13.4 - Documentar no CHANGELOG
- Adicionar entrada no CHANGELOG documentando a expansão de testes e a meta de cobertura atingida.
- `CHANGELOG.md`
- https://keepachangelog.com/
- `CHANGELOG.md`

---

## Resumo de Testes a Criar

| Fase | Projeto | Testes Existentes | Estimativa Novos Testes |
|------|---------|-------------------|------------------------|
| 2 | Mvp24Hours.Infrastructure | ~1198 | ~200 (concluída) |
| 3 | Mvp24Hours.Infrastructure.Data.EFCore | ~177 | ~177 (concluída) |
| 4 | Mvp24Hours.WebAPI | ~107 | ~107 (concluída) |
| 5 | Mvp24Hours.Infrastructure.RabbitMQ | ~122 | ~122 (concluída) |
| 6 | Mvp24Hours.Application | ~264 | ~50 |
| 7 | Mvp24Hours.Infrastructure.Pipe | ~78 | ~60 |
| 8 | Mvp24Hours.Infrastructure.Caching | ~38 | ~80 |
| 9 | Mvp24Hours.Infrastructure.Cqrs | ~347 | ~100 |
| 10 | Mvp24Hours.Infrastructure.CronJob | ~91 | ~50 |
| 11 | Mvp24Hours.Infrastructure.Data.MongoDb | ~129 | ~80 |
| 12 | Mvp24Hours.Core | ~788 | ~50 |
| **Total** | | **~1746** | **~1070** |

**Meta:** ~2816 testes para atingir >95% de cobertura.
