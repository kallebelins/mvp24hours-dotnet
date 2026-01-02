# Multi-Agent Coordination Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for implementing multi-agent coordination using Semantic Kernel Graph.

---

## Overview

Multi-agent coordination enables complex workflows with multiple specialized agents working together. This template covers:
- Creating and managing specialized agents
- Work distribution strategies
- Capability-based task assignment
- Result aggregation
- Health monitoring

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Complex multi-step workflows | ✅ Recommended |
| Specialized agent collaboration | ✅ Recommended |
| Parallel task processing | ✅ Recommended |
| High-availability systems | ✅ Recommended |
| Simple single-agent tasks | ⚠️ Use Graph Executor |
| Sequential processing only | ⚠️ Use Chain of Thought |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.*" />
</ItemGroup>
```

---

## Multi-Agent Architecture

```
┌────────────────────────────────────────────────────────────┐
│                  MultiAgentCoordinator                      │
├────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │   Agent 1   │  │   Agent 2   │  │   Agent N   │         │
│  │ (Analysis)  │  │ (Process)   │  │ (Report)    │         │
│  ├─────────────┤  ├─────────────┤  ├─────────────┤         │
│  │ Capabilities│  │ Capabilities│  │ Capabilities│         │
│  │ - text      │  │ - data      │  │ - format    │         │
│  │ - pattern   │  │ - extraction│  │ - generation│         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
│         │                │                │                 │
│  ┌──────┴────────────────┴────────────────┴──────┐         │
│  │           Work Distribution Layer             │         │
│  │  (RoleBased | RoundRobin | LeastLoaded)       │         │
│  └──────────────────────────────────────────────┘         │
│                         │                                   │
│  ┌──────────────────────┴─────────────────────────┐        │
│  │            Result Aggregation                   │        │
│  │  (Merge | Consensus | Weighted | First)         │        │
│  └────────────────────────────────────────────────┘        │
└────────────────────────────────────────────────────────────┘
```

---

## Core Components

### MultiAgentOptions

```csharp
/// <summary>
/// Configuration options for multi-agent coordination behavior.
/// </summary>
public class MultiAgentOptions
{
    /// <summary>
    /// Maximum number of agents that may run concurrently.
    /// </summary>
    public int MaxConcurrentAgents { get; set; } = 5;

    /// <summary>
    /// Overall timeout for coordination operations.
    /// </summary>
    public TimeSpan CoordinationTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Configuration for shared state handling between agents.
    /// </summary>
    public SharedStateOptions SharedStateOptions { get; set; } = new();

    /// <summary>
    /// How work is distributed among agents.
    /// </summary>
    public WorkDistributionOptions WorkDistributionOptions { get; set; } = new();

    /// <summary>
    /// How results from multiple agents are aggregated.
    /// </summary>
    public ResultAggregationOptions ResultAggregationOptions { get; set; } = new();
}

public class SharedStateOptions
{
    public ConflictResolutionStrategy ConflictResolutionStrategy { get; set; } = ConflictResolutionStrategy.Merge;
    public bool AllowOverwrite { get; set; } = true;
}

public class WorkDistributionOptions
{
    public WorkDistributionStrategy DistributionStrategy { get; set; } = WorkDistributionStrategy.RoleBased;
    public bool EnablePrioritization { get; set; } = true;
}

public class ResultAggregationOptions
{
    public AggregationStrategy DefaultAggregationStrategy { get; set; } = AggregationStrategy.Merge;
    public double ConsensusThreshold { get; set; } = 0.6;
}
```

### Enumerations

```csharp
public enum ConflictResolutionStrategy
{
    Merge,
    LastWrite,
    FirstWrite,
    Custom
}

public enum WorkDistributionStrategy
{
    RoleBased,
    RoundRobin,
    LeastLoaded,
    Random,
    Capability
}

public enum AggregationStrategy
{
    Merge,
    Consensus,
    Weighted,
    First,
    Last,
    All
}
```

---

## Implementation Patterns

### 1. Creating MultiAgentCoordinator

```csharp
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.MultiAgent;

