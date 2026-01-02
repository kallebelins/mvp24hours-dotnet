# AI Implementation Templates for .NET

> **Purpose**: This documentation provides comprehensive templates for AI agents to implement artificial intelligence capabilities in .NET applications using Microsoft Semantic Kernel, Semantic Kernel Graph, and Microsoft Agent Framework.

---

## Overview

This section provides structured templates and patterns for implementing AI capabilities in .NET applications. Each approach offers different levels of complexity, flexibility, and control over AI workflows.

### How AI Agents Should Use This Documentation

1. **Analyze Requirements**: Understand the AI capabilities needed (chat, reasoning, multi-agent, etc.)
2. **Select Approach**: Use the [Decision Matrix](#decision-matrix) to choose the appropriate framework
3. **Apply Templates**: Follow the specific templates for each use case
4. **Integrate with Mvp24Hours**: Combine AI patterns with existing architecture templates

---

## Available Approaches

### 1. Semantic Kernel (Pure)

Microsoft Semantic Kernel is the foundation for AI orchestration in .NET. Use this approach for:

| Template | Complexity | Use Case | Documentation |
|----------|-----------|----------|---------------|
| **Chat Completion** | Low | Conversational AI, chatbots | [template-sk-chat-completion.md](template-sk-chat-completion.md) |
| **Plugins & Functions** | Medium | Tool integration, function calling | [template-sk-plugins.md](template-sk-plugins.md) |
| **RAG Basic** | Medium | Document Q&A, knowledge retrieval | [template-sk-rag-basic.md](template-sk-rag-basic.md) |
| **Planners** | High | Task decomposition, auto-planning | [template-sk-planners.md](template-sk-planners.md) |

**When to Use**:
- Simple AI integration scenarios
- Standard chat/completion workflows
- Plugin-based tool augmentation
- Projects requiring minimal complexity

### 2. Semantic Kernel Graph

Semantic Kernel Graph extends SK with graph-based workflow orchestration. Use this approach for:

| Template | Complexity | Use Case | Documentation |
|----------|-----------|----------|---------------|
| **Graph Executor** | Medium | Workflow orchestration | [template-skg-graph-executor.md](template-skg-graph-executor.md) |
| **ReAct Agent** | High | Reasoning + Acting loops | [template-skg-react-agent.md](template-skg-react-agent.md) |
| **Chain of Thought** | High | Step-by-step reasoning | [template-skg-chain-of-thought.md](template-skg-chain-of-thought.md) |
| **Chatbot with Memory** | High | Contextual conversations | [template-skg-chatbot-memory.md](template-skg-chatbot-memory.md) |
| **Multi-Agent** | Very High | Coordinated agent systems | [template-skg-multi-agent.md](template-skg-multi-agent.md) |
| **Document Pipeline** | High | Document processing workflows | [template-skg-document-pipeline.md](template-skg-document-pipeline.md) |
| **Human-in-the-Loop** | High | Approval workflows, oversight | [template-skg-human-in-loop.md](template-skg-human-in-loop.md) |
| **Checkpointing** | Medium | State persistence, recovery | [template-skg-checkpointing.md](template-skg-checkpointing.md) |
| **Streaming** | Medium | Real-time events, monitoring | [template-skg-streaming.md](template-skg-streaming.md) |
| **Observability** | Medium | Metrics, visualization | [template-skg-observability.md](template-skg-observability.md) |

**When to Use**:
- Complex AI workflows with multiple steps
- Conditional branching and routing
- State management and persistence
- Production-grade AI systems with monitoring

### 3. Microsoft Agent Framework

The Microsoft Agent Framework provides high-level abstractions for building AI agents. Use this approach for:

| Template | Complexity | Use Case | Documentation |
|----------|-----------|----------|---------------|
| **Agent Framework Basic** | Medium | Simple agent creation | [template-agent-framework-basic.md](template-agent-framework-basic.md) |
| **Graph Workflows** | High | Workflow-based agents | [template-agent-framework-workflows.md](template-agent-framework-workflows.md) |
| **Multi-Agent** | Very High | Agent orchestration | [template-agent-framework-multi-agent.md](template-agent-framework-multi-agent.md) |
| **Middleware** | High | Request/response processing | [template-agent-framework-middleware.md](template-agent-framework-middleware.md) |

**When to Use**:
- Enterprise-grade agent development
- Azure OpenAI integration
- Microsoft ecosystem alignment
- Rapid agent prototyping

---

## Decision Matrix

### By Use Case

| Use Case | Recommended Approach | Template |
|----------|---------------------|----------|
| Simple chatbot | Semantic Kernel | Chat Completion |
| Q&A over documents | Semantic Kernel | RAG Basic |
| Tool-augmented AI | Semantic Kernel | Plugins & Functions |
| Complex reasoning | Semantic Kernel Graph | Chain of Thought |
| Agent with tools | Semantic Kernel Graph | ReAct Agent |
| Multi-step workflows | Semantic Kernel Graph | Graph Executor |
| Persistent conversations | Semantic Kernel Graph | Chatbot with Memory |
| Document processing | Semantic Kernel Graph | Document Pipeline |
| Multiple AI agents | Semantic Kernel Graph | Multi-Agent |
| Human oversight needed | Semantic Kernel Graph | Human-in-the-Loop |
| Enterprise agents | Agent Framework | Agent Framework Basic |

### By Complexity Level

| Complexity | Approaches | Typical Scenarios |
|------------|-----------|-------------------|
| **Low** | SK Chat Completion, SK Plugins | MVP, prototypes, simple integrations |
| **Medium** | SK RAG, SK Planners, SKG Graph Executor | Business applications, document Q&A |
| **High** | SKG ReAct, SKG Chain of Thought, SKG Chatbot | Advanced AI features, reasoning systems |
| **Very High** | SKG Multi-Agent, Agent Framework | Enterprise AI, autonomous systems |

### By Production Requirements

| Requirement | Recommended Approach | Key Features |
|-------------|---------------------|--------------|
| **High Availability** | Semantic Kernel Graph | Checkpointing, recovery |
| **Real-time Monitoring** | Semantic Kernel Graph | Streaming, observability |
| **Human Oversight** | Semantic Kernel Graph | Human-in-the-loop |
| **State Persistence** | Semantic Kernel Graph | Checkpointing |
| **Azure Integration** | Agent Framework | Azure OpenAI native support |

---

## Required NuGet Packages

### Semantic Kernel (Pure)

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

## Configuration Patterns

### appsettings.json Structure

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

### Environment Variables

```bash
# OpenAI
OPENAI_API_KEY=your-api-key-here

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-azure-api-key-here
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4o
```

---

## Integration with Mvp24Hours

### Service Registration Pattern

```csharp
public static class AIServiceExtensions
{
    public static IServiceCollection AddAIServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Semantic Kernel
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

### Controller Integration Pattern

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

## Quick Reference

### Kernel Creation

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

### Graph Executor Creation

```csharp
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Extensions;

var builder = Kernel.CreateBuilder();
builder.AddGraphSupport();
var kernel = builder.Build();

var executor = new GraphExecutor("MyGraph", "Graph description");
executor.AddNode(node);
executor.SetStartNode(node.NodeId);

var result = await executor.ExecuteAsync(kernel, arguments);
```

### Function Node Creation

```csharp
using SemanticKernel.Graph.Nodes;

var node = new FunctionGraphNode(
    kernel.CreateFunctionFromMethod(
        (string input) => $"Processed: {input}",
        functionName: "ProcessInput",
        description: "Processes the input"
    ),
    nodeId: "process-node"
).StoreResultAs("result");
```

---

## Related Documentation

### Architecture Templates
- [Clean Architecture](template-clean-architecture.md)
- [CQRS](template-cqrs.md)
- [Microservices](template-microservices.md)

### Core Documentation
- [Decision Matrix](decision-matrix.md)
- [Database Patterns](database-patterns.md)
- [Messaging Patterns](messaging-patterns.md)

### AI Decision Matrix
- [AI Decision Matrix](ai-decision-matrix.md)

---

## External References

- **Semantic Kernel**: [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel)
- **Semantic Kernel Graph**: [SemanticKernel.Graph](https://github.com/kallebelins/semantic-kernel-graph)
- **Microsoft Agent Framework**: [Agent Framework](https://github.com/microsoft/agent-framework)
- **Learn Semantic Kernel**: [Microsoft Learn](https://learn.microsoft.com/semantic-kernel)

