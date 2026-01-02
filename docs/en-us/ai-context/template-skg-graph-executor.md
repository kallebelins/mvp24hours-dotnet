# Graph Executor Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns and implementation guidelines for creating and executing graph-based workflows using Semantic Kernel Graph.

---

## Overview

Graph Executor is the core orchestration engine for workflow-based AI applications. This template covers:
- Graph structure and node types
- Sequential and parallel execution
- Conditional routing
- State management
- Result handling

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Multi-step workflows | ✅ Recommended |
| Conditional branching | ✅ Recommended |
| Parallel processing | ✅ Recommended |
| State management across steps | ✅ Recommended |
| Simple Q&A | ⚠️ Use Chat Completion |
| Single-step operations | ⚠️ Use Plugins directly |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
</ItemGroup>
```

---

## Core Concepts

### Graph Components

| Component | Description |
|-----------|-------------|
| **GraphExecutor** | Main execution engine that orchestrates workflow |
| **FunctionGraphNode** | Executes KernelFunction instances |
| **ConditionalEdge** | Routes execution based on conditions |
| **GraphState** | Manages state across node execution |
| **KernelArguments** | Carries data between nodes |

---

## Implementation Patterns

### 1. Basic Graph Setup

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Extensions;
using SemanticKernel.Graph.Nodes;

public class BasicGraphExample
{
    public static async Task<string> RunBasicGraphAsync()
    {
        // 1. Create kernel with graph support
        var builder = Kernel.CreateBuilder();
        builder.AddGraphSupport();
        builder.AddOpenAIChatCompletion("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
        var kernel = builder.Build();

        // 2. Create graph executor
        var graph = new GraphExecutor("BasicWorkflow", "A simple workflow example");

        // 3. Create nodes
        var greetingNode = CreateGreetingNode(kernel);
        var processingNode = CreateProcessingNode(kernel);
        var completionNode = CreateCompletionNode(kernel);

        // 4. Add nodes to graph
        graph.AddNode(greetingNode);
        graph.AddNode(processingNode);
        graph.AddNode(completionNode);

        // 5. Connect nodes
        graph.Connect(greetingNode.NodeId, processingNode.NodeId);
        graph.Connect(processingNode.NodeId, completionNode.NodeId);

        // 6. Set start node
        graph.SetStartNode(greetingNode.NodeId);

        // 7. Execute graph
        var arguments = new KernelArguments { ["user_name"] = "Developer" };
        var result = await graph.ExecuteAsync(kernel, arguments);

        // 8. Get results from state
        var graphState = arguments.GetOrCreateGraphState();
        return graphState.GetValue<string>("final_result") ?? "No result";
    }

    private static FunctionGraphNode CreateGreetingNode(Kernel kernel)
    {
        var function = kernel.CreateFunctionFromMethod(
            (string user_name) => $"Hello {user_name}! Starting workflow.",
            functionName: "Greeting",
            description: "Generates a greeting");

        return new FunctionGraphNode(function, "greeting-node")
            .StoreResultAs("greeting");
    }

    private static FunctionGraphNode CreateProcessingNode(Kernel kernel)
    {
        var function = kernel.CreateFunctionFromMethod(
            (KernelArguments args) =>
            {
                var greeting = args.GetOrCreateGraphState().GetValue<string>("greeting");
                return $"Processed: {greeting}";
            },
            functionName: "Processing",
            description: "Processes the greeting");

        return new FunctionGraphNode(function, "processing-node")
            .StoreResultAs("processed");
    }

    private static FunctionGraphNode CreateCompletionNode(Kernel kernel)
    {
        var function = kernel.CreateFunctionFromMethod(
            (KernelArguments args) =>
            {
                var processed = args.GetOrCreateGraphState().GetValue<string>("processed");
                return $"Workflow complete. Result: {processed}";
            },
            functionName: "Completion",
            description: "Completes the workflow");

        return new FunctionGraphNode(function, "completion-node")
            .StoreResultAs("final_result");
    }
}
```

### 2. Conditional Routing

```csharp
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;

public class ConditionalGraphExample
{
    public static GraphExecutor CreateConditionalGraph(Kernel kernel)
    {
        var graph = new GraphExecutor("ConditionalWorkflow", "Workflow with conditional routing");

        // Input validation node
        var validationNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var input = args.TryGetValue("input", out var val) ? val?.ToString() : "";
                    var isValid = !string.IsNullOrWhiteSpace(input) && input.Length >= 3;
                    args["is_valid"] = isValid;
                    return isValid ? "Valid input" : "Invalid input";
                },
                functionName: "ValidateInput",
                description: "Validates user input"),
            "validation-node")
            .StoreResultAs("validation_result");

        // Success path node
        var successNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var input = args["input"]?.ToString();
                    return $"Successfully processed: {input}";
                },
                functionName: "ProcessSuccess",
                description: "Processes valid input"),
            "success-node")
            .StoreResultAs("result");

        // Error path node
        var errorNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                () => "Error: Invalid input provided. Please provide valid input.",
                functionName: "HandleError",
                description: "Handles invalid input"),
            "error-node")
            .StoreResultAs("result");

        // Add nodes
        graph.AddNode(validationNode);
        graph.AddNode(successNode);
        graph.AddNode(errorNode);

        // Set start node
        graph.SetStartNode(validationNode.NodeId);

        // Add conditional edges
        graph.AddEdge(ConditionalEdge.Create(
            validationNode,
            successNode,
            context => context.TryGetValue("is_valid", out var val) && val is true));

        graph.AddEdge(ConditionalEdge.Create(
            validationNode,
            errorNode,
            context => !context.TryGetValue("is_valid", out var val) || val is not true));

        return graph;
    }
}
```

### 3. Parallel Execution

```csharp
public class ParallelGraphExample
{
    public static GraphExecutor CreateParallelGraph(Kernel kernel)
    {
        var graph = new GraphExecutor("ParallelWorkflow", "Workflow with parallel execution");

        // Start node - distributes work
        var distributeNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var input = args["input"]?.ToString() ?? "";
                    args["task1_input"] = input;
                    args["task2_input"] = input;
                    args["task3_input"] = input;
                    return "Work distributed";
                },
                functionName: "Distribute",
                description: "Distributes work to parallel tasks"),
            "distribute-node");

        // Parallel task nodes
        var task1Node = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    await Task.Delay(100); // Simulate work
                    var input = args["task1_input"]?.ToString();
                    return $"Task1 result: Analyzed '{input}'";
                },
                functionName: "Task1",
                description: "First parallel task"),
            "task1-node")
            .StoreResultAs("task1_result");

        var task2Node = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    await Task.Delay(150); // Simulate work
                    var input = args["task2_input"]?.ToString();
                    return $"Task2 result: Processed '{input}'";
                },
                functionName: "Task2",
                description: "Second parallel task"),
            "task2-node")
            .StoreResultAs("task2_result");

        var task3Node = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    await Task.Delay(120); // Simulate work
                    var input = args["task3_input"]?.ToString();
                    return $"Task3 result: Evaluated '{input}'";
                },
                functionName: "Task3",
                description: "Third parallel task"),
            "task3-node")
            .StoreResultAs("task3_result");

        // Aggregation node - combines results
        var aggregateNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var state = args.GetOrCreateGraphState();
                    var result1 = state.GetValue<string>("task1_result");
                    var result2 = state.GetValue<string>("task2_result");
                    var result3 = state.GetValue<string>("task3_result");
                    
                    return $"""
                        Aggregated Results:
                        - {result1}
                        - {result2}
                        - {result3}
                        """;
                },
                functionName: "Aggregate",
                description: "Aggregates parallel results"),
            "aggregate-node")
            .StoreResultAs("final_result");

        // Add nodes
        graph.AddNode(distributeNode);
        graph.AddNode(task1Node);
        graph.AddNode(task2Node);
        graph.AddNode(task3Node);
        graph.AddNode(aggregateNode);

        // Set start node
        graph.SetStartNode(distributeNode.NodeId);

        // Connect distribution to parallel tasks
        graph.Connect(distributeNode.NodeId, task1Node.NodeId);
        graph.Connect(distributeNode.NodeId, task2Node.NodeId);
        graph.Connect(distributeNode.NodeId, task3Node.NodeId);

        // Connect parallel tasks to aggregation
        graph.Connect(task1Node.NodeId, aggregateNode.NodeId);
        graph.Connect(task2Node.NodeId, aggregateNode.NodeId);
        graph.Connect(task3Node.NodeId, aggregateNode.NodeId);

        return graph;
    }
}
```

### 4. State Management

