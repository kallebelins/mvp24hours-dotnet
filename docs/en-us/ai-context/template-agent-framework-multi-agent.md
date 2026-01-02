# Multi-Agent Template - Microsoft Agent Framework

> **Purpose**: This template provides AI agents with patterns for implementing multi-agent systems with coordination, communication, and orchestration using Microsoft.Extensions.AI.

---

## Overview

Multi-agent systems enable complex AI behaviors through:
- Multiple specialized agents working together
- Agent coordination and orchestration
- Work distribution strategies
- Inter-agent communication
- Result aggregation and consensus

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Complex tasks requiring diverse expertise | ✅ Recommended |
| Parallel processing of subtasks | ✅ Recommended |
| Debate/consensus scenarios | ✅ Recommended |
| Review and validation workflows | ✅ Recommended |
| Simple single-purpose tasks | ⚠️ Use Basic Agent |
| Sequential workflows | ⚠️ Use Workflow Template |

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

### Multi-Agent Architecture

```
                    ┌──────────────────────┐
                    │    Orchestrator      │
                    │    Agent             │
                    └──────────┬───────────┘
                               │
           ┌───────────────────┼───────────────────┐
           │                   │                   │
           ▼                   ▼                   ▼
    ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
    │  Analyst    │     │  Reviewer   │     │  Writer     │
    │  Agent      │     │  Agent      │     │  Agent      │
    └─────────────┘     └─────────────┘     └─────────────┘
```

---

## Implementation Patterns

### 1. Agent Definition

```csharp
using Microsoft.Extensions.AI;

public interface IAgent
{
    string AgentId { get; }
    string Name { get; }
    string Role { get; }
    IReadOnlyList<string> Capabilities { get; }
    Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ProcessStreamingAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

public class AgentRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public string? FromAgentId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class AgentResponse
{
    public string ResponseId { get; set; } = Guid.NewGuid().ToString();
    public string AgentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public double? Confidence { get; set; }
}
```

### 2. Specialized Agent Implementation

```csharp
public class SpecializedAgent : IAgent
{
    private readonly IChatClient _chatClient;
    private readonly AgentConfiguration _config;
    private readonly ILogger<SpecializedAgent> _logger;

    public string AgentId { get; }
    public string Name { get; }
    public string Role { get; }
    public IReadOnlyList<string> Capabilities { get; }

    public SpecializedAgent(
        string agentId,
        string name,
        string role,
        IEnumerable<string> capabilities,
        IChatClient chatClient,
        AgentConfiguration config,
        ILogger<SpecializedAgent> logger)
    {
        AgentId = agentId;
        Name = name;
        Role = role;
        Capabilities = capabilities.ToList();
        _chatClient = chatClient;
        _config = config;
        _logger = logger;
    }

    public async Task<AgentResponse> ProcessAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Agent {AgentId} ({Name}) processing request {RequestId}",
            AgentId, Name, request.RequestId);

        var systemPrompt = BuildSystemPrompt(request);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, request.Message)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Temperature = _config.Temperature
        };

        try
        {
            var response = await _chatClient.CompleteAsync(messages, options, cancellationToken);

            return new AgentResponse
            {
                AgentId = AgentId,
                Content = response.Message.Text ?? string.Empty,
                Success = true,
                Metadata = new Dictionary<string, object>
                {
                    ["role"] = Role,
                    ["model"] = _config.ModelId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} failed to process request", AgentId);
            
            return new AgentResponse
            {
                AgentId = AgentId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async IAsyncEnumerable<string> ProcessStreamingAsync(
        AgentRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(request);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, request.Message)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Temperature = _config.Temperature
        };

        await foreach (var update in _chatClient.CompleteStreamingAsync(messages, options, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }

    private string BuildSystemPrompt(AgentRequest request)
    {
        var contextInfo = request.Context.Count > 0
            ? $"\n\nContext provided:\n{JsonSerializer.Serialize(request.Context)}"
            : string.Empty;

        return $"""
            You are {Name}, a specialized {Role}.
            
            Your capabilities include:
            {string.Join("\n- ", Capabilities.Select(c => c))}
            
            Respond professionally and focus on your area of expertise.
            If a task is outside your capabilities, clearly state that.
            {contextInfo}
            """;
    }
}
```

