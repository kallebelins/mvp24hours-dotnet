# Agent Framework Basic Template - Microsoft.Extensions.AI

> **Purpose**: This template provides AI agents with patterns and implementation guidelines for building AI agents using Microsoft.Extensions.AI abstractions and the unified AI library for .NET.

---

## Overview

Microsoft.Extensions.AI provides unified abstractions for AI services in .NET, enabling:
- Provider-agnostic AI integration
- Consistent interface for chat completion, embeddings, and tools
- Built-in support for OpenAI, Azure OpenAI, and other providers
- Native .NET 9+ integration with dependency injection

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Enterprise AI applications | ✅ Recommended |
| Azure OpenAI integration | ✅ Recommended |
| Provider-agnostic AI services | ✅ Recommended |
| .NET 9+ applications | ✅ Recommended |
| Simple prototypes | ⚠️ Consider Semantic Kernel |
| Complex graph workflows | ⚠️ Consider SK Graph |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <!-- Core abstractions -->
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.*-*" />
  
  <!-- OpenAI provider -->
  <PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.*-*" />
  
  <!-- Azure OpenAI provider -->
  <PackageReference Include="Microsoft.Extensions.AI.AzureAIInference" Version="9.*-*" />
  
  <!-- For Azure.AI.OpenAI client -->
  <PackageReference Include="Azure.AI.OpenAI" Version="2.*" />
</ItemGroup>
```

---

## Configuration

### appsettings.json

```json
{
  "AI": {
    "Provider": "AzureOpenAI",
    "OpenAI": {
      "ApiKey": "${OPENAI_API_KEY}",
      "ModelId": "gpt-4o",
      "Endpoint": "https://api.openai.com/v1"
    },
    "AzureOpenAI": {
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "ApiKey": "${AZURE_OPENAI_API_KEY}",
      "DeploymentName": "gpt-4o"
    }
  },
  "Agent": {
    "Name": "AssistantAgent",
    "SystemPrompt": "You are a helpful AI assistant.",
    "MaxTokens": 4096,
    "Temperature": 0.7
  }
}
```

### Configuration Classes

```csharp
public class AIProviderConfiguration
{
    public string Provider { get; set; } = "AzureOpenAI";
    public OpenAISettings OpenAI { get; set; } = new();
    public AzureOpenAISettings AzureOpenAI { get; set; } = new();
}

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "gpt-4o";
    public string Endpoint { get; set; } = "https://api.openai.com/v1";
}

public class AzureOpenAISettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = "gpt-4o";
}

public class AgentConfiguration
{
    public string Name { get; set; } = "AssistantAgent";
    public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.7f;
}
```

---

## Implementation Patterns

### 1. IChatClient Setup

The `IChatClient` interface is the core abstraction for chat-based AI interactions:

```csharp
using Microsoft.Extensions.AI;
using OpenAI;
using Azure.AI.OpenAI;

public static class ChatClientFactory
{
    public static IChatClient CreateChatClient(IConfiguration configuration)
    {
        var aiConfig = configuration.GetSection("AI").Get<AIProviderConfiguration>()!;

        return aiConfig.Provider switch
        {
            "OpenAI" => CreateOpenAIChatClient(aiConfig.OpenAI),
            "AzureOpenAI" => CreateAzureOpenAIChatClient(aiConfig.AzureOpenAI),
            _ => throw new ArgumentException($"Unknown provider: {aiConfig.Provider}")
        };
    }

    private static IChatClient CreateOpenAIChatClient(OpenAISettings settings)
    {
        var client = new OpenAIClient(settings.ApiKey);
        return client.AsChatClient(settings.ModelId);
    }

