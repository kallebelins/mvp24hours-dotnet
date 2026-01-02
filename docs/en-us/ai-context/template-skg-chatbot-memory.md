# Chatbot with Memory Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for building chatbots with persistent memory using Semantic Kernel Graph.

---

## Overview

Chatbots with memory maintain conversation context and user information across interactions. This template covers:
- Short-term conversation memory
- Long-term user memory
- Context management
- Intent recognition
- Personalized responses

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Multi-turn conversations | ✅ Recommended |
| Personalized assistants | ✅ Recommended |
| Customer support bots | ✅ Recommended |
| Context-aware responses | ✅ Recommended |
| Single Q&A | ⚠️ Use Chat Completion |
| Tool-heavy workflows | ⚠️ Use ReAct Agent |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.InMemory" Version="1.*-*" />
</ItemGroup>
```

---

## Memory Architecture

```
┌─────────────────────────────────────────┐
│           Memory Service                 │
├─────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────────┐ │
│  │ Short-term   │  │    Long-term     │ │
│  │ (Session)    │  │    (User)        │ │
│  ├──────────────┤  ├──────────────────┤ │
│  │ - Messages   │  │ - User Profile   │ │
│  │ - Context    │  │ - Preferences    │ │
│  │ - Intent     │  │ - History        │ │
│  └──────────────┘  └──────────────────┘ │
└─────────────────────────────────────────┘
```

---

## Implementation Patterns

### 1. Memory Service

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

public interface IChatMemoryService
{
    Task<ConversationContext> GetContextAsync(string conversationId, CancellationToken cancellationToken = default);
    Task SaveMessageAsync(string conversationId, ChatMessage message, CancellationToken cancellationToken = default);
    Task<UserProfile> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task SaveUserProfileAsync(string userId, UserProfile profile, CancellationToken cancellationToken = default);
    Task ClearConversationAsync(string conversationId, CancellationToken cancellationToken = default);
}

public class InMemoryChatMemoryService : IChatMemoryService
{
    private readonly Dictionary<string, ConversationContext> _conversations = new();
    private readonly Dictionary<string, UserProfile> _userProfiles = new();
    private readonly int _maxMessages;

    public InMemoryChatMemoryService(int maxMessages = 20)
    {
        _maxMessages = maxMessages;
    }

    public Task<ConversationContext> GetContextAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (!_conversations.TryGetValue(conversationId, out var context))
        {
            context = new ConversationContext { ConversationId = conversationId };
            _conversations[conversationId] = context;
        }
        return Task.FromResult(context);
    }

    public Task SaveMessageAsync(string conversationId, ChatMessage message, CancellationToken cancellationToken = default)
    {
        var context = _conversations.GetValueOrDefault(conversationId) ?? new ConversationContext { ConversationId = conversationId };
        
        context.Messages.Add(message);
        context.LastUpdated = DateTime.UtcNow;
        
        // Trim old messages
        if (context.Messages.Count > _maxMessages)
        {
            context.Messages = context.Messages.TakeLast(_maxMessages).ToList();
        }

        _conversations[conversationId] = context;
        return Task.CompletedTask;
    }

    public Task<UserProfile> GetUserProfileAsync(string userId, CancellationToken cancellationToken = default)
    {
        var profile = _userProfiles.GetValueOrDefault(userId) ?? new UserProfile { UserId = userId };
        return Task.FromResult(profile);
    }

    public Task SaveUserProfileAsync(string userId, UserProfile profile, CancellationToken cancellationToken = default)
    {
        _userProfiles[userId] = profile;
        return Task.CompletedTask;
    }

    public Task ClearConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        _conversations.Remove(conversationId);
        return Task.CompletedTask;
    }
}

public class ConversationContext
{
    public string ConversationId { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? DetectedIntent { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public Dictionary<string, string> Preferences { get; set; } = new();
    public List<string> Interests { get; set; } = new();
    public List<string> PastTopics { get; set; } = new();
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
```

