# Classes de Produção Sem Cobertura / Sem Testes Correspondentes

> Gerado em 2026-07-18 23:39 UTC a partir do baseline `tasks/coverage-baseline-tests.json` (task 1.3) e varredura de tipos públicos nos projetos `src/Mvp24Hours.*`.
>
> **Critério "sem cobertura":** (a) assembly sem dados Coverlet no baseline, ou (b) tipo com `lineCoverage = 0` no Cobertura, ou (c) tipo público ausente do relatório Coverlet.
>
> **Prioridade:** (1) lógica de negócio / implementações · (2) helpers/utilitários · (3) extensions · (4) interfaces/contratos/options tipicamente sem lógica.
>
> **Hint de teste dedicado:** nome de classe/arquivo de teste correspondente (`Foo` ↔ `FooTest`/`FooTests`) encontrado em `src/Tests/**` — não garante cobertura Coverlet (ex.: projetos sem instrumentação no merge).

## Resumo executivo

| Métrica | Valor |
|---------|------:|
| Tipos públicos escaneados | 2525 |
| Sem cobertura (este inventário) | 2393 |
| Com alguma cobertura Coverlet | 132 |
| Prioridade 1 — Business / lógica | 1546 |
| Prioridade 2 — Helpers / utils | 40 |
| Prioridade 3 — Extensions | 217 |
| Prioridade 4 — Contratos / interfaces | 590 |

### Assemblies com dados Coverlet (baseline 28.3%)

| Assembly | Cobertura linha | Tipos públicos sem cobertura |
|----------|----------------:|-----------------------------:|
| `Mvp24Hours.Infrastructure.Caching.Redis` | 40.9% | 0 |
| `Mvp24Hours.Infrastructure.Cqrs` | 28.1% | 183 |
| `Mvp24Hours.Infrastructure.CronJob` | 29% | 55 |

### Assemblies SEM dados Coverlet no baseline

Estes projetos aparecem com 100% dos tipos públicos neste inventário até a instrumentação Coverlet ser corrigida/incluída no merge:

| Projeto | Tipos públicos | P1 | P2 | P3 | P4 | Projeto de teste dedicado? |
|---------|---------------:|---:|---:|---:|---:|:--------------------------:|
| `Mvp24Hours.Core` | 513 | 290 | 7 | 49 | 167 | sim* |
| `Mvp24Hours.Application` | 167 | 115 | 1 | 14 | 37 | sim* |
| `Mvp24Hours.Infrastructure` | 351 | 216 | 16 | 34 | 85 | **não** |
| `Mvp24Hours.Infrastructure.Caching` | 75 | 56 | 0 | 17 | 2 | sim* |
| `Mvp24Hours.Infrastructure.Data.EFCore` | 141 | 100 | 2 | 25 | 14 | sim* |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | 205 | 152 | 3 | 24 | 26 | sim* |
| `Mvp24Hours.Infrastructure.Pipe` | 210 | 170 | 2 | 15 | 23 | sim* |
| `Mvp24Hours.Infrastructure.RabbitMQ` | 298 | 183 | 9 | 18 | 88 | sim* |
| `Mvp24Hours.WebAPI` | 195 | 169 | 0 | 12 | 14 | sim* |

* Pode haver cobertura via projeto de teste indireto (ex. Application.SQLServer.Test para EFCore, Application.Pipe.Test para Pipe), mas sem linhas no merge Coverlet do baseline.

### Projetos de produção sem projeto de teste dedicado

- `Mvp24Hours.Infrastructure` — prioridade máxima (FASE 2)
- `Mvp24Hours.Infrastructure.Data.EFCore` — coberto indiretamente por Application.*Sql.Test (FASE 3)

---

## Dashboard por projeto

| Projeto | Públicos | Sem cobertura | Com cobertura | Cov. dados | P1 | P2 | P3 | P4 |
|---------|---------:|--------------:|--------------:|:----------:|---:|---:|---:|---:|
| `Mvp24Hours.Core` | 513 | 513 | 0 | não | 290 | 7 | 49 | 167 |
| `Mvp24Hours.Application` | 167 | 167 | 0 | não | 115 | 1 | 14 | 37 |
| `Mvp24Hours.Infrastructure` | 351 | 351 | 0 | não | 216 | 16 | 34 | 85 |
| `Mvp24Hours.Infrastructure.Caching` | 75 | 75 | 0 | não | 56 | 0 | 17 | 2 |
| `Mvp24Hours.Infrastructure.Caching.Redis` | 1 | 0 | 1 | sim | 0 | 0 | 0 | 0 |
| `Mvp24Hours.Infrastructure.Cqrs` | 301 | 183 | 118 | sim | 67 | 0 | 5 | 111 |
| `Mvp24Hours.Infrastructure.CronJob` | 68 | 55 | 13 | sim | 28 | 0 | 4 | 23 |
| `Mvp24Hours.Infrastructure.Data.EFCore` | 141 | 141 | 0 | não | 100 | 2 | 25 | 14 |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | 205 | 205 | 0 | não | 152 | 3 | 24 | 26 |
| `Mvp24Hours.Infrastructure.Pipe` | 210 | 210 | 0 | não | 170 | 2 | 15 | 23 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | 298 | 298 | 0 | não | 183 | 9 | 18 | 88 |
| `Mvp24Hours.WebAPI` | 195 | 195 | 0 | não | 169 | 0 | 12 | 14 |

---

## Prioridade 1 — Lógica de negócio / implementações

Tipos concretos (`class`/`record`/`struct`/`enum` de domínio) sem cobertura Coverlet. Ordenados por projeto e pasta.

### `Mvp24Hours.Application` (115)

#### Contract/ (39)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ApplicationAuditEntry` | class | `Contract\Observability\IAuditableOperation.cs` | — |  |
| `ApplicationEventBase` | record | `Contract\Events\IApplicationEvent.cs` | — |  |
| `ApplicationEventDispatcherOptions` | class | `Contract\Events\IApplicationEventDispatcher.cs` | — |  |
| `ApplicationEventOutboxEntry` | class | `Contract\Events\IApplicationEventOutbox.cs` | — |  |
| `ApplicationEventOutboxStatus` | enum | `Contract\Events\IApplicationEventOutbox.cs` | — |  |
| `Auth` | class | `Contract\Resilience\ErrorCodes.cs` | — |  |
| `CacheableAttribute` | class | `Contract\Cache\CacheableAttribute.cs` | — |  |
| `CacheInvalidateAttribute` | class | `Contract\Cache\CacheInvalidateAttribute.cs` | — |  |
| `CacheInvalidationTiming` | enum | `Contract\Cache\CacheInvalidateAttribute.cs` | — |  |
| `Domain` | class | `Contract\Resilience\ErrorCodes.cs` | — |  |
| `EntitiesCreatedEvent` | record | `Contract\Events\EntityEvents.cs` | — |  |
| `EntitiesDeletedEvent` | record | `Contract\Events\EntityEvents.cs` | — |  |
| `EntitiesUpdatedEvent` | record | `Contract\Events\EntityEvents.cs` | — |  |
| `EntityCreatedEvent` | record | `Contract\Events\EntityEvents.cs` | — |  |
| `EntityDeletedEvent` | record | `Contract\Events\EntityEvents.cs` | — |  |
| `EntityOperationType` | enum | `Contract\Events\IApplicationEvent.cs` | — |  |
| `EntityUpdatedEvent` | record | `Contract\Events\EntityEvents.cs` | — |  |
| `ErrorCodes` | class | `Contract\Resilience\ErrorCodes.cs` | — |  |
| `EventDispatchStrategy` | enum | `Contract\Events\IApplicationEventDispatcher.cs` | — |  |
| `Exception` | class | `Contract\Resilience\ErrorCodes.cs` | — |  |
| `ExceptionMapping` | class | `Contract\Resilience\ExceptionMappingOptions.cs` | — |  |
| `ExceptionMappingOptions` | class | `Contract\Resilience\ExceptionMappingOptions.cs` | — |  |
| `MessageSeverity` | enum | `Contract\Resilience\MessageSeverity.cs` | — |  |
| `NestedValidationError` | class | `Contract\Validation\ICascadeValidator.cs` | — |  |
| `Operation` | class | `Contract\Resilience\ErrorCodes.cs` | — | sim |
| `OperationMetricsOptions` | class | `Contract\Observability\IOperationMetrics.cs` | — |  |
| `QueryCacheEntryOptions` | class | `Contract\Cache\QueryCacheEntryOptions.cs` | — |  |
| `Resource` | class | `Contract\Resilience\ErrorCodes.cs` | — |  |
| `ResultStatusCode` | enum | `Contract\Resilience\ResultStatusCode.cs` | — |  |
| `System` | class | `Contract\Resilience\ErrorCodes.cs` | — |  |
| `TransactionalAttribute` | class | `Contract\Transaction\TransactionalAttribute.cs` | — |  |
| `TransactionException` | class | `Contract\Transaction\TransactionException.cs` | — |  |
| `TransactionIsolationLevel` | enum | `Contract\Transaction\TransactionalAttribute.cs` | — |  |
| `TransactionStatus` | enum | `Contract\Transaction\ITransactionScope.cs` | — |  |
| `ValidateNestedAttribute` | class | `Contract\Validation\ICascadeValidator.cs` | — |  |
| `Validation` | class | `Contract\Resilience\ErrorCodes.cs` | — |  |
| `ValidationOptions` | class | `Contract\Validation\IValidationService.cs` | — |  |
| `ValidationServiceResult` | class | `Contract\Validation\IValidationService.cs` | — |  |
| `ValidationStepContext` | class | `Contract\Validation\IValidationPipeline.cs` | — |  |

#### Extensions/ (6)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ApplicationModuleOptions` | class | `Extensions\ApplicationModuleServiceCollectionExtensions.cs` | — |  |
| `ApplicationObservabilityOptions` | class | `Extensions\ObservabilityServiceCollectionExtensions.cs` | — |  |
| `ConventionServiceRegistration` | record | `Extensions\ConventionBasedServiceCollectionExtensions.cs` | — |  |
| `CursorNavigationInfo` | class | `Extensions\PagedResultExtensions.cs` | — |  |
| `PaginationOptions` | class | `Extensions\PaginationServiceCollectionExtensions.cs` | — |  |
| `TransactionScopeOptions` | class | `Extensions\TransactionServiceCollectionExtensions.cs` | — |  |

#### Logic/ (69)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityNames` | class | `Logic\Observability\ApplicationActivitySource.cs` | — |  |
| `AmbientTransactionContext` | class | `Logic\Transaction\AmbientTransactionContext.cs` | — |  |
| `ApplicationActivitySource` | class | `Logic\Observability\ApplicationActivitySource.cs` | — |  |
| `ApplicationEventDispatcher` | class | `Logic\Events\ApplicationEventDispatcher.cs` | — |  |
| `ApplicationEventMediatorNotification` | class | `Logic\Events\MediatorApplicationEventAdapter.cs` | — |  |
| `ApplicationEventNotification` | class | `Logic\Events\ApplicationEventDispatcher.cs` | — |  |
| `ApplicationEventOutboxProcessor` | class | `Logic\Events\ApplicationEventOutboxProcessor.cs` | — |  |
| `ApplicationEventOutboxProcessorOptions` | class | `Logic\Events\ApplicationEventOutboxProcessor.cs` | — |  |
| `ApplicationOperationMetrics` | class | `Logic\Observability\ApplicationOperationMetrics.cs` | — |  |
| `ApplicationServiceBase` | class | `Logic\ApplicationServiceBase.cs` | — |  |
| `ApplicationServiceBaseAsync` | class | `Logic\Async\ApplicationServiceBaseAsync.cs` | — |  |
| `ApplicationServiceBaseWithDto` | class | `Logic\ApplicationServiceBaseWithDto.cs` | — |  |
| `ApplicationServiceBaseWithDtoAsync` | class | `Logic\Async\ApplicationServiceBaseWithDtoAsync.cs` | — |  |
| `ApplicationServiceBaseWithSeparateDtos` | class | `Logic\ApplicationServiceBaseWithSeparateDtos.cs` | — |  |
| `ApplicationServiceBaseWithSeparateDtosAsync` | class | `Logic\Async\ApplicationServiceBaseWithSeparateDtosAsync.cs` | — |  |
| `BulkCommandServiceBaseAsync` | class | `Logic\Async\BulkCommandServiceBaseAsync.cs` | — |  |
| `BulkCommandServiceWithDtoBaseAsync` | class | `Logic\Async\BulkCommandServiceWithDtoBaseAsync.cs` | — |  |
| `BulkCommandServiceWithSeparateDtosBaseAsync` | class | `Logic\Async\BulkCommandServiceWithSeparateDtosBaseAsync.cs` | — |  |
| `BusinessResultWithStatus` | class | `Logic\Resilience\BusinessResultWithStatus.cs` | — | sim |
| `BusinessResultWithStatus` | class | `Logic\Resilience\BusinessResultWithStatus.cs` | — | sim |
| `CacheableApplicationServiceBaseAsync` | class | `Logic\Cache\CacheableApplicationServiceBaseAsync.cs` | — |  |
| `CacheableQueryServiceBaseAsync` | class | `Logic\Cache\CacheableQueryServiceBaseAsync.cs` | — |  |
| `CacheInvalidator` | class | `Logic\Cache\CacheInvalidator.cs` | — |  |
| `CascadeValidationStep` | class | `Logic\Validation\ValidationSteps\CascadeValidationStep.cs` | — |  |
| `CommandServiceBase` | class | `Logic\CommandServiceBase.cs` | — |  |
| `CommandServiceBaseAsync` | class | `Logic\Async\CommandServiceBaseAsync.cs` | — |  |
| `CompositeCursor` | class | `Logic\Pagination\CursorPagedResult.cs` | — |  |
| `CorrelationIdAccessor` | class | `Logic\Observability\CorrelationIdAccessor.cs` | — |  |
| `CorrelationIdContext` | class | `Logic\Observability\CorrelationIdAccessor.cs` | — |  |
| `CorrelationIdScope` | class | `Logic\Observability\CorrelationIdAccessor.cs` | — |  |
| `CursorPagedResult` | class | `Logic\Pagination\CursorPagedResult.cs` | — | sim |
| `CursorPagedResult` | class | `Logic\Pagination\CursorPagedResult.cs` | — | sim |
| `CustomValidationStep` | class | `Logic\Validation\ValidationSteps\CustomValidationStep.cs` | — |  |
| `DataAnnotationValidationStep` | class | `Logic\Validation\ValidationSteps\DataAnnotationValidationStep.cs` | — |  |
| `DefaultErrorMessageLocalizer` | class | `Logic\Resilience\DefaultErrorMessageLocalizer.cs` | — |  |
| `EventAwareCommandServiceBaseAsync` | class | `Logic\Events\EventAwareCommandServiceBaseAsync.cs` | — |  |
| `ExceptionToResultMapper` | class | `Logic\Resilience\ExceptionToResultMapper.cs` | — | sim |
| `FluentValidationStep` | class | `Logic\Validation\ValidationSteps\FluentValidationStep.cs` | — |  |
| `InMemoryApplicationAuditStore` | class | `Logic\Observability\InMemoryApplicationAuditStore.cs` | — |  |
| `InMemoryApplicationEventOutbox` | class | `Logic\Events\InMemoryApplicationEventOutbox.cs` | — |  |
| `MediatorApplicationEventAdapter` | class | `Logic\Events\MediatorApplicationEventAdapter.cs` | — |  |
| `MediatorApplicationEventAdapterFactory` | class | `Logic\Events\MediatorApplicationEventAdapter.cs` | — |  |
| `NullCheckValidationStep` | class | `Logic\Validation\ValidationSteps\NullCheckValidationStep.cs` | — |  |
| `NullOperationMetrics` | class | `Logic\Observability\ApplicationOperationMetrics.cs` | — |  |
| `ObservableApplicationServiceBaseAsync` | class | `Logic\Observability\ObservableApplicationServiceBaseAsync.cs` | — |  |
| `PagedBusinessResult` | class | `Logic\Pagination\PagedResult.cs` | — |  |
| `PagedResult` | class | `Logic\Pagination\PagedResult.cs` | — | sim |
| `PaginationMetadata` | class | `Logic\Pagination\PaginationHelper.cs` | — |  |
| `PredicateValidationStep` | class | `Logic\Validation\ValidationSteps\CustomValidationStep.cs` | — |  |
| `QueryCacheKeyGenerator` | class | `Logic\Cache\QueryCacheKeyGenerator.cs` | — |  |
| `QueryCacheOptions` | class | `Logic\Cache\QueryCacheProvider.cs` | — |  |
| `QueryCacheProvider` | class | `Logic\Cache\QueryCacheProvider.cs` | — |  |
| `QueryServiceBase` | class | `Logic\QueryServiceBase.cs` | — |  |
| `QueryServiceBaseAsync` | class | `Logic\Async\QueryServiceBaseAsync.cs` | — |  |
| `RepositoryPagingService` | class | `Logic\RepositoryPagingService.cs` | — |  |
| `RepositoryPagingServiceAsync` | class | `Logic\Async\RepositoryPagingServiceAsync.cs` | — |  |
| `RepositoryService` | class | `Logic\RepositoryService.cs` | — |  |
| `RepositoryServiceAsync` | class | `Logic\Async\RepositoryServiceAsync.cs` | — |  |
| `ResultMessage` | class | `Logic\Resilience\ResultMessage.cs` | — |  |
| `RuleBasedValidationStep` | class | `Logic\Validation\ValidationSteps\CustomValidationStep.cs` | — |  |
| `SafeExecutor` | class | `Logic\Resilience\SafeExecutor.cs` | — | sim |
| `TagNames` | class | `Logic\Observability\ApplicationActivitySource.cs` | — |  |
| `TransactionScope` | class | `Logic\Transaction\TransactionScope.cs` | — | sim |
| `TransactionScopeFactory` | class | `Logic\Transaction\TransactionScopeFactory.cs` | — |  |
| `TransactionScopeSync` | class | `Logic\Transaction\TransactionScope.cs` | — |  |
| `ValidationPipeline` | class | `Logic\Validation\ValidationPipeline.cs` | — |  |
| `ValidationPipelineBuilder` | class | `Logic\Validation\ValidationPipeline.cs` | — |  |
| `ValidationService` | class | `Logic\Validation\ValidationService.cs` | — |  |
| `ValidationServiceOptions` | class | `Logic\Validation\ValidationService.cs` | — |  |

#### Specifications/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `SpecificationCombinators` | class | `Specifications\SpecificationCombinators.cs` | — | sim |

### `Mvp24Hours.Core` (290)

#### Aspire/ (10)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AspireDatabaseOptions` | class | `Aspire\AspireComponentExtensions.cs` | — |  |
| `AspireDatabaseType` | enum | `Aspire\AspireComponentExtensions.cs` | — |  |
| `AspireHealthCheckOptions` | class | `Aspire\AspireOptions.cs` | — |  |
| `AspireOptions` | class | `Aspire\AspireOptions.cs` | — |  |
| `AspireRabbitMQOptions` | class | `Aspire\AspireComponentExtensions.cs` | — |  |
| `AspireRedisOptions` | class | `Aspire\AspireComponentExtensions.cs` | — |  |
| `AspireResilienceOptions` | class | `Aspire\AspireOptions.cs` | — |  |
| `AspireServiceDefaults` | class | `Aspire\AspireServiceDefaults.cs` | — |  |
| `AspireTelemetryOptions` | class | `Aspire\AspireOptions.cs` | — |  |
| `CorrelationIdAccessor` | class | `Aspire\AspireServiceDefaults.cs` | — |  |

#### Contract/ (48)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AsyncLocalCurrentUserProvider` | class | `Contract\Infrastructure\ICurrentUserProvider.cs` | — |  |
| `AsyncLocalTenantProvider` | class | `Contract\Infrastructure\ITenantProvider.cs` | — |  |
| `BulkheadOptions` | class | `Contract\Infrastructure\Pipe\IBulkheadOperation.cs` | — |  |
| `BulkheadRejectionReason` | enum | `Contract\Infrastructure\Pipe\IBulkheadOperation.cs` | — |  |
| `BulkOperationOptions` | class | `Contract\Data\Async\IBulkOperationsAsync.cs` | — |  |
| `BulkOperationResult` | class | `Contract\Data\Async\IBulkOperationsAsync.cs` | — |  |
| `CacheEntryOptions` | class | `Contract\Infrastructure\Caching\CacheEntryOptions.cs` | — |  |
| `CacheEntryPriority` | enum | `Contract\Infrastructure\Caching\CacheEntryOptions.cs` | — |  |
| `CacheInvalidationEvent` | class | `Contract\Infrastructure\Caching\ICacheSynchronizer.cs` | — |  |
| `CacheLevelStatistics` | class | `Contract\Infrastructure\Caching\IMultiLevelCache.cs` | — |  |
| `ChannelMessage` | record | `Contract\Infrastructure\Channels\IChannelMessage.cs` | — |  |
| `CircuitBreakerOptions` | class | `Contract\Infrastructure\Pipe\ICircuitBreakerOperation.cs` | — |  |
| `CompressionAlgorithm` | enum | `Contract\Infrastructure\Caching\ICacheCompressor.cs` | — |  |
| `DeadLetterOperation` | class | `Contract\Infrastructure\Pipe\IDeadLetterStore.cs` | — |  |
| `DeadLetterReason` | enum | `Contract\Infrastructure\Pipe\IDeadLetterStore.cs` | — |  |
| `DefaultRequestContext` | class | `Contract\Infrastructure\IRequestContext.cs` | — |  |
| `DomainEventBase` | record | `Contract\Domain\Entity\IDomainEvent.cs` | — |  |
| `EncryptionOptions` | class | `Contract\Infrastructure\IEncryptionProvider.cs` | — |  |
| `ErrorCategory` | enum | `Contract\ValueObjects\Logic\IStructuredMessageResult.cs` | — |  |
| `FallbackOptions` | class | `Contract\Infrastructure\Pipe\IFallbackOperation.cs` | — |  |
| `MultiLevelCacheStatistics` | class | `Contract\Infrastructure\Caching\IMultiLevelCache.cs` | — |  |
| `MvpChannelOptions` | class | `Contract\Infrastructure\Channels\IChannelOptions.cs` | — |  |
| `NativeRateLimiterOptions` | class | `Contract\Infrastructure\RateLimiting\IRateLimiterProvider.cs` | — |  |
| `NoTenantProvider` | class | `Contract\Infrastructure\ITenantProvider.cs` | — |  |
| `NullableNumericPropertyValidator` | class | `Contract\Infrastructure\Options\OptionsValidationContext.cs` | — |  |
| `NumericPropertyValidator` | class | `Contract\Infrastructure\Options\OptionsValidationContext.cs` | — |  |
| `OptionsValidationContext` | class | `Contract\Infrastructure\Options\OptionsValidationContext.cs` | — |  |
| `OptionsValidationResult` | class | `Contract\Infrastructure\Options\IOptionsValidator.cs` | — |  |
| `PipelineBulkheadRejectedException` | class | `Contract\Infrastructure\Pipe\IBulkheadOperation.cs` | — |  |
| `PipelineCircuitBreakerOpenException` | class | `Contract\Infrastructure\Pipe\ICircuitBreakerOperation.cs` | — |  |
| `PipelineCircuitState` | enum | `Contract\Infrastructure\Pipe\ICircuitBreakerOperation.cs` | — |  |
| `PipelineValidationError` | record | `Contract\Infrastructure\Pipe\IPipelineValidator.cs` | — |  |
| `PipelineValidationException` | class | `Contract\Infrastructure\Pipe\IPipelineValidator.cs` | — |  |
| `PipelineValidationResult` | class | `Contract\Infrastructure\Pipe\IPipelineValidator.cs` | — |  |
| `PrefetchRequest` | class | `Contract\Infrastructure\Caching\ICachePrefetcher.cs` | — |  |
| `RateLimitingAlgorithm` | enum | `Contract\Infrastructure\RateLimiting\IRateLimitedOperation.cs` | — |  |
| `RetryOptions` | class | `Contract\Infrastructure\Pipe\IRetryableOperation.cs` | — |  |
| `ServiceIgnoreAttribute` | class | `Contract\Infrastructure\DependencyInjection\ServiceKeyAttribute.cs` | — |  |
| `ServiceKeyAttribute` | class | `Contract\Infrastructure\DependencyInjection\ServiceKeyAttribute.cs` | — |  |
| `ServiceOrderAttribute` | class | `Contract\Infrastructure\DependencyInjection\ServiceKeyAttribute.cs` | — |  |
| `ServiceReplaceAttribute` | class | `Contract\Infrastructure\DependencyInjection\ServiceKeyAttribute.cs` | — |  |
| `ServiceTryAddAttribute` | class | `Contract\Infrastructure\DependencyInjection\ServiceKeyAttribute.cs` | — |  |
| `SetPropertyCall` | class | `Contract\Data\Async\IBulkOperationsAsync.cs` | — |  |
| `SetPropertyCalls` | class | `Contract\Data\Async\IBulkOperationsAsync.cs` | — |  |
| `StringPropertyValidator` | class | `Contract\Infrastructure\Options\OptionsValidationContext.cs` | — |  |
| `SystemUserProvider` | class | `Contract\Infrastructure\ICurrentUserProvider.cs` | — |  |
| `TimeSpanPropertyValidator` | class | `Contract\Infrastructure\Options\OptionsValidationContext.cs` | — |  |
| `UriPropertyValidator` | class | `Contract\Infrastructure\Options\OptionsValidationContext.cs` | — |  |

#### Converters/ (6)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `EntityIdNewtonsoftConverter` | class | `Converters\EntityIdNewtonsoftConverters.cs` | — |  |
| `GuidEntityIdNewtonsoftConverter` | class | `Converters\EntityIdNewtonsoftConverters.cs` | — |  |
| `IntEntityIdNewtonsoftConverter` | class | `Converters\EntityIdNewtonsoftConverters.cs` | — |  |
| `LongEntityIdNewtonsoftConverter` | class | `Converters\EntityIdNewtonsoftConverters.cs` | — |  |
| `StringEntityIdNewtonsoftConverter` | class | `Converters\EntityIdNewtonsoftConverters.cs` | — |  |
| `ValueObjectConverter` | class | `Converters\ValueObjectConverter.cs` | — |  |

#### Domain/ (21)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AndSpecification` | class | `Domain\Specifications\CompositeSpecifications.cs` | — |  |
| `AuditableEntity` | class | `Domain\Entities\AuditableEntity.cs` | — |  |
| `AuditableGuidEntity` | class | `Domain\Entities\AuditableEntity.cs` | — |  |
| `AuditableIntEntity` | class | `Domain\Entities\AuditableEntity.cs` | — |  |
| `AuditableLongEntity` | class | `Domain\Entities\AuditableEntity.cs` | — |  |
| `EntityBase` | class | `Domain\Entities\EntityBase.cs` | — |  |
| `Enumeration` | class | `Domain\Enumerations\Enumeration.cs` | — | sim |
| `GuidEntityBase` | class | `Domain\Entities\EntityBase.cs` | — |  |
| `InMemorySpecificationEvaluator` | class | `Domain\Specifications\InMemorySpecificationEvaluator.cs` | — |  |
| `InMemorySpecificationEvaluator` | class | `Domain\Specifications\InMemorySpecificationEvaluator.cs` | — |  |
| `IntEntityBase` | class | `Domain\Entities\EntityBase.cs` | — |  |
| `LongEntityBase` | class | `Domain\Entities\EntityBase.cs` | — |  |
| `NotSpecification` | class | `Domain\Specifications\CompositeSpecifications.cs` | — |  |
| `OrderStatus` | class | `Domain\Enumerations\Examples\OrderStatus.cs` | — | sim |
| `OrSpecification` | class | `Domain\Specifications\CompositeSpecifications.cs` | — |  |
| `PaymentMethod` | class | `Domain\Enumerations\Examples\PaymentMethod.cs` | — |  |
| `SoftDeletableEntity` | class | `Domain\Entities\SoftDeletableEntity.cs` | — |  |
| `SoftDeletableGuidEntity` | class | `Domain\Entities\SoftDeletableEntity.cs` | — |  |
| `SoftDeletableIntEntity` | class | `Domain\Entities\SoftDeletableEntity.cs` | — |  |
| `SoftDeletableLongEntity` | class | `Domain\Entities\SoftDeletableEntity.cs` | — |  |
| `Specification` | class | `Domain\Specifications\Specification.cs` | — | sim |

#### DTOs/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `PagingCriteriaRequest` | class | `DTOs\Models\PagingCriteriaRequest.cs` | — |  |
| `VoidResult` | class | `DTOs\VoidResult.cs` | — |  |

#### Entities/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `EntityBase` | class | `Entities\EntityBase.cs` | — |  |
| `EntityBaseLog` | class | `Entities\EntityBaseLog.cs` | — |  |

#### Enums/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MessageType` | enum | `Enums\MessageType.cs` | — |  |
| `PipelineInterceptorType` | enum | `Enums\Infrastructure\PipelineInterceptorType.cs` | — |  |
| `TelemetryLevels` | enum | `Enums\Infrastructure\TelemetryLevels.cs` | — |  |

#### Exceptions/ (13)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BusinessException` | class | `Exceptions\BusinessException.cs` | — |  |
| `ConfigurationException` | class | `Exceptions\ConfigurationException.cs` | — |  |
| `ConflictException` | class | `Exceptions\ConflictException.cs` | — |  |
| `DataException` | class | `Exceptions\DataException.cs` | — |  |
| `DomainException` | class | `Exceptions\DomainException.cs` | — |  |
| `ForbiddenException` | class | `Exceptions\ForbiddenException.cs` | — |  |
| `HttpStatusCodeException` | class | `Exceptions\HttpStatusCodeException.cs` | — |  |
| `Mvp24HoursException` | class | `Exceptions\Mvp24HoursException.cs` | — |  |
| `NotFoundException` | class | `Exceptions\NotFoundException.cs` | — |  |
| `PipelineException` | class | `Exceptions\PipelineException.cs` | — |  |
| `RateLimitExceededException` | class | `Exceptions\RateLimitExceededException.cs` | — |  |
| `UnauthorizedException` | class | `Exceptions\UnauthorizedException.cs` | — |  |
| `ValidationException` | class | `Exceptions\ValidationException.cs` | — |  |

#### Extensions/ (24)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BackgroundJobs` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `Cache` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `CompositeOptionsValidator` | class | `Extensions\Options\OptionsValidatorBase.cs` | — |  |
| `Database` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `DelegateOptionsValidator` | class | `Extensions\Options\OptionsValidatorBase.cs` | — |  |
| `DistributedLock` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `Email` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `Environment` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `FileStorage` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `HttpClient` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — | sim |
| `KeyedServiceAttribute` | class | `Extensions\KeyedServices\KeyedServiceExtensions.cs` | — |  |
| `KeyedServiceConfiguration` | class | `Extensions\KeyedServices\KeyedServiceExtensions.cs` | — |  |
| `Messaging` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `OptionsValidatorBase` | class | `Extensions\Options\OptionsValidatorBase.cs` | — |  |
| `RateLimiterRegistration` | record | `Extensions\RateLimitingServiceExtensions.cs` | — |  |
| `Secrets` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `Serializer` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `ServiceCollectionExtentions` | class | `Extensions\ServiceCollectionExtentions.cs` | — |  |
| `ServiceKeys` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `SimpleOptionsValidatorBase` | class | `Extensions\Options\OptionsValidatorBase.cs` | — |  |
| `Sms` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `TemplateRenderer` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `Tenant` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |
| `Validator` | class | `Extensions\KeyedServices\ServiceKeys.cs` | — |  |

#### Helpers/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `Data` | class | `Helpers\ContantsHelper.cs` | — |  |
| `Guard` | class | `Helpers\Guard.cs` | — | sim |

#### Infrastructure/ (16)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AesEncryptionProvider` | class | `Infrastructure\Security\AesEncryptionProvider.cs` | — |  |
| `ChannelFactory` | class | `Infrastructure\Channels\ChannelFactory.cs` | — |  |
| `Channels` | class | `Infrastructure\Channels\ChannelFactory.cs` | — |  |
| `ClockAdapter` | class | `Infrastructure\Clock\ClockAdapter.cs` | — |  |
| `DeterministicGuidGenerator` | class | `Infrastructure\GuidGenerators\DeterministicGuidGenerator.cs` | — |  |
| `MvpChannel` | class | `Infrastructure\Channels\MvpChannel.cs` | — |  |
| `NativeRateLimiterProvider` | class | `Infrastructure\RateLimiting\NativeRateLimiterProvider.cs` | — |  |
| `ProducerConsumer` | class | `Infrastructure\Channels\ProducerConsumer.cs` | — |  |
| `ProducerConsumer` | class | `Infrastructure\Channels\ProducerConsumer.cs` | — |  |
| `ProducerConsumerOptions` | class | `Infrastructure\Channels\ProducerConsumer.cs` | — |  |
| `SequentialGuidGenerator` | class | `Infrastructure\GuidGenerators\SequentialGuidGenerator.cs` | — |  |
| `SequentialGuidType` | enum | `Infrastructure\GuidGenerators\SequentialGuidGenerator.cs` | — |  |
| `StandardGuidGenerator` | class | `Infrastructure\GuidGenerators\StandardGuidGenerator.cs` | — |  |
| `SystemClock` | class | `Infrastructure\Clock\SystemClock.cs` | — |  |
| `TestClock` | class | `Infrastructure\Clock\TestClock.cs` | — |  |
| `TimeProviderAdapter` | class | `Infrastructure\Clock\TimeProviderAdapter.cs` | — |  |

#### Mappings/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MappingProfile` | class | `Mappings\MappingProfile.cs` | — |  |

