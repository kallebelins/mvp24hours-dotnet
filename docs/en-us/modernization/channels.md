# System.Threading.Channels (Producer/Consumer)

## Overview

**System.Threading.Channels** is a .NET native library for implementing high-performance producer/consumer patterns. It provides thread-safe, async-friendly data structures that enable efficient communication between producers and consumers.

Mvp24Hours integrates with System.Threading.Channels to provide:

- **High-performance in-memory queues** with backpressure support
- **Pipeline operation communication** for streaming data processing
- **RabbitMQ batch processing** with efficient message buffering
- **Generic producer/consumer patterns** for any async workload

## Why Use Channels?

Channels replace traditional approaches like `ConcurrentQueue`, `BlockingCollection`, and manual locking with a modern, async-first API:

| Traditional Approach | Channel Approach |
|---------------------|------------------|
| `ConcurrentQueue + AutoResetEvent` | `Channel<T>` |
| Manual backpressure with semaphores | `BoundedChannel` automatic backpressure |
| Blocking `Take()` calls | Async `ReadAsync()` |
| Complex cancellation handling | Built-in `CancellationToken` support |

## Core Concepts

### Channel Types

```csharp
// Unbounded: No limit, no backpressure
using var unbounded = Channels.CreateUnbounded<Order>();

// Bounded: Limited capacity, applies backpressure
using var bounded = Channels.CreateBounded<Order>(100);

// High-throughput: Optimized for performance
using var fast = Channels.CreateHighThroughput<Order>(1000);

// Drop strategies: Handle overflow gracefully
using var dropOldest = Channels.CreateDropOldest<Order>(100);
using var dropNewest = Channels.CreateDropNewest<Order>(100);
```

### Bounded Channel Full Modes

| Mode | Behavior |
|------|----------|
| `Wait` | Block producer until space available (default) |
| `DropOldest` | Remove oldest item to make room |
| `DropNewest` | Discard the new item being written |
| `DropWrite` | Return false for TryWrite, throw for WriteAsync |

## Basic Usage

### Creating a Channel

```csharp
using Mvp24Hours.Core.Infrastructure.Channels;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;

// Using factory (DI-friendly)
var factory = new ChannelFactory();
using var channel = factory.CreateBounded<Order>(100);

// Using static helper
using var channel = Channels.CreateBounded<Order>(100);

// Using constructor with options
using var channel = new MvpChannel<Order>(new ChannelOptions
{
    IsBounded = true,
    Capacity = 100,
    FullMode = BoundedChannelFullMode.Wait
});
```

### Writing to a Channel

```csharp
// Single item
await channel.Writer.WriteAsync(new Order { Id = 1 });

// Multiple items
await channel.Writer.WriteManyAsync(orders);

// Non-blocking write
if (channel.Writer.TryWrite(order))
{
    Console.WriteLine("Order queued");
}
else
{
    Console.WriteLine("Channel full!");
}

// Signal completion
channel.Writer.TryComplete();
```

### Reading from a Channel

```csharp
// Single item
var order = await channel.Reader.ReadAsync();

// All items (streaming)
await foreach (var order in channel.Reader.ReadAllAsync())
{
    await ProcessOrderAsync(order);
}

// Batch reading
await foreach (var batch in channel.Reader.ReadBatchAsync(
    batchSize: 10, 
    timeout: TimeSpan.FromSeconds(5)))
{
    await ProcessBatchAsync(batch);
}

// Non-blocking read
if (channel.Reader.TryRead(out var order))
{
    await ProcessOrderAsync(order);
}
```

## Producer/Consumer Pattern

### Simple Producer/Consumer

```csharp
using Mvp24Hours.Core.Infrastructure.Channels;

// Create producer-consumer with 4 workers
await using var pc = new ProducerConsumer<Order>(
    processor: async (order, ct) => 
    {
        await SaveOrderAsync(order, ct);
        await SendConfirmationAsync(order, ct);
    },
    workerCount: 4,
    options: new ProducerConsumerOptions 
    { 
        Capacity = 100,
        ContinueOnError = true 
    });

// Start workers
pc.Start();

// Produce items
foreach (var order in orders)
{
    await pc.ProduceAsync(order);
}

// Signal completion and wait
pc.Complete();
await pc.WaitForCompletionAsync();
```

### Producer/Consumer with Results

```csharp
await using var pc = new ProducerConsumer<Order, ProcessedOrder>(
    processor: async (order, ct) => 
    {
        var result = await ProcessOrderAsync(order, ct);
        return new ProcessedOrder { OrderId = order.Id, Result = result };
    },
    workerCount: Environment.ProcessorCount);

pc.Start();

// Produce in background
_ = Task.Run(async () =>
{
    foreach (var order in orders)
    {
        await pc.ProduceAsync(order);
    }
    pc.Complete();
});

// Consume results
await foreach (var result in pc.GetResultsAsync())
{
    Console.WriteLine($"Order {result.OrderId}: {result.Result}");
}
```

## Pipeline Integration

### Channel Pipeline