    private static IChatClient CreateAzureOpenAIChatClient(AzureOpenAISettings settings)
    {
        var client = new AzureOpenAIClient(
            new Uri(settings.Endpoint),
            new Azure.AzureKeyCredential(settings.ApiKey));
        
        return client.AsChatClient(settings.DeploymentName);
    }
}
```

### 2. Dependency Injection Registration

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

public static class AIServiceExtensions
{
    public static IServiceCollection AddAIServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var aiConfig = configuration.GetSection("AI").Get<AIProviderConfiguration>()!;
        var agentConfig = configuration.GetSection("Agent").Get<AgentConfiguration>()!;

        // Register configuration
        services.Configure<AIProviderConfiguration>(configuration.GetSection("AI"));
        services.Configure<AgentConfiguration>(configuration.GetSection("Agent"));

        // Register IChatClient
        services.AddSingleton<IChatClient>(sp =>
        {
            var chatClient = ChatClientFactory.CreateChatClient(configuration);
            
            // Add caching and logging middleware
            return new ChatClientBuilder(chatClient)
                .UseLogging()
                .Build();
        });

        // Register agent service
        services.AddScoped<IAgentService, AgentService>();

        return services;
    }
}
```

### 3. Basic Agent Service

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

public interface IAgentService
{
    Task<string> ProcessAsync(string userMessage, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ProcessStreamingAsync(string userMessage, CancellationToken cancellationToken = default);
    Task<string> ProcessWithHistoryAsync(IList<ChatMessage> history, string userMessage, CancellationToken cancellationToken = default);
}

public class AgentService : IAgentService
{
    private readonly IChatClient _chatClient;
    private readonly AgentConfiguration _config;
    private readonly ILogger<AgentService> _logger;

    public AgentService(
        IChatClient chatClient,
        IOptions<AgentConfiguration> config,
        ILogger<AgentService> logger)
    {
        _chatClient = chatClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string> ProcessAsync(
        string userMessage,
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
            Temperature = _config.Temperature
        };

        _logger.LogInformation("Processing message with agent {AgentName}", _config.Name);

        var response = await _chatClient.CompleteAsync(messages, options, cancellationToken);
        
        return response.Message.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> ProcessStreamingAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

        await foreach (var update in _chatClient.CompleteStreamingAsync(messages, options, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return update.Text;
            }
        }
    }

    public async Task<string> ProcessWithHistoryAsync(
        IList<ChatMessage> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage>(history)
        {
            new(ChatRole.User, userMessage)
        };

        // Ensure system message is first
        if (messages.Count == 0 || messages[0].Role != ChatRole.System)
        {
            messages.Insert(0, new ChatMessage(ChatRole.System, _config.SystemPrompt));
        }

        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Temperature = _config.Temperature
        };

        var response = await _chatClient.CompleteAsync(messages, options, cancellationToken);
        
        return response.Message.Text ?? string.Empty;
    }
}
```

### 4. Conversational Agent with State

```csharp
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;

public interface IConversationalAgent
{
    Task<string> SendMessageAsync(string conversationId, string message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> SendMessageStreamingAsync(string conversationId, string message, CancellationToken cancellationToken = default);
    void ClearConversation(string conversationId);
    IReadOnlyList<ChatMessage> GetHistory(string conversationId);
}

public class ConversationalAgent : IConversationalAgent
{
    private readonly IChatClient _chatClient;
    private readonly AgentConfiguration _config;
    private readonly ConcurrentDictionary<string, List<ChatMessage>> _conversations = new();
    private readonly ILogger<ConversationalAgent> _logger;

    public ConversationalAgent(
        IChatClient chatClient,
        IOptions<AgentConfiguration> config,
        ILogger<ConversationalAgent> logger)
    {
        _chatClient = chatClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string> SendMessageAsync(
        string conversationId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var history = GetOrCreateHistory(conversationId);
        history.Add(new ChatMessage(ChatRole.User, message));

        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Temperature = _config.Temperature
        };

        _logger.LogInformation(
            "Processing message in conversation {ConversationId}, history count: {Count}",
            conversationId, history.Count);

        var response = await _chatClient.CompleteAsync(history, options, cancellationToken);
        var assistantMessage = response.Message.Text ?? string.Empty;

        history.Add(new ChatMessage(ChatRole.Assistant, assistantMessage));

        return assistantMessage;
    }

