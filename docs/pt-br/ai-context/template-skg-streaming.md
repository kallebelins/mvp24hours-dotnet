# Template Streaming e Events - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para streaming de resultados e eventos em tempo real.

---

## Visão Geral

Streaming permite monitoramento em tempo real:
- Eventos de progresso de nós
- Streaming de saída da IA
- Notificações de estado
- Consumo assíncrono de eventos

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| UI em tempo real | ✅ Recomendado |
| Monitoramento de progresso | ✅ Recomendado |
| Logs de execução | ✅ Recomendado |
| Processamento batch | ⚠️ Pode ser desnecessário |

---

## Implementação

```csharp
using SemanticKernel.Graph.Streaming;
using System.Threading.Channels;

public enum GraphEventType
{
    WorkflowStarted,
    NodeStarted,
    NodeProgress,
    NodeCompleted,
    NodeFailed,
    WorkflowCompleted,
    WorkflowFailed
}

public class GraphEvent
{
    public string WorkflowId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public GraphEventType Type { get; set; }
    public object? Data { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class StreamingGraphExecutor
{
    private readonly GraphExecutor _executor;
    private readonly Channel<GraphEvent> _eventChannel;

    public StreamingGraphExecutor(GraphExecutor executor)
    {
        _executor = executor;
        _eventChannel = Channel.CreateUnbounded<GraphEvent>();
    }

    public ChannelReader<GraphEvent> Events => _eventChannel.Reader;

    public async Task<GraphExecutionResult> ExecuteWithStreamingAsync(
        Kernel kernel,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var workflowId = Guid.NewGuid().ToString();

        await EmitEventAsync(new GraphEvent
        {
            WorkflowId = workflowId,
            Type = GraphEventType.WorkflowStarted
        });

        // Configurar callbacks
        _executor.OnNodeStarted += async (nodeId) =>
        {
            await EmitEventAsync(new GraphEvent
            {
                WorkflowId = workflowId,
                NodeId = nodeId,
                Type = GraphEventType.NodeStarted
            });
        };

        _executor.OnNodeCompleted += async (nodeId, state) =>
        {
            await EmitEventAsync(new GraphEvent
            {
                WorkflowId = workflowId,
                NodeId = nodeId,
                Type = GraphEventType.NodeCompleted,
                Data = state.GetValue<object>("result")
            });
        };

        try
        {
            var result = await _executor.ExecuteAsync(kernel, arguments, cancellationToken);

            await EmitEventAsync(new GraphEvent
            {
                WorkflowId = workflowId,
                Type = GraphEventType.WorkflowCompleted,
                Data = result
            });

            return result;
        }
        catch (Exception ex)
        {
            await EmitEventAsync(new GraphEvent
            {
                WorkflowId = workflowId,
                Type = GraphEventType.WorkflowFailed,
                Data = ex.Message
            });
            throw;
        }
    }

    private async Task EmitEventAsync(GraphEvent evt)
    {
        await _eventChannel.Writer.WriteAsync(evt);
    }
}

// Integração com Web API usando SSE
[ApiController]
[Route("api/[controller]")]
public class StreamingController : ControllerBase
{
    private readonly StreamingGraphExecutor _executor;

    [HttpPost("execute")]
    public async Task ExecuteStreaming([FromBody] ExecuteRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");

        var kernel = HttpContext.RequestServices.GetRequiredService<Kernel>();
        var arguments = new KernelArguments { ["input"] = request.Input };

        // Iniciar execução em background
        var executionTask = _executor.ExecuteWithStreamingAsync(kernel, arguments, cancellationToken);

        // Stream de eventos
        await foreach (var evt in _executor.Events.ReadAllAsync(cancellationToken))
        {
            var json = JsonSerializer.Serialize(evt);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            if (evt.Type is GraphEventType.WorkflowCompleted or GraphEventType.WorkflowFailed)
                break;
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }
}

// Cliente JavaScript para consumir SSE
/*
const eventSource = new EventSource('/api/streaming/execute');

eventSource.onmessage = (event) => {
    if (event.data === '[DONE]') {
        eventSource.close();
        return;
    }
    
    const graphEvent = JSON.parse(event.data);
    console.log(`[${graphEvent.type}] Node: ${graphEvent.nodeId}`, graphEvent.data);
    
    // Atualizar UI baseado no evento
    updateProgressUI(graphEvent);
};
*/
```

---

## Boas Práticas

1. **Buffer**: Use channels bounded para controlar memória
2. **Heartbeat**: Envie heartbeats para conexões longas
3. **Reconexão**: Implemente lógica de reconexão no cliente
4. **Cleanup**: Feche channels quando workflow terminar

---

## Templates Relacionados

- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos
- [Observability](template-skg-observability.md) - Métricas e monitoramento

