# Template de Chat Completion - Semantic Kernel

> **Propósito**: Este template fornece padrões e diretrizes de implementação para funcionalidade básica de chat completion usando Microsoft Semantic Kernel.

---

## Visão Geral

Chat Completion é o padrão fundamental para IA conversacional em .NET. Este template cobre:
- Configuração do Kernel para diferentes provedores de IA
- Execução básica de prompts
- Gerenciamento de histórico de conversa
- Respostas em streaming
- Templates e configurações de prompts

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Chatbot simples de Q&A | ✅ Recomendado |
| Bot de suporte ao cliente | ✅ Recomendado |
| Geração de texto | ✅ Recomendado |
| Raciocínio complexo | ⚠️ Considere Chain of Thought |
| Workflows multi-etapas | ⚠️ Considere Graph Executor |
| IA com ferramentas | ⚠️ Considere template de Plugins |

---

## Pacotes NuGet Necessários

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <!-- OU para Azure OpenAI -->
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.AzureOpenAI" Version="1.*" />
</ItemGroup>
```

---

## Configuração

### appsettings.json

```json
{
  "AI": {
    "Provider": "OpenAI",
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
  "ChatCompletion": {
    "MaxTokens": 2048,
    "Temperature": 0.7
  }
}
```

---

## Padrões de Implementação

### 1. Configuração Básica do Kernel

```csharp
using Microsoft.SemanticKernel;

public static class KernelFactory
{
    public static Kernel CreateKernel(IConfiguration configuration)
    {
        var builder = Kernel.CreateBuilder();
        var provider = configuration["AI:Provider"];

        if (provider == "OpenAI")
        {
            builder.AddOpenAIChatCompletion(
                modelId: configuration["AI:OpenAI:ModelId"]!,
                apiKey: configuration["AI:OpenAI:ApiKey"]!);
        }
        else if (provider == "AzureOpenAI")
        {
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: configuration["AI:AzureOpenAI:DeploymentName"]!,
                endpoint: configuration["AI:AzureOpenAI:Endpoint"]!,
                apiKey: configuration["AI:AzureOpenAI:ApiKey"]!);
        }

        return builder.Build();
    }
}
```

### 2. Serviço de Chat

```csharp
using Microsoft.SemanticKernel.ChatCompletion;

public interface IChatService
{
    Task<string> GetResponseAsync(string userMessage, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GetStreamingResponseAsync(string userMessage, CancellationToken cancellationToken = default);
}

public class ChatService : IChatService
{
    private readonly IChatCompletionService _chatCompletion;

    public ChatService(Kernel kernel)
    {
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> GetResponseAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("Você é um assistente prestativo.");
        chatHistory.AddUserMessage(userMessage);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory, cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        string userMessage, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("Você é um assistente prestativo.");
        chatHistory.AddUserMessage(userMessage);

        await foreach (var chunk in _chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory, cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
                yield return chunk.Content;
        }
    }
}
```

### 3. Chat com Histórico de Conversa

```csharp
public class ConversationalChatService
{
    private readonly IChatCompletionService _chatCompletion;
    private readonly Dictionary<string, ChatHistory> _conversations = new();

    public async Task<string> SendMessageAsync(
        string conversationId, string userMessage, CancellationToken cancellationToken = default)
    {
        var history = GetOrCreateConversation(conversationId);
        history.AddUserMessage(userMessage);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            history, cancellationToken: cancellationToken);

        var assistantMessage = response.Content ?? string.Empty;
        history.AddAssistantMessage(assistantMessage);

        return assistantMessage;
    }

    private ChatHistory GetOrCreateConversation(string conversationId)
    {
        if (!_conversations.TryGetValue(conversationId, out var history))
        {
            history = new ChatHistory();
            history.AddSystemMessage("Você é um assistente prestativo.");
            _conversations[conversationId] = history;
        }
        return history;
    }
}
```

---

## Integração com Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService) => _chatService = chatService;

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var response = await _chatService.GetResponseAsync(request.Message, cancellationToken);
        return Ok(new { message = response });
    }

    [HttpPost("stream")]
    public async Task StreamChat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        await foreach (var chunk in _chatService.GetStreamingResponseAsync(request.Message, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}
```

---

## Boas Práticas

1. **Prompts de Sistema**: Defina comportamento e restrições claras
2. **Gerenciamento de Tokens**: Limite o histórico para evitar exceder limites
3. **Tratamento de Erros**: Implemente retry para rate limits
4. **Streaming**: Use para melhor UX em respostas longas

---

## Templates Relacionados

- [Plugins & Functions](template-sk-plugins.md) - Integração de ferramentas
- [RAG Básico](template-sk-rag-basic.md) - Q&A sobre documentos
- [Graph Executor](template-skg-graph-executor.md) - Workflows complexos

---

## Referências Externas

- [Documentação Semantic Kernel](https://learn.microsoft.com/semantic-kernel)
- [Referência API OpenAI](https://platform.openai.com/docs/api-reference)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai)

