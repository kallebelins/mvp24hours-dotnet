# Graph-based Workflows Template - Microsoft Agent Framework

> **Purpose**: This template provides AI agents with patterns for implementing workflow-based AI agents using graph structures with Microsoft.Extensions.AI.

---

## Overview

Graph-based workflows enable complex AI agent behaviors through:
- Directed graph execution patterns
- Conditional branching and routing
- State management across workflow steps
- Streaming and checkpointing support
- Human-in-the-loop integration

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Multi-step processing pipelines | ✅ Recommended |
| Conditional workflow routing | ✅ Recommended |
| Long-running agent processes | ✅ Recommended |
| Workflows requiring checkpoints | ✅ Recommended |
| Simple Q&A conversations | ⚠️ Use Basic Agent |
| Complex graph orchestration | ⚠️ Consider SK Graph |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.*-*" />
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.*-*" />
  <PackageReference Include="System.Threading.Channels" Version="8.*" />
</ItemGroup>
```

---

## Core Concepts

### Workflow Graph Structure

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Start     │────▶│  Process    │────▶│   Review    │
│   Node      │     │   Node      │     │   Node      │
└─────────────┘     └─────────────┘     └─────────────┘
                           │                   │
                           │ (condition)       │ (condition)
                           ▼                   ▼
                    ┌─────────────┐     ┌─────────────┐
                    │  Fallback   │     │  Approval   │
                    │   Node      │     │   Node      │
                    └─────────────┘     └─────────────┘
```

---

## Implementation Patterns

### 1. Workflow Node Abstraction

```csharp
using Microsoft.Extensions.AI;

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
    public Dictionary<string, object> Metadata { get; set; } = new();

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
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndTime { get; set; }

    public T GetState<T>(string key, T defaultValue = default!)
    {
        return State.TryGetValue(key, out var value) && value is T typedValue
            ? typedValue
            : defaultValue;
    }

    public void SetState(string key, object value)
    {
        State[key] = value;
    }
}
```

### 2. AI Processing Node

```csharp
public class AIProcessingNode : IWorkflowNode
{
    private readonly IChatClient _chatClient;
    private readonly string _systemPrompt;
    private readonly string _nextNodeId;

    public string NodeId { get; }
    public string Name { get; }

    public AIProcessingNode(
        string nodeId,
        string name,
        IChatClient chatClient,
        string systemPrompt,
        string nextNodeId)
    {
        NodeId = nodeId;
        Name = name;
        _chatClient = chatClient;
        _systemPrompt = systemPrompt;
        _nextNodeId = nextNodeId;
    }

    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default)
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

### 3. Conditional Branch Node

```csharp
public class ConditionalBranchNode : IWorkflowNode
{
    private readonly Func<WorkflowContext, string> _conditionEvaluator;

    public string NodeId { get; }
    public string Name { get; }
    public Dictionary<string, string> Branches { get; } = new();

    public ConditionalBranchNode(
        string nodeId,
        string name,
        Func<WorkflowContext, string> conditionEvaluator)
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
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var conditionResult = _conditionEvaluator(context);

        if (Branches.TryGetValue(conditionResult, out var nextNodeId))
        {
            return Task.FromResult(WorkflowResult.Continue(nextNodeId));
        }

        // Default branch
        if (Branches.TryGetValue("default", out var defaultNodeId))
        {
            return Task.FromResult(WorkflowResult.Continue(defaultNodeId));
        }

        return Task.FromResult(WorkflowResult.Fail($"No branch found for condition: {conditionResult}"));
    }
}
```

### 4. Parallel Execution Node

```csharp
public class ParallelExecutionNode : IWorkflowNode
{
    private readonly List<IWorkflowNode> _parallelNodes;
    private readonly string _nextNodeId;

    public string NodeId { get; }
    public string Name { get; }

    public ParallelExecutionNode(
        string nodeId,
        string name,
        IEnumerable<IWorkflowNode> parallelNodes,
        string nextNodeId)
    {
        NodeId = nodeId;
        Name = name;
        _parallelNodes = parallelNodes.ToList();
        _nextNodeId = nextNodeId;
    }

    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var tasks = _parallelNodes.Select(node =>
            ExecuteNodeAsync(node, context, cancellationToken));