#### Observability/ (93)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `Activities` | class | `Observability\ActivitySources.cs` | — |  |
| `ActivityEnricherBase` | class | `Observability\ActivityEnricherBase.cs` | — |  |
| `ActivityEnricherBase` | class | `Observability\ActivityEnricherBase.cs` | — |  |
| `BackgroundJobScope` | struct | `Observability\Metrics\InfrastructureMetrics.cs` | — |  |
| `BehaviorScope` | struct | `Observability\Metrics\CqrsMetrics.cs` | — |  |
| `CacheMetrics` | class | `Observability\Metrics\CacheMetrics.cs` | — |  |
| `CacheOperationScope` | struct | `Observability\Metrics\CacheMetrics.cs` | — |  |
| `Caching` | class | `Observability\ActivitySources.cs` | — |  |
| `Caching` | class | `Observability\MetricSources.cs` | — |  |
| `CommandScope` | struct | `Observability\Metrics\RepositoryMetrics.cs` | — |  |
| `CompositeActivityEnricher` | class | `Observability\ActivityEnricherBase.cs` | — |  |
| `CompositeLogEnricher` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `ConsoleExporterOptions` | class | `Observability\OpenTelemetryExporterExtensions.cs` | — |  |
| `ConsumeScope` | struct | `Observability\Metrics\MessagingMetrics.cs` | — |  |
| `Core` | class | `Observability\ActivitySources.cs` | — |  |
| `Core` | class | `Observability\MetricSources.cs` | — |  |
| `CorrelationIdEnricher` | class | `Observability\ActivityEnricherBase.cs` | — |  |
| `Cqrs` | class | `Observability\ActivitySources.cs` | — |  |
| `Cqrs` | class | `Observability\MetricSources.cs` | — |  |
| `CqrsMetrics` | class | `Observability\Metrics\CqrsMetrics.cs` | — |  |
| `CronJob` | class | `Observability\MetricSources.cs` | — | sim |
| `CronJob` | class | `Observability\ActivitySources.cs` | — | sim |
| `CronJobMetrics` | class | `Observability\Metrics\CronJobMetrics.cs` | — | sim |
| `Data` | class | `Observability\MetricSources.cs` | — |  |
| `Data` | class | `Observability\ActivitySources.cs` | — |  |
| `HttpClientRequestScope` | struct | `Observability\Metrics\InfrastructureMetrics.cs` | — |  |
| `HttpMetrics` | class | `Observability\Metrics\HttpMetrics.cs` | — |  |
| `HttpRequestScope` | struct | `Observability\Metrics\HttpMetrics.cs` | — |  |
| `Infrastructure` | class | `Observability\ActivitySources.cs` | — |  |
| `Infrastructure` | class | `Observability\MetricSources.cs` | — |  |
| `InfrastructureMetrics` | class | `Observability\Metrics\InfrastructureMetrics.cs` | — |  |
| `JobExecutionScope` | struct | `Observability\Metrics\CronJobMetrics.cs` | — |  |
| `LevelBasedLogSampler` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `LockScope` | struct | `Observability\Metrics\InfrastructureMetrics.cs` | — |  |
| `LogContextAccessor` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `LoggingOptions` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `LogMessagePatterns` | class | `Observability\OpenTelemetryLoggingExtensions.cs` | — |  |
| `LogResourceAttributes` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `LogScopeFactory` | class | `Observability\OpenTelemetryLoggingExtensions.cs` | — |  |
| `MessagingMetrics` | class | `Observability\Metrics\MessagingMetrics.cs` | — |  |
| `MetricNames` | class | `Observability\MetricNames.cs` | — |  |
| `MetricsOptions` | class | `Observability\MetricsServiceExtensions.cs` | — |  |
| `MetricTags` | class | `Observability\MetricNames.cs` | — |  |
| `Mvp24HoursActivitySources` | class | `Observability\ActivitySources.cs` | — |  |
| `Mvp24HoursMeters` | class | `Observability\MetricSources.cs` | — |  |
| `ObservabilityLoggingOptions` | class | `Observability\ObservabilityServiceExtensions.cs` | — |  |
| `ObservabilityMetricsOptions` | class | `Observability\ObservabilityServiceExtensions.cs` | — |  |
| `ObservabilityOptions` | class | `Observability\ObservabilityServiceExtensions.cs` | — |  |
| `ObservabilityTracingOptions` | class | `Observability\ObservabilityServiceExtensions.cs` | — |  |
| `OpenTelemetryExporterOptions` | class | `Observability\OpenTelemetryExporterExtensions.cs` | — |  |
| `OpenTelemetryLoggingConfig` | class | `Observability\OpenTelemetryLoggingExtensions.cs` | — |  |
| `OpenTelemetryLoggingOptions` | class | `Observability\OpenTelemetryLoggingBuilderExtensions.cs` | — |  |
| `OperationExecutionScope` | struct | `Observability\Metrics\PipelineMetrics.cs` | — |  |
| `OtlpExporterOptions` | class | `Observability\OpenTelemetryExporterExtensions.cs` | — |  |
| `OtlpExportProtocol` | enum | `Observability\OpenTelemetryExporterExtensions.cs` | — |  |
| `OtlpLogRecordAttributes` | class | `Observability\OpenTelemetryLoggingBuilderExtensions.cs` | — |  |
| `Pipe` | class | `Observability\MetricSources.cs` | — |  |
| `Pipe` | class | `Observability\ActivitySources.cs` | — |  |
| `PipelineExecutionScope` | struct | `Observability\Metrics\PipelineMetrics.cs` | — |  |
| `PipelineMetrics` | class | `Observability\Metrics\PipelineMetrics.cs` | — |  |
| `PrometheusExporterOptions` | class | `Observability\OpenTelemetryExporterExtensions.cs` | — |  |
| `PublishScope` | struct | `Observability\Metrics\MessagingMetrics.cs` | — |  |
| `QueryScope` | struct | `Observability\Metrics\RepositoryMetrics.cs` | — |  |
| `RabbitMQ` | class | `Observability\ActivitySources.cs` | — |  |
| `RabbitMQ` | class | `Observability\MetricSources.cs` | — |  |
| `RatioBasedLogSampler` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `RepositoryMetrics` | class | `Observability\Metrics\RepositoryMetrics.cs` | — |  |
| `RequestKind` | enum | `Observability\Metrics\CqrsMetrics.cs` | — |  |
| `RequestScope` | struct | `Observability\Metrics\CqrsMetrics.cs` | — |  |
| `SaveChangesScope` | struct | `Observability\Metrics\RepositoryMetrics.cs` | — |  |
| `ScopedActivity` | class | `Observability\ActivityExtensions.cs` | — |  |
| `SemanticEvents` | class | `Observability\SemanticTags.cs` | — |  |
| `SemanticTags` | class | `Observability\SemanticTags.cs` | — |  |
| `TenantContextEnricher` | class | `Observability\ActivityEnricherBase.cs` | — |  |
| `TenantContextLogEnricher` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `TraceContext` | class | `Observability\TracePropagation.cs` | — |  |
| `TraceContextAccessor` | class | `Observability\TracingServiceExtensions.cs` | — |  |
| `TraceContextLogEnricher` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `TraceContextLogSampler` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `TracePropagation` | class | `Observability\TracePropagation.cs` | — |  |
| `TracingOptions` | class | `Observability\TracingServiceExtensions.cs` | — |  |
| `UserContextEnricher` | class | `Observability\ActivityEnricherBase.cs` | — |  |
| `UserContextLogEnricher` | class | `Observability\LoggingServiceExtensions.cs` | — |  |
| `WebAPI` | class | `Observability\MetricSources.cs` | — |  |
| `WebAPI` | class | `Observability\ActivitySources.cs` | — |  |

#### Serialization/ (15)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AnonymousTypeContractResolver` | class | `Serialization\Json\AnonymousTypeContractResolver.cs` | — |  |
| `AotCompatibility` | class | `Serialization\SourceGeneration\AotCompatibility.cs` | — |  |
| `AotCompatibleAttribute` | class | `Serialization\SourceGeneration\AotCompatibility.cs` | — |  |
| `CompositeContractResolver` | class | `Serialization\Json\CompositeContractResolver.cs` | — |  |
| `EntityIdJsonConverterFactory` | class | `Serialization\Json\EntityIdJsonConverters.cs` | — |  |
| `GuidEntityIdJsonConverter` | class | `Serialization\Json\EntityIdJsonConverters.cs` | — |  |
| `IntEntityIdJsonConverter` | class | `Serialization\Json\EntityIdJsonConverters.cs` | — |  |
| `LongEntityIdJsonConverter` | class | `Serialization\Json\EntityIdJsonConverters.cs` | — |  |
| `LowerCaseResolver` | class | `Serialization\Json\LowerCaseResolver.cs` | — |  |
| `Mvp24HoursJsonSerializerContext` | class | `Serialization\SourceGeneration\Mvp24HoursJsonSerializerContext.cs` | — |  |
| `PropertyAndFieldsSerializerResolver` | class | `Serialization\Json\PropertyAndFieldsSerializerResolver.cs` | — |  |
| `PropertyRenameAndIgnoreSerializerContractResolver` | class | `Serialization\Json\PropertyRenameAndIgnoreSerializerContractResolver.cs` | — |  |
| `RequiresReflectionAttribute` | class | `Serialization\SourceGeneration\AotCompatibility.cs` | — |  |
| `StringEntityIdJsonConverter` | class | `Serialization\Json\EntityIdJsonConverters.cs` | — |  |
| `UpperCaseResolver` | class | `Serialization\Json\UpperCaseResolver.cs` | — |  |

#### ValueObjects/ (34)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `Address` | class | `ValueObjects\Address.cs` | — |  |
| `BaseVO` | class | `ValueObjects\BaseVO.cs` | — |  |
| `BusinessEvent` | class | `ValueObjects\Logic\BusinessEvent.cs` | — |  |
| `BusinessResult` | class | `ValueObjects\Logic\BusinessResultFactory.cs` | — |  |
| `BusinessResult` | class | `ValueObjects\Logic\BusinessResult.cs` | — |  |
| `Cnpj` | class | `ValueObjects\Cnpj.cs` | — |  |
| `Cpf` | class | `ValueObjects\Cpf.cs` | — |  |
| `CustomerId` | class | `ValueObjects\StronglyTypedIds.cs` | — |  |
| `DateRange` | class | `ValueObjects\DateRange.cs` | — |  |
| `Either` | class | `ValueObjects\Functional\Either.cs` | — |  |
| `Either` | struct | `ValueObjects\Functional\Either.cs` | — |  |
| `Email` | class | `ValueObjects\Email.cs` | — |  |
| `EntityId` | class | `ValueObjects\EntityId.cs` | — | sim |
| `GuidEntityId` | class | `ValueObjects\EntityId.cs` | — |  |
| `IdentityTransact` | class | `ValueObjects\Logic\IdentityTransact.cs` | — |  |
| `IntEntityId` | class | `ValueObjects\EntityId.cs` | — |  |
| `KeysetPageResult` | class | `ValueObjects\Logic\KeysetPageResult.cs` | — |  |
| `KeysetPageResultString` | class | `ValueObjects\Logic\KeysetPageResult.cs` | — |  |
| `LongEntityId` | class | `ValueObjects\EntityId.cs` | — |  |
| `Maybe` | struct | `ValueObjects\Functional\Maybe.cs` | — |  |
| `Maybe` | class | `ValueObjects\Functional\Maybe.cs` | — |  |
| `MessageResult` | class | `ValueObjects\Logic\MessageResult.cs` | — |  |
| `Money` | class | `ValueObjects\Money.cs` | — |  |
| `OrderId` | class | `ValueObjects\StronglyTypedIds.cs` | — |  |
| `PageResult` | class | `ValueObjects\Logic\PageResult.cs` | — |  |
| `PagingCriteria` | class | `ValueObjects\Logic\PagingCriteria.cs` | — |  |
| `PagingCriteriaExpression` | class | `ValueObjects\Logic\PagingCriteriaExpression.cs` | — |  |
| `PagingResult` | class | `ValueObjects\Logic\PagingResult.cs` | — |  |
| `Percentage` | class | `ValueObjects\Percentage.cs` | — |  |
| `PhoneNumber` | class | `ValueObjects\PhoneNumber.cs` | — |  |
| `ProductId` | class | `ValueObjects\StronglyTypedIds.cs` | — |  |
| `StringEntityId` | class | `ValueObjects\EntityId.cs` | — |  |
| `StructuredMessageResult` | class | `ValueObjects\Logic\StructuredMessageResult.cs` | — |  |
| `SummaryResult` | class | `ValueObjects\Logic\SummaryResult.cs` | — |  |

### `Mvp24Hours.Infrastructure` (216)

#### BackgroundJobs/ (25)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BatchJob` | class | `BackgroundJobs\Models\JobBatch.cs` | — |  |
| `ChildJob` | class | `BackgroundJobs\Models\ParentChildJob.cs` | — |  |
| `DashboardIntegrationHelpers` | class | `BackgroundJobs\Dashboard\DashboardIntegrationHelpers.cs` | — |  |
| `DeadLetterQueueFilter` | class | `BackgroundJobs\Management\IDeadLetterQueue.cs` | — |  |
| `FailedJob` | class | `BackgroundJobs\Management\IDeadLetterQueue.cs` | — |  |
| `HangfireJobOptions` | class | `BackgroundJobs\Options\HangfireJobOptions.cs` | — |  |
| `HangfireJobProvider` | class | `BackgroundJobs\Providers\HangfireJobProvider.cs` | — |  |
| `InMemoryDeadLetterQueue` | class | `BackgroundJobs\Management\InMemoryDeadLetterQueue.cs` | — |  |
| `InMemoryJobHistoryStore` | class | `BackgroundJobs\Management\InMemoryJobHistoryStore.cs` | — |  |
| `InMemoryJobMetrics` | class | `BackgroundJobs\Management\InMemoryJobMetrics.cs` | — |  |
| `InMemoryJobProvider` | class | `BackgroundJobs\Providers\InMemoryJobProvider.cs` | — |  |
| `JobBatch` | class | `BackgroundJobs\Models\JobBatch.cs` | — |  |
| `JobContext` | class | `BackgroundJobs\Models\JobContext.cs` | — |  |
| `JobExecutionRecord` | class | `BackgroundJobs\Management\IJobHistoryStore.cs` | — |  |
| `JobExecutionResult` | class | `BackgroundJobs\Results\JobExecutionResult.cs` | — |  |
| `JobExecutionStatistics` | class | `BackgroundJobs\Management\IJobHistoryStore.cs` | — |  |
| `JobExecutionStatus` | enum | `BackgroundJobs\Results\JobExecutionResult.cs` | — |  |
| `JobHistoryFilter` | class | `BackgroundJobs\Management\IJobHistoryStore.cs` | — |  |
| `JobMetric` | class | `BackgroundJobs\Management\IJobMetrics.cs` | — |  |
| `JobMetricsAggregate` | class | `BackgroundJobs\Management\IJobMetrics.cs` | — |  |
| `JobOptions` | class | `BackgroundJobs\Options\JobOptions.cs` | — |  |
| `ParentJob` | class | `BackgroundJobs\Models\ParentChildJob.cs` | — |  |
| `QuartzJobOptions` | class | `BackgroundJobs\Options\QuartzJobOptions.cs` | — |  |
| `QuartzJobProvider` | class | `BackgroundJobs\Providers\QuartzJobProvider.cs` | — |  |
| `QueueStatistics` | class | `BackgroundJobs\Management\IJobMetrics.cs` | — |  |

#### Configuration/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `InfrastructureOptions` | class | `Configuration\InfrastructureOptions.cs` | — |  |
| `ResilienceOptions` | class | `Configuration\InfrastructureOptions.cs` | — |  |
| `SecurityOptions` | class | `Configuration\InfrastructureOptions.cs` | — |  |

#### DistributedLocking/ (13)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BaseDistributedLockProvider` | class | `DistributedLocking\Providers\BaseDistributedLockProvider.cs` | — |  |
| `DistributedLockAcquisitionException` | class | `DistributedLocking\Exceptions\DistributedLockAcquisitionException.cs` | — |  |
| `DistributedLockFactory` | class | `DistributedLocking\DistributedLockFactory.cs` | — |  |
| `DistributedLockMetrics` | class | `DistributedLocking\Metrics\DistributedLockMetrics.cs` | — |  |
| `DistributedLockOptions` | class | `DistributedLocking\Options\DistributedLockOptions.cs` | — |  |
| `InMemoryDistributedLockProvider` | class | `DistributedLocking\Providers\InMemoryDistributedLockProvider.cs` | — |  |
| `LockAcquisitionResult` | class | `DistributedLocking\Results\LockAcquisitionResult.cs` | — |  |
| `LockAcquisitionStatus` | enum | `DistributedLocking\Results\LockAcquisitionResult.cs` | — |  |
| `LockHandleBase` | class | `DistributedLocking\Providers\LockHandleBase.cs` | — |  |
| `LockResourceMetrics` | class | `DistributedLocking\Metrics\DistributedLockMetrics.cs` | — |  |
| `PostgreSqlDistributedLockProvider` | class | `DistributedLocking\Providers\PostgreSqlDistributedLockProvider.cs` | — |  |
| `RedisDistributedLockProvider` | class | `DistributedLocking\Providers\RedisDistributedLockProvider.cs` | — |  |
| `SqlServerDistributedLockProvider` | class | `DistributedLocking\Providers\SqlServerDistributedLockProvider.cs` | — |  |

#### Email/ (35)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AzureCommunicationEmailOptions` | class | `Email\Options\AzureCommunicationEmailOptions.cs` | — |  |
| `AzureCommunicationEmailProvider` | class | `Email\Providers\AzureCommunicationEmailProvider.cs` | — |  |
| `BaseEmailProvider` | class | `Email\Providers\BaseEmailProvider.cs` | — |  |
| `BulkSendOptions` | class | `Email\Bulk\EmailBulkSender.cs` | — |  |
| `BulkSendProgress` | class | `Email\Bulk\EmailBulkSender.cs` | — |  |
| `EmailAttachment` | class | `Email\Models\EmailAttachment.cs` | — |  |
| `EmailBulkSender` | class | `Email\Bulk\EmailBulkSender.cs` | — |  |
| `EmailBulkSendResult` | class | `Email\Bulk\EmailBulkSender.cs` | — |  |
| `EmailDeliveryEvent` | enum | `Email\Tracking\EmailDeliveryTracking.cs` | — |  |
| `EmailDeliveryEventData` | class | `Email\Tracking\EmailDeliveryTracking.cs` | — |  |
| `EmailDeliveryStatus` | class | `Email\Tracking\EmailDeliveryTracking.cs` | — |  |
| `EmailDeliveryStatusType` | enum | `Email\Tracking\EmailDeliveryTracking.cs` | — |  |
| `EmailMessage` | class | `Email\Models\EmailMessage.cs` | — |  |
| `EmailOptions` | class | `Email\Options\EmailOptions.cs` | — |  |
| `EmailQueueItemStatus` | class | `Email\Queue\IEmailQueue.cs` | — |  |
| `EmailQueueProcessor` | class | `Email\Queue\EmailQueueProcessor.cs` | — |  |
| `EmailQueueProcessorOptions` | class | `Email\Queue\EmailQueueProcessor.cs` | — |  |
| `EmailQueueStatus` | enum | `Email\Queue\IEmailQueue.cs` | — |  |
| `EmailRateLimiter` | class | `Email\RateLimiting\EmailRateLimiter.cs` | — |  |
| `EmailSendResult` | class | `Email\Results\EmailSendResult.cs` | — |  |
| `EmbeddedImage` | class | `Email\Models\EmbeddedImage.cs` | — |  |
| `InMemoryEmailProvider` | class | `Email\Providers\InMemoryEmailProvider.cs` | — |  |
| `InMemoryEmailQueue` | class | `Email\Queue\InMemoryEmailQueue.cs` | — |  |
| `RateLimitOptions` | class | `Email\RateLimiting\EmailRateLimiter.cs` | — |  |
| `RateLimitStrategy` | enum | `Email\RateLimiting\EmailRateLimiter.cs` | — |  |
| `RazorEmailTemplateRenderer` | class | `Email\Templates\RazorEmailTemplateRenderer.cs` | — |  |
| `ScribanEmailTemplateRenderer` | class | `Email\Templates\ScribanEmailTemplateRenderer.cs` | — |  |
| `SendGridEmailOptions` | class | `Email\Options\SendGridEmailOptions.cs` | — |  |
| `SendGridEmailProvider` | class | `Email\Providers\SendGridEmailProvider.cs` | — |  |
| `SmtpEmailOptions` | class | `Email\Options\SmtpEmailOptions.cs` | — |  |
| `SmtpEmailProvider` | class | `Email\Providers\SmtpEmailProvider.cs` | — |  |
| `TemplateOptions` | class | `Email\Templates\ScribanEmailTemplateRenderer.cs` | — |  |
| `TemplateRenderException` | class | `Email\Templates\ScribanEmailTemplateRenderer.cs` | — |  |
| `TemplateValidationResult` | class | `Email\Templates\IEmailTemplateRenderer.cs` | — |  |
| `WebhookRegistrationResult` | class | `Email\Tracking\EmailDeliveryTracking.cs` | — |  |

#### FileStorage/ (12)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AwsS3StorageProvider` | class | `FileStorage\Providers\AwsS3StorageProvider.cs` | — |  |
| `AzureBlobStorageProvider` | class | `FileStorage\Providers\AzureBlobStorageProvider.cs` | — |  |
| `ChunkedUploadStatus` | class | `FileStorage\Results\ChunkedUploadStatus.cs` | — |  |
| `FileDownloadResult` | class | `FileStorage\Results\FileDownloadResult.cs` | — |  |
| `FileMetadata` | class | `FileStorage\Providers\FileMetadata.cs` | — |  |
| `FileStorageOptions` | class | `FileStorage\Options\FileStorageOptions.cs` | — |  |
| `FileUploadResult` | class | `FileStorage\Results\FileUploadResult.cs` | — |  |
| `FileVersion` | class | `FileStorage\Results\FileVersion.cs` | — |  |
| `InMemoryFileStorageProvider` | class | `FileStorage\Providers\InMemoryFileStorageProvider.cs` | — |  |
| `LocalFileStorageProvider` | class | `FileStorage\Providers\LocalFileStorageProvider.cs` | — |  |
| `MultipartUploadInfo` | class | `FileStorage\Results\MultipartUploadInfo.cs` | — |  |
| `SoftDeletedFile` | class | `FileStorage\Results\SoftDeletedFile.cs` | — |  |

#### HealthChecks/ (13)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BackgroundJobHealthCheck` | class | `HealthChecks\BackgroundJobHealthCheck.cs` | — |  |
| `BackgroundJobHealthCheckOptions` | class | `HealthChecks\BackgroundJobHealthCheck.cs` | — |  |
| `DistributedLockHealthCheck` | class | `HealthChecks\DistributedLockHealthCheck.cs` | — |  |
| `DistributedLockHealthCheckOptions` | class | `HealthChecks\DistributedLockHealthCheck.cs` | — |  |
| `EmailServiceHealthCheck` | class | `HealthChecks\EmailServiceHealthCheck.cs` | — |  |
| `EmailServiceHealthCheckOptions` | class | `HealthChecks\EmailServiceHealthCheck.cs` | — |  |
| `FileStorageHealthCheck` | class | `HealthChecks\FileStorageHealthCheck.cs` | — |  |
| `FileStorageHealthCheckOptions` | class | `HealthChecks\FileStorageHealthCheck.cs` | — |  |
| `HttpClientHealthCheck` | class | `HealthChecks\HttpClientHealthCheck.cs` | — |  |
| `HttpClientHealthCheckOptions` | class | `HealthChecks\HttpClientHealthCheck.cs` | — |  |
| `InfrastructureHealthCheckOptions` | class | `HealthChecks\InfrastructureHealthCheckExtensions.cs` | — |  |
| `SmsServiceHealthCheck` | class | `HealthChecks\SmsServiceHealthCheck.cs` | — |  |
| `SmsServiceHealthCheckOptions` | class | `HealthChecks\SmsServiceHealthCheck.cs` | — |  |

#### Helpers/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DirectoryService` | class | `Helpers\DirectoryHelper.cs` | — |  |

#### Http/ (42)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ApiKeyLocation` | enum | `Http\DelegatingHandlers\AuthenticationDelegatingHandler.cs` | — |  |
| `AuthenticationDelegatingHandler` | class | `Http\DelegatingHandlers\AuthenticationDelegatingHandler.cs` | — |  |
| `AuthenticationOptions` | class | `Http\DelegatingHandlers\AuthenticationDelegatingHandler.cs` | — |  |
| `AuthenticationScheme` | enum | `Http\DelegatingHandlers\AuthenticationDelegatingHandler.cs` | — |  |
| `BulkheadPolicy` | class | `Http\Resilience\BulkheadPolicy.cs` | — |  |
| `BulkheadPolicyOptions` | class | `Http\Resilience\BulkheadPolicy.cs` | — |  |
| `CertificateOptions` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `CircuitBreakerDelegatingHandler` | class | `Http\DelegatingHandlers\CircuitBreakerDelegatingHandler.cs` | — |  |
| `CircuitBreakerPolicy` | class | `Http\Resilience\CircuitBreakerPolicy.cs` | — |  |
| `CircuitBreakerPolicyOptions` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `CircuitBreakerStateChangeInfo` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `CompressionAlgorithm` | enum | `Http\DelegatingHandlers\CompressionDelegatingHandler.cs` | — |  |
| `CompressionDelegatingHandler` | class | `Http\DelegatingHandlers\CompressionDelegatingHandler.cs` | — |  |
| `CompressionHandlerOptions` | class | `Http\DelegatingHandlers\CompressionDelegatingHandler.cs` | — |  |
| `FallbackPolicy` | class | `Http\Resilience\FallbackPolicy.cs` | — |  |
| `FallbackPolicyOptions` | class | `Http\Resilience\FallbackPolicy.cs` | — |  |
| `HttpClientBuilder` | class | `Http\Builders\HttpClientBuilder.cs` | — |  |
| `HttpClientOptions` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `HttpLoggingOptions` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `HttpRequestTimeoutException` | class | `Http\DelegatingHandlers\TimeoutDelegatingHandler.cs` | — |  |
| `HttpResiliencePolicyBuilder` | class | `Http\Resilience\HttpClientResilienceExtensions.cs` | — |  |
| `JsonHttpClientSerializer` | class | `Http\Serializers\JsonHttpClientSerializer.cs` | — |  |
| `LoggingDelegatingHandler` | class | `Http\DelegatingHandlers\LoggingDelegatingHandler.cs` | — |  |
| `MessagePackHttpClientSerializer` | class | `Http\Serializers\MessagePackHttpClientSerializer.cs` | — |  |
| `NativeResilienceBuilder` | class | `Http\Resilience\NativeResilienceBuilder.cs` | — |  |
| `NativeResilienceOptions` | class | `Http\Resilience\NativeResilienceOptions.cs` | — |  |
| `NewtonsoftHttpClientSerializer` | class | `Http\Serializers\NewtonsoftHttpClientSerializer.cs` | — |  |
| `PolicyWrap` | class | `Http\Resilience\PolicyWrap.cs` | — |  |
| `PropagationAuthorizationDelegatingHandler` | class | `Http\DelegatingHandlers\PropagationAuthorizationDelegatingHandler.cs` | — |  |
| `PropagationCorrelationIdDelegatingHandler` | class | `Http\DelegatingHandlers\PropagationCorrelationIdDelegatingHandler.cs` | — |  |
| `PropagationHeaderDelegatingHandler` | class | `Http\DelegatingHandlers\PropagationHeaderDelegatingHandler.cs` | — |  |
| `ProxyOptions` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `RetryDelegatingHandler` | class | `Http\DelegatingHandlers\RetryDelegatingHandler.cs` | — |  |
| `RetryPolicy` | class | `Http\Resilience\RetryPolicy.cs` | — |  |
| `RetryPolicyOptions` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `TelemetryDelegatingHandler` | class | `Http\DelegatingHandlers\TelemetryDelegatingHandler.cs` | — |  |
| `TelemetryHandlerOptions` | class | `Http\DelegatingHandlers\TelemetryDelegatingHandler.cs` | — |  |
| `TimeoutDelegatingHandler` | class | `Http\DelegatingHandlers\TimeoutDelegatingHandler.cs` | — |  |
| `TimeoutPolicy` | class | `Http\Resilience\TimeoutPolicy.cs` | — |  |
| `TimeoutPolicyOptions` | class | `Http\Options\HttpClientOptions.cs` | — |  |
| `TypedHttpClient` | class | `Http\TypedHttpClient.cs` | — |  |
| `XmlHttpClientSerializer` | class | `Http\Serializers\XmlHttpClientSerializer.cs` | — |  |

#### Observability/ (4)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivitySources` | class | `Observability\ActivitySources.cs` | — |  |
| `CorrelationIdPropagation` | class | `Observability\CorrelationIdPropagation.cs` | — |  |
| `InfrastructureDiagnostics` | class | `Observability\InfrastructureDiagnostics.cs` | — |  |
| `InfrastructureMetrics` | class | `Observability\Metrics\InfrastructureMetrics.cs` | — |  |

#### Resilience/ (18)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `Bulkhead` | class | `Resilience\Implementations\Bulkhead.cs` | — |  |
| `Bulkhead` | class | `Resilience\Implementations\Bulkhead.cs` | — |  |
| `BulkheadRejectedException` | class | `Resilience\Exceptions\BulkheadRejectedException.cs` | — |  |
| `CircuitBreaker` | class | `Resilience\Implementations\CircuitBreaker.cs` | — | sim |
| `CircuitBreaker` | class | `Resilience\Implementations\CircuitBreaker.cs` | — | sim |
| `CircuitBreakerOpenException` | class | `Resilience\Exceptions\CircuitBreakerOpenException.cs` | — |  |
| `CircuitBreakerOptions` | class | `Resilience\Options\CircuitBreakerOptions.cs` | — |  |
| `CircuitBreakerStateChangeInfo` | class | `Resilience\Options\CircuitBreakerOptions.cs` | — |  |
| `NativeResilienceOptions` | class | `Resilience\Native\NativeResilienceOptions.cs` | — |  |
| `NativeResiliencePipeline` | class | `Resilience\Native\NativeResiliencePipeline.cs` | — |  |
| `NativeResiliencePipeline` | class | `Resilience\Native\NativeResiliencePipeline.cs` | — |  |
| `ResilienceBackoffType` | enum | `Resilience\Native\NativeResilienceOptions.cs` | — |  |
| `ResilienceCircuitState` | enum | `Resilience\Native\NativeResilienceOptions.cs` | — |  |
| `RetryAttemptInfo` | class | `Resilience\Options\RetryOptions.cs` | — |  |
| `RetryExhaustedInfo` | class | `Resilience\Options\RetryOptions.cs` | — |  |
| `RetryOptions` | class | `Resilience\Options\RetryOptions.cs` | — |  |
| `RetryPolicy` | class | `Resilience\Implementations\RetryPolicy.cs` | — |  |
| `RetryPolicy` | class | `Resilience\Implementations\RetryPolicy.cs` | — |  |

#### Security/ (7)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AwsSecretsManagerOptions` | class | `Security\Options\AwsSecretsManagerOptions.cs` | — |  |
| `AwsSecretsManagerProvider` | class | `Security\Providers\AwsSecretsManagerProvider.cs` | — |  |
| `AzureKeyVaultOptions` | class | `Security\Options\AzureKeyVaultOptions.cs` | — |  |
| `AzureKeyVaultSecretProvider` | class | `Security\Providers\AzureKeyVaultSecretProvider.cs` | — |  |
| `EnvironmentVariableOptions` | class | `Security\Options\EnvironmentVariableOptions.cs` | — |  |
| `EnvironmentVariableSecretProvider` | class | `Security\Providers\EnvironmentVariableSecretProvider.cs` | — |  |
| `SecretProviderOptions` | class | `Security\Options\SecretProviderOptions.cs` | — |  |

#### Sms/ (17)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AzureCommunicationSmsOptions` | class | `Sms\Options\AzureCommunicationSmsOptions.cs` | — |  |
| `AzureCommunicationSmsProvider` | class | `Sms\Providers\AzureCommunicationSmsProvider.cs` | — |  |
| `BaseSmsProvider` | class | `Sms\Providers\BaseSmsProvider.cs` | — |  |
| `DeliveryReport` | class | `Sms\Models\DeliveryReport.cs` | — |  |
| `InMemorySmsProvider` | class | `Sms\Providers\InMemorySmsProvider.cs` | — |  |
| `InMemorySmsRateLimiter` | class | `Sms\Services\InMemorySmsRateLimiter.cs` | — |  |
| `InMemorySmsTemplateService` | class | `Sms\Services\InMemorySmsTemplateService.cs` | — |  |
| `MmsAttachment` | class | `Sms\Models\MmsAttachment.cs` | — |  |
| `MmsMessage` | class | `Sms\Models\MmsMessage.cs` | — |  |
| `SmsDeliveryStatus` | enum | `Sms\Results\SmsSendResult.cs` | — |  |
| `SmsMessage` | class | `Sms\Models\SmsMessage.cs` | — |  |
| `SmsOptions` | class | `Sms\Options\SmsOptions.cs` | — |  |
| `SmsRateLimitOptions` | class | `Sms\Options\SmsRateLimitOptions.cs` | — |  |
| `SmsSendResult` | class | `Sms\Results\SmsSendResult.cs` | — |  |
| `SmsTemplate` | class | `Sms\Models\SmsTemplate.cs` | — |  |
| `TwilioSmsOptions` | class | `Sms\Options\TwilioSmsOptions.cs` | — |  |
| `TwilioSmsProvider` | class | `Sms\Providers\TwilioSmsProvider.cs` | — |  |

#### Testing/ (26)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityAssertions` | class | `Testing\Assertions\ActivityAssertions.cs` | — |  |
| `AssertionException` | class | `Testing\Assertions\AssertionException.cs` | — |  |
| `CategorizedLogEntry` | class | `Testing\Logging\InMemoryLoggerProvider.cs` | — |  |
| `EmailAssertions` | class | `Testing\Assertions\EmailAssertions.cs` | — |  |
| `FakeActivityListener` | class | `Testing\Observability\FakeActivityListener.cs` | — |  |
| `FakeEmailService` | class | `Testing\Fakes\FakeEmailService.cs` | — |  |
| `FakeFileStorage` | class | `Testing\Fakes\FakeFileStorage.cs` | — |  |
| `FakeLogger` | class | `Testing\Logging\FakeLogger.cs` | — |  |
| `FakeLogger` | class | `Testing\Logging\FakeLogger.cs` | — |  |
| `FakeMeterListener` | class | `Testing\Observability\FakeMeterListener.cs` | — |  |
| `FakeSmsService` | class | `Testing\Fakes\FakeSmsService.cs` | — |  |
| `FileStorageAssertions` | class | `Testing\Assertions\FileStorageAssertions.cs` | — |  |
| `HttpAssertions` | class | `Testing\Assertions\HttpAssertions.cs` | — |  |
| `HttpClientTestFixture` | class | `Testing\Fixtures\HttpClientTestFixture.cs` | — |  |
| `InfrastructureTestFixture` | class | `Testing\Fixtures\InfrastructureTestFixture.cs` | — |  |
| `InMemoryLoggerProvider` | class | `Testing\Logging\InMemoryLoggerProvider.cs` | — |  |
| `LogAssertions` | class | `Testing\Assertions\LogAssertions.cs` | — |  |
| `LogEntry` | class | `Testing\Logging\FakeLogger.cs` | — |  |
| `MetricAssertions` | class | `Testing\Assertions\MetricAssertions.cs` | — |  |
| `MockClock` | class | `Testing\MockClock.cs` | — |  |
| `ObservabilityTestFixture` | class | `Testing\Fixtures\ObservabilityTestFixture.cs` | — |  |
| `RecordedActivity` | class | `Testing\Observability\FakeActivityListener.cs` | — |  |
| `RecordedMeasurement` | class | `Testing\Observability\FakeMeterListener.cs` | — |  |
| `RecordedRequest` | class | `Testing\Http\TestHttpMessageHandler.cs` | — |  |
| `SmsAssertions` | class | `Testing\Assertions\SmsAssertions.cs` | — |  |
| `TestHttpMessageHandler` | class | `Testing\Http\TestHttpMessageHandler.cs` | — |  |

### `Mvp24Hours.Infrastructure.Caching` (56)

#### (root)/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheOptions` | class | `CacheOptions.cs` | — |  |
| `RepositoryCache` | class | `RepositoryCache.cs` | — |  |

