# Plugins & Functions Template - Semantic Kernel

> **Purpose**: This template provides AI agents with patterns and implementation guidelines for creating and using plugins with Microsoft Semantic Kernel for tool-augmented AI.

---

## Overview

Plugins extend AI capabilities by providing tools that can be called during conversations. This template covers:
- Native plugin creation
- Semantic functions
- Function calling with AI
- Plugin composition
- Best practices for tool design

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| AI needs to access external data | ✅ Recommended |
| AI needs to perform calculations | ✅ Recommended |
| AI needs to call APIs | ✅ Recommended |
| AI needs to interact with databases | ✅ Recommended |
| Simple text generation only | ⚠️ Use Chat Completion |
| Complex multi-step reasoning | ⚠️ Consider ReAct Agent |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Plugins.Core" Version="1.*-*" />
</ItemGroup>
```

---

## Plugin Types

### 1. Native Plugins (C# Classes)

Native plugins are C# classes with methods decorated with `[KernelFunction]`:

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class WeatherPlugin
{
    [KernelFunction("GetCurrentWeather")]
    [Description("Gets the current weather for a specified city")]
    public string GetCurrentWeather(
        [Description("The city name to get weather for")] string city)
    {
        // In production, call a real weather API
        return city.ToLowerInvariant() switch
        {
            "london" => "Cloudy, 15°C, light rain",
            "paris" => "Sunny, 22°C, clear skies",
            "tokyo" => "Partly cloudy, 18°C, humid",
            "new york" => "Clear, 20°C, moderate wind",
            _ => $"Weather data for {city}: Sunny, 20°C"
        };
    }

    [KernelFunction("GetWeatherForecast")]
    [Description("Gets the weather forecast for the next days")]
    public string GetWeatherForecast(
        [Description("The city name")] string city,
        [Description("Number of days for forecast (1-7)")] int days = 3)
    {
        days = Math.Clamp(days, 1, 7);
        return $"Forecast for {city} ({days} days): Mostly sunny with occasional clouds";
    }
}
```

### 2. Semantic Functions (Prompt-based)

Semantic functions use AI to process inputs:

```csharp
public class SemanticPluginFactory
{
    public static KernelPlugin CreateSummaryPlugin(Kernel kernel)
    {
        var functions = new List<KernelFunction>();

        // Create summarization function
        var summarizeFunction = kernel.CreateFunctionFromPrompt(
            promptTemplate: """
                Summarize the following text in {{$style}} style:
                
                {{$input}}
                
                Summary:
                """,
            functionName: "Summarize",
            description: "Summarizes text in a specified style");

        functions.Add(summarizeFunction);

        // Create translation function
        var translateFunction = kernel.CreateFunctionFromPrompt(
            promptTemplate: """
                Translate the following text to {{$language}}:
                
                {{$input}}
                
                Translation:
                """,
            functionName: "Translate",
            description: "Translates text to a specified language");

        functions.Add(translateFunction);

        return KernelPluginFactory.CreateFromFunctions("TextProcessing", functions);
    }
}
```

### 3. Async Plugins

For operations that require async processing:

```csharp
public class DataPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataPlugin> _logger;

    public DataPlugin(HttpClient httpClient, ILogger<DataPlugin> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    [KernelFunction("FetchData")]
    [Description("Fetches data from a specified URL")]
    public async Task<string> FetchDataAsync(
        [Description("The URL to fetch data from")] string url,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching data from {Url}", url);
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            return response.Length > 1000 
                ? response[..1000] + "... [truncated]" 
                : response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from {Url}", url);
            return $"Error fetching data: {ex.Message}";
        }
    }

    [KernelFunction("SearchDatabase")]
    [Description("Searches the database for records matching the query")]
    public async Task<string> SearchDatabaseAsync(
        [Description("The search query")] string query,
        [Description("Maximum number of results")] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        // Simulate database search
        await Task.Delay(100, cancellationToken);
        return $"Found {Math.Min(maxResults, 5)} results for '{query}'";
    }
}
```

---

## Plugin Registration

### Basic Registration

```csharp
using Microsoft.SemanticKernel;

public static class PluginConfiguration
{
    public static Kernel ConfigurePlugins(Kernel kernel)
    {
        // Register native plugins
        kernel.ImportPluginFromObject(new WeatherPlugin(), "Weather");
        kernel.ImportPluginFromObject(new CalculatorPlugin(), "Calculator");
        kernel.ImportPluginFromObject(new SearchPlugin(), "Search");

        return kernel;
    }
}
```