        var results = await Task.WhenAll(tasks);

        // Aggregate results
        var outputs = new Dictionary<string, object>();
        for (int i = 0; i < _parallelNodes.Count; i++)
        {
            if (results[i].Output != null)
            {
                outputs[_parallelNodes[i].NodeId] = results[i].Output!;
            }
        }

        context.SetState($"{NodeId}_results", outputs);

        var allSucceeded = results.All(r => r.Success);
        if (!allSucceeded)
        {
            var errors = results
                .Where(r => !r.Success)
                .Select(r => r.ErrorMessage)
                .ToList();
            return WorkflowResult.Fail(string.Join("; ", errors));
        }

        return WorkflowResult.Continue(_nextNodeId, outputs);
    }

    private async Task<WorkflowResult> ExecuteNodeAsync(
        IWorkflowNode node,
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await node.ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            return WorkflowResult.Fail($"Node {node.NodeId} failed: {ex.Message}");
        }
    }
}
```

### 5. Workflow Executor

```csharp
public class WorkflowExecutor
{
    private readonly Dictionary<string, IWorkflowNode> _nodes = new();
    private readonly ILogger<WorkflowExecutor> _logger;
    private string _startNodeId = string.Empty;

    public string WorkflowId { get; }
    public string Name { get; }

    public WorkflowExecutor(string workflowId, string name, ILogger<WorkflowExecutor> logger)
    {
        WorkflowId = workflowId;
        Name = name;
        _logger = logger;
    }

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

        _logger.LogInformation(
            "Starting workflow {WorkflowId} at node {NodeId}",
            WorkflowId, _startNodeId);

        while (!string.IsNullOrEmpty(context.CurrentNodeId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_nodes.TryGetValue(context.CurrentNodeId, out var node))
            {
                _logger.LogError("Node {NodeId} not found in workflow", context.CurrentNodeId);
                break;
            }

            _logger.LogInformation(
                "Executing node {NodeId} ({NodeName})",
                node.NodeId, node.Name);

            context.ExecutionHistory.Add(node.NodeId);

            var result = await node.ExecuteAsync(context, cancellationToken);

            if (!result.Success)
            {
                _logger.LogError(
                    "Node {NodeId} failed: {Error}",
                    node.NodeId, result.ErrorMessage);
                context.SetState("error", result.ErrorMessage ?? "Unknown error");
                break;
            }

            context.CurrentNodeId = result.NextNodeId ?? string.Empty;
        }

        context.EndTime = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Workflow {WorkflowId} completed. Duration: {Duration}ms",
            WorkflowId,
            (context.EndTime - context.StartTime)?.TotalMilliseconds);

        return context;
    }
}
```

---

## Streaming Workflow Support

### Streaming Node

```csharp
public interface IStreamingWorkflowNode : IWorkflowNode
{
    IAsyncEnumerable<WorkflowStreamUpdate> ExecuteStreamingAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default);
}

public class WorkflowStreamUpdate
{
    public string NodeId { get; set; } = string.Empty;
    public UpdateType Type { get; set; }
    public object? Data { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public enum UpdateType
    {
        Started,
        Progress,
        Output,
        Completed,
        Error
    }
}

public class StreamingAINode : IStreamingWorkflowNode
{
    private readonly IChatClient _chatClient;
    private readonly string _systemPrompt;
    private readonly string _nextNodeId;

    public string NodeId { get; }
    public string Name { get; }

    public StreamingAINode(
        string nodeId,
        string name,
        IChatClient chatClient,
        string systemPrompt,
        string nextNodeId)
    {
        NodeId = nodeId;
        Name = name;
        _chatClient = chatClient;
        _systemPrompt = systemPrompt;
        _nextNodeId = nextNodeId;
    }

    public Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Non-streaming fallback
        return ExecuteInternalAsync(context, cancellationToken);
    }