```csharp
public class StateManagementExample
{
    public static async Task DemonstrateStateManagement(Kernel kernel)
    {
        var graph = new GraphExecutor("StateWorkflow", "Demonstrates state management");

        // Node that writes to state
        var writerNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var state = args.GetOrCreateGraphState();
                    state.SetValue("counter", 0);
                    state.SetValue("items", new List<string>());
                    state.SetValue("metadata", new Dictionary<string, object>
                    {
                        ["created_at"] = DateTime.UtcNow,
                        ["version"] = "1.0"
                    });
                    return "State initialized";
                },
                functionName: "InitState",
                description: "Initializes state"),
            "init-node");

        // Node that modifies state
        var modifierNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var state = args.GetOrCreateGraphState();
                    
                    // Increment counter
                    var counter = state.GetValue<int>("counter");
                    state.SetValue("counter", counter + 1);
                    
                    // Add item to list
                    var items = state.GetValue<List<string>>("items") ?? new List<string>();
                    items.Add($"Item {counter + 1}");
                    state.SetValue("items", items);
                    
                    return $"State modified. Counter: {counter + 1}";
                },
                functionName: "ModifyState",
                description: "Modifies state"),
            "modify-node");

        // Node that reads state
        var readerNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var state = args.GetOrCreateGraphState();
                    var counter = state.GetValue<int>("counter");
                    var items = state.GetValue<List<string>>("items");
                    var metadata = state.GetValue<Dictionary<string, object>>("metadata");
                    
                    return $"""
                        Final State:
                        - Counter: {counter}
                        - Items: {string.Join(", ", items ?? new List<string>())}
                        - Created: {metadata?["created_at"]}
                        """;
                },
                functionName: "ReadState",
                description: "Reads final state"),
            "read-node")
            .StoreResultAs("summary");

        graph.AddNode(writerNode);
        graph.AddNode(modifierNode);
        graph.AddNode(readerNode);

        graph.SetStartNode(writerNode.NodeId);
        graph.Connect(writerNode.NodeId, modifierNode.NodeId);
        graph.Connect(modifierNode.NodeId, readerNode.NodeId);

        var args = new KernelArguments();
        await graph.ExecuteAsync(kernel, args);

        var summary = args.GetOrCreateGraphState().GetValue<string>("summary");
        Console.WriteLine(summary);
    }
}
```

### 5. AI-Powered Nodes

```csharp
public class AIGraphExample
{
    public static GraphExecutor CreateAIGraph(Kernel kernel)
    {
        var graph = new GraphExecutor("AIWorkflow", "Workflow with AI-powered nodes");

        // Analysis node using AI
        var analysisNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Analyze the following text and identify:
                1. Main topic
                2. Key points (up to 3)
                3. Sentiment (positive/negative/neutral)

                Text: {{$input}}

                Analysis:
                """,
                functionName: "AnalyzeText",
                description: "Analyzes text using AI"),
            "analysis-node")
            .StoreResultAs("analysis");

        // Summary node using AI
        var summaryNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Based on the following analysis, create a brief summary:

                Analysis: {{$analysis}}

                Summary (2-3 sentences):
                """,
                functionName: "Summarize",
                description: "Creates summary from analysis"),
            "summary-node")
            .StoreResultAs("summary");

        // Recommendation node using AI
        var recommendNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Based on this analysis and summary, provide one actionable recommendation:

                Analysis: {{$analysis}}
                Summary: {{$summary}}

                Recommendation:
                """,
                functionName: "Recommend",
                description: "Provides recommendation"),
            "recommend-node")
            .StoreResultAs("recommendation");

        graph.AddNode(analysisNode);
        graph.AddNode(summaryNode);
        graph.AddNode(recommendNode);

        graph.SetStartNode(analysisNode.NodeId);
        graph.Connect(analysisNode.NodeId, summaryNode.NodeId);
        graph.Connect(summaryNode.NodeId, recommendNode.NodeId);

        return graph;
    }
}
```

---

## Dependency Injection Setup

```csharp
public static class GraphServiceExtensions
{
    public static IServiceCollection AddGraphServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Kernel with graph support
        services.AddSingleton(sp =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddGraphSupport();

            var aiConfig = configuration.GetSection("AI");
            builder.AddOpenAIChatCompletion(
                modelId: aiConfig["ModelId"] ?? "gpt-4o",
                apiKey: aiConfig["ApiKey"]!);

            return builder.Build();
        });

        // Register graph factory
        services.AddSingleton<IGraphFactory, GraphFactory>();

        // Register workflow services
        services.AddScoped<IWorkflowService, WorkflowService>();

        return services;
    }
}

public interface IGraphFactory
{
    GraphExecutor CreateGraph(string name, string description);
}

public class GraphFactory : IGraphFactory
{
    public GraphExecutor CreateGraph(string name, string description)
    {
        return new GraphExecutor(name, description);
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly IGraphFactory _graphFactory;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        Kernel kernel,
        IGraphFactory graphFactory,
        ILogger<WorkflowController> logger)
    {
        _kernel = kernel;
        _graphFactory = graphFactory;
        _logger = logger;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteWorkflow(
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var graph = BuildWorkflow(request.WorkflowType);
        
        var arguments = new KernelArguments();
        foreach (var param in request.Parameters)
        {
            arguments[param.Key] = param.Value;
        }

        await graph.ExecuteAsync(_kernel, arguments, cancellationToken);

        var state = arguments.GetOrCreateGraphState();
        var result = state.GetValue<string>("result") ?? "No result";

        return Ok(new { result });
    }

    private GraphExecutor BuildWorkflow(string workflowType)
    {
        return workflowType switch
        {
            "analysis" => AIGraphExample.CreateAIGraph(_kernel),
            "conditional" => ConditionalGraphExample.CreateConditionalGraph(_kernel),
            "parallel" => ParallelGraphExample.CreateParallelGraph(_kernel),
            _ => throw new ArgumentException($"Unknown workflow type: {workflowType}")
        };
    }
}

public record WorkflowRequest(
    string WorkflowType,
    Dictionary<string, object> Parameters);
```