### 3. Agent Factory

```csharp
public interface IAgentFactory
{
    IAgent CreateAgent(string agentType);
    IEnumerable<IAgent> CreateAgentTeam(string teamType);
}

public class AgentFactory : IAgentFactory
{
    private readonly IChatClient _chatClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;

    public AgentFactory(
        IChatClient chatClient,
        ILoggerFactory loggerFactory,
        IConfiguration configuration)
    {
        _chatClient = chatClient;
        _loggerFactory = loggerFactory;
        _configuration = configuration;
    }

    public IAgent CreateAgent(string agentType)
    {
        var config = GetAgentConfig(agentType);
        var logger = _loggerFactory.CreateLogger<SpecializedAgent>();

        return agentType switch
        {
            "analyst" => new SpecializedAgent(
                Guid.NewGuid().ToString(),
                "Data Analyst",
                "Data Analysis Specialist",
                new[] { "data analysis", "statistical insights", "trend identification" },
                _chatClient,
                config,
                logger),

            "writer" => new SpecializedAgent(
                Guid.NewGuid().ToString(),
                "Technical Writer",
                "Documentation Specialist",
                new[] { "technical writing", "documentation", "content structuring" },
                _chatClient,
                config,
                logger),

            "reviewer" => new SpecializedAgent(
                Guid.NewGuid().ToString(),
                "Quality Reviewer",
                "Quality Assurance Specialist",
                new[] { "quality review", "error detection", "improvement suggestions" },
                _chatClient,
                config,
                logger),

            "researcher" => new SpecializedAgent(
                Guid.NewGuid().ToString(),
                "Research Assistant",
                "Research Specialist",
                new[] { "information gathering", "source analysis", "fact verification" },
                _chatClient,
                config,
                logger),

            _ => throw new ArgumentException($"Unknown agent type: {agentType}")
        };
    }

    public IEnumerable<IAgent> CreateAgentTeam(string teamType)
    {
        return teamType switch
        {
            "content" => new[]
            {
                CreateAgent("researcher"),
                CreateAgent("writer"),
                CreateAgent("reviewer")
            },

            "analysis" => new[]
            {
                CreateAgent("analyst"),
                CreateAgent("reviewer")
            },

            "full" => new[]
            {
                CreateAgent("researcher"),
                CreateAgent("analyst"),
                CreateAgent("writer"),
                CreateAgent("reviewer")
            },

            _ => throw new ArgumentException($"Unknown team type: {teamType}")
        };
    }

    private AgentConfiguration GetAgentConfig(string agentType)
    {
        return new AgentConfiguration
        {
            ModelId = _configuration[$"Agents:{agentType}:ModelId"] ?? "gpt-4o",
            MaxTokens = int.Parse(_configuration[$"Agents:{agentType}:MaxTokens"] ?? "2048"),
            Temperature = float.Parse(_configuration[$"Agents:{agentType}:Temperature"] ?? "0.7")
        };
    }
}
```

---

## Orchestration Patterns

### 1. Sequential Orchestration

```csharp
public class SequentialOrchestrator
{
    private readonly ILogger<SequentialOrchestrator> _logger;

    public SequentialOrchestrator(ILogger<SequentialOrchestrator> logger)
    {
        _logger = logger;
    }

    public async Task<OrchestrationResult> ExecuteAsync(
        IEnumerable<IAgent> agents,
        string initialInput,
        CancellationToken cancellationToken = default)
    {
        var result = new OrchestrationResult();
        var currentInput = initialInput;

        foreach (var agent in agents)
        {
            _logger.LogInformation(
                "Sequential execution: Agent {AgentId} ({Name})",
                agent.AgentId, agent.Name);

            var request = new AgentRequest
            {
                Message = currentInput,
                Context = new Dictionary<string, object>
                {
                    ["previous_responses"] = result.AgentResponses
                }
            };

            var response = await agent.ProcessAsync(request, cancellationToken);
            result.AgentResponses.Add(response);

            if (!response.Success)
            {
                result.Success = false;
                result.ErrorMessage = $"Agent {agent.Name} failed: {response.ErrorMessage}";
                break;
            }

            // Pass output to next agent
            currentInput = response.Content;
        }

        result.FinalOutput = currentInput;
        result.Success = result.AgentResponses.All(r => r.Success);

        return result;
    }
}

public class OrchestrationResult
{
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public string FinalOutput { get; set; } = string.Empty;
    public List<AgentResponse> AgentResponses { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### 2. Parallel Orchestration

```csharp
public class ParallelOrchestrator
{
    private readonly ILogger<ParallelOrchestrator> _logger;