    public async IAsyncEnumerable<WorkflowStreamUpdate> ExecuteStreamingAsync(
        WorkflowContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new WorkflowStreamUpdate
        {
            NodeId = NodeId,
            Type = WorkflowStreamUpdate.UpdateType.Started
        };

        var input = context.GetState<string>("input", string.Empty);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _systemPrompt),
            new(ChatRole.User, input)
        };

        var fullOutput = new StringBuilder();

        await foreach (var update in _chatClient.CompleteStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                fullOutput.Append(update.Text);

                yield return new WorkflowStreamUpdate
                {
                    NodeId = NodeId,
                    Type = WorkflowStreamUpdate.UpdateType.Output,
                    Data = update.Text
                };
            }
        }

        context.SetState($"{NodeId}_output", fullOutput.ToString());

        yield return new WorkflowStreamUpdate
        {
            NodeId = NodeId,
            Type = WorkflowStreamUpdate.UpdateType.Completed,
            Data = new { NextNodeId = _nextNodeId }
        };
    }

    private async Task<WorkflowResult> ExecuteInternalAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
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

### Streaming Workflow Executor

```csharp
public class StreamingWorkflowExecutor
{
    private readonly Dictionary<string, IWorkflowNode> _nodes = new();
    private readonly ILogger<StreamingWorkflowExecutor> _logger;
    private string _startNodeId = string.Empty;

    public string WorkflowId { get; }
    public string Name { get; }

    public StreamingWorkflowExecutor(
        string workflowId,
        string name,
        ILogger<StreamingWorkflowExecutor> logger)
    {
        WorkflowId = workflowId;
        Name = name;
        _logger = logger;
    }

    public StreamingWorkflowExecutor AddNode(IWorkflowNode node)
    {
        _nodes[node.NodeId] = node;
        return this;
    }

    public StreamingWorkflowExecutor SetStartNode(string nodeId)
    {
        _startNodeId = nodeId;
        return this;
    }

    public async IAsyncEnumerable<WorkflowStreamUpdate> ExecuteStreamingAsync(
        WorkflowContext? initialContext = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var context = initialContext ?? new WorkflowContext();
        context.CurrentNodeId = _startNodeId;

        while (!string.IsNullOrEmpty(context.CurrentNodeId))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_nodes.TryGetValue(context.CurrentNodeId, out var node))
            {
                yield return new WorkflowStreamUpdate
                {
                    NodeId = context.CurrentNodeId,
                    Type = WorkflowStreamUpdate.UpdateType.Error,
                    Data = $"Node {context.CurrentNodeId} not found"
                };
                break;
            }

            context.ExecutionHistory.Add(node.NodeId);

            if (node is IStreamingWorkflowNode streamingNode)
            {
                string? nextNodeId = null;

                await foreach (var update in streamingNode.ExecuteStreamingAsync(context, cancellationToken))
                {
                    yield return update;

                    if (update.Type == WorkflowStreamUpdate.UpdateType.Completed && update.Data is { } data)
                    {
                        var dataDict = data as dynamic;
                        nextNodeId = dataDict?.NextNodeId?.ToString();
                    }
                }

                context.CurrentNodeId = nextNodeId ?? string.Empty;
            }
            else
            {
                var result = await node.ExecuteAsync(context, cancellationToken);

                yield return new WorkflowStreamUpdate
                {
                    NodeId = node.NodeId,
                    Type = result.Success
                        ? WorkflowStreamUpdate.UpdateType.Completed
                        : WorkflowStreamUpdate.UpdateType.Error,
                    Data = result.Success ? result.Output : result.ErrorMessage
                };

                if (!result.Success)
                    break;

                context.CurrentNodeId = result.NextNodeId ?? string.Empty;
            }
        }

        context.EndTime = DateTimeOffset.UtcNow;
    }
}
```

---

## Checkpointing Support

### Checkpoint Manager

