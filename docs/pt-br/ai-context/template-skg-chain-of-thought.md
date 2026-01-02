# Template Chain of Thought - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para implementar raciocínio estruturado passo a passo usando grafos.

---

## Visão Geral

Chain of Thought (CoT) permite raciocínio explícito e verificável:
- Decomposição de problemas em etapas
- Validação de cada etapa de raciocínio
- Scoring de confiança
- Backtracking quando necessário

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Problemas matemáticos | ✅ Recomendado |
| Análise lógica | ✅ Recomendado |
| Decisões complexas | ✅ Recomendado |
| Respostas simples | ⚠️ Use Chat Completion |

---

## Implementação

```csharp
using SemanticKernel.Graph.Nodes;

public class ChainOfThoughtService
{
    private readonly Kernel _kernel;

    public async Task<CoTResult> ReasonAsync(string problem)
    {
        var graph = new GraphExecutor("CoT", "Chain of Thought");

        // Nó de decomposição
        var decomposeNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Decomponha o problema em etapas de raciocínio:
                
                Problema: {{$problem}}
                
                Liste as etapas necessárias (uma por linha):
                """),
            nodeId: "decompose"
        ).StoreResultAs("steps");

        // Nó de raciocínio por etapa
        var reasonStepNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Execute esta etapa de raciocínio:
                
                Etapa: {{$current_step}}
                Contexto anterior: {{$context}}
                
                Raciocínio:
                """),
            nodeId: "reason_step"
        ).StoreResultAs("step_result");

        // Nó de validação
        var validateNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Valide o raciocínio (responda VALID ou INVALID com explicação):
                
                Raciocínio: {{$step_result}}
                
                Validação:
                """),
            nodeId: "validate"
        ).StoreResultAs("validation");

        // Nó de síntese
        var synthesizeNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Sintetize a resposta final baseado no raciocínio:
                
                Problema: {{$problem}}
                Raciocínio completo: {{$context}}
                
                Resposta final:
                """),
            nodeId: "synthesize"
        ).StoreResultAs("final_answer");

        graph.AddNode(decomposeNode);
        graph.AddNode(reasonStepNode);
        graph.AddNode(validateNode);
        graph.AddNode(synthesizeNode);

        graph.Connect("decompose", "reason_step");
        graph.Connect("reason_step", "validate");
        graph.Connect("validate", "synthesize", ctx =>
            ctx.State.GetValue<string>("validation").Contains("VALID"));
        graph.Connect("validate", "reason_step", ctx =>
            ctx.State.GetValue<string>("validation").Contains("INVALID"));

        graph.SetStartNode("decompose");

        var result = await graph.ExecuteAsync(_kernel, new KernelArguments
        {
            ["problem"] = problem,
            ["context"] = ""
        });

        return new CoTResult
        {
            Answer = result.State.GetValue<string>("final_answer"),
            ReasoningSteps = result.State.GetValue<List<string>>("all_steps")
        };
    }
}

public class CoTResult
{
    public string Answer { get; set; } = string.Empty;
    public List<string> ReasoningSteps { get; set; } = new();
    public double Confidence { get; set; }
}
```

---

## Boas Práticas

1. **Etapas Verificáveis**: Cada etapa deve ser independentemente verificável
2. **Confidence Scoring**: Atribua confiança a cada etapa
3. **Backtracking**: Permita voltar quando validação falhar
4. **Limite de Profundidade**: Evite raciocínio infinito

---

## Templates Relacionados

- [ReAct Agent](template-skg-react-agent.md) - Raciocínio com ações
- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos

