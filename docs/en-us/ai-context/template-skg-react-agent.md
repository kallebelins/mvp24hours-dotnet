# ReAct Agent Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for implementing ReAct (Reasoning → Acting → Observing) agents using Semantic Kernel Graph.

---

## Overview

ReAct agents combine reasoning with action execution in an iterative loop. This template covers:
- ReAct pattern implementation
- Tool registration and discovery
- Action selection strategies
- Parameter validation
- Extensible tool systems

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| AI needs to use multiple tools | ✅ Recommended |
| Problem requires reasoning steps | ✅ Recommended |
| Dynamic tool selection needed | ✅ Recommended |
| Fixed workflows | ⚠️ Use Graph Executor |
| Simple Q&A | ⚠️ Use Chat Completion |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
</ItemGroup>
```

---

## ReAct Pattern

```
┌─────────────┐
│   Query     │
└──────┬──────┘
       ▼
┌─────────────┐
│  Reasoning  │◄─────┐
└──────┬──────┘      │
       ▼             │
┌─────────────┐      │
│   Acting    │      │
└──────┬──────┘      │
       ▼             │
┌─────────────┐      │
│  Observing  │──────┘
└──────┬──────┘
       ▼
┌─────────────┐
│   Answer    │
└─────────────┘
```

---

## Implementation Patterns

### 1. Basic ReAct Agent

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;
using System.ComponentModel;

public class ReActAgentBuilder
{
    public static GraphExecutor CreateReActAgent(Kernel kernel)
    {
        // Register tools
        RegisterTools(kernel);

        var executor = new GraphExecutor("ReActAgent", "ReAct agent with tool usage");

        // Reasoning node - analyzes query and selects action
        var reasoningNode = new FunctionGraphNode(
            CreateReasoningFunction(kernel),
            "reasoning",
            "Analyze query and select action")
            .StoreResultAs("reasoning_result");

        // Action node - executes selected tool
        var actionNode = ActionGraphNode.CreateWithActions(
            kernel,
            new ActionSelectionCriteria(),
            "action");
        actionNode.ConfigureExecution(
            ActionSelectionStrategy.Intelligent,
            enableParameterValidation: true);

        // Observation node - summarizes action result
        var observationNode = new FunctionGraphNode(
            CreateObservationFunction(kernel),
            "observation",
            "Summarize action result")
            .StoreResultAs("final_answer");

        // Build graph
        executor.AddNode(reasoningNode);
        executor.AddNode(actionNode);
        executor.AddNode(observationNode);

        executor.SetStartNode(reasoningNode.NodeId);
        executor.AddEdge(ConditionalEdge.CreateUnconditional(reasoningNode, actionNode));
        executor.AddEdge(ConditionalEdge.CreateUnconditional(actionNode, observationNode));

        return executor;
    }

    private static void RegisterTools(Kernel kernel)
    {
        kernel.ImportPluginFromObject(new WeatherTool(), "Weather");
        kernel.ImportPluginFromObject(new CalculatorTool(), "Calculator");
        kernel.ImportPluginFromObject(new SearchTool(), "Search");
    }

    private static KernelFunction CreateReasoningFunction(Kernel kernel)
    {
        return kernel.CreateFunctionFromMethod(
            (KernelArguments args) =>
            {
                var query = args.TryGetValue("user_query", out var q) 
                    ? q?.ToString() ?? "" 
                    : "";

                // Simple heuristics for tool selection
                var (action, parameters) = AnalyzeQuery(query);

                args["suggested_action"] = action;
                args["action_parameters"] = parameters;

                return $"Reasoning: Selected action='{action}' with parameters";
            },
            functionName: "Reasoning",
            description: "Analyzes query and suggests action");
    }

    private static (string action, Dictionary<string, object> parameters) AnalyzeQuery(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        var parameters = new Dictionary<string, object>();

        if (lowerQuery.Contains("weather"))
        {
            var cityMatch = System.Text.RegularExpressions.Regex.Match(
                query, @"in ([A-Za-z\s]+)", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (cityMatch.Success)
                parameters["city"] = cityMatch.Groups[1].Value.Trim();

            return ("GetWeather", parameters);
        }

        if (lowerQuery.Contains("calculate") || 
            System.Text.RegularExpressions.Regex.IsMatch(query, @"\d+\s*[\*\+\-\/]\s*\d+"))
        {
            var calcMatch = System.Text.RegularExpressions.Regex.Match(
                query, @"(\d+\s*[\*\+\-\/]\s*\d+)");
            
            if (calcMatch.Success)
                parameters["expression"] = calcMatch.Groups[1].Value;

            return ("Calculate", parameters);
        }

        parameters["query"] = query;
        return ("Search", parameters);
    }

    private static KernelFunction CreateObservationFunction(Kernel kernel)
    {
        return kernel.CreateFunctionFromMethod(
            (KernelArguments args) =>
            {
                var action = args.TryGetValue("suggested_action", out var a) 
                    ? a?.ToString() ?? "" 
                    : "";
                var result = args.TryGetValue("action_result", out var r) 
                    ? r?.ToString() ?? "" 
                    : "";

                return action switch
                {
                    "GetWeather" => $"Based on your weather query: {result}",
                    "Calculate" => $"The calculation result is: {result}",
                    "Search" => $"Here's what I found: {result}",
                    _ => $"Result: {result}"
                };
            },
            functionName: "Observation",
            description: "Summarizes action result");
    }
}
```