public class MultiAgentSetup
{
    public static MultiAgentCoordinator CreateCoordinator(ILoggerFactory loggerFactory)
    {
        var options = new MultiAgentOptions
        {
            MaxConcurrentAgents = 5,
            CoordinationTimeout = TimeSpan.FromMinutes(10),
            SharedStateOptions = new SharedStateOptions
            {
                ConflictResolutionStrategy = ConflictResolutionStrategy.Merge,
                AllowOverwrite = true
            },
            WorkDistributionOptions = new WorkDistributionOptions
            {
                DistributionStrategy = WorkDistributionStrategy.RoleBased,
                EnablePrioritization = true
            },
            ResultAggregationOptions = new ResultAggregationOptions
            {
                DefaultAggregationStrategy = AggregationStrategy.Consensus,
                ConsensusThreshold = 0.6
            }
        };

        var logger = new SemanticKernelGraphLogger(
            loggerFactory.CreateLogger<SemanticKernelGraphLogger>(),
            new GraphOptions());

        return new MultiAgentCoordinator(options, logger);
    }
}
```

### 2. Creating Specialized Agents

```csharp
public class AgentFactory
{
    private readonly Kernel _kernel;
    private readonly ILoggerFactory _loggerFactory;

    public AgentFactory(Kernel kernel, ILoggerFactory loggerFactory)
    {
        _kernel = kernel;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates an analysis agent specialized in text analysis tasks.
    /// </summary>
    public async Task<AgentInstance> CreateAnalysisAgentAsync(MultiAgentCoordinator coordinator)
    {
        var executor = new GraphExecutor(
            "Analysis Graph",
            "Specialized in text analysis",
            new SemanticKernelGraphLogger(
                _loggerFactory.CreateLogger<SemanticKernelGraphLogger>(), 
                new GraphOptions()));

        // Create analysis node
        var analysisNode = new FunctionGraphNode(
            CreateAnalysisFunction(),
            "analyze-text",
            "Text Analysis");
        analysisNode.StoreResultAs("analysis_result");
        analysisNode.SetMetadata("StrictValidation", false);

        executor.AddNode(analysisNode);
        executor.SetStartNode(analysisNode.NodeId);

        // Register with coordinator
        return await coordinator.RegisterAgentAsync(
            agentId: "analysis-agent",
            name: "Text Analysis Agent",
            description: "Specialized in comprehensive text analysis",
            executor: executor,
            capabilities: new[] { "text-analysis", "pattern-recognition", "insight-extraction" },
            metadata: new Dictionary<string, object>
            {
                ["specialization"] = "text_analysis",
                ["version"] = "1.0",
                ["performance_profile"] = "high_accuracy"
            });
    }

    /// <summary>
    /// Creates a processing agent specialized in data transformation.
    /// </summary>
    public async Task<AgentInstance> CreateProcessingAgentAsync(MultiAgentCoordinator coordinator)
    {
        var executor = new GraphExecutor(
            "Processing Graph",
            "Specialized in data processing",
            new SemanticKernelGraphLogger(
                _loggerFactory.CreateLogger<SemanticKernelGraphLogger>(), 
                new GraphOptions()));

        var processingNode = new FunctionGraphNode(
            CreateProcessingFunction(),
            "process-data",
            "Data Processing");
        processingNode.StoreResultAs("processed_result");

        executor.AddNode(processingNode);
        executor.SetStartNode(processingNode.NodeId);

        return await coordinator.RegisterAgentAsync(
            agentId: "processing-agent",
            name: "Data Processing Agent",
            description: "Specialized in data processing and enhancement",
            executor: executor,
            capabilities: new[] { "data-processing", "extraction", "transformation" },
            metadata: new Dictionary<string, object>
            {
                ["specialization"] = "data_processing",
                ["version"] = "1.0"
            });
    }

    /// <summary>
    /// Creates a reporting agent specialized in output generation.
    /// </summary>
    public async Task<AgentInstance> CreateReportingAgentAsync(MultiAgentCoordinator coordinator)
    {
        var executor = new GraphExecutor(
            "Reporting Graph",
            "Specialized in report generation",
            new SemanticKernelGraphLogger(
                _loggerFactory.CreateLogger<SemanticKernelGraphLogger>(), 
                new GraphOptions()));

        var reportingNode = new FunctionGraphNode(
            CreateReportingFunction(),
            "generate-report",
            "Report Generation");
        reportingNode.StoreResultAs("report");

        executor.AddNode(reportingNode);
        executor.SetStartNode(reportingNode.NodeId);

        return await coordinator.RegisterAgentAsync(
            agentId: "reporting-agent",
            name: "Report Generation Agent",
            description: "Specialized in generating comprehensive reports",
            executor: executor,
            capabilities: new[] { "report-generation", "formatting", "summarization" },
            metadata: new Dictionary<string, object>
            {
                ["specialization"] = "reporting",
                ["version"] = "1.0"
            });
    }

    private KernelFunction CreateAnalysisFunction()
    {
        return KernelFunctionFactory.CreateFromMethod(
            (KernelArguments args) =>
            {
                var input = args.TryGetValue("input_text", out var i) ? i?.ToString() ?? string.Empty : string.Empty;
                var analysisType = args.TryGetValue("analysis_type", out var a) ? a?.ToString() ?? "basic" : "basic";

                var analysisResult = new
                {
                    TextLength = input.Length,
                    WordCount = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                    AnalysisType = analysisType,
                    Insights = new[] { "Insight 1", "Insight 2", "Insight 3" },
                    Confidence = 0.95
                };

                args["analysis_result"] = analysisResult;
                return $"Analysis completed: {analysisResult.WordCount} words, {analysisResult.Insights.Length} insights";
            },
            functionName: "analyze_text",
            description: "Performs comprehensive text analysis");
    }

    private KernelFunction CreateProcessingFunction()
    {
        return KernelFunctionFactory.CreateFromMethod(
            (KernelArguments args) =>
            {
                var analysisResult = args.TryGetValue("analysis_result", out var ar) ? ar : null;

                var processedResult = new
                {
                    ProcessedAt = DateTime.UtcNow,
                    EnhancedInsights = new[] { "Enhanced insight 1", "Enhanced insight 2", "Enhanced insight 3" },
                    ProcessingQuality = "high",
                    Metadata = new { Source = "analysis_agent", Version = "1.0" }
                };

                args["processed_result"] = processedResult;
                return $"Processing completed: {processedResult.EnhancedInsights.Length} enhanced insights";
            },
            functionName: "process_analysis",
            description: "Processes analysis results and enhances insights");
    }

    private KernelFunction CreateReportingFunction()
    {
        return KernelFunctionFactory.CreateFromMethod(
            (KernelArguments args) =>
            {
                var processedResult = args.TryGetValue("processed_result", out var pr) ? pr : null;

                var report = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    Summary = "Analysis complete with enhanced insights",
                    Format = "executive_summary",
                    Status = "completed"
                };

                args["report"] = report;
                return $"Report generated: {report.Summary}";
            },
            functionName: "generate_report",
            description: "Generates comprehensive report from processed data");
    }
}
```

### 3. Simple Workflow Execution

```csharp
public class SimpleWorkflowExample
{
    public static async Task<WorkflowExecutionResult> ExecuteSimpleWorkflowAsync(
        MultiAgentCoordinator coordinator,
        Kernel kernel,
        ILoggerFactory loggerFactory)
    {
        // Create agents
        var factory = new AgentFactory(kernel, loggerFactory);
        var analysisAgent = await factory.CreateAnalysisAgentAsync(coordinator);
        var processingAgent = await factory.CreateProcessingAgentAsync(coordinator);
        var reportingAgent = await factory.CreateReportingAgentAsync(coordinator);

        // Prepare arguments
        var arguments = new KernelArguments
        {
            ["input_text"] = "The quick brown fox jumps over the lazy dog. This is sample text for analysis.",
            ["analysis_type"] = "comprehensive",
            ["output_format"] = "detailed_report"
        };

        // Execute workflow with automatic distribution
        return await coordinator.ExecuteSimpleWorkflowAsync(
            kernel,
            arguments,
            new[] { analysisAgent.AgentId, processingAgent.AgentId, reportingAgent.AgentId },
            AggregationStrategy.Merge);
    }
}
```

### 4. Advanced Workflow with Builder Pattern

```csharp
public class AdvancedWorkflowExample
{
    public static async Task<WorkflowExecutionResult> ExecuteAdvancedWorkflowAsync(
        MultiAgentCoordinator coordinator,
        Kernel kernel)
    {
        // Build workflow using fluent API
        var workflow = coordinator.CreateWorkflow("advanced-analysis", "Advanced Text Analysis Workflow")
            .WithDescription("Comprehensive text analysis using multiple specialized agents")
            .RequireAgents("analysis-agent", "processing-agent", "reporting-agent")
            
            // Task 1: Content Analysis
            .AddTask("analyze-content", "Content Analysis", task => task
                .WithDescription("Analyze text content for patterns and insights")
                .WithPriority(10)
                .RequireCapabilities("text-analysis", "pattern-recognition")
                .WithParameter("analysis_depth", "deep")
                .WithEstimatedDuration(TimeSpan.FromMinutes(2)))
            
            // Task 2: Result Processing
            .AddTask("process-results", "Result Processing", task => task
                .WithDescription("Process analysis results and extract key findings")
                .WithPriority(8)
                .RequireCapabilities("data-processing", "extraction")
                .WithParameter("processing_mode", "comprehensive")
                .WithEstimatedDuration(TimeSpan.FromMinutes(3)))
            
            // Task 3: Report Generation
            .AddTask("generate-report", "Report Generation", task => task
                .WithDescription("Generate comprehensive report from processed data")
                .WithPriority(5)
                .RequireCapabilities("report-generation", "formatting")
                .WithParameter("report_format", "executive_summary")
                .WithEstimatedDuration(TimeSpan.FromMinutes(1)))
            
            .WithAggregationStrategy(AggregationStrategy.Weighted)
            .WithMetadata("workflow_type", "analysis")
            .WithMetadata("complexity", "high")
            .Build();

        // Prepare arguments
        var arguments = new KernelArguments
        {
            ["document_content"] = "Sample document content for analysis...",
            ["analysis_requirements"] = "sentiment, topics, key_phrases, entities",
            ["output_preferences"] = "json_structured"
        };

        // Execute workflow
        return await coordinator.ExecuteWorkflowAsync(workflow, kernel, arguments);
    }
}
```

### 5. Health Monitoring

```csharp
public class HealthMonitoringExample
{
    private readonly MultiAgentCoordinator _coordinator;
    private readonly ILogger _logger;