#### Async/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RepositoryCacheAsync` | class | `Async\RepositoryCacheAsync.cs` | — |  |

#### Attributes/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheableAttribute` | class | `Attributes\CacheableAttribute.cs` | — |  |
| `CacheInvalidateAttribute` | class | `Attributes\CacheInvalidateAttribute.cs` | — |  |

#### Base/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RepositoryCacheBase` | class | `Base\RepositoryCacheBase.cs` | — |  |

#### Compression/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheCompressor` | class | `Compression\CacheCompressor.cs` | — |  |

#### EFCore/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `EfCoreCacheInterceptor` | class | `EFCore\EfCoreCacheInterceptor.cs` | — |  |
| `EfCoreCacheOptions` | class | `EFCore\EfCoreCacheInterceptor.cs` | — |  |

#### Extensions/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheSerializer` | enum | `Extensions\MvpCachingExtensions.cs` | — |  |
| `CacheType` | enum | `Extensions\MvpCachingExtensions.cs` | — |  |
| `MultiLevelCacheOptions` | class | `Extensions\MultiLevelCacheExtensions.cs` | — |  |
| `MvpCachingOptions` | class | `Extensions\MvpCachingExtensions.cs` | — |  |
| `ObservableCacheProviderOptions` | class | `Extensions\ObservabilityExtensions.cs` | — |  |

#### HybridCache/ (7)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `HybridCacheProvider` | class | `HybridCache\HybridCacheProvider.cs` | — |  |
| `HybridCacheSerializerType` | enum | `HybridCache\MvpHybridCacheOptions.cs` | — |  |
| `HybridCacheTagStatistics` | class | `HybridCache\IHybridCacheTagManager.cs` | — |  |
| `InMemoryHybridCacheTagManager` | class | `HybridCache\InMemoryHybridCacheTagManager.cs` | — |  |
| `MvpHybridCacheOptions` | class | `HybridCache\MvpHybridCacheOptions.cs` | — |  |
| `RedisHybridCacheTagManager` | class | `HybridCache\RedisHybridCacheTagManager.cs` | — |  |
| `RedisHybridCacheTagManagerOptions` | class | `HybridCache\RedisHybridCacheTagManager.cs` | — |  |

#### Invalidation/ (7)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheDependencyManager` | class | `Invalidation\CacheDependencyManager.cs` | — |  |
| `CacheInvalidationEvent` | class | `Invalidation\InMemoryCacheInvalidationEventPublisher.cs` | — |  |
| `CacheInvalidationEventType` | enum | `Invalidation\InMemoryCacheInvalidationEventPublisher.cs` | — |  |
| `CacheStampedePrevention` | class | `Invalidation\CacheStampedePrevention.cs` | — |  |
| `CacheTagManager` | class | `Invalidation\CacheTagManager.cs` | — |  |
| `InMemoryCacheInvalidationEventPublisher` | class | `Invalidation\InMemoryCacheInvalidationEventPublisher.cs` | — |  |
| `RedisCacheInvalidationEventPublisher` | class | `Invalidation\RedisCacheInvalidationEventPublisher.cs` | — |  |

#### KeyGenerators/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DefaultCacheKeyGenerator` | class | `KeyGenerators\DefaultCacheKeyGenerator.cs` | — |  |

#### Observability/ (7)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityNames` | class | `Observability\CacheActivitySource.cs` | — |  |
| `CacheActivitySource` | class | `Observability\CacheActivitySource.cs` | — |  |
| `CacheHealthCheck` | class | `Observability\CacheHealthCheck.cs` | — |  |
| `CacheHealthCheckOptions` | class | `Observability\CacheHealthCheck.cs` | — |  |
| `CacheMetrics` | class | `Observability\CacheMetrics.cs` | — |  |
| `ObservableCacheProvider` | class | `Observability\ObservableCacheProvider.cs` | — |  |
| `TagNames` | class | `Observability\CacheActivitySource.cs` | — |  |

#### Patterns/ (6)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ReadThroughCache` | class | `Patterns\ReadThroughCache.cs` | — |  |
| `RefreshAheadCache` | class | `Patterns\RefreshAheadCache.cs` | — |  |
| `WriteBehindBackgroundService` | class | `Patterns\WriteBehindBackgroundService.cs` | — |  |
| `WriteBehindCache` | class | `Patterns\WriteBehindCache.cs` | — |  |
| `WriteBehindOptions` | class | `Patterns\WriteBehindBackgroundService.cs` | — |  |
| `WriteThroughCache` | class | `Patterns\WriteThroughCache.cs` | — |  |

#### Prefetching/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CachePrefetcher` | class | `Prefetching\CachePrefetcher.cs` | — |  |

#### Providers/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DistributedCacheProvider` | class | `Providers\DistributedCacheProvider.cs` | — |  |
| `MemoryCacheProvider` | class | `Providers\MemoryCacheProvider.cs` | — |  |
| `MultiLevelCache` | class | `Providers\MultiLevelCache.cs` | — |  |

#### Repository/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheableRepository` | class | `Repository\CacheableRepository.cs` | — |  |
| `CacheableRepositoryOptions` | class | `Repository\CacheableRepository.cs` | — |  |

#### Resilience/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheResilienceOptions` | class | `Resilience\CacheResilienceOptions.cs` | — |  |
| `ResilientCacheProvider` | class | `Resilience\ResilientCacheProvider.cs` | — |  |

#### Serializers/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CompressedCacheSerializer` | class | `Serializers\CompressedCacheSerializer.cs` | — |  |
| `JsonCacheSerializer` | class | `Serializers\JsonCacheSerializer.cs` | — |  |
| `MessagePackCacheSerializer` | class | `Serializers\MessagePackCacheSerializer.cs` | — |  |

#### Synchronization/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `InMemoryCacheSynchronizer` | class | `Synchronization\InMemoryCacheSynchronizer.cs` | — |  |

#### Warming/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CacheWarmer` | class | `Warming\CacheWarmer.cs` | — |  |
| `CacheWarmupHostedService` | class | `Warming\CacheWarmupHostedService.cs` | — |  |

### `Mvp24Hours.Infrastructure.Cqrs` (67)

#### Abstractions/ (10)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DeadLetterStatus` | enum | `Abstractions\IDeadLetterStore.cs` | — |  |
| `DomainEventBase` | record | `Abstractions\IDomainEvent.cs` | — |  |
| `NotificationPublishingStrategy` | enum | `Abstractions\NotificationPublishingStrategy.cs` | — |  |
| `OutboxMessageStatus` | enum | `Abstractions\IIntegrationEventOutbox.cs` | — |  |
| `ParallelNotificationPublisher` | class | `Abstractions\NotificationPublishingStrategy.cs` | 0 |  |
| `ParallelNoWaitNotificationPublisher` | class | `Abstractions\NotificationPublishingStrategy.cs` | 0 |  |
| `PipelineHookBase` | class | `Abstractions\IPipelineHook.cs` | 0 |  |
| `PipelineHookBase` | class | `Abstractions\IPipelineHook.cs` | 0 |  |
| `SequentialContinueOnExceptionPublisher` | class | `Abstractions\NotificationPublishingStrategy.cs` | 0 |  |
| `SequentialNotificationPublisher` | class | `Abstractions\NotificationPublishingStrategy.cs` | 0 |  |

#### Behaviors/ (13)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AuthorizationBehavior` | class | `Behaviors\AuthorizationBehavior.cs` | 0 |  |
| `CacheInvalidationBehavior` | class | `Behaviors\CachingBehavior.cs` | 0 |  |
| `CachingBehavior` | class | `Behaviors\CachingBehavior.cs` | 0 |  |
| `CircuitState` | enum | `Behaviors\CircuitBreakerBehavior.cs` | — |  |
| `CqrsResilienceBackoffType` | enum | `Behaviors\NativeResilienceBehavior.cs` | — |  |
| `DefaultIdempotencyKeyGenerator` | class | `Behaviors\IdempotencyBehavior.cs` | 0 |  |
| `IdempotencyBehavior` | class | `Behaviors\IdempotencyBehavior.cs` | 0 |  |
| `MediatorTelemetryData` | record | `Behaviors\TelemetryBehavior.cs` | 0 |  |
| `NativeCqrsResilienceOptions` | class | `Behaviors\NativeResilienceBehavior.cs` | 0 |  |
| `NativeResilienceBehavior` | class | `Behaviors\NativeResilienceBehavior.cs` | 0 |  |
| `RetryBehavior` | class | `Behaviors\RetryBehavior.cs` | 0 |  |
| `TelemetryEventNames` | class | `Behaviors\TelemetryBehavior.cs` | — |  |
| `TenantNotFoundException` | class | `Behaviors\TenantBehavior.cs` | 0 |  |

#### EventSourcing/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AggregateRoot` | class | `EventSourcing\AggregateRoot.cs` | 0 |  |
| `AggregateRoot` | class | `EventSourcing\AggregateRoot.cs` | 0 |  |
| `CompositeSnapshotStrategy` | class | `EventSourcing\Snapshot.cs` | 0 |  |
| `DefaultAggregateFactory` | class | `EventSourcing\IAggregate.cs` | 0 |  |
| `DefaultEventTypeResolver` | class | `EventSourcing\IEventSerializer.cs` | 0 |  |
| `EventMetadata` | class | `EventSourcing\StoredEvent.cs` | 0 |  |
| `EventSourcingOptions` | class | `EventSourcing\EventSourcingExtensions.cs` | 0 |  |
| `EventStreamInfo` | class | `EventSourcing\EventStream.cs` | 0 |  |
| `SnapshotAggregateRoot` | class | `EventSourcing\AggregateRoot.cs` | 0 |  |

#### Extensions/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AutoIntegrationEventHandler` | class | `Extensions\DomainToIntegrationEventExtensions.cs` | 0 |  |
| `MediatorCacheOptions` | class | `Extensions\MediatorCachingExtensions.cs` | 0 |  |

#### Implementations/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RabbitMqIntegrationEventPublisher` | class | `Implementations\RabbitMqIntegrationEventPublisher.cs` | 0 |  |

#### Messaging/ (7)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `InboxCleanupService` | class | `Messaging\InboxProcessor.cs` | 0 |  |
| `OutboxCleanupService` | class | `Messaging\OutboxProcessor.cs` | 0 |  |
| `OutboxProcessor` | class | `Messaging\OutboxProcessor.cs` | 0 |  |
| `RabbitMQOutboxAdapter` | class | `Messaging\RabbitMQOutboxAdapter.cs` | 0 |  |
| `RabbitMQOutboxIntegrationEvent` | class | `Messaging\RabbitMQOutboxAdapter.cs` | 0 |  |
| `RabbitMQOutboxMessage` | class | `Messaging\RabbitMQOutboxAdapter.cs` | 0 |  |
| `RabbitMQOutboxStatus` | enum | `Messaging\RabbitMQOutboxAdapter.cs` | — |  |

#### MultiTenancy/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `TenantQueryFilter` | class | `MultiTenancy\TenantQueryFilter.cs` | 0 |  |

#### Observability/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityNames` | class | `Observability\OpenTelemetryIntegration.cs` | — |  |
| `TagNames` | class | `Observability\OpenTelemetryIntegration.cs` | — |  |

#### Projections/ (12)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AggregatingProjectionHandler` | class | `Projections\IProjectionHandler.cs` | 0 |  |
| `ApplyProjection` | class | `Projections\IncrementalProjection.cs` | 0 |  |
| `BatchProjection` | class | `Projections\IncrementalProjection.cs` | 0 |  |
| `IncrementalProjection` | class | `Projections\IncrementalProjection.cs` | 0 |  |
| `PagedReadModelResult` | class | `Projections\IReadModelRepository.cs` | 0 |  |
| `ProjectionHandlerBase` | class | `Projections\IProjectionHandler.cs` | — |  |
| `ProjectionHostedService` | class | `Projections\ProjectionHostedService.cs` | 0 |  |
| `ProjectionInfo` | class | `Projections\IProjection.cs` | 0 |  |
| `ProjectionStatus` | enum | `Projections\IProjection.cs` | — |  |
| `ReadModelProjectionHandler` | class | `Projections\IProjectionHandler.cs` | 0 |  |
| `RebuildState` | enum | `Projections\ProjectionHostedService.cs` | — |  |
| `RebuildStatus` | record | `Projections\ProjectionHostedService.cs` | 0 |  |

#### Queries/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `SortDirection` | enum | `Queries\SortedQuery.cs` | — |  |

#### Saga/ (6)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CompensatableCommandBase` | record | `Saga\CompensatingCommand.cs` | 0 |  |
| `CompensationRecord` | class | `Saga\CompensatingCommand.cs` | 0 |  |
| `SagaCompensationException` | class | `Saga\SagaExceptions.cs` | 0 |  |
| `SagaHostedService` | class | `Saga\SagaHostedService.cs` | 0 |  |
| `SagaMaxRetriesExceededException` | class | `Saga\SagaExceptions.cs` | 0 |  |
| `SagaStatus` | enum | `Saga\SagaStatus.cs` | — |  |

#### Scheduling/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ScheduledCommandHostedService` | class | `Scheduling\ScheduledCommandHostedService.cs` | 0 |  |
| `ScheduledCommandStatus` | enum | `Scheduling\IScheduledCommand.cs` | — |  |

#### Serialization/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CqrsJsonSerializerContext` | class | `Serialization\CqrsJsonSerializerContext.cs` | 0 |  |

### `Mvp24Hours.Infrastructure.CronJob` (28)

#### Configuration/ (4)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronJobGlobalOptions` | class | `Configuration\CronJobGlobalOptions.cs` | 0 |  |
| `CronJobGlobalOptionsValidator` | class | `Configuration\CronJobOptionsValidator.cs` | 0 |  |
| `CronJobOptions` | class | `Configuration\CronJobOptions.cs` | 0 |  |
| `CronJobOptionsValidator` | class | `Configuration\CronJobOptionsValidator.cs` | 0 |  |

#### Context/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronJobContext` | class | `Context\CronJobContext.cs` | 0 |  |
| `CronJobContextAccessor` | class | `Context\CronJobContextAccessor.cs` | 0 |  |

#### Control/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronJobController` | class | `Control\CronJobController.cs` | 0 |  |
| `CronJobExecutionState` | enum | `Control\ICronJobController.cs` | — |  |
| `CronJobStatus` | class | `Control\ICronJobController.cs` | 0 |  |

#### Dependencies/ (4)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronJobDependency` | class | `Dependencies\ICronJobDependency.cs` | 0 |  |
| `CronJobDependencyBuilder` | class | `Dependencies\ICronJobDependency.cs` | 0 |  |
| `InMemoryCronJobDependencyTracker` | class | `Dependencies\InMemoryCronJobDependencyTracker.cs` | 0 |  |
| `JobCompletionRecord` | class | `Dependencies\ICronJobDependency.cs` | 0 |  |

#### Events/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronJobEventDispatcher` | class | `Events\CronJobEventDispatcher.cs` | 0 |  |
| `CronJobEventHandlerBase` | class | `Events\ICronJobEventHandler.cs` | 0 |  |
| `SkipReason` | enum | `Events\ICronJobEventHandler.cs` | — |  |

#### Extensions/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronJobAdvancedOptions` | class | `Extensions\CronJobAdvancedExtensions.cs` | 0 |  |

#### Observability/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityNames` | class | `Observability\CronJobActivitySource.cs` | — |  |
| `Tags` | class | `Observability\CronJobActivitySource.cs` | — |  |

#### Resiliency/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CircuitBreakerState` | enum | `Resiliency\ICronJobResilienceConfig.cs` | — |  |
| `DistributedLockInfo` | class | `Resiliency\IDistributedCronJobLock.cs` | 0 |  |
| `InMemoryDistributedCronJobLock` | class | `Resiliency\InMemoryDistributedCronJobLock.cs` | 0 |  |

#### Scheduling/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronExpressionFormat` | enum | `Scheduling\CronExpressionFormat.cs` | — |  |
| `CronExpressionParser` | class | `Scheduling\CronExpressionParser.cs` | 0 |  |

#### Services/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AdvancedCronJobOptions` | class | `Services\AdvancedCronJobService.cs` | 0 |  |
| `AdvancedCronJobService` | class | `Services\AdvancedCronJobService.cs` | 0 |  |

#### State/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CronJobState` | class | `State\ICronJobStateStore.cs` | 0 |  |
| `InMemoryCronJobStateStore` | class | `State\InMemoryCronJobStateStore.cs` | 0 |  |

### `Mvp24Hours.Infrastructure.Data.EFCore` (100)

#### (root)/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `Mvp24HoursContext` | class | `Mvp24HoursContext.cs` | — |  |
| `ReadOnlyRepository` | class | `ReadOnlyRepository.cs` | — |  |
| `Repository` | class | `Repository.cs` | — |  |
| `UnitOfWork` | class | `UnitOfWork.cs` | — |  |
| `UnitOfWorkWithEvents` | class | `UnitOfWorkWithEvents.cs` | — |  |

#### Async/ (6)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BulkOperationsRepositoryAsync` | class | `Async\BulkOperationsRepositoryAsync.cs` | — |  |
| `ReadOnlyRepositoryAsync` | class | `Async\ReadOnlyRepositoryAsync.cs` | — |  |
| `RepositoryAsync` | class | `Async\RepositoryAsync.cs` | — |  |
| `StreamingRepositoryAsync` | class | `Async\StreamingRepositoryAsync.cs` | — |  |
| `UnitOfWorkAsync` | class | `Async\UnitOfWorkAsync.cs` | — |  |
| `UnitOfWorkWithEventsAsync` | class | `Async\UnitOfWorkWithEventsAsync.cs` | — |  |

#### Base/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RepositoryBase` | class | `Base\RepositoryBase.cs` | — |  |

#### Configuration/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `EFCoreRepositoryOptions` | class | `Configuration\EFCoreRepositoryOptions.cs` | — |  |
| `EFCoreResilienceOptions` | class | `Configuration\EFCoreResilienceOptions.cs` | — |  |

#### Converters/ (10)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `EncryptedAttribute` | class | `Converters\EncryptedValueConverters.cs` | — |  |
| `EncryptedBinaryConverter` | class | `Converters\EncryptedValueConverters.cs` | — |  |
| `EncryptedJsonConverter` | class | `Converters\EncryptedValueConverters.cs` | — |  |
| `EncryptedStringConverter` | class | `Converters\EncryptedValueConverters.cs` | — |  |
| `EntityIdValueConverter` | class | `Converters\EntityIdValueConverters.cs` | — |  |
| `GuidEntityIdValueConverter` | class | `Converters\EntityIdValueConverters.cs` | — |  |
| `IntEntityIdValueConverter` | class | `Converters\EntityIdValueConverters.cs` | — |  |
| `LongEntityIdValueConverter` | class | `Converters\EntityIdValueConverters.cs` | — |  |
| `NullableEncryptedStringConverter` | class | `Converters\EncryptedValueConverters.cs` | — |  |
| `StringEntityIdValueConverter` | class | `Converters\EntityIdValueConverters.cs` | — |  |

#### Cqrs/ (4)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DomainEventDispatcherAdapter` | class | `Cqrs\DomainEventDispatcherAdapter.cs` | — |  |
| `NoOpDomainEventDispatcher` | class | `Cqrs\NoOpDomainEventDispatcher.cs` | — |  |
| `ReadDbContextBase` | class | `Cqrs\ReadWriteDbContext.cs` | — |  |
| `WriteDbContextBase` | class | `Cqrs\ReadWriteDbContext.cs` | — |  |

#### Extensions/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CompiledQueryBase` | class | `Extensions\CompiledQueryExtensions.cs` | — |  |
| `EFCoreCqrsOptions` | class | `Extensions\EFCoreCqrsIntegrationExtensions.cs` | — |  |
| `EFCoreObservabilityOptions` | class | `Extensions\EFCoreObservabilityExtensions.cs` | — |  |
| `SlowQueryInterceptorOptions` | class | `Extensions\EFCoreObservabilityExtensions.cs` | — |  |
| `StructuredLoggingOptions` | class | `Extensions\EFCoreObservabilityExtensions.cs` | — |  |

#### HealthChecks/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DbContextHealthCheck` | class | `HealthChecks\DbContextHealthCheck.cs` | — |  |
| `DbContextHealthCheckOptions` | class | `HealthChecks\DbContextHealthCheckOptions.cs` | — |  |
| `MySqlHealthCheck` | class | `HealthChecks\MySqlHealthCheck.cs` | — |  |
| `MySqlHealthCheckOptions` | class | `HealthChecks\MySqlHealthCheck.cs` | — |  |
| `PostgreSqlHealthCheck` | class | `HealthChecks\PostgreSqlHealthCheck.cs` | — |  |
| `PostgreSqlHealthCheckOptions` | class | `HealthChecks\PostgreSqlHealthCheck.cs` | — |  |
| `SqlServerHealthCheck` | class | `HealthChecks\SqlServerHealthCheck.cs` | — |  |
| `SqlServerHealthCheckOptions` | class | `HealthChecks\SqlServerHealthCheck.cs` | — |  |

#### Interceptors/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AuditSaveChangesInterceptor` | class | `Interceptors\AuditSaveChangesInterceptor.cs` | — |  |
| `CommandLoggingInterceptor` | class | `Interceptors\CommandLoggingInterceptor.cs` | — |  |
| `ConcurrencyInterceptor` | class | `Interceptors\ConcurrencyInterceptor.cs` | — |  |
| `DomainEventSaveChangesInterceptor` | class | `Interceptors\DomainEventSaveChangesInterceptor.cs` | — |  |
| `SlowQueryInterceptor` | class | `Interceptors\SlowQueryInterceptor.cs` | — |  |
| `SoftDeleteInterceptor` | class | `Interceptors\SoftDeleteInterceptor.cs` | — |  |
| `StructuredLoggingInterceptor` | class | `Interceptors\StructuredLoggingInterceptor.cs` | — |  |
| `TenantInterceptorOptions` | class | `Interceptors\TenantSaveChangesInterceptor.cs` | — |  |
| `TenantSaveChangesInterceptor` | class | `Interceptors\TenantSaveChangesInterceptor.cs` | — |  |

#### Migrations/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DataSeederBase` | class | `Migrations\MigrationHostedService.cs` | — |  |
| `MigrationHostedService` | class | `Migrations\MigrationHostedService.cs` | — |  |
| `MigrationOptions` | class | `Migrations\MigrationOptions.cs` | — |  |
| `MigrationResult` | class | `Migrations\MigrationResult.cs` | — |  |
| `MigrationService` | class | `Migrations\MigrationService.cs` | — |  |
| `SchemaDifference` | class | `Migrations\MigrationResult.cs` | — |  |
| `SchemaDifferenceType` | enum | `Migrations\MigrationResult.cs` | — |  |
| `SchemaValidationResult` | class | `Migrations\MigrationResult.cs` | — |  |

#### Observability/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityNames` | class | `Observability\EFCoreActivitySource.cs` | — |  |
| `EFCoreActivitySource` | class | `Observability\EFCoreActivitySource.cs` | — |  |
| `EFCoreDiagnosticsListener` | class | `Observability\EFCoreDiagnosticsListener.cs` | — |  |
| `EFCoreMetrics` | class | `Observability\EFCoreMetrics.cs` | — |  |
| `TagNames` | class | `Observability\EFCoreActivitySource.cs` | — |  |

#### ReadWriteSplitting/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ConnectionResolver` | class | `ReadWriteSplitting\ConnectionResolver.cs` | — |  |
| `ReadWriteOptions` | class | `ReadWriteSplitting\ReadWriteOptions.cs` | — |  |
| `ReplicaHealth` | class | `ReadWriteSplitting\IReplicaSelector.cs` | — |  |
| `ReplicaLoadBalancing` | enum | `ReadWriteSplitting\ReadWriteOptions.cs` | — |  |
| `ReplicaSelector` | class | `ReadWriteSplitting\ReplicaSelector.cs` | — |  |

#### Resilience/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CircuitBreakerOpenException` | class | `Resilience\DbContextCircuitBreaker.cs` | — |  |
| `CircuitState` | enum | `Resilience\DbContextCircuitBreaker.cs` | — |  |
| `DbContextCircuitBreaker` | class | `Resilience\DbContextCircuitBreaker.cs` | — |  |
| `DbContextPoolMonitor` | class | `Resilience\DbContextPoolMonitor.cs` | — |  |
| `DbContextPoolStatistics` | class | `Resilience\DbContextPoolMonitor.cs` | — |  |
| `DbResilienceBackoffType` | enum | `Resilience\NativeDbResilienceExtensions.cs` | — |  |
| `MvpExecutionStrategy` | class | `Resilience\MvpExecutionStrategy.cs` | — |  |
| `NativeDbResilienceOptions` | class | `Resilience\NativeDbResilienceExtensions.cs` | — |  |
| `PoolStatisticsSnapshot` | class | `Resilience\DbContextPoolMonitor.cs` | — |  |

#### SchemaValidation/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `IssueSeverity` | enum | `SchemaValidation\ISchemaValidator.cs` | — |  |
| `IssueType` | enum | `SchemaValidation\ISchemaValidator.cs` | — |  |
| `ModelSummary` | class | `SchemaValidation\ISchemaValidator.cs` | — |  |
| `SchemaIssue` | class | `SchemaValidation\ISchemaValidator.cs` | — |  |
| `SchemaValidationException` | class | `SchemaValidation\SchemaValidationHostedService.cs` | — |  |
| `SchemaValidationHostedService` | class | `SchemaValidation\SchemaValidationHostedService.cs` | — |  |
| `SchemaValidationOptions` | class | `SchemaValidation\SchemaValidationOptions.cs` | — |  |
| `SchemaValidationResult` | class | `SchemaValidation\ISchemaValidator.cs` | — |  |
| `SchemaValidator` | class | `SchemaValidation\SchemaValidator.cs` | — |  |

#### Security/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DatabaseType` | enum | `Security\RowLevelSecurityHelper.cs` | — |  |

#### Specifications/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `SpecificationEvaluator` | class | `Specifications\SpecificationEvaluator.cs` | — |  |
| `SpecificationEvaluator` | class | `Specifications\SpecificationEvaluator.cs` | — |  |

#### Testing/ (11)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActionDataSeeder` | class | `Testing\IDataSeeder.cs` | — |  |
| `CompositeDataSeeder` | class | `Testing\IDataSeeder.cs` | — |  |
| `InMemoryDbContextFactory` | class | `Testing\InMemoryDbContextFactory.cs` | — |  |
| `InMemoryDbContextOptions` | class | `Testing\InMemoryDbContextOptions.cs` | — |  |
| `InMemoryTestDbContextFactory` | class | `Testing\TestDbContextFactory.cs` | — |  |
| `RepositoryFake` | class | `Testing\RepositoryFake.cs` | — |  |
| `RepositoryFakeAsync` | class | `Testing\RepositoryFakeAsync.cs` | — |  |
| `TestDbContextFactoryBase` | class | `Testing\TestDbContextFactory.cs` | — |  |
| `TestDbContextFactoryOptions` | class | `Testing\TestDbContextFactory.cs` | — |  |
| `UnitOfWorkFake` | class | `Testing\UnitOfWorkFake.cs` | — |  |
| `UnitOfWorkFakeAsync` | class | `Testing\UnitOfWorkFake.cs` | — |  |

### `Mvp24Hours.Infrastructure.Data.MongoDb` (152)

#### (root)/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `Mvp24HoursContext` | class | `Mvp24HoursContext.cs` | — |  |
| `ReadOnlyRepository` | class | `ReadOnlyRepository.cs` | — |  |
| `Repository` | class | `Repository.cs` | — |  |
| `UnitOfWork` | class | `UnitOfWork.cs` | — |  |
| `UnitOfWorkWithEvents` | class | `UnitOfWorkWithEvents.cs` | — |  |

#### Advanced/ (39)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CappedCollectionOptions` | class | `Advanced\CappedCollections\CappedCollectionOptions.cs` | — |  |
| `CappedCollectionStats` | class | `Advanced\CappedCollections\IMongoDbCappedCollectionService.cs` | — |  |
| `CollationPresets` | class | `Advanced\Collation\MongoDbCollationOptions.cs` | — |  |
| `ConcernPresets` | class | `Advanced\Concerns\MongoDbConcernOptions.cs` | — |  |
| `GeoNearResult` | class | `Advanced\Geospatial\IMongoDbGeospatialService.cs` | — |  |
| `GeoPoint` | class | `Advanced\Geospatial\GeoPoint.cs` | — |  |
| `GeoPolygon` | class | `Advanced\Geospatial\GeoPoint.cs` | — |  |
| `JsonSchemaBuilder` | class | `Advanced\SchemaValidation\JsonSchemaBuilder.cs` | — |  |
| `MongoDbCappedCollectionService` | class | `Advanced\CappedCollections\MongoDbCappedCollectionService.cs` | — |  |
| `MongoDbChangeStreamService` | class | `Advanced\ChangeStreams\MongoDbChangeStreamService.cs` | — |  |
| `MongoDbCollationOptions` | class | `Advanced\Collation\MongoDbCollationOptions.cs` | — |  |
| `MongoDbConcernOptions` | class | `Advanced\Concerns\MongoDbConcernOptions.cs` | — |  |
| `MongoDbGeospatialService` | class | `Advanced\Geospatial\MongoDbGeospatialService.cs` | — |  |
| `MongoDbGridFsService` | class | `Advanced\GridFS\MongoDbGridFsService.cs` | — |  |
| `MongoDbSchemaValidationOptions` | class | `Advanced\SchemaValidation\MongoDbSchemaValidationOptions.cs` | — |  |
| `MongoDbSchemaValidationService` | class | `Advanced\SchemaValidation\MongoDbSchemaValidationService.cs` | — |  |
| `MongoDbShardingOptions` | class | `Advanced\Sharding\MongoDbShardingOptions.cs` | — |  |
| `MongoDbShardingService` | class | `Advanced\Sharding\MongoDbShardingService.cs` | — |  |
| `MongoDbTextSearchOptions` | class | `Advanced\TextSearch\MongoDbTextSearchOptions.cs` | — |  |
| `MongoDbTextSearchService` | class | `Advanced\TextSearch\MongoDbTextSearchService.cs` | — |  |
| `MongoDbTimeSeriesService` | class | `Advanced\TimeSeries\MongoDbTimeSeriesService.cs` | — |  |
| `MongoDbTransactionManager` | class | `Advanced\Transactions\MongoDbTransactionManager.cs` | — |  |
| `MongoDbTransactionOptions` | class | `Advanced\Transactions\MongoDbTransactionOptions.cs` | — |  |
| `PropertySchemaBuilder` | class | `Advanced\SchemaValidation\JsonSchemaBuilder.cs` | — |  |
| `ReadConcernLevel` | enum | `Advanced\Concerns\MongoDbConcernOptions.cs` | — |  |
| `ReadPreferenceMode` | enum | `Advanced\Concerns\MongoDbConcernOptions.cs` | — |  |
| `SchemaValidationAction` | enum | `Advanced\SchemaValidation\MongoDbSchemaValidationOptions.cs` | — |  |
| `SchemaValidationLevel` | enum | `Advanced\SchemaValidation\MongoDbSchemaValidationOptions.cs` | — |  |
| `SchemaValidationResult` | class | `Advanced\SchemaValidation\IMongoDbSchemaValidationService.cs` | — |  |
| `ShardDistribution` | class | `Advanced\Sharding\IMongoDbShardingService.cs` | — |  |
| `ShardInfo` | class | `Advanced\Sharding\IMongoDbShardingService.cs` | — |  |
| `ShardKeyField` | class | `Advanced\Sharding\MongoDbShardingOptions.cs` | — |  |
| `ShardStats` | class | `Advanced\Sharding\IMongoDbShardingService.cs` | — |  |
| `TextSearchResult` | class | `Advanced\TextSearch\TextSearchResult.cs` | — |  |
| `TimeSeriesAggregationType` | enum | `Advanced\TimeSeries\IMongoDbTimeSeriesService.cs` | — |  |
| `TimeSeriesGranularity` | class | `Advanced\TimeSeries\TimeSeriesOptions.cs` | — |  |
| `TimeSeriesOptions` | class | `Advanced\TimeSeries\TimeSeriesOptions.cs` | — |  |
| `TimeWindowAggregation` | class | `Advanced\TimeSeries\IMongoDbTimeSeriesService.cs` | — |  |
| `WriteConcernMode` | enum | `Advanced\Concerns\MongoDbConcernOptions.cs` | — |  |

#### Async/ (6)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BulkOperationsRepositoryAsync` | class | `Async\BulkOperationsRepositoryAsync.cs` | — |  |
| `ReadOnlyRepositoryAsync` | class | `Async\ReadOnlyRepositoryAsync.cs` | — |  |
| `RepositoryAsync` | class | `Async\RepositoryAsync.cs` | — |  |
| `RepositoryAsyncWithInterceptors` | class | `Async\RepositoryAsyncWithInterceptors.cs` | — |  |
| `UnitOfWorkAsync` | class | `Async\UnitOfWorkAsync.cs` | — |  |
| `UnitOfWorkWithEventsAsync` | class | `Async\UnitOfWorkWithEventsAsync.cs` | — |  |

#### Base/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RepositoryBase` | class | `Base\RepositoryBase.cs` | — |  |

#### Configuration/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MongoDbBulkOperationOptions` | class | `Configuration\MongoDbBulkOperationOptions.cs` | — |  |
| `MongoDbOptions` | class | `Configuration\MongoDbOptions.cs` | — |  |
| `MongoDbRepositoryOptions` | class | `Configuration\MongoDbRepositoryOptions.cs` | — |  |

#### Cqrs/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DomainEventDispatcherAdapter` | class | `Cqrs\DomainEventDispatcherAdapter.cs` | — |  |
| `NoOpDomainEventDispatcher` | class | `Cqrs\NoOpDomainEventDispatcher.cs` | — |  |

#### Extensions/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MongoDbInterceptorOptions` | class | `Extensions\MongoDbInterceptorExtensions.cs` | — |  |
| `MongoDbReadWriteSeparationOptions` | class | `Extensions\MongoDbCqrsIntegrationExtensions.cs` | — |  |

#### HealthChecks/ (4)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MongoDbHealthCheck` | class | `HealthChecks\MongoDbHealthCheck.cs` | — |  |
| `MongoDbHealthCheckOptions` | class | `HealthChecks\MongoDbHealthCheck.cs` | — |  |
| `MongoDbReplicaSetHealthCheck` | class | `HealthChecks\MongoDbReplicaSetHealthCheck.cs` | — |  |
| `MongoDbReplicaSetHealthCheckOptions` | class | `HealthChecks\MongoDbReplicaSetHealthCheck.cs` | — |  |

