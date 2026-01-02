# Template Graph Executor - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para criar e executar workflows baseados em grafos usando Semantic Kernel Graph.

---

## Visão Geral

Graph Executor é o motor de orquestração central para aplicações de IA baseadas em workflows. Cobre:
- Estrutura de grafos e tipos de nós
- Execução sequencial e paralela
- Roteamento condicional
- Gerenciamento de estado

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Workflows multi-etapas | ✅ Recomendado |
| Ramificação condicional | ✅ Recomendado |
| Processamento paralelo | ✅ Recomendado |
| Q&A simples | ⚠️ Use Chat Completion |

---

## Pacotes NuGet Necessários

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
</ItemGroup>
```

---

## Conceitos Principais

| Componente | Descrição |
|------------|-----------|
| **GraphExecutor** | Motor de execução que orquestra workflow |
| **FunctionGraphNode** | Executa instâncias de KernelFunction |
| **ConditionalEdge** | Roteia execução baseado em condições |
| **GraphState** | Gerencia estado entre nós |

---

## Padrões de Implementação

### Configuração Básica do Grafo

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Extensions;
using SemanticKernel.Graph.Nodes;

// 1. Criar kernel com suporte a grafos
var builder = Kernel.CreateBuilder();
builder.AddGraphSupport();
builder.AddOpenAIChatCompletion("gpt-4o", apiKey);
var kernel = builder.Build();

// 2. Criar executor de grafo
var graph = new GraphExecutor("MeuWorkflow", "Exemplo de workflow");

// 3. Criar nós
var node1 = new FunctionGraphNode(
    kernel.CreateFunctionFromMethod(
        (string input) => $"Processado: {input}",
        functionName: "ProcessInput"
    ),
    nodeId: "process-node"
).StoreResultAs("result");

// 4. Adicionar nós ao grafo
graph.AddNode(node1);
graph.SetStartNode("process-node");

// 5. Executar grafo
var arguments = new KernelArguments { ["input"] = "dados" };
var result = await graph.ExecuteAsync(kernel, arguments);
```

### Conexão de Nós

```csharp
// Conexão simples
graph.Connect("node1", "node2");

// Conexão condicional
graph.Connect("analysis", "approve", condition: ctx =>
    ctx.State.GetValue<double>("score") > 0.8);

graph.Connect("analysis", "reject", condition: ctx =>
    ctx.State.GetValue<double>("score") <= 0.8);
```

### Execução Paralela

```csharp
var parallelNode = new ParallelGraphNode("parallel-analysis")
    .AddBranch(sentimentNode)
    .AddBranch(categoryNode)
    .AddBranch(summaryNode);

graph.AddNode(parallelNode);
```

---

## Boas Práticas

1. **Nós Atômicos**: Cada nó deve fazer uma coisa bem
2. **Nomenclatura Clara**: Use IDs descritivos para nós
3. **Armazenamento de Resultados**: Use `StoreResultAs()` para persistir resultados
4. **Tratamento de Erros**: Implemente fallback nodes

---

## Templates Relacionados

- [Chat Completion](template-sk-chat-completion.md) - Chat básico
- [ReAct Agent](template-skg-react-agent.md) - Raciocínio com ações
- [Multi-Agent](template-skg-multi-agent.md) - Coordenação de agentes

---

## Referências Externas

- [SemanticKernel.Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [Documentação](D:\Github\semantic-kernel-graph-pkg\semantic-kernel-graph-docs\docs\first-graph-5-minutes.md)

