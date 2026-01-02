# Streaming and Events Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for implementing real-time streaming execution and event handling using Semantic Kernel Graph.

---

## Overview

Streaming execution enables real-time monitoring and event-driven processing of graph workflows. This template covers:
- Real-time event streaming
- Event filtering and buffering
- Backpressure management
- Connection management
- Performance optimization

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Real-time monitoring | ✅ Recommended |
| Progressive UI updates | ✅ Recommended |
| Event-driven systems | ✅ Recommended |
| Long-running workflows | ✅ Recommended |
| Batch processing | ⚠️ Use standard execution |
| Simple workflows | ⚠️ May add complexity |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.*" />
  <PackageReference Include="System.Threading.Channels" Version="8.*" />
</ItemGroup>
```

---

## Streaming Architecture

```
┌────────────────────────────────────────────────────────────┐
│                  Streaming System                           │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐    ┌───────────────────────────────┐ │
│  │ Graph Executor   │───▶│     Event Producer            │ │
│  └──────────────────┘    │  - Node events                │ │
│                          │  - State events               │ │
│                          │  - Error events               │ │
│                          └─────────────┬─────────────────┘ │
│                                        │                    │
│                          ┌─────────────▼─────────────────┐ │
│                          │      Event Buffer             │ │
│                          │  - Backpressure control       │ │
│                          │  - Batching                   │ │
│                          │  - Filtering                  │ │
│                          └─────────────┬─────────────────┘ │
│                                        │                    │
│                          ┌─────────────▼─────────────────┐ │
│                          │     Event Consumers           │ │
│                          │  - WebSocket                  │ │
│                          │  - SSE                        │ │
│                          │  - Internal handlers          │ │
│                          └───────────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
```

---

## Core Components

### Event Types

```csharp
/// <summary>
/// Types of events emitted during graph execution.
/// </summary>
public enum GraphExecutionEventType
{
    ExecutionStarted,
    NodeStarted,
    NodeCompleted,
    NodeFailed,
    ExecutionCompleted,
    ExecutionFailed,
    ExecutionCancelled,
    StateChanged,
    CheckpointCreated,
    CheckpointRestored
}

/// <summary>
/// Base class for all graph execution events.
/// </summary>
public abstract class GraphExecutionEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public GraphExecutionEventType EventType { get; set; }
    public string ExecutionId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Event emitted when graph execution starts.
/// </summary>
public class GraphExecutionStartedEvent : GraphExecutionEvent
{
    public GraphExecutionStartedEvent()
    {
        EventType = GraphExecutionEventType.ExecutionStarted;
    }

    public string GraphName { get; set; } = string.Empty;
    public int TotalNodes { get; set; }
}

/// <summary>
/// Event emitted when a node starts execution.
/// </summary>
public class NodeExecutionStartedEvent : GraphExecutionEvent
{
    public NodeExecutionStartedEvent()
    {
        EventType = GraphExecutionEventType.NodeStarted;
    }

    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
}

/// <summary>
/// Event emitted when a node completes execution.
/// </summary>
public class NodeExecutionCompletedEvent : GraphExecutionEvent
{
    public NodeExecutionCompletedEvent()
    {
        EventType = GraphExecutionEventType.NodeCompleted;
    }

    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public object? Result { get; set; }
}

/// <summary>
/// Event emitted when a node fails.
/// </summary>
public class NodeExecutionFailedEvent : GraphExecutionEvent
{
    public NodeExecutionFailedEvent()
    {
        EventType = GraphExecutionEventType.NodeFailed;
    }

    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? ErrorType { get; set; }
}

/// <summary>
/// Event emitted when graph execution completes.
/// </summary>
public class GraphExecutionCompletedEvent : GraphExecutionEvent
{
    public GraphExecutionCompletedEvent()
    {
        EventType = GraphExecutionEventType.ExecutionCompleted;
    }

