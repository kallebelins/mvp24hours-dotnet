# Chat Completion Template - Semantic Kernel

> **Purpose**: This template provides AI agents with patterns and implementation guidelines for basic chat completion functionality using Microsoft Semantic Kernel.

---

## Overview

Chat Completion is the foundational pattern for conversational AI in .NET. This template covers:
- Kernel configuration for different AI providers
- Basic prompt execution
- Conversation history management
- Streaming responses
- Prompt templates and settings

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Simple Q&A chatbot | ✅ Recommended |
| Customer support bot | ✅ Recommended |
| Text generation | ✅ Recommended |
| Complex reasoning | ⚠️ Consider Chain of Thought |
| Multi-step workflows | ⚠️ Consider Graph Executor |
| Tool-augmented AI | ⚠️ Consider Plugins template |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <!-- OR for Azure OpenAI -->
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.AzureOpenAI" Version="1.*" />
</ItemGroup>
```

---

## Configuration

### appsettings.json

```json
{
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "${OPENAI_API_KEY}",
      "ModelId": "gpt-4o",
      "OrganizationId": ""
    },
    "AzureOpenAI": {
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "ApiKey": "${AZURE_OPENAI_API_KEY}",
      "DeploymentName": "gpt-4o"
    }
  },
  "ChatCompletion": {
    "MaxTokens": 2048,
    "Temperature": 0.7,
    "TopP": 1.0,
    "FrequencyPenalty": 0.0,
    "PresencePenalty": 0.0
  }
}
```

### Configuration Class

```csharp
public class AIConfiguration
{
    public string Provider { get; set; } = "OpenAI";
    public OpenAIConfiguration OpenAI { get; set; } = new();
    public AzureOpenAIConfiguration AzureOpenAI { get; set; } = new();
}

public class OpenAIConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4o";
    public string? OrganizationId { get; set; }
}

public class AzureOpenAIConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4o";
}

public class ChatCompletionConfiguration
{
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 1.0;
    public double FrequencyPenalty { get; set; } = 0.0;
    public double PresencePenalty { get; set; } = 0.0;
}
```

---

## Implementation Patterns

### 1. Basic Kernel Setup

```csharp
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class KernelFactory
{
    public static Kernel CreateKernel(IConfiguration configuration)
    {
        var builder = Kernel.CreateBuilder();
        var aiConfig = configuration.GetSection("AI").Get<AIConfiguration>()!;

        if (aiConfig.Provider == "OpenAI")
        {
            builder.AddOpenAIChatCompletion(
                modelId: aiConfig.OpenAI.ModelId,
                apiKey: aiConfig.OpenAI.ApiKey,
                orgId: aiConfig.OpenAI.OrganizationId);
        }
        else if (aiConfig.Provider == "AzureOpenAI")
        {
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: aiConfig.AzureOpenAI.DeploymentName,
                endpoint: aiConfig.AzureOpenAI.Endpoint,
                apiKey: aiConfig.AzureOpenAI.ApiKey);
        }

        return builder.Build();
    }
}
```

### 2. Dependency Injection Registration

```csharp
public static class AIServiceExtensions
{
    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var aiConfig = configuration.GetSection("AI").Get<AIConfiguration>()!;

        services.AddSingleton(sp =>
        {
            var builder = Kernel.CreateBuilder();

            if (aiConfig.Provider == "OpenAI")
            {
                builder.AddOpenAIChatCompletion(
                    modelId: aiConfig.OpenAI.ModelId,
                    apiKey: aiConfig.OpenAI.ApiKey);
            }
            else if (aiConfig.Provider == "AzureOpenAI")
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: aiConfig.AzureOpenAI.DeploymentName,
                    endpoint: aiConfig.AzureOpenAI.Endpoint,
                    apiKey: aiConfig.AzureOpenAI.ApiKey);
            }

            return builder.Build();
        });

        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}
```

### 3. Basic Chat Service

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

public interface IChatService
{
    Task<string> GetResponseAsync(string userMessage, CancellationToken cancellationToken = default);
    Task<string> GetResponseAsync(ChatHistory chatHistory, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GetStreamingResponseAsync(string userMessage, CancellationToken cancellationToken = default);
}

public class ChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;

    public ChatService(Kernel kernel)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
    }

    public async Task<string> GetResponseAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful assistant.");
        chatHistory.AddUserMessage(userMessage);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }

    public async Task<string> GetResponseAsync(ChatHistory chatHistory, CancellationToken cancellationToken = default)
    {
        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful assistant.");
        chatHistory.AddUserMessage(userMessage);

        await foreach (var chunk in _chatCompletion.GetStreamingChatMessageContentsAsync(
            chatHistory,
            cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }
}
```

### 4. Chat with Conversation History

