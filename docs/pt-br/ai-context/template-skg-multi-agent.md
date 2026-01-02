# Template Multi-Agent - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para coordenar múltiplos agentes de IA especializados usando grafos.

---

## Visão Geral

Coordenação multi-agente permite:
- Múltiplos agentes especializados trabalhando juntos
- Distribuição de trabalho
- Agregação de resultados
- Monitoramento de saúde

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Tarefas complexas multidisciplinares | ✅ Recomendado |
| Processamento paralelo | ✅ Recomendado |
| Revisão e validação | ✅ Recomendado |
| Tarefas simples | ⚠️ Use agente único |

---

## Implementação

```csharp
using SemanticKernel.Graph.Agents;

public class MultiAgentCoordinator
{
    private readonly Dictionary<string, AgentInstance> _agents = new();
    private readonly Kernel _kernel;

    public MultiAgentCoordinator(Kernel kernel)
    {
        _kernel = kernel;
        RegisterAgents();
    }

    private void RegisterAgents()
    {
        _agents["analyst"] = new AgentInstance
        {
            Id = "analyst",
            Name = "Analista de Dados",
            Capabilities = new[] { "análise", "estatísticas", "insights" },
            SystemPrompt = "Você é um analista de dados especializado."
        };

        _agents["writer"] = new AgentInstance
        {
            Id = "writer",
            Name = "Redator Técnico",
            Capabilities = new[] { "escrita", "documentação", "comunicação" },
            SystemPrompt = "Você é um redator técnico especializado."
        };

        _agents["reviewer"] = new AgentInstance
        {
            Id = "reviewer",
            Name = "Revisor de Qualidade",
            Capabilities = new[] { "revisão", "qualidade", "validação" },
            SystemPrompt = "Você é um revisor de qualidade especializado."
        };
    }

    public async Task<CoordinationResult> ExecuteTaskAsync(string task, string strategy = "sequential")
    {
        return strategy switch
        {
            "sequential" => await ExecuteSequentialAsync(task),
            "parallel" => await ExecuteParallelAsync(task),
            "debate" => await ExecuteDebateAsync(task),
            _ => throw new ArgumentException($"Estratégia desconhecida: {strategy}")
        };
    }

    private async Task<CoordinationResult> ExecuteSequentialAsync(string task)
    {
        var result = new CoordinationResult { Task = task };
        var context = task;

        foreach (var agent in _agents.Values)
        {
            var response = await ExecuteAgentAsync(agent, context);
            result.AgentOutputs[agent.Id] = response;
            context = response; // Passa resultado para próximo agente
        }

        result.FinalOutput = context;
        return result;
    }

    private async Task<CoordinationResult> ExecuteParallelAsync(string task)
    {
        var result = new CoordinationResult { Task = task };
        
        var tasks = _agents.Values.Select(async agent =>
        {
            var response = await ExecuteAgentAsync(agent, task);
            return (agent.Id, response);
        });

        var responses = await Task.WhenAll(tasks);

        foreach (var (id, response) in responses)
            result.AgentOutputs[id] = response;

        result.FinalOutput = AggregateResponses(result.AgentOutputs.Values);
        return result;
    }

    private async Task<CoordinationResult> ExecuteDebateAsync(string task, int rounds = 3)
    {
        var result = new CoordinationResult { Task = task };
        var debateHistory = new List<string>();

        for (int round = 0; round < rounds; round++)
        {
            foreach (var agent in _agents.Values)
            {
                var prompt = round == 0
                    ? $"Apresente sua posição sobre: {task}"
                    : $"Responda aos argumentos anteriores sobre: {task}\n\nHistórico:\n{string.Join("\n", debateHistory)}";

                var response = await ExecuteAgentAsync(agent, prompt);
                debateHistory.Add($"[{agent.Name}]: {response}");
                result.AgentOutputs[$"{agent.Id}_round{round}"] = response;
            }
        }

        result.FinalOutput = await SynthesizeDebateAsync(task, debateHistory);
        return result;
    }

    private async Task<string> ExecuteAgentAsync(AgentInstance agent, string input)
    {
        var prompt = $"""
            {agent.SystemPrompt}
            
            Tarefa: {input}
            
            Resposta:
            """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.GetValue<string>() ?? string.Empty;
    }

    private string AggregateResponses(IEnumerable<string> responses)
    {
        return string.Join("\n\n---\n\n", responses);
    }

    private async Task<string> SynthesizeDebateAsync(string task, List<string> history)
    {
        var prompt = $"""
            Sintetize os pontos principais do debate sobre: {task}
            
            Debate:
            {string.Join("\n", history)}
            
            Síntese final (incluindo pontos de consenso e divergência):
            """;

        var result = await _kernel.InvokePromptAsync(prompt);
        return result.GetValue<string>() ?? string.Empty;
    }
}

public class AgentInstance
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public string SystemPrompt { get; set; } = string.Empty;
}

public class CoordinationResult
{
    public string Task { get; set; } = string.Empty;
    public Dictionary<string, string> AgentOutputs { get; set; } = new();
    public string FinalOutput { get; set; } = string.Empty;
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

- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos
- [ReAct Agent](template-skg-react-agent.md) - Agente único com ferramentas