### 2. Basic Chatbot with Memory

```csharp
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;

public class MemoryChatbotBuilder
{
    public static GraphExecutor CreateChatbot(Kernel kernel, IChatMemoryService memoryService)
    {
        var executor = new GraphExecutor("MemoryChatbot", "Chatbot with conversation memory");

        // Load context node
        var loadContextNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var conversationId = args["conversation_id"]?.ToString() ?? "default";
                    var context = await memoryService.GetContextAsync(conversationId);
                    
                    args["conversation_context"] = context;
                    args["message_history"] = FormatHistory(context.Messages);
                    
                    return $"Loaded context with {context.Messages.Count} messages";
                },
                functionName: "LoadContext",
                description: "Loads conversation context"),
            "load-context");

        // Intent detection node
        var intentNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Analyze the user's message and determine their intent.

                Message: {{$user_message}}

                Possible intents:
                - greeting: User is saying hello
                - question: User is asking a question
                - request: User wants something done
                - feedback: User is providing feedback
                - farewell: User is saying goodbye
                - other: None of the above

                Respond with only the intent name:
                """,
                functionName: "DetectIntent",
                description: "Detects user intent"),
            "detect-intent")
            .StoreResultAs("detected_intent");

        // Generate response node
        var responseNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                You are a helpful, friendly assistant with memory of the conversation.

                Conversation history:
                {{$message_history}}

                User's current message: {{$user_message}}
                Detected intent: {{$detected_intent}}

                Respond naturally, referencing previous context when relevant.
                Be concise but helpful.

                Response:
                """,
                functionName: "GenerateResponse",
                description: "Generates contextual response"),
            "generate-response")
            .StoreResultAs("response");

        // Save context node
        var saveContextNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var conversationId = args["conversation_id"]?.ToString() ?? "default";
                    var userMessage = args["user_message"]?.ToString() ?? "";
                    var response = args.GetOrCreateGraphState().GetValue<string>("response") ?? "";

                    await memoryService.SaveMessageAsync(conversationId, new ChatMessage
                    {
                        Role = "user",
                        Content = userMessage
                    });

                    await memoryService.SaveMessageAsync(conversationId, new ChatMessage
                    {
                        Role = "assistant",
                        Content = response
                    });

                    return "Context saved";
                },
                functionName: "SaveContext",
                description: "Saves conversation context"),
            "save-context");

        // Build graph
        executor.AddNode(loadContextNode);
        executor.AddNode(intentNode);
        executor.AddNode(responseNode);
        executor.AddNode(saveContextNode);

        executor.SetStartNode(loadContextNode.NodeId);
        executor.Connect(loadContextNode.NodeId, intentNode.NodeId);
        executor.Connect(intentNode.NodeId, responseNode.NodeId);
        executor.Connect(responseNode.NodeId, saveContextNode.NodeId);

        return executor;
    }

    private static string FormatHistory(List<ChatMessage> messages)
    {
        if (!messages.Any())
            return "(No previous conversation)";

        return string.Join("\n", messages.TakeLast(10).Select(m => 
            $"{(m.Role == "user" ? "User" : "Assistant")}: {m.Content}"));
    }
}
```

### 3. Advanced Chatbot with User Profiles

