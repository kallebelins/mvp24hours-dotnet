# Templates de Implementação de IA para .NET

> **Propósito**: Esta documentação fornece templates abrangentes para agentes de IA implementarem capacidades de inteligência artificial em aplicações .NET usando Microsoft Semantic Kernel, Semantic Kernel Graph e Microsoft Agent Framework.

---

## Visão Geral

Esta seção fornece templates e padrões estruturados para implementar capacidades de IA em aplicações .NET. Cada abordagem oferece diferentes níveis de complexidade, flexibilidade e controle sobre fluxos de trabalho de IA.

### Como Agentes de IA Devem Usar Esta Documentação

1. **Analisar Requisitos**: Entender as capacidades de IA necessárias (chat, raciocínio, multi-agente, etc.)
2. **Selecionar Abordagem**: Usar a [Matriz de Decisão](#matriz-de-decisão) para escolher o framework apropriado
3. **Aplicar Templates**: Seguir os templates específicos para cada caso de uso
4. **Integrar com Mvp24Hours**: Combinar padrões de IA com templates de arquitetura existentes

---

## Abordagens Disponíveis

### 1. Semantic Kernel (Puro)

Microsoft Semantic Kernel é a base para orquestração de IA em .NET. Use esta abordagem para:

| Template | Complexidade | Caso de Uso | Documentação |
|----------|-------------|-------------|--------------|
| **Chat Completion** | Baixa | IA conversacional, chatbots | [template-sk-chat-completion.md](template-sk-chat-completion.md) |
| **Plugins & Functions** | Média | Integração de ferramentas, function calling | [template-sk-plugins.md](template-sk-plugins.md) |
| **RAG Básico** | Média | Q&A sobre documentos, busca de conhecimento | [template-sk-rag-basic.md](template-sk-rag-basic.md) |
| **Planners** | Alta | Decomposição de tarefas, planejamento automático | [template-sk-planners.md](template-sk-planners.md) |

**Quando Usar**:
- Cenários simples de integração de IA
- Fluxos padrão de chat/completion
- Aumentação de ferramentas via plugins
- Projetos que requerem complexidade mínima

### 2. Semantic Kernel Graph

Semantic Kernel Graph estende o SK com orquestração de fluxos baseada em grafos. Use esta abordagem para:

| Template | Complexidade | Caso de Uso | Documentação |
|----------|-------------|-------------|--------------|
| **Graph Executor** | Média | Orquestração de workflows | [template-skg-graph-executor.md](template-skg-graph-executor.md) |
| **ReAct Agent** | Alta | Loops de Raciocínio + Ação | [template-skg-react-agent.md](template-skg-react-agent.md) |
| **Chain of Thought** | Alta | Raciocínio passo a passo | [template-skg-chain-of-thought.md](template-skg-chain-of-thought.md) |
| **Chatbot com Memória** | Alta | Conversas contextuais | [template-skg-chatbot-memory.md](template-skg-chatbot-memory.md) |
| **Multi-Agent** | Muito Alta | Sistemas de agentes coordenados | [template-skg-multi-agent.md](template-skg-multi-agent.md) |
| **Document Pipeline** | Alta | Workflows de processamento de documentos | [template-skg-document-pipeline.md](template-skg-document-pipeline.md) |
| **Human-in-the-Loop** | Alta | Workflows de aprovação, supervisão | [template-skg-human-in-loop.md](template-skg-human-in-loop.md) |
| **Checkpointing** | Média | Persistência de estado, recuperação | [template-skg-checkpointing.md](template-skg-checkpointing.md) |
| **Streaming** | Média | Eventos em tempo real, monitoramento | [template-skg-streaming.md](template-skg-streaming.md) |
| **Observability** | Média | Métricas, visualização | [template-skg-observability.md](template-skg-observability.md) |

**Quando Usar**:
- Workflows de IA complexos com múltiplos passos
- Ramificação e roteamento condicional
- Gerenciamento e persistência de estado
- Sistemas de IA de produção com monitoramento

### 3. Microsoft Agent Framework

O Microsoft Agent Framework fornece abstrações de alto nível para construir agentes de IA. Use esta abordagem para:

| Template | Complexidade | Caso de Uso | Documentação |
|----------|-------------|-------------|--------------|
| **Agent Framework Básico** | Média | Criação simples de agentes | [template-agent-framework-basic.md](template-agent-framework-basic.md) |
| **Graph Workflows** | Alta | Agentes baseados em workflows | [template-agent-framework-workflows.md](template-agent-framework-workflows.md) |
| **Multi-Agent** | Muito Alta | Orquestração de agentes | [template-agent-framework-multi-agent.md](template-agent-framework-multi-agent.md) |
| **Middleware** | Alta | Processamento de request/response | [template-agent-framework-middleware.md](template-agent-framework-middleware.md) |

**Quando Usar**:
- Desenvolvimento de agentes de nível enterprise
- Integração com Azure OpenAI
- Alinhamento com ecossistema Microsoft
- Prototipação rápida de agentes

---

## Matriz de Decisão

### Por Caso de Uso

| Caso de Uso | Abordagem Recomendada | Template |
|-------------|----------------------|----------|
| Chatbot simples | Semantic Kernel | Chat Completion |
| Q&A sobre documentos | Semantic Kernel | RAG Básico |
| IA com ferramentas | Semantic Kernel | Plugins & Functions |
| Raciocínio complexo | Semantic Kernel Graph | Chain of Thought |
| Agente com ferramentas | Semantic Kernel Graph | ReAct Agent |
| Workflows multi-etapas | Semantic Kernel Graph | Graph Executor |
| Conversas persistentes | Semantic Kernel Graph | Chatbot com Memória |
| Processamento de documentos | Semantic Kernel Graph | Document Pipeline |
| Múltiplos agentes IA | Semantic Kernel Graph | Multi-Agent |
| Supervisão humana necessária | Semantic Kernel Graph | Human-in-the-Loop |
| Agentes enterprise | Agent Framework | Agent Framework Básico |

### Por Nível de Complexidade

| Complexidade | Abordagens | Cenários Típicos |
|--------------|-----------|------------------|
| **Baixa** | SK Chat Completion, SK Plugins | MVP, protótipos, integrações simples |
| **Média** | SK RAG, SK Planners, SKG Graph Executor | Aplicações de negócio, Q&A sobre documentos |
| **Alta** | SKG ReAct, SKG Chain of Thought, SKG Chatbot | Recursos avançados de IA, sistemas de raciocínio |
| **Muito Alta** | SKG Multi-Agent, Agent Framework | IA enterprise, sistemas autônomos |

### Por Requisitos de Produção

| Requisito | Abordagem Recomendada | Recursos Chave |
|-----------|----------------------|----------------|
| **Alta Disponibilidade** | Semantic Kernel Graph | Checkpointing, recuperação |
| **Monitoramento em Tempo Real** | Semantic Kernel Graph | Streaming, observabilidade |
| **Supervisão Humana** | Semantic Kernel Graph | Human-in-the-loop |
| **Persistência de Estado** | Semantic Kernel Graph | Checkpointing |
| **Integração Azure** | Agent Framework | Suporte nativo Azure OpenAI |

---

## Pacotes NuGet Necessários

### Semantic Kernel (Puro)

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.AzureOpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Plugins.Core" Version="1.*-*" />
  <PackageReference Include="Microsoft.SemanticKernel.Plugins.Memory" Version="1.*-*" />
</ItemGroup>
```

### Semantic Kernel Graph

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
</ItemGroup>
```

### Microsoft Agent Framework

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.*-*" />
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.*-*" />
  <PackageReference Include="Microsoft.Extensions.AI.AzureAIInference" Version="9.*-*" />
</ItemGroup>
```

---

## Padrões de Configuração

### Estrutura appsettings.json

```json
{
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "${OPENAI_API_KEY}",
      "ModelId": "gpt-4o",
      "EmbeddingModelId": "text-embedding-3-small"
    },
    "AzureOpenAI": {
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "ApiKey": "${AZURE_OPENAI_API_KEY}",
      "DeploymentName": "gpt-4o",
      "EmbeddingDeploymentName": "text-embedding-3-small"
    }
  },
  "SemanticKernelGraph": {
    "EnableCheckpointing": true,
    "EnableStreaming": true,
    "EnableMetrics": true,
    "MaxConcurrentAgents": 5,
    "DefaultTimeout": "00:05:00"
  }
}
```

### Variáveis de Ambiente

```bash
# OpenAI
OPENAI_API_KEY=sua-api-key-aqui

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://seu-recurso.openai.azure.com/
AZURE_OPENAI_API_KEY=sua-azure-api-key-aqui
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
```

---

## Integração com Mvp24Hours

### Padrão de Registro de Serviços

```csharp
public static class AIServiceExtensions
{
    public static IServiceCollection AddAIServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Registrar Semantic Kernel
        var kernelBuilder = Kernel.CreateBuilder();
        