    public HealthMonitoringExample(MultiAgentCoordinator coordinator, ILogger logger)
    {
        _coordinator = coordinator;
        _logger = logger;
    }

    public async Task MonitorAgentsAsync()
    {
        // Get all registered agents
        var agents = _coordinator.GetAllAgents();
        _logger.LogInformation("Monitoring {Count} agents...", agents.Count);

        // Check each agent's health
        foreach (var agent in agents)
        {
            // Get cached health status
            var healthStatus = agent.GetHealthStatus(_coordinator);
            _logger.LogInformation("Agent {AgentId}: {Status}", 
                agent.AgentId, 
                healthStatus?.Status ?? HealthStatus.Unknown);

            // Perform active health check
            var healthCheck = await agent.PerformHealthCheckAsync(_coordinator);
            _logger.LogInformation("  Health Check: {Result} (Response: {ResponseTime:F2} ms)",
                healthCheck.Success ? "Passed" : "Failed",
                healthCheck.ResponseTime.TotalMilliseconds);
        }

        // Log system-wide health metrics
        var healthMonitor = _coordinator.HealthMonitor;
        _logger.LogInformation(
            "System Health: {Healthy}/{Total} agents healthy ({Ratio:P})",
            healthMonitor.HealthyAgentCount,
            healthMonitor.MonitoredAgentCount,
            healthMonitor.SystemHealthRatio);
    }