```csharp
public class ProfileAwareChatbot
{
    private readonly Kernel _kernel;
    private readonly IChatMemoryService _memoryService;

    public ProfileAwareChatbot(Kernel kernel, IChatMemoryService memoryService)
    {
        _kernel = kernel;
        _memoryService = memoryService;
    }

    public GraphExecutor CreateChatbot()
    {
        var executor = new GraphExecutor("ProfileChatbot", "Chatbot with user profiles");

        // Load user profile and context
        var loadDataNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var userId = args["user_id"]?.ToString() ?? "anonymous";
                    var conversationId = args["conversation_id"]?.ToString() ?? $"conv_{userId}";

                    var profile = await _memoryService.GetUserProfileAsync(userId);
                    var context = await _memoryService.GetContextAsync(conversationId);

                    // Update last seen
                    profile.LastSeen = DateTime.UtcNow;

                    args["user_profile"] = profile;
                    args["conversation_context"] = context;
                    args["message_history"] = FormatHistory(context.Messages);
                    args["user_info"] = FormatUserInfo(profile);

                    return $"Loaded profile for {profile.Name ?? "User"}";
                },
                functionName: "LoadData",
                description: "Loads user data"),
            "load-data");

        // Extract information from message
        var extractInfoNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt(
                """
                Extract any personal information from the user's message.

                Message: {{$user_message}}

                Look for:
                - Name (if they introduce themselves)
                - Interests mentioned
                - Preferences expressed
                - Topics discussed

                Format as JSON:
                {"name": null, "interests": [], "preferences": {}, "topics": []}
                """,
                functionName: "ExtractInfo",
                description: "Extracts user information"),
            "extract-info")
            .StoreResultAs("extracted_info");

        // Update profile
        var updateProfileNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var userId = args["user_id"]?.ToString() ?? "anonymous";
                    var profile = (UserProfile)args["user_profile"]!;
                    var extractedInfo = args.GetOrCreateGraphState().GetValue<string>("extracted_info") ?? "{}";

                    try
                    {
                        var info = System.Text.Json.JsonSerializer.Deserialize<ExtractedInfo>(extractedInfo);
                        
                        if (!string.IsNullOrEmpty(info?.Name))
                            profile.Name = info.Name;
                        
                        if (info?.Interests?.Any() == true)
                            profile.Interests = profile.Interests.Union(info.Interests).Distinct().ToList();
                        
                        if (info?.Topics?.Any() == true)
                            profile.PastTopics = profile.PastTopics.Union(info.Topics).Distinct().TakeLast(20).ToList();

                        await _memoryService.SaveUserProfileAsync(userId, profile);
                    }
                    catch { /* Ignore parsing errors */ }

                    return "Profile updated";
                },
                functionName: "UpdateProfile",
                description: "Updates user profile"),
            "update-profile");

        // Generate personalized response
        var responseNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt(
                """
                You are a helpful, personalized assistant.

                User Information:
                {{$user_info}}

                Conversation History:
                {{$message_history}}

                Current Message: {{$user_message}}

                Generate a personalized response that:
                - Uses the user's name if known
                - References their interests when relevant
                - Maintains conversation context
                - Is friendly and helpful

                Response:
                """,
                functionName: "PersonalizedResponse",
                description: "Generates personalized response"),
            "response")
            .StoreResultAs("response");

        // Save conversation
        var saveNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var conversationId = args["conversation_id"]?.ToString() ?? "default";
                    var userMessage = args["user_message"]?.ToString() ?? "";
                    var response = args.GetOrCreateGraphState().GetValue<string>("response") ?? "";

                    await _memoryService.SaveMessageAsync(conversationId, 
                        new ChatMessage { Role = "user", Content = userMessage });
                    await _memoryService.SaveMessageAsync(conversationId, 
                        new ChatMessage { Role = "assistant", Content = response });

                    return "Saved";
                },
                functionName: "Save",
                description: "Saves conversation"),
            "save");

        // Build graph
        executor.AddNode(loadDataNode);
        executor.AddNode(extractInfoNode);
        executor.AddNode(updateProfileNode);
        executor.AddNode(responseNode);
        executor.AddNode(saveNode);

        executor.SetStartNode(loadDataNode.NodeId);
        executor.Connect(loadDataNode.NodeId, extractInfoNode.NodeId);
        executor.Connect(extractInfoNode.NodeId, updateProfileNode.NodeId);
        executor.Connect(updateProfileNode.NodeId, responseNode.NodeId);
        executor.Connect(responseNode.NodeId, saveNode.NodeId);

        return executor;
    }

    private static string FormatHistory(List<ChatMessage> messages)
    {
        if (!messages.Any())
            return "(Starting new conversation)";

        return string.Join("\n", messages.TakeLast(10).Select(m =>
            $"{(m.Role == "user" ? "User" : "Assistant")}: {m.Content}"));
    }

    private static string FormatUserInfo(UserProfile profile)
    {
        var info = new List<string>();
        
        if (!string.IsNullOrEmpty(profile.Name))
            info.Add($"Name: {profile.Name}");
        
        if (profile.Interests.Any())
            info.Add($"Interests: {string.Join(", ", profile.Interests)}");
        
        if (profile.PastTopics.Any())
            info.Add($"Recent topics: {string.Join(", ", profile.PastTopics.TakeLast(5))}");

        return info.Any() 
            ? string.Join("\n", info) 
            : "(New user, no information yet)";
    }

    private class ExtractedInfo
    {
        public string? Name { get; set; }
        public List<string>? Interests { get; set; }
        public Dictionary<string, string>? Preferences { get; set; }
        public List<string>? Topics { get; set; }
    }
}
```

