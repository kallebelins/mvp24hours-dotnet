# Template Chatbot com Memória - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para implementar chatbots com memória de curto e longo prazo usando grafos.

---

## Visão Geral

Chatbot com memória permite conversas contextuais e persistentes:
- Memória de curto prazo (sessão)
- Memória de longo prazo (persistida)
- Reconhecimento de intenção
- Contexto de conversa

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Assistente personalizado | ✅ Recomendado |
| Conversas multi-turno | ✅ Recomendado |
| Suporte ao cliente | ✅ Recomendado |
| Q&A sem contexto | ⚠️ Use Chat Completion |

---

## Implementação

```csharp
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Memory;

public class MemoryChatbot
{
    private readonly GraphExecutor _graph;
    private readonly GraphMemoryService _memory;
    private readonly Kernel _kernel;

    public MemoryChatbot(Kernel kernel)
    {
        _kernel = kernel;
        _memory = new GraphMemoryService();
        _graph = BuildChatGraph();
    }

    private GraphExecutor BuildChatGraph()
    {
        var graph = new GraphExecutor("MemoryChatbot", "Chatbot com memória");

        // Nó de recuperação de contexto
        var contextNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (string userId, string message) =>
                {
                    var shortTerm = await _memory.GetShortTermAsync(userId);
                    var longTerm = await _memory.GetRelevantLongTermAsync(userId, message);
                    return $"Histórico recente:\n{shortTerm}\n\nInformações relevantes:\n{longTerm}";
                },
                "GetContext"
            ),
            nodeId: "get_context"
        ).StoreResultAs("context");

        // Nó de reconhecimento de intenção
        var intentNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Identifique a intenção do usuário:
                
                Mensagem: {{$message}}
                Contexto: {{$context}}
                
                Intenções possíveis: QUESTION, COMMAND, SMALL_TALK, FEEDBACK
                
                Intenção:
                """),
            nodeId: "detect_intent"
        ).StoreResultAs("intent");

        // Nó de geração de resposta
        var responseNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Responda ao usuário considerando o contexto:
                
                Mensagem: {{$message}}
                Intenção: {{$intent}}
                Contexto: {{$context}}
                
                Resposta:
                """),
            nodeId: "generate_response"
        ).StoreResultAs("response");

        // Nó de atualização de memória
        var updateMemoryNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (string userId, string message, string response) =>
                {
                    await _memory.AddToShortTermAsync(userId, $"User: {message}\nAssistant: {response}");
                    
                    // Extrair informações importantes para memória de longo prazo
                    var important = await ExtractImportantInfoAsync(message, response);
                    if (!string.IsNullOrEmpty(important))
                        await _memory.AddToLongTermAsync(userId, important);
                    
                    return "Memory updated";
                },
                "UpdateMemory"
            ),
            nodeId: "update_memory"
        );

        graph.AddNode(contextNode);
        graph.AddNode(intentNode);
        graph.AddNode(responseNode);
        graph.AddNode(updateMemoryNode);

        graph.Connect("get_context", "detect_intent");
        graph.Connect("detect_intent", "generate_response");
        graph.Connect("generate_response", "update_memory");

        graph.SetStartNode("get_context");
        return graph;
    }

    public async Task<string> ChatAsync(string userId, string message)
    {
        var arguments = new KernelArguments
        {
            ["userId"] = userId,
            ["message"] = message
        };

        var result = await _graph.ExecuteAsync(_kernel, arguments);
        return result.State.GetValue<string>("response");
    }
}

public class GraphMemoryService
{
    private readonly Dictionary<string, List<string>> _shortTerm = new();
    private readonly Dictionary<string, List<string>> _longTerm = new();
    private const int MaxShortTermMessages = 10;

    public Task<string> GetShortTermAsync(string userId)
    {
        if (!_shortTerm.TryGetValue(userId, out var messages))
            return Task.FromResult(string.Empty);
        return Task.FromResult(string.Join("\n", messages.TakeLast(MaxShortTermMessages)));
    }

    public Task<string> GetRelevantLongTermAsync(string userId, string query)
    {
        if (!_longTerm.TryGetValue(userId, out var memories))
            return Task.FromResult(string.Empty);
        // Em produção, use busca semântica
        return Task.FromResult(string.Join("\n", memories.TakeLast(5)));
    }

    public Task AddToShortTermAsync(string userId, string content)
    {
        if (!_shortTerm.ContainsKey(userId))
            _shortTerm[userId] = new List<string>();
        _shortTerm[userId].Add(content);
        return Task.CompletedTask;
    }

    public Task AddToLongTermAsync(string userId, string content)
    {
        if (!_longTerm.ContainsKey(userId))
            _longTerm[userId] = new List<string>();
        _longTerm[userId].Add(content);
        return Task.CompletedTask;
    }
}
```

---

## Boas Práticas

1. **Limite de Memória**: Defina limites para memória de curto prazo
2. **Busca Semântica**: Use embeddings para memória de longo prazo
3. **Privacidade**: Implemente políticas de retenção de dados
4. **Persistência**: Use banco de dados para produção

---

## Templates Relacionados

- [Chat Completion](template-sk-chat-completion.md) - Chat básico
- [RAG Básico](template-sk-rag-basic.md) - Recuperação de documentos

