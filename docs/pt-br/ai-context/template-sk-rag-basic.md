# Template RAG Básico - Semantic Kernel

> **Propósito**: Este template fornece padrões e diretrizes de implementação para Retrieval-Augmented Generation (RAG) usando Microsoft Semantic Kernel.

---

## Visão Geral

RAG (Retrieval-Augmented Generation) combina recuperação de documentos com geração de IA para fornecer respostas precisas e contextuais. Este template cobre:
- Configuração de vector store
- Chunking e embedding de documentos
- Busca semântica
- Injeção de contexto

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Q&A sobre documentos | ✅ Recomendado |
| Chatbot de base de conhecimento | ✅ Recomendado |
| Busca em documentos | ✅ Recomendado |
| Conversa geral | ⚠️ Use Chat Completion |
| Raciocínio complexo | ⚠️ Considere Chain of Thought |

---

## Pacotes NuGet Necessários

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.InMemory" Version="1.*-*" />
  <!-- OU para produção -->
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.Qdrant" Version="1.*-*" />
</ItemGroup>
```

---

## Configuração

```json
{
  "AI": {
    "OpenAI": {
      "ApiKey": "${OPENAI_API_KEY}",
      "ModelId": "gpt-4o",
      "EmbeddingModelId": "text-embedding-3-small"
    }
  },
  "RAG": {
    "ChunkSize": 1000,
    "ChunkOverlap": 200,
    "TopK": 5
  }
}
```

---

## Modelo de Documento

```csharp
using Microsoft.Extensions.VectorData;

public class DocumentRecord
{
    [VectorStoreRecordKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string Source { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public string Content { get; set; } = string.Empty;

    [VectorStoreRecordVector(1536)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
```

---

## Serviço de Chunking

```csharp
public class SimpleDocumentChunker
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public IEnumerable<DocumentChunk> ChunkDocument(string content, string documentId, string source)
    {
        var position = 0;
        var chunkIndex = 0;

        while (position < content.Length)
        {
            var endPosition = Math.Min(position + _chunkSize, content.Length);
            var chunkContent = content[position..endPosition].Trim();

            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                yield return new DocumentChunk
                {
                    Id = $"{documentId}-{chunkIndex}",
                    DocumentId = documentId,
                    Source = source,
                    Content = chunkContent
                };
                chunkIndex++;
            }

            position = endPosition - _chunkOverlap;
            if (position <= 0 || endPosition >= content.Length)
                position = endPosition;
        }
    }
}
```

---

## Serviço RAG

```csharp
public class RAGService
{
    private readonly Kernel _kernel;
    private readonly IVectorStoreService _vectorStore;
    private readonly RAGConfiguration _config;

    public async Task<RAGResponse> QueryWithSourcesAsync(string question, CancellationToken cancellationToken = default)
    {
        // 1. Recuperar documentos relevantes
        var documents = await _vectorStore.SearchAsync(question, _config.TopK, cancellationToken);

        if (!documents.Any())
            return new RAGResponse { Answer = "Não encontrei informações relevantes." };

        // 2. Construir contexto
        var context = BuildContext(documents);

        // 3. Gerar resposta usando contexto
        var prompt = BuildRAGPrompt(question, context);
        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);

        return new RAGResponse
        {
            Answer = result.GetValue<string>() ?? string.Empty,
            Sources = documents.Select(d => new SourceReference
            {
                Source = d.Source,
                Content = d.Content.Length > 200 ? d.Content[..200] + "..." : d.Content
            }).ToList()
        };
    }

    private string BuildRAGPrompt(string question, string context)
    {
        return $"""
            Você é um assistente que responde perguntas baseado no contexto fornecido.
            
            Contexto:
            {context}
            
            Instruções:
            - Responda APENAS com base nas informações do contexto acima
            - Se o contexto não contém informação suficiente, diga isso
            - Seja conciso mas completo
            - Se citar do contexto, indique a fonte
            
            Pergunta: {question}
            
            Resposta:
            """;
    }
}
```

---

## Integração com Web API

```csharp
[ApiController]
[Route("api/[controller]")]
public class RAGController : ControllerBase
{
    private readonly RAGService _ragService;

    [HttpPost("query")]
    public async Task<IActionResult> Query([FromBody] QueryRequest request, CancellationToken cancellationToken)
    {
        var response = await _ragService.QueryWithSourcesAsync(request.Question, cancellationToken);
        return Ok(response);
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] IngestRequest request, CancellationToken cancellationToken)
    {
        await _ragService.IngestDocumentAsync(request.Content, request.Source, cancellationToken);
        return Ok(new { message = "Documento ingerido com sucesso" });
    }
}
```

---

## Boas Práticas

1. **Preparação de Documentos**: Remova formatação desnecessária
2. **Chunking Estruturado**: Respeite estrutura do documento (parágrafos, seções)
3. **Overlap**: Use overlap entre chunks para manter contexto
4. **Metadados**: Inclua metadados relevantes para filtragem
5. **Batch Embeddings**: Gere embeddings em lotes para eficiência

---

## Templates Relacionados

- [Chat Completion](template-sk-chat-completion.md) - Funcionalidade básica de chat
- [Plugins & Functions](template-sk-plugins.md) - Integração de ferramentas
- [Document Pipeline](template-skg-document-pipeline.md) - Processamento avançado de documentos

---

## Referências Externas

- [Semantic Kernel Memory](https://learn.microsoft.com/semantic-kernel/memories)
- [Vector Stores](https://learn.microsoft.com/semantic-kernel/memories/vector-stores)
- [RAG Best Practices](https://learn.microsoft.com/azure/ai-services/openai/concepts/retrieval-augmented-generation)