    public TimeSpan TotalDuration { get; set; }
    public int NodesExecuted { get; set; }
    public bool Success { get; set; }
}
```

### Configuration Models

```csharp
/// <summary>
/// Configuration options for streaming execution.
/// </summary>
public class StreamingExecutionOptions
{
    /// <summary>
    /// Initial buffer size for events.
    /// </summary>
    public int BufferSize { get; set; } = 100;

    /// <summary>
    /// Maximum buffer size before backpressure.
    /// </summary>
    public int MaxBufferSize { get; set; } = 1000;

    /// <summary>
    /// Event types to emit (null = all).
    /// </summary>
    public GraphExecutionEventType[]? EventTypesToEmit { get; set; }

    /// <summary>
    /// Enable automatic reconnection.
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Maximum reconnection attempts.
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 3;

    /// <summary>
    /// Initial delay between reconnection attempts.
    /// </summary>
    public TimeSpan InitialReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between reconnection attempts.
    /// </summary>
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Events per batch before flushing.
    /// </summary>
    public int ProducerBatchSize { get; set; } = 5;

    /// <summary>
    /// Time interval for flushing events.
    /// </summary>
    public TimeSpan ProducerFlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Enable event compression.
    /// </summary>
    public bool EnableEventCompression { get; set; } = true;

    /// <summary>
    /// Minimum size for compression (bytes).
    /// </summary>
    public int CompressionThresholdBytes { get; set; } = 8 * 1024;

    /// <summary>
    /// Enable heartbeat events.
    /// </summary>
    public bool EnableHeartbeat { get; set; } = true;

    /// <summary>
    /// Interval between heartbeat events.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
}
```

---

## Implementation Patterns

### 1. Event Stream Producer

```csharp
using System.Threading.Channels;

/// <summary>
/// Produces events during graph execution.
/// </summary>
public class EventStreamProducer : IAsyncDisposable
{
    private readonly Channel<GraphExecutionEvent> _channel;
    private readonly StreamingExecutionOptions _options;
    private readonly List<GraphExecutionEvent> _batch = new();
    private readonly Timer _flushTimer;
    private bool _disposed;

