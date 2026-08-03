# Pipeline (Pipes and Filters)

`Mvp24Hours.Infrastructure.Pipe` executes synchronous, asynchronous, and typed operations in a defined flow. The package targets .NET 10.

## Install and register

```bash
dotnet add package Mvp24Hours.Infrastructure.Pipe
```

```csharp
using Mvp24Hours.Extensions;

builder.Services.AddMvp24HoursPipeline(options =>
{
    options.IsBreakOnFail = true;
    options.ForceRollbackOnFalure = true; // Public API keeps this spelling.
    options.DefaultOperationTimeout = TimeSpan.FromSeconds(10);
    options.ValidateBeforeExecute = true;
    options.UseMiddleware = true;
});

builder.Services.AddMvp24HoursPipelineAsync(options =>
{
    options.IsBreakOnFail = true;
    options.ForceRollbackOnFalure = true;
});
```

Both methods default to `ServiceLifetime.Scoped` and accept a factory and a `ServiceLifetime`. `Singleton` shares mutable pipeline state across callers and should only be used when the configured pipeline is safe for concurrent reuse.

## `PipelineOptions` and `PipelineAsyncOptions`

Both classes expose the same properties.

| Name | Type | Default | Description |
|---|---|---|---|
| `IsBreakOnFail` | `bool` | `false` | Stops after a failed operation/message. |
| `ForceRollbackOnFalure` | `bool` | `false` | Rolls back completed operations in reverse order after failure. |
| `AllowPropagateException` | `bool` | `false` | Rethrows an exception after pipeline handling. |
| `DefaultOperationTimeout` | `TimeSpan?` | `null` | Default operation timeout; null disables it. |
| `ValidateBeforeExecute` | `bool` | `false` | Runs the configured pipeline validator before execution. |
| `MaxOperations` | `int` | `1000` | Maximum operations accepted by the pipeline. |
| `UseMiddleware` | `bool` | `false` | Enables the middleware execution path. |
| `ExceptionMapper` | `IPipelineExceptionMapper?` | `null` | Converts exceptions into pipeline messages/results. |
| `Validator` | `IPipelineValidator?` | `null` | Validates the pipeline definition. |

The options are intentionally separate types: configure both registrations when the application resolves both `IPipeline` and `IPipelineAsync`.

## Operations and rollback

```csharp
public sealed class ReserveInventory : OperationBaseAsync
{
    public override Task ExecuteAsync(IPipelineMessage input)
    {
        input.AddContent("inventory-reserved", true);
        return Task.CompletedTask;
    }

    public override Task RollbackAsync(IPipelineMessage input)
    {
        input.AddContent("inventory-released", true);
        return Task.CompletedTask;
    }
}

var pipeline = serviceProvider.GetRequiredService<IPipelineAsync>();
pipeline.Add<ReserveInventory>();
await pipeline.ExecuteAsync(new PipelineMessage());
```

When `ForceRollbackOnFalure` is true, only successfully executed earlier operations are compensated, in reverse order. `IsRequired` operations can still execute when the message is locked.

## Validation, exception mapping, and middleware

```csharp
builder.Services
    .AddPipelineExceptionMapper(mapper =>
    {
        // Configure mappings on DefaultPipelineExceptionMapper.
    })
    .AddPipelineValidator()
    .AddPipelineLoggingMiddleware()
    .AddPipelineTimeoutMiddleware(TimeSpan.FromSeconds(10))
    .AddPipelineMiddleware<AuditPipelineMiddleware>();
```

Use `AddPipelineMiddlewareSync<TMiddleware>()` for `IPipelineMiddlewareSync`. The configured mapper and validator can also be supplied directly through the pipeline options.

## Typed pipelines

```csharp
builder.Services.AddTypedPipelineAsync<CreateOrder, OrderReceipt>(
    pipeline =>
    {
        pipeline.Add<CreateOrder, ValidatedOrder>(new ValidateOrder());
        pipeline.Add<ValidatedOrder, OrderReceipt>(new PersistOrder());
    });

var typed = serviceProvider
    .GetRequiredService<ITypedPipelineAsync<CreateOrder, OrderReceipt>>();
IOperationResult<OrderReceipt> result =
    await typed.ExecuteAsync(new CreateOrder(...), cancellationToken);
```

Use `AddTypedPipeline`, `AddTypedPipelineAsync`, `AddTypedOperation`, and `AddTypedOperationAsync`. Typed pipelines are scoped by default; typed operations are transient by default, and all four methods accept an explicit `ServiceLifetime`. Typed pipelines can be configured with `WithBreakOnFail()` and `WithRollbackOnFailure()`. The default exception mapper, validator, and middleware registrations are singleton, so custom singleton components must not capture scoped services.