### 2. Tool Implementations

```csharp
public class WeatherTool
{
    [KernelFunction("GetWeather")]
    [Description("Gets the current weather for a city")]
    public string GetWeather(
        [Description("City name")] string city)
    {
        // Simulate weather data
        return city.ToLowerInvariant() switch
        {
            "london" => $"Weather in London: Cloudy, 15°C, light rain expected",
            "paris" => $"Weather in Paris: Sunny, 22°C, clear skies",
            "tokyo" => $"Weather in Tokyo: Partly cloudy, 18°C, humid",
            "new york" => $"Weather in New York: Clear, 20°C, moderate wind",
            "são paulo" or "sao paulo" => $"Weather in São Paulo: Warm, 28°C, chance of afternoon showers",
            _ => $"Weather in {city}: Sunny, 20°C, pleasant conditions"
        };
    }

    [KernelFunction("GetForecast")]
    [Description("Gets weather forecast for multiple days")]
    public string GetForecast(
        [Description("City name")] string city,
        [Description("Number of days (1-7)")] int days = 3)
    {
        days = Math.Clamp(days, 1, 7);
        return $"Forecast for {city} ({days} days): Generally pleasant with temperatures between 15-25°C";
    }
}

public class CalculatorTool
{
    [KernelFunction("Calculate")]
    [Description("Performs mathematical calculations")]
    public string Calculate(
        [Description("Mathematical expression (e.g., '5 * 3')")] string expression)
    {
        try
        {
            // Simple expression evaluation
            var result = EvaluateExpression(expression);
            return $"{expression} = {result}";
        }
        catch (Exception ex)
        {
            return $"Error calculating '{expression}': {ex.Message}";
        }
    }

    private double EvaluateExpression(string expression)
    {
        expression = expression.Replace(" ", "");

        if (expression.Contains("*"))
        {
            var parts = expression.Split('*');
            return double.Parse(parts[0]) * double.Parse(parts[1]);
        }
        if (expression.Contains("/"))
        {
            var parts = expression.Split('/');
            var divisor = double.Parse(parts[1]);
            if (divisor == 0) throw new DivideByZeroException();
            return double.Parse(parts[0]) / divisor;
        }
        if (expression.Contains("+"))
        {
            var parts = expression.Split('+');
            return double.Parse(parts[0]) + double.Parse(parts[1]);
        }
        if (expression.Contains("-"))
        {
            var parts = expression.Split('-');
            return double.Parse(parts[0]) - double.Parse(parts[1]);
        }

        return double.Parse(expression);
    }

    [KernelFunction("Percentage")]
    [Description("Calculates percentage of a value")]
    public string Percentage(
        [Description("The value")] double value,
        [Description("The percentage")] double percent)
    {
        var result = value * percent / 100;
        return $"{percent}% of {value} = {result}";
    }
}

public class SearchTool
{
    [KernelFunction("Search")]
    [Description("Searches for information on a topic")]
    public string Search(
        [Description("Search query")] string query)
    {
        // Simulate search results
        var lowerQuery = query.ToLowerInvariant();

        return lowerQuery switch
        {
            var q when q.Contains("programming") && q.Contains("best practices") =>
                "Best programming practices include: clean code, SOLID principles, proper testing, and documentation.",
            var q when q.Contains("machine learning") =>
                "Machine learning is a subset of AI that enables systems to learn from data without explicit programming.",
            var q when q.Contains("dotnet") || q.Contains(".net") =>
                ".NET is a free, open-source development platform for building modern applications.",
            _ => $"Search results for '{query}': Found relevant information on the topic."
        };
    }

    [KernelFunction("SearchWithFilters")]
    [Description("Searches with category filters")]
    public string SearchWithFilters(
        [Description("Search query")] string query,
        [Description("Category filter (tech, science, business)")] string category = "general")
    {
        return $"[{category.ToUpper()}] Results for '{query}': Found {Random.Shared.Next(5, 20)} relevant items.";
    }
}
```