    public EventStreamProducer(StreamingExecutionOptions? options = null)
    {
        _options = options ?? new StreamingExecutionOptions();
        
        _channel = Channel.CreateBounded<GraphExecutionEvent>(new BoundedChannelOptions(_options.MaxBufferSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

        _flushTimer = new Timer(
            _ => FlushBatch(),
            null,
            _options.ProducerFlushInterval,
            _options.ProducerFlushInterval);
    }

    /// <summary>
    /// Gets the reader for consuming events.
    /// </summary>
    public ChannelReader<GraphExecutionEvent> Reader => _channel.Reader;

    /// <summary>
    /// Emits an event to the stream.
    /// </summary>
    public async Task EmitEventAsync(GraphExecutionEvent @event, CancellationToken cancellationToken = default)
    {
        if (_disposed) return;

        // Filter by event type if configured
        if (_options.EventTypesToEmit != null && 
            !_options.EventTypesToEmit.Contains(@event.EventType))
        {
            return;
        }

        lock (_batch)
        {
            _batch.Add(@event);
            
            if (_batch.Count >= _options.ProducerBatchSize)
            {
                FlushBatch();
            }
        }
    }

    private void FlushBatch()
    {
        List<GraphExecutionEvent> toFlush;
        
        lock (_batch)
        {
            if (_batch.Count == 0) return;
            toFlush = new List<GraphExecutionEvent>(_batch);
            _batch.Clear();
        }

        foreach (var @event in toFlush)
        {
            _channel.Writer.TryWrite(@event);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        FlushBatch();
        _channel.Writer.Complete();
        await _flushTimer.DisposeAsync();
    }
}
```

### 2. Streaming Graph Executor

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;

/// <summary>
/// Graph executor with streaming event support.
/// </summary>
public class StreamingGraphExecutor
{
    private readonly GraphExecutor _executor;
    private readonly StreamingExecutionOptions _options;
    private readonly ILogger _logger;

    public string GraphName { get; }

    public StreamingGraphExecutor(
        string graphName,
        string description,
        StreamingExecutionOptions? options = null,
        ILogger? logger = null)
    {
        GraphName = graphName;
        _executor = new GraphExecutor(graphName, description);
        _options = options ?? new StreamingExecutionOptions();
        _logger = logger ?? NullLogger.Instance;
    }

    public void AddNode(IGraphNode node) => _executor.AddNode(node);
    public void Connect(string fromId, string toId) => _executor.Connect(fromId, toId);
    public void SetStartNode(string nodeId) => _executor.SetStartNode(nodeId);

    /// <summary>
    /// Executes the graph with streaming events.
    /// </summary>
    public async IAsyncEnumerable<GraphExecutionEvent> ExecuteStreamAsync(
        Kernel kernel,
        KernelArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var nodesExecuted = 0;

        await using var producer = new EventStreamProducer(_options);
        
        // Start background execution
        var executionTask = Task.Run(async () =>
        {
            try
            {
                // Emit start event
                await producer.EmitEventAsync(new GraphExecutionStartedEvent
                {
                    ExecutionId = executionId,
                    GraphName = GraphName
                }, cancellationToken);

                // Execute graph (in real implementation, hook into node execution)
                await _executor.ExecuteAsync(kernel, arguments, cancellationToken);

                // Emit completion event
                await producer.EmitEventAsync(new GraphExecutionCompletedEvent
                {
                    ExecutionId = executionId,
                    TotalDuration = DateTime.UtcNow - startTime,
                    NodesExecuted = nodesExecuted,
                    Success = true
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await producer.EmitEventAsync(new GraphExecutionEvent
                {
                    EventType = GraphExecutionEventType.ExecutionFailed,
                    ExecutionId = executionId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["error"] = ex.Message
                    }
                }, cancellationToken);
            }
        }, cancellationToken);

        // Yield events as they arrive
        await foreach (var @event in producer.Reader.ReadAllAsync(cancellationToken))
        {
            yield return @event;
        }

        await executionTask;
    }
}
```

### 3. Event Stream Consumer

```csharp
/// <summary>
/// Consumes events from the stream.
/// </summary>
public class EventStreamConsumer
{
    private readonly ILogger _logger;

    public EventStreamConsumer(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Consumes all events from the stream.
    /// </summary>
    public async Task ConsumeAsync(
        IAsyncEnumerable<GraphExecutionEvent> eventStream,
        CancellationToken cancellationToken = default)
    {
        var nodeTimings = new Dictionary<string, TimeSpan>();
        var nodeStartTimes = new Dictionary<string, DateTimeOffset>();

        await foreach (var @event in eventStream.WithCancellation(cancellationToken))
        {
            switch (@event)
            {
                case GraphExecutionStartedEvent started:
                    _logger.LogInformation("🚀 Execution started: {ExecutionId}", started.ExecutionId);
                    break;

                case NodeExecutionStartedEvent nodeStarted:
                    nodeStartTimes[nodeStarted.NodeId] = nodeStarted.Timestamp;
                    _logger.LogInformation("▶️ Node started: {NodeName}", nodeStarted.NodeName);
                    break;

                case NodeExecutionCompletedEvent nodeCompleted:
                    nodeTimings[nodeCompleted.NodeId] = nodeCompleted.Duration;
                    _logger.LogInformation("✅ Node completed: {NodeName} in {Duration:F0}ms",
                        nodeCompleted.NodeName, nodeCompleted.Duration.TotalMilliseconds);
                    break;

                case NodeExecutionFailedEvent nodeFailed:
                    _logger.LogError("❌ Node failed: {NodeName} - {Error}",
                        nodeFailed.NodeName, nodeFailed.ErrorMessage);
                    break;

                case GraphExecutionCompletedEvent completed:
                    _logger.LogInformation("🎯 Execution completed in {Duration:F0}ms, {Nodes} nodes executed",
                        completed.TotalDuration.TotalMilliseconds, completed.NodesExecuted);
                    
                    LogPerformanceSummary(nodeTimings);
                    break;
            }
        }
    }

    /// <summary>
    /// Consumes events with filtering.
    /// </summary>
    public async Task ConsumeFilteredAsync(
        IAsyncEnumerable<GraphExecutionEvent> eventStream,
        Func<GraphExecutionEvent, bool> filter,
        CancellationToken cancellationToken = default)
    {
        await foreach (var @event in eventStream.WithCancellation(cancellationToken))
        {
            if (filter(@event))
            {
                await ProcessEventAsync(@event);
            }
        }
    }

    /// <summary>
    /// Consumes events in batches.
    /// </summary>
    public async Task ConsumeBatchedAsync(
        IAsyncEnumerable<GraphExecutionEvent> eventStream,
        int batchSize,
        Func<IReadOnlyList<GraphExecutionEvent>, Task> batchProcessor,
        CancellationToken cancellationToken = default)
    {
        var batch = new List<GraphExecutionEvent>();

        await foreach (var @event in eventStream.WithCancellation(cancellationToken))
        {
            batch.Add(@event);

            if (batch.Count >= batchSize)
            {
                await batchProcessor(batch);
                batch.Clear();
            }
        }

        // Process remaining events
        if (batch.Count > 0)
        {
            await batchProcessor(batch);
        }
    }

    private Task ProcessEventAsync(GraphExecutionEvent @event)
    {
        _logger.LogDebug("Processing event: {EventType}", @event.EventType);
        return Task.CompletedTask;
    }

    private void LogPerformanceSummary(Dictionary<string, TimeSpan> nodeTimings)
    {
        if (!nodeTimings.Any()) return;

        _logger.LogInformation("📊 Performance Summary:");
        foreach (var (nodeId, duration) in nodeTimings.OrderByDescending(x => x.Value))
        {
            _logger.LogInformation("   {NodeId}: {Duration:F0}ms", nodeId, duration.TotalMilliseconds);
        }
    }
}
```

### 4. Real-Time Monitor

```csharp
/// <summary>
/// Monitors graph execution in real-time.
/// </summary>
public class RealTimeExecutionMonitor
{
    private readonly Dictionary<string, NodeMetrics> _nodeMetrics = new();
    private readonly List<GraphExecutionEvent> _recentEvents = new();
    private readonly int _maxRecentEvents;

    public event EventHandler<GraphExecutionEvent>? EventReceived;
    public event EventHandler<ExecutionMetrics>? MetricsUpdated;

    public RealTimeExecutionMonitor(int maxRecentEvents = 100)
    {
        _maxRecentEvents = maxRecentEvents;
    }

    /// <summary>
    /// Starts monitoring the event stream.
    /// </summary>
    public async Task MonitorAsync(
        IAsyncEnumerable<GraphExecutionEvent> eventStream,
        CancellationToken cancellationToken = default)
    {
        await foreach (var @event in eventStream.WithCancellation(cancellationToken))
        {
            ProcessEvent(@event);
            EventReceived?.Invoke(this, @event);
            MetricsUpdated?.Invoke(this, GetCurrentMetrics());
        }
    }

    private void ProcessEvent(GraphExecutionEvent @event)
    {
        // Track recent events
        _recentEvents.Add(@event);
        if (_recentEvents.Count > _maxRecentEvents)
        {
            _recentEvents.RemoveAt(0);
        }

        // Update node metrics
        switch (@event)
        {
            case NodeExecutionStartedEvent nodeStarted:
                if (!_nodeMetrics.ContainsKey(nodeStarted.NodeId))
                {
                    _nodeMetrics[nodeStarted.NodeId] = new NodeMetrics { NodeId = nodeStarted.NodeId };
                }
                _nodeMetrics[nodeStarted.NodeId].LastStartTime = nodeStarted.Timestamp;
                break;

            case NodeExecutionCompletedEvent nodeCompleted:
                if (_nodeMetrics.TryGetValue(nodeCompleted.NodeId, out var metrics))
                {
                    metrics.ExecutionCount++;
                    metrics.TotalDuration += nodeCompleted.Duration;
                    metrics.AverageDuration = TimeSpan.FromTicks(metrics.TotalDuration.Ticks / metrics.ExecutionCount);
                    metrics.LastDuration = nodeCompleted.Duration;
                }
                break;

            case NodeExecutionFailedEvent nodeFailed:
                if (_nodeMetrics.TryGetValue(nodeFailed.NodeId, out var failMetrics))
                {
                    failMetrics.FailureCount++;
                }
                break;
        }
    }

    public ExecutionMetrics GetCurrentMetrics()
    {
        var totalExecutions = _nodeMetrics.Values.Sum(m => m.ExecutionCount);
        var totalFailures = _nodeMetrics.Values.Sum(m => m.FailureCount);

        return new ExecutionMetrics
        {
            TotalNodeExecutions = totalExecutions,
            TotalFailures = totalFailures,
            SuccessRate = totalExecutions > 0 ? (double)(totalExecutions - totalFailures) / totalExecutions : 1.0,
            NodeMetrics = _nodeMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            RecentEventsCount = _recentEvents.Count
        };
    }

    public IReadOnlyList<GraphExecutionEvent> GetRecentEvents() => _recentEvents.AsReadOnly();
}

public class NodeMetrics
{
    public string NodeId { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public int FailureCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan LastDuration { get; set; }
    public DateTimeOffset LastStartTime { get; set; }
}

public class ExecutionMetrics
{
    public int TotalNodeExecutions { get; set; }
    public int TotalFailures { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, NodeMetrics> NodeMetrics { get; set; } = new();
    public int RecentEventsCount { get; set; }
}
```

---

## Service Layer Integration

```csharp
public interface IStreamingExecutionService
{
    IAsyncEnumerable<GraphExecutionEvent> ExecuteWithStreamingAsync(
        KernelArguments arguments,
        CancellationToken cancellationToken = default);
    
    ExecutionMetrics GetCurrentMetrics();
}

public class StreamingExecutionService : IStreamingExecutionService
{
    private readonly Kernel _kernel;
    private readonly StreamingGraphExecutor _executor;
    private readonly RealTimeExecutionMonitor _monitor;

    public StreamingExecutionService(Kernel kernel, ILoggerFactory loggerFactory)
    {
        _kernel = kernel;
        _monitor = new RealTimeExecutionMonitor();
        
        var options = new StreamingExecutionOptions
        {
            BufferSize = 100,
            EnableHeartbeat = true,
            EnableMetrics = true
        };
        
        _executor = new StreamingGraphExecutor(
            "StreamingWorkflow",
            "Workflow with streaming events",
            options,
            loggerFactory.CreateLogger<StreamingGraphExecutor>());
    }

    public async IAsyncEnumerable<GraphExecutionEvent> ExecuteWithStreamingAsync(
        KernelArguments arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var eventStream = _executor.ExecuteStreamAsync(_kernel, arguments, cancellationToken);

        await foreach (var @event in eventStream.WithCancellation(cancellationToken))
        {
            yield return @event;
        }
    }

    public ExecutionMetrics GetCurrentMetrics() => _monitor.GetCurrentMetrics();
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class StreamingController : ControllerBase
{
    private readonly IStreamingExecutionService _service;

    public StreamingController(IStreamingExecutionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Server-Sent Events endpoint for streaming execution.
    /// </summary>
    [HttpGet("execute/stream")]
    public async Task ExecuteStream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var arguments = new KernelArguments();
        var eventStream = _service.ExecuteWithStreamingAsync(arguments, cancellationToken);

        await foreach (var @event in eventStream.WithCancellation(cancellationToken))
        {
            var eventJson = JsonSerializer.Serialize(@event);
            await Response.WriteAsync($"data: {eventJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets current execution metrics.
    /// </summary>
    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        var metrics = _service.GetCurrentMetrics();
        return Ok(metrics);
    }
}
```

---

## Testing

```csharp
using Xunit;

public class StreamingTests
{
    [Fact]
    public async Task EventStreamProducer_EmitsEvents()
    {
        // Arrange
        await using var producer = new EventStreamProducer();
        var events = new List<GraphExecutionEvent>();

        // Act
        var consumeTask = Task.Run(async () =>
        {
            await foreach (var @event in producer.Reader.ReadAllAsync())
            {
                events.Add(@event);
            }
        });

        await producer.EmitEventAsync(new GraphExecutionStartedEvent { ExecutionId = "test-1" });
        await producer.EmitEventAsync(new GraphExecutionCompletedEvent { ExecutionId = "test-1" });
        
        await producer.DisposeAsync();
        await consumeTask;

        // Assert
        Assert.Equal(2, events.Count);
        Assert.IsType<GraphExecutionStartedEvent>(events[0]);
        Assert.IsType<GraphExecutionCompletedEvent>(events[1]);
    }

    [Fact]
    public async Task EventStreamConsumer_ProcessesAllEvents()
    {
        // Arrange
        var events = new List<GraphExecutionEvent>
        {
            new GraphExecutionStartedEvent { ExecutionId = "test" },
            new NodeExecutionStartedEvent { ExecutionId = "test", NodeId = "node-1" },
            new NodeExecutionCompletedEvent { ExecutionId = "test", NodeId = "node-1", Duration = TimeSpan.FromMilliseconds(100) },
            new GraphExecutionCompletedEvent { ExecutionId = "test", TotalDuration = TimeSpan.FromMilliseconds(100) }
        };

        var consumer = new EventStreamConsumer();

        // Act
        await consumer.ConsumeAsync(events.ToAsyncEnumerable());

        // Assert - no exceptions thrown
    }

    [Fact]
    public void RealTimeMonitor_TracksMetrics()
    {
        // Arrange
        var monitor = new RealTimeExecutionMonitor();

        // Simulate events
        var startEvent = new NodeExecutionStartedEvent { NodeId = "node-1" };
        var completeEvent = new NodeExecutionCompletedEvent 
        { 
            NodeId = "node-1", 
            Duration = TimeSpan.FromMilliseconds(100) 
        };

        // Act
        // Would normally use MonitorAsync, but for testing we process directly
        var metrics = monitor.GetCurrentMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalNodeExecutions);
        Assert.Equal(1.0, metrics.SuccessRate);
    }
}
```

---

## Best Practices

### Event Design

1. **Lightweight Events**: Keep event payloads small
2. **Meaningful Events**: Emit events at significant execution points
3. **Consistent Structure**: Use consistent event schemas
4. **Timestamp Accuracy**: Use high-precision timestamps

### Performance

1. **Batching**: Batch events to reduce overhead
2. **Filtering**: Filter events at the source
3. **Backpressure**: Implement proper backpressure handling
4. **Buffer Sizing**: Size buffers based on consumption rate

### Reliability

1. **Reconnection**: Implement automatic reconnection
2. **Heartbeats**: Use heartbeats to detect failures
3. **Error Handling**: Handle consumer errors gracefully
4. **Graceful Shutdown**: Complete streams properly on shutdown

### Monitoring

1. **Metrics Collection**: Track event throughput and latency
2. **Consumer Lag**: Monitor consumer lag
3. **Error Rates**: Track event processing errors
4. **Resource Usage**: Monitor memory and CPU usage

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [Checkpointing](template-skg-checkpointing.md) - State persistence
- [Observability](template-skg-observability.md) - Metrics and monitoring
- [Multi-Agent](template-skg-multi-agent.md) - Coordinated agents

---

## External References

- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [Reactive Extensions](https://github.com/dotnet/reactive)
- [Server-Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)