## Fork/join

`ForkJoinOperation<TInput,TBranchInput,TBranchOutput,TOutput>` is an operation, not a fluent method on `IPipeline`:

```csharp
var forkJoin =
    new ForkJoinOperation<IEnumerable<int>, int, int, int>(
        fork: values => values,
        branch: value => OperationResult<int>.Success(value * 2),
        join: results =>
            OperationResult<int>.Success(results.Sum(x => x.Value)));

IOperationResult<int> total = forkJoin.Execute([1, 2, 3]);
```

Async branch and join delegates are also supported and honor cancellation.

## Priority flow

```csharp
var priority = new PriorityPipeline
{
    IsBreakOnFail = true
};

priority
    .Add(new PersistOperation(), PriorityLevel.High)
    .Add(new AuditOperation(), PriorityLevel.Normal);

IPipelineMessage message = priority.Execute(new PipelineMessage());
```

`PriorityPipeline` orders higher numeric priorities first and supports synchronous and asynchronous operations, explicit integer priorities, priority levels, and optional groups.

## Dependency graph

```csharp
var graph = new DependencyGraph<OrderContext>();
graph.AddNode(new LambdaDependencyGraphNode<OrderContext>(
    "validate",
    (context, _) => OperationResult<object>.Success(context)));
graph.AddNode(new LambdaDependencyGraphNode<OrderContext>(
    "persist",
    (context, dependencies) => OperationResult<object>.Success(context))
    .DependsOn("validate"));

var executor = new DependencyGraphExecutor<OrderContext>(
    graph,
    new DependencyGraphOptions { StopOnFirstFailure = true });
DependencyGraphResult<OrderContext> result =
    await executor.ExecuteAsync(context, cancellationToken);
```

### `DependencyGraphOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `MaxDegreeOfParallelism` | `int?` | `null` | Concurrency for independent nodes. |
| `StopOnFirstFailure` | `bool` | `true` | Stops scheduling after a failure. |
| `ExecutionTimeout` | `TimeSpan?` | `null` | Whole-graph timeout. |
| `NodeTimeout` | `TimeSpan?` | `null` | Per-node timeout. |
| `ValidateNoCycles` | `bool` | `true` | Rejects circular dependencies. |

DI registration is `AddDependencyGraphExecutor<TContext>()`.

## Saga orchestration

```csharp
var saga = new PipelineSagaOrchestrator<OrderContext>(
    new PipelineSagaOptions { AutoCompensateOnFailure = true })
    .AddStep(
        "reserve",
        (ctx, ct) => ReserveAsync(ctx, ct),
        (ctx, ct) => ReleaseAsync(ctx, ct))
    .AddStep(
        "charge",
        (ctx, ct) => ChargeAsync(ctx, ct),
        (ctx, ct) => RefundAsync(ctx, ct));

PipelineSagaResult<OrderContext> result =
    await saga.ExecuteAsync(context, cancellationToken);
```

### `PipelineSagaOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `AutoCompensateOnFailure` | `bool` | `true` | Compensates completed steps after failure. |
| `SagaTimeout` | `TimeSpan?` | `null` | Whole-saga timeout. |
| `StepTimeout` | `TimeSpan?` | `null` | Per-step timeout. |
| `CompensationTimeout` | `TimeSpan?` | `null` | Compensation timeout. |
| `ContinueCompensationOnError` | `bool` | `true` | Continues compensating after a compensation failure. |
| `EnableStatePersistence` | `bool` | `false` | Persists recoverable saga state. |
| `StepDelay` | `TimeSpan?` | `null` | Delay between steps. |

Use `AddPipelineSaga<TContext>()` and `AddInMemorySagaStateStore<TContext>()`, or register a durable `IPipelineSagaStateStore<TContext>`.

## Checkpoints

```csharp
builder.Services
    .AddInMemoryCheckpointStore()
    .AddCheckpointablePipeline(options =>
    {
        options.CheckpointInterval = 1;
        options.CheckpointOnError = true;
        options.AutoResume = false;
        options.CheckpointExpiration = TimeSpan.FromHours(24);
    });
```

### `CheckpointOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Enables checkpointing. |
| `CheckpointInterval` | `int` | `1` | Operations between automatic checkpoints. |
| `CheckpointOnError` | `bool` | `true` | Saves state when execution fails. |
| `AutoResume` | `bool` | `false` | Resumes from the latest valid checkpoint. |
| `CheckpointExpiration` | `TimeSpan?` | `null` | Resume lifetime. |
| `CleanupOnSuccess` | `bool` | `true` | Deletes checkpoints after success. |
| `StateSerializer` | `IStateSerializer?` | `null` | Custom serializer; DI adds `JsonStateSerializer`. |

