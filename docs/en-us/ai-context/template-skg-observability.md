# Observability with Graphs Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for implementing comprehensive observability in graph-based workflows using Semantic Kernel Graph.

---

## Overview

Observability enables monitoring, analysis, and visualization of graph execution performance. This template covers:
- Performance metrics collection
- Node execution monitoring
- Execution path analysis
- Metrics export and visualization
- Alerting and dashboards

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Production monitoring | ✅ Recommended |
| Performance optimization | ✅ Recommended |
| Debugging workflows | ✅ Recommended |
| Capacity planning | ✅ Recommended |
| Simple scripts | ⚠️ May add overhead |
| Development only | ⚠️ Use simpler logging |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.*" />
  <PackageReference Include="System.Diagnostics.DiagnosticSource" Version="8.*" />
</ItemGroup>
```

---

## Observability Architecture

```
┌────────────────────────────────────────────────────────────┐
│                  Observability System                       │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐     ┌───────────────────────────────┐│
│  │ Graph Executor   │────▶│    Metrics Collector          ││
│  └──────────────────┘     │  - Execution metrics          ││
│                           │  - Node metrics               ││
│                           │  - Resource metrics           ││
│                           └─────────────┬─────────────────┘│
│                                         │                   │
│           ┌─────────────────────────────┼──────────────────┐│
│           │                             │                  ││
│  ┌────────▼────────┐    ┌───────────────▼─────┐   ┌───────▼────────┐│
│  │   Aggregator    │    │     Exporter        │   │   Dashboard    ││
│  │  - Summarize    │    │  - JSON             │   │  - Real-time   ││
│  │  - Trends       │    │  - Prometheus       │   │  - Historical  ││
│  │  - Alerts       │    │  - CSV              │   │  - Alerts      ││
│  └─────────────────┘    └─────────────────────┘   └────────────────┘│
└────────────────────────────────────────────────────────────┘
```

---

## Core Components

### Configuration Models

```csharp
/// <summary>
/// Configuration options for metrics collection.
/// </summary>
public class GraphMetricsOptions
{
    /// <summary>
    /// Enable node-level metrics collection.
    /// </summary>
    public bool EnableNodeMetrics { get; set; } = true;

    /// <summary>
    /// Enable execution-level metrics.
    /// </summary>
    public bool EnableExecutionMetrics { get; set; } = true;

    /// <summary>
    /// Enable resource usage monitoring.
    /// </summary>
    public bool EnableResourceMonitoring { get; set; } = true;

    /// <summary>
    /// Interval for metrics collection.
    /// </summary>
    public TimeSpan MetricsCollectionInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum number of metrics to keep in history.
    /// </summary>
    public int MaxMetricsHistory { get; set; } = 10000;

    /// <summary>
    /// Enable metrics compression.
    /// </summary>
    public bool EnableMetricsCompression { get; set; } = true;

    /// <summary>
    /// Enable metrics aggregation.
    /// </summary>
    public bool EnableMetricsAggregation { get; set; } = true;

    /// <summary>
    /// Aggregation interval.
    /// </summary>
    public TimeSpan AggregationInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Creates development-friendly options.
    /// </summary>
    public static GraphMetricsOptions CreateDevelopmentOptions()
    {
        return new GraphMetricsOptions
        {
            MetricsCollectionInterval = TimeSpan.FromMilliseconds(50),
            MaxMetricsHistory = 1000,
            EnableResourceMonitoring = false
        };
    }

    /// <summary>
    /// Creates production-optimized options.
    /// </summary>
    public static GraphMetricsOptions CreateProductionOptions()
    {
        return new GraphMetricsOptions
        {
            MetricsCollectionInterval = TimeSpan.FromSeconds(1),
            MaxMetricsHistory = 100000,
            EnableResourceMonitoring = true,
            EnableMetricsCompression = true,
            EnableMetricsAggregation = true
        };
    }
}

/// <summary>
/// Export format for metrics.
/// </summary>
public enum MetricsExportFormat
{
    Json,
    Csv,
    Prometheus,
    OpenTelemetry
}