### 3. AI-Powered Reasoning

```csharp
public class AIReActAgentBuilder
{
    public static GraphExecutor CreateAIReActAgent(Kernel kernel)
    {
        RegisterTools(kernel);

        var executor = new GraphExecutor("AIReActAgent", "ReAct agent with AI reasoning");

        // AI-powered reasoning
        var reasoningNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                You are a helpful assistant that selects the best tool to answer user queries.

                Available tools:
                - GetWeather(city): Get weather for a city
                - Calculate(expression): Perform math calculations
                - Search(query): Search for information

                User query: {{$user_query}}

                Analyze the query and respond in this exact format:
                ACTION: [tool_name]
                PARAMETERS: [parameter_name]=[value]
                REASONING: [brief explanation]
                """,
                functionName: "AIReasoning",
                description: "AI-powered reasoning"),
            "ai-reasoning")
            .StoreResultAs("ai_reasoning");

        // Parse AI reasoning and execute action
        var actionParserNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var reasoning = args.GetOrCreateGraphState().GetValue<string>("ai_reasoning") ?? "";
                    var (action, parameters) = ParseAIResponse(reasoning);
                    
                    args["suggested_action"] = action;
                    args["action_parameters"] = parameters;
                    
                    return $"Parsed: action={action}";
                },
                functionName: "ParseReasoning",
                description: "Parses AI reasoning"),
            "action-parser");

        // Action execution
        var actionNode = ActionGraphNode.CreateWithActions(
            kernel,
            new ActionSelectionCriteria(),
            "action-executor");

        // AI-powered observation
        var observationNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Based on the user's query and the tool result, provide a helpful response.

                User query: {{$user_query}}
                Tool used: {{$suggested_action}}
                Tool result: {{$action_result}}

                Provide a natural, helpful response:
                """,
                functionName: "AIObservation",
                description: "AI-powered observation"),
            "ai-observation")
            .StoreResultAs("final_answer");

        executor.AddNode(reasoningNode);
        executor.AddNode(actionParserNode);
        executor.AddNode(actionNode);
        executor.AddNode(observationNode);

        executor.SetStartNode(reasoningNode.NodeId);
        executor.Connect(reasoningNode.NodeId, actionParserNode.NodeId);
        executor.Connect(actionParserNode.NodeId, actionNode.NodeId);
        executor.Connect(actionNode.NodeId, observationNode.NodeId);

        return executor;
    }

    private static (string action, Dictionary<string, object> parameters) ParseAIResponse(string response)
    {
        var action = "Search";
        var parameters = new Dictionary<string, object>();

        var actionMatch = System.Text.RegularExpressions.Regex.Match(
            response, @"ACTION:\s*(\w+)");
        if (actionMatch.Success)
            action = actionMatch.Groups[1].Value;

        var paramMatch = System.Text.RegularExpressions.Regex.Match(
            response, @"PARAMETERS:\s*(\w+)=(.+?)(?:\n|$)");
        if (paramMatch.Success)
            parameters[paramMatch.Groups[1].Value] = paramMatch.Groups[2].Value.Trim();

        return (action, parameters);
    }

    private static void RegisterTools(Kernel kernel)
    {
        kernel.ImportPluginFromObject(new WeatherTool(), "Weather");
        kernel.ImportPluginFromObject(new CalculatorTool(), "Calculator");
        kernel.ImportPluginFromObject(new SearchTool(), "Search");
    }
}
```

