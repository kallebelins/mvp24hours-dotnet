# RAG Basic Template - Semantic Kernel

> **Purpose**: This template provides AI agents with patterns and implementation guidelines for Retrieval-Augmented Generation (RAG) using Microsoft Semantic Kernel.

---

## Overview

RAG (Retrieval-Augmented Generation) combines document retrieval with AI generation to provide accurate, context-aware responses. This template covers:
- Vector store configuration
- Document chunking and embedding
- Semantic search
- Context injection
- Hybrid search strategies

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Q&A over documents | ✅ Recommended |
| Knowledge base chatbot | ✅ Recommended |
| Document search | ✅ Recommended |
| Code documentation assistant | ✅ Recommended |
| General conversation | ⚠️ Use Chat Completion |
| Complex multi-step reasoning | ⚠️ Consider Chain of Thought |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.*" />
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.Qdrant" Version="1.*-*" />
  <!-- OR for in-memory (development only) -->
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.InMemory" Version="1.*-*" />
  <!-- OR for Azure AI Search -->
  <PackageReference Include="Microsoft.SemanticKernel.Connectors.AzureAISearch" Version="1.*-*" />
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
      "EmbeddingModelId": "text-embedding-3-small"
    },
    "AzureOpenAI": {
      "Endpoint": "${AZURE_OPENAI_ENDPOINT}",
      "ApiKey": "${AZURE_OPENAI_API_KEY}",
      "DeploymentName": "gpt-4o",
      "EmbeddingDeploymentName": "text-embedding-3-small"
    }
  },
  "VectorStore": {
    "Provider": "Qdrant",
    "Qdrant": {
      "Host": "localhost",
      "Port": 6334,
      "CollectionName": "documents"
    },
    "AzureAISearch": {
      "Endpoint": "${AZURE_SEARCH_ENDPOINT}",
      "ApiKey": "${AZURE_SEARCH_API_KEY}",
      "IndexName": "documents"
    }
  },
  "RAG": {
    "ChunkSize": 1000,
    "ChunkOverlap": 200,
    "TopK": 5,
    "MinRelevanceScore": 0.7
  }
}
```

### Configuration Classes

```csharp
public class VectorStoreConfiguration
{
    public string Provider { get; set; } = "InMemory";
    public QdrantConfiguration Qdrant { get; set; } = new();
    public AzureAISearchConfiguration AzureAISearch { get; set; } = new();
}

public class QdrantConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
    public string CollectionName { get; set; } = "documents";
}

public class AzureAISearchConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "documents";
}

public class RAGConfiguration
{
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    public int TopK { get; set; } = 5;
    public double MinRelevanceScore { get; set; } = 0.7;
}
```

---

## Data Models

### Document Record

```csharp
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

