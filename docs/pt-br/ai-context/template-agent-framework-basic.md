# Template Básico Agent Framework - Microsoft.Extensions.AI

> **Propósito**: Este template fornece padrões para construir agentes de IA usando Microsoft.Extensions.AI.

---

## Visão Geral

Microsoft.Extensions.AI fornece abstrações unificadas para serviços de IA em .NET:
- Integração agnóstica de provedor
- Interface consistente para chat completion, embeddings e ferramentas
- Suporte nativo .NET 9+ com injeção de dependência

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Aplicações IA enterprise | ✅ Recomendado |
| Integração Azure OpenAI | ✅ Recomendado |
| Serviços IA agnósticos de provedor | ✅ Recomendado |
| Protótipos simples | ⚠️ Considere Semantic Kernel |

---

## Pacotes NuGet Necessários

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.*-*" />
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.*-*" />
  <PackageReference Include="Microsoft.Extensions.AI.AzureAIInference" Version="9.*-*" />
</ItemGroup>
```

---

## Configuração

```json
{
  "AI": {
    "Provider": "AzureOpenAI",
    "OpenAI": {
      "ApiKey": "${OPENAI_API_KEY}",
      "ModelId": "gpt-4o"
    },
    "AzureOpenAI": {
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "ApiKey": "${AZURE_OPENAI_API_KEY}",
      "DeploymentName": "gpt-4o"
    }
  },
  "Agent": {
    "SystemPrompt": "Você é um assistente de IA prestativo.",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

---

## Implementação

### Serviço de Agente Básico

```csharp
using Microsoft.Extensions.AI;

public interface IAgentService
{
    Task<string> ProcessAsync(string userMessage, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ProcessStreamingAsync(string userMessage, CancellationToken cancellationToken = default);
}

public class AgentService : IAgentService
{
    private readonly IChatClient _chatClient;
    private readonly AgentConfiguration _config;

    public AgentService(IChatClient chatClient, IOptions<AgentConfiguration> config)
    {
        _chatClient = chatClient;
        _config = config.Value;
    }

    public async Task<string> ProcessAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _config.SystemPrompt),
            new(ChatRole.User, userMessage)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Temperature = _config.Temperature
        };

        var response = await _chatClient.CompleteAsync(messages, options, cancellationToken);
        return response.Message.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> ProcessStreamingAsync(
        string userMessage, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _config.SystemPrompt),
            new(ChatRole.User, userMessage)
        };

        await foreach (var update in _chatClient.CompleteStreamingAsync(messages, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }
}
```

### Agente com Ferramentas

```csharp
public class ToolEnabledAgent
{
    private readonly IChatClient _chatClient;
    private readonly AgentConfiguration _config;

    public async Task<string> ProcessWithToolsAsync(
        string userMessage,
        IEnumerable<AITool> tools,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _config.SystemPrompt),
            new(ChatRole.User, userMessage)
        };

        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Tools = tools.ToList()
        };

        var response = await _chatClient.CompleteAsync(messages, options, cancellationToken);

        // Processar chamadas de ferramentas
        while (response.FinishReason == ChatFinishReason.ToolCalls)
        {
            messages.Add(response.Message);

            foreach (var toolCall in response.Message.Contents.OfType<FunctionCallContent>())
            {
                var result = await ExecuteToolAsync(toolCall, tools, cancellationToken);
                messages.Add(new ChatMessage(ChatRole.Tool, new[]
                {
                    new FunctionResultContent(toolCall.CallId, toolCall.Name, result)
                }));
            }

            response = await _chatClient.CompleteAsync(messages, options, cancellationToken);
        }

        return response.Message.Text ?? string.Empty;
    }
}
```

### Definição de Ferramentas

```csharp
using Microsoft.Extensions.AI;
using System.ComponentModel;

public static class AgentTools
{
    public static IEnumerable<AITool> GetDefaultTools()
    {
        yield return AIFunctionFactory.Create(GetCurrentWeather);
        yield return AIFunctionFactory.Create(GetCurrentTime);
    }

    [Description("Obtém o clima atual para uma cidade")]
    public static string GetCurrentWeather(
        [Description("Nome da cidade")] string city)
    {
        return city.ToLowerInvariant() switch
        {
            "são paulo" => "Ensolarado, 28°C",
            "rio de janeiro" => "Parcialmente nublado, 32°C",
            _ => $"Clima para {city}: Parcialmente nublado, 22°C"
        };
    }

    [Description("Obtém data e hora atuais")]
    public static string GetCurrentTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
```

---

## Integração com Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AgentRequest request, CancellationToken cancellationToken)
    {
        var response = await _agentService.ProcessAsync(request.Message, cancellationToken);
        return Ok(new { message = response });
    }

    [HttpPost("chat/stream")]
    public async Task StreamChat([FromBody] AgentRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        await foreach (var chunk in _agentService.ProcessStreamingAsync(request.Message, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
```

---

## Boas Práticas

1. **Prompts de Sistema**: Defina comportamento claro para o agente
2. **Gerenciamento de Tokens**: Limite histórico para não exceder limites
3. **Tratamento de Erros**: Implemente retry para rate limits
4. **Descrições de Ferramentas**: Descrições claras ajudam a IA escolher corretamente

---

## Templates Relacionados

- [Graph Workflows](template-agent-framework-workflows.md) - Workflows com grafos
- [Multi-Agent](template-agent-framework-multi-agent.md) - Coordenação de agentes
- [Chat Completion](template-sk-chat-completion.md) - Alternativa Semantic Kernel

---

## Referências Externas

- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai)