Use `AddCheckpointStore<TStore>()` for durable state. `AddAdvancedPipelineFlow()` registers the in-memory store and checkpoint defaults.

## Caching operations

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddPipelineCaching(options =>
{
    options.DefaultAbsoluteExpiration = TimeSpan.FromMinutes(5);
    options.CacheFailedResults = false;
    options.CacheKeyPrefix = "pipe:";
});
builder.Services.AddPipelineCacheMiddleware();
```

`WithCaching(...)` wraps a typed async operation, and `AddCachedOperation<TOperation,TInput,TOutput>()` registers a caching wrapper.

### `CacheOperationOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `DefaultAbsoluteExpiration` | `TimeSpan` | `5 minutes` | Default result TTL. |
| `DefaultSlidingExpiration` | `TimeSpan?` | `null` | Optional sliding TTL. |
| `CacheFailedResults` | `bool` | `false` | Caches failed operation results. |
| `CacheKeyPrefix` | `string` | `pipe:` | Key namespace. |
| `UseCompression` | `bool` | `false` | Compresses cached results. |
| `CompressionThreshold` | `int` | `1024` | Minimum bytes before compression. |

## FluentValidation

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();
builder.Services.AddPipelineFluentValidation(options =>
{
    options.ThrowValidationException = false;
    options.FailOnMissingData = true;
    options.LockPipelineOnFailure = true;
});
builder.Services.AddFluentValidationOperation<CreateOrder>();
```

### `FluentValidationOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `IsRequired` | `bool` | `false` | Runs even when an earlier operation failed. |
| `ThrowValidationException` | `bool` | `false` | Throws instead of returning a failed operation result. |
| `ThrowOnValidatorException` | `bool` | `false` | Rethrows validator exceptions instead of mapping them to failures. |
| `FailOnMissingData` | `bool` | `true` | Fails when the value to validate is absent. |
| `LockPipelineOnFailure` | `bool` | `true` | Locks an `IPipelineMessage` after validation failure. |
| `IncludeNonErrorMessages` | `bool` | `true` | Includes warning/information validation messages. |
| `RuleSet` | `string?` | `null` | Validator rule set; null uses the default rules. |

Use `AddFluentValidationPipelineOperation<T>()`, `AddValidation()`, or `AddInlineValidation()` for the corresponding flow.

## OpenTelemetry

```csharp
builder.Services.AddPipelineOpenTelemetry(options =>
{
    options.ServiceName = "Orders";
    options.RecordExceptions = true;
    options.MinimumDurationMs = 0;
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
        tracing.AddSource(OpenTelemetryExtensions.ActivitySourceName));
```

### `OpenTelemetryOptions`

| Name | Type | Default | Description |
|---|---|---|---|
| `UseFullTypeName` | `bool` | `false` | Uses full operation type names for spans. |
| `IncludeInputDetails` | `bool` | `true` | Adds input details to span tags. |
| `IncludeMessageDetails` | `bool` | `true` | Adds pipeline-message details to span tags. |
| `RecordExceptions` | `bool` | `true` | Records exceptions as span events. |
| `CustomTags` | `Dictionary<string,object>?` | `null` | Tags applied to every span. |
| `ServiceName` | `string` | `Mvp24Hours.Pipeline` | Service name emitted on spans. |
| `ServiceVersion` | `string?` | `null` | Optional service version. |
| `CreateChildSpans` | `bool` | `true` | Creates child spans for nested operations. |
| `MinimumDurationMs` | `int` | `0` | Minimum recorded duration; zero records all spans. |

## Related

- [CQRS behaviors](cqrs/behaviors.md)
- [Caching advanced](caching-advanced.md)
- [Observability](observability/home.md)

> **Samples:** [`minimal-pipeline-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/minimal-pipeline-customer-api/CustomerAPI/README.md) (Minimal) · [`simple-pipeline-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/simple-pipeline-customer-api/CustomerAPI.WebAPI/README.md) (Simple) · [`complex-pipeline-builder-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-pipeline-builder-customer-api/CustomerAPI.WebAPI/README.md) (Complex builder) · [`complex-pipeline-ports-adapters-customer-api`](https://github.com/kallebelins/mvp24hours-dotnet/blob/main/samples/src/complex-pipeline-ports-adapters-customer-api/CustomerAPI.WebAPI/README.md) (ports and adapters)