    public async Task PerformRecoveryAsync()
    {
        var unhealthyAgents = _coordinator.GetAllAgents()
            .Where(a => a.GetHealthStatus(_coordinator)?.Status != HealthStatus.Healthy);

        foreach (var agent in unhealthyAgents)
        {
            _logger.LogWarning("Attempting recovery for agent {AgentId}", agent.AgentId);
            
            try
            {
                await agent.RestartAsync(_coordinator);
                _logger.LogInformation("Agent {AgentId} recovered successfully", agent.AgentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recover agent {AgentId}", agent.AgentId);
            }
        }
    }
}
```

### 6. Result Logging

```csharp
public static class WorkflowResultLogger
{
    public static void LogResult(WorkflowExecutionResult result, ILogger logger)
    {
        logger.LogInformation("\n=== Workflow Execution Results ===");
        logger.LogInformation("Success: {Success}", result.Success);
        logger.LogInformation("Execution ID: {ExecutionId}", result.ExecutionId);
        logger.LogInformation("Duration: {Duration:F2} ms", result.Duration.TotalMilliseconds);
        logger.LogInformation("Agents Used: {Count}", result.AgentsUsed.Count);

        foreach (var agent in result.AgentsUsed)
        {
            logger.LogInformation("  - {AgentId}: {Status}", agent.AgentId, agent.Status);
        }

        if (result.AggregatedResult != null)
        {
            logger.LogInformation("Aggregated Result: {Result}", result.AggregatedResult);
        }

        if (result.Errors.Any())
        {
            logger.LogWarning("Errors (showing up to 3): {ErrorCount}", result.Errors.Count);
            foreach (var error in result.Errors.Take(3))
            {
                logger.LogWarning("  - {Error}", error);
            }
        }
    }
}
```

---

## Service Layer Integration

```csharp
public interface IMultiAgentService
{
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        string workflowName, 
        KernelArguments arguments, 
        CancellationToken cancellationToken = default);
    
