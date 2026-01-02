# Template ReAct Agent - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para implementar agentes ReAct (Reasoning + Acting) usando grafos.

---

## Visão Geral

ReAct combina raciocínio e ação em um ciclo iterativo:
- **Reasoning**: IA analisa situação e planeja próximo passo
- **Acting**: Executa ação usando ferramentas disponíveis
- **Observing**: Processa resultado e decide próximo ciclo

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Agente com múltiplas ferramentas | ✅ Recomendado |
| Resolução de problemas complexos | ✅ Recomendado |
| Tarefas que requerem raciocínio | ✅ Recomendado |
| Q&A simples | ⚠️ Use Chat Completion |

---

## Pacotes NuGet

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
</ItemGroup>
```

---

## Padrão ReAct com Grafos

```csharp
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;

public class ReActAgent
{
    private readonly GraphExecutor _graph;
    private readonly Kernel _kernel;

    public ReActAgent(Kernel kernel)
    {
        _kernel = kernel;
        _graph = BuildReActGraph();
    }

    private GraphExecutor BuildReActGraph()
    {
        var graph = new GraphExecutor("ReActAgent", "Agente ReAct");

        // Nó de Raciocínio
        var reasonNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Analise a tarefa e o histórico de ações.
                
                Tarefa: {{$task}}
                Histórico: {{$history}}
                Ferramentas: {{$tools}}
                
                Responda no formato:
                THOUGHT: [seu raciocínio]
                ACTION: [nome_ferramenta] ou FINISH
                INPUT: [entrada para ferramenta]
                """),
            nodeId: "reason"
        ).StoreResultAs("reasoning");

        // Nó de Ação (ActionGraphNode com auto-discovery de tools)
        var actionNode = new ActionGraphNode("action", _kernel)
            .WithToolDiscovery();

        // Nó de Observação
        var observeNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (string result, string history) => $"{history}\nObservation: {result}",
                "UpdateHistory"
            ),
            nodeId: "observe"
        ).StoreResultAs("history");

        graph.AddNode(reasonNode);
        graph.AddNode(actionNode);
        graph.AddNode(observeNode);

        // Conexões
        graph.Connect("reason", "action", ctx => 
            !ctx.State.GetValue<string>("reasoning").Contains("FINISH"));
        graph.Connect("reason", GraphExecutor.EndNodeId, ctx =>
            ctx.State.GetValue<string>("reasoning").Contains("FINISH"));
        graph.Connect("action", "observe");
        graph.Connect("observe", "reason");

        graph.SetStartNode("reason");
        return graph;
    }

    public async Task<string> ExecuteAsync(string task, IEnumerable<AITool> tools)
    {
        var arguments = new KernelArguments
        {
            ["task"] = task,
            ["tools"] = string.Join(", ", tools.Select(t => t.Metadata.Name)),
            ["history"] = ""
        };

        var result = await _graph.ExecuteAsync(_kernel, arguments);
        return result.State.GetValue<string>("reasoning");
    }
}
```

---

## Boas Práticas

1. **Limite de Iterações**: Defina máximo de ciclos para evitar loops
2. **Ferramentas Claras**: Descrições detalhadas ajudam a IA escolher
3. **Histórico Conciso**: Mantenha histórico resumido para contexto
4. **Fallback**: Implemente fallback quando ferramenta falhar

---

## Templates Relacionados

- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos
- [Chain of Thought](template-skg-chain-of-thought.md) - Raciocínio estruturado
- [Plugins](template-sk-plugins.md) - Criação de ferramentas