```csharp
using Mvp24Hours.Infrastructure.Pipe.Channels;

// Create a multi-stage pipeline
var pipeline = new ChannelPipeline<RawOrder, ProcessedOrder>(
    options: new ChannelPipelineOptions
    {
        ChannelCapacity = 100,
        MaxDegreeOfParallelism = 4
    },
    logger: logger);

// Add processing stages
pipeline
    .AddStage<RawOrder, ValidatedOrder>(order => ValidateOrder(order))
    .AddStageAsync<ValidatedOrder, EnrichedOrder>(
        async (order, ct) => await EnrichOrderAsync(order, ct))
    .AddStage<EnrichedOrder, ProcessedOrder>(order => FinalizeOrder(order));

// Process items with streaming output
await foreach (var result in pipeline.ProcessAsync(rawOrders))
{
    await SaveResultAsync(result);
}

// Or process in parallel
await foreach (var result in pipeline.ProcessParallelAsync(
    rawOrders, 
    maxDegreeOfParallelism: 4))
{
    await SaveResultAsync(result);
}
```

## RabbitMQ Integration

### Channel-Based Batch Processing

```csharp
using Mvp24Hours.Infrastructure.RabbitMQ.Channels;

// Create channel batch processor
await using var processor = new ChannelBatchProcessor<Order>(
    options: new BatchConsumerOptions
    {
        MaxBatchSize = 100,
        MinBatchSize = 10,
        BatchTimeout = TimeSpan.FromSeconds(5)
    },
    serviceProvider: serviceProvider,
    messageSerializer: serializer,
    logger: logger,
    channel: rabbitChannel);

// Start background processing
processor.Start();

// Messages are added via RabbitMQ consumer
consumer.Received += async (s, e) =>
{
    await processor.AddMessageAsync(e);
};

// Graceful shutdown
await processor.FlushAsync();
```

## Dependency Injection

### Registration

```csharp
using Mvp24Hours.Core.Extensions;

// Basic registration
services.AddMvpChannels();

// Register specific channels
services.AddBoundedChannel<Order>(100);
services.AddUnboundedChannel<Event>();
services.AddHighThroughputChannel<LogEntry>(1000);
services.AddDropOldestChannel<Metric>(500);

// Register with custom options
services.AddChannel<Message>(options =>
{
    options.Capacity = 200;
    options.FullMode = BoundedChannelFullMode.DropNewest;
    options.SingleReader = true;
});

// Keyed channels for different purposes
services.AddKeyedBoundedChannel<Order>("priority", 50);
services.AddKeyedBoundedChannel<Order>("standard", 200);
```

### Usage in Services

```csharp
public class OrderProcessor
{
    private readonly IChannel<Order> _orderChannel;
    private readonly IChannelWriter<Order> _writer;
    private readonly IChannelReader<Order> _reader;

    public OrderProcessor(IChannel<Order> channel)
    {
        _orderChannel = channel;
        _writer = channel.Writer;
        _reader = channel.Reader;
    }

    public async Task QueueOrderAsync(Order order)
    {
        await _writer.WriteAsync(order);
    }

    public async Task ProcessOrdersAsync(CancellationToken ct)
    {
        await foreach (var order in _reader.ReadAllAsync(ct))
        {
            await ProcessAsync(order);
        }
    }
}
```

## Best Practices

### 1. Choose the Right Channel Type

```csharp
// For producer-consumer with flow control
var channel = Channels.CreateBounded<T>(capacity);

// For event streaming where latest matters
var channel = Channels.CreateDropOldest<T>(capacity);

// For fire-and-forget with unlimited buffering
var channel = Channels.CreateUnbounded<T>();
```

### 2. Handle Backpressure

```csharp
// Option 1: Wait (default)
await channel.Writer.WriteAsync(item); // Blocks when full

// Option 2: Try non-blocking
if (!channel.Writer.TryWrite(item))
{
    // Handle overflow
    await _fallbackQueue.EnqueueAsync(item);
}

// Option 3: Use drop strategies
var channel = Channels.CreateDropOldest<T>(capacity);
```

### 3. Always Complete Writers

```csharp
try
{
    foreach (var item in items)
    {
        await channel.Writer.WriteAsync(item);
    }
}
finally
{
    channel.Writer.TryComplete();
}
```

### 4. Use Cancellation Tokens

```csharp
await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
{
    await ProcessAsync(item, cancellationToken);
}
```

## Performance Considerations

| Scenario | Recommendation |
|----------|----------------|
| High-throughput | Use `SingleReader`/`SingleWriter` when applicable |
| Memory-sensitive | Use bounded channels with appropriate capacity |
| Real-time data | Use `DropOldest` to prioritize recent data |
| Batch processing | Use `ReadBatchAsync` to reduce overhead |

## Migration from ConcurrentQueue

### Before (ConcurrentQueue)

```csharp
private readonly ConcurrentQueue<Order> _queue = new();
private readonly AutoResetEvent _signal = new(false);

public void Enqueue(Order order)
{
    _queue.Enqueue(order);
    _signal.Set();
}

public async Task ProcessAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        _signal.WaitOne(TimeSpan.FromSeconds(1));
        while (_queue.TryDequeue(out var order))
        {
            await ProcessOrderAsync(order);
        }
    }
}
```

### After (Channel)

```csharp
private readonly IChannel<Order> _channel;

public async Task EnqueueAsync(Order order)
{
    await _channel.Writer.WriteAsync(order);
}

public async Task ProcessAsync(CancellationToken ct)
{
    await foreach (var order in _channel.Reader.ReadAllAsync(ct))
    {
        await ProcessOrderAsync(order);
    }
}
```

## See Also

- [Rate Limiting](rate-limiting.md) - Rate limiting with `System.Threading.RateLimiting`
- [Periodic Timer](periodic-timer.md) - Modern timer patterns
- [Time Provider](time-provider.md) - Time abstraction for testing