### 4. Multi-Turn ReAct Agent

```csharp
public class MultiTurnReActAgent
{
    private readonly Kernel _kernel;
    private readonly GraphExecutor _graph;
    private readonly List<ConversationTurn> _history = new();

    public MultiTurnReActAgent(Kernel kernel)
    {
        _kernel = kernel;
        _graph = CreateMultiTurnGraph(kernel);
    }

    public async Task<string> ProcessAsync(string userQuery, CancellationToken cancellationToken = default)
    {
        _history.Add(new ConversationTurn { Role = "user", Content = userQuery });

        var arguments = new KernelArguments
        {
            ["user_query"] = userQuery,
            ["conversation_history"] = FormatHistory(),
            ["max_iterations"] = 3
        };

        await _graph.ExecuteAsync(_kernel, arguments, cancellationToken);

        var answer = arguments.GetOrCreateGraphState().GetValue<string>("final_answer") ?? "No answer";
        _history.Add(new ConversationTurn { Role = "assistant", Content = answer });

        return answer;
    }

    private string FormatHistory()
    {
        return string.Join("\n", _history.TakeLast(10).Select(t => $"{t.Role}: {t.Content}"));
    }

    private GraphExecutor CreateMultiTurnGraph(Kernel kernel)
    {
        // Register tools
        kernel.ImportPluginFromObject(new WeatherTool(), "Weather");
        kernel.ImportPluginFromObject(new CalculatorTool(), "Calculator");
        kernel.ImportPluginFromObject(new SearchTool(), "Search");

        var executor = new GraphExecutor("MultiTurnReAct", "Multi-turn ReAct agent");

        // Context-aware reasoning
        var reasoningNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Previous conversation:
                {{$conversation_history}}

                Current query: {{$user_query}}

                Available tools: GetWeather, Calculate, Search

                Based on the conversation context, select the best action.
                Response format:
                ACTION: [tool_name]
                PARAMETERS: [name]=[value]
                """,
                functionName: "ContextReasoning",
                description: "Context-aware reasoning"),
            "reasoning");

        // ... rest of the graph setup similar to previous examples

        return executor;
    }

    private class ConversationTurn
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
```

### 5. Tool Extensibility

