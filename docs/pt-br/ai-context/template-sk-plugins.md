# Template de Plugins & Functions - Semantic Kernel

> **Propósito**: Este template fornece padrões e diretrizes de implementação para criar e usar plugins com Microsoft Semantic Kernel para IA aumentada com ferramentas.

---

## Visão Geral

Plugins estendem as capacidades da IA fornecendo ferramentas que podem ser chamadas durante conversas. Este template cobre:
- Criação de plugins nativos
- Semantic functions
- Function calling com IA
- Composição de plugins

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| IA precisa acessar dados externos | ✅ Recomendado |
| IA precisa fazer cálculos | ✅ Recomendado |
| IA precisa chamar APIs | ✅ Recomendado |
| IA precisa interagir com banco de dados | ✅ Recomendado |
| Apenas geração de texto simples | ⚠️ Use Chat Completion |

---

## Pacotes NuGet Necessários

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Plugins.Core" Version="1.*-*" />
</ItemGroup>
```

---

## Tipos de Plugin

### 1. Plugins Nativos (Classes C#)

```csharp
using Microsoft.SemanticKernel;
using System.ComponentModel;

public class WeatherPlugin
{
    [KernelFunction("GetCurrentWeather")]
    [Description("Obtém o clima atual para uma cidade especificada")]
    public string GetCurrentWeather(
        [Description("Nome da cidade")] string city)
    {
        return city.ToLowerInvariant() switch
        {
            "são paulo" => "Ensolarado, 28°C",
            "rio de janeiro" => "Parcialmente nublado, 32°C",
            "curitiba" => "Nublado, 18°C, possibilidade de chuva",
            _ => $"Clima para {city}: Ensolarado, 25°C"
        };
    }
}

public class CalculatorPlugin
{
    [KernelFunction("Add")]
    [Description("Soma dois números")]
    public double Add(
        [Description("Primeiro número")] double a,
        [Description("Segundo número")] double b) => a + b;

    [KernelFunction("Multiply")]
    [Description("Multiplica dois números")]
    public double Multiply(
        [Description("Primeiro número")] double a,
        [Description("Segundo número")] double b) => a * b;
}
```

### 2. Registro de Plugins

```csharp
public static class PluginConfiguration
{
    public static Kernel ConfigurePlugins(Kernel kernel)
    {
        kernel.ImportPluginFromObject(new WeatherPlugin(), "Weather");
        kernel.ImportPluginFromObject(new CalculatorPlugin(), "Calculator");
        return kernel;
    }
}
```

---

## Auto Function Calling

```csharp
using Microsoft.SemanticKernel.Connectors.OpenAI;

public class AutoFunctionCallingService
{
    private readonly Kernel _kernel;

    public async Task<string> ProcessWithToolsAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("Você é um assistente com acesso a ferramentas de clima e cálculo.");
        chatHistory.AddUserMessage(userMessage);

        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory, settings, _kernel, cancellationToken);

        return response.Content ?? string.Empty;
    }
}
```

---

## Integração com Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class AssistantController : ControllerBase
{
    private readonly Kernel _kernel;

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("Você é um assistente com acesso a ferramentas.");
        chatHistory.AddUserMessage(request.Message);

        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();
        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory, settings, _kernel, cancellationToken);

        return Ok(new { message = response.Content });
    }
}
```

---

## Boas Práticas

1. **Descrições Claras**: Escreva descrições detalhadas para funções e parâmetros
2. **Funções Focadas**: Cada função deve fazer uma coisa bem
3. **Nomes Consistentes**: Use padrão verbo-substantivo (GetWeather, CalculateSum)
4. **Mensagens de Erro**: Retorne mensagens úteis, não exceções
5. **Segurança**: Valide entradas e limite escopo de ações

---

## Templates Relacionados

- [Chat Completion](template-sk-chat-completion.md) - Funcionalidade básica de chat
- [RAG Básico](template-sk-rag-basic.md) - Recuperação de documentos
- [ReAct Agent](template-skg-react-agent.md) - Raciocínio com ferramentas

---

## Referências Externas

- [Semantic Kernel Plugins](https://learn.microsoft.com/semantic-kernel/agents/plugins)
- [Function Calling](https://learn.microsoft.com/semantic-kernel/agents/plugins/using-ai-functions)

