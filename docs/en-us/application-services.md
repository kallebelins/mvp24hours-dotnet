# Application services

`Mvp24Hours.Application` provides repository-backed service bases, DTO mapping, validation, transactions, result mapping, query caching, application events, pagination and module-level DI registration for .NET 10.

## Install and choose a module

```bash
dotnet add package Mvp24Hours.Application
```

```csharp
var assembly = typeof(CustomerService).Assembly;

services.AddMvp24HoursApplicationMinimal(assembly); // mapper + application services
services.AddMvp24HoursApplicationForApi(assembly);  // validation, transactions, resilience, observability, pagination, specs
services.AddMvp24HoursApplicationFull(assembly);    // all features, including cache and events
```

| Module | Mapper/services | Validation | Transactions | Resilience/observability | Cache/events | Pagination/specifications |
|---|---|---|---|---|---|---|
| `Minimal` | yes | no | no | no | no | no |
| `ForApi` | yes | yes | yes | yes | no | yes |
| `Full` | yes | yes | yes | yes | yes | yes |

`Full` requires an `IDistributedCache` registration. All module methods require at least one assembly.

## `ApplicationModuleOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `EnableAutoMapper` | `bool` | `true` | Scans and registers AutoMapper profiles. |
| `EnableApplicationServices` | `bool` | `true` | Scans supported application-service interfaces. |
| `EnableValidation` | `bool` | `true` | Registers validators and validation services. |
| `EnableTransactions` | `bool` | `true` | Registers transaction scopes. |
| `EnableResilience` | `bool` | `true` | Registers exception-to-result mapping. |
| `EnableObservability` | `bool` | `true` | Registers application metrics/audit services. |
| `EnableCache` | `bool` | `false` | Registers query caching; requires `IDistributedCache`. |
| `EnableEvents` | `bool` | `false` | Registers dispatcher and scanned handlers. |
| `EnablePagination` | `bool` | `true` | Registers `PaginationOptions`. |
| `EnableSpecifications` | `bool` | `true` | Registers specification services. |
| `EnableBulkOperations` | `bool` | `false` | Feature marker; bulk bases use repositories and add no DI service. |
| `EnableConventionBasedRegistration` | `bool` | `true` | Scans marker interfaces for service registration. |
| `ApplicationServiceLifetime` | `ServiceLifetime` | `Scoped` | Lifetime for scanned application services. |
| `ValidatorLifetime` | `ServiceLifetime` | `Scoped` | Lifetime for validators. |
| `ConfigureAutoMapper` | `Action<IMapperConfigurationExpression>?` | `null` | Extra mapper configuration. |
| `ValidationServiceOptions` | `Action<ValidationServiceOptions>?` | `null` | Validation settings. |
| `TransactionOptions` | `Action<TransactionScopeOptions>` | no-op | Transaction settings. |
| `ResilienceOptions` | `Action<ExceptionMappingOptions>` | no-op | Exception mappings. |
| `ObservabilityOptions` | `Action<ApplicationObservabilityOptions>` | no-op | Metrics/audit settings. |
| `CacheOptions` | `Action<QueryCacheOptions>` | no-op | Query-cache settings. |
| `EventOptions` | `Action<ApplicationEventDispatcherOptions>` | no-op | Event dispatch settings. |
| `PaginationOptions` | `Action<PaginationOptions>` | no-op | Pagination settings. |

```csharp
services.AddDistributedMemoryCache();
services.AddMvp24HoursApplicationModule(options =>
{
    options.EnableCache = true;
    options.EnableEvents = true;
    options.ValidationServiceOptions = v => v.ValidateAllNestedObjects = false;
    options.PaginationOptions = p => p.MaxPageSize = 200;
    options.ResilienceOptions = r =>
        r.AddMapping<DuplicateCustomerException>(
            ResultStatusCode.Conflict, "CUSTOMER_DUPLICATE");
}, typeof(CustomerService).Assembly);
```

## Service bases and DTO mapping

Real service bases include:

- `ApplicationServiceBase<TEntity,TUoW>` and `ApplicationServiceBaseAsync<TEntity,TUoW>`
- `ApplicationServiceBaseWithDto...` and `ApplicationServiceBaseWithSeparateDtos...` sync/async variants
- `QueryServiceBaseAsync`, `CommandServiceBaseAsync`, and bulk command bases
- `CacheableQueryServiceBaseAsync`, `CacheableApplicationServiceBaseAsync`
- `EventAwareCommandServiceBaseAsync` and `ObservableApplicationServiceBaseAsync`

```csharp
public sealed class CustomerService(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    IValidator<Customer>? entityValidator = null,
    IValidator<CreateCustomerDto>? createValidator = null,
    IValidator<UpdateCustomerDto>? updateValidator = null)
    : ApplicationServiceBaseWithSeparateDtosAsync<
        Customer, CustomerDto, CreateCustomerDto, UpdateCustomerDto, IUnitOfWorkAsync>(
        unitOfWork, mapper, entityValidator, createValidator, updateValidator);
```

### PATCH semantics

`Patch`/`PatchAsync` call the `protected virtual ApplyPatchToEntity(TUpdateDto, TEntity)`
extension point. It copies each non-null DTO property to the entity property with the same
name. The DTO-to-entity property pairs are resolved by reflection once per
`(TUpdateDto, TEntity)` combination and cached, so repeated PATCH requests do not re-scan
the types.

A pair is only produced when the entity exposes a public instance property with the same
name, that property is writable, and its type is assignable from the DTO property type.

Known limitations of this default implementation:

| Limitation | Effect | Workaround |
|---|---|---|
| `null` is the "not informed" marker | A field cannot be cleared through PATCH. | Use `Modify`/`ModifyAsync` (full update), or override `ApplyPatchToEntity`. |
| Non-nullable value types are always applied | `int 0`, `bool false` and `DateTime.MinValue` are indistinguishable from "informed as default". | Declare **both** sides as nullable (`int?` on the DTO **and** on the entity): `int` is not assignable from `int?`, so a nullable DTO property over a non-nullable entity property is skipped instead. |
| Unmatched, read-only and type-incompatible properties are skipped silently | No exception is raised. | A debug log entry is written once, when the map is built for the type pair. Enable `Debug` logging on the service to inspect it. |

Override `ApplyPatchToEntity` when the domain needs different semantics (explicit null
handling, per-field opt-in, JSON Patch, and so on).

Register mapping and services separately when the unified module is unnecessary:

```csharp
services.AddMvp24HoursAutoMapper(typeof(CustomerProfile).Assembly);
services.AddMvp24HoursApplicationServices(typeof(CustomerService).Assembly);
services.AddMvp24HoursValidators(typeof(CustomerValidator).Assembly);
```

## Validation

```csharp
services.AddMvp24HoursValidationFromAssemblyContaining<CustomerValidator>(options =>
{
    options.UseFluentValidation = true;
    options.UseDataAnnotations = true;
    options.UseCascadeValidation = true;
});

services.AddDefaultValidationPipeline<CreateCustomerDto>();
```

| Name | Type | Default | Description |
|---|---|---|---|
| `UseFluentValidation` | `bool` | `true` | Runs registered FluentValidation validators. |
| `UseDataAnnotations` | `bool` | `true` | Runs DataAnnotations validation. |
| `UseCascadeValidation` | `bool` | `true` | Validates nested values through cascade support. |
| `ValidateAllNestedObjects` | `bool` | `false` | Traverses nested objects without an explicit marker. |

Custom pipelines use `AddValidationPipeline<T>(builder => ...)`; available built-in steps are null check, FluentValidation, DataAnnotations and cascade validation.

## Transactions

`AddTransactionScope` requires `IUnitOfWork` and/or `IUnitOfWorkAsync` to be registered.

