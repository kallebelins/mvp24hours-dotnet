# Channels and Producer/Consumer Processing

Mvp24Hours.Core wraps `System.Threading.Channels` with DI-friendly contracts, timeouts, batching, presets, and high-level producer/consumer workers. Use bounded channels when producers must experience backpressure.

## Create a channel

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using Mvp24Hours.Core.Infrastructure.Channels;

using IChannel<Order> channel =
    Channels.CreateBounded<Order>(capacity: 100);

await channel.Writer.WriteAsync(order);

if (channel.Reader.TryRead(out Order? queued))
{
    await ProcessAsync(queued);
}

channel.Writer.TryComplete();
```

`Channels` is the static Mvp24Hours factory, not `System.Threading.Channels.Channel`. `ChannelFactory` provides the same basic creation through `IChannelFactory`.

## MvpChannelOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `IsBounded` | `bool` | `true` | Use a bounded channel |
| `Capacity` | `int` | `100` | Maximum buffered items when bounded |
| `FullMode` | `BoundedChannelFullMode` | `Wait` | Behavior when the buffer is full |
| `AllowSynchronousContinuations` | `bool` | `false` | Permit continuations on the completing thread |
| `SingleReader` | `bool` | `false` | Optimize when exactly one reader is guaranteed |
| `SingleWriter` | `bool` | `false` | Optimize when exactly one writer is guaranteed |
| `WriteTimeout` | `TimeSpan?` | `null` | Write timeout; `null` waits indefinitely |
| `ReadTimeout` | `TimeSpan?` | `null` | Read timeout; `null` waits indefinitely |

### Presets

| Factory | Important values |
|---|---|
| `Unbounded()` | `IsBounded = false` |
| `Bounded(capacity, fullMode)` | Bounded with caller capacity; mode defaults to `Wait` |
| `HighThroughput(capacity = 1000)` | `Wait`, synchronous continuations, single reader |
| `DropOldest(capacity = 100)` | Removes the oldest buffered item |
| `DropNewest(capacity = 100)` | Removes the newest buffered item |
| `DropWrite(capacity = 100)` | Drops the item being written |

For `Wait`, `WriteAsync` asynchronously waits for space. Drop modes trade delivery guarantees for bounded latency and memory.

## Read, write, and batch

```csharp
await channel.Writer.WriteManyAsync(orders, cancellationToken);

await foreach (Order item in channel.Reader.ReadAllAsync(cancellationToken))
{
    await ProcessAsync(item, cancellationToken);
}
```

Batch reads are available on the Mvp24Hours reader:

```csharp
await foreach (IReadOnlyList<Order> batch in channel.Reader.ReadBatchAsync(
    batchSize: 20,
    timeout: TimeSpan.FromSeconds(2),
    cancellationToken))
{
    await ProcessBatchAsync(batch, cancellationToken);
}
```

Complete the writer when no more items will arrive. Dispose the channel when its owning service stops.

## Dependency injection

```csharp
using Mvp24Hours.Core.Extensions;

services.AddMvpChannels();
services.AddBoundedChannel<Order>(100);
services.AddUnboundedChannel<DomainEvent>();
services.AddHighThroughputChannel<Metric>(1000);
services.AddDropOldestChannel<Snapshot>(50);
services.AddDropWriteChannel<DiagnosticEvent>(500);
```

`AddChannel<T>` registers `IChannel<T>`, `IChannelReader<T>`, and `IChannelWriter<T>` as singletons:

```csharp
services.AddChannel<Order>(options =>
{
    options.Capacity = 200;
    options.FullMode = BoundedChannelFullMode.Wait;
    options.SingleReader = true;
    options.WriteTimeout = TimeSpan.FromSeconds(5);
});
```

Use a keyed channel when several queues carry the same type:

```csharp
services.AddKeyedBoundedChannel<Order>("priority", 50);

public sealed class PriorityDispatcher(
    [FromKeyedServices("priority")] IChannel<Order> channel)
{
    public ValueTask DispatchAsync(Order order, CancellationToken cancellationToken) =>
        channel.Writer.WriteAsync(order, cancellationToken);
}
```

The keyed registration exposes the keyed `IChannel<T>` itself; it does not separately register keyed reader and writer contracts.

## ProducerConsumer

`ProducerConsumer<TItem>` owns its internal channel and starts one or more worker tasks:

```csharp
await using var processor = new ProducerConsumer<Order>(
    async (order, cancellationToken) =>
        await ProcessAsync(order, cancellationToken),
    workerCount: 4,
    options: new ProducerConsumerOptions
    {
        Capacity = 100,
        FullMode = BoundedChannelFullMode.Wait,
        ContinueOnError = false
    });

processor.Start();
await processor.ProduceManyAsync(orders, cancellationToken);
processor.Complete();
await processor.WaitForCompletionAsync(cancellationToken);
```

`WaitForCompletionAsync` starts workers if needed. `RunAsync` wraps start, producer execution, completion, and waiting. Producing after `Complete()` throws `InvalidOperationException`.

### ProducerConsumerOptions

| Property | Type | Default | Meaning |
|---|---|---|---|
| `IsBounded` | `bool` | `true` | Bound the internal queue |
| `Capacity` | `int` | `100` | Internal queue capacity |
| `FullMode` | `BoundedChannelFullMode` | `Wait` | Full-buffer behavior |
| `ContinueOnError` | `bool` | `true` | Log a processor exception and continue; when `false`, fail the worker |
| `AllowSynchronousContinuations` | `bool` | `false` | Permit synchronous channel continuations |

When `workerCount` is omitted, it defaults to `Environment.ProcessorCount`.

## Transforming results

`ProducerConsumer<TInput, TOutput>` publishes processed values as an async stream:

```csharp
await using var processor = new ProducerConsumer<Order, Receipt>(
    (order, cancellationToken) => CreateReceiptAsync(order, cancellationToken),
    workerCount: 2);

processor.Start();

foreach (Order order in orders)
{
    await processor.ProduceAsync(order, cancellationToken);
}

processor.Complete();

await foreach (Receipt receipt in processor.GetResultsAsync(cancellationToken))
{
    await SaveAsync(receipt, cancellationToken);
}
```

The output stream completes after all workers complete. Dispose cancels outstanding work and waits for workers.

## Choosing a strategy

| Requirement | Choice |
|---|---|
| Delivery with flow control | Bounded + `Wait` |
| Latest data matters most | `DropOldest` |
| Preserve older buffered data | `DropNewest` |
| Best-effort diagnostics | `DropWrite` |
| Unknown but safely bounded workload | Default `MvpChannelOptions` |
| Parallel item processing | `ProducerConsumer<T>` |
| Parallel transformation with streamed output | `ProducerConsumer<TInput, TOutput>` |

Unbounded channels have no backpressure and can grow until the process runs out of memory. Use them only when growth is externally constrained.

## Related documentation

- [Core infrastructure abstractions](../core/infrastructure-abstractions.md)
- [Keyed services](keyed-services.md)
- [Pipeline](../pipeline.md)
- [`System.Threading.Channels`](https://learn.microsoft.com/dotnet/core/extensions/channels)
