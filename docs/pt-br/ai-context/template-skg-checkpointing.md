# Template Checkpointing e Recovery - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para persistência de estado e recuperação de workflows.

---

## Visão Geral

Checkpointing permite workflows resilientes:
- Persistência de estado entre nós
- Recuperação após falhas
- Replay de execuções
- Compressão de estado

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Workflows de longa duração | ✅ Recomendado |
| Processos críticos | ✅ Recomendado |
| Recuperação de falhas | ✅ Recomendado |
| Tarefas rápidas | ⚠️ Pode ser overhead |

---

## Implementação

```csharp
using SemanticKernel.Graph.Checkpointing;

public interface ICheckpointManager
{
    Task SaveAsync(string workflowId, WorkflowCheckpoint checkpoint, CancellationToken cancellationToken = default);
    Task<WorkflowCheckpoint?> LoadAsync(string workflowId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string workflowId, CancellationToken cancellationToken = default);
}

public class WorkflowCheckpoint
{
    public string WorkflowId { get; set; } = string.Empty;
    public string CurrentNodeId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<string> ExecutedNodes { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class FileCheckpointManager : ICheckpointManager
{
    private readonly string _basePath;

    public FileCheckpointManager(string basePath)
    {
        _basePath = basePath;
        Directory.CreateDirectory(_basePath);
    }

    public async Task SaveAsync(string workflowId, WorkflowCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        checkpoint.UpdatedAt = DateTimeOffset.UtcNow;
        var json = JsonSerializer.Serialize(checkpoint);
        var path = GetPath(workflowId);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public async Task<WorkflowCheckpoint?> LoadAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(workflowId);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<WorkflowCheckpoint>(json);
    }

    public Task DeleteAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var path = GetPath(workflowId);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string GetPath(string workflowId) => Path.Combine(_basePath, $"{workflowId}.json");
}

public class CheckpointedGraphExecutor
{
    private readonly GraphExecutor _executor;
    private readonly ICheckpointManager _checkpointManager;
    private readonly ILogger _logger;

    public CheckpointedGraphExecutor(
        GraphExecutor executor,
        ICheckpointManager checkpointManager,
        ILogger<CheckpointedGraphExecutor> logger)
    {
        _executor = executor;
        _checkpointManager = checkpointManager;
        _logger = logger;
    }

    public async Task<GraphExecutionResult> ExecuteWithCheckpointsAsync(
        Kernel kernel,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var workflowId = Guid.NewGuid().ToString();

        // Configurar callback para checkpoint após cada nó
        _executor.OnNodeCompleted += async (nodeId, state) =>
        {
            var checkpoint = new WorkflowCheckpoint
            {
                WorkflowId = workflowId,
                CurrentNodeId = nodeId,
                State = state.ToDictionary(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _checkpointManager.SaveAsync(workflowId, checkpoint, cancellationToken);
            _logger.LogInformation("Checkpoint salvo após nó {NodeId}", nodeId);
        };

        var result = await _executor.ExecuteAsync(kernel, arguments, cancellationToken);

        // Limpar checkpoint após conclusão bem-sucedida
        await _checkpointManager.DeleteAsync(workflowId, cancellationToken);

        return result;
    }

    public async Task<GraphExecutionResult?> ResumeAsync(
        string workflowId,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = await _checkpointManager.LoadAsync(workflowId, cancellationToken);
        if (checkpoint == null)
        {
            _logger.LogWarning("Checkpoint não encontrado para workflow {WorkflowId}", workflowId);
            return null;
        }

        _logger.LogInformation(
            "Resumindo workflow {WorkflowId} do nó {NodeId}",
            workflowId, checkpoint.CurrentNodeId);

        var arguments = new KernelArguments();
        foreach (var kvp in checkpoint.State)
            arguments[kvp.Key] = kvp.Value;

        // Configurar executor para começar do nó salvo
        _executor.SetStartNode(checkpoint.CurrentNodeId);

        return await ExecuteWithCheckpointsAsync(kernel, arguments, cancellationToken);
    }
}
```

---

## Boas Práticas

1. **Frequência**: Balance entre segurança e performance
2. **Compressão**: Comprima estados grandes
3. **Limpeza**: Remova checkpoints antigos
4. **Storage**: Use storage durável para produção

---

## Templates Relacionados

- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos
- [Human-in-the-Loop](template-skg-human-in-loop.md) - Aprovação humana