        var provider = configuration["AI:Provider"];
        if (provider == "OpenAI")
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: configuration["AI:OpenAI:ModelId"]!,
                apiKey: configuration["AI:OpenAI:ApiKey"]!);
        }
        else if (provider == "AzureOpenAI")
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: configuration["AI:AzureOpenAI:DeploymentName"]!,
                endpoint: configuration["AI:AzureOpenAI:Endpoint"]!,
                apiKey: configuration["AI:AzureOpenAI:ApiKey"]!);
        }

        services.AddSingleton(kernelBuilder.Build());
        
        return services;
    }
}
```

### Padrão de Integração com Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class AIController : ControllerBase
{
    private readonly Kernel _kernel;
    
    public AIController(Kernel kernel)
    {
        _kernel = kernel;
    }
    
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        var result = await _kernel.InvokePromptAsync(request.Message);
        return Ok(new { response = result.GetValue<string>() });
    }
}
```

---

## Referência Rápida

### Criação do Kernel

```csharp
// OpenAI
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o", apiKey)
    .Build();

// Azure OpenAI
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .Build();
```

### Criação do Graph Executor

```csharp
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Extensions;

var builder = Kernel.CreateBuilder();
builder.AddGraphSupport();
var kernel = builder.Build();

var executor = new GraphExecutor("MeuGrafo", "Descrição do grafo");
executor.AddNode(node);
executor.SetStartNode(node.NodeId);

var result = await executor.ExecuteAsync(kernel, arguments);
```

### Criação de Function Node

```csharp
using SemanticKernel.Graph.Nodes;

var node = new FunctionGraphNode(
    kernel.CreateFunctionFromMethod(
        (string input) => $"Processado: {input}",
        functionName: "ProcessInput",
        description: "Processa a entrada"
    ),
    nodeId: "process-node"
).StoreResultAs("result");
```

---

## Documentação Relacionada

### Templates de Arquitetura
- [Clean Architecture](template-clean-architecture.md)
- [CQRS](template-cqrs.md)
- [Microservices](template-microservices.md)

### Documentação Principal
- [Matriz de Decisão](decision-matrix.md)
- [Padrões de Banco de Dados](database-patterns.md)
- [Padrões de Mensageria](messaging-patterns.md)

### Matriz de Decisão de IA
- [Matriz de Decisão de IA](ai-decision-matrix.md)

---

## Referências Externas

- **Semantic Kernel**: [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- **Semantic Kernel Graph**: [SemanticKernel.Graph](https://github.com/kallebelins/semantic-kernel-graph)
- **Microsoft Agent Framework**: [Agent Framework](https://github.com/microsoft/agent-framework)
- **Aprender Semantic Kernel**: [Microsoft Learn](https://learn.microsoft.com/semantic-kernel)