```csharp
public class ConversationalChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly Dictionary<string, ChatHistory> _conversations = new();
    private readonly string _systemPrompt;

    public ConversationalChatService(Kernel kernel, string systemPrompt = "You are a helpful assistant.")
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _systemPrompt = systemPrompt;
    }

    public ChatHistory GetOrCreateConversation(string conversationId)
    {
        if (!_conversations.TryGetValue(conversationId, out var history))
        {
            history = new ChatHistory();
            history.AddSystemMessage(_systemPrompt);
            _conversations[conversationId] = history;
        }
        return history;
    }

    public async Task<string> SendMessageAsync(
        string conversationId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var history = GetOrCreateConversation(conversationId);
        history.AddUserMessage(userMessage);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            history,
            cancellationToken: cancellationToken);

        var assistantMessage = response.Content ?? string.Empty;
        history.AddAssistantMessage(assistantMessage);

        return assistantMessage;
    }

    public void ClearConversation(string conversationId)
    {
        _conversations.Remove(conversationId);
    }

    public IEnumerable<ChatMessageContent> GetConversationHistory(string conversationId)
    {
        return _conversations.TryGetValue(conversationId, out var history)
            ? history
            : Enumerable.Empty<ChatMessageContent>();
    }
}
```

### 5. Prompt Templates

```csharp
public class PromptTemplateService
{
    private readonly Kernel _kernel;

    public PromptTemplateService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ExecutePromptAsync(
        string promptTemplate,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var result = await _kernel.InvokePromptAsync(
            promptTemplate,
            arguments,
            cancellationToken: cancellationToken);

        return result.GetValue<string>() ?? string.Empty;
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
    {
        var prompt = """
            Summarize the following text in a concise manner:

            {{$input}}

            Summary:
            """;

        return await ExecutePromptAsync(
            prompt,
            new KernelArguments { ["input"] = text },
            cancellationToken);
    }

    public async Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var prompt = """
            Translate the following text to {{$language}}:

            {{$input}}

            Translation:
            """;

        return await ExecutePromptAsync(
            prompt,
            new KernelArguments
            {
                ["input"] = text,
                ["language"] = targetLanguage
            },
            cancellationToken);
    }

    public async Task<string> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
        var prompt = """
            Analyze the sentiment of the following text and respond with one of: 
            POSITIVE, NEGATIVE, NEUTRAL, or MIXED.

            Text: {{$input}}

            Sentiment:
            """;

        return await ExecutePromptAsync(
            prompt,
            new KernelArguments { ["input"] = text },
            cancellationToken);
    }
}
```

### 6. Execution Settings

```csharp
using Microsoft.SemanticKernel.Connectors.OpenAI;

public class ChatServiceWithSettings
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly OpenAIPromptExecutionSettings _defaultSettings;

    public ChatServiceWithSettings(Kernel kernel, IConfiguration configuration)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        var chatConfig = configuration.GetSection("ChatCompletion").Get<ChatCompletionConfiguration>()!;
        _defaultSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = chatConfig.MaxTokens,
            Temperature = chatConfig.Temperature,
            TopP = chatConfig.TopP,
            FrequencyPenalty = chatConfig.FrequencyPenalty,
            PresencePenalty = chatConfig.PresencePenalty
        };
    }

    public async Task<string> GetCreativeResponseAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = _defaultSettings.MaxTokens,
            Temperature = 0.9,  // Higher temperature for creativity
            TopP = 1.0
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a creative writing assistant.");
        chatHistory.AddUserMessage(userMessage);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            settings,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }

    public async Task<string> GetPreciseResponseAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = _defaultSettings.MaxTokens,
            Temperature = 0.1,  // Lower temperature for precision
            TopP = 0.5
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a precise and factual assistant. Provide accurate, well-structured answers.");
        chatHistory.AddUserMessage(userMessage);

        var response = await _chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            settings,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }
}
```

---

## Web API Integration

### Controller Implementation

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _chatService.GetResponseAsync(request.Message, cancellationToken);
            return Ok(new ChatResponse { Message = response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { error = "An error occurred processing your request" });
        }
    }

    [HttpPost("stream")]
    public async Task StreamChat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        await foreach (var chunk in _chatService.GetStreamingResponseAsync(request.Message, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }
}

public record ChatRequest(string Message);
public record ChatResponse
{
    public string Message { get; init; } = string.Empty;
}
```

### Minimal API Alternative

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSemanticKernel(builder.Configuration);

var app = builder.Build();

app.MapPost("/api/chat", async (
    ChatRequest request,
    IChatService chatService,
    CancellationToken cancellationToken) =>
{
    var response = await chatService.GetResponseAsync(request.Message, cancellationToken);
    return Results.Ok(new { message = response });
});

app.MapPost("/api/chat/stream", async (
    ChatRequest request,
    IChatService chatService,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    context.Response.ContentType = "text/event-stream";

    await foreach (var chunk in chatService.GetStreamingResponseAsync(request.Message, cancellationToken))
    {
        await context.Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.Run();
```