    IReadOnlyList<AgentInstance> GetAgents();
    Task<AgentHealthStatus> GetSystemHealthAsync(CancellationToken cancellationToken = default);
}

public class MultiAgentService : IMultiAgentService, IDisposable
{
    private readonly Kernel _kernel;
    private readonly MultiAgentCoordinator _coordinator;
    private readonly ILogger<MultiAgentService> _logger;
    private bool _initialized;

    public MultiAgentService(
        Kernel kernel,
        ILoggerFactory loggerFactory)
    {
        _kernel = kernel;
        _logger = loggerFactory.CreateLogger<MultiAgentService>();
        _coordinator = MultiAgentSetup.CreateCoordinator(loggerFactory);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        var factory = new AgentFactory(_kernel, _logger.GetLoggerFactory());
        
        await factory.CreateAnalysisAgentAsync(_coordinator);
        await factory.CreateProcessingAgentAsync(_coordinator);
        await factory.CreateReportingAgentAsync(_coordinator);

        _initialized = true;
        _logger.LogInformation("Multi-agent system initialized with {Count} agents", 
            _coordinator.GetAllAgents().Count);
    }

    public async Task<WorkflowExecutionResult> ExecuteWorkflowAsync(
        string workflowName,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            await InitializeAsync(cancellationToken);

        var workflow = _coordinator.CreateWorkflow(workflowName, $"Workflow: {workflowName}")
            .RequireAgents(_coordinator.GetAllAgents().Select(a => a.AgentId).ToArray())
            .WithAggregationStrategy(AggregationStrategy.Merge)
            .Build();

        return await _coordinator.ExecuteWorkflowAsync(workflow, _kernel, arguments);
    }

    public IReadOnlyList<AgentInstance> GetAgents() => _coordinator.GetAllAgents();

    public async Task<AgentHealthStatus> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        var monitor = _coordinator.HealthMonitor;
        
        return new AgentHealthStatus
        {
            TotalAgents = monitor.MonitoredAgentCount,
            HealthyAgents = monitor.HealthyAgentCount,
            HealthRatio = monitor.SystemHealthRatio,
            Timestamp = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _coordinator?.Dispose();
    }
}

