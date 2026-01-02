# Template de Planners - Semantic Kernel

> **Propósito**: Este template fornece padrões e diretrizes de implementação para usar planners no Microsoft Semantic Kernel para decomposição de tarefas e planejamento automático.

---

## Visão Geral

Planners permitem que a IA decomponha automaticamente tarefas complexas em etapas e as execute. Este template cobre:
- Function Calling Planner (recomendado)
- Handlebars Planner
- Estratégias de planejamento personalizadas
- Execução e monitoramento de planos

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Automação de tarefas multi-etapas | ✅ Recomendado |
| Criação dinâmica de workflows | ✅ Recomendado |
| IA orientada a objetivos | ✅ Recomendado |
| Q&A simples | ⚠️ Use Chat Completion |
| Workflows fixos | ⚠️ Use Graph Executor |

---

## Pacotes NuGet Necessários

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Planners.Handlebars" Version="1.*-*" />
</ItemGroup>
```

---

## Comparação de Planners

| Planner | Melhor Para | Complexidade | Confiabilidade |
|---------|-------------|--------------|----------------|
| Function Calling | Maioria dos cenários | Baixa | Alta |
| Handlebars | Workflows baseados em template | Média | Média |
| Personalizado | Requisitos específicos | Alta | Variável |

---

## Function Calling Planner (Recomendado)

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

public class FunctionCallingPlannerService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;

    public async Task<string> ExecuteGoalAsync(string goal, CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("""
            Você é um assistente que realiza objetivos usando as ferramentas disponíveis.
            Divida tarefas complexas em etapas e execute-as uma por uma.
            Explique seu raciocínio e as etapas que está tomando.
            """);
        chatHistory.AddUserMessage(goal);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory, settings, _kernel, cancellationToken);

        return response.Content ?? string.Empty;
    }
}
```

---

## Planner Stepwise Personalizado

```csharp
public class StepwisePlannerService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;

    public async Task<PlanExecutionResult> ExecuteWithStepsAsync(
        string goal, int maxSteps = 10, CancellationToken cancellationToken = default)
    {
        var result = new PlanExecutionResult { Goal = goal };
        var chatHistory = new ChatHistory();
        
        chatHistory.AddSystemMessage(GetPlannerSystemPrompt());
        chatHistory.AddUserMessage($"Objetivo: {goal}");

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        for (int step = 0; step < maxSteps; step++)
        {
            var response = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory, settings, _kernel, cancellationToken);

            result.Steps.Add(new PlanStep
            {
                StepNumber = step + 1,
                Action = response.Content ?? string.Empty
            });

            if (IsGoalComplete(response.Content))
            {
                result.IsComplete = true;
                result.FinalAnswer = ExtractFinalAnswer(response.Content);
                break;
            }

            chatHistory.AddAssistantMessage(response.Content ?? string.Empty);
            chatHistory.AddUserMessage("Continue com a próxima etapa.");
        }

        return result;
    }

    private string GetPlannerSystemPrompt()
    {
        return """
            Você é um assistente orientado a objetivos que divide tarefas complexas em etapas.
            
            Para cada etapa:
            1. Explique o que vai fazer
            2. Use as funções disponíveis para realizar
            3. Relate o resultado
            
            Quando o objetivo estiver completo, comece com "OBJETIVO COMPLETO:" seguido da resposta final.
            """;
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
}
```

---

## Plugins para Planejamento

### Plugin de Tarefas

```csharp
public class TaskPlugin
{
    private readonly List<TaskItem> _tasks = new();

    [KernelFunction("CreateTask")]
    [Description("Cria uma nova tarefa")]
    public string CreateTask(
        [Description("Título da tarefa")] string title,
        [Description("Descrição da tarefa")] string description)
    {
        var task = new TaskItem { Title = title, Description = description };
        _tasks.Add(task);
        return $"Tarefa '{title}' criada com sucesso";
    }

    [KernelFunction("ListTasks")]
    [Description("Lista todas as tarefas")]
    public string ListTasks()
    {
        if (!_tasks.Any()) return "Nenhuma tarefa encontrada.";
        return string.Join("\n", _tasks.Select(t => $"- [{t.Status}] {t.Title}"));
    }

    [KernelFunction("CompleteTask")]
    [Description("Marca uma tarefa como concluída")]
    public string CompleteTask([Description("Título da tarefa")] string title)
    {
        var task = _tasks.FirstOrDefault(t => t.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        if (task == null) return $"Tarefa '{title}' não encontrada.";
        task.Status = "Concluída";
        return $"Tarefa '{task.Title}' marcada como concluída.";
    }
}
```

---

## Integração com Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class PlannerController : ControllerBase
{
    private readonly FunctionCallingPlannerService _plannerService;

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteGoal([FromBody] GoalRequest request, CancellationToken cancellationToken)
    {
        var result = await _plannerService.ExecuteGoalAsync(request.Goal, cancellationToken);
        return Ok(new { result });
    }

    [HttpPost("execute/stream")]
    public async Task StreamExecuteGoal([FromBody] GoalRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        await foreach (var chunk in _plannerService.ExecuteGoalStreamingAsync(request.Goal, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
```

---

## Boas Práticas

1. **Objetivos Específicos**: Objetivos claros e mensuráveis geram melhores planos
2. **Forneça Contexto**: Inclua restrições e requisitos relevantes
3. **Defina Limites**: Especifique quais ferramentas/ações são permitidas
4. **Funções Atômicas**: Cada função deve fazer uma coisa bem
5. **Descrições Claras**: IA precisa entender o que funções fazem

---

## Templates Relacionados

- [Chat Completion](template-sk-chat-completion.md) - Funcionalidade básica de chat
- [Plugins & Functions](template-sk-plugins.md) - Integração de ferramentas
- [Graph Executor](template-skg-graph-executor.md) - Execução de workflows fixos
- [ReAct Agent](template-skg-react-agent.md) - Raciocínio com ferramentas

---

## Referências Externas

- [Semantic Kernel Planners](https://learn.microsoft.com/semantic-kernel/agents/planners)
- [Function Calling](https://learn.microsoft.com/semantic-kernel/agents/plugins/using-ai-functions)

