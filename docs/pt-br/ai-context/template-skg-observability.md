# Template Observability - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para métricas, logs e visualização de grafos.

---

## Visão Geral

Observability permite monitoramento e análise:
- Métricas de performance por nó
- Timing de execução
- Visualização de grafos (DOT, Mermaid, JSON)
- Logging estruturado

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Monitoramento de produção | ✅ Recomendado |
| Debug de workflows | ✅ Recomendado |
| Otimização de performance | ✅ Recomendado |
| Desenvolvimento inicial | ⚠️ Pode ser overhead |

---

## Implementação

```csharp
using SemanticKernel.Graph.Metrics;

public class GraphPerformanceMetrics
{
    private readonly ConcurrentDictionary<string, NodeMetrics> _nodeMetrics = new();
    private readonly List<WorkflowExecution> _executions = new();

    public void RecordNodeStart(string nodeId)
    {
        var metrics = _nodeMetrics.GetOrAdd(nodeId, _ => new NodeMetrics { NodeId = nodeId });
        metrics.LastStartTime = DateTimeOffset.UtcNow;
    }

    public void RecordNodeEnd(string nodeId, bool success)
    {
        if (!_nodeMetrics.TryGetValue(nodeId, out var metrics)) return;

        var duration = DateTimeOffset.UtcNow - metrics.LastStartTime;
        metrics.ExecutionCount++;
        metrics.TotalDuration += duration;
        if (success) metrics.SuccessCount++;
        else metrics.FailureCount++;

        if (duration > metrics.MaxDuration) metrics.MaxDuration = duration;
        if (metrics.MinDuration == TimeSpan.Zero || duration < metrics.MinDuration)
            metrics.MinDuration = duration;
    }

    public void RecordWorkflowExecution(string workflowId, TimeSpan duration, bool success)
    {
        _executions.Add(new WorkflowExecution
        {
            WorkflowId = workflowId,
            Duration = duration,
            Success = success,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public MetricsReport GenerateReport()
    {
        return new MetricsReport
        {
            NodeMetrics = _nodeMetrics.Values.ToList(),
            TotalExecutions = _executions.Count,
            SuccessRate = _executions.Count > 0
                ? (double)_executions.Count(e => e.Success) / _executions.Count
                : 0,
            AverageWorkflowDuration = _executions.Count > 0
                ? TimeSpan.FromTicks((long)_executions.Average(e => e.Duration.Ticks))
                : TimeSpan.Zero
        };
    }
}

public class NodeMetrics
{
    public string NodeId { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public DateTimeOffset LastStartTime { get; set; }

    public TimeSpan AverageDuration => ExecutionCount > 0
        ? TimeSpan.FromTicks(TotalDuration.Ticks / ExecutionCount)
        : TimeSpan.Zero;

    public double SuccessRate => ExecutionCount > 0
        ? (double)SuccessCount / ExecutionCount
        : 0;
}

public class WorkflowExecution
{
    public string WorkflowId { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class MetricsReport
{
    public List<NodeMetrics> NodeMetrics { get; set; } = new();
    public int TotalExecutions { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageWorkflowDuration { get; set; }
}

// Visualização de Grafos
public static class GraphVisualizer
{
    public static string ToMermaid(GraphExecutor graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("graph TD");

        foreach (var node in graph.Nodes)
        {
            sb.AppendLine($"    {node.NodeId}[\"{node.Name ?? node.NodeId}\"]");
        }

        foreach (var edge in graph.Edges)
        {
            var label = edge.Condition != null ? $"|{edge.Label ?? "condition"}|" : "";
            sb.AppendLine($"    {edge.Source} -->{label} {edge.Target}");
        }

        return sb.ToString();
    }

    public static string ToDot(GraphExecutor graph)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph G {");
        sb.AppendLine("    rankdir=TB;");

        foreach (var node in graph.Nodes)
        {
            sb.AppendLine($"    {node.NodeId} [label=\"{node.Name ?? node.NodeId}\"];");
        }

        foreach (var edge in graph.Edges)
        {
            var label = edge.Condition != null ? $" [label=\"{edge.Label ?? "condition"}\"]" : "";
            sb.AppendLine($"    {edge.Source} -> {edge.Target}{label};");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    public static string ToJson(GraphExecutor graph)
    {
        var graphData = new
        {
            id = graph.GraphId,
            name = graph.Name,
            nodes = graph.Nodes.Select(n => new { id = n.NodeId, name = n.Name }),
            edges = graph.Edges.Select(e => new
            {
                source = e.Source,
                target = e.Target,
                hasCondition = e.Condition != null,
                label = e.Label
            })
        };

        return JsonSerializer.Serialize(graphData, new JsonSerializerOptions { WriteIndented = true });
    }
}

// Integração com Web API
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly GraphPerformanceMetrics _metrics;

    [HttpGet]
    public IActionResult GetMetrics()
    {
        var report = _metrics.GenerateReport();
        return Ok(report);
    }

    [HttpGet("visualization/{format}")]
    public IActionResult GetVisualization(string format, [FromServices] GraphExecutor graph)
    {
        return format.ToLower() switch
        {
            "mermaid" => Content(GraphVisualizer.ToMermaid(graph), "text/plain"),
            "dot" => Content(GraphVisualizer.ToDot(graph), "text/plain"),
            "json" => Ok(JsonSerializer.Deserialize<object>(GraphVisualizer.ToJson(graph))),
            _ => BadRequest($"Formato não suportado: {format}")
        };
    }
}
```

---

## Boas Práticas

1. **Sampling**: Em produção, use sampling para métricas
2. **Retenção**: Defina políticas de retenção de dados
3. **Alertas**: Configure alertas para anomalias
4. **Dashboards**: Crie dashboards para visualização

---

## Templates Relacionados

- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos
- [Streaming](template-skg-streaming.md) - Eventos em tempo real