#### Infrastructure/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MigrationResult` | class | `Infrastructure\Migrations\IMongoDbMigrationRunner.cs` | — |  |
| `MigrationStatus` | enum | `Infrastructure\Migrations\MongoDbMigrationHistory.cs` | — |  |
| `MigrationStepResult` | class | `Infrastructure\Migrations\IMongoDbMigrationRunner.cs` | — |  |
| `MongoDbIndexVerificationOptions` | class | `Infrastructure\MongoDbIndexVerificationService.cs` | — |  |
| `MongoDbIndexVerificationService` | class | `Infrastructure\MongoDbIndexVerificationService.cs` | — |  |
| `MongoDbMigrationHistory` | class | `Infrastructure\Migrations\MongoDbMigrationHistory.cs` | — |  |
| `MongoDbMigrationHostedService` | class | `Infrastructure\Migrations\MongoDbMigrationHostedService.cs` | — |  |
| `MongoDbMigrationOptions` | class | `Infrastructure\Migrations\MongoDbMigrationRunner.cs` | — |  |
| `MongoDbMigrationRunner` | class | `Infrastructure\Migrations\MongoDbMigrationRunner.cs` | — |  |

#### Interceptors/ (11)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AuditInterceptor` | class | `Interceptors\AuditInterceptor.cs` | — |  |
| `AuditOperation` | enum | `Interceptors\AuditTrailInterceptor.cs` | — |  |
| `AuditTrailEntry` | class | `Interceptors\AuditTrailInterceptor.cs` | — |  |
| `AuditTrailInterceptor` | class | `Interceptors\AuditTrailInterceptor.cs` | — |  |
| `CommandLogger` | class | `Interceptors\CommandLogger.cs` | — |  |
| `DeleteInterceptionResult` | struct | `Interceptors\IMongoDbInterceptor.cs` | — |  |
| `MongoDbInterceptorBase` | class | `Interceptors\MongoDbInterceptorBase.cs` | — |  |
| `MongoDbInterceptorPipeline` | class | `Interceptors\MongoDbInterceptorPipeline.cs` | — |  |
| `NoOpInterceptorPipeline` | class | `Interceptors\MongoDbInterceptorPipeline.cs` | — |  |
| `SoftDeleteInterceptor` | class | `Interceptors\SoftDeleteInterceptor.cs` | — |  |
| `TenantInterceptor` | class | `Interceptors\TenantInterceptor.cs` | — |  |

#### Observability/ (13)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CommandFailureDetails` | class | `Observability\MongoDbSlowQueryLogger.cs` | — |  |
| `CommandSummary` | class | `Observability\MongoDbDurationTracker.cs` | — |  |
| `ConnectionPoolStats` | class | `Observability\IMongoDbMetrics.cs` | — |  |
| `DurationStatistics` | class | `Observability\IMongoDbMetrics.cs` | — |  |
| `MongoDbConnectionPoolMetrics` | class | `Observability\MongoDbConnectionPoolMetrics.cs` | — |  |
| `MongoDbDurationTracker` | class | `Observability\MongoDbDurationTracker.cs` | — |  |
| `MongoDbMetrics` | class | `Observability\MongoDbMetrics.cs` | — |  |
| `MongoDbMetricsSnapshot` | class | `Observability\IMongoDbMetrics.cs` | — |  |
| `MongoDbObservabilityOptions` | class | `Observability\MongoDbObservabilityOptions.cs` | — |  |
| `MongoDbOpenTelemetryInstrumentation` | class | `Observability\MongoDbOpenTelemetryInstrumentation.cs` | — |  |
| `MongoDbSlowQueryLogger` | class | `Observability\MongoDbSlowQueryLogger.cs` | — |  |
| `MongoDbStructuredLogger` | class | `Observability\MongoDbStructuredLogger.cs` | — |  |
| `SlowQueryDetails` | class | `Observability\MongoDbSlowQueryLogger.cs` | — |  |

#### Performance/ (18)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BsonCollectionAttribute` | class | `Performance\Indexes\MongoDbIndexManager.cs` | — |  |
| `CollectionStats` | class | `Performance\Profiling\MongoDbQueryProfiler.cs` | — |  |
| `ExplainVerbosity` | enum | `Performance\Profiling\MongoDbQueryProfiler.cs` | — |  |
| `IndexInfo` | class | `Performance\Profiling\MongoDbQueryProfiler.cs` | — |  |
| `KeysetPagedResult` | class | `Performance\Pagination\MongoDbKeysetPagination.cs` | — |  |
| `MongoCompoundIndexAttribute` | class | `Performance\Attributes\MongoCompoundIndexAttribute.cs` | — |  |
| `MongoDbAggregationPipeline` | class | `Performance\Aggregation\MongoDbAggregationPipeline.cs` | — |  |
| `MongoDbAsyncStreaming` | class | `Performance\Streaming\MongoDbAsyncStreaming.cs` | — |  |
| `MongoDbConnectionPoolOptions` | class | `Performance\ConnectionPool\MongoDbConnectionPoolOptions.cs` | — |  |
| `MongoDbIndexManager` | class | `Performance\Indexes\MongoDbIndexManager.cs` | — |  |
| `MongoDbKeysetPagination` | class | `Performance\Pagination\MongoDbKeysetPagination.cs` | — |  |
| `MongoDbProjection` | class | `Performance\Projections\MongoDbProjection.cs` | — |  |
| `MongoDbProjectionOptions` | class | `Performance\Projections\MongoDbProjection.cs` | — |  |
| `MongoDbQueryProfiler` | class | `Performance\Profiling\MongoDbQueryProfiler.cs` | — |  |
| `MongoIndexAttribute` | class | `Performance\Attributes\MongoIndexAttribute.cs` | — |  |
| `MongoIndexType` | enum | `Performance\Attributes\MongoIndexType.cs` | — |  |
| `MongoTtlIndexAttribute` | class | `Performance\Attributes\MongoTtlIndexAttribute.cs` | — |  |
| `QueryExplainResult` | class | `Performance\Profiling\MongoDbQueryProfiler.cs` | — |  |

#### Resiliency/ (16)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CircuitBreakerState` | enum | `Resiliency\IMongoDbResiliencyPolicy.cs` | — |  |
| `ConnectionStateChangedEventArgs` | class | `Resiliency\MongoDbConnectionManager.cs` | — |  |
| `MongoDbCircuitBreaker` | class | `Resiliency\MongoDbCircuitBreaker.cs` | — | sim |
| `MongoDbCircuitBreakerOpenException` | class | `Resiliency\MongoDbResiliencyExceptions.cs` | — |  |
| `MongoDbConnectionManager` | class | `Resiliency\MongoDbConnectionManager.cs` | — | sim |
| `MongoDbConnectionRecoveryException` | class | `Resiliency\MongoDbResiliencyExceptions.cs` | — |  |
| `MongoDbFailoverException` | class | `Resiliency\MongoDbResiliencyExceptions.cs` | — |  |
| `MongoDbOperationTimeoutException` | class | `Resiliency\MongoDbResiliencyExceptions.cs` | — |  |
| `MongoDbResilienceBackoffType` | enum | `Resiliency\NativeMongoDbResilienceExtensions.cs` | — |  |
| `MongoDbResiliencyException` | class | `Resiliency\MongoDbResiliencyExceptions.cs` | — |  |
| `MongoDbResiliencyOptions` | class | `Resiliency\MongoDbResiliencyOptions.cs` | — | sim |
| `MongoDbResiliencyPolicy` | class | `Resiliency\MongoDbResiliencyPolicy.cs` | — |  |
| `MongoDbRetryExhaustedException` | class | `Resiliency\MongoDbResiliencyExceptions.cs` | — |  |
| `MongoDbRetryPolicy` | class | `Resiliency\MongoDbRetryPolicy.cs` | — | sim |
| `NativeMongoDbResilienceOptions` | class | `Resiliency\NativeMongoDbResilienceExtensions.cs` | — |  |
| `ReconnectAttemptEventArgs` | class | `Resiliency\MongoDbConnectionManager.cs` | — |  |

#### Security/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AesFieldEncryptor` | class | `Security\FieldEncryption.cs` | — |  |
| `CompositeSecurityPolicy` | class | `Security\MongoDbRowLevelSecurity.cs` | — |  |
| `EncryptedFieldAttribute` | class | `Security\FieldEncryption.cs` | — |  |
| `EncryptedStringSerializer` | class | `Security\FieldEncryption.cs` | — |  |
| `MongoDbAuthenticationOptions` | class | `Security\MongoDbAuthenticationOptions.cs` | — |  |
| `MongoDbAuthMechanism` | enum | `Security\MongoDbAuthenticationOptions.cs` | — |  |
| `MongoDbRowLevelSecurity` | class | `Security\MongoDbRowLevelSecurity.cs` | — |  |
| `OwnerBasedSecurityPolicy` | class | `Security\MongoDbRowLevelSecurity.cs` | — |  |

#### Specifications/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MongoDbSpecificationEvaluator` | class | `Specifications\MongoDbSpecificationEvaluator.cs` | — |  |
| `MongoDbSpecificationEvaluator` | class | `Specifications\MongoDbSpecificationEvaluator.cs` | — |  |

#### Testing/ (13)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActionMongoDataSeeder` | class | `Testing\IMongoDataSeeder.cs` | — |  |
| `ActionMongoDataSeederAsync` | class | `Testing\IMongoDataSeeder.cs` | — |  |
| `CompositeMongoDataSeeder` | class | `Testing\IMongoDataSeeder.cs` | — |  |
| `InMemoryMongoCollection` | class | `Testing\MongoDbInMemoryProvider.cs` | — |  |
| `MongoDbContainerInfo` | record | `Testing\MongoDbTestcontainersHelper.cs` | — |  |
| `MongoDbContextFactory` | class | `Testing\MongoDbContextFactory.cs` | — |  |
| `MongoDbInMemoryOptions` | class | `Testing\MongoDbInMemoryOptions.cs` | — |  |
| `MongoDbInMemoryProvider` | class | `Testing\MongoDbInMemoryProvider.cs` | — |  |
| `MongoDbTestcontainersOptions` | class | `Testing\MongoDbTestcontainersHelper.cs` | — |  |
| `MongoRepositoryFake` | class | `Testing\MongoRepositoryFake.cs` | — |  |
| `MongoRepositoryFakeAsync` | class | `Testing\MongoRepositoryFake.cs` | — |  |
| `MongoUnitOfWorkFake` | class | `Testing\MongoUnitOfWorkFake.cs` | — |  |
| `MongoUnitOfWorkFakeAsync` | class | `Testing\MongoUnitOfWorkFake.cs` | — |  |

### `Mvp24Hours.Infrastructure.Pipe` (170)

#### (root)/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DynamicContents` | class | `DynamicContents.cs` | — |  |
| `Pipeline` | class | `Pipeline.cs` | — | sim |
| `PipelineAsync` | class | `PipelineAsync.cs` | — | sim |
| `PipelineBase` | class | `PipelineBase.cs` | — |  |
| `PipelineMessage` | class | `PipelineMessage.cs` | — |  |

#### AdvancedFlow/ (34)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CheckpointablePipeline` | class | `AdvancedFlow\Checkpoint\CheckpointablePipeline.cs` | — |  |
| `CheckpointableResult` | class | `AdvancedFlow\Checkpoint\CheckpointablePipeline.cs` | — |  |
| `CheckpointableStepResult` | class | `AdvancedFlow\Checkpoint\CheckpointablePipeline.cs` | — |  |
| `CheckpointOptions` | class | `AdvancedFlow\Checkpoint\ICheckpointStore.cs` | — |  |
| `CheckpointStatus` | enum | `AdvancedFlow\Checkpoint\ICheckpointStore.cs` | — |  |
| `DependencyGraph` | class | `AdvancedFlow\DependencyGraph\DependencyGraphExecutor.cs` | — |  |
| `DependencyGraphExecutor` | class | `AdvancedFlow\DependencyGraph\DependencyGraphExecutor.cs` | — |  |
| `DependencyGraphNodeAsyncBase` | class | `AdvancedFlow\DependencyGraph\DependencyGraphNode.cs` | — |  |
| `DependencyGraphNodeBase` | class | `AdvancedFlow\DependencyGraph\DependencyGraphNode.cs` | — |  |
| `DependencyGraphOptions` | class | `AdvancedFlow\DependencyGraph\IDependencyGraphOperation.cs` | — |  |
| `DependencyGraphResult` | class | `AdvancedFlow\DependencyGraph\IDependencyGraphOperation.cs` | — |  |
| `ForkJoinOperation` | class | `AdvancedFlow\ForkJoin\ForkJoinOperation.cs` | — |  |
| `ForkJoinOperation` | class | `AdvancedFlow\ForkJoin\ForkJoinOperation.cs` | — |  |
| `ForkJoinOptions` | class | `AdvancedFlow\ForkJoin\IForkJoinOperation.cs` | — |  |
| `ForkJoinResult` | class | `AdvancedFlow\ForkJoin\IForkJoinOperation.cs` | — |  |
| `InMemoryCheckpointStore` | class | `AdvancedFlow\Checkpoint\InMemoryCheckpointStore.cs` | — |  |
| `InMemorySagaStateStore` | class | `AdvancedFlow\AdvancedFlowServiceExtensions.cs` | — |  |
| `JsonStateSerializer` | class | `AdvancedFlow\Checkpoint\CheckpointablePipeline.cs` | — |  |
| `LambdaDependencyGraphNode` | class | `AdvancedFlow\DependencyGraph\DependencyGraphNode.cs` | — |  |
| `NodeExecutionResult` | class | `AdvancedFlow\DependencyGraph\IDependencyGraphOperation.cs` | — |  |
| `OperationPriorityAttribute` | class | `AdvancedFlow\Priority\OperationPriority.cs` | — |  |
| `OperationPriorityComparer` | class | `AdvancedFlow\Priority\OperationPriority.cs` | — |  |
| `PipelineCheckpoint` | class | `AdvancedFlow\Checkpoint\ICheckpointStore.cs` | — |  |
| `PipelineSagaOptions` | class | `AdvancedFlow\Saga\IPipelineSaga.cs` | — |  |
| `PipelineSagaOrchestrator` | class | `AdvancedFlow\Saga\PipelineSagaOrchestrator.cs` | — |  |
| `PipelineSagaResult` | class | `AdvancedFlow\Saga\IPipelineSaga.cs` | — |  |
| `PipelineSagaStepBase` | class | `AdvancedFlow\Saga\PipelineSagaOrchestrator.cs` | — |  |
| `PrioritizedOperation` | class | `AdvancedFlow\Priority\OperationPriority.cs` | — |  |
| `PriorityLevel` | enum | `AdvancedFlow\Priority\OperationPriority.cs` | — |  |
| `PriorityPipeline` | class | `AdvancedFlow\Priority\PriorityPipeline.cs` | — |  |
| `SagaPersistedState` | class | `AdvancedFlow\Saga\IPipelineSaga.cs` | — |  |
| `SagaState` | enum | `AdvancedFlow\Saga\IPipelineSaga.cs` | — |  |
| `SagaStepResult` | class | `AdvancedFlow\Saga\IPipelineSaga.cs` | — |  |
| `StepExecutionRecord` | class | `AdvancedFlow\Saga\IPipelineSaga.cs` | — |  |

#### Builders/ (6)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ConditionalBranchBuilder` | class | `Builders\ConditionalBranchBuilder.cs` | — |  |
| `ConditionalBranchBuilderAsync` | class | `Builders\ConditionalBranchBuilder.cs` | — |  |
| `ParallelOperationBuilder` | class | `Builders\ParallelOperationBuilder.cs` | — |  |
| `ParallelOperationBuilderAsync` | class | `Builders\ParallelOperationBuilder.cs` | — |  |
| `SubPipelineBuilder` | class | `Builders\SubPipelineBuilder.cs` | — |  |
| `SubPipelineBuilderAsync` | class | `Builders\SubPipelineBuilder.cs` | — |  |

#### Channels/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ChannelPipeline` | class | `Channels\ChannelPipeline.cs` | — |  |
| `ChannelPipeline` | class | `Channels\ChannelPipeline.cs` | — |  |
| `ChannelPipelineBuilder` | class | `Channels\ChannelPipeline.cs` | — |  |
| `ChannelPipelineOptions` | class | `Channels\ChannelPipeline.cs` | — |  |
| `ChannelPipelineStageBuilder` | class | `Channels\ChannelPipeline.cs` | — |  |

#### Configuration/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `PipelineAsyncOptions` | class | `Configuration\PipelineAsyncOptions.cs` | — |  |
| `PipelineOptions` | class | `Configuration\PipelineOptions.cs` | — |  |

#### Context/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ContextPropagationMiddleware` | class | `Context\ContextPropagationMiddleware.cs` | — |  |
| `ContextPropagationOptions` | class | `Context\ContextPropagationMiddleware.cs` | — |  |
| `OperationActivityMiddleware` | class | `Context\OperationActivityMiddleware.cs` | — |  |
| `OperationActivityOptions` | class | `Context\OperationActivityMiddleware.cs` | — |  |
| `PipelineContext` | class | `Context\PipelineContext.cs` | — |  |
| `PipelineContextAccessor` | class | `Context\IPipelineContextAccessor.cs` | — |  |
| `PipelineContextOptions` | class | `Context\PipelineContextServiceExtensions.cs` | — |  |
| `PipelineStateSnapshot` | record | `Context\IPipelineContext.cs` | — |  |

#### ExceptionMapping/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DefaultPipelineExceptionMapper` | class | `ExceptionMapping\DefaultPipelineExceptionMapper.cs` | — |  |

#### Integration/ (18)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BatchStreamingOperation` | class | `Integration\Streaming\StreamingOperation.cs` | — |  |
| `CacheOperationOptions` | class | `Integration\Caching\ICacheableOperation.cs` | — |  |
| `CacheResultAttribute` | class | `Integration\Caching\CacheResultsMiddleware.cs` | — |  |
| `CacheResultsMiddleware` | class | `Integration\Caching\CacheResultsMiddleware.cs` | — |  |
| `CachingOperation` | class | `Integration\Caching\CachingOperation.cs` | — | sim |
| `FilterStreamingOperation` | class | `Integration\Streaming\StreamingOperation.cs` | — |  |
| `FlatMapStreamingOperation` | class | `Integration\Streaming\StreamingOperation.cs` | — |  |
| `FluentValidationOperation` | class | `Integration\FluentValidation\FluentValidationOperation.cs` | — | sim |
| `FluentValidationOptions` | class | `Integration\FluentValidation\FluentValidationOptions.cs` | — |  |
| `FluentValidationPipelineOperation` | class | `Integration\FluentValidation\FluentValidationOperation.cs` | — |  |
| `InlineValidator` | class | `Integration\FluentValidation\FluentValidationExtensions.cs` | — |  |
| `OpenTelemetryMiddleware` | class | `Integration\OpenTelemetry\OpenTelemetryMiddleware.cs` | — |  |
| `OpenTelemetryMiddlewareSync` | class | `Integration\OpenTelemetry\OpenTelemetryMiddleware.cs` | — |  |
| `OpenTelemetryOptions` | class | `Integration\OpenTelemetry\OpenTelemetryOptions.cs` | — |  |
| `StreamingOperationBase` | class | `Integration\Streaming\StreamingOperation.cs` | — |  |
| `StreamingPipeline` | class | `Integration\Streaming\StreamingPipeline.cs` | — | sim |
| `TracingTypedOperation` | class | `Integration\OpenTelemetry\TracingTypedOperation.cs` | — |  |
| `TransformStreamingOperation` | class | `Integration\Streaming\StreamingOperation.cs` | — |  |

#### Middleware/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `LoggingPipelineMiddleware` | class | `Middleware\LoggingPipelineMiddleware.cs` | — |  |
| `LoggingPipelineMiddlewareSync` | class | `Middleware\LoggingPipelineMiddleware.cs` | — |  |
| `PipelineMiddlewareExecutor` | class | `Middleware\PipelineMiddlewareExecutor.cs` | — |  |
| `PipelineTimeoutException` | class | `Middleware\TimeoutPipelineMiddleware.cs` | — |  |
| `TimeoutPipelineMiddleware` | class | `Middleware\TimeoutPipelineMiddleware.cs` | — |  |

#### Observability/ (28)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DiagramDirection` | enum | `Observability\IPipelineVisualizer.cs` | — |  |
| `InterceptorGroup` | class | `Observability\IPipelineVisualizer.cs` | — |  |
| `MetricsCollectorObserver` | class | `Observability\IPipelineObserver.cs` | — |  |
| `OperationCategory` | enum | `Observability\IPipelineVisualizer.cs` | — |  |
| `OperationEndEventArgs` | class | `Observability\IPipelineObserver.cs` | — |  |
| `OperationFailureEventArgs` | class | `Observability\IPipelineObserver.cs` | — |  |
| `OperationMetrics` | class | `Observability\IPipelineMetrics.cs` | — |  |
| `OperationNode` | class | `Observability\IPipelineVisualizer.cs` | — |  |
| `OperationStartEventArgs` | class | `Observability\IPipelineObserver.cs` | — |  |
| `PipelineCompleteEventArgs` | class | `Observability\IPipelineObserver.cs` | — |  |
| `PipelineEventArgsBase` | class | `Observability\IPipelineObserver.cs` | — |  |
| `PipelineExecutionMetrics` | class | `Observability\IPipelineMetrics.cs` | — |  |
| `PipelineHealthCheck` | class | `Observability\PipelineHealthCheck.cs` | — |  |
| `PipelineHealthCheckOptions` | class | `Observability\PipelineHealthCheck.cs` | — |  |
| `PipelineHealthMonitor` | class | `Observability\PipelineHealthCheck.cs` | — |  |
| `PipelineHealthStatus` | class | `Observability\PipelineHealthCheck.cs` | — |  |
| `PipelineMetrics` | class | `Observability\PipelineMetrics.cs` | — |  |
| `PipelineMetricsSnapshot` | class | `Observability\IPipelineMetrics.cs` | — |  |
| `PipelineObservabilityOptions` | class | `Observability\PipelineObservabilityExtensions.cs` | — |  |
| `PipelineObserverManager` | class | `Observability\IPipelineObserver.cs` | — |  |
| `PipelineStartEventArgs` | class | `Observability\IPipelineObserver.cs` | — |  |
| `PipelineStructure` | class | `Observability\IPipelineVisualizer.cs` | — |  |
| `PipelineVisualizationOptions` | class | `Observability\IPipelineVisualizer.cs` | — |  |
| `PipelineVisualizer` | class | `Observability\PipelineVisualizer.cs` | — |  |
| `RollbackEventArgs` | class | `Observability\IPipelineObserver.cs` | — |  |
| `StructuredLoggingMiddleware` | class | `Observability\StructuredLoggingMiddleware.cs` | — |  |
| `StructuredLoggingMiddlewareSync` | class | `Observability\StructuredLoggingMiddleware.cs` | — |  |
| `StructuredLoggingOptions` | class | `Observability\StructuredLoggingMiddleware.cs` | — |  |

#### Operations/ (27)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BranchCase` | record | `Operations\Branch\ConditionalBranchOperation.cs` | — |  |
| `ConditionalBranchOperation` | class | `Operations\Branch\ConditionalBranchOperation.cs` | — |  |
| `ConditionalBranchOperationAsync` | class | `Operations\Branch\ConditionalBranchOperation.cs` | — |  |
| `FileLogWriteOperation` | class | `Operations\Custom\Files\FileLogWriteOperation.cs` | — |  |
| `FileLogWriteOperationAsync` | class | `Operations\Custom\Async\Files\FileLogWriteOperationAsync.cs` | — |  |
| `FileTokenReadOperation` | class | `Operations\Custom\Files\FileTokenReadOperation.cs` | — |  |
| `FileTokenReadOperationAsync` | class | `Operations\Custom\Async\Files\FileTokenReadOperationAsync.cs` | — |  |
| `FileTokenWriteOperation` | class | `Operations\Custom\Files\FileTokenWriteOperation.cs` | — |  |
| `FileTokenWriteOperationAsync` | class | `Operations\Custom\Async\Files\FileTokenWriteOperationAsync.cs` | — |  |
| `OperationAction` | class | `Operations\OperationAction.cs` | — |  |
| `OperationActionAsync` | class | `Operations\Async\OperationActionAsync.cs` | — |  |
| `OperationBase` | class | `Operations\OperationBase.cs` | — |  |
| `OperationBaseAsync` | class | `Operations\Async\OperationBaseAsync.cs` | — |  |
| `OperationConditional` | class | `Operations\Custom\OperationConditional.cs` | — |  |
| `OperationConditionalAsync` | class | `Operations\Custom\Async\OperationConditionalAsync.cs` | — |  |
| `OperationMapper` | class | `Operations\Custom\OperationMapper.cs` | — |  |
| `OperationMapper` | class | `Operations\Custom\OperationMapper.cs` | — |  |
| `OperationMapperAsync` | class | `Operations\Custom\Async\OperationMapperAsync.cs` | — |  |
| `OperationMapperAsync` | class | `Operations\Custom\Async\OperationMapperAsync.cs` | — |  |
| `OperationMediator` | class | `Operations\Custom\OperationMediator.cs` | — |  |
| `OperationMediatorAsync` | class | `Operations\Custom\Async\OperationMediatorAsync.cs` | — |  |
| `OperationValidator` | class | `Operations\Custom\OperationValidator.cs` | — |  |
| `OperationValidatorAsync` | class | `Operations\Custom\Async\OperationValidatorAsync.cs` | — |  |
| `ParallelOperationGroup` | class | `Operations\Parallel\ParallelOperationGroup.cs` | — |  |
| `ParallelOperationGroupAsync` | class | `Operations\Parallel\ParallelOperationGroup.cs` | — |  |
| `SubPipelineOperation` | class | `Operations\Composition\SubPipelineOperation.cs` | — |  |
| `SubPipelineOperationAsync` | class | `Operations\Composition\SubPipelineOperation.cs` | — |  |

#### Resiliency/ (17)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BulkheadPipelineMiddleware` | class | `Resiliency\BulkheadPipelineMiddleware.cs` | — |  |
| `CircuitBreakerPipelineMiddleware` | class | `Resiliency\CircuitBreakerPipelineMiddleware.cs` | — |  |
| `DeadLetterOptions` | class | `Resiliency\DeadLetterPipelineMiddleware.cs` | — |  |
| `DeadLetterPipelineMiddleware` | class | `Resiliency\DeadLetterPipelineMiddleware.cs` | — |  |
| `DeadLetterStoreType` | enum | `Resiliency\PipelineResiliencyExtensions.cs` | — |  |
| `FallbackFailedException` | class | `Resiliency\FallbackPipelineMiddleware.cs` | — |  |
| `FallbackPipelineMiddleware` | class | `Resiliency\FallbackPipelineMiddleware.cs` | — |  |
| `FallbackPipelineMiddlewareSync` | class | `Resiliency\FallbackPipelineMiddleware.cs` | — |  |
| `InMemoryDeadLetterStore` | class | `Resiliency\InMemoryDeadLetterStore.cs` | — |  |
| `NativePipelineResilienceMiddleware` | class | `Resiliency\NativePipelineResilienceExtensions.cs` | — |  |
| `NativePipelineResilienceOptions` | class | `Resiliency\NativePipelineResilienceExtensions.cs` | — |  |
| `PipelineResilienceBackoffType` | enum | `Resiliency\NativePipelineResilienceExtensions.cs` | — |  |
| `PipelineResiliencyOptions` | class | `Resiliency\PipelineResiliencyExtensions.cs` | — |  |
| `RateLimitingPipelineMiddleware` | class | `Resiliency\RateLimitingPipelineMiddleware.cs` | — |  |
| `RateLimitingPipelineOptions` | class | `Resiliency\RateLimitingPipelineMiddleware.cs` | — |  |
| `RetryExhaustedException` | class | `Resiliency\RetryPipelineMiddleware.cs` | — |  |
| `RetryPipelineMiddleware` | class | `Resiliency\RetryPipelineMiddleware.cs` | — |  |

#### Resolvers/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `PipelineBuilderResolver` | class | `Resolvers\PipelineBuilderResolver.cs` | — |  |
| `PipelineBuilderResolverContainer` | class | `Resolvers\PipelineBuilderResolverContainer.cs` | — |  |

#### Typed/ (11)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `OperationChain` | class | `Typed\OperationChain.cs` | — |  |
| `OperationChain` | class | `Typed\OperationChain.cs` | — |  |
| `OperationResult` | class | `Typed\OperationResult.cs` | — |  |
| `OperationResult` | class | `Typed\OperationResult.cs` | — |  |
| `Pipe` | class | `Typed\TypedPipelineFluentExtensions.cs` | — |  |
| `TypedOperationBase` | class | `Typed\TypedOperationBase.cs` | — |  |
| `TypedOperationBase` | class | `Typed\TypedOperationBase.cs` | — |  |
| `TypedOperationBaseAsync` | class | `Typed\TypedOperationBaseAsync.cs` | — |  |
| `TypedOperationBaseAsync` | class | `Typed\TypedOperationBaseAsync.cs` | — |  |
| `TypedPipeline` | class | `Typed\TypedPipeline.cs` | — |  |
| `TypedPipelineAsync` | class | `Typed\TypedPipelineAsync.cs` | — |  |

#### Validation/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DefaultPipelineValidator` | class | `Validation\DefaultPipelineValidator.cs` | — |  |

### `Mvp24Hours.Infrastructure.RabbitMQ` (183)

#### (root)/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MvpRabbitMQClient` | class | `MvpRabbitMQClient.cs` | — |  |
| `MvpRabbitMQConnection` | class | `MvpRabbitMQConnection.cs` | — |  |

#### Channels/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ChannelBatchProcessor` | class | `Channels\ChannelBatchProcessor.cs` | — |  |
| `ChannelBatchProcessorOptions` | class | `Channels\ChannelBatchProcessor.cs` | — |  |

#### Configuration/ (34)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BatchConsumerOptions` | class | `Configuration\BatchConsumerOptions.cs` | — |  |
| `BatchPublishOptions` | class | `Configuration\BatchPublishOptions.cs` | — |  |
| `CircuitBreakerPolicyBuilder` | class | `Configuration\Fluent\CircuitBreakerPolicyBuilder.cs` | — |  |
| `CircuitBreakerPolicyConfiguration` | class | `Configuration\Fluent\CircuitBreakerPolicyBuilder.cs` | — |  |
| `CircuitBreakerState` | enum | `Configuration\Fluent\CircuitBreakerPolicyBuilder.cs` | — |  |
| `ConsumerConfiguration` | class | `Configuration\Fluent\ConsumerConfiguration.cs` | — |  |
| `ConsumerConfigurationRegistration` | record | `Configuration\Fluent\RabbitMQBusConfiguration.cs` | — |  |
| `ConsumerPrefetchOptions` | class | `Configuration\ConsumerPrefetchOptions.cs` | — |  |
| `EndpointConfiguration` | class | `Configuration\Fluent\EndpointConfigurationBuilder.cs` | — |  |
| `EndpointConfigurationBuilder` | class | `Configuration\Fluent\EndpointConfigurationBuilder.cs` | — |  |
| `EndpointNamingStyle` | enum | `Configuration\Fluent\EndpointConfigurationBuilder.cs` | — |  |
| `HeadersExchangeOptions` | class | `Configuration\HeadersExchangeOptions.cs` | — |  |
| `HostConfigurationBuilder` | class | `Configuration\Fluent\HostConfigurationBuilder.cs` | — |  |
| `MessageDeduplicationOptions` | class | `Configuration\MessageDeduplicationOptions.cs` | — |  |
| `MessageSchedulerOptions` | class | `Configuration\MessageSchedulerOptions.cs` | — |  |
| `MessageTtlOptions` | class | `Configuration\MessageTtlOptions.cs` | — |  |
| `OutboxOptions` | class | `Configuration\Fluent\OutboxOptions.cs` | — |  |
| `PriorityQueueOptions` | class | `Configuration\PriorityQueueOptions.cs` | — |  |
| `PublisherConfirmOptions` | class | `Configuration\PublisherConfirmOptions.cs` | — |  |
| `RabbitMQBusConfiguration` | class | `Configuration\Fluent\RabbitMQBusConfiguration.cs` | — |  |
| `RabbitMQClientOptions` | class | `Configuration\RabbitMQClientOptions.cs` | — |  |
| `RabbitMQConfigurationBuilder` | class | `Configuration\Fluent\RabbitMQConfigurationBuilder.cs` | — |  |
| `RabbitMQConnection` | class | `Configuration\RabbitMQConnection.cs` | — |  |
| `RabbitMQConnectionOptions` | class | `Configuration\RabbitMQConnectionOptions.cs` | — |  |
| `RabbitMQHostedOptions` | class | `Configuration\RabbitMQHostedOptions.cs` | — |  |
| `RabbitMQOptions` | class | `Configuration\RabbitMQOptions.cs` | — |  |
| `RabbitMQSslConfiguration` | class | `Configuration\Fluent\HostConfigurationBuilder.cs` | — |  |
| `RequestClientOptions` | class | `Configuration\RequestClientOptions.cs` | — |  |
| `RetryPolicyBuilder` | class | `Configuration\Fluent\RetryPolicyBuilder.cs` | — |  |
| `RetryPolicyConfiguration` | class | `Configuration\Fluent\RetryPolicyBuilder.cs` | — |  |
| `RetryType` | enum | `Configuration\Fluent\RetryPolicyBuilder.cs` | — |  |
| `SagaConfigurationBuilder` | class | `Configuration\Fluent\SagaConfigurationBuilder.cs` | — |  |
| `SagaConfigurationBuilderBase` | class | `Configuration\Fluent\SagaConfigurationBuilder.cs` | — |  |
| `SslConfigurationBuilder` | class | `Configuration\Fluent\HostConfigurationBuilder.cs` | — |  |

#### Consumers/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BatchConsumeContext` | class | `Consumers\BatchConsumeContext.cs` | — |  |
| `BatchConsumerDefinition` | class | `Consumers\BatchConsumerDefinition.cs` | — |  |
| `BatchConsumerProcessor` | class | `Consumers\BatchConsumerProcessor.cs` | — |  |
| `BatchMessageItem` | class | `Consumers\BatchMessageItem.cs` | — |  |
| `BatchMessageResult` | class | `Consumers\BatchMessageResult.cs` | — |  |
| `ConsumeContext` | class | `Consumers\ConsumeContext.cs` | — |  |
| `ConsumerDefinition` | class | `Consumers\ConsumerDefinition.cs` | — |  |
| `FaultContext` | class | `Consumers\FaultContext.cs` | — |  |

#### Core/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MvpRabbitMQExchangeType` | enum | `Core\Enums\MvpRabbitMQExchangeType.cs` | — |  |

#### Deduplication/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `InMemoryMessageDeduplicationStore` | class | `Deduplication\InMemoryMessageDeduplicationStore.cs` | — |  |

#### Exceptions/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RequestTimeoutException` | class | `Exceptions\RequestTimeoutException.cs` | — |  |

#### Extensions/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RabbitMQAdvancedOptions` | class | `Extensions\RabbitMQServiceExtensions.cs` | — |  |

#### HealthChecks/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RabbitMQHealthCheck` | class | `HealthChecks\RabbitMQHealthCheck.cs` | — |  |

#### Hosted/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `MvpRabbitMQHostedService` | class | `Hosted\MvpRabbitMQHostedService.cs` | — |  |

#### Logging/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RabbitMQStructuredLogger` | class | `Logging\RabbitMQStructuredLogger.cs` | — |  |

#### Messages/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `Message` | class | `Messages\Message.cs` | — |  |