```csharp
services.AddTransactionScope(options =>
{
    options.DefaultTimeoutSeconds = 30;
    options.DefaultIsolationLevel = TransactionIsolationLevel.ReadCommitted;
});

await using ITransactionScope transaction = transactionFactory.Create();
await orderRepository.AddAsync(order);
await transaction.CommitAsync();
```

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultTimeoutSeconds` | `int` | `30` | Transaction timeout. |
| `DefaultIsolationLevel` | `TransactionIsolationLevel` | `ReadCommitted` | Default isolation. |
| `EnableRetryOnTransientFailure` | `bool` | `false` | Enables transaction retry policy. |
| `MaxRetryAttempts` | `int` | `3` | Retry limit. |
| `EnableLogging` | `bool` | `true` | Logs transaction lifecycle. |
| `AutoRollbackOnDispose` | `bool` | `true` | Rolls back uncommitted scopes. |

`[Transactional]` is metadata for an AOP/interceptor integration; the package does not automatically intercept arbitrary methods merely because the attribute is present. Its properties include `ReadOnly`, `TimeoutSeconds`, `RequiresNew`, `Suppress`, `IsolationLevel`, retry controls, rollback exception lists and `Name`.

## Pagination

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultPageSize` | `int` | `20` | Page size when omitted. |
| `MaxPageSize` | `int` | `100` | Maximum accepted size. |
| `MinPageSize` | `int` | `1` | Minimum accepted size. |
| `ValidateParameters` | `bool` | `true` | Validates page inputs. |
| `NormalizePageNumbers` | `bool` | `false` | Normalizes out-of-range pages. |
| `IncludeHeaderMetadata` | `bool` | `true` | Enables pagination response metadata. |
| `TotalCountHeaderName` | `string` | `X-Total-Count` | Total-count header. |
| `LinkHeaderName` | `string` | `Link` | Navigation-link header. |

## Cacheable and event-aware services

```csharp
services.AddStackExchangeRedisCache(o => o.Configuration = redis);
services.AddMvpApplicationQueryCache(o =>
{
    o.EnableL1Cache = true;
    o.L1CacheDuration = TimeSpan.FromMinutes(1);
    o.DefaultDuration = TimeSpan.FromMinutes(5);
    o.KeyPrefix = "query:";
});

services.AddMvp24HoursApplicationEventsWithOutbox(
    dispatcherOptions => dispatcherOptions.Strategy = EventDispatchStrategy.Parallel,
    processorOptions => processorOptions.BatchSize = 100,
    typeof(CustomerCreatedHandler).Assembly);
```

The built-in outbox is in memory and loses events on restart. Register a persistent `IApplicationEventOutbox` for production.

## Exception-to-result mapping

```csharp
services.AddMvpResilience(options =>
{
    options.IncludeExceptionDetails = false;
    options.LogServerErrors = true;
    options.AddMapping<EntityNotFoundException>(
        ResultStatusCode.NotFound, "ENTITY_NOT_FOUND");
});

var result = await SafeExecutor.ExecuteOrNotFoundAsync(
    () => repository.GetByIdAsync(id),
    exceptionToResultMapper,
    "Customer not found",
    logger);
```

| Name | Type | Default | Description |
|---|---|---|---|
| `IncludeExceptionDetails` | `bool` | `false` | Exposes exception detail in results. |
| `IncludeStackTrace` | `bool` | `false` | Exposes stack traces. |
| `DefaultErrorMessage` | `string` | generic safe message | Message for unmapped exceptions. |
| `LogServerErrors` | `bool` | `true` | Logs server-class failures. |
| `LogClientErrors` | `bool` | `false` | Logs client-class failures. |
| `CustomMappings` | `Dictionary<Type,ExceptionMapping>` | empty | Custom status/error mappings. |

`SafeExecutor` supplies sync/async execution, result projection and null-to-NotFound variants. It uses `IExceptionToResultMapper`; it is not a retry executor.

## Test reference

The executable examples are in `Mvp24Hours.Application.Test` (module DI, validation, transactions, cache, events and `SafeExecutor`) and `Mvp24Hours.Application.Integration.Test`.

## Related pages

- [Database application service basics](database/use-service.md)
- [Mapping](mapping.md)
- [Validation](validation.md)
- [Specifications](specification.md)
- [Observability](observability/home.md)