```csharp
public interface ICheckpointManager
{
    Task SaveCheckpointAsync(WorkflowContext context, CancellationToken cancellationToken = default);
    Task<WorkflowContext?> LoadCheckpointAsync(string workflowId, CancellationToken cancellationToken = default);
    Task DeleteCheckpointAsync(string workflowId, CancellationToken cancellationToken = default);
}

public class InMemoryCheckpointManager : ICheckpointManager
{
    private readonly ConcurrentDictionary<string, string> _checkpoints = new();
    private readonly ILogger<InMemoryCheckpointManager> _logger;

    public InMemoryCheckpointManager(ILogger<InMemoryCheckpointManager> logger)
    {
        _logger = logger;
    }

    public Task SaveCheckpointAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(context);
        _checkpoints[context.WorkflowId] = json;

        _logger.LogInformation(
            "Checkpoint saved for workflow {WorkflowId} at node {NodeId}",
            context.WorkflowId, context.CurrentNodeId);

        return Task.CompletedTask;
    }

    public Task<WorkflowContext?> LoadCheckpointAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        if (_checkpoints.TryGetValue(workflowId, out var json))
        {
            var context = JsonSerializer.Deserialize<WorkflowContext>(json);

            _logger.LogInformation(
                "Checkpoint loaded for workflow {WorkflowId}",
                workflowId);

            return Task.FromResult(context);
        }

        return Task.FromResult<WorkflowContext?>(null);
    }

    public Task DeleteCheckpointAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        _checkpoints.TryRemove(workflowId, out _);

        _logger.LogInformation(
            "Checkpoint deleted for workflow {WorkflowId}",
            workflowId);

        return Task.CompletedTask;
    }
}

public class FileCheckpointManager : ICheckpointManager
{
    private readonly string _basePath;
    private readonly ILogger<FileCheckpointManager> _logger;

    public FileCheckpointManager(string basePath, ILogger<FileCheckpointManager> logger)
    {
        _basePath = basePath;
        _logger = logger;
        Directory.CreateDirectory(_basePath);
    }

    public async Task SaveCheckpointAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetCheckpointPath(context.WorkflowId);
        var json = JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        _logger.LogInformation(
            "Checkpoint saved to {FilePath}",
            filePath);
    }

    public async Task<WorkflowContext?> LoadCheckpointAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetCheckpointPath(workflowId);

        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<WorkflowContext>(json);
    }

    public Task DeleteCheckpointAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        var filePath = GetCheckpointPath(workflowId);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    private string GetCheckpointPath(string workflowId)
        => Path.Combine(_basePath, $"{workflowId}.json");
}
```

### Checkpointed Workflow Executor

```csharp
public class CheckpointedWorkflowExecutor
{
    private readonly WorkflowExecutor _executor;
    private readonly ICheckpointManager _checkpointManager;
    private readonly ILogger<CheckpointedWorkflowExecutor> _logger;

    public CheckpointedWorkflowExecutor(
        WorkflowExecutor executor,
        ICheckpointManager checkpointManager,
        ILogger<CheckpointedWorkflowExecutor> logger)
    {
        _executor = executor;
        _checkpointManager = checkpointManager;
        _logger = logger;
    }

    public async Task<WorkflowContext> ExecuteWithCheckpointsAsync(
        WorkflowContext? initialContext = null,
        CancellationToken cancellationToken = default)
    {
        var context = initialContext ?? new WorkflowContext();

        // Try to resume from checkpoint
        var checkpoint = await _checkpointManager.LoadCheckpointAsync(
            context.WorkflowId,
            cancellationToken);

        if (checkpoint != null)
        {
            _logger.LogInformation(
                "Resuming workflow {WorkflowId} from checkpoint at node {NodeId}",
                context.WorkflowId,
                checkpoint.CurrentNodeId);

            context = checkpoint;
        }

        // Execute with periodic checkpointing
        // This is a simplified version - in production, checkpoint after each node
        var result = await _executor.ExecuteAsync(context, cancellationToken);

        // Save final state
        await _checkpointManager.SaveCheckpointAsync(result, cancellationToken);

        return result;
    }

    public async Task<WorkflowContext?> ResumeAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = await _checkpointManager.LoadCheckpointAsync(
            workflowId,
            cancellationToken);

        if (checkpoint == null)
        {
            _logger.LogWarning("No checkpoint found for workflow {WorkflowId}", workflowId);
            return null;
        }

        return await _executor.ExecuteAsync(checkpoint, cancellationToken);
    }
}
```

