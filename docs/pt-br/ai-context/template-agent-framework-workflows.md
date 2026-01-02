# Template Graph-based Workflows - Microsoft Agent Framework

> **Propósito**: Este template fornece padrões para workflows baseados em grafos usando Microsoft.Extensions.AI.

---

## Visão Geral

Workflows baseados em grafos permitem:
- Padrões de execução de grafos dirigidos
- Ramificação e roteamento condicional
- Gerenciamento de estado
- Suporte a streaming e checkpointing

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Pipelines de processamento multi-etapas | ✅ Recomendado |
| Roteamento condicional | ✅ Recomendado |
| Processos de longa duração | ✅ Recomendado |
| Q&A simples | ⚠️ Use Agente Básico |

---

## Implementação

### Abstração de Nó de Workflow

```csharp
public interface IWorkflowNode
{
    string NodeId { get; }
    string Name { get; }
    Task<WorkflowResult> ExecuteAsync(WorkflowContext context, CancellationToken cancellationToken = default);
}

public class WorkflowResult
{
    public bool Success { get; set; }
    public string? NextNodeId { get; set; }
    public object? Output { get; set; }
    public string? ErrorMessage { get; set; }

    public static WorkflowResult Continue(string nextNodeId, object? output = null)
        => new() { Success = true, NextNodeId = nextNodeId, Output = output };

    public static WorkflowResult Complete(object? output = null)
        => new() { Success = true, NextNodeId = null, Output = output };

    public static WorkflowResult Fail(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

public class WorkflowContext
{
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
    public string CurrentNodeId { get; set; } = string.Empty;
    public Dictionary<string, object> State { get; set; } = new();
    public List<string> ExecutionHistory { get; set; } = new();

    public T GetState<T>(string key, T defaultValue = default!)
    {
        return State.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue : defaultValue;
    }

    public void SetState(string key, object value) => State[key] = value;
}
```

### Executor de Workflow

```csharp
public class WorkflowExecutor
{
    private readonly Dictionary<string, IWorkflowNode> _nodes = new();
    private readonly ILogger _logger;
    private string _startNodeId = string.Empty;

    public WorkflowExecutor(string workflowId, string name, ILogger<WorkflowExecutor> logger)
    {
        WorkflowId = workflowId;
        Name = name;
        _logger = logger;
    }

    public string WorkflowId { get; }
    public string Name { get; }

    public WorkflowExecutor AddNode(IWorkflowNode node)
    {
        _nodes[node.NodeId] = node;
        return this;
    }

    public WorkflowExecutor SetStartNode(string nodeId)
    {
        _startNodeId = nodeId;
        return this;
    }

    public async Task<WorkflowContext> ExecuteAsync(
        WorkflowContext? initialContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = initialContext ?? new WorkflowContext();
        context.CurrentNodeId = _startNodeId;

        while (!string.IsNullOrEmpty(context.CurrentNodeId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_nodes.TryGetValue(context.CurrentNodeId, out var node))
            {
                _logger.LogError("Nó {NodeId} não encontrado", context.CurrentNodeId);
                break;
            }

            _logger.LogInformation("Executando nó {NodeId}", node.NodeId);
            context.ExecutionHistory.Add(node.NodeId);

            var result = await node.ExecuteAsync(context, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError("Nó {NodeId} falhou: {Error}", node.NodeId, result.ErrorMessage);
                context.SetState("error", result.ErrorMessage ?? "Erro desconhecido");
                break;
            }

            context.CurrentNodeId = result.NextNodeId ?? string.Empty;
        }

        return context;
    }
}
```

### Nó de Processamento IA

```csharp
public class AIProcessingNode : IWorkflowNode
{
    private readonly IChatClient _chatClient;
    private readonly string _systemPrompt;
    private readonly string _nextNodeId;

    public string NodeId { get; }
    public string Name { get; }

    public AIProcessingNode(
        string nodeId, string name, IChatClient chatClient,
        string systemPrompt, string nextNodeId)
    {
        NodeId = nodeId;
        Name = name;
        _chatClient = chatClient;
        _systemPrompt = systemPrompt;
        _nextNodeId = nextNodeId;
    }

    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var input = context.GetState<string>("input", string.Empty);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _systemPrompt),
            new(ChatRole.User, input)
        };

        var response = await _chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);
        var output = response.Message.Text ?? string.Empty;
        context.SetState($"{NodeId}_output", output);

        return WorkflowResult.Continue(_nextNodeId, output);
    }
}
```

### Nó de Ramificação Condicional

```csharp
public class ConditionalBranchNode : IWorkflowNode
{
    private readonly Func<WorkflowContext, string> _conditionEvaluator;
    public Dictionary<string, string> Branches { get; } = new();

    public string NodeId { get; }
    public string Name { get; }

    public ConditionalBranchNode(
        string nodeId, string name, Func<WorkflowContext, string> conditionEvaluator)
    {
        NodeId = nodeId;
        Name = name;
        _conditionEvaluator = conditionEvaluator;
    }

    public ConditionalBranchNode AddBranch(string condition, string targetNodeId)
    {
        Branches[condition] = targetNodeId;
        return this;
    }

    public Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var conditionResult = _conditionEvaluator(context);

        if (Branches.TryGetValue(conditionResult, out var nextNodeId))
            return Task.FromResult(WorkflowResult.Continue(nextNodeId));

        if (Branches.TryGetValue("default", out var defaultNodeId))
            return Task.FromResult(WorkflowResult.Continue(defaultNodeId));

        return Task.FromResult(WorkflowResult.Fail($"Sem branch para condição: {conditionResult}"));
    }
}
```

---

## Integração com Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    [HttpPost("{workflowType}")]
    public async Task<IActionResult> StartWorkflow(
        string workflowType, [FromBody] WorkflowStartRequest request, CancellationToken cancellationToken)
    {
        var executor = CreateWorkflowExecutor(workflowType);
        var context = new WorkflowContext();
        foreach (var kvp in request.InitialState ?? new())
            context.SetState(kvp.Key, kvp.Value);

        var result = await executor.ExecuteAsync(context, cancellationToken);

        return Ok(new
        {
            workflowId = result.WorkflowId,
            status = "Completed",
            executionHistory = result.ExecutionHistory,
            output = result.State
        });
    }
}
```

---

## Boas Práticas

1. **Responsabilidade Única**: Cada nó deve fazer uma coisa bem
2. **Idempotência**: Nós devem ser seguros para re-executar
3. **Checkpointing**: Salve estado em limites significativos
4. **Timeout**: Defina timeouts apropriados para cada nó

---

## Templates Relacionados

- [Agent Framework Básico](template-agent-framework-basic.md) - Configuração de agente
- [Multi-Agent](template-agent-framework-multi-agent.md) - Coordenação de agentes