/// <summary>
/// Options for metrics export.
/// </summary>
public class GraphMetricsExportOptions
{
    public bool IndentedOutput { get; set; } = false;
    public bool IncludeTimestamps { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
}
```

---

## Implementation Patterns

### 1. Node Execution Tracker

```csharp
/// <summary>
/// Tracks execution of individual nodes.
/// </summary>
public class NodeExecutionTracker
{
    public string TrackerId { get; } = Guid.NewGuid().ToString();
    public string NodeId { get; }
    public string NodeName { get; }
    public string ExecutionId { get; }
    public DateTimeOffset StartTime { get; }
    public DateTimeOffset? EndTime { get; private set; }
    public bool? Success { get; private set; }
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
    public object? Result { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Dictionary<string, object> Metadata { get; } = new();

    public NodeExecutionTracker(string nodeId, string nodeName, string executionId)
    {
        NodeId = nodeId;
        NodeName = nodeName;
        ExecutionId = executionId;
        StartTime = DateTimeOffset.UtcNow;
    }

    public void Complete(bool success, object? result = null, string? errorMessage = null)
    {
        EndTime = DateTimeOffset.UtcNow;
        Success = success;
        Result = result;
        ErrorMessage = errorMessage;
    }
}
```

### 2. Performance Metrics Collector

```csharp
using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>
/// Collects and manages performance metrics for graph execution.
/// </summary>
public class GraphPerformanceMetrics : IDisposable
{
    private readonly GraphMetricsOptions _options;
    private readonly ConcurrentDictionary<string, NodeExecutionTracker> _activeTrackers = new();
    private readonly ConcurrentDictionary<string, List<NodeExecutionMetrics>> _nodeMetrics = new();
    private readonly ConcurrentDictionary<string, ExecutionPathMetrics> _executionPaths = new();
    private readonly Timer? _resourceTimer;
    private bool _disposed;

    public double CurrentCpuUsage { get; private set; }
    public double CurrentAvailableMemoryMB { get; private set; }

    public GraphPerformanceMetrics(GraphMetricsOptions? options = null)
    {
        _options = options ?? new GraphMetricsOptions();

        if (_options.EnableResourceMonitoring)
        {
            _resourceTimer = new Timer(
                _ => SampleResourceUsage(),
                null,
                _options.MetricsCollectionInterval,
                _options.MetricsCollectionInterval);
        }
    }

    /// <summary>
    /// Starts tracking a node execution.
    /// </summary>
    public NodeExecutionTracker StartNodeTracking(string nodeId, string nodeName, string executionId)
    {
        var tracker = new NodeExecutionTracker(nodeId, nodeName, executionId);
        _activeTrackers[tracker.TrackerId] = tracker;
        return tracker;
    }

    /// <summary>
    /// Completes tracking a node execution.
    /// </summary>
    public void CompleteNodeTracking(NodeExecutionTracker tracker, bool success, object? result = null)
    {
        tracker.Complete(success, result);
        _activeTrackers.TryRemove(tracker.TrackerId, out _);

        // Store metrics
        if (!_nodeMetrics.ContainsKey(tracker.NodeId))
        {
            _nodeMetrics[tracker.NodeId] = new List<NodeExecutionMetrics>();
        }

        _nodeMetrics[tracker.NodeId].Add(new NodeExecutionMetrics
        {
            NodeId = tracker.NodeId,
            NodeName = tracker.NodeName,
            ExecutionId = tracker.ExecutionId,
            StartTime = tracker.StartTime,
            EndTime = tracker.EndTime!.Value,
            Duration = tracker.Duration!.Value,
            Success = success
        });

        // Trim history if needed
        if (_nodeMetrics[tracker.NodeId].Count > _options.MaxMetricsHistory)
        {
            _nodeMetrics[tracker.NodeId].RemoveAt(0);
        }
    }