    public async IAsyncEnumerable<string> SendMessageStreamingAsync(
        string conversationId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var history = GetOrCreateHistory(conversationId);
        history.Add(new ChatMessage(ChatRole.User, message));

        var options = new ChatOptions
        {
            MaxOutputTokens = _config.MaxTokens,
            Temperature = _config.Temperature
        };

        var fullResponse = new StringBuilder();

        await foreach (var update in _chatClient.CompleteStreamingAsync(history, options, cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                fullResponse.Append(update.Text);
                yield return update.Text;
            }
        }

        history.Add(new ChatMessage(ChatRole.Assistant, fullResponse.ToString()));
    }

    public void ClearConversation(string conversationId)
    {
        _conversations.TryRemove(conversationId, out _);
        _logger.LogInformation("Cleared conversation {ConversationId}", conversationId);
    }

    public IReadOnlyList<ChatMessage> GetHistory(string conversationId)
    {
        return _conversations.TryGetValue(conversationId, out var history)
            ? history.AsReadOnly()
            : Array.Empty<ChatMessage>();
    }

    private List<ChatMessage> GetOrCreateHistory(string conversationId)
    {
        return _conversations.GetOrAdd(conversationId, _ => new List<ChatMessage>
        {
            new(ChatRole.System, _config.SystemPrompt)
        });
    }
}
```

### 5. Agent with Tool Calling

```csharp
using Microsoft.Extensions.AI;

public class ToolEnabledAgent
{
    private readonly IChatClient _chatClient;
    private readonly AgentConfiguration _config;
    private readonly ILogger<ToolEnabledAgent> _logger;

    public ToolEnabledAgent(
        IChatClient chatClient,
        IOptions<AgentConfiguration> config,
        ILogger<ToolEnabledAgent> logger)
    {
        _chatClient = chatClient;
        _config = config.Value;
        _logger = logger;
    }

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
            Temperature = _config.Temperature,
            Tools = tools.ToList()
        };

        var response = await _chatClient.CompleteAsync(messages, options, cancellationToken);

