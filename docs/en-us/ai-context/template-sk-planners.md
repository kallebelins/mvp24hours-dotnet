# Planners Template - Semantic Kernel

> **Purpose**: This template provides AI agents with patterns and implementation guidelines for using planners in Microsoft Semantic Kernel for task decomposition and auto-planning.

---

## Overview

Planners enable AI to automatically decompose complex tasks into steps and execute them. This template covers:
- Function Calling Planner (recommended)
- Handlebars Planner
- Custom planning strategies
- Plan execution and monitoring

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Multi-step task automation | ✅ Recommended |
| Dynamic workflow creation | ✅ Recommended |
| Goal-oriented AI | ✅ Recommended |
| Simple Q&A | ⚠️ Use Chat Completion |
| Fixed workflows | ⚠️ Use Graph Executor |
| Complex reasoning | ⚠️ Consider Chain of Thought |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Planners.Handlebars" Version="1.*-*" />
</ItemGroup>
```

---

## Planner Types Comparison

| Planner | Best For | Complexity | Reliability |
|---------|----------|------------|-------------|
| Function Calling | Most scenarios | Low | High |
| Handlebars | Template-based workflows | Medium | Medium |
| Custom | Specific requirements | High | Varies |

---

## Implementation Patterns

### 1. Function Calling Planner (Recommended)

The modern approach using automatic function calling:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

public class FunctionCallingPlannerService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;

    public FunctionCallingPlannerService(Kernel kernel)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> ExecuteGoalAsync(
        string goal,
        CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("""
            You are a helpful assistant that accomplishes goals by using the available tools.
            Break down complex tasks into steps and execute them one by one.
            Explain your reasoning and the steps you're taking.
            """);
        chatHistory.AddUserMessage(goal);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel,
            cancellationToken);

        return response.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> ExecuteGoalStreamingAsync(
        string goal,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("""
            You are a helpful assistant that accomplishes goals by using the available tools.
            Break down complex tasks into steps and execute them one by one.
            """);
        chatHistory.AddUserMessage(goal);

        await foreach (var chunk in _chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory,
            settings,
            _kernel,
            cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }
}
```

### 2. Handlebars Planner

For template-based plan generation:

```csharp
using Microsoft.SemanticKernel.Planning.Handlebars;

public class HandlebarsPlannerService
{
    private readonly Kernel _kernel;
    private readonly HandlebarsPlanner _planner;

    public HandlebarsPlannerService(Kernel kernel)
    {
        _kernel = kernel;
        _planner = new HandlebarsPlanner(new HandlebarsPlannerOptions
        {
            AllowLoops = true,
            MaxTokens = 4000
        });
    }

    public async Task<HandlebarsPlan> CreatePlanAsync(
        string goal,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planner.CreatePlanAsync(_kernel, goal, cancellationToken: cancellationToken);
        return plan;
    }

    public async Task<string> ExecutePlanAsync(
        HandlebarsPlan plan,
        CancellationToken cancellationToken = default)
    {
        var result = await plan.InvokeAsync(_kernel, cancellationToken: cancellationToken);
        return result;
    }

    public async Task<string> CreateAndExecuteAsync(
        string goal,
        CancellationToken cancellationToken = default)
    {
        var plan = await CreatePlanAsync(goal, cancellationToken);
        return await ExecutePlanAsync(plan, cancellationToken);
    }
}
```

### 3. Stepwise Planner Pattern

A custom pattern for step-by-step execution with visibility:

```csharp
public class StepwisePlannerService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly ILogger<StepwisePlannerService> _logger;

    public StepwisePlannerService(Kernel kernel, ILogger<StepwisePlannerService> logger)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _logger = logger;
    }

    public async Task<PlanExecutionResult> ExecuteWithStepsAsync(
        string goal,
        int maxSteps = 10,
        CancellationToken cancellationToken = default)
    {
        var result = new PlanExecutionResult { Goal = goal };
        var chatHistory = new ChatHistory();
        
        chatHistory.AddSystemMessage(GetPlannerSystemPrompt());
        chatHistory.AddUserMessage($"Goal: {goal}");

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        for (int step = 0; step < maxSteps; step++)
        {
            var stepResult = new PlanStep { StepNumber = step + 1 };

            try
            {
                var response = await _chatCompletion.GetChatMessageContentAsync(
                    chatHistory,
                    settings,
                    _kernel,
                    cancellationToken);

                stepResult.Action = response.Content ?? string.Empty;
                stepResult.Success = true;

                // Check for function calls in metadata
                if (response.Metadata?.TryGetValue("FunctionCalls", out var calls) == true)
                {
                    stepResult.FunctionsCalled = calls?.ToString();
                }

                chatHistory.AddAssistantMessage(response.Content ?? string.Empty);

                // Check if goal is complete
                if (IsGoalComplete(response.Content))
                {
                    result.IsComplete = true;
                    result.FinalAnswer = ExtractFinalAnswer(response.Content);
                    break;
                }

                // Add continuation prompt
                chatHistory.AddUserMessage("Continue with the next step, or provide the final answer if the goal is complete.");
            }
            catch (Exception ex)
            {
                stepResult.Success = false;
                stepResult.Error = ex.Message;
                _logger.LogError(ex, "Error in step {Step}", step + 1);
            }

            result.Steps.Add(stepResult);
        }

        return result;
    }

    private string GetPlannerSystemPrompt()
    {
        return """
            You are a goal-oriented assistant that breaks down complex tasks into steps.
            
            For each step:
            1. Explain what you're going to do
            2. Use the available functions to accomplish it
            3. Report the result
            
            When the goal is complete, start your response with "GOAL COMPLETE:" followed by the final answer.
            
            Available functions will be called automatically based on your needs.
            """;
    }

    private bool IsGoalComplete(string? content)
    {
        return content?.Contains("GOAL COMPLETE:", StringComparison.OrdinalIgnoreCase) == true;
    }

    private string ExtractFinalAnswer(string? content)
    {
        if (content == null) return string.Empty;
        
        var index = content.IndexOf("GOAL COMPLETE:", StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            return content[(index + "GOAL COMPLETE:".Length)..].Trim();
        }
        return content;
    }
}

public class PlanExecutionResult
{
    public string Goal { get; set; } = string.Empty;
    public List<PlanStep> Steps { get; set; } = new();
    public bool IsComplete { get; set; }
    public string FinalAnswer { get; set; } = string.Empty;
}

public class PlanStep
{
    public int StepNumber { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? FunctionsCalled { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}
```

### 4. Goal-Oriented Agent

A more sophisticated planning approach:

```csharp
public class GoalOrientedAgent
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly List<string> _executionHistory = new();

    public GoalOrientedAgent(Kernel kernel)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<AgentResult> AchieveGoalAsync(
        string goal,
        AgentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new AgentOptions();
        var result = new AgentResult { Goal = goal };
        
        // Phase 1: Plan Generation
        var plan = await GeneratePlanAsync(goal, cancellationToken);
        result.Plan = plan;

        // Phase 2: Plan Validation
        if (options.ValidatePlan)
        {
            var isValid = await ValidatePlanAsync(plan, goal, cancellationToken);
            if (!isValid)
            {
                result.Status = AgentStatus.PlanValidationFailed;
                return result;
            }
        }

        // Phase 3: Plan Execution
        result.ExecutionResult = await ExecutePlanAsync(plan, options, cancellationToken);
        result.Status = result.ExecutionResult.Success 
            ? AgentStatus.Completed 
            : AgentStatus.ExecutionFailed;

        return result;
    }

    private async Task<string> GeneratePlanAsync(
        string goal,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
            Create a detailed step-by-step plan to achieve the following goal:
            
            Goal: {goal}
            
            Available tools: {GetAvailableFunctions()}
            
            Plan (numbered steps):
            """;

        var planResult = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return planResult.GetValue<string>() ?? string.Empty;
    }

    private async Task<bool> ValidatePlanAsync(
        string plan,
        string goal,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
            Validate if the following plan can achieve the goal.
            
            Goal: {goal}
            
            Plan: {plan}
            
            Answer only YES or NO:
            """;

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.GetValue<string>()?.Trim().Equals("YES", StringComparison.OrdinalIgnoreCase) == true;
    }

    private async Task<ExecutionResult> ExecutePlanAsync(
        string plan,
        AgentOptions options,
        CancellationToken cancellationToken)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage($"""
            Execute the following plan step by step:
            
            {plan}
            
            Use the available tools to complete each step.
            Report progress after each step.
            """);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel,
            cancellationToken);

        return new ExecutionResult
        {
            Success = true,
            Output = response.Content ?? string.Empty
        };
    }

    private string GetAvailableFunctions()
    {
        var functions = _kernel.Plugins
            .SelectMany(p => p.Select(f => $"- {p.Name}.{f.Name}: {f.Description}"));
        return string.Join("\n", functions);
    }
}

public class AgentOptions
{
    public bool ValidatePlan { get; set; } = true;
    public int MaxSteps { get; set; } = 10;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
}

public class AgentResult
{
    public string Goal { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public ExecutionResult ExecutionResult { get; set; } = new();
}

public enum AgentStatus
{
    Pending,
    Planning,
    Executing,
    Completed,
    PlanValidationFailed,
    ExecutionFailed
}

public class ExecutionResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<string> StepOutputs { get; set; } = new();
}
```