---

## Human-in-the-Loop Node

```csharp
public class HumanApprovalNode : IWorkflowNode
{
    private readonly Channel<ApprovalRequest> _requestChannel;
    private readonly Channel<ApprovalResponse> _responseChannel;
    private readonly TimeSpan _timeout;
    private readonly string _approveNodeId;
    private readonly string _rejectNodeId;

    public string NodeId { get; }
    public string Name { get; }

    public HumanApprovalNode(
        string nodeId,
        string name,
        string approveNodeId,
        string rejectNodeId,
        TimeSpan? timeout = null)
    {
        NodeId = nodeId;
        Name = name;
        _approveNodeId = approveNodeId;
        _rejectNodeId = rejectNodeId;
        _timeout = timeout ?? TimeSpan.FromMinutes(30);
        _requestChannel = Channel.CreateBounded<ApprovalRequest>(1);
        _responseChannel = Channel.CreateBounded<ApprovalResponse>(1);
    }

    public ChannelReader<ApprovalRequest> RequestReader => _requestChannel.Reader;
    public ChannelWriter<ApprovalResponse> ResponseWriter => _responseChannel.Writer;

    public async Task<WorkflowResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        var request = new ApprovalRequest
        {
            WorkflowId = context.WorkflowId,
            NodeId = NodeId,
            Data = context.GetState<object>("approval_data"),
            RequestTime = DateTimeOffset.UtcNow
        };

        await _requestChannel.Writer.WriteAsync(request, cancellationToken);

        using var timeoutCts = new CancellationTokenSource(_timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCts.Token);

        try
        {
            var response = await _responseChannel.Reader.ReadAsync(linkedCts.Token);

            context.SetState("approval_response", response);

            return response.Approved
                ? WorkflowResult.Continue(_approveNodeId, response)
                : WorkflowResult.Continue(_rejectNodeId, response);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return WorkflowResult.Fail("Approval request timed out");
        }
    }
}

public class ApprovalRequest
{
    public string WorkflowId { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public object? Data { get; set; }
    public DateTimeOffset RequestTime { get; set; }
}

public class ApprovalResponse
{
    public bool Approved { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Comments { get; set; }
    public DateTimeOffset ResponseTime { get; set; } = DateTimeOffset.UtcNow;
}
```

---

## Web API Integration