```csharp
public class ExtensibleReActAgent
{
    private readonly Kernel _kernel;
    private readonly List<object> _tools = new();

    public ExtensibleReActAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public void RegisterTool(object tool, string pluginName)
    {
        _tools.Add(tool);
        _kernel.ImportPluginFromObject(tool, pluginName);
    }

    public void RegisterToolFromType<T>(string pluginName) where T : new()
    {
        var tool = new T();
        RegisterTool(tool, pluginName);
    }

    public string GetAvailableTools()
    {
        var tools = _kernel.Plugins
            .SelectMany(p => p.Select(f => $"- {p.Name}.{f.Name}: {f.Description}"))
            .ToList();

        return string.Join("\n", tools);
    }

    public GraphExecutor BuildAgent()
    {
        var executor = new GraphExecutor("ExtensibleAgent", "Agent with dynamic tools");

        // Build reasoning node with dynamic tool list
        var reasoningNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt(
                $"""
                You are an assistant with access to these tools:
                {GetAvailableTools()}

                User query: {{{{$user_query}}}}

                Select the best tool and parameters.
                """,
                functionName: "DynamicReasoning",
                description: "Dynamic reasoning"),
            "reasoning");

        var actionNode = ActionGraphNode.CreateWithActions(
            _kernel,
            new ActionSelectionCriteria(),
            "action");

        var observationNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var result = args.TryGetValue("action_result", out var r) ? r?.ToString() : "";
                    return $"Result: {result}";
                },
                functionName: "Observe",
                description: "Observation"),
            "observation")
            .StoreResultAs("final_answer");

        executor.AddNode(reasoningNode);
        executor.AddNode(actionNode);
        executor.AddNode(observationNode);

        executor.SetStartNode(reasoningNode.NodeId);
        executor.Connect(reasoningNode.NodeId, actionNode.NodeId);
        executor.Connect(actionNode.NodeId, observationNode.NodeId);

        return executor;
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ILogger<AgentController> _logger;

    public AgentController(Kernel kernel, ILogger<AgentController> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query(
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken)
    {
        var agent = ReActAgentBuilder.CreateReActAgent(_kernel);

        var arguments = new KernelArguments
        {
            ["user_query"] = request.Query
        };

        await agent.ExecuteAsync(_kernel, arguments, cancellationToken);

        var answer = arguments.GetOrCreateGraphState().GetValue<string>("final_answer");

        return Ok(new { answer });
    }
}

public record AgentRequest(string Query);
```

---

## Testing

```csharp
using Xunit;

public class ReActAgentTests
{
    [Fact]
    public async Task Agent_HandlesWeatherQuery()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var agent = ReActAgentBuilder.CreateReActAgent(kernel);
        var args = new KernelArguments { ["user_query"] = "What's the weather in London?" };

        // Act
        await agent.ExecuteAsync(kernel, args);
        var answer = args.GetOrCreateGraphState().GetValue<string>("final_answer");

        // Assert
        Assert.Contains("weather", answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("London", answer);
    }

    [Fact]
    public async Task Agent_HandlesCalculation()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var agent = ReActAgentBuilder.CreateReActAgent(kernel);
        var args = new KernelArguments { ["user_query"] = "Calculate 15 * 7" };

        // Act
        await agent.ExecuteAsync(kernel, args);
        var answer = args.GetOrCreateGraphState().GetValue<string>("final_answer");

        // Assert
        Assert.Contains("105", answer);
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

### Tool Design

1. **Clear Descriptions**: AI needs to understand what tools do
2. **Parameter Validation**: Validate inputs before processing
3. **Consistent Returns**: Return formatted, usable results
4. **Error Messages**: Return helpful error information

### Reasoning Quality

1. **Explicit Tool List**: Always provide available tools to reasoning
2. **Structured Output**: Use consistent formats for parsing
3. **Context Awareness**: Include conversation history when relevant

### Performance

1. **Limit Iterations**: Set maximum ReAct loop iterations
2. **Cache Results**: Cache tool results when appropriate
3. **Timeout Handling**: Set reasonable timeouts for tools

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [Chain of Thought](template-skg-chain-of-thought.md) - Step-by-step reasoning
- [Plugins & Functions](template-sk-plugins.md) - Tool creation patterns

---

## External References

- [ReAct Paper](https://arxiv.org/abs/2210.03629)
- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [Tool Use Patterns](https://learn.microsoft.com/semantic-kernel)

