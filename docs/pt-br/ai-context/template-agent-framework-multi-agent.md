# Template Multi-Agent - Microsoft Agent Framework

> **Propósito**: Este template fornece padrões para sistemas multi-agente com coordenação e orquestração.

---

## Visão Geral

Sistemas multi-agente permitem:
- Múltiplos agentes especializados trabalhando juntos
- Coordenação e orquestração de agentes
- Distribuição de trabalho
- Agregação de resultados

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Tarefas complexas com diversas especialidades | ✅ Recomendado |
| Processamento paralelo de subtarefas | ✅ Recomendado |
| Cenários de debate/consenso | ✅ Recomendado |
| Tarefas simples de propósito único | ⚠️ Use Agente Básico |

---

## Implementação

### Definição de Agente

```csharp
public interface IAgent
{
    string AgentId { get; }
    string Name { get; }
    string Role { get; }
    IReadOnlyList<string> Capabilities { get; }
    Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

public class AgentRequest
{
    public string Message { get; set; } = string.Empty;
    public string? FromAgentId { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
}

public class AgentResponse
{
    public string AgentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
```

### Agente Especializado

```csharp
public class SpecializedAgent : IAgent
{
    private readonly IChatClient _chatClient;
    private readonly string _systemPrompt;

    public string AgentId { get; }
    public string Name { get; }
    public string Role { get; }
    public IReadOnlyList<string> Capabilities { get; }

    public async Task<AgentResponse> ProcessAsync(
        AgentRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _systemPrompt),
            new(ChatRole.User, request.Message)
        };

        var response = await _chatClient.CompleteAsync(messages, cancellationToken: cancellationToken);

        return new AgentResponse
        {
            AgentId = AgentId,
            Content = response.Message.Text ?? string.Empty,
            Success = true
        };
    }
}
```

### Orquestrador Sequencial

```csharp
public class SequentialOrchestrator
{
    public async Task<OrchestrationResult> ExecuteAsync(
        IEnumerable<IAgent> agents, string initialInput, CancellationToken cancellationToken = default)
    {
        var result = new OrchestrationResult();
        var currentInput = initialInput;

        foreach (var agent in agents)
        {
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
                result.ErrorMessage = $"Agente {agent.Name} falhou: {response.ErrorMessage}";
                break;
            }

            currentInput = response.Content;
        }

        result.FinalOutput = currentInput;
        result.Success = result.AgentResponses.All(r => r.Success);
        return result;
    }
}
```

### Orquestrador Paralelo

```csharp
public class ParallelOrchestrator
{
    public async Task<OrchestrationResult> ExecuteAsync(
        IEnumerable<IAgent> agents, string input, CancellationToken cancellationToken = default)
    {
        var result = new OrchestrationResult();

        var tasks = agents.Select(agent =>
            agent.ProcessAsync(new AgentRequest { Message = input }, cancellationToken));

        var responses = await Task.WhenAll(tasks);
        result.AgentResponses.AddRange(responses);
        result.Success = responses.All(r => r.Success);
        result.FinalOutput = AggregateResponses(responses);

        return result;
    }

    private string AggregateResponses(AgentResponse[] responses)
    {
        return string.Join("\n\n---\n\n", responses.Where(r => r.Success).Select(r => r.Content));
    }
}
```

### Orquestrador de Debate

```csharp
public class DebateOrchestrator
{
    private readonly IChatClient _chatClient;
    private readonly int _maxRounds;

    public async Task<DebateResult> ExecuteDebateAsync(
        IAgent[] debaters, string topic, CancellationToken cancellationToken = default)
    {
        var result = new DebateResult { Topic = topic };
        var debateHistory = new List<string>();

        for (int round = 0; round < _maxRounds; round++)
        {
            foreach (var debater in debaters)
            {
                var prompt = round == 0
                    ? $"Apresente sua posição sobre: {topic}"
                    : $"Responda aos argumentos sobre: {topic}\n\nHistórico:\n{string.Join("\n", debateHistory)}";

                var response = await debater.ProcessAsync(new AgentRequest { Message = prompt }, cancellationToken);
                debateHistory.Add($"[{debater.Name}]: {response.Content}");
            }

            // Verificar consenso
            if (round > 0 && await CheckConsensusAsync(debateHistory, cancellationToken))
            {
                result.ConsensusReached = true;
                break;
            }
        }

        result.FinalSummary = await SynthesizeDebateAsync(topic, debateHistory, cancellationToken);
        return result;
    }
}

public class OrchestrationResult
{
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public string FinalOutput { get; set; } = string.Empty;
    public List<AgentResponse> AgentResponses { get; set; } = new();
}

public class DebateResult
{
    public string Topic { get; set; } = string.Empty;
    public bool ConsensusReached { get; set; }
    public string FinalSummary { get; set; } = string.Empty;
}
```

---

## Integração com Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class MultiAgentController : ControllerBase
{
    private readonly IAgentFactory _agentFactory;

    [HttpPost("sequential")]
    public async Task<IActionResult> ExecuteSequential(
        [FromBody] MultiAgentRequest request, CancellationToken cancellationToken)
    {
        var agents = request.AgentTypes.Select(t => _agentFactory.CreateAgent(t)).ToList();
        var orchestrator = new SequentialOrchestrator();
        var result = await orchestrator.ExecuteAsync(agents, request.Input, cancellationToken);
        return Ok(result);
    }

    [HttpPost("parallel")]
    public async Task<IActionResult> ExecuteParallel(
        [FromBody] MultiAgentRequest request, CancellationToken cancellationToken)
    {
        var agents = request.AgentTypes.Select(t => _agentFactory.CreateAgent(t)).ToList();
        var orchestrator = new ParallelOrchestrator();
        var result = await orchestrator.ExecuteAsync(agents, request.Input, cancellationToken);
        return Ok(result);
    }
}
```

---

## Boas Práticas

1. **Agentes Especializados**: Cada agente deve ter foco claro
2. **Estratégia Apropriada**: Escolha estratégia baseado na tarefa
3. **Timeout**: Defina timeouts para cada agente
4. **Fallback**: Implemente fallback quando agente falhar

---

## Templates Relacionados

- [Agent Framework Básico](template-agent-framework-basic.md) - Configuração de agente único
- [Graph Workflows](template-agent-framework-workflows.md) - Padrões de workflow