public class DocumentRecord
{
    [VectorStoreRecordKey]
    public string Id { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string DocumentId { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public string Source { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public string Content { get; set; } = string.Empty;

    [VectorStoreRecordData]
    public string Title { get; set; } = string.Empty;

    [VectorStoreRecordData(IsFilterable = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [VectorStoreRecordData(IsFilterable = true)]
    public string Category { get; set; } = string.Empty;

    [VectorStoreRecordVector(1536)] // For text-embedding-3-small
    public ReadOnlyMemory<float> Embedding { get; set; }
}
```

---

## Implementation Patterns

### 1. Kernel Setup with Embeddings

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

public static class RAGKernelFactory
{
    public static Kernel CreateKernel(IConfiguration configuration)
    {
        var builder = Kernel.CreateBuilder();
        var aiConfig = configuration.GetSection("AI").Get<AIConfiguration>()!;

        if (aiConfig.Provider == "OpenAI")
        {
            builder.AddOpenAIChatCompletion(
                modelId: aiConfig.OpenAI.ModelId,
                apiKey: aiConfig.OpenAI.ApiKey);

            builder.AddOpenAITextEmbeddingGeneration(
                modelId: aiConfig.OpenAI.EmbeddingModelId,
                apiKey: aiConfig.OpenAI.ApiKey);
        }
        else if (aiConfig.Provider == "AzureOpenAI")
        {
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: aiConfig.AzureOpenAI.DeploymentName,
                endpoint: aiConfig.AzureOpenAI.Endpoint,
                apiKey: aiConfig.AzureOpenAI.ApiKey);

            builder.AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: aiConfig.AzureOpenAI.EmbeddingDeploymentName,
                endpoint: aiConfig.AzureOpenAI.Endpoint,
                apiKey: aiConfig.AzureOpenAI.ApiKey);
        }

        return builder.Build();
    }
}
```

### 2. Document Chunking Service

```csharp
public interface IDocumentChunker
{
    IEnumerable<DocumentChunk> ChunkDocument(string content, string documentId, string source);
}

public class DocumentChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}

public class SimpleDocumentChunker : IDocumentChunker
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public SimpleDocumentChunker(RAGConfiguration config)
    {
        _chunkSize = config.ChunkSize;
        _chunkOverlap = config.ChunkOverlap;
    }

    public IEnumerable<DocumentChunk> ChunkDocument(string content, string documentId, string source)
    {
        if (string.IsNullOrWhiteSpace(content))
            yield break;

        var chunks = new List<DocumentChunk>();
        var position = 0;
        var chunkIndex = 0;

        while (position < content.Length)
        {
            var endPosition = Math.Min(position + _chunkSize, content.Length);
            
            // Try to break at sentence or paragraph boundary
            if (endPosition < content.Length)
            {
                var lastParagraph = content.LastIndexOf("\n\n", endPosition, Math.Min(_chunkSize, endPosition));
                var lastSentence = content.LastIndexOf(". ", endPosition, Math.Min(_chunkSize, endPosition));
                
                if (lastParagraph > position + _chunkSize / 2)
                    endPosition = lastParagraph + 2;
                else if (lastSentence > position + _chunkSize / 2)
                    endPosition = lastSentence + 2;
            }

            var chunkContent = content[position..endPosition].Trim();
            
            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                yield return new DocumentChunk
                {
                    Id = $"{documentId}-{chunkIndex}",
                    DocumentId = documentId,
                    Source = source,
                    Content = chunkContent,
                    ChunkIndex = chunkIndex,
                    StartPosition = position,
                    EndPosition = endPosition
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

### 3. Vector Store Service

```csharp
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.Extensions.VectorData;

public interface IVectorStoreService
{
    Task<string> AddDocumentAsync(DocumentChunk chunk, CancellationToken cancellationToken = default);
    Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentRecord>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
}

public class VectorStoreService : IVectorStoreService
{
    private readonly IVectorStoreRecordCollection<string, DocumentRecord> _collection;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ILogger<VectorStoreService> _logger;

    public VectorStoreService(
        IVectorStoreRecordCollection<string, DocumentRecord> collection,
        ITextEmbeddingGenerationService embeddingService,
        ILogger<VectorStoreService> logger)
    {
        _collection = collection;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<string> AddDocumentAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content, cancellationToken: cancellationToken);

        var record = new DocumentRecord
        {
            Id = chunk.Id,
            DocumentId = chunk.DocumentId,
            Source = chunk.Source,
            Content = chunk.Content,
            Embedding = embedding
        };

        await _collection.UpsertAsync(record, cancellationToken: cancellationToken);
        _logger.LogInformation("Added document chunk {ChunkId} from {Source}", chunk.Id, chunk.Source);

        return chunk.Id;
    }

    public async Task AddDocumentsAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        var contents = chunkList.Select(c => c.Content).ToList();
        
        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(contents, cancellationToken: cancellationToken);

        var records = chunkList.Select((chunk, index) => new DocumentRecord
        {
            Id = chunk.Id,
            DocumentId = chunk.DocumentId,
            Source = chunk.Source,
            Content = chunk.Content,
            Embedding = embeddings[index]
        });

        await _collection.UpsertBatchAsync(records, cancellationToken: cancellationToken).ToListAsync(cancellationToken);
        _logger.LogInformation("Added {Count} document chunks", chunkList.Count);
    }

    public async Task<IReadOnlyList<DocumentRecord>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: cancellationToken);

        var searchOptions = new VectorSearchOptions
        {
            Top = topK,
            VectorPropertyName = nameof(DocumentRecord.Embedding)
        };

        var results = await _collection.VectorizedSearchAsync(
            queryEmbedding,
            searchOptions,
            cancellationToken);

        var documents = new List<DocumentRecord>();
        await foreach (var result in results.Results.WithCancellation(cancellationToken))
        {
            if (result.Record != null)
            {
                documents.Add(result.Record);
            }
        }

        return documents;
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        // Implementation depends on vector store capabilities
        _logger.LogInformation("Deleted document {DocumentId}", documentId);
    }
}
```

### 4. RAG Service

```csharp
public interface IRAGService
{
    Task<string> QueryAsync(string question, CancellationToken cancellationToken = default);
    Task<RAGResponse> QueryWithSourcesAsync(string question, CancellationToken cancellationToken = default);
    Task IngestDocumentAsync(string content, string source, string? title = null, CancellationToken cancellationToken = default);
}

public class RAGResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SourceReference> Sources { get; set; } = new();
}