#### Metrics/ (2)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RabbitMQMetrics` | class | `Metrics\RabbitMQMetrics.cs` | — |  |
| `RabbitMQMetricsSnapshot` | class | `Metrics\RabbitMQMetricsSnapshot.cs` | — |  |

#### MultiTenancy/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `InMemoryTenantRabbitMQResolver` | class | `MultiTenancy\InMemoryTenantRabbitMQResolver.cs` | — |  |
| `TenantConnectionFactory` | class | `MultiTenancy\TenantConnectionFactory.cs` | — |  |
| `TenantConsumeFilter` | class | `MultiTenancy\TenantConsumeFilter.cs` | — |  |
| `TenantIsolationStrategy` | enum | `MultiTenancy\Configuration\TenantRabbitMQOptions.cs` | — |  |
| `TenantMessageContext` | class | `MultiTenancy\TenantConsumeFilter.cs` | — |  |
| `TenantPublishFilter` | class | `MultiTenancy\TenantPublishFilter.cs` | — |  |
| `TenantRabbitMQConnectionConfig` | class | `MultiTenancy\Configuration\TenantRabbitMQOptions.cs` | — |  |
| `TenantRabbitMQOptions` | class | `MultiTenancy\Configuration\TenantRabbitMQOptions.cs` | — |  |
| `TenantSendFilter` | class | `MultiTenancy\TenantPublishFilter.cs` | — |  |

#### Observability/ (18)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityNames` | class | `Observability\RabbitMQActivitySource.cs` | — |  |
| `BaggageContext` | class | `Observability\BaggagePropagation.cs` | — |  |
| `BaggagePropagation` | class | `Observability\BaggagePropagation.cs` | — |  |
| `ConnectionInfo` | class | `Observability\RabbitMQDiagnostics.cs` | — |  |
| `ConnectionStatus` | enum | `Observability\RabbitMQDiagnostics.cs` | — |  |
| `ConsumerInfo` | class | `Observability\RabbitMQDiagnostics.cs` | — |  |
| `EnhancedStructuredLogger` | class | `Observability\EnhancedStructuredLogger.cs` | — |  |
| `ErrorInfo` | class | `Observability\RabbitMQDiagnostics.cs` | — |  |
| `Keys` | class | `Observability\BaggagePropagation.cs` | — |  |
| `MessageEnvelope` | class | `Observability\EnhancedStructuredLogger.cs` | — |  |
| `NullObserverManager` | class | `Observability\ObserverManager.cs` | — |  |
| `ObserverManager` | class | `Observability\ObserverManager.cs` | — |  |
| `QueueStats` | class | `Observability\RabbitMQDiagnostics.cs` | — |  |
| `RabbitMQActivitySource` | class | `Observability\RabbitMQActivitySource.cs` | — |  |
| `RabbitMQDiagnostics` | class | `Observability\RabbitMQDiagnostics.cs` | — |  |
| `RabbitMQHealthStatus` | class | `Observability\RabbitMQDiagnostics.cs` | — |  |
| `RabbitMQPrometheusMetrics` | class | `Observability\PrometheusMetrics.cs` | — |  |
| `Tags` | class | `Observability\RabbitMQActivitySource.cs` | — |  |

#### Pipeline/ (29)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ConsumeFilterContext` | class | `Pipeline\ConsumeFilterContext.cs` | — |  |
| `CorrelationConsumeFilter` | class | `Pipeline\Filters\CorrelationConsumeFilter.cs` | — |  |
| `CorrelationContext` | class | `Pipeline\Filters\CorrelationConsumeFilter.cs` | — |  |
| `CorrelationPublishFilter` | class | `Pipeline\Filters\CorrelationConsumeFilter.cs` | — |  |
| `CorrelationSendFilter` | class | `Pipeline\Filters\CorrelationConsumeFilter.cs` | — |  |
| `ExceptionHandlingConsumeFilter` | class | `Pipeline\Filters\ExceptionHandlingConsumeFilter.cs` | — |  |
| `ExceptionHandlingFilterOptions` | class | `Pipeline\Filters\ExceptionHandlingConsumeFilter.cs` | — |  |
| `FilterPipelineExecutor` | class | `Pipeline\FilterPipelineExecutor.cs` | — |  |
| `FilterPipelineOptions` | class | `Pipeline\FilterPipelineOptions.cs` | — |  |
| `LoggingConsumeFilter` | class | `Pipeline\Filters\LoggingConsumeFilter.cs` | — |  |
| `LoggingPublishFilter` | class | `Pipeline\Filters\LoggingPublishFilter.cs` | — |  |
| `LoggingSendFilter` | class | `Pipeline\Filters\LoggingSendFilter.cs` | — |  |
| `MessageValidationException` | class | `Pipeline\Filters\ValidationConsumeFilter.cs` | — |  |
| `PublishFilterContext` | class | `Pipeline\PublishFilterContext.cs` | — |  |
| `PublishRateLimitKeyMode` | enum | `Pipeline\Filters\RateLimitingPublishFilter.cs` | — |  |
| `RateLimitExceededBehavior` | enum | `Pipeline\Filters\RateLimitingConsumeFilter.cs` | — |  |
| `RateLimitingConsumeFilter` | class | `Pipeline\Filters\RateLimitingConsumeFilter.cs` | — |  |
| `RateLimitingConsumeFilterOptions` | class | `Pipeline\Filters\RateLimitingConsumeFilter.cs` | — |  |
| `RateLimitingPublishFilter` | class | `Pipeline\Filters\RateLimitingPublishFilter.cs` | — |  |
| `RateLimitingPublishFilterOptions` | class | `Pipeline\Filters\RateLimitingPublishFilter.cs` | — |  |
| `RateLimitKeyMode` | enum | `Pipeline\Filters\RateLimitingConsumeFilter.cs` | — |  |
| `SendFilterContext` | class | `Pipeline\SendFilterContext.cs` | — |  |
| `TelemetryConsumeFilter` | class | `Pipeline\Filters\TelemetryConsumeFilter.cs` | — |  |
| `TelemetryPublishFilter` | class | `Pipeline\Filters\TelemetryConsumeFilter.cs` | — |  |
| `TelemetrySendFilter` | class | `Pipeline\Filters\TelemetryConsumeFilter.cs` | — |  |
| `ValidationConsumeFilter` | class | `Pipeline\Filters\ValidationConsumeFilter.cs` | — |  |
| `ValidationError` | class | `Pipeline\Filters\ValidationConsumeFilter.cs` | — |  |
| `ValidationFilterOptions` | class | `Pipeline\Filters\ValidationConsumeFilter.cs` | — |  |
| `ValidationPublishFilter` | class | `Pipeline\Filters\ValidationConsumeFilter.cs` | — |  |

#### RequestResponse/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `RequestClient` | class | `RequestResponse\RequestClient.cs` | — |  |

#### Saga/ (22)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CqrsSagaAdapter` | class | `Saga\CqrsSagaIntegration.cs` | — |  |
| `CqrsSagaConsumerFactory` | class | `Saga\CqrsSagaIntegration.cs` | — |  |
| `CqrsSagaMessageConsumer` | class | `Saga\CqrsSagaIntegration.cs` | — |  |
| `EFCoreSagaRepository` | class | `Saga\Persistence\EFCoreSagaRepository.cs` | — |  |
| `EventHandler` | class | `Saga\SagaStateMachine.cs` | — |  |
| `EventHandlerBuilder` | class | `Saga\SagaStateMachine.cs` | — |  |
| `InMemorySagaRepository` | class | `Saga\Persistence\InMemorySagaRepository.cs` | — |  |
| `MongoDbSagaRepository` | class | `Saga\Persistence\MongoDbSagaRepository.cs` | — |  |
| `RedisSagaRepository` | class | `Saga\Persistence\RedisSagaRepository.cs` | — |  |
| `RedisSagaRepositoryOptions` | class | `Saga\Persistence\RedisSagaRepository.cs` | — |  |
| `SagaConsumeContext` | class | `Saga\SagaConsumeContext.cs` | — |  |
| `SagaConsumerDefinition` | class | `Saga\SagaConsumerDefinition.cs` | — |  |
| `SagaConsumerProcessor` | class | `Saga\SagaConsumerProcessor.cs` | — |  |
| `SagaEventContext` | class | `Saga\SagaStateMachine.cs` | — |  |
| `SagaInstance` | class | `Saga\SagaInstance.cs` | — |  |
| `SagaMessageConsumerAdapter` | class | `Saga\SagaConsumerProcessor.cs` | — |  |
| `SagaState` | class | `Saga\SagaStateMachine.cs` | — |  |
| `SagaStateEntity` | class | `Saga\Persistence\EFCoreSagaRepository.cs` | — |  |
| `SagaStateMachine` | class | `Saga\SagaStateMachine.cs` | — |  |
| `SagaStateMachineConsumer` | class | `Saga\SagaStateMachineConsumer.cs` | — |  |
| `SagaStateMachineConsumerDefinition` | class | `Saga\SagaConsumerDefinition.cs` | — |  |
| `SagaStateTransition` | class | `Saga\SagaInstance.cs` | — |  |

#### Scheduling/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `InMemoryScheduledMessageStore` | class | `Scheduling\InMemoryScheduledMessageStore.cs` | — |  |
| `MessageScheduler` | class | `Scheduling\MessageScheduler.cs` | — |  |
| `RecurringSchedule` | class | `Scheduling\ScheduledMessage.cs` | — |  |
| `RecurringScheduleType` | enum | `Scheduling\ScheduledMessage.cs` | — |  |
| `RedisScheduledMessageStore` | class | `Scheduling\RedisScheduledMessageStore.cs` | — |  |
| `ScheduledMessage` | class | `Scheduling\ScheduledMessage.cs` | — |  |
| `ScheduledMessageBackgroundService` | class | `Scheduling\ScheduledMessageBackgroundService.cs` | — |  |
| `ScheduledMessageStatus` | enum | `Scheduling\ScheduledMessage.cs` | — |  |

#### Serialization/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `JsonMessageSerializer` | class | `Serialization\JsonMessageSerializer.cs` | — |  |
| `MessageTypeResolver` | class | `Serialization\MessageTypeResolver.cs` | — |  |
| `RabbitMQJsonSerializerContext` | class | `Serialization\RabbitMQJsonSerializerContext.cs` | — |  |

#### Testing/ (10)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ConsumedMessage` | class | `Testing\ConsumedMessage.cs` | — |  |
| `ConsumedMessage` | class | `Testing\ConsumedMessage.cs` | — |  |
| `ConsumeResult` | class | `Testing\ConsumeResult.cs` | — |  |
| `ConsumerHarness` | class | `Testing\TestHarness.cs` | — |  |
| `InMemoryBus` | class | `Testing\InMemoryBus.cs` | — |  |
| `PublishedMessage` | class | `Testing\PublishedMessage.cs` | — |  |
| `PublishedMessage` | class | `Testing\PublishedMessage.cs` | — |  |
| `TestConsumeContext` | class | `Testing\TestConsumeContext.cs` | — |  |
| `TestConsumeContextBuilder` | class | `Testing\TestConsumeContextBuilder.cs` | — |  |
| `TestHarness` | class | `Testing\TestHarness.cs` | — |  |

#### Topology/ (15)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AutoBindingOptions` | class | `Topology\AutoBindingHelper.cs` | — |  |
| `ConsumerBindingInfo` | class | `Topology\AutoBindingHelper.cs` | — |  |
| `EndpointCasingStyle` | enum | `Topology\EndpointNameFormatter.cs` | — |  |
| `EndpointConvention` | class | `Topology\EndpointConvention.cs` | — |  |
| `EndpointConventionOptions` | class | `Topology\EndpointConvention.cs` | — |  |
| `EndpointInfo` | class | `Topology\EndpointConvention.cs` | — |  |
| `EndpointNameFormatter` | class | `Topology\EndpointNameFormatter.cs` | — |  |
| `EndpointNamingConventionOptions` | class | `Topology\EndpointNameFormatter.cs` | — |  |
| `MessageBindingInfo` | class | `Topology\AutoBindingHelper.cs` | — |  |
| `MessageTopology` | class | `Topology\MessageTopology.cs` | — |  |
| `MessageTopologyRegistry` | class | `Topology\MessageTopology.cs` | — |  |
| `RoutingKeyConvention` | class | `Topology\RoutingKeyConvention.cs` | — |  |
| `RoutingKeyConventionOptions` | class | `Topology\RoutingKeyConvention.cs` | — |  |
| `TopologyBuilder` | class | `Topology\TopologyBuilder.cs` | — |  |
| `TopologyBuilderOptions` | class | `Topology\TopologyBuilder.cs` | — |  |

#### Transactional/ (12)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `InMemoryTransactionalOutbox` | class | `Transactional\InMemoryTransactionalOutbox.cs` | — |  |
| `InMemoryTransactionalOutboxOptions` | class | `Transactional\InMemoryTransactionalOutbox.cs` | — |  |
| `OutboxPublisher` | class | `Transactional\OutboxPublisher.cs` | — |  |
| `OutboxPublisherOptions` | class | `Transactional\OutboxPublisher.cs` | — |  |
| `TransactionalBus` | class | `Transactional\TransactionalBus.cs` | — |  |
| `TransactionalBusOptions` | class | `Transactional\TransactionalBus.cs` | — |  |
| `TransactionalConsumeContext` | class | `Transactional\TransactionalConsumeContext.cs` | — |  |
| `TransactionalConsumeContextFactory` | class | `Transactional\TransactionalConsumeContext.cs` | — |  |
| `TransactionalEnlistment` | class | `Transactional\TransactionalEnlistment.cs` | — |  |
| `TransactionalUnitOfWork` | class | `Transactional\UnitOfWorkTransactionalExtensions.cs` | — |  |
| `TransactionalUnitOfWorkAsync` | class | `Transactional\UnitOfWorkTransactionalExtensions.cs` | — |  |
| `TransactionalUnitOfWorkFactory` | class | `Transactional\UnitOfWorkTransactionalExtensions.cs` | — |  |

### `Mvp24Hours.WebAPI` (169)

#### Binders/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DateOnlyModelBinder` | class | `Binders\DateOnlyModelBinder.cs` | — |  |
| `DateTimeOffsetModelBinder` | class | `Binders\DateTimeOffsetModelBinder.cs` | — |  |
| `EntityIdModelBinder` | class | `Binders\EntityIdModelBinder.cs` | — |  |
| `ExtensionBinder` | class | `Binders\ExtensionBinder.cs` | — |  |
| `ModelBinder` | class | `Binders\ModelBinder.cs` | — |  |
| `Mvp24HoursModelBinderProvider` | class | `Binders\Mvp24HoursModelBinderProvider.cs` | — |  |
| `PagingCriteriaModelBinder` | class | `Binders\PagingCriteriaModelBinder.cs` | — |  |
| `TimeOnlyModelBinder` | class | `Binders\TimeOnlyModelBinder.cs` | — |  |

#### Configuration/ (68)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AntiForgeryOptions` | class | `Configuration\AntiForgeryOptions.cs` | — |  |
| `ApiKeyAuthenticationOptions` | class | `Configuration\ApiKeyAuthenticationOptions.cs` | — |  |
| `ApiKeyLocation` | enum | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `ApiKeyRateLimitOptions` | class | `Configuration\ApiKeyAuthenticationOptions.cs` | — |  |
| `ApiKeyValidationResult` | class | `Configuration\ApiKeyAuthenticationOptions.cs` | — |  |
| `ApiVersioningOptions` | class | `Configuration\ApiVersioningOptions.cs` | — |  |
| `ApiVersioningStrategy` | enum | `Configuration\ApiVersioningOptions.cs` | — |  |
| `CacheControlOptions` | class | `Configuration\CacheControlOptions.cs` | — |  |
| `CacheControlPolicy` | class | `Configuration\CacheControlOptions.cs` | — |  |
| `CacheProfile` | class | `Configuration\ResponseCachingOptions.cs` | — |  |
| `CompressionOptions` | class | `Configuration\CompressionOptions.cs` | — |  |
| `ConfigureSwaggerGenOptions` | class | `Configuration\ConfigureSwaggerGenOptions.cs` | — |  |
| `ContentFormat` | enum | `Configuration\ContentNegotiationOptions.cs` | — |  |
| `ContentNegotiationOptions` | class | `Configuration\ContentNegotiationOptions.cs` | — |  |
| `CorrelationIdOptions` | class | `Configuration\CorrelationIdOptions.cs` | — |  |
| `CorsOptions` | class | `Configuration\CorsOptions.cs` | — |  |
| `DistributedRateLimitingOptions` | class | `Configuration\RateLimitingOptions.cs` | — |  |
| `ETagAlgorithm` | enum | `Configuration\ETagOptions.cs` | — |  |
| `ETagOptions` | class | `Configuration\ETagOptions.cs` | — |  |
| `ExceptionOptions` | class | `Configuration\ExceptionOptions.cs` | — |  |
| `HealthCheckOptions` | class | `Configuration\HealthCheckOptions.cs` | — |  |
| `IdempotencyKeySource` | enum | `Configuration\IdempotencyOptions.cs` | — |  |
| `IdempotencyOptions` | class | `Configuration\IdempotencyOptions.cs` | — |  |
| `IdempotencyStorageType` | enum | `Configuration\IdempotencyOptions.cs` | — |  |
| `InputSanitizationOptions` | class | `Configuration\InputSanitizationOptions.cs` | — |  |
| `IpFilteringMode` | enum | `Configuration\IpFilteringOptions.cs` | — |  |
| `IpFilteringOptions` | class | `Configuration\IpFilteringOptions.cs` | — |  |
| `JsonSerializationOptions` | class | `Configuration\ContentNegotiationOptions.cs` | — |  |
| `MediaTypeMapping` | class | `Configuration\ContentNegotiationOptions.cs` | — |  |
| `MvpProblemDetailsOptions` | class | `Configuration\MvpProblemDetailsOptions.cs` | — |  |
| `NativeOpenApiOptions` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiApiKeySecurityScheme` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiAuthenticationScheme` | enum | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiBearerSecurityScheme` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiContactInfo` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiLicenseInfo` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiServerInfo` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiServerVariable` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiTagInfo` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OpenApiVersionConfig` | class | `Configuration\NativeOpenApiOptions.cs` | — |  |
| `OutputCachePolicyOptions` | class | `Configuration\OutputCachingOptions.cs` | — |  |
| `OutputCachePolicyPreset` | enum | `Configuration\OutputCachingOptions.cs` | — |  |
| `OutputCachingOptions` | class | `Configuration\OutputCachingOptions.cs` | — |  |
| `ProblemDetailsOptions` | class | `Configuration\ProblemDetailsOptions.cs` | — |  |
| `QueueProcessingOrder` | enum | `Configuration\RateLimitingOptions.cs` | — |  |
| `RateLimitingAlgorithm` | enum | `Configuration\RateLimitingOptions.cs` | — |  |
| `RateLimitingOptions` | class | `Configuration\RateLimitingOptions.cs` | — |  |
| `RateLimitKeySource` | enum | `Configuration\RateLimitingOptions.cs` | — |  |
| `RateLimitPolicy` | class | `Configuration\RateLimitingOptions.cs` | — |  |
| `ReferrerPolicyValue` | enum | `Configuration\SecurityHeadersOptions.cs` | — |  |
| `RequestContextOptions` | class | `Configuration\RequestContextOptions.cs` | — |  |
| `RequestDecompressionOptions` | class | `Configuration\RequestDecompressionOptions.cs` | — |  |
| `RequestLoggingLevel` | enum | `Configuration\RequestLoggingOptions.cs` | — |  |
| `RequestLoggingOptions` | class | `Configuration\RequestLoggingOptions.cs` | — |  |
| `RequestSizeLimitOptions` | class | `Configuration\RequestSizeLimitOptions.cs` | — |  |
| `RequestTelemetryOptions` | class | `Configuration\RequestTelemetryOptions.cs` | — |  |
| `RequestTimeoutOptions` | class | `Configuration\RequestTimeoutOptions.cs` | — |  |
| `ResponseCacheLocation` | enum | `Configuration\ResponseCachingOptions.cs` | — |  |
| `ResponseCachingOptions` | class | `Configuration\ResponseCachingOptions.cs` | — |  |
| `SanitizationMode` | enum | `Configuration\InputSanitizationOptions.cs` | — |  |
| `SecurityHeadersOptions` | class | `Configuration\SecurityHeadersOptions.cs` | — |  |
| `SwaggerContact` | class | `Configuration\SwaggerOptions.cs` | — |  |
| `SwaggerLicense` | class | `Configuration\SwaggerOptions.cs` | — |  |
| `SwaggerOptions` | class | `Configuration\SwaggerOptions.cs` | — |  |
| `SwaggerVersionInfo` | class | `Configuration\SwaggerOptions.cs` | — |  |
| `XFrameOptionsValue` | enum | `Configuration\SecurityHeadersOptions.cs` | — |  |
| `XmlSerializationOptions` | class | `Configuration\ContentNegotiationOptions.cs` | — |  |
| `XssProtectionMode` | enum | `Configuration\SecurityHeadersOptions.cs` | — |  |

#### ContentNegotiation/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AcceptHeaderNegotiator` | class | `ContentNegotiation\AcceptHeaderNegotiator.cs` | — |  |
| `ContentFormatterRegistry` | class | `ContentNegotiation\ContentFormatterRegistry.cs` | — |  |
| `ContentNegotiationBuilder` | class | `ContentNegotiation\ContentNegotiationBuilder.cs` | — |  |
| `ContentNegotiationResult` | class | `ContentNegotiation\AcceptHeaderNegotiator.cs` | — |  |
| `JsonContentFormatter` | class | `ContentNegotiation\JsonContentFormatter.cs` | — |  |
| `MediaTypeEntry` | class | `ContentNegotiation\AcceptHeaderNegotiator.cs` | — |  |
| `ProblemDetailsJsonFormatter` | class | `ContentNegotiation\ProblemDetailsFormatters.cs` | — |  |
| `ProblemDetailsXmlFormatter` | class | `ContentNegotiation\ProblemDetailsFormatters.cs` | — |  |
| `XmlContentFormatter` | class | `ContentNegotiation\XmlContentFormatter.cs` | — |  |

#### Conventions/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ApiConventions` | class | `Conventions\ApiConventions.cs` | — |  |

#### Endpoints/ (9)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CorrelationIdEndpointFilter` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `ExceptionHandlingEndpointFilter` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `ExceptionHandlingEndpointFilterFactory` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `IdempotencyEndpointFilter` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `LoggingEndpointFilter` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `NativeValidationEndpointFilter` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `TimeoutEndpointFilter` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `TimeoutEndpointFilterFactory` | class | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `ValidationEndpointFilter` | class | `Endpoints\Filters\ValidationEndpointFilter.cs` | — |  |

#### Exceptions/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CompositeExceptionToProblemDetailsMapper` | class | `Exceptions\CompositeExceptionToProblemDetailsMapper.cs` | — |  |
| `DefaultExceptionToProblemDetailsMapper` | class | `Exceptions\DefaultExceptionToProblemDetailsMapper.cs` | — |  |
| `ValidationProblemDetailsMapper` | class | `Exceptions\ValidationProblemDetailsMapper.cs` | — |  |

#### Extensions/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ServiceCollectionExtentions` | class | `Extensions\ServiceCollectionExtentions.cs` | — |  |

#### Filters/ (11)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AuthResponsesOperationFilter` | class | `Filters\Swagger\AuthResponsesOperationFilter.cs` | — |  |
| `ContentNegotiationResultFilter` | class | `Filters\ContentNegotiationResultFilter.cs` | — |  |
| `CustomSwaggerFilter` | class | `Filters\Swagger\CustomSwaggerFilter.cs` | — |  |
| `DeprecationOperationFilter` | class | `Filters\Swagger\DeprecationOperationFilter.cs` | — |  |
| `ExamplesOperationFilter` | class | `Filters\Swagger\ExamplesOperationFilter.cs` | — |  |
| `ModelStateValidationFilter` | class | `Filters\ModelStateValidationFilter.cs` | — |  |
| `ProblemDetailsResultFilter` | class | `Filters\ProblemDetailsResultFilter.cs` | — |  |
| `ProducesContentTypeAttribute` | class | `Filters\ContentNegotiationResultFilter.cs` | — |  |
| `RequireAcceptableMediaTypeAttribute` | class | `Filters\ContentNegotiationResultFilter.cs` | — |  |
| `VersionedSwaggerDocumentFilter` | class | `Filters\Swagger\VersionedSwaggerDocumentFilter.cs` | — |  |
| `VersionedSwaggerOperationFilter` | class | `Filters\Swagger\VersionedSwaggerDocumentFilter.cs` | — |  |

#### HealthChecks/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `BaseHealthCheck` | class | `HealthChecks\BaseHealthCheck.cs` | — |  |
| `CacheHealthCheck` | class | `HealthChecks\CacheHealthCheck.cs` | — |  |
| `CacheHealthCheckOptions` | class | `HealthChecks\CacheHealthCheck.cs` | — |  |

#### Http/ (4)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AsyncLocalCorrelationContextProvider` | class | `Http\CorrelationIdHandler.cs` | — |  |
| `CorrelationContext` | record | `Http\CorrelationIdHandler.cs` | — |  |
| `CorrelationIdHandler` | class | `Http\CorrelationIdHandler.cs` | — |  |
| `CorrelationIdPropagatingHandler` | class | `Http\CorrelationIdHandler.cs` | — |  |

#### Idempotency/ (8)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CqrsIdempotencyKeyGenerator` | class | `Idempotency\CqrsIdempotencyKeyGenerator.cs` | — |  |
| `DefaultIdempotencyKeyGenerator` | class | `Idempotency\DefaultIdempotencyKeyGenerator.cs` | — |  |
| `DistributedCacheIdempotencyStore` | class | `Idempotency\DistributedCacheIdempotencyStore.cs` | — |  |
| `IdempotencyKeyResult` | class | `Idempotency\IIdempotencyKeyGenerator.cs` | — |  |
| `IdempotencyLockResult` | class | `Idempotency\IIdempotencyStore.cs` | — |  |
| `IdempotencyRecord` | class | `Idempotency\IIdempotencyStore.cs` | — |  |
| `IdempotencyRecordStatus` | enum | `Idempotency\IIdempotencyStore.cs` | — |  |
| `InMemoryIdempotencyStore` | class | `Idempotency\InMemoryIdempotencyStore.cs` | — |  |

#### Middlewares/ (24)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `AntiForgeryMiddleware` | class | `Middlewares\AntiForgeryMiddleware.cs` | — |  |
| `ApiKeyAuthenticationMiddleware` | class | `Middlewares\ApiKeyAuthenticationMiddleware.cs` | — |  |
| `ApiKeyRequiredAttribute` | class | `Middlewares\ApiKeyAuthenticationMiddleware.cs` | — |  |
| `CacheControlMiddleware` | class | `Middlewares\CacheControlMiddleware.cs` | — |  |
| `CachingMiddleware` | class | `Middlewares\CachingMiddleware.cs` | — |  |
| `ContentNegotiationMiddleware` | class | `Middlewares\ContentNegotiationMiddleware.cs` | — |  |
| `CorrelationIdMiddleware` | class | `Middlewares\CorrelationIdMiddleware.cs` | — |  |
| `CorsMiddleware` | class | `Middlewares\CorsMiddleware.cs` | — |  |
| `ETagMiddleware` | class | `Middlewares\ETagMiddleware.cs` | — |  |
| `ExceptionMiddleware` | class | `Middlewares\ExceptionMiddleware.cs` | — |  |
| `IdempotencyMiddleware` | class | `Middlewares\IdempotencyMiddleware.cs` | — |  |
| `InputSanitizationMiddleware` | class | `Middlewares\InputSanitizationMiddleware.cs` | — |  |
| `IpFilteringMiddleware` | class | `Middlewares\IpFilteringMiddleware.cs` | — |  |
| `ProblemDetailsMiddleware` | class | `Middlewares\ProblemDetailsMiddleware.cs` | — |  |
| `RateLimitingMiddleware` | class | `Middlewares\RateLimitingMiddleware.cs` | — |  |
| `RequestContextKeys` | class | `Middlewares\RequestContextMiddleware.cs` | — |  |
| `RequestContextMiddleware` | class | `Middlewares\RequestContextMiddleware.cs` | — |  |
| `RequestDecompressionMiddleware` | class | `Middlewares\RequestDecompressionMiddleware.cs` | — |  |
| `RequestLoggingMiddleware` | class | `Middlewares\RequestLoggingMiddleware.cs` | — |  |
| `RequestSizeLimitMiddleware` | class | `Middlewares\RequestSizeLimitMiddleware.cs` | — |  |
| `RequestTelemetryMiddleware` | class | `Middlewares\RequestTelemetryMiddleware.cs` | — |  |
| `RequestTimeoutMiddleware` | class | `Middlewares\RequestTimeoutMiddleware.cs` | — |  |
| `ResponseTransformMiddleware` | class | `Middlewares\ContentNegotiationMiddleware.cs` | — |  |
| `SecurityHeadersMiddleware` | class | `Middlewares\SecurityHeadersMiddleware.cs` | — |  |

#### Models/ (1)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `SwaggerAuthorizationScheme` | enum | `Models\SwaggerAuthorizationScheme.cs` | — |  |

#### Observability/ (3)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `ActivityNames` | class | `Observability\WebApiActivitySource.cs` | — |  |
| `TagNames` | class | `Observability\WebApiActivitySource.cs` | — |  |
| `WebApiActivitySource` | class | `Observability\WebApiActivitySource.cs` | — |  |

#### OpenApi/ (7)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `CommonResponsesTransformer` | class | `OpenApi\OpenApiDocumentTransformers.cs` | — |  |
| `CustomHeadersTransformer` | class | `OpenApi\OpenApiDocumentTransformers.cs` | — |  |
| `DeprecationTransformer` | class | `OpenApi\OpenApiDocumentTransformers.cs` | — |  |
| `ProblemDetailsTransformer` | class | `OpenApi\OpenApiDocumentTransformers.cs` | — |  |
| `RateLimitHeadersTransformer` | class | `OpenApi\OpenApiDocumentTransformers.cs` | — |  |
| `SecuritySchemeTransformer` | class | `OpenApi\SecuritySchemeTransformer.cs` | — |  |
| `TagFilterTransformer` | class | `OpenApi\OpenApiDocumentTransformers.cs` | — |  |

#### RateLimiting/ (5)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DefaultRateLimitKeyGenerator` | class | `RateLimiting\DefaultRateLimitKeyGenerator.cs` | — |  |
| `DistributedRateLimitResult` | class | `RateLimiting\IDistributedRateLimiter.cs` | — |  |
| `InMemoryRateLimiter` | class | `RateLimiting\RedisDistributedRateLimiter.cs` | — |  |
| `RateLimitPartitionResolver` | class | `RateLimiting\RateLimitPartitionResolver.cs` | — |  |
| `RedisDistributedRateLimiter` | class | `RateLimiting\RedisDistributedRateLimiter.cs` | — |  |

#### Services/ (4)

| Tipo | Kind | Arquivo | Cov% | Test hint |
|------|------|---------|-----:|:---------:|
| `DefaultRequestLogger` | class | `Services\DefaultRequestLogger.cs` | — |  |
| `ExceptionLogData` | record | `Services\IRequestLogger.cs` | — |  |
| `RequestLogData` | record | `Services\IRequestLogger.cs` | — |  |
| `ResponseLogData` | record | `Services\IRequestLogger.cs` | — |  |

---

## Prioridade 2 — Helpers / utilitários