### Dependency Injection Registration

```csharp
public static class PluginServiceExtensions
{
    public static IServiceCollection AddAIPlugins(this IServiceCollection services)
    {
        // Register plugin instances with DI
        services.AddSingleton<WeatherPlugin>();
        services.AddSingleton<CalculatorPlugin>();
        services.AddScoped<DataPlugin>();

        // Register kernel with plugins
        services.AddSingleton(sp =>
        {
            var kernel = KernelFactory.CreateKernel(sp.GetRequiredService<IConfiguration>());

            kernel.ImportPluginFromObject(sp.GetRequiredService<WeatherPlugin>(), "Weather");
            kernel.ImportPluginFromObject(sp.GetRequiredService<CalculatorPlugin>(), "Calculator");

            return kernel;
        });

        return services;
    }
}
```

### Plugin from Type

```csharp
// Register plugin from type (creates instance internally)
kernel.ImportPluginFromType<WeatherPlugin>("Weather");
kernel.ImportPluginFromType<CalculatorPlugin>("Calculator");
```

---

## Common Plugin Implementations

### Calculator Plugin

```csharp
public class CalculatorPlugin
{
    [KernelFunction("Add")]
    [Description("Adds two numbers")]
    public double Add(
        [Description("First number")] double a,
        [Description("Second number")] double b) => a + b;

    [KernelFunction("Subtract")]
    [Description("Subtracts second number from first")]
    public double Subtract(
        [Description("First number")] double a,
        [Description("Second number")] double b) => a - b;

    [KernelFunction("Multiply")]
    [Description("Multiplies two numbers")]
    public double Multiply(
        [Description("First number")] double a,
        [Description("Second number")] double b) => a * b;

    [KernelFunction("Divide")]
    [Description("Divides first number by second")]
    public double Divide(
        [Description("Dividend")] double a,
        [Description("Divisor")] double b)
    {
        if (b == 0)
            throw new ArgumentException("Cannot divide by zero");
        return a / b;
    }

    [KernelFunction("Percentage")]
    [Description("Calculates percentage of a value")]
    public double Percentage(
        [Description("The value")] double value,
        [Description("The percentage")] double percent) => value * percent / 100;
}
```

### DateTime Plugin

```csharp
public class DateTimePlugin
{
    [KernelFunction("GetCurrentDateTime")]
    [Description("Gets the current date and time")]
    public string GetCurrentDateTime() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    [KernelFunction("GetCurrentDate")]
    [Description("Gets the current date")]
    public string GetCurrentDate() => DateTime.Now.ToString("yyyy-MM-dd");

    [KernelFunction("GetCurrentTime")]
    [Description("Gets the current time")]
    public string GetCurrentTime() => DateTime.Now.ToString("HH:mm:ss");

    [KernelFunction("AddDays")]
    [Description("Adds days to a date")]
    public string AddDays(
        [Description("The date in yyyy-MM-dd format")] string date,
        [Description("Number of days to add")] int days)
    {
        if (DateTime.TryParse(date, out var parsedDate))
            return parsedDate.AddDays(days).ToString("yyyy-MM-dd");
        return "Invalid date format";
    }

    [KernelFunction("GetDayOfWeek")]
    [Description("Gets the day of week for a date")]
    public string GetDayOfWeek(
        [Description("The date in yyyy-MM-dd format")] string date)
    {
        if (DateTime.TryParse(date, out var parsedDate))
            return parsedDate.DayOfWeek.ToString();
        return "Invalid date format";
    }
}
```

### File Plugin

```csharp
public class FilePlugin
{
    private readonly string _basePath;
    private readonly ILogger<FilePlugin> _logger;

    public FilePlugin(IConfiguration configuration, ILogger<FilePlugin> logger)
    {
        _basePath = configuration["FilePlugin:BasePath"] ?? Path.GetTempPath();
        _logger = logger;
    }

    [KernelFunction("ReadFile")]
    [Description("Reads content from a file")]
    public async Task<string> ReadFileAsync(
        [Description("The file name (relative to base path)")] string fileName,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, fileName);
        
        if (!File.Exists(fullPath))
            return $"File not found: {fileName}";

        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);
        return content.Length > 5000 
            ? content[..5000] + "... [truncated]" 
            : content;
    }

    [KernelFunction("ListFiles")]
    [Description("Lists files in the directory")]
    public string ListFiles(
        [Description("File pattern (e.g., *.txt)")] string pattern = "*.*")
    {
        var files = Directory.GetFiles(_basePath, pattern, SearchOption.TopDirectoryOnly);
        return string.Join("\n", files.Select(Path.GetFileName));
    }
}
```