---

## Plugins for Planning

### Task Management Plugin

```csharp
public class TaskPlugin
{
    private readonly List<TaskItem> _tasks = new();

    [KernelFunction("CreateTask")]
    [Description("Creates a new task")]
    public string CreateTask(
        [Description("Task title")] string title,
        [Description("Task description")] string description,
        [Description("Priority (1-5, 1 is highest)")] int priority = 3)
    {
        var task = new TaskItem
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            Description = description,
            Priority = priority,
            Status = "Pending"
        };
        _tasks.Add(task);
        return $"Created task '{title}' with ID {task.Id}";
    }

    [KernelFunction("ListTasks")]
    [Description("Lists all tasks")]
    public string ListTasks()
    {
        if (!_tasks.Any())
            return "No tasks found.";

        return string.Join("\n", _tasks.Select(t => 
            $"- [{t.Status}] {t.Title} (Priority: {t.Priority})"));
    }

    [KernelFunction("CompleteTask")]
    [Description("Marks a task as complete")]
    public string CompleteTask([Description("Task ID or title")] string identifier)
    {
        var task = _tasks.FirstOrDefault(t => 
            t.Id == identifier || t.Title.Contains(identifier, StringComparison.OrdinalIgnoreCase));

        if (task == null)
            return $"Task '{identifier}' not found.";

        task.Status = "Completed";
        return $"Task '{task.Title}' marked as complete.";
    }
}

public class TaskItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Status { get; set; } = "Pending";
}
```

### Research Plugin

```csharp
public class ResearchPlugin
{
    private readonly HttpClient _httpClient;

    public ResearchPlugin(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [KernelFunction("SearchWeb")]
    [Description("Searches the web for information")]
    public async Task<string> SearchWebAsync(
        [Description("Search query")] string query,
        CancellationToken cancellationToken = default)
    {
        // Simplified - in production, use a real search API
        return $"Search results for '{query}': Found relevant information about {query}.";
    }

    [KernelFunction("SummarizeUrl")]
    [Description("Summarizes content from a URL")]
    public async Task<string> SummarizeUrlAsync(
        [Description("URL to summarize")] string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _httpClient.GetStringAsync(url, cancellationToken);
            var preview = content.Length > 500 ? content[..500] + "..." : content;
            return $"Content from {url}: {preview}";
        }
        catch
        {
            return $"Unable to fetch content from {url}";
        }
    }

    [KernelFunction("TakeNotes")]
    [Description("Records notes for later reference")]
    public string TakeNotes(
        [Description("Topic of the notes")] string topic,
        [Description("Notes content")] string content)
    {
        // In production, persist this
        return $"Recorded notes on '{topic}': {content[..Math.Min(100, content.Length)]}...";
    }
}
```

---

## Web API Integration

### Planner Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class PlannerController : ControllerBase
{
    private readonly FunctionCallingPlannerService _plannerService;
    private readonly ILogger<PlannerController> _logger;

    public PlannerController(
        FunctionCallingPlannerService plannerService,
        ILogger<PlannerController> logger)
    {
        _plannerService = plannerService;
        _logger = logger;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteGoal(
        [FromBody] GoalRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _plannerService.ExecuteGoalAsync(request.Goal, cancellationToken);
        return Ok(new { result });
    }

    [HttpPost("execute/stream")]
    public async Task StreamExecuteGoal(
        [FromBody] GoalRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";

        await foreach (var chunk in _plannerService.ExecuteGoalStreamingAsync(request.Goal, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }
}

public record GoalRequest(string Goal);
```

---

## Dependency Injection Setup

```csharp
public static class PlannerServiceExtensions
{
    public static IServiceCollection AddPlannerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Kernel with plugins
        services.AddSingleton(sp =>
        {
            var kernel = KernelFactory.CreateKernel(configuration);

            // Register planning-relevant plugins
            kernel.ImportPluginFromType<TaskPlugin>("Tasks");
            kernel.ImportPluginFromType<CalculatorPlugin>("Calculator");
            kernel.ImportPluginFromType<DateTimePlugin>("DateTime");

            // Add research plugin with HttpClient
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            kernel.ImportPluginFromObject(new ResearchPlugin(httpClient), "Research");

            return kernel;
        });

        services.AddHttpClient();
        services.AddScoped<FunctionCallingPlannerService>();
        services.AddScoped<StepwisePlannerService>();
        services.AddScoped<GoalOrientedAgent>();

        return services;
    }
}
```

---

## Error Handling

```csharp
public class ResilientPlannerService
{
    private readonly FunctionCallingPlannerService _plannerService;
    private readonly ILogger<ResilientPlannerService> _logger;

    public async Task<PlannerResult> ExecuteWithRetryAsync(
        string goal,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var result = new PlannerResult { Goal = goal };

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                result.Output = await _plannerService.ExecuteGoalAsync(goal, cancellationToken);
                result.Success = true;
                return result;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limited, waiting before retry {Attempt}", attempt + 1);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Planner execution failed on attempt {Attempt}", attempt + 1);
                result.Errors.Add(ex.Message);
                
                if (attempt == maxRetries - 1)
                {
                    result.Success = false;
                    result.Output = "Failed to execute goal after multiple attempts.";
                }
            }
        }

        return result;
    }
}

public class PlannerResult
{
    public string Goal { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
```

---

## Testing

```csharp
using Xunit;

public class PlannerServiceTests
{
    [Fact]
    public async Task ExecuteGoal_WithValidGoal_ReturnsResult()
    {
        // Integration test with real kernel
        // Arrange
        var kernel = CreateTestKernel();
        var service = new FunctionCallingPlannerService(kernel);

        // Act
        var result = await service.ExecuteGoalAsync("Calculate 5 + 3");

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("8", result);
    }

    [Fact]
    public async Task StepwisePlanner_TracksSteps()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var logger = new Mock<ILogger<StepwisePlannerService>>();
        var service = new StepwisePlannerService(kernel, logger.Object);

        // Act
        var result = await service.ExecuteWithStepsAsync("What day is today?", maxSteps: 5);

        // Assert
        Assert.True(result.Steps.Count > 0);
    }

    private Kernel CreateTestKernel()
    {
        var builder = Kernel.CreateBuilder();
        // Add test configuration
        return builder.Build();
    }
}
```

---

## Best Practices

### Goal Definition

1. **Be Specific**: Clear, measurable goals lead to better plans
2. **Provide Context**: Include relevant constraints and requirements
3. **Set Boundaries**: Specify what tools/actions are allowed

```csharp
// Good goal
var goal = "Calculate the total cost of 5 items at $10 each, apply a 15% discount, and add 8% tax";

// Vague goal (harder to plan)
var goal = "Figure out the price";
```

### Plugin Design for Planning

1. **Atomic Functions**: Each function does one thing well
2. **Clear Descriptions**: AI needs to understand what functions do
3. **Predictable Output**: Consistent output formats help planning
4. **Error Messages**: Helpful errors guide plan adjustments

### Monitoring and Debugging

```csharp
public class MonitoredPlannerService
{
    private readonly FunctionCallingPlannerService _planner;
    private readonly ILogger _logger;

    public async Task<string> ExecuteWithMonitoringAsync(string goal, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting plan execution for goal: {Goal}", goal);

        try
        {
            var result = await _planner.ExecuteGoalAsync(goal, cancellationToken);
            
            _logger.LogInformation(
                "Plan execution completed in {Elapsed}ms. Result length: {Length}",
                stopwatch.ElapsedMilliseconds,
                result.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan execution failed after {Elapsed}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## Related Templates

- [Chat Completion](template-sk-chat-completion.md) - Basic chat functionality
- [Plugins & Functions](template-sk-plugins.md) - Tool integration
- [Graph Executor](template-skg-graph-executor.md) - Fixed workflow execution
- [ReAct Agent](template-skg-react-agent.md) - Reasoning with tools

---

## External References

- [Semantic Kernel Planners](https://learn.microsoft.com/semantic-kernel/agents/planners)
- [Function Calling](https://learn.microsoft.com/semantic-kernel/agents/plugins/using-ai-functions)
- [Handlebars Planner](https://learn.microsoft.com/semantic-kernel/agents/planners/handlebars-planner)