---

## Error Handling

```csharp
public class ResilientGraphExecutor
{
    private readonly Kernel _kernel;
    private readonly ILogger<ResilientGraphExecutor> _logger;

    public async Task<GraphResult> ExecuteWithRetryAsync(
        GraphExecutor graph,
        KernelArguments arguments,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var result = new GraphResult();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await graph.ExecuteAsync(_kernel, arguments, cancellationToken);
                result.Success = true;
                result.State = arguments.GetOrCreateGraphState();
                return result;
            }
            catch (Exception ex) when (attempt < maxRetries - 1)
            {
                _logger.LogWarning(ex, "Graph execution failed, attempt {Attempt}/{MaxRetries}", 
                    attempt + 1, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Graph execution failed after {MaxRetries} attempts", maxRetries);
                result.Success = false;
                result.Error = ex.Message;
            }
        }

        return result;
    }
}

public class GraphResult
{
    public bool Success { get; set; }
    public GraphState? State { get; set; }
    public string? Error { get; set; }
}
```

---

## Testing

```csharp
using Xunit;

public class GraphExecutorTests
{
    [Fact]
    public async Task BasicGraph_ExecutesAllNodes()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var graph = new GraphExecutor("TestGraph", "Test");

        var node1 = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(() => "Step 1", "Step1", ""),
            "node1").StoreResultAs("step1");

        var node2 = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) => 
                {
                    var prev = args.GetOrCreateGraphState().GetValue<string>("step1");
                    return $"{prev} -> Step 2";
                }, "Step2", ""),
            "node2").StoreResultAs("step2");

        graph.AddNode(node1);
        graph.AddNode(node2);
        graph.SetStartNode(node1.NodeId);
        graph.Connect(node1.NodeId, node2.NodeId);

        // Act
        var args = new KernelArguments();
        await graph.ExecuteAsync(kernel, args);

        // Assert
        var state = args.GetOrCreateGraphState();
        Assert.Equal("Step 1", state.GetValue<string>("step1"));
        Assert.Equal("Step 1 -> Step 2", state.GetValue<string>("step2"));
    }

    [Fact]
    public async Task ConditionalGraph_RoutesCorrectly()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var graph = ConditionalGraphExample.CreateConditionalGraph(kernel);

        // Act - Valid input
        var validArgs = new KernelArguments { ["input"] = "valid input" };
        await graph.ExecuteAsync(kernel, validArgs);
        var validResult = validArgs.GetOrCreateGraphState().GetValue<string>("result");

        // Act - Invalid input
        var invalidArgs = new KernelArguments { ["input"] = "" };
        await graph.ExecuteAsync(kernel, invalidArgs);
        var invalidResult = invalidArgs.GetOrCreateGraphState().GetValue<string>("result");

        // Assert
        Assert.Contains("Successfully processed", validResult);
        Assert.Contains("Error", invalidResult);
    }

    private Kernel CreateTestKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddGraphSupport();
        return builder.Build();
    }
}
```

---

## Best Practices

### Graph Design

1. **Single Responsibility**: Each node should do one thing well
2. **Clear Naming**: Use descriptive node IDs and function names
3. **State Isolation**: Use `StoreResultAs` to explicitly manage state
4. **Error Boundaries**: Add error handling nodes for recovery

### Performance

1. **Parallel When Possible**: Use parallel execution for independent tasks
2. **Minimize State**: Only store necessary data in state
3. **Lazy Loading**: Load heavy resources only when needed

### Debugging

```csharp
// Enable detailed logging
var graph = new GraphExecutor("DebugGraph", "Debug example", logger);
graph.EnableDetailedLogging = true;

// Log node execution
graph.OnNodeExecuted += (sender, e) =>
{
    Console.WriteLine($"Node {e.NodeId} completed in {e.Duration.TotalMilliseconds}ms");
};
```

---

## Related Templates

- [ReAct Agent](template-skg-react-agent.md) - Reasoning with tools
- [Chain of Thought](template-skg-chain-of-thought.md) - Step-by-step reasoning
- [Chatbot with Memory](template-skg-chatbot-memory.md) - Contextual conversations
- [Multi-Agent](template-skg-multi-agent.md) - Coordinated agents

---

## External References

- [Semantic Kernel Graph Documentation](https://github.com/kallebelins/semantic-kernel-graph)
- [Graph Concepts](https://learn.microsoft.com/semantic-kernel)
- [Workflow Patterns](https://www.enterpriseintegrationpatterns.com/)