---

## Auto Function Calling

Enable AI to automatically call functions when needed:

```csharp
using Microsoft.SemanticKernel.Connectors.OpenAI;

public class AutoFunctionCallingService
{
    private readonly Kernel _kernel;

    public AutoFunctionCallingService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ProcessWithToolsAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("""
            You are a helpful assistant with access to various tools.
            Use the available functions to help answer user questions.
            Always explain what you're doing when using tools.
            """);
        chatHistory.AddUserMessage(userMessage);

        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel,
            cancellationToken);

        return response.Content ?? string.Empty;
    }
}
```

### Function Choice Behaviors

```csharp
// Auto: Let AI decide when to call functions
var autoSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

// Required: Force AI to call at least one function
var requiredSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
};

// None: Disable function calling
var noneSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.None()
};

// Specific functions only
var specificSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(
        functions: new[]
        {
            kernel.Plugins["Weather"]["GetCurrentWeather"],
            kernel.Plugins["Calculator"]["Add"]
        })
};
```

---

## Manual Function Invocation

```csharp
public class ManualFunctionInvocationService
{
    private readonly Kernel _kernel;

    public ManualFunctionInvocationService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> InvokeFunctionAsync(
        string pluginName,
        string functionName,
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var function = _kernel.Plugins[pluginName][functionName];
        var result = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        return result.GetValue<string>() ?? string.Empty;
    }

    public async Task<string> GetWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        return await InvokeFunctionAsync(
            "Weather",
            "GetCurrentWeather",
            new KernelArguments { ["city"] = city },
            cancellationToken);
    }
}
```

---

## Plugin Composition

Combine multiple plugins for complex operations:

```csharp
public class TravelAssistantPlugin
{
    private readonly WeatherPlugin _weatherPlugin;
    private readonly DateTimePlugin _dateTimePlugin;

    public TravelAssistantPlugin(WeatherPlugin weatherPlugin, DateTimePlugin dateTimePlugin)
    {
        _weatherPlugin = weatherPlugin;
        _dateTimePlugin = dateTimePlugin;
    }

    [KernelFunction("GetTravelInfo")]
    [Description("Gets travel information for a destination")]
    public string GetTravelInfo(
        [Description("Destination city")] string city,
        [Description("Travel date in yyyy-MM-dd format")] string travelDate)
    {
        var weather = _weatherPlugin.GetCurrentWeather(city);
        var dayOfWeek = _dateTimePlugin.GetDayOfWeek(travelDate);

        return $"""
            Travel Information for {city}
            Date: {travelDate} ({dayOfWeek})
            Weather: {weather}
            """;
    }
}
```

---

## Web API Integration

### Plugin-enabled Chat Endpoint

```csharp
[ApiController]
[Route("api/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ILogger<AssistantController> _logger;

    public AssistantController(Kernel kernel, ILogger<AssistantController> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful assistant with access to weather, calculator, and datetime tools.");
        chatHistory.AddUserMessage(request.Message);

        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            settings,
            _kernel,
            cancellationToken);

        return Ok(new { message = response.Content });
    }

    [HttpGet("plugins")]
    public IActionResult GetAvailablePlugins()
    {
        var plugins = _kernel.Plugins.Select(p => new
        {
            Name = p.Name,
            Functions = p.Select(f => new
            {
                Name = f.Name,
                Description = f.Description,
                Parameters = f.Metadata.Parameters.Select(param => new
                {
                    Name = param.Name,
                    Description = param.Description,
                    Type = param.ParameterType?.Name,
                    IsRequired = param.IsRequired
                })
            })
        });

        return Ok(plugins);
    }
}
```

---

## Error Handling in Plugins

```csharp
public class RobustPlugin
{
    private readonly ILogger<RobustPlugin> _logger;

    public RobustPlugin(ILogger<RobustPlugin> logger)
    {
        _logger = logger;
    }

    [KernelFunction("SafeOperation")]
    [Description("Performs a safe operation with error handling")]
    public async Task<string> SafeOperationAsync(
        [Description("The operation parameter")] string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Error: Input cannot be empty";
        }

        try
        {
            // Simulate operation
            await Task.Delay(100, cancellationToken);
            return $"Successfully processed: {input}";
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SafeOperation with input: {Input}", input);
            return $"Error processing request: {ex.Message}";
        }
    }
}
```