public class SourceReference
{
    public string Source { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
}

public class RAGService : IRAGService
{
    private readonly Kernel _kernel;
    private readonly IVectorStoreService _vectorStore;
    private readonly IDocumentChunker _chunker;
    private readonly RAGConfiguration _config;
    private readonly ILogger<RAGService> _logger;

    public RAGService(
        Kernel kernel,
        IVectorStoreService vectorStore,
        IDocumentChunker chunker,
        RAGConfiguration config,
        ILogger<RAGService> logger)
    {
        _kernel = kernel;
        _vectorStore = vectorStore;
        _chunker = chunker;
        _config = config;
        _logger = logger;
    }

    public async Task<string> QueryAsync(string question, CancellationToken cancellationToken = default)
    {
        var response = await QueryWithSourcesAsync(question, cancellationToken);
        return response.Answer;
    }

    public async Task<RAGResponse> QueryWithSourcesAsync(string question, CancellationToken cancellationToken = default)
    {
        // 1. Retrieve relevant documents
        var documents = await _vectorStore.SearchAsync(question, _config.TopK, cancellationToken);

        if (!documents.Any())
        {
            return new RAGResponse
            {
                Answer = "I couldn't find any relevant information to answer your question.",
                Sources = new List<SourceReference>()
            };
        }

        // 2. Build context from retrieved documents
        var context = BuildContext(documents);

        // 3. Generate response using context
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

    public async Task IngestDocumentAsync(
        string content,
        string source,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        var documentId = Guid.NewGuid().ToString();
        var chunks = _chunker.ChunkDocument(content, documentId, source);
        await _vectorStore.AddDocumentsAsync(chunks, cancellationToken);
        
        _logger.LogInformation("Ingested document from {Source} with {ChunkCount} chunks", 
            source, chunks.Count());
    }

    private string BuildContext(IReadOnlyList<DocumentRecord> documents)
    {
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("Relevant Information:");
        contextBuilder.AppendLine();

        foreach (var doc in documents)
        {
            contextBuilder.AppendLine($"[Source: {doc.Source}]");
            contextBuilder.AppendLine(doc.Content);
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    private string BuildRAGPrompt(string question, string context)
    {
        return $"""
            You are a helpful assistant that answers questions based on the provided context.
            
            Context:
            {context}
            
            Instructions:
            - Answer the question based ONLY on the information provided in the context above
            - If the context doesn't contain enough information to answer the question, say so
            - Be concise but complete in your answer
            - If you quote from the context, indicate the source
            
            Question: {question}
            
            Answer:
            """;
    }
}
```

### 5. Dependency Injection Setup

```csharp
public static class RAGServiceExtensions
{
    public static IServiceCollection AddRAGServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        var ragConfig = configuration.GetSection("RAG").Get<RAGConfiguration>() ?? new();
        services.AddSingleton(ragConfig);

        // Kernel with embeddings
        services.AddSingleton(sp => RAGKernelFactory.CreateKernel(configuration));

        // Embedding service
        services.AddSingleton(sp =>
        {
            var kernel = sp.GetRequiredService<Kernel>();
            return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        });

        // Vector store (example with in-memory for development)
        services.AddSingleton<IVectorStoreRecordCollection<string, DocumentRecord>>(sp =>
        {
            var vectorStore = new InMemoryVectorStore();
            return vectorStore.GetCollection<string, DocumentRecord>("documents");
        });

        // Services
        services.AddSingleton<IDocumentChunker, SimpleDocumentChunker>();
        services.AddSingleton<IVectorStoreService, VectorStoreService>();
        services.AddScoped<IRAGService, RAGService>();

        return services;
    }
}
```

---

## Web API Integration

### RAG Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class RAGController : ControllerBase
{
    private readonly IRAGService _ragService;
    private readonly ILogger<RAGController> _logger;

    public RAGController(IRAGService ragService, ILogger<RAGController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    [HttpPost("query")]
    public async Task<IActionResult> Query(
        [FromBody] QueryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _ragService.QueryWithSourcesAsync(request.Question, cancellationToken);
        return Ok(response);
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest(
        [FromBody] IngestRequest request,
        CancellationToken cancellationToken)
    {
        await _ragService.IngestDocumentAsync(
            request.Content,
            request.Source,
            request.Title,
            cancellationToken);

        return Ok(new { message = "Document ingested successfully" });
    }

    [HttpPost("ingest/file")]
    public async Task<IActionResult> IngestFile(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync(cancellationToken);

        await _ragService.IngestDocumentAsync(
            content,
            file.FileName,
            Path.GetFileNameWithoutExtension(file.FileName),
            cancellationToken);

        return Ok(new { message = $"File {file.FileName} ingested successfully" });
    }
}

public record QueryRequest(string Question);
public record IngestRequest(string Content, string Source, string? Title = null);
```

---

## Advanced Patterns

### Hybrid Search (Vector + Keyword)

```csharp
public class HybridSearchService
{
    private readonly IVectorStoreService _vectorStore;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public async Task<IReadOnlyList<DocumentRecord>> HybridSearchAsync(
        string query,
        int topK = 5,
        double vectorWeight = 0.7,
        CancellationToken cancellationToken = default)
    {
        // Vector search
        var vectorResults = await _vectorStore.SearchAsync(query, topK * 2, cancellationToken);

        // Keyword search (simplified - in production use proper full-text search)
        var keywords = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keywordScores = vectorResults
            .Select(doc => new
            {
                Document = doc,
                KeywordScore = CalculateKeywordScore(doc.Content, keywords)
            })
            .ToList();

        // Combine scores
        var combinedResults = keywordScores
            .OrderByDescending(r => r.KeywordScore * (1 - vectorWeight))
            .Take(topK)
            .Select(r => r.Document)
            .ToList();

        return combinedResults;
    }

    private double CalculateKeywordScore(string content, string[] keywords)
    {
        var lowerContent = content.ToLowerInvariant();
        var matches = keywords.Count(k => lowerContent.Contains(k));
        return (double)matches / keywords.Length;
    }
}
```

### Query Expansion

```csharp
public class QueryExpansionService
{
    private readonly Kernel _kernel;

    public async Task<List<string>> ExpandQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var prompt = $"""
            Generate 3 alternative phrasings for the following search query.
            Return only the alternative queries, one per line.
            
            Original query: {query}
            
            Alternative queries:
            """;

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var alternatives = result.GetValue<string>()?
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList() ?? new List<string>();

        alternatives.Insert(0, query); // Include original query
        return alternatives;
    }
}
```

### Re-ranking

```csharp
public class ReRankingService
{
    private readonly Kernel _kernel;

    public async Task<List<DocumentRecord>> ReRankAsync(
        string query,
        List<DocumentRecord> documents,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var scoredDocs = new List<(DocumentRecord Doc, double Score)>();

        foreach (var doc in documents)
        {
            var score = await ScoreRelevanceAsync(query, doc.Content, cancellationToken);
            scoredDocs.Add((doc, score));
        }

        return scoredDocs
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Doc)
            .ToList();
    }

    private async Task<double> ScoreRelevanceAsync(
        string query,
        string content,
        CancellationToken cancellationToken)
    {
        var prompt = $"""
            Rate the relevance of the following document to the query on a scale of 0 to 10.
            Return only a number.
            
            Query: {query}
            
            Document: {content[..Math.Min(500, content.Length)]}
            
            Relevance score:
            """;

        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var scoreText = result.GetValue<string>() ?? "0";
        
        return double.TryParse(scoreText.Trim(), out var score) ? score / 10.0 : 0;
    }
}
```

---

## Integration with Mvp24Hours

### Business Service Integration

```csharp
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

public class RAGBusinessService
{
    private readonly IRAGService _ragService;

    public RAGBusinessService(IRAGService ragService)
    {
        _ragService = ragService;
    }

    public async Task<IBusinessResult<RAGResponse>> QueryKnowledgeBaseAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var response = await _ragService.QueryWithSourcesAsync(question, cancellationToken);

        return new BusinessResult<RAGResponse>
        {
            Data = response
        }.SetSuccess();
    }
}
```

---

## Testing

```csharp
using Xunit;

public class RAGServiceTests
{
    [Fact]
    public async Task QueryAsync_WithRelevantDocuments_ReturnsAnswer()
    {
        // Arrange
        var mockVectorStore = new Mock<IVectorStoreService>();
        mockVectorStore
            .Setup(x => x.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentRecord>
            {
                new() { Content = "The capital of France is Paris.", Source = "geography.txt" }
            });

        // Act & Assert based on your implementation
    }

    [Fact]
    public async Task IngestDocumentAsync_ChunksAndStoresDocument()
    {
        // Test document ingestion
    }
}

public class DocumentChunkerTests
{
    [Fact]
    public void ChunkDocument_SplitsLargeDocument()
    {
        // Arrange
        var config = new RAGConfiguration { ChunkSize = 100, ChunkOverlap = 20 };
        var chunker = new SimpleDocumentChunker(config);
        var content = new string('x', 500);

        // Act
        var chunks = chunker.ChunkDocument(content, "doc1", "test.txt").ToList();

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, c => Assert.True(c.Content.Length <= config.ChunkSize + 50)); // Allow for boundary adjustment
    }
}
```

---

## Best Practices

### Document Preparation

1. **Clean Data**: Remove unnecessary formatting, headers, footers
2. **Structured Chunking**: Respect document structure (paragraphs, sections)
3. **Metadata**: Include relevant metadata for filtering
4. **Overlap**: Use chunk overlap to maintain context

### Query Optimization

1. **Query Preprocessing**: Clean and normalize queries
2. **Query Expansion**: Generate alternative phrasings
3. **Filtering**: Use metadata filters to narrow results
4. **Re-ranking**: Apply additional relevance scoring

### Performance

1. **Batch Embeddings**: Generate embeddings in batches
2. **Caching**: Cache frequently accessed documents
3. **Index Optimization**: Configure vector store indexes appropriately
4. **Async Operations**: Use async throughout the pipeline

---

## Related Templates

- [Chat Completion](template-sk-chat-completion.md) - Basic chat functionality
- [Plugins & Functions](template-sk-plugins.md) - Tool integration
- [Document Pipeline](template-skg-document-pipeline.md) - Advanced document processing

---

## External References

- [Semantic Kernel Memory](https://learn.microsoft.com/semantic-kernel/memories)
- [Vector Stores Overview](https://learn.microsoft.com/semantic-kernel/memories/vector-stores)
- [RAG Best Practices](https://learn.microsoft.com/azure/ai-services/openai/concepts/retrieval-augmented-generation)