### 4. Chatbot Service Layer

```csharp
public interface IChatbotService
{
    Task<ChatResponse> SendMessageAsync(string userId, string message, CancellationToken cancellationToken = default);
    Task<ConversationContext> GetConversationAsync(string conversationId, CancellationToken cancellationToken = default);
    Task ClearConversationAsync(string conversationId, CancellationToken cancellationToken = default);
}

public class ChatbotService : IChatbotService
{
    private readonly Kernel _kernel;
    private readonly IChatMemoryService _memoryService;
    private readonly GraphExecutor _chatbot;

    public ChatbotService(Kernel kernel, IChatMemoryService memoryService)
    {
        _kernel = kernel;
        _memoryService = memoryService;
        
        var chatbotBuilder = new ProfileAwareChatbot(kernel, memoryService);
        _chatbot = chatbotBuilder.CreateChatbot();
    }

    public async Task<ChatResponse> SendMessageAsync(
        string userId, 
        string message, 
        CancellationToken cancellationToken = default)
    {
        var conversationId = $"conv_{userId}";

        var arguments = new KernelArguments
        {
            ["user_id"] = userId,
            ["conversation_id"] = conversationId,
            ["user_message"] = message
        };

        await _chatbot.ExecuteAsync(_kernel, arguments, cancellationToken);

        var state = arguments.GetOrCreateGraphState();
        var response = state.GetValue<string>("response") ?? "I couldn't process that. Please try again.";
        var intent = state.GetValue<string>("detected_intent") ?? "unknown";

        return new ChatResponse
        {
            Message = response,
            Intent = intent,
            ConversationId = conversationId,
            Timestamp = DateTime.UtcNow
        };
    }

    public Task<ConversationContext> GetConversationAsync(
        string conversationId, 
        CancellationToken cancellationToken = default)
    {
        return _memoryService.GetContextAsync(conversationId, cancellationToken);
    }

    public Task ClearConversationAsync(
        string conversationId, 
        CancellationToken cancellationToken = default)
    {
        return _memoryService.ClearConversationAsync(conversationId, cancellationToken);
    }
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

---

## Dependency Injection Setup

```csharp
public static class ChatbotServiceExtensions
{
    public static IServiceCollection AddChatbotServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Kernel
        services.AddSingleton(sp =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddGraphSupport();
            builder.AddOpenAIChatCompletion(
                modelId: configuration["AI:ModelId"] ?? "gpt-4o",
                apiKey: configuration["AI:ApiKey"]!);
            return builder.Build();
        });

        // Register memory service
        services.AddSingleton<IChatMemoryService>(sp =>
        {
            var maxMessages = configuration.GetValue<int>("Chatbot:MaxMessages", 20);
            return new InMemoryChatMemoryService(maxMessages);
        });

        // Register chatbot service
        services.AddScoped<IChatbotService, ChatbotService>();

        return services;
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatbotService _chatbotService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatbotService chatbotService, ILogger<ChatController> logger)
    {
        _chatbotService = chatbotService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(
        [FromBody] ChatRequest request,
        [FromHeader(Name = "X-User-Id")] string? userId,
        CancellationToken cancellationToken)
    {
        userId ??= "anonymous";
        
        var response = await _chatbotService.SendMessageAsync(
            userId, 
            request.Message, 
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversation(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var context = await _chatbotService.GetConversationAsync(conversationId, cancellationToken);
        return Ok(context);
    }

    [HttpDelete("conversation/{conversationId}")]
    public async Task<IActionResult> ClearConversation(
        string conversationId,
        CancellationToken cancellationToken)
    {
        await _chatbotService.ClearConversationAsync(conversationId, cancellationToken);
        return NoContent();
    }

    [HttpPost("stream")]
    public async Task StreamMessage(
        [FromBody] ChatRequest request,
        [FromHeader(Name = "X-User-Id")] string? userId,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");

        // For streaming, you'd need to modify the chatbot to support streaming
        var response = await _chatbotService.SendMessageAsync(
            userId ?? "anonymous",
            request.Message,
            cancellationToken);

        await Response.WriteAsync($"data: {response.Message}\n\n", cancellationToken);
        await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
    }
}

public record ChatRequest(string Message);
```

---

## Testing

```csharp
using Xunit;

public class ChatbotTests
{
    [Fact]
    public async Task Chatbot_RemembersUserName()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var memoryService = new InMemoryChatMemoryService();
        var chatbot = MemoryChatbotBuilder.CreateChatbot(kernel, memoryService);

        // Act - First message
        var args1 = new KernelArguments
        {
            ["user_id"] = "user1",
            ["conversation_id"] = "conv1",
            ["user_message"] = "Hi, my name is John"
        };
        await chatbot.ExecuteAsync(kernel, args1);

        // Act - Second message
        var args2 = new KernelArguments
        {
            ["user_id"] = "user1",
            ["conversation_id"] = "conv1",
            ["user_message"] = "What's my name?"
        };
        await chatbot.ExecuteAsync(kernel, args2);
        var response = args2.GetOrCreateGraphState().GetValue<string>("response");

        // Assert
        Assert.Contains("John", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Chatbot_MaintainsConversationContext()
    {
        // Arrange
        var memoryService = new InMemoryChatMemoryService();
        
        // Act
        await memoryService.SaveMessageAsync("conv1", 
            new ChatMessage { Role = "user", Content = "Hello" });
        await memoryService.SaveMessageAsync("conv1", 
            new ChatMessage { Role = "assistant", Content = "Hi there!" });

        var context = await memoryService.GetContextAsync("conv1");

        // Assert
        Assert.Equal(2, context.Messages.Count);
    }

    private Kernel CreateTestKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddGraphSupport();
        return builder.Build();
    }
}
```

---

## Best Practices

### Memory Management

1. **Limit History**: Keep only relevant recent messages
2. **Summarize**: Periodically summarize long conversations
3. **Clean Up**: Remove stale conversations
4. **Selective Storage**: Only store important information

### Personalization

1. **Gradual Learning**: Build user profiles over time
2. **Explicit Consent**: Be transparent about memory
3. **Privacy**: Allow users to clear their data
4. **Relevance**: Reference past context appropriately

### Performance

1. **Efficient Storage**: Use appropriate storage for your scale
2. **Lazy Loading**: Load context on demand
3. **Caching**: Cache frequently accessed profiles
4. **Async Operations**: Keep all I/O async

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [Chat Completion](template-sk-chat-completion.md) - Basic chat
- [RAG Basic](template-sk-rag-basic.md) - Knowledge retrieval
- [Multi-Agent](template-skg-multi-agent.md) - Coordinated agents

---

## External References

- [Semantic Kernel Memory](https://learn.microsoft.com/semantic-kernel/memories)
- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [Conversation Design](https://designguidelines.withgoogle.com/conversation/)