---

## Testing Plugins

```csharp
using Xunit;

public class WeatherPluginTests
{
    private readonly WeatherPlugin _plugin;

    public WeatherPluginTests()
    {
        _plugin = new WeatherPlugin();
    }

    [Theory]
    [InlineData("London", "Cloudy")]
    [InlineData("Paris", "Sunny")]
    [InlineData("Tokyo", "Partly cloudy")]
    public void GetCurrentWeather_ReturnsExpectedFormat(string city, string expectedContains)
    {
        // Act
        var result = _plugin.GetCurrentWeather(city);

        // Assert
        Assert.Contains(expectedContains, result);
        Assert.Contains(city, result);
    }

    [Theory]
    [InlineData("Unknown City")]
    [InlineData("")]
    public void GetCurrentWeather_HandlesUnknownCity(string city)
    {
        // Act
        var result = _plugin.GetCurrentWeather(city);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("°C", result); // Should still return weather-like format
    }
}

public class CalculatorPluginTests
{
    private readonly CalculatorPlugin _plugin;

    public CalculatorPluginTests()
    {
        _plugin = new CalculatorPlugin();
    }

    [Theory]
    [InlineData(5, 3, 8)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public void Add_ReturnsCorrectResult(double a, double b, double expected)
    {
        Assert.Equal(expected, _plugin.Add(a, b));
    }

    [Fact]
    public void Divide_ByZero_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => _plugin.Divide(10, 0));
    }
}
```

---

## Best Practices

### Function Design

1. **Clear Descriptions**: Write detailed descriptions for functions and parameters
2. **Focused Functions**: Each function should do one thing well
3. **Consistent Naming**: Use verb-noun naming (GetWeather, CalculateSum)
4. **Error Messages**: Return helpful error messages, not exceptions
5. **Idempotency**: Design functions to be safely re-callable

### Parameter Guidelines

```csharp
// Good: Clear, typed parameters with descriptions
[KernelFunction("SearchProducts")]
[Description("Searches for products matching the criteria")]
public async Task<string> SearchProductsAsync(
    [Description("Product category (e.g., electronics, clothing)")] string category,
    [Description("Minimum price in USD")] decimal? minPrice = null,
    [Description("Maximum price in USD")] decimal? maxPrice = null,
    [Description("Maximum number of results (1-50)")] int maxResults = 10)
{
    maxResults = Math.Clamp(maxResults, 1, 50);
    // Implementation...
}

// Avoid: Vague parameters
[KernelFunction("Search")]
public string Search(string query, string options) // Bad: What is "options"?
{
    // Implementation...
}
```

### Security Considerations

```csharp
public class SecureFilePlugin
{
    private readonly string _allowedBasePath;
    private readonly HashSet<string> _allowedExtensions;

    public SecureFilePlugin(IConfiguration configuration)
    {
        _allowedBasePath = configuration["FilePlugin:AllowedPath"]!;
        _allowedExtensions = new HashSet<string>(
            configuration.GetSection("FilePlugin:AllowedExtensions").Get<string[]>() 
            ?? new[] { ".txt", ".md", ".json" },
            StringComparer.OrdinalIgnoreCase);
    }

    [KernelFunction("ReadFile")]
    [Description("Reads a file from the allowed directory")]
    public async Task<string> ReadFileAsync(
        [Description("File name")] string fileName,
        CancellationToken cancellationToken = default)
    {
        // Validate file extension
        var extension = Path.GetExtension(fileName);
        if (!_allowedExtensions.Contains(extension))
            return $"Error: File type {extension} is not allowed";

        // Prevent path traversal
        var fullPath = Path.GetFullPath(Path.Combine(_allowedBasePath, fileName));
        if (!fullPath.StartsWith(_allowedBasePath))
            return "Error: Invalid file path";

        if (!File.Exists(fullPath))
            return "Error: File not found";

        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }
}
```

---

## Related Templates

- [Chat Completion](template-sk-chat-completion.md) - Basic chat functionality
- [RAG Basic](template-sk-rag-basic.md) - Document retrieval
- [ReAct Agent](template-skg-react-agent.md) - Reasoning with tools

---

## External References

- [Semantic Kernel Plugins](https://learn.microsoft.com/semantic-kernel/agents/plugins)
- [Function Calling](https://learn.microsoft.com/semantic-kernel/agents/plugins/using-ai-functions)
- [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling)