---

## Integration with Mvp24Hours

### Service Layer Integration

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

public class ChatBusinessService : IBusinessService
{
    private readonly IChatService _chatService;
    private readonly IPipelineAsync _pipeline;

    public ChatBusinessService(IChatService chatService, IPipelineAsync pipeline)
    {
        _chatService = chatService;
        _pipeline = pipeline;
    }

    public async Task<IBusinessResult<ChatResponse>> ProcessChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _chatService.GetResponseAsync(request.Message, cancellationToken);

        return new BusinessResult<ChatResponse>
        {
            Data = new ChatResponse { Message = response }
        }.SetSuccess();
    }
}
```

### Pipeline Integration

```csharp
public class AIChatOperation : OperationBaseAsync<ChatRequest, ChatResponse>
{
    private readonly IChatService _chatService;

    public AIChatOperation(IChatService chatService)
    {
        _chatService = chatService;
    }

    public override async Task<ChatResponse> ExecuteAsync(
        ChatRequest input,
        CancellationToken cancellationToken = default)
    {
        var response = await _chatService.GetResponseAsync(input.Message, cancellationToken);
        return new ChatResponse { Message = response };
    }
}
```

---

## Error Handling

```csharp
public class ResilientChatService : IChatService
{
    private readonly IChatService _innerService;
    private readonly ILogger<ResilientChatService> _logger;

    public ResilientChatService(IChatService innerService, ILogger<ResilientChatService> logger)
    {
        _innerService = innerService;
        _logger = logger;
    }

    public async Task<string> GetResponseAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _innerService.GetResponseAsync(userMessage, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limit exceeded, implementing backoff");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return await _innerService.GetResponseAsync(userMessage, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogError(ex, "AI service unavailable");
            return "I'm sorry, the AI service is currently unavailable. Please try again later.";
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogInformation("Request was cancelled by the user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in chat service");
            return "I encountered an unexpected error. Please try again.";
        }
    }

    // Implement other interface methods similarly...
}
```

---

## Testing Patterns

### Unit Testing

```csharp
using Moq;
using Xunit;

public class ChatServiceTests
{
    [Fact]
    public async Task GetResponseAsync_ReturnsValidResponse()
    {
        // Arrange
        var mockChatCompletion = new Mock<IChatCompletionService>();
        mockChatCompletion
            .Setup(x => x.GetChatMessageContentAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatMessageContent(AuthorRole.Assistant, "Hello! How can I help?"));

        var kernel = Kernel.CreateBuilder().Build();
        // Note: In real tests, you'd need to properly inject the mock

        // Act & Assert
        // Implement based on your specific service implementation
    }
}
```

### Integration Testing

```csharp
public class ChatServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ChatServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Chat_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new { message = "Hello" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }
}
```

---

## Best Practices

### System Prompts

1. **Be Specific**: Define clear behavior and constraints
2. **Include Context**: Provide relevant background information
3. **Set Boundaries**: Specify what the AI should and shouldn't do
4. **Format Instructions**: Guide output formatting when needed

```csharp
private const string CustomerSupportPrompt = """
    You are a customer support assistant for TechCorp.
    
    Guidelines:
    - Be polite, professional, and empathetic
    - Answer questions about our products and services
    - If you don't know the answer, say so and offer to escalate
    - Never make up information about prices or policies
    - Keep responses concise but complete
    
    Available products: Widget Pro, Widget Basic, Widget Enterprise
    Support hours: Monday-Friday, 9 AM - 6 PM EST
    """;
```

### Token Management

```csharp
public class TokenAwareChatService
{
    private const int MaxContextTokens = 4000;
    private const int ReservedResponseTokens = 1000;

    public ChatHistory TrimHistoryToFit(ChatHistory history, int estimatedTokens)
    {
        var availableTokens = MaxContextTokens - ReservedResponseTokens;
        
        if (estimatedTokens <= availableTokens)
            return history;

        // Remove oldest messages (except system message) to fit
        var trimmedHistory = new ChatHistory();
        var systemMessage = history.FirstOrDefault(m => m.Role == AuthorRole.System);
        
        if (systemMessage != null)
            trimmedHistory.Add(systemMessage);

        var recentMessages = history
            .Where(m => m.Role != AuthorRole.System)
            .TakeLast(10);  // Keep last 10 messages

        foreach (var message in recentMessages)
            trimmedHistory.Add(message);

        return trimmedHistory;
    }
}
```

---

## Related Templates

- [Plugins & Functions](template-sk-plugins.md) - Tool integration
- [RAG Basic](template-sk-rag-basic.md) - Document Q&A
- [Graph Executor](template-skg-graph-executor.md) - Complex workflows

---

## External References

- [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai)