### Workflow Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICheckpointManager _checkpointManager;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        IServiceProvider serviceProvider,
        ICheckpointManager checkpointManager,
        ILogger<WorkflowController> logger)
    {
        _serviceProvider = serviceProvider;
        _checkpointManager = checkpointManager;
        _logger = logger;
    }

    [HttpPost("{workflowType}")]
    public async Task<IActionResult> StartWorkflow(
        string workflowType,
        [FromBody] WorkflowStartRequest request,
        CancellationToken cancellationToken)
    {
        var executor = CreateWorkflowExecutor(workflowType);
        
        var context = new WorkflowContext();
        foreach (var kvp in request.InitialState ?? new())
        {
            context.SetState(kvp.Key, kvp.Value);
        }

        var result = await executor.ExecuteAsync(context, cancellationToken);

        return Ok(new WorkflowResponse
        {
            WorkflowId = result.WorkflowId,
            Status = result.EndTime.HasValue ? "Completed" : "Running",
            ExecutionHistory = result.ExecutionHistory,
            Output = result.State
        });
    }

    [HttpPost("{workflowType}/stream")]
    public async Task StreamWorkflow(
        string workflowType,
        [FromBody] WorkflowStartRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");

        var executor = CreateStreamingWorkflowExecutor(workflowType);

        var context = new WorkflowContext();
        foreach (var kvp in request.InitialState ?? new())
        {
            context.SetState(kvp.Key, kvp.Value);
        }

        await foreach (var update in executor.ExecuteStreamingAsync(context, cancellationToken))
        {
            var json = JsonSerializer.Serialize(update);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }

    [HttpPost("{workflowId}/resume")]
    public async Task<IActionResult> ResumeWorkflow(
        string workflowId,
        CancellationToken cancellationToken)
    {
        var checkpoint = await _checkpointManager.LoadCheckpointAsync(workflowId, cancellationToken);

        if (checkpoint == null)
            return NotFound($"No checkpoint found for workflow {workflowId}");

        // Resume execution based on workflow type stored in checkpoint
        var workflowType = checkpoint.GetState<string>("workflow_type", "default");
        var executor = CreateWorkflowExecutor(workflowType);

        var result = await executor.ExecuteAsync(checkpoint, cancellationToken);

        return Ok(new WorkflowResponse
        {
            WorkflowId = result.WorkflowId,
            Status = "Completed",
            ExecutionHistory = result.ExecutionHistory,
            Output = result.State
        });
    }

    [HttpGet("{workflowId}/status")]
    public async Task<IActionResult> GetWorkflowStatus(
        string workflowId,
        CancellationToken cancellationToken)
    {
        var checkpoint = await _checkpointManager.LoadCheckpointAsync(workflowId, cancellationToken);

        if (checkpoint == null)
            return NotFound();

        return Ok(new
        {
            WorkflowId = workflowId,
            CurrentNode = checkpoint.CurrentNodeId,
            ExecutionHistory = checkpoint.ExecutionHistory,
            StartTime = checkpoint.StartTime,
            EndTime = checkpoint.EndTime
        });
    }

    private WorkflowExecutor CreateWorkflowExecutor(string workflowType)
    {
        var chatClient = _serviceProvider.GetRequiredService<IChatClient>();
        var logger = _serviceProvider.GetRequiredService<ILogger<WorkflowExecutor>>();

        return workflowType switch
        {
            "document-processing" => CreateDocumentProcessingWorkflow(chatClient, logger),
            "review-approval" => CreateReviewApprovalWorkflow(chatClient, logger),
            _ => CreateDefaultWorkflow(chatClient, logger)
        };
    }

    private StreamingWorkflowExecutor CreateStreamingWorkflowExecutor(string workflowType)
    {
        var chatClient = _serviceProvider.GetRequiredService<IChatClient>();
        var logger = _serviceProvider.GetRequiredService<ILogger<StreamingWorkflowExecutor>>();

        var executor = new StreamingWorkflowExecutor(
            Guid.NewGuid().ToString(),
            workflowType,
            logger);

        // Add workflow nodes based on type
        // ...

        return executor;
    }

    private WorkflowExecutor CreateDefaultWorkflow(IChatClient chatClient, ILogger<WorkflowExecutor> logger)
    {
        var executor = new WorkflowExecutor(Guid.NewGuid().ToString(), "default", logger);

        executor
            .AddNode(new AIProcessingNode(
                "analyze",
                "Analyze Input",
                chatClient,
                "You are an analyzer. Analyze the given input and provide insights.",
                "summarize"))
            .AddNode(new AIProcessingNode(
                "summarize",
                "Summarize Results",
                chatClient,
                "You are a summarizer. Create a concise summary of the analysis.",
                ""))
            .SetStartNode("analyze");

        return executor;
    }

    private WorkflowExecutor CreateDocumentProcessingWorkflow(
        IChatClient chatClient,
        ILogger<WorkflowExecutor> logger)
    {
        // Implementation for document processing workflow
        return CreateDefaultWorkflow(chatClient, logger);
    }

    private WorkflowExecutor CreateReviewApprovalWorkflow(
        IChatClient chatClient,
        ILogger<WorkflowExecutor> logger)
    {
        // Implementation for review/approval workflow
        return CreateDefaultWorkflow(chatClient, logger);
    }
}

public record WorkflowStartRequest(Dictionary<string, object>? InitialState);