| Tipo | Projeto | Arquivo | Cov% | Test hint |
|------|---------|---------|-----:|:---------:|
| `PaginationHelper` | `Mvp24Hours.Application` | `Logic\Pagination\PaginationHelper.cs` | — | sim |
| `ContantsHelper` | `Mvp24Hours.Core` | `Helpers\ContantsHelper.cs` | — |  |
| `JsonHelper` | `Mvp24Hours.Core` | `Helpers\JsonHelper.cs` | — | sim |
| `ObjectHelper` | `Mvp24Hours.Core` | `Helpers\ObjectHelper.cs` | — | sim |
| `StringHelper` | `Mvp24Hours.Core` | `Helpers\StringHelper.cs` | — | sim |
| `TelemetryHelper` | `Mvp24Hours.Core` | `Helpers\TelemetryHelper.cs` | — |  |
| `PeriodicTimerHelper` | `Mvp24Hours.Core` | `Infrastructure\Timers\PeriodicTimerHelper.cs` | — |  |
| `ActivityHelper` | `Mvp24Hours.Core` | `Observability\ActivityHelper.cs` | — |  |
| `ConfigurationHelper` | `Mvp24Hours.Infrastructure` | `Helpers\ConfigurationHelper.cs` | — |  |
| `DirectoryHelper` | `Mvp24Hours.Infrastructure` | `Helpers\DirectoryHelper.cs` | — |  |
| `EncryptionHelper` | `Mvp24Hours.Infrastructure` | `Helpers\EncryptionHelper.cs` | — |  |
| `FileLogHelper` | `Mvp24Hours.Infrastructure` | `Helpers\FileLogHelper.cs` | — |  |
| `HttpPolicyHelper` | `Mvp24Hours.Infrastructure` | `Helpers\HttpPolicyHelper.cs` | — |  |
| `TimeZoneHelper` | `Mvp24Hours.Infrastructure` | `Helpers\TimeZoneHelper.cs` | — |  |
| `WebRequestHelper` | `Mvp24Hours.Infrastructure` | `Helpers\WebRequestHelper.cs` | — |  |
| `CertificateHelper` | `Mvp24Hours.Infrastructure` | `Http\Helpers\CertificateHelper.cs` | — |  |
| `MultipartFormDataHelper` | `Mvp24Hours.Infrastructure` | `Http\Helpers\MultipartFormDataHelper.cs` | — |  |
| `ObservabilityHelper` | `Mvp24Hours.Infrastructure` | `Observability\Helpers\ObservabilityHelper.cs` | — |  |
| `RetryHelper` | `Mvp24Hours.Infrastructure` | `Resilience\Helpers\RetryHelper.cs` | — |  |
| `JitterStrategy` | `Mvp24Hours.Infrastructure` | `Resilience\Helpers\RetryWithJitter.cs` | — |  |
| `RetryWithJitter` | `Mvp24Hours.Infrastructure` | `Resilience\Helpers\RetryWithJitter.cs` | — |  |
| `SecretRotationHelper` | `Mvp24Hours.Infrastructure` | `Security\Helpers\SecretRotationHelper.cs` | — |  |
| `SensitiveDataMasker` | `Mvp24Hours.Infrastructure` | `Security\Helpers\SensitiveDataMasker.cs` | — |  |
| `FakeTimeProviderHelper` | `Mvp24Hours.Infrastructure` | `Testing\FakeTimeProviderHelper.cs` | — |  |
| `RowLevelSecurityHelper` | `Mvp24Hours.Infrastructure.Data.EFCore` | `Security\RowLevelSecurityHelper.cs` | — |  |
| `InMemoryDbContextHelper` | `Mvp24Hours.Infrastructure.Data.EFCore` | `Testing\InMemoryDbContextFactory.cs` | — |  |
| `EncryptionKeyHelper` | `Mvp24Hours.Infrastructure.Data.MongoDb` | `Security\FieldEncryption.cs` | — |  |
| `MongoDbContextHelper` | `Mvp24Hours.Infrastructure.Data.MongoDb` | `Testing\MongoDbContextFactory.cs` | — |  |
| `MongoDbTestcontainersHelper` | `Mvp24Hours.Infrastructure.Data.MongoDb` | `Testing\MongoDbTestcontainersHelper.cs` | — |  |
| `OperationPriorityHelper` | `Mvp24Hours.Infrastructure.Pipe` | `AdvancedFlow\Priority\OperationPriority.cs` | — |  |
| `StateSnapshotHelper` | `Mvp24Hours.Infrastructure.Pipe` | `Context\StateSnapshotHelper.cs` | — |  |
| `BatchProcessingHelper` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Consumers\BatchProcessingHelper.cs` | — |  |
| `TenantDeadLetterQueueHelper` | `Mvp24Hours.Infrastructure.RabbitMQ` | `MultiTenancy\TenantDeadLetterQueueHelper.cs` | — |  |
| `CronExpressionHelper` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Scheduling\CronExpressionHelper.cs` | — |  |
| `TestHarnessBuilder` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Testing\Helpers\TestHarnessBuilder.cs` | — |  |
| `TestMessageHelpers` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Testing\Helpers\TestMessageHelpers.cs` | — |  |
| `AutoBindingHelper` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Topology\AutoBindingHelper.cs` | — |  |
| `ExchangeBindingHelper` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Topology\ExchangeBindingHelper.cs` | — |  |
| `FanoutExchangeHelper` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Topology\FanoutExchangeHelper.cs` | — |  |
| `TopicExchangeHelper` | `Mvp24Hours.Infrastructure.RabbitMQ` | `Topology\TopicExchangeHelper.cs` | — |  |

---

## Prioridade 3 — Extensions

### `Mvp24Hours.Application` (14)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `ApplicationEventServiceCollectionExtensions` | `Extensions\ApplicationEventServiceCollectionExtensions.cs` | — |  |
| `ApplicationModuleServiceCollectionExtensions` | `Extensions\ApplicationModuleServiceCollectionExtensions.cs` | — |  |
| `ApplicationServiceCollectionExtensions` | `Extensions\ApplicationServiceCollectionExtensions.cs` | — |  |
| `BulkServiceCollectionExtensions` | `Extensions\BulkServiceCollectionExtensions.cs` | — |  |
| `BusinessResultWithStatusExtensions` | `Extensions\BusinessResultWithStatusExtensions.cs` | — |  |
| `CacheServiceCollectionExtensions` | `Extensions\CacheServiceCollectionExtensions.cs` | — |  |
| `ConventionBasedServiceCollectionExtensions` | `Extensions\ConventionBasedServiceCollectionExtensions.cs` | — |  |
| `ObservabilityServiceCollectionExtensions` | `Extensions\ObservabilityServiceCollectionExtensions.cs` | — |  |
| `PagedResultExtensions` | `Extensions\PagedResultExtensions.cs` | — |  |
| `PaginationServiceCollectionExtensions` | `Extensions\PaginationServiceCollectionExtensions.cs` | — |  |
| `ResilienceServiceCollectionExtensions` | `Extensions\ResilienceServiceCollectionExtensions.cs` | — |  |
| `SpecificationServiceCollectionExtensions` | `Extensions\SpecificationServiceCollectionExtensions.cs` | — |  |
| `TransactionServiceCollectionExtensions` | `Extensions\TransactionServiceCollectionExtensions.cs` | — |  |
| `ValidationServiceCollectionExtensions` | `Extensions\ValidationServiceCollectionExtensions.cs` | — |  |

### `Mvp24Hours.Core` (49)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `ActivityExtensions` | `Observability\ActivityExtensions.cs` | — |  |
| `AspireComponentExtensions` | `Aspire\AspireComponentExtensions.cs` | — |  |
| `AspireHostingExtensions` | `Aspire\AspireHostingExtensions.cs` | — |  |
| `AutoMappingAsyncExtensions` | `Extensions\Mapping\AutoMappingAsyncExtensions.cs` | — |  |
| `AutoMappingExtensions` | `Extensions\Mapping\AutoMappingExtensions.cs` | — |  |
| `BusinessAsyncExtensions` | `Extensions\Logic\BusinessAsyncExtensions.cs` | — |  |
| `BusinessEventExtensions` | `Extensions\Logic\BusinessEventExtensions.cs` | — |  |
| `BusinessExtensions` | `Extensions\Logic\BusinessExtensions.cs` | — |  |
| `BusinessPagingAsyncExtensions` | `Extensions\Logic\BusinessPagingAsyncExtensions.cs` | — |  |
| `BusinessPagingExtensions` | `Extensions\Logic\BusinessPagingExtensions.cs` | — |  |
| `BusinessResultFunctionalExtensions` | `Extensions\Logic\BusinessResultFunctionalExtensions.cs` | — |  |
| `ChannelServiceExtensions` | `Extensions\ChannelServiceExtensions.cs` | — |  |
| `EnumerableExtensions` | `Extensions\EnumerableExtensions.cs` | — | sim |
| `EnumExtensions` | `Extensions\EnumExtensions.cs` | — |  |
| `ExceptionExtensions` | `Extensions\Exceptions\ExceptionExtensions.cs` | — |  |
| `GenerateKeyExtensions` | `Extensions\GenerateKeyExtensions.cs` | — |  |
| `GuardClauseExtensions` | `Helpers\Guard.cs` | — |  |
| `GuidExtensions` | `Extensions\GuidExtensions.cs` | — |  |
| `JsonExtensions` | `Extensions\Data\JsonExtensions.cs` | — |  |
| `KeyedServiceExtensions` | `Extensions\KeyedServices\KeyedServiceExtensions.cs` | — |  |
| `LoggerTraceContextExtensions` | `Observability\LoggingServiceExtensions.cs` | — |  |
| `LoggingBuilderOpenTelemetryExtensions` | `Observability\OpenTelemetryLoggingBuilderExtensions.cs` | — |  |
| `LoggingServiceExtensions` | `Observability\LoggingServiceExtensions.cs` | — |  |
| `MaybeExtensions` | `Extensions\Functional\MaybeExtensions.cs` | — |  |
| `MessageResultExtensions` | `Extensions\Logic\MessageResultExtensions.cs` | — |  |
| `MetricsServiceExtensions` | `Observability\MetricsServiceExtensions.cs` | — |  |
| `Mvp24HoursOpenTelemetryIntegrationExtensions` | `Observability\OpenTelemetryExporterExtensions.cs` | — |  |
| `ObjectExtensions` | `Extensions\ObjectExtensions.cs` | — |  |
| `ObservabilityServiceExtensions` | `Observability\ObservabilityServiceExtensions.cs` | — |  |
| `OpenTelemetryBuilderExtensions` | `Observability\TracingServiceExtensions.cs` | — |  |
| `OpenTelemetryExporterExtensions` | `Observability\OpenTelemetryExporterExtensions.cs` | — |  |
| `OpenTelemetryLoggingBuilderExtensions` | `Observability\OpenTelemetryLoggingBuilderExtensions.cs` | — |  |
| `OpenTelemetryLoggingExtensions` | `Observability\OpenTelemetryLoggingExtensions.cs` | — |  |
| `OpenTelemetryMeterBuilderExtensions` | `Observability\MetricsServiceExtensions.cs` | — |  |
| `OptionsValidationExtensions` | `Extensions\Options\OptionsValidationExtensions.cs` | — |  |
| `PeriodicTimerExtensions` | `Infrastructure\Timers\PeriodicTimerHelper.cs` | — |  |
| `QueryableExtensions` | `Extensions\Data\QueryableExtensions.cs` | — |  |
| `RateLimitingServiceExtensions` | `Extensions\RateLimitingServiceExtensions.cs` | — |  |
| `SourceGenerationServiceExtensions` | `Extensions\SourceGeneration\SourceGenerationServiceExtensions.cs` | — |  |
| `SpecificationExtensions` | `Extensions\SpecificationExtensions.cs` | — |  |
| `SpecificationPagingExtensions` | `Extensions\SpecificationPagingExtensions.cs` | — |  |
| `StringExtensions` | `Extensions\StringExtensions.cs` | — | sim |
| `StructuredLoggingExtensions` | `Observability\OpenTelemetryLoggingBuilderExtensions.cs` | — |  |
| `TaskExtensions` | `Extensions\TaskExtensions.cs` | — |  |
| `TelemetryExtensions` | `Extensions\TelemetryExtensions.cs` | — |  |
| `TimeProviderServiceExtensions` | `Extensions\TimeProviderServiceExtensions.cs` | — |  |
| `TracingServiceExtensions` | `Observability\TracingServiceExtensions.cs` | — |  |
| `ValidatorEntityExtensions` | `Extensions\Validation\ValidatorEntityExtensions.cs` | — |  |
| `ValidatorNumberExtensions` | `Extensions\Validation\ValidatorNumberExtensions.cs` | — |  |

### `Mvp24Hours.Infrastructure` (34)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `ActivityExtensions` | `Extensions\ActivityExtensions.cs` | — |  |
| `BackgroundJobsServiceExtensions` | `BackgroundJobs\Extensions\BackgroundJobsServiceExtensions.cs` | — |  |
| `CircuitBreakerOptionsExtensions` | `Http\DelegatingHandlers\CircuitBreakerDelegatingHandler.cs` | — |  |
| `CompressionExtensions` | `Http\DelegatingHandlers\CompressionDelegatingHandler.cs` | — |  |
| `DistributedLockingServiceExtensions` | `DistributedLocking\Extensions\DistributedLockingServiceExtensions.cs` | — |  |
| `EmailServiceExtensions` | `Email\Extensions\EmailServiceExtensions.cs` | — |  |
| `FileStorageAdvancedExtensions` | `FileStorage\Advanced\FileStorageAdvancedExtensions.cs` | — |  |
| `FileStorageServiceExtensions` | `FileStorage\Extensions\FileStorageServiceExtensions.cs` | — |  |
| `HttpClientExtensions` | `Extensions\HttpClientExtensions.cs` | — |  |
| `HttpClientResilienceExtensions` | `Http\Resilience\HttpClientResilienceExtensions.cs` | — |  |
| `HttpClientSerializationExtensions` | `Http\Extensions\HttpClientSerializationExtensions.cs` | — |  |
| `HttpClientServiceExtensions` | `Http\Extensions\HttpClientServiceExtensions.cs` | — |  |
| `HttpContextExtensions` | `Extensions\HttpContextExtensions.cs` | — |  |
| `HttpRequestMessageTimeoutExtensions` | `Http\DelegatingHandlers\TimeoutDelegatingHandler.cs` | — |  |
| `HttpResponseExtensions` | `Http\Extensions\HttpResponseExtensions.cs` | — |  |
| `InfrastructureHealthCheckExtensions` | `HealthChecks\InfrastructureHealthCheckExtensions.cs` | — |  |
| `InfrastructureServiceExtensions` | `Configuration\InfrastructureServiceExtensions.cs` | — |  |
| `LoggingExtensions` | `Security\Extensions\LoggingExtensions.cs` | — |  |
| `MemoryCacheExtensions` | `Extensions\MemoryCacheExtensions.cs` | — |  |
| `MultipartFormDataExtensions` | `Http\Helpers\MultipartFormDataHelper.cs` | — |  |
| `NativeHttpResilienceExtensions` | `Http\Resilience\NativeHttpResilienceExtensions.cs` | — |  |
| `NativeResilienceBuilderExtensions` | `Http\Resilience\NativeResilienceBuilder.cs` | — |  |
| `NativeResilienceServiceExtensions` | `Resilience\Native\NativeResilienceServiceExtensions.cs` | — |  |
| `ObservabilityOptions` | `Observability\Extensions\ObservabilityServiceExtensions.cs` | — |  |
| `ObservabilityServiceExtensions` | `Observability\Extensions\ObservabilityServiceExtensions.cs` | — |  |
| `PredicateExtensions` | `Extensions\PredicateExtensions.cs` | — |  |
| `SecurityServiceExtensions` | `Security\Extensions\SecurityServiceExtensions.cs` | — |  |
| `ServiceCollectionExtensions` | `Extensions\ServiceCollectionExtensions.cs` | — |  |
| `SmsServiceExtensions` | `Sms\Extensions\SmsServiceExtensions.cs` | — |  |
| `StandardHandlersOptions` | `Http\Extensions\HttpClientServiceExtensions.cs` | — |  |
| `StructuredLoggingExtensions` | `Observability\Logging\StructuredLoggingExtensions.cs` | — |  |
| `TemplateEngine` | `Email\Extensions\EmailServiceExtensions.cs` | — |  |
| `TestInfrastructureOptions` | `Testing\Extensions\TestingServiceExtensions.cs` | — |  |
| `TestingServiceExtensions` | `Testing\Extensions\TestingServiceExtensions.cs` | — |  |

### `Mvp24Hours.Infrastructure.Caching` (17)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `CacheAsideExtensions` | `Patterns\CacheAsideExtensions.cs` | — |  |
| `CacheAsyncExtensions` | `Extensions\CacheAsyncExtensions.cs` | — |  |
| `CacheExtensions` | `Extensions\CacheExtensions.cs` | — |  |
| `CacheInvalidationExtensions` | `Extensions\CacheInvalidationExtensions.cs` | — |  |
| `CacheInvalidationServiceExtensions` | `Extensions\CacheInvalidationServiceExtensions.cs` | — |  |
| `CachePatternExtensions` | `Patterns\CachePatternExtensions.cs` | — |  |
| `CachePerformanceExtensions` | `Extensions\CachePerformanceExtensions.cs` | — |  |
| `CacheProviderExtensions` | `Extensions\CacheProviderExtensions.cs` | — |  |
| `CacheResilienceExtensions` | `Resilience\CacheResilienceExtensions.cs` | — |  |
| `CachingServiceExtensions` | `Extensions\CachingServiceExtensions.cs` | — |  |
| `HybridCacheExtensions` | `HybridCache\HybridCacheExtensions.cs` | — |  |
| `HybridCacheServiceExtensions` | `HybridCache\HybridCacheServiceExtensions.cs` | — |  |
| `MultiLevelCacheExtensions` | `Extensions\MultiLevelCacheExtensions.cs` | — |  |
| `MvpCachingExtensions` | `Extensions\MvpCachingExtensions.cs` | — |  |
| `ObjectCacheAsyncExtensions` | `Extensions\ObjectCacheAsyncExtensions.cs` | — |  |
| `ObjectCacheExtensions` | `Extensions\ObjectCacheExtensions.cs` | — |  |
| `ObservabilityExtensions` | `Extensions\ObservabilityExtensions.cs` | — |  |

### `Mvp24Hours.Infrastructure.Cqrs` (5)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `DomainEventExtensions` | `Extensions\DomainEventExtensions.cs` | 0 |  |
| `DomainToIntegrationEventExtensions` | `Extensions\DomainToIntegrationEventExtensions.cs` | 0 |  |
| `MediatorCachingExtensions` | `Extensions\MediatorCachingExtensions.cs` | 0 |  |
| `NativeResilienceBehaviorExtensions` | `Behaviors\NativeResilienceBehavior.cs` | 0 |  |
| `RetryPolicyExtensions` | `Behaviors\RetryBehavior.cs` | 0 |  |

### `Mvp24Hours.Infrastructure.CronJob` (4)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `CronJobAdvancedExtensions` | `Extensions\CronJobAdvancedExtensions.cs` | 0 |  |
| `CronJobConfigurationExtensions` | `Configuration\CronJobConfigurationExtensions.cs` | 0 |  |
| `CronJobObservabilityExtensions` | `Observability\CronJobObservabilityExtensions.cs` | 0 |  |
| `ScheduledServiceExtensions` | `Extensions\ScheduledServiceExtensions.cs` | 0 |  |

### `Mvp24Hours.Infrastructure.Data.EFCore` (25)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `BulkOperationsExtensions` | `Extensions\BulkOperationsExtensions.cs` | — |  |
| `CompiledQueryExtensions` | `Extensions\CompiledQueryExtensions.cs` | — |  |
| `ConcurrencyModelBuilderExtensions` | `Interceptors\ConcurrencyInterceptor.cs` | — |  |
| `EFCoreActivityExtensions` | `Observability\EFCoreActivitySource.cs` | — |  |
| `EFCoreCqrsIntegrationExtensions` | `Extensions\EFCoreCqrsIntegrationExtensions.cs` | — |  |
| `EFCoreObservabilityExtensions` | `Extensions\EFCoreObservabilityExtensions.cs` | — |  |
| `EFCoreServiceExtensions` | `Extensions\EFCoreServiceExtensions.cs` | — |  |
| `EncryptedModelBuilderExtensions` | `Converters\EncryptedValueConverters.cs` | — |  |
| `EncryptedPropertyExtensions` | `Converters\EncryptedValueConverters.cs` | — |  |
| `EntityIdModelBuilderExtensions` | `Extensions\EntityIdModelBuilderExtensions.cs` | — |  |
| `HealthCheckExtensions` | `HealthChecks\HealthCheckExtensions.cs` | — |  |
| `MigrationExtensions` | `Migrations\MigrationExtensions.cs` | — |  |
| `ModelBuilderExtensions` | `Extensions\ModelBuilderExtensions.cs` | — |  |
| `NativeDbResilienceExtensions` | `Resilience\NativeDbResilienceExtensions.cs` | — |  |
| `ProjectionExtensions` | `Extensions\ProjectionExtensions.cs` | — |  |
| `QueryPerformanceExtensions` | `Extensions\QueryPerformanceExtensions.cs` | — |  |
| `QueryTimeoutExtensions` | `Extensions\QueryTimeoutExtensions.cs` | — |  |
| `QueryTrackingExtensions` | `Extensions\QueryTrackingExtensions.cs` | — |  |
| `ReadWriteExtensions` | `ReadWriteSplitting\ReadWriteExtensions.cs` | — |  |
| `ResilienceDbContextExtensions` | `Extensions\ResilienceDbContextExtensions.cs` | — |  |
| `RowLevelSecurityDbContextExtensions` | `Security\RowLevelSecurityHelper.cs` | — |  |
| `SchemaValidationExtensions` | `SchemaValidation\SchemaValidationExtensions.cs` | — |  |
| `SoftDeleteModelBuilderExtensions` | `Interceptors\SoftDeleteInterceptor.cs` | — |  |
| `TenantModelBuilderExtensions` | `Extensions\TenantModelBuilderExtensions.cs` | — |  |
| `TestingExtensions` | `Testing\TestingExtensions.cs` | — |  |

### `Mvp24Hours.Infrastructure.Data.MongoDb` (24)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `MongoDbAdvancedExtensions` | `Advanced\MongoDbAdvancedExtensions.cs` | — |  |
| `MongoDbAggregationExtensions` | `Extensions\MongoDbAggregationExtensions.cs` | — |  |
| `MongoDbAuthenticationExtensions` | `Security\MongoDbAuthenticationOptions.cs` | — |  |
| `MongoDbBsonExtensions` | `Extensions\MongoDbBsonExtensions.cs` | — |  |
| `MongoDbBulkOperationsExtensions` | `Extensions\MongoDbBulkOperationsExtensions.cs` | — |  |
| `MongoDbCollationExtensions` | `Advanced\Collation\MongoDbCollationExtensions.cs` | — |  |
| `MongoDbConcernExtensions` | `Advanced\Concerns\MongoDbConcernExtensions.cs` | — |  |
| `MongoDbCqrsIntegrationExtensions` | `Extensions\MongoDbCqrsIntegrationExtensions.cs` | — |  |
| `MongoDbHealthCheckExtensions` | `HealthChecks\MongoDbHealthCheckExtensions.cs` | — |  |
| `MongoDbInfrastructureExtensions` | `Infrastructure\MongoDbInfrastructureExtensions.cs` | — |  |
| `MongoDbInterceptorExtensions` | `Extensions\MongoDbInterceptorExtensions.cs` | — |  |
| `MongoDbObservabilityExtensions` | `Observability\MongoDbObservabilityExtensions.cs` | — |  |
| `MongoDbPaginationExtensions` | `Extensions\MongoDbPaginationExtensions.cs` | — |  |
| `MongoDbPerformanceExtensions` | `Extensions\MongoDbPerformanceExtensions.cs` | — |  |
| `MongoDbProfilingExtensions` | `Extensions\MongoDbProfilingExtensions.cs` | — |  |
| `MongoDbProjectionExtensions` | `Extensions\MongoDbProjectionExtensions.cs` | — |  |
| `MongoDbResiliencyExtensions` | `Extensions\MongoDbResiliencyExtensions.cs` | — | sim |
| `MongoDbServiceExtensions` | `Extensions\MongoDbServiceExtensions.cs` | — |  |
| `MongoDbSpecificationExtensions` | `Extensions\MongoDbSpecificationExtensions.cs` | — |  |
| `MongoDbStreamingExtensions` | `Extensions\MongoDbStreamingExtensions.cs` | — |  |
| `MongoDbTenantExtensions` | `Extensions\MongoDbTenantExtensions.cs` | — |  |
| `MongoDbTestingExtensions` | `Testing\MongoDbTestingExtensions.cs` | — |  |
| `NativeMongoDbResilienceExtensions` | `Resiliency\NativeMongoDbResilienceExtensions.cs` | — | sim |
| `RowLevelSecurityExtensions` | `Security\MongoDbRowLevelSecurity.cs` | — |  |

### `Mvp24Hours.Infrastructure.Pipe` (15)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `AdvancedFlowServiceExtensions` | `AdvancedFlow\AdvancedFlowServiceExtensions.cs` | — |  |
| `CachingExtensions` | `Integration\Caching\CachingExtensions.cs` | — |  |
| `FluentValidationExtensions` | `Integration\FluentValidation\FluentValidationExtensions.cs` | — |  |
| `NativePipelineResilienceExtensions` | `Resiliency\NativePipelineResilienceExtensions.cs` | — |  |
| `OpenTelemetryExtensions` | `Integration\OpenTelemetry\OpenTelemetryExtensions.cs` | — |  |
| `PipelineContextServiceExtensions` | `Context\PipelineContextServiceExtensions.cs` | — |  |
| `PipelineFluentExtensions` | `Extensions\PipelineFluentExtensions.cs` | — |  |
| `PipelineMessageContextExtensions` | `Context\PipelineMessageContextExtensions.cs` | — |  |
| `PipelineMessageExtensions` | `Extensions\PipelineMessageExtensions.cs` | — |  |
| `PipelineObservabilityExtensions` | `Observability\PipelineObservabilityExtensions.cs` | — |  |
| `PipelineResiliencyExtensions` | `Resiliency\PipelineResiliencyExtensions.cs` | — |  |
| `PipelineServiceExtensions` | `Extensions\PipelineServiceExtensions.cs` | — |  |
| `RateLimitingPipelineExtensions` | `Resiliency\RateLimitingPipelineExtensions.cs` | — |  |
| `StreamingExtensions` | `Integration\Streaming\StreamingExtensions.cs` | — |  |
| `TypedPipelineFluentExtensions` | `Typed\TypedPipelineFluentExtensions.cs` | — |  |

### `Mvp24Hours.Infrastructure.RabbitMQ` (18)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `BatchMessageResultExtensions` | `Consumers\BatchMessageResult.cs` | — |  |
| `CqrsSagaServiceExtensions` | `Saga\Extensions\CqrsSagaServiceExtensions.cs` | — |  |
| `EnhancedLoggingOptions` | `Observability\Extensions\ObservabilityServiceExtensions.cs` | — |  |
| `FilterPipelineExtensions` | `Pipeline\Extensions\FilterPipelineExtensions.cs` | — |  |
| `ObservabilityServiceExtensions` | `Observability\Extensions\ObservabilityServiceExtensions.cs` | — |  |
| `RabbitMQObservabilityOptions` | `Observability\Extensions\ObservabilityServiceExtensions.cs` | — |  |
| `RabbitMQServiceExtensions` | `Extensions\RabbitMQServiceExtensions.cs` | — |  |
| `RateLimitingExtensions` | `Pipeline\Extensions\RateLimitingExtensions.cs` | — |  |
| `SagaOptions` | `Saga\Extensions\SagaServiceExtensions.cs` | — |  |
| `SagaPersistenceType` | `Saga\Extensions\SagaServiceExtensions.cs` | — |  |
| `SagaServiceExtensions` | `Saga\Extensions\SagaServiceExtensions.cs` | — |  |
| `TenantRabbitMQServiceExtensions` | `MultiTenancy\Extensions\TenantRabbitMQServiceExtensions.cs` | — |  |
| `TestHarnessOptions` | `Testing\Extensions\TestingServiceExtensions.cs` | — |  |
| `TestingServiceExtensions` | `Testing\Extensions\TestingServiceExtensions.cs` | — |  |
| `TopologyOptions` | `Topology\Extensions\TopologyServiceExtensions.cs` | — |  |
| `TopologyServiceExtensions` | `Topology\Extensions\TopologyServiceExtensions.cs` | — |  |
| `TransactionalMessagingExtensions` | `Transactional\Extensions\TransactionalMessagingExtensions.cs` | — |  |
| `UnitOfWorkTransactionalExtensions` | `Transactional\UnitOfWorkTransactionalExtensions.cs` | — |  |

### `Mvp24Hours.WebAPI` (12)

| Tipo | Arquivo | Cov% | Test hint |
|------|---------|-----:|:---------:|
| `ApplicationBuilderExtensions` | `Extensions\ApplicationBuilderExtensions.cs` | — |  |
| `CorrelationIdExtensions` | `Extensions\CorrelationIdExtensions.cs` | — |  |
| `EndpointFilterExtensions` | `Endpoints\Filters\NativeEndpointFilters.cs` | — |  |
| `EndpointGroupExtensions` | `Endpoints\EndpointGroupExtensions.cs` | — |  |
| `HttpContextRequestContextExtensions` | `Middlewares\RequestContextMiddleware.cs` | — |  |
| `NativeMinimalApiEndpointExtensions` | `Endpoints\NativeMinimalApiEndpointExtensions.cs` | — |  |
| `NativeOpenApiApplicationBuilderExtensions` | `Extensions\NativeOpenApiApplicationBuilderExtensions.cs` | — |  |
| `NativeOpenApiServiceExtensions` | `Extensions\NativeOpenApiServiceExtensions.cs` | — |  |
| `NativeProblemDetailsExtensions` | `Extensions\NativeProblemDetailsExtensions.cs` | — |  |
| `NativeTypedResultsExtensions` | `Endpoints\NativeTypedResultsExtensions.cs` | — |  |
| `OutputCachingExtensions` | `Extensions\OutputCachingExtensions.cs` | — |  |
| `TypedResultsExtensions` | `Endpoints\TypedResultsExtensions.cs` | — |  |

---

## Prioridade 4 — Interfaces / contratos / options

Em geral cobertos indiretamente pelos testes das implementações. Inventário resumido por projeto (lista completa abaixo).

| Projeto | Total P4 | interfaces | class/record/enum/struct |
|---------|---------:|-----------:|-------------------------:|
| `Mvp24Hours.Application` | 37 | 37 | 0 |
| `Mvp24Hours.Core` | 167 | 167 | 0 |
| `Mvp24Hours.Infrastructure` | 85 | 60 | 25 |
| `Mvp24Hours.Infrastructure.Caching` | 2 | 2 | 0 |
| `Mvp24Hours.Infrastructure.Cqrs` | 111 | 111 | 0 |
| `Mvp24Hours.Infrastructure.CronJob` | 23 | 23 | 0 |
| `Mvp24Hours.Infrastructure.Data.EFCore` | 14 | 14 | 0 |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | 26 | 25 | 1 |
| `Mvp24Hours.Infrastructure.Pipe` | 23 | 23 | 0 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | 88 | 77 | 11 |
| `Mvp24Hours.WebAPI` | 14 | 13 | 1 |

<details>
<summary>Lista completa Prioridade 4 (interfaces/contratos)</summary>

### `Mvp24Hours.Application`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `IApplicationAuditStore` | interface | `Contract\Observability\IAuditableOperation.cs` |
| `IApplicationEvent` | interface | `Contract\Events\IApplicationEvent.cs` |
| `IApplicationEventDispatcher` | interface | `Contract\Events\IApplicationEventDispatcher.cs` |
| `IApplicationEventHandler` | interface | `Contract\Events\IApplicationEventHandler.cs` |
| `IApplicationEventHandlerSync` | interface | `Contract\Events\IApplicationEventHandler.cs` |
| `IApplicationEventOutbox` | interface | `Contract\Events\IApplicationEventOutbox.cs` |
| `IAuditableOperation` | interface | `Contract\Observability\IAuditableOperation.cs` |
| `IBusinessResultWithStatus` | interface | `Contract\Resilience\IBusinessResultWithStatus.cs` |
| `ICacheableQuery` | interface | `Contract\Cache\ICacheableQuery.cs` |
| `ICacheableQuery` | interface | `Contract\Cache\ICacheableQuery.cs` |
| `ICacheInvalidator` | interface | `Contract\Cache\ICacheInvalidator.cs` |
| `ICascadeValidator` | interface | `Contract\Validation\ICascadeValidator.cs` |
| `ICompositeCursor` | interface | `Contract\Pagination\ICursorPagedResult.cs` |
| `ICorrelationIdAccessor` | interface | `Contract\Observability\ICorrelationIdAccessor.cs` |
| `ICorrelationIdContext` | interface | `Contract\Observability\ICorrelationIdAccessor.cs` |
| `ICorrelationIdSetter` | interface | `Contract\Observability\ICorrelationIdAccessor.cs` |
| `ICursorPagedResult` | interface | `Contract\Pagination\ICursorPagedResult.cs` |
| `ICursorPagedResult` | interface | `Contract\Pagination\ICursorPagedResult.cs` |
| `IEntityApplicationEvent` | interface | `Contract\Events\IApplicationEvent.cs` |
| `IErrorMessageLocalizer` | interface | `Contract\Resilience\IErrorMessageLocalizer.cs` |
| `IExceptionToResultMapper` | interface | `Contract\Resilience\IExceptionToResultMapper.cs` |
| `IHasNestedValidation` | interface | `Contract\Validation\ICascadeValidator.cs` |
| `IOperationMetrics` | interface | `Contract\Observability\IOperationMetrics.cs` |
| `IPagedBusinessResult` | interface | `Contract\Pagination\IPagedResult.cs` |
| `IPagedResult` | interface | `Contract\Pagination\IPagedResult.cs` |
| `IPaginationService` | interface | `Contract\Pagination\IPaginationService.cs` |
| `IPaginationService` | interface | `Contract\Pagination\IPaginationService.cs` |
| `IQueryCacheKeyGenerator` | interface | `Contract\Cache\IQueryCacheKeyGenerator.cs` |
| `IQueryCacheProvider` | interface | `Contract\Cache\IQueryCacheProvider.cs` |
| `IResultMessage` | interface | `Contract\Resilience\IResultMessage.cs` |
| `ITransactionScope` | interface | `Contract\Transaction\ITransactionScope.cs` |
| `ITransactionScopeFactory` | interface | `Contract\Transaction\ITransactionScopeFactory.cs` |
| `ITransactionScopeSync` | interface | `Contract\Transaction\ITransactionScope.cs` |
| `IValidationPipeline` | interface | `Contract\Validation\IValidationPipeline.cs` |
| `IValidationPipelineBuilder` | interface | `Contract\Validation\IValidationPipeline.cs` |
| `IValidationService` | interface | `Contract\Validation\IValidationService.cs` |
| `IValidationStep` | interface | `Contract\Validation\IValidationPipeline.cs` |

### `Mvp24Hours.Core`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `IActivityEnricher` | interface | `Observability\IActivityEnricher.cs` |
| `IActivityEnricher` | interface | `Observability\IActivityEnricher.cs` |
| `IAggregateRoot` | interface | `Contract\Domain\Entity\IAggregateRoot.cs` |
| `IAggregateRoot` | interface | `Contract\Domain\Entity\IAggregateRoot.cs` |
| `IApplicationService` | interface | `Contract\Logic\IApplicationService.cs` |
| `IApplicationServiceAsync` | interface | `Contract\Logic\Async\IApplicationServiceAsync.cs` |
| `IApplicationServiceWithDto` | interface | `Contract\Logic\IApplicationServiceWithDto.cs` |
| `IApplicationServiceWithDtoAsync` | interface | `Contract\Logic\Async\IApplicationServiceWithDtoAsync.cs` |
| `IApplicationServiceWithSeparateDtos` | interface | `Contract\Logic\IApplicationServiceWithDtoSeparate.cs` |
| `IApplicationServiceWithSeparateDtosAsync` | interface | `Contract\Logic\Async\IApplicationServiceWithDtoSeparateAsync.cs` |
| `IAuditableEntity` | interface | `Contract\Domain\Entity\IAuditableEntity.cs` |
| `IAuditableEntity` | interface | `Contract\Domain\Entity\IAuditableEntity.cs` |
| `IBulkCommandServiceAsync` | interface | `Contract\Logic\Async\IBulkCommandServiceAsync.cs` |
| `IBulkCommandServiceWithDtoAsync` | interface | `Contract\Logic\Async\IBulkCommandServiceWithDtoAsync.cs` |
| `IBulkCommandServiceWithSeparateDtosAsync` | interface | `Contract\Logic\Async\IBulkCommandServiceWithDtoAsync.cs` |
| `IBulkheadOperation` | interface | `Contract\Infrastructure\Pipe\IBulkheadOperation.cs` |
| `IBulkOperationsAsync` | interface | `Contract\Data\Async\IBulkOperationsAsync.cs` |
| `IBulkOperationsRepositoryAsync` | interface | `Contract\Data\Async\IBulkOperationsRepositoryAsync.cs` |
| `IBusinessEvent` | interface | `Contract\ValueObjects\Logic\IBusinessEvent.cs` |
| `IBusinessResult` | interface | `Contract\ValueObjects\Logic\IBusinessResult.cs` |
| `ICacheCompressor` | interface | `Contract\Infrastructure\Caching\ICacheCompressor.cs` |
| `ICacheInvalidationEventPublisher` | interface | `Contract\Infrastructure\Caching\ICacheInvalidationEventPublisher.cs` |
| `ICacheInvalidationEventSubscriber` | interface | `Contract\Infrastructure\Caching\ICacheInvalidationEventSubscriber.cs` |
| `ICacheKeyGenerator` | interface | `Contract\Infrastructure\Caching\ICacheKeyGenerator.cs` |
| `ICachePrefetcher` | interface | `Contract\Infrastructure\Caching\ICachePrefetcher.cs` |
| `ICacheProvider` | interface | `Contract\Infrastructure\Caching\ICacheProvider.cs` |
| `ICacheSerializer` | interface | `Contract\Infrastructure\Caching\ICacheSerializer.cs` |
| `ICacheStampedePrevention` | interface | `Contract\Infrastructure\Caching\ICacheStampedePrevention.cs` |
| `ICacheSynchronizer` | interface | `Contract\Infrastructure\Caching\ICacheSynchronizer.cs` |
| `ICacheTagManager` | interface | `Contract\Infrastructure\Caching\ICacheTagManager.cs` |
| `ICacheWarmer` | interface | `Contract\Infrastructure\Caching\ICacheWarmer.cs` |
| `ICacheWarmupOperation` | interface | `Contract\Infrastructure\Caching\ICacheWarmer.cs` |
| `IChannel` | interface | `Contract\Infrastructure\Channels\IChannel.cs` |
| `IChannelFactory` | interface | `Contract\Infrastructure\Channels\IChannel.cs` |
| `IChannelMessage` | interface | `Contract\Infrastructure\Channels\IChannelMessage.cs` |
| `IChannelReader` | interface | `Contract\Infrastructure\Channels\IChannelReader.cs` |
| `IChannelWriter` | interface | `Contract\Infrastructure\Channels\IChannelWriter.cs` |
| `ICircuitBreakerOperation` | interface | `Contract\Infrastructure\Pipe\ICircuitBreakerOperation.cs` |
| `IClock` | interface | `Contract\Infrastructure\IClock.cs` |
| `ICommand` | interface | `Contract\Data\ICommand.cs` |
| `ICommandAsync` | interface | `Contract\Data\Async\ICommandAsync.cs` |
| `ICommandService` | interface | `Contract\Logic\ICommandService.cs` |
| `ICommandServiceAsync` | interface | `Contract\Logic\Async\ICommandServiceAsync.cs` |
| `IComposablePipeline` | interface | `Contract\Infrastructure\Pipe\IComposablePipeline.cs` |
| `IComposablePipelineAsync` | interface | `Contract\Infrastructure\Pipe\IComposablePipeline.cs` |
| `IConditionalBranch` | interface | `Contract\Infrastructure\Pipe\IConditionalBranch.cs` |
| `IConditionalBranchAsync` | interface | `Contract\Infrastructure\Pipe\IConditionalBranch.cs` |
| `IConditionalBranchBuilder` | interface | `Contract\Infrastructure\Pipe\IConditionalBranch.cs` |
| `ICorrelationIdAccessor` | interface | `Aspire\AspireServiceDefaults.cs` |
| `ICurrentUserProvider` | interface | `Contract\Infrastructure\ICurrentUserProvider.cs` |
| `IDeadLetterStore` | interface | `Contract\Infrastructure\Pipe\IDeadLetterStore.cs` |
| `IDomainEvent` | interface | `Contract\Domain\Entity\IDomainEvent.cs` |
| `IEncryptionProvider` | interface | `Contract\Infrastructure\IEncryptionProvider.cs` |
| `IEntity` | interface | `Contract\Domain\Entity\IEntity.cs` |
| `IEntity` | interface | `Contract\Domain\Entity\IEntity.cs` |
| `IEntityBase` | interface | `Contract\Domain\Entity\IEntityBase.cs` |
| `IEntityDateLog` | interface | `Contract\Domain\Entity\IEntityDateLog.cs` |
| `IEntityLog` | interface | `Contract\Domain\Entity\IEntityLog.cs` |
| `IExceptionMappingRule` | interface | `Contract\Infrastructure\Pipe\IPipelineExceptionMapper.cs` |
| `IExtendedEncryptionProvider` | interface | `Contract\Infrastructure\IEncryptionProvider.cs` |
| `IFallbackOperation` | interface | `Contract\Infrastructure\Pipe\IFallbackOperation.cs` |
| `IFallbackOperationSync` | interface | `Contract\Infrastructure\Pipe\IFallbackOperation.cs` |
| `IGuardClause` | interface | `Helpers\Guard.cs` |
| `IGuidGenerator` | interface | `Contract\Infrastructure\IGuidGenerator.cs` |
| `IHasDomainEvents` | interface | `Contract\Domain\Entity\IHasDomainEvents.cs` |
| `IKeyedService` | interface | `Contract\Infrastructure\DependencyInjection\IServiceLifetimeMarker.cs` |
| `IKeysetPageResult` | interface | `Contract\ValueObjects\Logic\IKeysetPageResult.cs` |
| `IKeysetPageResultString` | interface | `Contract\ValueObjects\Logic\IKeysetPageResult.cs` |
| `ILogContextAccessor` | interface | `Observability\LoggingServiceExtensions.cs` |
| `ILogEnricher` | interface | `Observability\LoggingServiceExtensions.cs` |
| `ILogSampler` | interface | `Observability\LoggingServiceExtensions.cs` |
| `IMapFrom` | interface | `Contract\Mappings\IMapFrom.cs` |
| `IMessageResult` | interface | `Contract\ValueObjects\Logic\IMessageResult.cs` |
| `IMultiLevelCache` | interface | `Contract\Infrastructure\Caching\IMultiLevelCache.cs` |
| `IOperation` | interface | `Contract\Infrastructure\Pipe\IOperation.cs` |
| `IOperationAsync` | interface | `Contract\Infrastructure\Pipe\Async\IOperationAsync.cs` |
| `IOperationAsyncWithCancellation` | interface | `Contract\Infrastructure\Pipe\IOperationWithTimeout.cs` |
| `IOperationResult` | interface | `Contract\Infrastructure\Pipe\IOperationResult.cs` |
| `IOperationResult` | interface | `Contract\Infrastructure\Pipe\IOperationResult.cs` |
| `IOperationWithTimeout` | interface | `Contract\Infrastructure\Pipe\IOperationWithTimeout.cs` |
| `IOptionsValidator` | interface | `Contract\Infrastructure\Options\IOptionsValidator.cs` |
| `IPageResult` | interface | `Contract\ValueObjects\Logic\IPageResult.cs` |
| `IPagingCriteria` | interface | `Contract\ValueObjects\Logic\IPaggingCriteria.cs` |
| `IPagingCriteriaExpression` | interface | `Contract\ValueObjects\Logic\IPaggingCriteriaExpression.cs` |
| `IPagingResult` | interface | `Contract\ValueObjects\Logic\IPagingResult.cs` |
| `IParallelOperationBuilder` | interface | `Contract\Infrastructure\Pipe\IParallelOperation.cs` |
| `IParallelOperationGroup` | interface | `Contract\Infrastructure\Pipe\IParallelOperation.cs` |
| `IParallelOperationGroupAsync` | interface | `Contract\Infrastructure\Pipe\IParallelOperation.cs` |
| `IPipeline` | interface | `Contract\Infrastructure\Pipe\IPipeline.cs` |
| `IPipelineAsync` | interface | `Contract\Infrastructure\Pipe\Async\IPipelineAsync.cs` |
| `IPipelineBuilder` | interface | `Contract\Application\Pipe\IPipelineBuilder.cs` |
| `IPipelineBuilderAsync` | interface | `Contract\Application\Pipe\Async\IPipelineBuilderAsync.cs` |
| `IPipelineExceptionMapper` | interface | `Contract\Infrastructure\Pipe\IPipelineExceptionMapper.cs` |
| `IPipelineMessage` | interface | `Contract\Infrastructure\Pipe\IPipelineMessage.cs` |
| `IPipelineMiddleware` | interface | `Contract\Infrastructure\Pipe\IPipelineMiddleware.cs` |
| `IPipelineMiddleware` | interface | `Contract\Infrastructure\Pipe\IPipelineMiddleware.cs` |
| `IPipelineMiddlewareSync` | interface | `Contract\Infrastructure\Pipe\IPipelineMiddleware.cs` |
| `IPipelineValidator` | interface | `Contract\Infrastructure\Pipe\IPipelineValidator.cs` |
| `IQuery` | interface | `Contract\Data\IQuery.cs` |
| `IQueryAsync` | interface | `Contract\Data\Async\IQueryAsync.cs` |
| `IQueryPagingService` | interface | `Contract\Logic\IQueryPagingService.cs` |
| `IQueryPagingServiceAsync` | interface | `Contract\Logic\Async\IQueryPagingServiceAsync.cs` |
| `IQueryRelation` | interface | `Contract\Data\IQueryRelation.cs` |
| `IQueryRelationAsync` | interface | `Contract\Data\Async\IQueryRelationAsync.cs` |
| `IQueryService` | interface | `Contract\Logic\IQueryService.cs` |
| `IQueryServiceAsync` | interface | `Contract\Logic\Async\IQueryServiceAsync.cs` |
| `IRateLimitedOperation` | interface | `Contract\Infrastructure\RateLimiting\IRateLimitedOperation.cs` |
| `IRateLimiterProvider` | interface | `Contract\Infrastructure\RateLimiting\IRateLimiterProvider.cs` |
| `IReadOnlyApplicationService` | interface | `Contract\Logic\IReadOnlyApplicationService.cs` |
| `IReadOnlyApplicationServiceAsync` | interface | `Contract\Logic\Async\IReadOnlyApplicationServiceAsync.cs` |
| `IReadOnlyApplicationServiceWithDto` | interface | `Contract\Logic\IApplicationServiceWithDto.cs` |
| `IReadOnlyApplicationServiceWithDtoAsync` | interface | `Contract\Logic\Async\IApplicationServiceWithDtoAsync.cs` |
| `IReadOnlyApplicationServiceWithSeparateDtos` | interface | `Contract\Logic\IApplicationServiceWithDtoSeparate.cs` |
| `IReadOnlyApplicationServiceWithSeparateDtosAsync` | interface | `Contract\Logic\Async\IApplicationServiceWithDtoSeparateAsync.cs` |
| `IReadOnlyRepository` | interface | `Contract\Data\IReadOnlyRepository.cs` |
| `IReadOnlyRepositoryAsync` | interface | `Contract\Data\Async\IReadOnlyRepositoryAsync.cs` |
| `IReadThroughCache` | interface | `Contract\Infrastructure\Caching\IReadThroughCache.cs` |
| `IRefreshAheadCache` | interface | `Contract\Infrastructure\Caching\IRefreshAheadCache.cs` |
| `IRepository` | interface | `Contract\Data\IRepository.cs` |
| `IRepositoryAsync` | interface | `Contract\Data\Async\IRepositoryAsync.cs` |
| `IRepositoryCache` | interface | `Contract\Data\IRepositoryCache.cs` |
| `IRepositoryCacheAsync` | interface | `Contract\Data\Async\IRepositoryCacheAsync.cs` |
| `IRequestContext` | interface | `Contract\Infrastructure\IRequestContext.cs` |
| `IRetryableOperation` | interface | `Contract\Infrastructure\Pipe\IRetryableOperation.cs` |
| `IScopedService` | interface | `Contract\Infrastructure\DependencyInjection\IServiceLifetimeMarker.cs` |
| `ISelfRegistering` | interface | `Contract\Infrastructure\DependencyInjection\IServiceLifetimeMarker.cs` |
| `IServiceLifetimeMarker` | interface | `Contract\Infrastructure\DependencyInjection\IServiceLifetimeMarker.cs` |
| `ISingletonService` | interface | `Contract\Infrastructure\DependencyInjection\IServiceLifetimeMarker.cs` |
| `ISoftDeletable` | interface | `Contract\Domain\Entity\ISoftDeletable.cs` |
| `ISoftDeletable` | interface | `Contract\Domain\Entity\ISoftDeletable.cs` |
| `ISpecification` | interface | `Contract\Domain\Specifications\ISpecification.cs` |
| `ISpecificationEvaluator` | interface | `Contract\Domain\Specifications\ISpecificationEvaluator.cs` |
| `ISpecificationEvaluator` | interface | `Contract\Domain\Specifications\ISpecificationEvaluator.cs` |
| `ISpecificationModel` | interface | `Contract\Domain\Specifications\ISpecificationModel.cs` |
| `ISpecificationQuery` | interface | `Contract\Domain\Specifications\ISpecificationQuery.cs` |
| `ISpecificationQueryEnhanced` | interface | `Contract\Domain\Specifications\ISpecificationQueryEnhanced.cs` |
| `ISQL` | interface | `Contract\Data\ISQL.cs` |
| `ISQLAsync` | interface | `Contract\Data\Async\ISQLAsync.cs` |
| `IStreamingQueryAsync` | interface | `Contract\Data\Async\IStreamingQueryAsync.cs` |
| `IStreamingRepositoryAsync` | interface | `Contract\Data\Async\IStreamingQueryAsync.cs` |
| `IStructuredMessageResult` | interface | `Contract\ValueObjects\Logic\IStructuredMessageResult.cs` |
| `ISubPipelineBuilder` | interface | `Contract\Infrastructure\Pipe\IComposablePipeline.cs` |
| `ISubPipelineBuilderAsync` | interface | `Contract\Infrastructure\Pipe\IComposablePipeline.cs` |
| `ISummaryResult` | interface | `Contract\ValueObjects\Logic\ISummaryResult.cs` |
| `ITelemetryService` | interface | `Contract\Infrastructure\Telemetry\ITelemetryService.cs` |
| `ITenantEntity` | interface | `Contract\Domain\Entity\ITenantEntity.cs` |
| `ITenantEntity` | interface | `Contract\Domain\Entity\ITenantEntity.cs` |
| `ITenantProvider` | interface | `Contract\Infrastructure\ITenantProvider.cs` |
| `ITenantProvider` | interface | `Contract\Infrastructure\ITenantProvider.cs` |
| `ITraceContextAccessor` | interface | `Observability\TracingServiceExtensions.cs` |
| `ITransientService` | interface | `Contract\Infrastructure\DependencyInjection\IServiceLifetimeMarker.cs` |
| `ITypedOperation` | interface | `Contract\Infrastructure\Pipe\ITypedOperation.cs` |
| `ITypedOperation` | interface | `Contract\Infrastructure\Pipe\ITypedOperation.cs` |
| `ITypedOperationAsync` | interface | `Contract\Infrastructure\Pipe\Async\ITypedOperationAsync.cs` |
| `ITypedOperationAsync` | interface | `Contract\Infrastructure\Pipe\Async\ITypedOperationAsync.cs` |
| `ITypedPipeline` | interface | `Contract\Infrastructure\Pipe\ITypedPipeline.cs` |
| `ITypedPipelineAsync` | interface | `Contract\Infrastructure\Pipe\Async\ITypedPipelineAsync.cs` |
| `IUnitOfWork` | interface | `Contract\Data\IUnitOfWork.cs` |
| `IUnitOfWorkAsync` | interface | `Contract\Data\Async\IUnitOfWorkAsync.cs` |
| `IUnitOfWorkWithEvents` | interface | `Contract\Data\IUnitOfWorkWithEvents.cs` |
| `IUnitOfWorkWithEventsAsync` | interface | `Contract\Data\Async\IUnitOfWorkWithEventsAsync.cs` |
| `IVersionedAggregate` | interface | `Contract\Domain\Entity\IAggregateRoot.cs` |
| `IVersionedAggregate` | interface | `Contract\Domain\Entity\IAggregateRoot.cs` |
| `IVersionedEntity` | interface | `Contract\Domain\Entity\IVersionedEntity.cs` |
| `IVersionedEntityWithCounter` | interface | `Contract\Domain\Entity\IVersionedEntity.cs` |
| `IWriteBehindCache` | interface | `Contract\Infrastructure\Caching\IWriteBehindCache.cs` |
| `IWriteThroughCache` | interface | `Contract\Infrastructure\Caching\IWriteThroughCache.cs` |

### `Mvp24Hours.Infrastructure`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `BackoffType` | enum | `Http\Options\HttpClientOptions.cs` |
| `BatchExecutionMode` | enum | `BackgroundJobs\Contract\IJobBatch.cs` |
| `BatchOptions` | class | `BackgroundJobs\Contract\IJobBatch.cs` |
| `BatchStatus` | enum | `BackgroundJobs\Contract\IJobBatch.cs` |
| `ChildExecutionMode` | enum | `BackgroundJobs\Contract\IParentChildJob.cs` |
| `CircuitBreakerState` | enum | `Resilience\Options\CircuitBreakerOptions.cs` |
| `ContinuationOptions` | class | `BackgroundJobs\Contract\IJobContinuation.cs` |
| `CropRectangle` | class | `FileStorage\Contract\IImageProcessingStorage.cs` |
| `EmailPriority` | enum | `Email\Options\EmailOptions.cs` |
| `ErrorInfo` | class | `Observability\Contract\IInfrastructureDiagnostics.cs` |
| `FileValidationContext` | class | `FileStorage\Contract\IFileValidator.cs` |
| `HangfireStorageProvider` | enum | `BackgroundJobs\Options\HangfireJobOptions.cs` |
| `IBackgroundJob` | interface | `BackgroundJobs\Contract\IBackgroundJob.cs` |
| `IBackgroundJob` | interface | `BackgroundJobs\Contract\IBackgroundJob.cs` |
| `IBackgroundJobsBuilder` | interface | `BackgroundJobs\Extensions\BackgroundJobsServiceExtensions.cs` |
| `IBatchJob` | interface | `BackgroundJobs\Contract\IJobBatch.cs` |
| `IBulkhead` | interface | `Resilience\Contract\IBulkhead.cs` |
| `IBulkhead` | interface | `Resilience\Contract\IBulkhead.cs` |
| `ICdnStorage` | interface | `FileStorage\Contract\ICdnStorage.cs` |
| `IChildJob` | interface | `BackgroundJobs\Contract\IParentChildJob.cs` |
| `IChunkedUploadStatus` | interface | `FileStorage\Contract\IChunkedUploadStorage.cs` |
| `IChunkedUploadStorage` | interface | `FileStorage\Contract\IChunkedUploadStorage.cs` |
| `ICircuitBreaker` | interface | `Resilience\Contract\ICircuitBreaker.cs` |
| `ICircuitBreaker` | interface | `Resilience\Contract\ICircuitBreaker.cs` |
| `IDeadLetterQueue` | interface | `BackgroundJobs\Management\IDeadLetterQueue.cs` |
| `IDeliveryReportHandler` | interface | `Sms\Contract\IDeliveryReportHandler.cs` |
| `IDistributedLock` | interface | `DistributedLocking\Contract\IDistributedLock.cs` |
| `IDistributedLockFactory` | interface | `DistributedLocking\Contract\IDistributedLockFactory.cs` |
| `IDistributedLockingBuilder` | interface | `DistributedLocking\Extensions\DistributedLockingServiceExtensions.cs` |
| `IEmailAttachment` | interface | `Email\Contract\IEmailAttachment.cs` |
| `IEmailDeliveryTracking` | interface | `Email\Tracking\EmailDeliveryTracking.cs` |
| `IEmailQueue` | interface | `Email\Queue\IEmailQueue.cs` |
| `IEmailService` | interface | `Email\Contract\IEmailService.cs` |
| `IEmailTemplateRenderer` | interface | `Email\Templates\IEmailTemplateRenderer.cs` |
| `IFakeEmailService` | interface | `Testing\Fakes\IFakeEmailService.cs` |
| `IFakeFileStorage` | interface | `Testing\Fakes\IFakeFileStorage.cs` |
| `IFakeSmsService` | interface | `Testing\Fakes\IFakeSmsService.cs` |
| `IFileMetadata` | interface | `FileStorage\Contract\IFileMetadata.cs` |
| `IFileStorage` | interface | `FileStorage\Contract\IFileStorage.cs` |
| `IFileValidator` | interface | `FileStorage\Contract\IFileValidator.cs` |
| `IFileVersion` | interface | `FileStorage\Contract\IFileVersioningStorage.cs` |
| `IFileVersioningStorage` | interface | `FileStorage\Contract\IFileVersioningStorage.cs` |
| `IHttpClientSerializer` | interface | `Http\Contract\IHttpClientSerializer.cs` |
| `IHttpContentSerializer` | interface | `Http\Contract\IHttpContentSerializer.cs` |
| `IHttpResiliencePolicy` | interface | `Http\Resilience\IHttpResiliencePolicy.cs` |
| `IImageProcessingHook` | interface | `FileStorage\Contract\IImageProcessingStorage.cs` |
| `IImageProcessingStorage` | interface | `FileStorage\Contract\IImageProcessingStorage.cs` |
| `IInfrastructureBuilder` | interface | `Configuration\InfrastructureBuilder.cs` |
| `IInfrastructureDiagnostics` | interface | `Observability\Contract\IInfrastructureDiagnostics.cs` |
| `IJobBatch` | interface | `BackgroundJobs\Contract\IJobBatch.cs` |
| `IJobContext` | interface | `BackgroundJobs\Contract\IJobContext.cs` |
| `IJobContinuation` | interface | `BackgroundJobs\Contract\IJobContinuation.cs` |
| `IJobHistoryStore` | interface | `BackgroundJobs\Management\IJobHistoryStore.cs` |
| `IJobMetrics` | interface | `BackgroundJobs\Management\IJobMetrics.cs` |
| `IJobScheduler` | interface | `BackgroundJobs\Contract\IJobScheduler.cs` |
| `ILockHandle` | interface | `DistributedLocking\Contract\ILockHandle.cs` |
| `ImageFormat` | enum | `FileStorage\Contract\IImageProcessingStorage.cs` |
| `ImageTransformations` | class | `FileStorage\Contract\IImageProcessingStorage.cs` |
| `IMultipartUploadInfo` | interface | `FileStorage\Contract\IPresignedUrlStorage.cs` |
| `INativeResiliencePipeline` | interface | `Resilience\Native\INativeResiliencePipeline.cs` |
| `INativeResiliencePipeline` | interface | `Resilience\Native\INativeResiliencePipeline.cs` |
| `IParentJob` | interface | `BackgroundJobs\Contract\IParentChildJob.cs` |
| `IPresignedUrlStorage` | interface | `FileStorage\Contract\IPresignedUrlStorage.cs` |
| `IRetryPolicy` | interface | `Resilience\Contract\IRetryPolicy.cs` |
| `IRetryPolicy` | interface | `Resilience\Contract\IRetryPolicy.cs` |
| `ISecretProvider` | interface | `Security\Contract\ISecretProvider.cs` |
| `ISecretRotationHelper` | interface | `Security\Helpers\ISecretRotationHelper.cs` |
| `ISmsRateLimiter` | interface | `Sms\Contract\ISmsRateLimiter.cs` |
| `ISmsService` | interface | `Sms\Contract\ISmsService.cs` |
| `ISmsTemplateService` | interface | `Sms\Contract\ISmsTemplateService.cs` |
| `ISoftDeletedFile` | interface | `FileStorage\Contract\ISoftDeleteStorage.cs` |
| `ISoftDeleteStorage` | interface | `FileStorage\Contract\ISoftDeleteStorage.cs` |
| `ISubsystemDiagnosticsProvider` | interface | `Observability\InfrastructureDiagnostics.cs` |
| `ITypedHttpClient` | interface | `Http\Contract\ITypedHttpClient.cs` |
| `JobPriority` | enum | `BackgroundJobs\Options\JobOptions.cs` |
| `JobStatus` | enum | `BackgroundJobs\Contract\IJobScheduler.cs` |
| `ParentChildJobOptions` | class | `BackgroundJobs\Contract\IParentChildJob.cs` |
| `QuartzSerializationType` | enum | `BackgroundJobs\Options\QuartzJobOptions.cs` |
| `QuartzStorageProvider` | enum | `BackgroundJobs\Options\QuartzJobOptions.cs` |
| `ResizeMode` | enum | `FileStorage\Contract\IImageProcessingStorage.cs` |
| `RetryBackoffType` | enum | `Resilience\Options\RetryOptions.cs` |
| `SubsystemDiagnostics` | class | `Observability\Contract\IInfrastructureDiagnostics.cs` |
| `SubsystemHealth` | enum | `Observability\Contract\IInfrastructureDiagnostics.cs` |
| `ThumbnailMode` | enum | `FileStorage\Contract\IImageProcessingStorage.cs` |
| `ValidationResult` | class | `FileStorage\Contract\IFileValidator.cs` |

### `Mvp24Hours.Infrastructure.Caching`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `ICacheMetrics` | interface | `Observability\CacheMetrics.cs` |
| `IHybridCacheTagManager` | interface | `HybridCache\IHybridCacheTagManager.cs` |

### `Mvp24Hours.Infrastructure.Cqrs`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `IAdvancedReadModelRepository` | interface | `Projections\IReadModelRepository.cs` |
| `IAggregate` | interface | `EventSourcing\IAggregate.cs` |
| `IAggregate` | interface | `EventSourcing\IAggregate.cs` |
| `IAggregateFactory` | interface | `EventSourcing\IAggregate.cs` |
| `IAuditStore` | interface | `Observability\IAuditStore.cs` |
| `IAuthorized` | interface | `Behaviors\AuthorizationBehavior.cs` |
| `IBatchProjection` | interface | `Projections\IncrementalProjection.cs` |
| `IBypassTenantFilter` | interface | `MultiTenancy\TenantQueryFilter.cs` |
| `ICacheable` | interface | `Behaviors\CachingBehavior.cs` |
| `ICacheInvalidator` | interface | `Behaviors\CachingBehavior.cs` |
| `ICircuitBreakerPolicy` | interface | `Behaviors\CircuitBreakerBehavior.cs` |
| `ICircuitBreakerProtected` | interface | `Behaviors\CircuitBreakerBehavior.cs` |
| `ICommandScheduler` | interface | `Scheduling\ICommandScheduler.cs` |
| `ICompensatableCommand` | interface | `Saga\CompensatingCommand.cs` |
| `ICompensatableCommand` | interface | `Saga\CompensatingCommand.cs` |
| `ICurrentUser` | interface | `MultiTenancy\ICurrentUser.cs` |
| `ICurrentUserAccessor` | interface | `MultiTenancy\ICurrentUser.cs` |
| `ICurrentUserFactory` | interface | `MultiTenancy\ICurrentUser.cs` |
| `IDeadLetterStore` | interface | `Abstractions\IDeadLetterStore.cs` |
| `IDomainEvent` | interface | `Abstractions\IDomainEvent.cs` |
| `IDomainEventDispatcher` | interface | `Abstractions\IHasDomainEvents.cs` |
| `IDomainEventHandler` | interface | `Abstractions\IDomainEvent.cs` |
| `IDomainToIntegrationEventConverter` | interface | `Abstractions\IIntegrationEventPublisher.cs` |
| `IEventSerializer` | interface | `EventSourcing\IEventSerializer.cs` |
| `IEventSourcedAggregate` | interface | `EventSourcing\IAggregate.cs` |
| `IEventSourcedAggregate` | interface | `EventSourcing\IAggregate.cs` |
| `IEventStore` | interface | `EventSourcing\IEventStore.cs` |
| `IEventStoreRepository` | interface | `EventSourcing\EventStoreRepository.cs` |
| `IEventStoreWithSubscription` | interface | `EventSourcing\IEventStore.cs` |
| `IEventTypeResolver` | interface | `EventSourcing\IEventSerializer.cs` |
| `IExceptionHandler` | interface | `Abstractions\IExceptionHandler.cs` |
| `IExceptionHandlerGlobal` | interface | `Abstractions\IExceptionHandler.cs` |
| `IHasDomainEvents` | interface | `Abstractions\IHasDomainEvents.cs` |
| `IHasTenant` | interface | `MultiTenancy\ITenantContext.cs` |
| `IHasTimeout` | interface | `Behaviors\TimeoutBehavior.cs` |
| `IHasUserTracking` | interface | `MultiTenancy\ICurrentUser.cs` |
| `IIdempotencyKeyGenerator` | interface | `Behaviors\IdempotencyBehavior.cs` |
| `IIdempotentCommand` | interface | `Behaviors\IdempotencyBehavior.cs` |
| `IInboxProcessor` | interface | `Messaging\InboxProcessor.cs` |
| `IInboxStore` | interface | `Abstractions\IInboxStore.cs` |
| `IIncrementalProjection` | interface | `Projections\IncrementalProjection.cs` |
| `IIntegrationEvent` | interface | `Abstractions\IIntegrationEvent.cs` |
| `IIntegrationEventHandler` | interface | `Abstractions\IIntegrationEvent.cs` |
| `IIntegrationEventOutbox` | interface | `Abstractions\IIntegrationEventOutbox.cs` |
| `IIntegrationEventPublisher` | interface | `Abstractions\IIntegrationEventPublisher.cs` |
| `IMediator` | interface | `Abstractions\IMediator.cs` |
| `IMediatorCommand` | interface | `Abstractions\IMediatorCommand.cs` |
| `IMediatorCommand` | interface | `Abstractions\IMediatorCommand.cs` |
| `IMediatorCommandHandler` | interface | `Abstractions\IMediatorCommand.cs` |
| `IMediatorCommandHandler` | interface | `Abstractions\IMediatorCommand.cs` |
| `IMediatorDecorator` | interface | `Abstractions\IMediatorDecorator.cs` |
| `IMediatorDomainEvent` | interface | `Abstractions\IDomainEvent.cs` |
| `IMediatorDomainEventHandler` | interface | `Abstractions\IDomainEvent.cs` |
| `IMediatorHasDomainEvents` | interface | `Abstractions\IHasDomainEvents.cs` |
| `IMediatorNotification` | interface | `Abstractions\IMediatorNotification.cs` |
| `IMediatorNotificationHandler` | interface | `Abstractions\IMediatorNotification.cs` |
| `IMediatorQuery` | interface | `Abstractions\IMediatorQuery.cs` |
| `IMediatorQueryHandler` | interface | `Abstractions\IMediatorQuery.cs` |
| `IMediatorRequest` | interface | `Abstractions\IMediatorRequest.cs` |
| `IMediatorRequest` | interface | `Abstractions\IMediatorRequest.cs` |
| `IMediatorRequestHandler` | interface | `Abstractions\IMediatorRequestHandler.cs` |
| `IMediatorRequestHandler` | interface | `Abstractions\IMediatorRequestHandler.cs` |
| `IMultiEventProjectionHandler` | interface | `Projections\IProjectionHandler.cs` |
| `INativeResilient` | interface | `Behaviors\NativeResilienceBehavior.cs` |
| `INotificationPublisher` | interface | `Abstractions\NotificationPublishingStrategy.cs` |
| `IPipelineBehavior` | interface | `Abstractions\IPipelineBehavior.cs` |
| `IPipelineHook` | interface | `Abstractions\IPipelineHook.cs` |
| `IPipelineHook` | interface | `Abstractions\IPipelineHook.cs` |
| `IPositionTrackedProjection` | interface | `Projections\IProjection.cs` |
| `IPostProcessor` | interface | `Abstractions\IPostProcessor.cs` |
| `IPostProcessorGlobal` | interface | `Abstractions\IPostProcessor.cs` |
| `IPreProcessor` | interface | `Abstractions\IPreProcessor.cs` |
| `IPreProcessorGlobal` | interface | `Abstractions\IPreProcessor.cs` |
| `IProjection` | interface | `Projections\IProjection.cs` |
| `IProjectionCheckpointManager` | interface | `Projections\IncrementalProjection.cs` |
| `IProjectionManager` | interface | `Projections\ProjectionManager.cs` |
| `IProjectionPositionStore` | interface | `Projections\ProjectionManager.cs` |
| `IProjectionRebuildService` | interface | `Projections\ProjectionHostedService.cs` |
| `IPublisher` | interface | `Abstractions\IMediator.cs` |
| `IRabbitMQOutbox` | interface | `Messaging\RabbitMQOutboxAdapter.cs` |
| `IReadModelRepository` | interface | `Projections\IReadModelRepository.cs` |
| `IRequestContext` | interface | `Observability\IRequestContext.cs` |
| `IRequestContextAccessor` | interface | `Observability\IRequestContext.cs` |
| `IRequestContextFactory` | interface | `Observability\IRequestContext.cs` |
| `IResettableProjection` | interface | `Projections\IProjection.cs` |
| `IRetryable` | interface | `Behaviors\RetryBehavior.cs` |
| `ISaga` | interface | `Saga\ISaga.cs` |
| `ISaga` | interface | `Saga\ISaga.cs` |
| `ISagaOrchestrator` | interface | `Saga\ISagaOrchestrator.cs` |
| `ISagaStateStore` | interface | `Saga\ISagaStateStore.cs` |
| `ISagaStep` | interface | `Saga\ISagaStep.cs` |
| `IScheduledCommand` | interface | `Scheduling\IScheduledCommand.cs` |
| `IScheduledCommand` | interface | `Scheduling\IScheduledCommand.cs` |
| `IScheduledCommandStore` | interface | `Scheduling\IScheduledCommandStore.cs` |
| `ISender` | interface | `Abstractions\IMediator.cs` |
| `ISharedEntity` | interface | `MultiTenancy\TenantQueryFilter.cs` |
| `ISnapshotAggregate` | interface | `EventSourcing\IAggregate.cs` |
| `ISnapshotStore` | interface | `EventSourcing\Snapshot.cs` |
| `ISnapshotStrategy` | interface | `EventSourcing\Snapshot.cs` |
| `IStreamRequest` | interface | `Abstractions\IStreamRequest.cs` |
| `IStreamRequestHandler` | interface | `Abstractions\IStreamRequest.cs` |
| `IStreamSender` | interface | `Abstractions\IMediator.cs` |
| `ITenantAware` | interface | `Behaviors\TenantBehavior.cs` |
| `ITenantContext` | interface | `MultiTenancy\ITenantContext.cs` |
| `ITenantContextAccessor` | interface | `MultiTenancy\ITenantContext.cs` |
| `ITenantFilter` | interface | `MultiTenancy\ITenantContext.cs` |
| `ITenantResolver` | interface | `MultiTenancy\ITenantContext.cs` |
| `ITenantStore` | interface | `MultiTenancy\ITenantContext.cs` |
| `ITimeoutPolicy` | interface | `Behaviors\TimeoutBehavior.cs` |
| `ITransactional` | interface | `Behaviors\TransactionBehavior.cs` |
| `IUserContext` | interface | `Behaviors\AuthorizationBehavior.cs` |

### `Mvp24Hours.Infrastructure.CronJob`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `IAdvancedCronJobOptions` | interface | `Services\AdvancedCronJobService.cs` |
| `ICronJobCancelledHandler` | interface | `Events\ICronJobEventHandler.cs` |
| `ICronJobCompletedHandler` | interface | `Events\ICronJobEventHandler.cs` |
| `ICronJobContext` | interface | `Context\ICronJobContext.cs` |
| `ICronJobContextAccessor` | interface | `Context\CronJobContextAccessor.cs` |
| `ICronJobController` | interface | `Control\ICronJobController.cs` |
| `ICronJobDependency` | interface | `Dependencies\ICronJobDependency.cs` |
| `ICronJobDependencyTracker` | interface | `Dependencies\ICronJobDependency.cs` |
| `ICronJobEventDispatcher` | interface | `Events\CronJobEventDispatcher.cs` |
| `ICronJobEventHandler` | interface | `Events\ICronJobEventHandler.cs` |
| `ICronJobExecutionLock` | interface | `Resiliency\ICronJobExecutionLock.cs` |
| `ICronJobFailedHandler` | interface | `Events\ICronJobEventHandler.cs` |
| `ICronJobLockHandle` | interface | `Resiliency\ICronJobExecutionLock.cs` |
| `ICronJobMetrics` | interface | `Observability\ICronJobMetrics.cs` |
| `ICronJobResilienceConfig` | interface | `Resiliency\ICronJobResilienceConfig.cs` |
| `ICronJobRetryHandler` | interface | `Events\ICronJobEventHandler.cs` |
| `ICronJobSkippedHandler` | interface | `Events\ICronJobEventHandler.cs` |
| `ICronJobStartingHandler` | interface | `Events\ICronJobEventHandler.cs` |
| `ICronJobStateStore` | interface | `State\ICronJobStateStore.cs` |
| `IDistributedCronJobLock` | interface | `Resiliency\IDistributedCronJobLock.cs` |
| `IDistributedCronJobLockHandle` | interface | `Resiliency\IDistributedCronJobLock.cs` |
| `IResilientScheduleConfig` | interface | `Interfaces\IResilientScheduleConfig.cs` |
| `IScheduleConfig` | interface | `Interfaces\IScheduleConfig.cs` |

### `Mvp24Hours.Infrastructure.Data.EFCore`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `IConnectionResolver` | interface | `ReadWriteSplitting\IConnectionResolver.cs` |
| `IDataSeeder` | interface | `Migrations\MigrationHostedService.cs` |
| `IDataSeeder` | interface | `Testing\IDataSeeder.cs` |
| `IDataSeederAsync` | interface | `Testing\IDataSeeder.cs` |
| `IDomainEventDispatcherEFCore` | interface | `Cqrs\IDomainEventDispatcherEFCore.cs` |
| `IEntitySeeder` | interface | `Testing\IDataSeeder.cs` |
| `IMigrationService` | interface | `Migrations\IMigrationService.cs` |
| `IReadDbContext` | interface | `Cqrs\ReadWriteDbContext.cs` |
| `IReadOnlyDbContext` | interface | `ReadWriteSplitting\IConnectionResolver.cs` |
| `IReplicaSelector` | interface | `ReadWriteSplitting\IReplicaSelector.cs` |
| `ISchemaValidator` | interface | `SchemaValidation\ISchemaValidator.cs` |
| `ITestDbContextFactory` | interface | `Testing\TestDbContextFactory.cs` |
| `IWriteDbContext` | interface | `ReadWriteSplitting\IConnectionResolver.cs` |
| `IWriteDbContext` | interface | `Cqrs\ReadWriteDbContext.cs` |

### `Mvp24Hours.Infrastructure.Data.MongoDb`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `IBsonClassMap` | interface | `Core\Contract\Data\IBsonClassMap.cs` |
| `IBulkOperationsMongoDbAsync` | interface | `Core\Contract\Data\IBulkOperationsMongoDbAsync.cs` |
| `ICircuitBreakerMetrics` | interface | `Resiliency\IMongoDbResiliencyPolicy.cs` |
| `IDomainEventDispatcherMongoDb` | interface | `Cqrs\IDomainEventDispatcherMongoDb.cs` |
| `IFieldEncryptor` | interface | `Security\FieldEncryption.cs` |
| `IMongoDataSeeder` | interface | `Testing\IMongoDataSeeder.cs` |
| `IMongoDataSeederAsync` | interface | `Testing\IMongoDataSeeder.cs` |
| `IMongoDbCappedCollectionService` | interface | `Advanced\CappedCollections\IMongoDbCappedCollectionService.cs` |
| `IMongoDbChangeStreamService` | interface | `Advanced\ChangeStreams\IMongoDbChangeStreamService.cs` |
| `IMongoDbGeospatialService` | interface | `Advanced\Geospatial\IMongoDbGeospatialService.cs` |
| `IMongoDbGridFsService` | interface | `Advanced\GridFS\IMongoDbGridFsService.cs` |
| `IMongoDbIndexManager` | interface | `Performance\Indexes\IMongoDbIndexManager.cs` |
| `IMongoDbInterceptor` | interface | `Interceptors\IMongoDbInterceptor.cs` |
| `IMongoDbInterceptorPipeline` | interface | `Interceptors\MongoDbInterceptorPipeline.cs` |
| `IMongoDbMetrics` | interface | `Observability\IMongoDbMetrics.cs` |
| `IMongoDbMigration` | interface | `Infrastructure\Migrations\IMongoDbMigration.cs` |
| `IMongoDbMigrationRunner` | interface | `Infrastructure\Migrations\IMongoDbMigrationRunner.cs` |
| `IMongoDbResiliencyPolicy` | interface | `Resiliency\IMongoDbResiliencyPolicy.cs` |
| `IMongoDbSchemaValidationService` | interface | `Advanced\SchemaValidation\IMongoDbSchemaValidationService.cs` |
| `IMongoDbShardingService` | interface | `Advanced\Sharding\IMongoDbShardingService.cs` |
| `IMongoDbTextSearchService` | interface | `Advanced\TextSearch\IMongoDbTextSearchService.cs` |
| `IMongoDbTimeSeriesService` | interface | `Advanced\TimeSeries\IMongoDbTimeSeriesService.cs` |
| `IMongoDbTransactionManager` | interface | `Advanced\Transactions\IMongoDbTransactionManager.cs` |
| `IMongoEntitySeeder` | interface | `Testing\IMongoDataSeeder.cs` |
| `IRowLevelSecurityPolicy` | interface | `Security\MongoDbRowLevelSecurity.cs` |
| `MongoDbBulkOperationResult` | class | `Core\Contract\Data\IBulkOperationsMongoDbAsync.cs` |

### `Mvp24Hours.Infrastructure.Pipe`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `ICacheableOperation` | interface | `Integration\Caching\ICacheableOperation.cs` |
| `ICheckpointStore` | interface | `AdvancedFlow\Checkpoint\ICheckpointStore.cs` |
| `IDependencyGraphExecutor` | interface | `AdvancedFlow\DependencyGraph\IDependencyGraphOperation.cs` |
| `IDependencyGraphNode` | interface | `AdvancedFlow\DependencyGraph\IDependencyGraphOperation.cs` |
| `IDependencyGraphNode` | interface | `AdvancedFlow\DependencyGraph\IDependencyGraphOperation.cs` |
| `IForkJoinOperation` | interface | `AdvancedFlow\ForkJoin\IForkJoinOperation.cs` |
| `IForkJoinOperationAsync` | interface | `AdvancedFlow\ForkJoin\IForkJoinOperation.cs` |
| `IForkOperation` | interface | `AdvancedFlow\ForkJoin\IForkJoinOperation.cs` |
| `IJoinOperation` | interface | `AdvancedFlow\ForkJoin\IForkJoinOperation.cs` |
| `IPipelineContext` | interface | `Context\IPipelineContext.cs` |
| `IPipelineContextAccessor` | interface | `Context\IPipelineContextAccessor.cs` |
| `IPipelineHealthMonitor` | interface | `Observability\PipelineHealthCheck.cs` |
| `IPipelineMetrics` | interface | `Observability\IPipelineMetrics.cs` |
| `IPipelineObserver` | interface | `Observability\IPipelineObserver.cs` |
| `IPipelineObserverManager` | interface | `Observability\IPipelineObserver.cs` |
| `IPipelineObserverSync` | interface | `Observability\IPipelineObserver.cs` |
| `IPipelineSagaStateStore` | interface | `AdvancedFlow\Saga\IPipelineSaga.cs` |
| `IPipelineSagaStep` | interface | `AdvancedFlow\Saga\IPipelineSaga.cs` |
| `IPipelineVisualizer` | interface | `Observability\IPipelineVisualizer.cs` |
| `IPrioritizedOperation` | interface | `AdvancedFlow\Priority\OperationPriority.cs` |
| `IStateSerializer` | interface | `AdvancedFlow\Checkpoint\ICheckpointStore.cs` |
| `IStreamingOperation` | interface | `Integration\Streaming\IStreamingPipeline.cs` |
| `IStreamingPipeline` | interface | `Integration\Streaming\IStreamingPipeline.cs` |

### `Mvp24Hours.Infrastructure.RabbitMQ`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `ConnectionObserverContext` | class | `Observability\Contract\IRabbitMQObserver.cs` |
| `ConsumeObserverContext` | class | `Observability\Contract\IRabbitMQObserver.cs` |
| `IBatchConsumeContext` | interface | `Core\Contract\IBatchConsumeContext.cs` |
| `IBatchConsumer` | interface | `Core\Contract\IBatchConsumer.cs` |
| `IBatchConsumerDefinition` | interface | `Core\Contract\IBatchConsumerDefinition.cs` |
| `IBatchConsumerDefinition` | interface | `Core\Contract\IBatchConsumerDefinition.cs` |
| `IBatchMessageItem` | interface | `Core\Contract\IBatchConsumeContext.cs` |
| `IBatchMessageResult` | interface | `Core\Contract\IBatchConsumer.cs` |
| `IConnectionObserver` | interface | `Observability\Contract\IRabbitMQObserver.cs` |
| `IConsumeContext` | interface | `Core\Contract\IConsumeContext.cs` |
| `IConsumedMessage` | interface | `Testing\Contract\IConsumedMessage.cs` |
| `IConsumedMessage` | interface | `Testing\Contract\IConsumedMessage.cs` |
| `IConsumeFilter` | interface | `Pipeline\Contract\IConsumeFilter.cs` |
| `IConsumeFilter` | interface | `Pipeline\Contract\IConsumeFilter.cs` |
| `IConsumeFilterContext` | interface | `Pipeline\Contract\IConsumeFilterContext.cs` |
| `IConsumeObserver` | interface | `Observability\Contract\IRabbitMQObserver.cs` |
| `IConsumerDefinition` | interface | `Core\Contract\IConsumerDefinition.cs` |
| `IConsumerDefinition` | interface | `Core\Contract\IConsumerDefinition.cs` |
| `IConsumerHarness` | interface | `Testing\Contract\ITestHarness.cs` |
| `IEndpointNameFormatter` | interface | `Topology\Contract\IEndpointNameFormatter.cs` |
| `IFaultConsumer` | interface | `Core\Contract\IFaultConsumer.cs` |
| `IFaultContext` | interface | `Core\Contract\IFaultConsumer.cs` |
| `IFilterPipelineExecutor` | interface | `Pipeline\FilterPipelineExecutor.cs` |
| `IInMemoryBus` | interface | `Testing\Contract\IInMemoryBus.cs` |
| `IMessage` | interface | `Core\Contract\IMessage.cs` |
| `IMessage` | interface | `Core\Contract\IMessage.cs` |
| `IMessageConsumer` | interface | `Core\Contract\IMessageConsumer.cs` |
| `IMessageDeduplicationStore` | interface | `Core\Contract\IMessageDeduplicationStore.cs` |
| `IMessageScheduler` | interface | `Core\Contract\IMessageScheduler.cs` |
| `IMessageSerializer` | interface | `Core\Contract\IMessageSerializer.cs` |
| `IMessageTopology` | interface | `Topology\Contract\IMessageTopology.cs` |
| `IMessageTopology` | interface | `Topology\Contract\IMessageTopology.cs` |
| `IMessageTypeResolver` | interface | `Core\Contract\IMessageTypeResolver.cs` |
| `IMongoSagaCollection` | interface | `Saga\Persistence\MongoDbSagaRepository.cs` |
| `IMvpRabbitMQClient` | interface | `Core\Contract\IMvpRabbitMQClient.cs` |
| `IMvpRabbitMQConnection` | interface | `Core\Contract\IMvpRabbitMQConnection.cs` |
| `IMvpRabbitMQConsumer` | interface | `Core\Contract\IMvpRabbitMQConsumer.cs` |
| `IMvpRabbitMQConsumerAsync` | interface | `Core\Contract\IMvpRabbitMQConsumerAsync.cs` |
| `IMvpRabbitMQConsumerRecoveryAsync` | interface | `Core\Contract\IMvpRabbitMQConsumerRecoveryAsync.cs` |
| `IMvpRabbitMQConsumerRecoverySync` | interface | `Core\Contract\IMvpRabbitMQConsumerRecoverySync.cs` |
| `IMvpRabbitMQConsumerSync` | interface | `Core\Contract\IMvpRabbitMQConsumerSync.cs` |
| `IObserverManager` | interface | `Observability\ObserverManager.cs` |
| `IOutboxPublisher` | interface | `Transactional\Contract\ITransactionalBus.cs` |
| `IPublishedMessage` | interface | `Testing\Contract\IPublishedMessage.cs` |
| `IPublishedMessage` | interface | `Testing\Contract\IPublishedMessage.cs` |
| `IPublishFilter` | interface | `Pipeline\Contract\IPublishFilter.cs` |
| `IPublishFilter` | interface | `Pipeline\Contract\IPublishFilter.cs` |
| `IPublishFilterContext` | interface | `Pipeline\Contract\IPublishFilterContext.cs` |
| `IPublishObserver` | interface | `Observability\Contract\IRabbitMQObserver.cs` |
| `IRabbitMQDiagnostics` | interface | `Observability\RabbitMQDiagnostics.cs` |
| `IRabbitMQMetrics` | interface | `Metrics\IRabbitMQMetrics.cs` |
| `IRabbitMQStructuredLogger` | interface | `Logging\IRabbitMQStructuredLogger.cs` |
| `IRequestClient` | interface | `Core\Contract\IRequestClient.cs` |
| `IRequestHandler` | interface | `Core\Contract\IRequestHandler.cs` |
| `IRoutingKeyConvention` | interface | `Topology\Contract\IRoutingKeyConvention.cs` |
| `ISagaConsumeContext` | interface | `Saga\Contract\ISagaConsumeContext.cs` |
| `ISagaConsumer` | interface | `Saga\Contract\ISagaConsumer.cs` |
| `ISagaDbContext` | interface | `Saga\Persistence\EFCoreSagaRepository.cs` |
| `ISagaRepository` | interface | `Saga\Contract\ISagaRepository.cs` |
| `IScheduledMessageStore` | interface | `Core\Contract\IScheduledMessageStore.cs` |
| `ISendFilter` | interface | `Pipeline\Contract\ISendFilter.cs` |
| `ISendFilter` | interface | `Pipeline\Contract\ISendFilter.cs` |
| `ISendFilterContext` | interface | `Pipeline\Contract\ISendFilter.cs` |
| `ISendObserver` | interface | `Observability\Contract\IRabbitMQObserver.cs` |
| `IServiceScope` | interface | `Core\Contract\IConsumeContext.cs` |
| `ITenantConnectionFactory` | interface | `MultiTenancy\Contract\ITenantConnectionFactory.cs` |
| `ITenantConsumeFilter` | interface | `MultiTenancy\Contract\ITenantConsumeFilter.cs` |
| `ITenantDeadLetterQueueHelper` | interface | `MultiTenancy\TenantDeadLetterQueueHelper.cs` |
| `ITenantPublishFilter` | interface | `MultiTenancy\Contract\ITenantConsumeFilter.cs` |
| `ITenantRabbitMQResolver` | interface | `MultiTenancy\Contract\ITenantRabbitMQResolver.cs` |
| `ITenantSendFilter` | interface | `MultiTenancy\Contract\ITenantConsumeFilter.cs` |
| `ITestHarness` | interface | `Testing\Contract\ITestHarness.cs` |
| `ITopologyBuilder` | interface | `Topology\Contract\ITopologyBuilder.cs` |
| `ITransactionalBus` | interface | `Transactional\Contract\ITransactionalBus.cs` |
| `ITransactionalConsumeContext` | interface | `Transactional\ITransactionalConsumeContext.cs` |
| `ITransactionalConsumeContextFactory` | interface | `Transactional\TransactionalConsumeContext.cs` |
| `ITransactionalEnlistment` | interface | `Transactional\TransactionalEnlistment.cs` |
| `ITransactionalOutbox` | interface | `Transactional\Contract\ITransactionalOutbox.cs` |
| `ITransactionalUnitOfWorkFactory` | interface | `Transactional\UnitOfWorkTransactionalExtensions.cs` |
| `OutboxPublisherStatus` | class | `Transactional\Contract\ITransactionalBus.cs` |
| `PublishObserverContext` | class | `Observability\Contract\IRabbitMQObserver.cs` |
| `Response` | class | `Core\Contract\Response.cs` |
| `ResponseStatus` | enum | `Core\Contract\Response.cs` |
| `ScheduleMessageOptions` | class | `Core\Contract\IMessageScheduler.cs` |
| `SendObserverContext` | class | `Observability\Contract\IRabbitMQObserver.cs` |
| `TenantRabbitMQConfiguration` | class | `MultiTenancy\Contract\ITenantRabbitMQResolver.cs` |
| `TransactionalOutboxMessage` | class | `Transactional\Contract\ITransactionalOutbox.cs` |
| `TransactionalOutboxStatus` | enum | `Transactional\Contract\ITransactionalOutbox.cs` |

### `Mvp24Hours.WebAPI`

| Tipo | Kind | Arquivo |
|------|------|---------|
| `IContentFormatter` | interface | `ContentNegotiation\IContentFormatter.cs` |
| `IContentFormatterRegistry` | interface | `ContentNegotiation\IContentFormatter.cs` |
| `ICorrelationContextProvider` | interface | `Http\CorrelationIdHandler.cs` |
| `IDistributedRateLimiter` | interface | `RateLimiting\IDistributedRateLimiter.cs` |
| `IEndpointRouteBuilderExtensions` | class | `Endpoints\IEndpointRouteBuilderExtensions.cs` |
| `IExceptionToProblemDetailsMapper` | interface | `Exceptions\IExceptionToProblemDetailsMapper.cs` |
| `IExtensionBinder` | interface | `Binders\IExtensionBinder.cs` |
| `IExtensionBinderWithParameter` | interface | `Binders\IExtensionBinderWithParameter.cs` |
| `IIdempotencyKeyGenerator` | interface | `Idempotency\IIdempotencyKeyGenerator.cs` |
| `IIdempotencyStore` | interface | `Idempotency\IIdempotencyStore.cs` |
| `IOutputCacheInvalidator` | interface | `Extensions\OutputCachingExtensions.cs` |
| `IProblemDetailsFormatter` | interface | `ContentNegotiation\IContentFormatter.cs` |
| `IRateLimitKeyGenerator` | interface | `RateLimiting\IRateLimitKeyGenerator.cs` |
| `IRequestLogger` | interface | `Services\IRequestLogger.cs` |

</details>

---

## Destaques: 0% Coverlet em assemblies instrumentados

Tipos com dados Coverlet e `lineCoverage = 0` — maior ROI imediato (já há projeto de teste).

| Projeto | Tipo | Prio | Arquivo | Linhas |
|---------|------|------|---------|-------:|
| `Mvp24Hours.Infrastructure.Cqrs` | `AggregateRoot` | 1 | `EventSourcing\AggregateRoot.cs` | 28 |
| `Mvp24Hours.Infrastructure.Cqrs` | `AggregateRoot` | 1 | `EventSourcing\AggregateRoot.cs` | 28 |
| `Mvp24Hours.Infrastructure.Cqrs` | `AggregatingProjectionHandler` | 1 | `Projections\IProjectionHandler.cs` | 10 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ApplyProjection` | 1 | `Projections\IncrementalProjection.cs` | 46 |
| `Mvp24Hours.Infrastructure.Cqrs` | `AuthorizationBehavior` | 1 | `Behaviors\AuthorizationBehavior.cs` | 60 |
| `Mvp24Hours.Infrastructure.Cqrs` | `AutoIntegrationEventHandler` | 1 | `Extensions\DomainToIntegrationEventExtensions.cs` | 25 |
| `Mvp24Hours.Infrastructure.Cqrs` | `BatchProjection` | 1 | `Projections\IncrementalProjection.cs` | 7 |
| `Mvp24Hours.Infrastructure.Cqrs` | `CacheInvalidationBehavior` | 1 | `Behaviors\CachingBehavior.cs` | 32 |
| `Mvp24Hours.Infrastructure.Cqrs` | `CachingBehavior` | 1 | `Behaviors\CachingBehavior.cs` | 83 |
| `Mvp24Hours.Infrastructure.Cqrs` | `CompensatableCommandBase` | 1 | `Saga\CompensatingCommand.cs` | 1 |
| `Mvp24Hours.Infrastructure.Cqrs` | `CompensationRecord` | 1 | `Saga\CompensatingCommand.cs` | 12 |
| `Mvp24Hours.Infrastructure.Cqrs` | `CompositeSnapshotStrategy` | 1 | `EventSourcing\Snapshot.cs` | 5 |
| `Mvp24Hours.Infrastructure.Cqrs` | `CqrsJsonSerializerContext` | 1 | `Serialization\CqrsJsonSerializerContext.cs` | 4329 |
| `Mvp24Hours.Infrastructure.Cqrs` | `DefaultAggregateFactory` | 1 | `EventSourcing\IAggregate.cs` | 3 |
| `Mvp24Hours.Infrastructure.Cqrs` | `DefaultEventTypeResolver` | 1 | `EventSourcing\IEventSerializer.cs` | 6 |
| `Mvp24Hours.Infrastructure.Cqrs` | `DefaultIdempotencyKeyGenerator` | 1 | `Behaviors\IdempotencyBehavior.cs` | 19 |
| `Mvp24Hours.Infrastructure.Cqrs` | `EventMetadata` | 1 | `EventSourcing\StoredEvent.cs` | 6 |
| `Mvp24Hours.Infrastructure.Cqrs` | `EventSourcingOptions` | 1 | `EventSourcing\EventSourcingExtensions.cs` | 3 |
| `Mvp24Hours.Infrastructure.Cqrs` | `EventStreamInfo` | 1 | `EventSourcing\EventStream.cs` | 6 |
| `Mvp24Hours.Infrastructure.Cqrs` | `IdempotencyBehavior` | 1 | `Behaviors\IdempotencyBehavior.cs` | 82 |
| `Mvp24Hours.Infrastructure.Cqrs` | `InboxCleanupService` | 1 | `Messaging\InboxProcessor.cs` | 39 |
| `Mvp24Hours.Infrastructure.Cqrs` | `IncrementalProjection` | 1 | `Projections\IncrementalProjection.cs` | 15 |
| `Mvp24Hours.Infrastructure.Cqrs` | `MediatorCacheOptions` | 1 | `Extensions\MediatorCachingExtensions.cs` | 4 |
| `Mvp24Hours.Infrastructure.Cqrs` | `MediatorTelemetryData` | 1 | `Behaviors\TelemetryBehavior.cs` | 12 |
| `Mvp24Hours.Infrastructure.Cqrs` | `NativeCqrsResilienceOptions` | 1 | `Behaviors\NativeResilienceBehavior.cs` | 44 |
| `Mvp24Hours.Infrastructure.Cqrs` | `NativeResilienceBehavior` | 1 | `Behaviors\NativeResilienceBehavior.cs` | 138 |
| `Mvp24Hours.Infrastructure.Cqrs` | `OutboxCleanupService` | 1 | `Messaging\OutboxProcessor.cs` | 39 |
| `Mvp24Hours.Infrastructure.Cqrs` | `OutboxProcessor` | 1 | `Messaging\OutboxProcessor.cs` | 124 |
| `Mvp24Hours.Infrastructure.Cqrs` | `PagedReadModelResult` | 1 | `Projections\IReadModelRepository.cs` | 7 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ParallelNotificationPublisher` | 1 | `Abstractions\NotificationPublishingStrategy.cs` | 4 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ParallelNoWaitNotificationPublisher` | 1 | `Abstractions\NotificationPublishingStrategy.cs` | 23 |
| `Mvp24Hours.Infrastructure.Cqrs` | `PipelineHookBase` | 1 | `Abstractions\IPipelineHook.cs` | 9 |
| `Mvp24Hours.Infrastructure.Cqrs` | `PipelineHookBase` | 1 | `Abstractions\IPipelineHook.cs` | 9 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ProjectionHostedService` | 1 | `Projections\ProjectionHostedService.cs` | 25 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ProjectionInfo` | 1 | `Projections\IProjection.cs` | 6 |
| `Mvp24Hours.Infrastructure.Cqrs` | `RabbitMqIntegrationEventPublisher` | 1 | `Implementations\RabbitMqIntegrationEventPublisher.cs` | 89 |
| `Mvp24Hours.Infrastructure.Cqrs` | `RabbitMQOutboxAdapter` | 1 | `Messaging\RabbitMQOutboxAdapter.cs` | 124 |
| `Mvp24Hours.Infrastructure.Cqrs` | `RabbitMQOutboxIntegrationEvent` | 1 | `Messaging\RabbitMQOutboxAdapter.cs` | 12 |
| `Mvp24Hours.Infrastructure.Cqrs` | `RabbitMQOutboxMessage` | 1 | `Messaging\RabbitMQOutboxAdapter.cs` | 17 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ReadModelProjectionHandler` | 1 | `Projections\IProjectionHandler.cs` | 2 |
| `Mvp24Hours.Infrastructure.Cqrs` | `RebuildStatus` | 1 | `Projections\ProjectionHostedService.cs` | 8 |
| `Mvp24Hours.Infrastructure.Cqrs` | `RetryBehavior` | 1 | `Behaviors\RetryBehavior.cs` | 47 |
| `Mvp24Hours.Infrastructure.Cqrs` | `SagaCompensationException` | 1 | `Saga\SagaExceptions.cs` | 3 |
| `Mvp24Hours.Infrastructure.Cqrs` | `SagaHostedService` | 1 | `Saga\SagaHostedService.cs` | 52 |
| `Mvp24Hours.Infrastructure.Cqrs` | `SagaMaxRetriesExceededException` | 1 | `Saga\SagaExceptions.cs` | 5 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ScheduledCommandHostedService` | 1 | `Scheduling\ScheduledCommandHostedService.cs` | 148 |
| `Mvp24Hours.Infrastructure.Cqrs` | `SequentialContinueOnExceptionPublisher` | 1 | `Abstractions\NotificationPublishingStrategy.cs` | 21 |
| `Mvp24Hours.Infrastructure.Cqrs` | `SequentialNotificationPublisher` | 1 | `Abstractions\NotificationPublishingStrategy.cs` | 6 |
| `Mvp24Hours.Infrastructure.Cqrs` | `SnapshotAggregateRoot` | 1 | `EventSourcing\AggregateRoot.cs` | 8 |
| `Mvp24Hours.Infrastructure.Cqrs` | `TenantNotFoundException` | 1 | `Behaviors\TenantBehavior.cs` | 9 |
| `Mvp24Hours.Infrastructure.Cqrs` | `TenantQueryFilter` | 1 | `MultiTenancy\TenantQueryFilter.cs` | 13 |
| `Mvp24Hours.Infrastructure.Cqrs` | `DomainEventExtensions` | 3 | `Extensions\DomainEventExtensions.cs` | 33 |
| `Mvp24Hours.Infrastructure.Cqrs` | `DomainToIntegrationEventExtensions` | 3 | `Extensions\DomainToIntegrationEventExtensions.cs` | 53 |
| `Mvp24Hours.Infrastructure.Cqrs` | `MediatorCachingExtensions` | 3 | `Extensions\MediatorCachingExtensions.cs` | 20 |
| `Mvp24Hours.Infrastructure.Cqrs` | `NativeResilienceBehaviorExtensions` | 3 | `Behaviors\NativeResilienceBehavior.cs` | 14 |
| `Mvp24Hours.Infrastructure.Cqrs` | `RetryPolicyExtensions` | 3 | `Behaviors\RetryBehavior.cs` | 10 |
| `Mvp24Hours.Infrastructure.Cqrs` | `IAuthorized` | 4 | `Behaviors\AuthorizationBehavior.cs` | 3 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ICacheable` | 4 | `Behaviors\CachingBehavior.cs` | 2 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ICircuitBreakerPolicy` | 4 | `Behaviors\CircuitBreakerBehavior.cs` | 1 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ICircuitBreakerProtected` | 4 | `Behaviors\CircuitBreakerBehavior.cs` | 5 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ICurrentUser` | 4 | `MultiTenancy\ICurrentUser.cs` | 6 |
| `Mvp24Hours.Infrastructure.Cqrs` | `IHasTimeout` | 4 | `Behaviors\TimeoutBehavior.cs` | 1 |
| `Mvp24Hours.Infrastructure.Cqrs` | `IIdempotentCommand` | 4 | `Behaviors\IdempotencyBehavior.cs` | 2 |
| `Mvp24Hours.Infrastructure.Cqrs` | `INativeResilient` | 4 | `Behaviors\NativeResilienceBehavior.cs` | 1 |
| `Mvp24Hours.Infrastructure.Cqrs` | `IRetryable` | 4 | `Behaviors\RetryBehavior.cs` | 8 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ITenantAware` | 4 | `Behaviors\TenantBehavior.cs` | 1 |
| `Mvp24Hours.Infrastructure.Cqrs` | `ITenantContext` | 4 | `MultiTenancy\ITenantContext.cs` | 1 |
| `Mvp24Hours.Infrastructure.Cqrs` | `IUserContext` | 4 | `Behaviors\AuthorizationBehavior.cs` | 7 |
| `Mvp24Hours.Infrastructure.CronJob` | `AdvancedCronJobOptions` | 1 | `Services\AdvancedCronJobService.cs` | 4 |
| `Mvp24Hours.Infrastructure.CronJob` | `AdvancedCronJobService` | 1 | `Services\AdvancedCronJobService.cs` | 140 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronExpressionParser` | 1 | `Scheduling\CronExpressionParser.cs` | 125 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobAdvancedOptions` | 1 | `Extensions\CronJobAdvancedExtensions.cs` | 5 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobContext` | 1 | `Context\CronJobContext.cs` | 67 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobContextAccessor` | 1 | `Context\CronJobContextAccessor.cs` | 26 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobController` | 1 | `Control\CronJobController.cs` | 152 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobDependency` | 1 | `Dependencies\ICronJobDependency.cs` | 12 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobDependencyBuilder` | 1 | `Dependencies\ICronJobDependency.cs` | 25 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobEventDispatcher` | 1 | `Events\CronJobEventDispatcher.cs` | 91 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobEventHandlerBase` | 1 | `Events\ICronJobEventHandler.cs` | 19 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobGlobalOptions` | 1 | `Configuration\CronJobGlobalOptions.cs` | 56 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobGlobalOptionsValidator` | 1 | `Configuration\CronJobOptionsValidator.cs` | 49 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobOptions` | 1 | `Configuration\CronJobOptions.cs` | 62 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobOptionsValidator` | 1 | `Configuration\CronJobOptionsValidator.cs` | 103 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobState` | 1 | `State\ICronJobStateStore.cs` | 58 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobStatus` | 1 | `Control\ICronJobController.cs` | 32 |
| `Mvp24Hours.Infrastructure.CronJob` | `DistributedLockInfo` | 1 | `Resiliency\IDistributedCronJobLock.cs` | 5 |
| `Mvp24Hours.Infrastructure.CronJob` | `InMemoryCronJobDependencyTracker` | 1 | `Dependencies\InMemoryCronJobDependencyTracker.cs` | 106 |
| `Mvp24Hours.Infrastructure.CronJob` | `InMemoryCronJobStateStore` | 1 | `State\InMemoryCronJobStateStore.cs` | 56 |
| `Mvp24Hours.Infrastructure.CronJob` | `InMemoryDistributedCronJobLock` | 1 | `Resiliency\InMemoryDistributedCronJobLock.cs` | 122 |
| `Mvp24Hours.Infrastructure.CronJob` | `JobCompletionRecord` | 1 | `Dependencies\ICronJobDependency.cs` | 4 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobAdvancedExtensions` | 3 | `Extensions\CronJobAdvancedExtensions.cs` | 84 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobConfigurationExtensions` | 3 | `Configuration\CronJobConfigurationExtensions.cs` | 220 |
| `Mvp24Hours.Infrastructure.CronJob` | `CronJobObservabilityExtensions` | 3 | `Observability\CronJobObservabilityExtensions.cs` | 46 |
| `Mvp24Hours.Infrastructure.CronJob` | `ScheduledServiceExtensions` | 3 | `Extensions\ScheduledServiceExtensions.cs` | 120 |
| `Mvp24Hours.Infrastructure.CronJob` | `ICronJobEventHandler` | 4 | `Events\ICronJobEventHandler.cs` | 1 |