    public ParallelOrchestrator(ILogger<ParallelOrchestrator> logger)
    {
        _logger = logger;
    }

    public async Task<OrchestrationResult> ExecuteAsync(
        IEnumerable<IAgent> agents,
        string input,
        CancellationToken cancellationToken = default)
    {
        var result = new OrchestrationResult();

        _logger.LogInformation("Starting parallel execution with {Count} agents", agents.Count());

        var tasks = agents.Select(agent =>
            ExecuteAgentAsync(agent, input, cancellationToken));

        var responses = await Task.WhenAll(tasks);
        result.AgentResponses.AddRange(responses);
        result.Success = responses.All(r => r.Success);

        // Aggregate results
        result.FinalOutput = AggregateResponses(responses);

        return result;
    }

    private async Task<AgentResponse> ExecuteAgentAsync(
        IAgent agent,
        string input,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AgentRequest { Message = input };
            return await agent.ProcessAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} failed", agent.AgentId);
            return new AgentResponse
            {
                AgentId = agent.AgentId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    protected virtual string AggregateResponses(AgentResponse[] responses)
    {
        var successfulResponses = responses
            .Where(r => r.Success)
            .Select(r => $"[{r.AgentId}]: {r.Content}");

        return string.Join("\n\n", successfulResponses);
    }
}
```

### 3. Debate Orchestration

```csharp
public class DebateOrchestrator
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<DebateOrchestrator> _logger;
    private readonly int _maxRounds;

    public DebateOrchestrator(
        IChatClient chatClient,
        ILogger<DebateOrchestrator> logger,
        int maxRounds = 3)
    {
        _chatClient = chatClient;
        _logger = logger;
        _maxRounds = maxRounds;
    }

    public async Task<DebateResult> ExecuteDebateAsync(
        IAgent[] debaters,
        string topic,
        CancellationToken cancellationToken = default)
    {
        var result = new DebateResult { Topic = topic };
        var debateHistory = new List<DebateRound>();

        for (int round = 1; round <= _maxRounds; round++)
        {
            _logger.LogInformation("Debate round {Round} of {MaxRounds}", round, _maxRounds);

            var roundResponses = new List<AgentResponse>();

            foreach (var debater in debaters)
            {
                var context = new Dictionary<string, object>
                {
                    ["round"] = round,
                    ["topic"] = topic,
                    ["previous_arguments"] = debateHistory
                };

                var prompt = round == 1
                    ? $"Present your initial position on: {topic}"
                    : $"Respond to the other arguments and refine your position on: {topic}\n\nPrevious arguments:\n{FormatDebateHistory(debateHistory)}";

                var request = new AgentRequest
                {
                    Message = prompt,
                    Context = context
                };

                var response = await debater.ProcessAsync(request, cancellationToken);
                roundResponses.Add(response);
            }

            debateHistory.Add(new DebateRound
            {
                RoundNumber = round,
                Arguments = roundResponses.ToDictionary(r => r.AgentId, r => r.Content)
            });

            // Check for consensus
            if (round > 1 && await CheckConsensusAsync(roundResponses, cancellationToken))
            {
                _logger.LogInformation("Consensus reached at round {Round}", round);
                result.ConsensusReached = true;
                break;
            }
        }

        result.Rounds = debateHistory;
        result.FinalSummary = await GenerateSummaryAsync(topic, debateHistory, cancellationToken);

        return result;
    }