public record WorkflowResponse
{
    public string WorkflowId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> ExecutionHistory { get; init; } = new();
    public object? Output { get; init; }
}
```

---

## Complete Workflow Example

### Document Review Workflow

```csharp
public static class DocumentReviewWorkflowFactory
{
    public static WorkflowExecutor Create(
        IChatClient chatClient,
        ILogger<WorkflowExecutor> logger)
    {
        var executor = new WorkflowExecutor(
            Guid.NewGuid().ToString(),
            "Document Review",
            logger);

        // Extract content
        executor.AddNode(new AIProcessingNode(
            "extract",
            "Extract Key Information",
            chatClient,
            "Extract key information from the document: main topics, entities mentioned, and important dates.",
            "analyze"));

        // Analyze content
        executor.AddNode(new AIProcessingNode(
            "analyze",
            "Analyze Content",
            chatClient,
            "Analyze the extracted information for accuracy, completeness, and potential issues.",
            "classify"));

        // Classify document
        executor.AddNode(new ConditionalBranchNode(
            "classify",
            "Classify Document",
            context =>
            {
                var analysis = context.GetState<string>("analyze_output", "");
                if (analysis.Contains("critical", StringComparison.OrdinalIgnoreCase))
                    return "critical";
                if (analysis.Contains("review", StringComparison.OrdinalIgnoreCase))
                    return "review";
                return "standard";
            })
            .AddBranch("critical", "escalate")
            .AddBranch("review", "manual_review")
            .AddBranch("standard", "summarize"));

        // Standard path - summarize
        executor.AddNode(new AIProcessingNode(
            "summarize",
            "Summarize Document",
            chatClient,
            "Create a comprehensive summary of the document.",
            ""));

        // Review path - needs manual intervention
        executor.AddNode(new AIProcessingNode(
            "manual_review",
            "Prepare for Manual Review",
            chatClient,
            "Prepare review notes highlighting areas that need human attention.",
            ""));

        // Critical path - escalate
        executor.AddNode(new AIProcessingNode(
            "escalate",
            "Escalate Document",
            chatClient,
            "Document marked as critical. Prepare escalation report with urgent items.",
            ""));

        executor.SetStartNode("extract");

        return executor;
    }
}
```

---

## Best Practices

### Workflow Design

1. **Single Responsibility**: Each node should do one thing well
2. **Idempotency**: Nodes should be safe to re-execute
3. **Error Handling**: Always handle failures gracefully
4. **Checkpointing**: Save state at meaningful boundaries
5. **Timeout Handling**: Set appropriate timeouts for each node

### State Management

```csharp
// Good: Typed state access
var input = context.GetState<string>("input");
var results = context.GetState<List<string>>("results", new List<string>());

// Avoid: Untyped state access
var data = context.State["input"]; // May throw
```

### Testing Workflows

```csharp
[Fact]
public async Task Workflow_ExecutesAllNodes()
{
    // Arrange
    var mockChatClient = new Mock<IChatClient>();
    mockChatClient
        .Setup(x => x.CompleteAsync(
            It.IsAny<IList<ChatMessage>>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ChatCompletion(
            new ChatMessage(ChatRole.Assistant, "Test response")));

    var executor = DocumentReviewWorkflowFactory.Create(
        mockChatClient.Object,
        new Mock<ILogger<WorkflowExecutor>>().Object);

    var context = new WorkflowContext();
    context.SetState("input", "Test document content");

    // Act
    var result = await executor.ExecuteAsync(context);

    // Assert
    Assert.True(result.ExecutionHistory.Count > 0);
    Assert.NotNull(result.EndTime);
}
```

---

## Related Templates

- [Agent Framework Basic](template-agent-framework-basic.md) - Simple agent setup
- [Multi-Agent](template-agent-framework-multi-agent.md) - Agent coordination
- [SK Graph Executor](template-skg-graph-executor.md) - Alternative graph approach

---

## External References

- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions)
- [System.Threading.Channels](https://learn.microsoft.com/dotnet/core/extensions/channels)
- [Workflow Patterns](https://www.workflowpatterns.com/)