---

## Top gaps por pasta (Prioridade 1)

Pastas com mais tipos P1 sem cobertura — guia rápido para as fases 2+.

| Projeto | Pasta | Tipos P1 |
|---------|-------|---------:|
| `Mvp24Hours.Core` | `Observability/` | 93 |
| `Mvp24Hours.Application` | `Logic/` | 69 |
| `Mvp24Hours.WebAPI` | `Configuration/` | 68 |
| `Mvp24Hours.Core` | `Contract/` | 48 |
| `Mvp24Hours.Infrastructure` | `Http/` | 42 |
| `Mvp24Hours.Application` | `Contract/` | 39 |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | `Advanced/` | 39 |
| `Mvp24Hours.Infrastructure` | `Email/` | 35 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | `Configuration/` | 34 |
| `Mvp24Hours.Core` | `ValueObjects/` | 34 |
| `Mvp24Hours.Infrastructure.Pipe` | `AdvancedFlow/` | 34 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | `Pipeline/` | 29 |
| `Mvp24Hours.Infrastructure.Pipe` | `Observability/` | 28 |
| `Mvp24Hours.Infrastructure.Pipe` | `Operations/` | 27 |
| `Mvp24Hours.Infrastructure` | `Testing/` | 26 |
| `Mvp24Hours.Infrastructure` | `BackgroundJobs/` | 25 |
| `Mvp24Hours.Core` | `Extensions/` | 24 |
| `Mvp24Hours.WebAPI` | `Middlewares/` | 24 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | `Saga/` | 22 |
| `Mvp24Hours.Core` | `Domain/` | 21 |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | `Performance/` | 18 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | `Observability/` | 18 |
| `Mvp24Hours.Infrastructure` | `Resilience/` | 18 |
| `Mvp24Hours.Infrastructure.Pipe` | `Integration/` | 18 |
| `Mvp24Hours.Infrastructure` | `Sms/` | 17 |
| `Mvp24Hours.Infrastructure.Pipe` | `Resiliency/` | 17 |
| `Mvp24Hours.Core` | `Infrastructure/` | 16 |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | `Resiliency/` | 16 |
| `Mvp24Hours.Core` | `Serialization/` | 15 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | `Topology/` | 15 |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | `Testing/` | 13 |
| `Mvp24Hours.Infrastructure` | `HealthChecks/` | 13 |
| `Mvp24Hours.Infrastructure` | `DistributedLocking/` | 13 |
| `Mvp24Hours.Infrastructure.Cqrs` | `Behaviors/` | 13 |
| `Mvp24Hours.Infrastructure.Data.MongoDb` | `Observability/` | 13 |
| `Mvp24Hours.Core` | `Exceptions/` | 13 |
| `Mvp24Hours.Infrastructure.RabbitMQ` | `Transactional/` | 12 |
| `Mvp24Hours.Infrastructure.Cqrs` | `Projections/` | 12 |
| `Mvp24Hours.Infrastructure` | `FileStorage/` | 12 |
| `Mvp24Hours.Infrastructure.Pipe` | `Typed/` | 11 |

---

## Notas metodológicas

1. Varredura regex de `public (class|interface|enum|record|struct)` — nested types e `file`-scoped raros podem ficar de fora; tipos `internal` não entram.
2. Coverlet no baseline só instrumentou **3/12** assemblies; os outros 9 entram integralmente como "sem cobertura" até a instrumentação Coverlet ser corrigida/incluída no merge.
3. `HasDedicatedTestHint` é heurística de nome; testes de integração/padrões podem cobrir tipos sem arquivo homônimo.
4. Artefato máquina-legível: `tasks/classes-without-tests.raw.json`.
5. Baseline de cobertura: `tasks/coverage-baseline-tests.json`.