public class AgentHealthStatus
{
    public int TotalAgents { get; set; }
    public int HealthyAgents { get; set; }
    public double HealthRatio { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

## Dependency Injection Setup

```csharp
public static class MultiAgentServiceExtensions
{
    public static IServiceCollection AddMultiAgentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Kernel
        services.AddSingleton(sp =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddGraphSupport();
            builder.AddOpenAIChatCompletion(
                modelId: configuration["AI:ModelId"] ?? "gpt-4o",
                apiKey: configuration["AI:ApiKey"]!);
            return builder.Build();
        });

        // Register multi-agent service
        services.AddSingleton<IMultiAgentService, MultiAgentService>();

        return services;
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class MultiAgentController : ControllerBase
{
    private readonly IMultiAgentService _service;
    private readonly ILogger<MultiAgentController> _logger;

    public MultiAgentController(IMultiAgentService service, ILogger<MultiAgentController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("workflow/{workflowName}")]
    public async Task<IActionResult> ExecuteWorkflow(
        string workflowName,
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var arguments = new KernelArguments();
        foreach (var kvp in request.Parameters)
        {
            arguments[kvp.Key] = kvp.Value;
        }

        var result = await _service.ExecuteWorkflowAsync(workflowName, arguments, cancellationToken);

        return Ok(new
        {
            result.Success,
            result.ExecutionId,
            Duration = result.Duration.TotalMilliseconds,
            AgentsUsed = result.AgentsUsed.Select(a => new { a.AgentId, a.Status }),
            result.AggregatedResult,
            Errors = result.Errors.Take(5)
        });
    }

    [HttpGet("agents")]
    public IActionResult GetAgents()
    {
        var agents = _service.GetAgents();
        return Ok(agents.Select(a => new
        {
            a.AgentId,
            a.Name,
            a.Description,
            a.Capabilities,
            a.Metadata
        }));
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var health = await _service.GetSystemHealthAsync(cancellationToken);
        return Ok(health);
    }
}

public record WorkflowRequest(Dictionary<string, string> Parameters);
```

---

## Testing

```csharp
using Xunit;
using Microsoft.Extensions.Logging;

public class MultiAgentTests
{
    [Fact]
    public async Task Coordinator_RegistersAgents_Successfully()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var coordinator = MultiAgentSetup.CreateCoordinator(loggerFactory);
        var kernel = CreateTestKernel();
        var factory = new AgentFactory(kernel, loggerFactory);

        // Act
        var agent = await factory.CreateAnalysisAgentAsync(coordinator);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("analysis-agent", agent.AgentId);
        Assert.Contains("text-analysis", agent.Capabilities);
    }

    [Fact]
    public async Task Coordinator_ExecutesSimpleWorkflow_Successfully()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var coordinator = MultiAgentSetup.CreateCoordinator(loggerFactory);
        var kernel = CreateTestKernel();

        // Act
        var result = await SimpleWorkflowExample.ExecuteSimpleWorkflowAsync(
            coordinator, kernel, loggerFactory);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AgentsUsed.Count >= 1);
    }

    [Fact]
    public async Task HealthMonitor_ReportsSystemHealth()
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var coordinator = MultiAgentSetup.CreateCoordinator(loggerFactory);
        var kernel = CreateTestKernel();
        var factory = new AgentFactory(kernel, loggerFactory);

        await factory.CreateAnalysisAgentAsync(coordinator);
        await factory.CreateProcessingAgentAsync(coordinator);

        // Act
        var agents = coordinator.GetAllAgents();
        var healthMonitor = coordinator.HealthMonitor;

        // Assert
        Assert.Equal(2, agents.Count);
        Assert.Equal(2, healthMonitor.MonitoredAgentCount);
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

### Agent Design

1. **Single Responsibility**: Each agent should have a focused specialization
2. **Clear Capabilities**: Define explicit capabilities for proper task routing
3. **Idempotent Operations**: Agent functions should be idempotent when possible
4. **Graceful Degradation**: Handle partial failures gracefully

### Work Distribution

1. **Capability Matching**: Use RoleBased strategy for specialized workflows
2. **Load Balancing**: Use LeastLoaded for even distribution
3. **Priority Management**: Enable prioritization for time-sensitive tasks
4. **Timeout Configuration**: Set appropriate timeouts per workflow complexity

### Result Aggregation

1. **Merge**: Combine results when agents produce complementary data
2. **Consensus**: Use when agents produce similar results needing validation
3. **Weighted**: Apply when agent outputs have different reliability levels
4. **First/Last**: Use for fallback scenarios

### Health Monitoring

1. **Active Checks**: Perform periodic health checks
2. **Automatic Recovery**: Implement restart logic for failed agents
3. **Circuit Breaking**: Disable unhealthy agents temporarily
4. **Alerting**: Log and alert on system health degradation

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [ReAct Agent](template-skg-react-agent.md) - Single agent with tools
- [Chatbot with Memory](template-skg-chatbot-memory.md) - Conversational agents
- [Chain of Thought](template-skg-chain-of-thought.md) - Reasoning patterns

---

## External References

- [Semantic Kernel Graph - Multi-Agent](https://github.com/kallebelins/semantic-kernel-graph)
- [Multi-Agent and Shared State](../how-to/multi-agent-and-shared-state.md)
- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)