    /// <summary>
    /// Records an execution path.
    /// </summary>
    public void RecordExecutionPath(string executionId, string[] nodeIds, TimeSpan duration, bool success)
    {
        _executionPaths[executionId] = new ExecutionPathMetrics
        {
            ExecutionId = executionId,
            NodeIds = nodeIds,
            TotalDuration = duration,
            Success = success,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Gets aggregated metrics for a node.
    /// </summary>
    public NodeAggregatedMetrics GetNodeAggregatedMetrics(string nodeId)
    {
        if (!_nodeMetrics.TryGetValue(nodeId, out var metrics) || !metrics.Any())
        {
            return new NodeAggregatedMetrics { NodeId = nodeId };
        }

        return new NodeAggregatedMetrics
        {
            NodeId = nodeId,
            TotalExecutions = metrics.Count,
            SuccessCount = metrics.Count(m => m.Success),
            FailureCount = metrics.Count(m => !m.Success),
            AverageDuration = TimeSpan.FromTicks((long)metrics.Average(m => m.Duration.Ticks)),
            MinDuration = metrics.Min(m => m.Duration),
            MaxDuration = metrics.Max(m => m.Duration),
            P95Duration = CalculatePercentile(metrics.Select(m => m.Duration).ToList(), 0.95),
            P99Duration = CalculatePercentile(metrics.Select(m => m.Duration).ToList(), 0.99)
        };
    }

    /// <summary>
    /// Gets all node metrics.
    /// </summary>
    public IReadOnlyDictionary<string, List<NodeExecutionMetrics>> GetAllNodeMetrics()
    {
        return _nodeMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
    }

    /// <summary>
    /// Gets all execution paths.
    /// </summary>
    public IReadOnlyDictionary<string, ExecutionPathMetrics> GetAllExecutionPaths()
    {
        return _executionPaths.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private void SampleResourceUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            CurrentCpuUsage = process.TotalProcessorTime.TotalMilliseconds;
            CurrentAvailableMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        }
        catch
        {
            // Ignore resource sampling errors
        }
    }

    private TimeSpan CalculatePercentile(List<TimeSpan> values, double percentile)
    {
        if (!values.Any()) return TimeSpan.Zero;

        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _resourceTimer?.Dispose();
    }
}

public class NodeExecutionMetrics
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
}

public class NodeAggregatedMetrics
{
    public string NodeId { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessCount / TotalExecutions : 0;
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan P95Duration { get; set; }
    public TimeSpan P99Duration { get; set; }
}

public class ExecutionPathMetrics
{
    public string ExecutionId { get; set; } = string.Empty;
    public string[] NodeIds { get; set; } = Array.Empty<string>();
    public TimeSpan TotalDuration { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

### 3. Metrics Exporter

```csharp
using System.Text;
using System.Text.Json;

/// <summary>
/// Exports metrics to various formats.
/// </summary>
public class GraphMetricsExporter : IDisposable
{
    private readonly GraphMetricsExportOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public GraphMetricsExporter(GraphMetricsExportOptions? options = null)
    {
        _options = options ?? new GraphMetricsExportOptions();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = _options.IndentedOutput,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Exports metrics to the specified format.
    /// </summary>
    public string ExportMetrics(
        GraphPerformanceMetrics metrics,
        MetricsExportFormat format,
        TimeSpan? timeRange = null)
    {
        return format switch
        {
            MetricsExportFormat.Json => ExportToJson(metrics, timeRange),
            MetricsExportFormat.Csv => ExportToCsv(metrics, timeRange),
            MetricsExportFormat.Prometheus => ExportToPrometheus(metrics, timeRange),
            _ => throw new NotSupportedException($"Format {format} is not supported")
        };
    }

    private string ExportToJson(GraphPerformanceMetrics metrics, TimeSpan? timeRange)
    {
        var nodeMetrics = metrics.GetAllNodeMetrics();
        var executionPaths = metrics.GetAllExecutionPaths();

        var export = new
        {
            ExportedAt = DateTimeOffset.UtcNow,
            TimeRange = timeRange?.ToString(),
            NodeMetrics = nodeMetrics.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    Executions = kvp.Value.Count,
                    Aggregated = metrics.GetNodeAggregatedMetrics(kvp.Key)
                }),
            ExecutionPaths = executionPaths.Values.ToList(),
            ResourceMetrics = new
            {
                CpuUsage = metrics.CurrentCpuUsage,
                AvailableMemoryMB = metrics.CurrentAvailableMemoryMB
            }
        };

        return JsonSerializer.Serialize(export, _jsonOptions);
    }

    private string ExportToCsv(GraphPerformanceMetrics metrics, TimeSpan? timeRange)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NodeId,NodeName,ExecutionId,StartTime,EndTime,DurationMs,Success");

        foreach (var (nodeId, nodeMetricsList) in metrics.GetAllNodeMetrics())
        {
            foreach (var m in nodeMetricsList)
            {
                sb.AppendLine($"{m.NodeId},{m.NodeName},{m.ExecutionId},{m.StartTime:O},{m.EndTime:O},{m.Duration.TotalMilliseconds:F2},{m.Success}");
            }
        }

        return sb.ToString();
    }

    private string ExportToPrometheus(GraphPerformanceMetrics metrics, TimeSpan? timeRange)
    {
        var sb = new StringBuilder();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        foreach (var (nodeId, _) in metrics.GetAllNodeMetrics())
        {
            var agg = metrics.GetNodeAggregatedMetrics(nodeId);

            sb.AppendLine($"# HELP graph_node_executions_total Total number of node executions");
            sb.AppendLine($"# TYPE graph_node_executions_total counter");
            sb.AppendLine($"graph_node_executions_total{{node=\"{nodeId}\"}} {agg.TotalExecutions} {timestamp}");

            sb.AppendLine($"# HELP graph_node_success_rate Success rate of node executions");
            sb.AppendLine($"# TYPE graph_node_success_rate gauge");
            sb.AppendLine($"graph_node_success_rate{{node=\"{nodeId}\"}} {agg.SuccessRate:F4} {timestamp}");

            sb.AppendLine($"# HELP graph_node_duration_ms_avg Average execution duration in milliseconds");
            sb.AppendLine($"# TYPE graph_node_duration_ms_avg gauge");
            sb.AppendLine($"graph_node_duration_ms_avg{{node=\"{nodeId}\"}} {agg.AverageDuration.TotalMilliseconds:F2} {timestamp}");

            sb.AppendLine($"# HELP graph_node_duration_ms_p95 95th percentile execution duration");
            sb.AppendLine($"# TYPE graph_node_duration_ms_p95 gauge");
            sb.AppendLine($"graph_node_duration_ms_p95{{node=\"{nodeId}\"}} {agg.P95Duration.TotalMilliseconds:F2} {timestamp}");

            sb.AppendLine($"# HELP graph_node_duration_ms_p99 99th percentile execution duration");
            sb.AppendLine($"# TYPE graph_node_duration_ms_p99 gauge");
            sb.AppendLine($"graph_node_duration_ms_p99{{node=\"{nodeId}\"}} {agg.P99Duration.TotalMilliseconds:F2} {timestamp}");
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

### 4. Metrics Dashboard

```csharp
/// <summary>
/// Real-time metrics dashboard for monitoring.
/// </summary>
public class MetricsDashboard
{
    private readonly GraphPerformanceMetrics _metrics;
    private readonly Timer _updateTimer;
    private readonly TimeSpan _updateInterval;

    public event EventHandler<DashboardUpdate>? Updated;

    public MetricsDashboard(
        GraphPerformanceMetrics metrics,
        TimeSpan? updateInterval = null)
    {
        _metrics = metrics;
        _updateInterval = updateInterval ?? TimeSpan.FromMilliseconds(500);
        
        _updateTimer = new Timer(
            _ => EmitUpdate(),
            null,
            _updateInterval,
            _updateInterval);
    }

    private void EmitUpdate()
    {
        var update = new DashboardUpdate
        {
            Timestamp = DateTimeOffset.UtcNow,
            NodeSummaries = GetNodeSummaries(),
            ResourceUsage = new ResourceUsage
            {
                CpuUsage = _metrics.CurrentCpuUsage,
                MemoryMB = _metrics.CurrentAvailableMemoryMB
            },
            RecentExecutions = GetRecentExecutions()
        };

        Updated?.Invoke(this, update);
    }

    private List<NodeSummary> GetNodeSummaries()
    {
        var summaries = new List<NodeSummary>();

        foreach (var (nodeId, _) in _metrics.GetAllNodeMetrics())
        {
            var agg = _metrics.GetNodeAggregatedMetrics(nodeId);
            summaries.Add(new NodeSummary
            {
                NodeId = nodeId,
                TotalExecutions = agg.TotalExecutions,
                SuccessRate = agg.SuccessRate,
                AverageDurationMs = agg.AverageDuration.TotalMilliseconds,
                P99DurationMs = agg.P99Duration.TotalMilliseconds
            });
        }

        return summaries;
    }

    private List<RecentExecution> GetRecentExecutions()
    {
        return _metrics.GetAllExecutionPaths()
            .OrderByDescending(kvp => kvp.Value.Timestamp)
            .Take(10)
            .Select(kvp => new RecentExecution
            {
                ExecutionId = kvp.Key,
                NodeCount = kvp.Value.NodeIds.Length,
                DurationMs = kvp.Value.TotalDuration.TotalMilliseconds,
                Success = kvp.Value.Success,
                Timestamp = kvp.Value.Timestamp
            })
            .ToList();
    }

    public void Stop()
    {
        _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }
}

public class DashboardUpdate
{
    public DateTimeOffset Timestamp { get; set; }
    public List<NodeSummary> NodeSummaries { get; set; } = new();
    public ResourceUsage ResourceUsage { get; set; } = new();
    public List<RecentExecution> RecentExecutions { get; set; } = new();
}

public class NodeSummary
{
    public string NodeId { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDurationMs { get; set; }
    public double P99DurationMs { get; set; }
}

public class ResourceUsage
{
    public double CpuUsage { get; set; }
    public double MemoryMB { get; set; }
}

public class RecentExecution
{
    public string ExecutionId { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public double DurationMs { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
```

### 5. Alerting System

```csharp
/// <summary>
/// Alerting system for metrics thresholds.
/// </summary>
public class MetricsAlerting
{
    private readonly List<AlertRule> _rules = new();
    private readonly ILogger _logger;

    public event EventHandler<Alert>? AlertRaised;

    public MetricsAlerting(ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    public void AddRule(AlertRule rule)
    {
        _rules.Add(rule);
    }

    public void AddHighErrorRateRule(string nodeId, double threshold = 0.1)
    {
        _rules.Add(new AlertRule
        {
            RuleId = $"high_error_rate_{nodeId}",
            NodeId = nodeId,
            Severity = AlertSeverity.Critical,
            Condition = metrics => metrics.SuccessRate < (1 - threshold),
            Message = $"High error rate detected for node {nodeId}"
        });
    }

    public void AddSlowExecutionRule(string nodeId, TimeSpan threshold)
    {
        _rules.Add(new AlertRule
        {
            RuleId = $"slow_execution_{nodeId}",
            NodeId = nodeId,
            Severity = AlertSeverity.Warning,
            Condition = metrics => metrics.P95Duration > threshold,
            Message = $"Slow execution detected for node {nodeId}"
        });
    }

    public List<Alert> CheckAlerts(GraphPerformanceMetrics metrics)
    {
        var alerts = new List<Alert>();

        foreach (var rule in _rules)
        {
            var nodeMetrics = metrics.GetNodeAggregatedMetrics(rule.NodeId);
            
            if (rule.Condition(nodeMetrics))
            {
                var alert = new Alert
                {
                    RuleId = rule.RuleId,
                    Severity = rule.Severity,
                    Message = rule.Message,
                    NodeId = rule.NodeId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Metrics = nodeMetrics
                };

                alerts.Add(alert);
                AlertRaised?.Invoke(this, alert);
                
                _logger.LogWarning("Alert raised: {Message} (Severity: {Severity})", 
                    alert.Message, alert.Severity);
            }
        }

        return alerts;
    }
}

public class AlertRule
{
    public string RuleId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public Func<NodeAggregatedMetrics, bool> Condition { get; set; } = _ => false;
    public string Message { get; set; } = string.Empty;
}

public class Alert
{
    public string RuleId { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public NodeAggregatedMetrics? Metrics { get; set; }
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}
```

---

## Service Layer Integration

```csharp
public interface IObservabilityService
{
    void TrackNodeExecution(string nodeId, string nodeName, string executionId, Func<Task> action);
    Task<string> ExportMetricsAsync(MetricsExportFormat format, CancellationToken cancellationToken = default);
    DashboardUpdate GetCurrentDashboard();
    List<Alert> CheckAlerts();
}

public class ObservabilityService : IObservabilityService, IDisposable
{
    private readonly GraphPerformanceMetrics _metrics;
    private readonly GraphMetricsExporter _exporter;
    private readonly MetricsDashboard _dashboard;
    private readonly MetricsAlerting _alerting;
    private DashboardUpdate? _lastUpdate;

    public ObservabilityService(ILoggerFactory loggerFactory)
    {
        var options = GraphMetricsOptions.CreateProductionOptions();
        _metrics = new GraphPerformanceMetrics(options);
        _exporter = new GraphMetricsExporter(new GraphMetricsExportOptions { IndentedOutput = true });
        _dashboard = new MetricsDashboard(_metrics);
        _alerting = new MetricsAlerting(loggerFactory.CreateLogger<MetricsAlerting>());

        _dashboard.Updated += (_, update) => _lastUpdate = update;
    }

    public void TrackNodeExecution(string nodeId, string nodeName, string executionId, Func<Task> action)
    {
        var tracker = _metrics.StartNodeTracking(nodeId, nodeName, executionId);
        
        try
        {
            action().GetAwaiter().GetResult();
            _metrics.CompleteNodeTracking(tracker, success: true);
        }
        catch (Exception ex)
        {
            _metrics.CompleteNodeTracking(tracker, success: false);
            throw;
        }
    }

    public Task<string> ExportMetricsAsync(MetricsExportFormat format, CancellationToken cancellationToken = default)
    {
        var export = _exporter.ExportMetrics(_metrics, format, TimeSpan.FromHours(1));
        return Task.FromResult(export);
    }

    public DashboardUpdate GetCurrentDashboard()
    {
        return _lastUpdate ?? new DashboardUpdate();
    }

    public List<Alert> CheckAlerts()
    {
        return _alerting.CheckAlerts(_metrics);
    }

    public void Dispose()
    {
        _metrics.Dispose();
        _exporter.Dispose();
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IObservabilityService _service;

    public MetricsController(IObservabilityService service)
    {
        _service = service;
    }

    [HttpGet("export/{format}")]
    public async Task<IActionResult> ExportMetrics(
        MetricsExportFormat format,
        CancellationToken cancellationToken)
    {
        var export = await _service.ExportMetricsAsync(format, cancellationToken);
        
        var contentType = format switch
        {
            MetricsExportFormat.Json => "application/json",
            MetricsExportFormat.Csv => "text/csv",
            MetricsExportFormat.Prometheus => "text/plain",
            _ => "text/plain"
        };

        return Content(export, contentType);
    }

    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        var dashboard = _service.GetCurrentDashboard();
        return Ok(dashboard);
    }

    [HttpGet("alerts")]
    public IActionResult GetAlerts()
    {
        var alerts = _service.CheckAlerts();
        return Ok(alerts);
    }
}
```

---

## Testing

```csharp
using Xunit;

public class ObservabilityTests
{
    [Fact]
    public void PerformanceMetrics_TracksNodeExecutions()
    {
        // Arrange
        using var metrics = new GraphPerformanceMetrics();
        var tracker = metrics.StartNodeTracking("node-1", "Test Node", "exec-1");

        // Act
        Thread.Sleep(50); // Simulate work
        metrics.CompleteNodeTracking(tracker, success: true);

        // Assert
        var agg = metrics.GetNodeAggregatedMetrics("node-1");
        Assert.Equal(1, agg.TotalExecutions);
        Assert.Equal(1, agg.SuccessCount);
        Assert.True(agg.AverageDuration.TotalMilliseconds >= 50);
    }

    [Fact]
    public void MetricsExporter_ExportsToJson()
    {
        // Arrange
        using var metrics = new GraphPerformanceMetrics();
        using var exporter = new GraphMetricsExporter(new GraphMetricsExportOptions { IndentedOutput = true });

        var tracker = metrics.StartNodeTracking("node-1", "Test", "exec-1");
        metrics.CompleteNodeTracking(tracker, success: true);

        // Act
        var json = exporter.ExportMetrics(metrics, MetricsExportFormat.Json, TimeSpan.FromHours(1));

        // Assert
        Assert.Contains("node-1", json);
        Assert.Contains("exportedAt", json);
    }

    [Fact]
    public void MetricsExporter_ExportsToPrometheus()
    {
        // Arrange
        using var metrics = new GraphPerformanceMetrics();
        using var exporter = new GraphMetricsExporter();

        var tracker = metrics.StartNodeTracking("node-1", "Test", "exec-1");
        metrics.CompleteNodeTracking(tracker, success: true);

        // Act
        var prometheus = exporter.ExportMetrics(metrics, MetricsExportFormat.Prometheus, TimeSpan.FromHours(1));

        // Assert
        Assert.Contains("graph_node_executions_total", prometheus);
        Assert.Contains("graph_node_success_rate", prometheus);
    }

    [Fact]
    public void Alerting_RaisesAlertOnHighErrorRate()
    {
        // Arrange
        using var metrics = new GraphPerformanceMetrics();
        var alerting = new MetricsAlerting();
        alerting.AddHighErrorRateRule("node-1", threshold: 0.1);

        // Simulate failures
        for (int i = 0; i < 10; i++)
        {
            var tracker = metrics.StartNodeTracking("node-1", "Test", $"exec-{i}");
            metrics.CompleteNodeTracking(tracker, success: i % 5 == 0); // 80% failure rate
        }

        // Act
        var alerts = alerting.CheckAlerts(metrics);

        // Assert
        Assert.Single(alerts);
        Assert.Equal(AlertSeverity.Critical, alerts[0].Severity);
    }
}
```

---

## Best Practices

### Metrics Collection

1. **Selective Collection**: Only collect metrics you'll use
2. **Sampling**: Use sampling for high-volume metrics
3. **Aggregation**: Aggregate metrics to reduce storage
4. **Retention**: Configure appropriate retention periods

### Performance

1. **Async Operations**: Use async for metric storage
2. **Batching**: Batch metric writes
3. **Compression**: Compress historical metrics
4. **Indexing**: Index metrics for fast queries

### Alerting

1. **Meaningful Thresholds**: Set actionable thresholds
2. **Avoid Alert Fatigue**: Limit alert frequency
3. **Severity Levels**: Use appropriate severity levels
4. **Escalation**: Implement escalation policies

### Visualization

1. **Real-Time Updates**: Update dashboards in real-time
2. **Historical Views**: Provide historical analysis
3. **Drill-Down**: Enable detailed investigation
4. **Export Options**: Support multiple export formats

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [Streaming](template-skg-streaming.md) - Real-time events
- [Checkpointing](template-skg-checkpointing.md) - State persistence
- [Multi-Agent](template-skg-multi-agent.md) - Coordinated agents

---

## External References

- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Prometheus .NET Client](https://github.com/prometheus-net/prometheus-net)