    private async Task<bool> CheckConsensusAsync(
        List<AgentResponse> responses,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
            Analyze these responses and determine if there is consensus (agreement on key points).
            Respond with only "CONSENSUS" or "NO_CONSENSUS".
            
            Responses:
            {string.Join("\n\n", responses.Select(r => $"{r.AgentId}: {r.Content}"))}
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, prompt)
        };

        var response = await _chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);
        return response.Message.Text?.Contains("CONSENSUS", StringComparison.OrdinalIgnoreCase) == true
            && !response.Message.Text.Contains("NO_CONSENSUS", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GenerateSummaryAsync(
        string topic,
        List<DebateRound> rounds,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
            Summarize the key points and conclusions from this debate on: {topic}
            
            Debate history:
            {FormatDebateHistory(rounds)}
            
            Provide a balanced summary including:
            1. Main arguments presented
            2. Points of agreement
            3. Points of disagreement
            4. Conclusions or recommendations
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, prompt)
        };

        var response = await _chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);
        return response.Message.Text ?? string.Empty;
    }

    private string FormatDebateHistory(List<DebateRound> rounds)
    {
        var sb = new StringBuilder();
        foreach (var round in rounds)
        {
            sb.AppendLine($"Round {round.RoundNumber}:");
            foreach (var arg in round.Arguments)
            {
                sb.AppendLine($"  {arg.Key}: {arg.Value}");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

public class DebateResult
{
    public string Topic { get; set; } = string.Empty;
    public bool ConsensusReached { get; set; }
    public List<DebateRound> Rounds { get; set; } = new();
    public string FinalSummary { get; set; } = string.Empty;
}

public class DebateRound
{
    public int RoundNumber { get; set; }
    public Dictionary<string, string> Arguments { get; set; } = new();
}
```

### 4. Supervisor Orchestration

```csharp
public class SupervisorOrchestrator
{
    private readonly IAgent _supervisor;
    private readonly Dictionary<string, IAgent> _workers;
    private readonly ILogger<SupervisorOrchestrator> _logger;

    public SupervisorOrchestrator(
        IAgent supervisor,
        IEnumerable<IAgent> workers,
        ILogger<SupervisorOrchestrator> logger)
    {
        _supervisor = supervisor;
        _workers = workers.ToDictionary(w => w.AgentId);
        _logger = logger;
    }

    public async Task<OrchestrationResult> ExecuteAsync(
        string task,
        CancellationToken cancellationToken = default)
    {
        var result = new OrchestrationResult();

        // Supervisor plans the work
        var plan = await CreatePlanAsync(task, cancellationToken);
        _logger.LogInformation("Supervisor created plan with {Count} steps", plan.Steps.Count);

        // Execute plan
        foreach (var step in plan.Steps)
        {
            var worker = SelectWorker(step);
            if (worker == null)
            {
                _logger.LogWarning("No suitable worker found for step: {Step}", step.Description);
                continue;
            }

            _logger.LogInformation(
                "Assigning step '{Step}' to worker {WorkerId}",
                step.Description, worker.AgentId);

            var request = new AgentRequest
            {
                Message = step.Instructions,
                Context = new Dictionary<string, object>
                {
                    ["original_task"] = task,
                    ["step_number"] = step.Order,
                    ["previous_results"] = result.AgentResponses
                }
            };

            var response = await worker.ProcessAsync(request, cancellationToken);
            result.AgentResponses.Add(response);

            if (!response.Success)
            {
                // Ask supervisor to handle failure
                var recovery = await HandleFailureAsync(step, response, cancellationToken);
                if (!recovery.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Step '{step.Description}' failed and recovery unsuccessful";
                    break;
                }
            }
        }

        // Supervisor synthesizes final result
        result.FinalOutput = await SynthesizeResultsAsync(task, result.AgentResponses, cancellationToken);
        result.Success = result.AgentResponses.All(r => r.Success);

        return result;
    }

    private async Task<WorkPlan> CreatePlanAsync(
        string task,
        CancellationToken cancellationToken)
    {
        var workerInfo = string.Join("\n", _workers.Values.Select(w =>
            $"- {w.Name} ({w.AgentId}): {string.Join(", ", w.Capabilities)}"));

        var request = new AgentRequest
        {
            Message = $"""
                Create a work plan to accomplish this task: {task}
                
                Available workers:
                {workerInfo}
                
                Respond with a JSON plan in this format:
                {{
                    "steps": [
                        {{
                            "order": 1,
                            "description": "step description",
                            "assignedWorker": "worker_id or 'any'",
                            "requiredCapabilities": ["capability1"],
                            "instructions": "detailed instructions for the worker"
                        }}
                    ]
                }}
                """
        };

        var response = await _supervisor.ProcessAsync(request, cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<WorkPlan>(response.Content)
                ?? new WorkPlan();
        }
        catch
        {
            // Fallback to single-step plan
            return new WorkPlan
            {
                Steps = new List<WorkStep>
                {
                    new()
                    {
                        Order = 1,
                        Description = "Process task",
                        Instructions = task,
                        AssignedWorker = "any"
                    }
                }
            };
        }
    }

    private IAgent? SelectWorker(WorkStep step)
    {
        if (step.AssignedWorker != "any" && _workers.TryGetValue(step.AssignedWorker, out var assigned))
            return assigned;

        // Select by capabilities
        return _workers.Values.FirstOrDefault(w =>
            step.RequiredCapabilities.All(c =>
                w.Capabilities.Any(wc => wc.Contains(c, StringComparison.OrdinalIgnoreCase))));
    }

    private async Task<AgentResponse> HandleFailureAsync(
        WorkStep step,
        AgentResponse failedResponse,
        CancellationToken cancellationToken)
    {
        var request = new AgentRequest
        {
            Message = $"""
                A step in the plan failed. Please provide recovery instructions.
                
                Failed step: {step.Description}
                Error: {failedResponse.ErrorMessage}
                
                Options:
                1. Provide alternative instructions
                2. Skip this step
                3. Abort the task
                
                Respond with your decision and any alternative instructions.
                """
        };

        return await _supervisor.ProcessAsync(request, cancellationToken);
    }

    private async Task<string> SynthesizeResultsAsync(
        string originalTask,
        List<AgentResponse> responses,
        CancellationToken cancellationToken)
    {
        var resultsText = string.Join("\n\n", responses.Select(r =>
            $"Agent {r.AgentId}:\n{r.Content}"));

        var request = new AgentRequest
        {
            Message = $"""
                Synthesize the following results into a coherent final output for the task:
                {originalTask}
                
                Worker outputs:
                {resultsText}
                
                Provide a comprehensive final response that incorporates all relevant information.
                """
        };

        var response = await _supervisor.ProcessAsync(request, cancellationToken);
        return response.Content;
    }
}

public class WorkPlan
{
    public List<WorkStep> Steps { get; set; } = new();
}

public class WorkStep
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AssignedWorker { get; set; } = "any";
    public List<string> RequiredCapabilities { get; set; } = new();
    public string Instructions { get; set; } = string.Empty;
}
```

---

## Communication Patterns

### Agent Message Bus

```csharp
public interface IAgentMessageBus
{
    Task PublishAsync(AgentMessage message, CancellationToken cancellationToken = default);
    Task<AgentMessage?> ReceiveAsync(string agentId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AgentMessage> SubscribeAsync(string agentId, CancellationToken cancellationToken = default);
}

public class AgentMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string FromAgentId { get; set; } = string.Empty;
    public string ToAgentId { get; set; } = string.Empty;
    public string Type { get; set; } = "message";
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class InMemoryAgentMessageBus : IAgentMessageBus
{
    private readonly ConcurrentDictionary<string, Channel<AgentMessage>> _channels = new();
    private readonly ILogger<InMemoryAgentMessageBus> _logger;

    public InMemoryAgentMessageBus(ILogger<InMemoryAgentMessageBus> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync(
        AgentMessage message,
        CancellationToken cancellationToken = default)
    {
        var channel = _channels.GetOrAdd(
            message.ToAgentId,
            _ => Channel.CreateUnbounded<AgentMessage>());

        await channel.Writer.WriteAsync(message, cancellationToken);

        _logger.LogDebug(
            "Message {MessageId} published from {From} to {To}",
            message.MessageId, message.FromAgentId, message.ToAgentId);
    }

    public async Task<AgentMessage?> ReceiveAsync(
        string agentId,
        CancellationToken cancellationToken = default)
    {
        var channel = _channels.GetOrAdd(
            agentId,
            _ => Channel.CreateUnbounded<AgentMessage>());

        if (await channel.Reader.WaitToReadAsync(cancellationToken))
        {
            return await channel.Reader.ReadAsync(cancellationToken);
        }

        return null;
    }

    public async IAsyncEnumerable<AgentMessage> SubscribeAsync(
        string agentId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _channels.GetOrAdd(
            agentId,
            _ => Channel.CreateUnbounded<AgentMessage>());

        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return message;
        }
    }
}
```

---

## Web API Integration

### Multi-Agent Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class MultiAgentController : ControllerBase
{
    private readonly IAgentFactory _agentFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MultiAgentController> _logger;

    public MultiAgentController(
        IAgentFactory agentFactory,
        IServiceProvider serviceProvider,
        ILogger<MultiAgentController> logger)
    {
        _agentFactory = agentFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [HttpPost("sequential")]
    public async Task<IActionResult> ExecuteSequential(
        [FromBody] MultiAgentRequest request,
        CancellationToken cancellationToken)
    {
        var agents = request.AgentTypes.Select(t => _agentFactory.CreateAgent(t)).ToList();
        var orchestrator = new SequentialOrchestrator(
            _serviceProvider.GetRequiredService<ILogger<SequentialOrchestrator>>());

        var result = await orchestrator.ExecuteAsync(agents, request.Input, cancellationToken);

        return Ok(result);
    }

    [HttpPost("parallel")]
    public async Task<IActionResult> ExecuteParallel(
        [FromBody] MultiAgentRequest request,
        CancellationToken cancellationToken)
    {
        var agents = request.AgentTypes.Select(t => _agentFactory.CreateAgent(t)).ToList();
        var orchestrator = new ParallelOrchestrator(
            _serviceProvider.GetRequiredService<ILogger<ParallelOrchestrator>>());

        var result = await orchestrator.ExecuteAsync(agents, request.Input, cancellationToken);

        return Ok(result);
    }

    [HttpPost("debate")]
    public async Task<IActionResult> ExecuteDebate(
        [FromBody] DebateRequest request,
        CancellationToken cancellationToken)
    {
        var agents = request.DebaterTypes.Select(t => _agentFactory.CreateAgent(t)).ToArray();
        var chatClient = _serviceProvider.GetRequiredService<IChatClient>();
        var orchestrator = new DebateOrchestrator(
            chatClient,
            _serviceProvider.GetRequiredService<ILogger<DebateOrchestrator>>(),
            request.MaxRounds);

        var result = await orchestrator.ExecuteDebateAsync(agents, request.Topic, cancellationToken);

        return Ok(result);
    }

    [HttpPost("supervised")]
    public async Task<IActionResult> ExecuteSupervised(
        [FromBody] SupervisedRequest request,
        CancellationToken cancellationToken)
    {
        var supervisor = _agentFactory.CreateAgent(request.SupervisorType);
        var workers = request.WorkerTypes.Select(t => _agentFactory.CreateAgent(t));
        var orchestrator = new SupervisorOrchestrator(
            supervisor,
            workers,
            _serviceProvider.GetRequiredService<ILogger<SupervisorOrchestrator>>());

        var result = await orchestrator.ExecuteAsync(request.Task, cancellationToken);

        return Ok(result);
    }

    [HttpPost("team/{teamType}")]
    public async Task<IActionResult> ExecuteTeam(
        string teamType,
        [FromBody] TeamRequest request,
        CancellationToken cancellationToken)
    {
        var agents = _agentFactory.CreateAgentTeam(teamType).ToList();
        var orchestrator = new SequentialOrchestrator(
            _serviceProvider.GetRequiredService<ILogger<SequentialOrchestrator>>());

        var result = await orchestrator.ExecuteAsync(agents, request.Input, cancellationToken);

        return Ok(result);
    }
}

public record MultiAgentRequest(string Input, string[] AgentTypes);
public record DebateRequest(string Topic, string[] DebaterTypes, int MaxRounds = 3);
public record SupervisedRequest(string Task, string SupervisorType, string[] WorkerTypes);
public record TeamRequest(string Input);
```

---

## Testing Multi-Agent Systems

```csharp
public class MultiAgentTests
{
    [Fact]
    public async Task SequentialOrchestrator_ExecutesAgentsInOrder()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var callOrder = new List<string>();

        mockChatClient
            .Setup(x => x.CompleteAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IList<ChatMessage> messages, ChatOptions options, CancellationToken ct) =>
            {
                var userMessage = messages.Last(m => m.Role == ChatRole.User).Text;
                callOrder.Add(userMessage ?? "");
                return new ChatCompletion(new ChatMessage(ChatRole.Assistant, $"Processed: {userMessage}"));
            });

        var config = new AgentConfiguration();
        var logger = new Mock<ILogger<SpecializedAgent>>().Object;

        var agents = new[]
        {
            new SpecializedAgent("1", "Agent1", "Role1", new[] { "cap1" }, mockChatClient.Object, config, logger),
            new SpecializedAgent("2", "Agent2", "Role2", new[] { "cap2" }, mockChatClient.Object, config, logger)
        };

        var orchestrator = new SequentialOrchestrator(
            new Mock<ILogger<SequentialOrchestrator>>().Object);

        // Act
        var result = await orchestrator.ExecuteAsync(agents, "Initial input");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.AgentResponses.Count);
    }

    [Fact]
    public async Task ParallelOrchestrator_ExecutesAgentsConcurrently()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var startTimes = new ConcurrentBag<DateTimeOffset>();

        mockChatClient
            .Setup(x => x.CompleteAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (IList<ChatMessage> messages, ChatOptions options, CancellationToken ct) =>
            {
                startTimes.Add(DateTimeOffset.UtcNow);
                await Task.Delay(100, ct);
                return new ChatCompletion(new ChatMessage(ChatRole.Assistant, "Done"));
            });

        var config = new AgentConfiguration();
        var logger = new Mock<ILogger<SpecializedAgent>>().Object;

        var agents = Enumerable.Range(1, 3)
            .Select(i => new SpecializedAgent(
                i.ToString(), $"Agent{i}", $"Role{i}", 
                new[] { $"cap{i}" }, mockChatClient.Object, config, logger))
            .ToArray();

        var orchestrator = new ParallelOrchestrator(
            new Mock<ILogger<ParallelOrchestrator>>().Object);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await orchestrator.ExecuteAsync(agents, "Test input");
        stopwatch.Stop();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.AgentResponses.Count);
        // Should complete faster than sequential (3 * 100ms)
        Assert.True(stopwatch.ElapsedMilliseconds < 250);
    }
}
```

---

## Best Practices

### Agent Design

1. **Single Responsibility**: Each agent should have a clear, focused role
2. **Clear Capabilities**: Define explicit capabilities for proper task routing
3. **Graceful Degradation**: Agents should handle failures gracefully
4. **Stateless Design**: Prefer stateless agents for scalability

### Orchestration

1. **Choose the Right Pattern**: Match orchestration pattern to task requirements
2. **Timeout Handling**: Set appropriate timeouts for agent operations
3. **Error Recovery**: Implement fallback and retry strategies
4. **Monitoring**: Log agent interactions for debugging and optimization

### Communication

```csharp
// Good: Clear, structured messages
var message = new AgentMessage
{
    FromAgentId = analyst.AgentId,
    ToAgentId = writer.AgentId,
    Type = "analysis_complete",
    Content = analysisResult,
    Metadata = new Dictionary<string, object>
    {
        ["confidence"] = 0.95,
        ["topics"] = new[] { "finance", "quarterly" }
    }
};

// Avoid: Unstructured communication
var message = new AgentMessage
{
    Content = "Done with analysis, here's everything: ..."
};
```

---

## Related Templates

- [Agent Framework Basic](template-agent-framework-basic.md) - Single agent setup
- [Graph Workflows](template-agent-framework-workflows.md) - Workflow patterns
- [SK Multi-Agent](template-skg-multi-agent.md) - Alternative approach

---

## External References

- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions)
- [Multi-Agent Systems](https://en.wikipedia.org/wiki/Multi-agent_system)
- [Agent Communication Patterns](https://www.fipa.org/specs/fipa00061/)