        // Handle tool calls
        while (response.FinishReason == ChatFinishReason.ToolCalls)
        {
            messages.Add(response.Message);

            foreach (var toolCall in response.Message.Contents.OfType<FunctionCallContent>())
            {
                _logger.LogInformation("Executing tool: {ToolName}", toolCall.Name);
                
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

    private async Task<object?> ExecuteToolAsync(
        FunctionCallContent toolCall,
        IEnumerable<AITool> tools,
        CancellationToken cancellationToken)
    {
        var tool = tools.FirstOrDefault(t => t.Metadata.Name == toolCall.Name);
        
        if (tool is AIFunction function)
        {
            return await function.InvokeAsync(toolCall.Arguments, cancellationToken);
        }

        return $"Tool {toolCall.Name} not found";
    }
}
```

---

## Tool Definition

### Creating AI Functions

```csharp
using Microsoft.Extensions.AI;
using System.ComponentModel;

public static class AgentTools
{
    public static IEnumerable<AITool> GetDefaultTools()
    {
        yield return AIFunctionFactory.Create(GetCurrentWeather);
        yield return AIFunctionFactory.Create(GetCurrentTime);
        yield return AIFunctionFactory.Create(CalculateExpression);
    }

    [Description("Gets the current weather for a specified city")]
    public static string GetCurrentWeather(
        [Description("The city name to get weather for")] string city)
    {
        // Simulated weather data
        return city.ToLowerInvariant() switch
        {
            "london" => "Cloudy, 15°C, light rain expected",
            "paris" => "Sunny, 22°C, clear skies",
            "tokyo" => "Partly cloudy, 18°C, humid",
            "new york" => "Clear, 20°C, moderate wind",
            _ => $"Weather for {city}: Partly cloudy, 18°C"
        };
    }

    [Description("Gets the current date and time")]
    public static string GetCurrentTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    [Description("Calculates a mathematical expression")]
    public static string CalculateExpression(
        [Description("The mathematical expression to evaluate (e.g., '2 + 2')")] string expression)
    {
        try
        {
            var result = new System.Data.DataTable().Compute(expression, null);
            return $"{expression} = {result}";
        }
        catch
        {
            return $"Unable to calculate: {expression}";
        }
    }
}
```

### Service-Based Tools with DI

```csharp
using Microsoft.Extensions.AI;

public class DatabaseQueryTool
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DatabaseQueryTool> _logger;

    public DatabaseQueryTool(IDbConnection connection, ILogger<DatabaseQueryTool> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    [Description("Searches the database for customer information")]
    public async Task<string> SearchCustomersAsync(
        [Description("The search term to find customers")] string searchTerm,
        [Description("Maximum number of results")] int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching customers with term: {SearchTerm}", searchTerm);
        
        // Implement actual database query
        await Task.Delay(100, cancellationToken); // Simulated delay
        
        return $"Found 3 customers matching '{searchTerm}'";
    }
}

public static class ToolRegistrationExtensions
{
    public static IServiceCollection AddAgentTools(this IServiceCollection services)
    {
        services.AddScoped<DatabaseQueryTool>();
        
        services.AddScoped<IEnumerable<AITool>>(sp =>
        {
            var dbTool = sp.GetRequiredService<DatabaseQueryTool>();
            
            return new AITool[]
            {
                AIFunctionFactory.Create(dbTool.SearchCustomersAsync),
                AIFunctionFactory.Create(AgentTools.GetCurrentWeather),
                AIFunctionFactory.Create(AgentTools.GetCurrentTime),
                AIFunctionFactory.Create(AgentTools.CalculateExpression)
            };
        });

        return services;
    }
}
```

---

## Web API Integration

### Controller Implementation

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IConversationalAgent _conversationalAgent;
    private readonly ILogger<AgentController> _logger;

    public AgentController(
        IAgentService agentService,
        IConversationalAgent conversationalAgent,
        ILogger<AgentController> logger)
    {
        _agentService = agentService;
        _conversationalAgent = conversationalAgent;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _agentService.ProcessAsync(request.Message, cancellationToken);
        return Ok(new AgentResponse { Message = response });
    }

    [HttpPost("chat/stream")]
    public async Task StreamChat(
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        await foreach (var chunk in _agentService.ProcessStreamingAsync(request.Message, cancellationToken))
        {
            await Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }

    [HttpPost("conversation/{conversationId}")]
    public async Task<IActionResult> SendMessage(
        string conversationId,
        [FromBody] AgentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _conversationalAgent.SendMessageAsync(
            conversationId,
            request.Message,
            cancellationToken);

        return Ok(new AgentResponse 
        { 
            Message = response,
            ConversationId = conversationId
        });
    }

    [HttpDelete("conversation/{conversationId}")]
    public IActionResult ClearConversation(string conversationId)
    {
        _conversationalAgent.ClearConversation(conversationId);
        return NoContent();
    }

    [HttpGet("conversation/{conversationId}/history")]
    public IActionResult GetHistory(string conversationId)
    {
        var history = _conversationalAgent.GetHistory(conversationId);
        return Ok(history.Select(m => new 
        { 
            Role = m.Role.Value, 
            Content = m.Text 
        }));
    }
}

public record AgentRequest(string Message);
public record AgentResponse
{
    public string Message { get; init; } = string.Empty;
    public string? ConversationId { get; init; }
}
```

### Minimal API Alternative

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAIServices(builder.Configuration);
builder.Services.AddAgentTools();

var app = builder.Build();

app.MapPost("/api/agent/chat", async (
    AgentRequest request,
    IAgentService agentService,
    CancellationToken cancellationToken) =>
{
    var response = await agentService.ProcessAsync(request.Message, cancellationToken);
    return Results.Ok(new { message = response });
});

app.MapPost("/api/agent/chat/stream", async (
    AgentRequest request,
    IAgentService agentService,
    HttpContext context,
    CancellationToken cancellationToken) =>
{
    context.Response.ContentType = "text/event-stream";

    await foreach (var chunk in agentService.ProcessStreamingAsync(request.Message, cancellationToken))
    {
        await context.Response.WriteAsync($"data: {chunk}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
});

app.MapPost("/api/agent/tools", async (
    AgentRequest request,
    ToolEnabledAgent toolAgent,
    IEnumerable<AITool> tools,
    CancellationToken cancellationToken) =>
{
    var response = await toolAgent.ProcessWithToolsAsync(request.Message, tools, cancellationToken);
    return Results.Ok(new { message = response });
});

app.Run();
```

---

## ChatClient Middleware Pipeline

### Building a Pipeline

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;

public static class ChatClientPipelineExtensions
{
    public static IChatClient BuildAgentPipeline(
        this IChatClient innerClient,
        IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>();
        var cache = services.GetService<IDistributedCache>();

        var builder = new ChatClientBuilder(innerClient);

        // Add logging
        builder.UseLogging(logger);

        // Add distributed caching if available
        if (cache != null)
        {
            builder.UseDistributedCache(cache);
        }

        // Add retry policy
        builder.UseRetry(maxRetries: 3);

        // Add rate limiting
        builder.UseRateLimiting(requestsPerMinute: 60);

        return builder.Build();
    }
}

// Custom middleware example
public class MetricsMiddleware : DelegatingChatClient
{
    private readonly IMetricsService _metrics;

    public MetricsMiddleware(IChatClient innerClient, IMetricsService metrics)
        : base(innerClient)
    {
        _metrics = metrics;
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await base.CompleteAsync(chatMessages, options, cancellationToken);
            
            stopwatch.Stop();
            _metrics.RecordLatency("chat_completion", stopwatch.ElapsedMilliseconds);
            _metrics.IncrementCounter("chat_completion_success");
            
            return result;
        }
        catch (Exception)
        {
            _metrics.IncrementCounter("chat_completion_error");
            throw;
        }
    }
}
```

---

## Integration with Mvp24Hours

### Service Layer Integration

```csharp
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Microsoft.Extensions.AI;

public class AgentBusinessService
{
    private readonly IAgentService _agentService;
    private readonly IPipelineAsync _pipeline;

    public AgentBusinessService(IAgentService agentService, IPipelineAsync pipeline)
    {
        _agentService = agentService;
        _pipeline = pipeline;
    }

    public async Task<IBusinessResult<AgentResponse>> ProcessMessageAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _agentService.ProcessAsync(request.Message, cancellationToken);

        return new BusinessResult<AgentResponse>
        {
            Data = new AgentResponse { Message = response }
        }.SetSuccess();
    }
}
```

### Pipeline Operation

```csharp
public class AIProcessingOperation : OperationBaseAsync<AgentRequest, AgentResponse>
{
    private readonly IAgentService _agentService;

    public AIProcessingOperation(IAgentService agentService)
    {
        _agentService = agentService;
    }

    public override async Task<AgentResponse> ExecuteAsync(
        AgentRequest input,
        CancellationToken cancellationToken = default)
    {
        var response = await _agentService.ProcessAsync(input.Message, cancellationToken);
        return new AgentResponse { Message = response };
    }
}
```

---

## Error Handling

```csharp
public class ResilientAgentService : IAgentService
{
    private readonly IAgentService _innerService;
    private readonly ILogger<ResilientAgentService> _logger;

    public ResilientAgentService(
        IAgentService innerService,
        ILogger<ResilientAgentService> logger)
    {
        _innerService = innerService;
        _logger = logger;
    }

    public async Task<string> ProcessAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _innerService.ProcessAsync(userMessage, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Rate limit exceeded, implementing backoff");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return await _innerService.ProcessAsync(userMessage, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogError(ex, "AI service unavailable");
            return "I'm sorry, the AI service is currently unavailable. Please try again later.";
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Request was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in agent service");
            return "I encountered an unexpected error. Please try again.";
        }
    }

    public IAsyncEnumerable<string> ProcessStreamingAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        return _innerService.ProcessStreamingAsync(userMessage, cancellationToken);
    }

    public Task<string> ProcessWithHistoryAsync(
        IList<ChatMessage> history,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        return _innerService.ProcessWithHistoryAsync(history, userMessage, cancellationToken);
    }
}
```

---

## Testing Patterns

### Unit Testing

```csharp
using Moq;
using Xunit;
using Microsoft.Extensions.AI;

public class AgentServiceTests
{
    [Fact]
    public async Task ProcessAsync_ReturnsValidResponse()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var expectedResponse = new ChatCompletion(new ChatMessage(ChatRole.Assistant, "Hello!"));
        
        mockChatClient
            .Setup(x => x.CompleteAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var config = Options.Create(new AgentConfiguration());
        var logger = new Mock<ILogger<AgentService>>();
        
        var service = new AgentService(mockChatClient.Object, config, logger.Object);

        // Act
        var result = await service.ProcessAsync("Hello");

        // Assert
        Assert.Equal("Hello!", result);
    }

    [Fact]
    public async Task ProcessStreamingAsync_YieldsChunks()
    {
        // Arrange
        var mockChatClient = new Mock<IChatClient>();
        var updates = new[]
        {
            new StreamingChatCompletionUpdate { Text = "Hello" },
            new StreamingChatCompletionUpdate { Text " World" },
            new StreamingChatCompletionUpdate { Text = "!" }
        };

        mockChatClient
            .Setup(x => x.CompleteStreamingAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(updates.ToAsyncEnumerable());

        var config = Options.Create(new AgentConfiguration());
        var logger = new Mock<ILogger<AgentService>>();
        
        var service = new AgentService(mockChatClient.Object, config, logger.Object);

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in service.ProcessStreamingAsync("Hi"))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Equal("Hello World!", string.Join("", chunks));
    }
}
```

### Integration Testing

```csharp
public class AgentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AgentIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Chat_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new { message = "Hello, agent!" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agent/chat", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AgentResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }
}
```

---

## Best Practices

### System Prompts

```csharp
public static class AgentPrompts
{
    public const string CustomerSupport = """
        You are a customer support agent for TechCorp.
        
        Guidelines:
        - Be polite, professional, and empathetic
        - Answer questions about our products and services
        - If you don't know the answer, say so and offer to escalate
        - Never make up information about prices or policies
        - Keep responses concise but helpful
        
        Available products: Widget Pro, Widget Basic, Widget Enterprise
        Support hours: Monday-Friday, 9 AM - 6 PM EST
        """;

    public const string DataAnalyst = """
        You are a data analysis assistant.
        
        Guidelines:
        - Help users understand and analyze data
        - Provide clear explanations of statistical concepts
        - Suggest appropriate visualizations
        - Be precise with numbers and calculations
        - Always explain your reasoning
        """;

    public const string CodeReviewer = """
        You are a code review assistant.
        
        Guidelines:
        - Review code for bugs, security issues, and best practices
        - Suggest improvements with clear explanations
        - Be constructive and educational
        - Follow language-specific conventions
        - Consider performance implications
        """;
}
```

### Token Management

```csharp
public class TokenAwareAgent
{
    private const int MaxContextTokens = 8000;
    private const int ReservedResponseTokens = 2000;

    public IList<ChatMessage> TrimHistoryToFit(IList<ChatMessage> history)
    {
        var availableTokens = MaxContextTokens - ReservedResponseTokens;
        var estimatedTokens = EstimateTokens(history);

        if (estimatedTokens <= availableTokens)
            return history;

        var trimmedHistory = new List<ChatMessage>();
        
        // Always keep system message
        var systemMessage = history.FirstOrDefault(m => m.Role == ChatRole.System);
        if (systemMessage != null)
            trimmedHistory.Add(systemMessage);

        // Keep most recent messages
        var recentMessages = history
            .Where(m => m.Role != ChatRole.System)
            .TakeLast(10);

        trimmedHistory.AddRange(recentMessages);

        return trimmedHistory;
    }

    private int EstimateTokens(IList<ChatMessage> messages)
    {
        // Rough estimation: ~4 characters per token
        return messages.Sum(m => (m.Text?.Length ?? 0) / 4);
    }
}
```

---

## Related Templates

- [Chat Completion](template-sk-chat-completion.md) - Semantic Kernel alternative
- [Graph Workflows](template-agent-framework-workflows.md) - Complex agent workflows
- [Multi-Agent](template-agent-framework-multi-agent.md) - Agent orchestration

---

## External References

- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [.NET 9 AI Features](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)

