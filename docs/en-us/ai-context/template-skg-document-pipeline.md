# Document Analysis Pipeline Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for building comprehensive document processing workflows using Semantic Kernel Graph.

---

## Overview

Document analysis pipelines process documents through multiple stages of analysis, classification, and information extraction. This template covers:
- Multi-stage document processing
- Parallel analysis execution
- Document classification
- Information extraction
- Error handling and recovery
- Batch processing

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Multi-document processing | ✅ Recommended |
| Content extraction | ✅ Recommended |
| Document classification | ✅ Recommended |
| Parallel analysis tasks | ✅ Recommended |
| Simple single-file parsing | ⚠️ Use Graph Executor |
| Interactive processing | ⚠️ Use Chatbot with Memory |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.*" />
</ItemGroup>
```

---

## Pipeline Architecture

```
┌────────────────────────────────────────────────────────────┐
│                   Document Pipeline                         │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────┐                                       │
│  │    Ingestion     │                                       │
│  │  - File loading  │                                       │
│  │  - Validation    │                                       │
│  │  - Metadata      │                                       │
│  └────────┬─────────┘                                       │
│           │                                                 │
│  ┌────────▼─────────┐                                       │
│  │  Classification  │                                       │
│  │  - Type detect   │                                       │
│  │  - Category      │                                       │
│  └────────┬─────────┘                                       │
│           │                                                 │
│  ┌────────▼─────────────────────────────────┐               │
│  │         Parallel Analysis                 │               │
│  ├──────────────┬──────────────┬────────────┤               │
│  │ Text         │ Structure    │ Semantic   │               │
│  │ Analysis     │ Analysis     │ Analysis   │               │
│  └──────────────┴──────────────┴────────────┘               │
│           │                                                 │
│  ┌────────▼─────────┐                                       │
│  │   Aggregation    │                                       │
│  │  - Results merge │                                       │
│  │  - Summary       │                                       │
│  └──────────────────┘                                       │
└────────────────────────────────────────────────────────────┘
```

---

## Implementation Patterns

### 1. Document Models

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;

/// <summary>
/// Document metadata extracted during ingestion.
/// </summary>
public class DocumentMetadata
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileExtension { get; set; } = string.Empty;
    public DateTime IngestionTimestamp { get; set; }
    public int ContentLength { get; set; }
    public int LineCount { get; set; }
    public int WordCount { get; set; }
}

/// <summary>
/// Document classification results.
/// </summary>
public class DocumentClassification
{
    public string DocumentType { get; set; } = string.Empty; // text, markdown, pdf, word
    public string ContentCategory { get; set; } = string.Empty; // financial, legal, report, general
    public double Confidence { get; set; }
}

/// <summary>
/// Text analysis results.
/// </summary>
public class TextAnalysisResult
{
    public double ReadabilityScore { get; set; }
    public double SentimentScore { get; set; }
    public string LanguageDetected { get; set; } = string.Empty;
    public List<string> KeyPhrases { get; set; } = new();
    public List<string> NamedEntities { get; set; } = new();
}

/// <summary>
/// Structure analysis results.
/// </summary>
public class StructureAnalysisResult
{
    public List<string> Sections { get; set; } = new();
    public List<string> Headers { get; set; } = new();
    public int ListCount { get; set; }
    public int TableCount { get; set; }
    public Dictionary<string, object> Formatting { get; set; } = new();
}

/// <summary>
/// Semantic analysis results.
/// </summary>
public class SemanticAnalysisResult
{
    public List<string> Topics { get; set; } = new();
    public List<string> Themes { get; set; } = new();
    public Dictionary<string, List<string>> Relationships { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public List<string> KeyInsights { get; set; } = new();
}

/// <summary>
/// Comprehensive document analysis result.
/// </summary>
public class DocumentAnalysisResult
{
    public string DocumentId { get; set; } = string.Empty;
    public DocumentMetadata Metadata { get; set; } = new();
    public DocumentClassification Classification { get; set; } = new();
    public TextAnalysisResult? TextAnalysis { get; set; }
    public StructureAnalysisResult? StructureAnalysis { get; set; }
    public SemanticAnalysisResult? SemanticAnalysis { get; set; }
    public string ProcessingStatus { get; set; } = string.Empty;
    public DateTime AnalysisTimestamp { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### 2. Basic Document Analysis Pipeline

```csharp
public class DocumentAnalysisPipelineBuilder
{
    private readonly Kernel _kernel;

    public DocumentAnalysisPipelineBuilder(Kernel kernel)
    {
        _kernel = kernel;
    }

    public GraphExecutor CreateBasicPipeline()
    {
        var executor = new GraphExecutor("DocumentAnalysisPipeline", "Basic document analysis");

        // Stage 1: Document Ingestion
        var ingestionNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var documentPath = args["document_path"]?.ToString() ?? string.Empty;
                    
                    if (!File.Exists(documentPath))
                    {
                        args["processing_status"] = "failed";
                        args["error_message"] = $"Document not found: {documentPath}";
                        return "Document not found";
                    }

                    var content = await File.ReadAllTextAsync(documentPath);
                    var fileInfo = new FileInfo(documentPath);

                    var metadata = new DocumentMetadata
                    {
                        FileName = fileInfo.Name,
                        FileSize = fileInfo.Length,
                        FileExtension = fileInfo.Extension,
                        IngestionTimestamp = DateTime.UtcNow,
                        ContentLength = content.Length,
                        LineCount = content.Split('\n').Length,
                        WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                    };

                    args["document_content"] = content;
                    args["document_metadata"] = metadata;
                    args["file_extension"] = fileInfo.Extension;
                    args["processing_status"] = "ingested";

                    return $"Document ingested: {fileInfo.Name} ({fileInfo.Length} bytes)";
                },
                functionName: "IngestDocument",
                description: "Ingests and validates document"),
            "ingest-document");

        // Stage 2: Document Classification
        var classificationNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var content = args["document_content"]?.ToString() ?? string.Empty;
                    var extension = args["file_extension"]?.ToString() ?? string.Empty;

                    // Type classification by extension
                    var documentType = extension.ToLower() switch
                    {
                        ".txt" => "text",
                        ".md" => "markdown",
                        ".pdf" => "pdf",
                        ".doc" or ".docx" => "word",
                        ".json" => "json",
                        ".xml" => "xml",
                        _ => "unknown"
                    };

                    // Content category classification
                    var contentLower = content.ToLower();
                    var contentCategory = contentLower switch
                    {
                        var c when c.Contains("invoice") || c.Contains("bill") || c.Contains("payment") => "financial",
                        var c when c.Contains("contract") || c.Contains("agreement") || c.Contains("terms") => "legal",
                        var c when c.Contains("report") || c.Contains("analysis") || c.Contains("summary") => "report",
                        var c when c.Contains("email") || c.Contains("correspondence") => "communication",
                        var c when c.Contains("manual") || c.Contains("guide") || c.Contains("documentation") => "technical",
                        _ => "general"
                    };

                    var classification = new DocumentClassification
                    {
                        DocumentType = documentType,
                        ContentCategory = contentCategory,
                        Confidence = 0.85
                    };

                    args["document_classification"] = classification;
                    args["document_type"] = documentType;
                    args["content_category"] = contentCategory;
                    args["processing_status"] = "classified";

                    return $"Document classified as {documentType} ({contentCategory})";
                },
                functionName: "ClassifyDocument",
                description: "Classifies document by type and content"),
            "classify-document");

        // Stage 3: Content Analysis
        var analysisNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var content = args["document_content"]?.ToString() ?? string.Empty;
                    var contentCategory = args["content_category"]?.ToString() ?? "general";

                    var textAnalysis = new TextAnalysisResult
                    {
                        ReadabilityScore = CalculateReadabilityScore(content),
                        SentimentScore = 0.65, // Neutral positive
                        LanguageDetected = "en",
                        KeyPhrases = ExtractKeyPhrases(content),
                        NamedEntities = ExtractNamedEntities(content)
                    };

                    args["text_analysis"] = textAnalysis;
                    args["processing_status"] = "analyzed";
                    args["analysis_timestamp"] = DateTime.UtcNow;

                    return $"Content analysis completed: {textAnalysis.KeyPhrases.Count} key phrases extracted";
                },
                functionName: "AnalyzeContent",
                description: "Analyzes document content"),
            "analyze-content");

        // Build pipeline
        executor.AddNode(ingestionNode);
        executor.AddNode(classificationNode);
        executor.AddNode(analysisNode);

        executor.SetStartNode(ingestionNode.NodeId);
        executor.Connect(ingestionNode.NodeId, classificationNode.NodeId);
        executor.Connect(classificationNode.NodeId, analysisNode.NodeId);

        return executor;
    }

    private static double CalculateReadabilityScore(string content)
    {
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var sentences = content.Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries).Length;
        
        if (sentences == 0) return 0;
        
        var avgWordsPerSentence = (double)words / sentences;
        return Math.Max(0, Math.Min(100, 100 - (avgWordsPerSentence * 2)));
    }

    private static List<string> ExtractKeyPhrases(string content)
    {
        var words = content.ToLower()
            .Split([' ', '\n', '\r', '\t', '.', ',', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        return words;
    }

    private static List<string> ExtractNamedEntities(string content)
    {
        // Simple pattern: words starting with uppercase
        var entities = content
            .Split([' ', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2 && char.IsUpper(w[0]))
            .Distinct()
            .Take(10)
            .ToList();

        return entities;
    }
}
```

### 3. Advanced Pipeline with Parallel Processing

```csharp
public class AdvancedDocumentPipelineBuilder
{
    private readonly Kernel _kernel;

    public AdvancedDocumentPipelineBuilder(Kernel kernel)
    {
        _kernel = kernel;
    }

    public GraphExecutor CreateAdvancedPipeline()
    {
        var executor = new GraphExecutor("AdvancedDocumentPipeline", "Parallel document analysis");

        // Ingestion node (same as basic)
        var ingestionNode = CreateIngestionNode();

        // Parallel analysis nodes
        var textAnalysisNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var content = args["document_content"]?.ToString() ?? string.Empty;

                    var analysis = new TextAnalysisResult
                    {
                        ReadabilityScore = CalculateReadabilityScore(content),
                        SentimentScore = AnalyzeSentiment(content),
                        LanguageDetected = DetectLanguage(content),
                        KeyPhrases = ExtractKeyPhrases(content),
                        NamedEntities = ExtractNamedEntities(content)
                    };

                    args["text_analysis"] = analysis;
                    return "Text analysis completed";
                },
                functionName: "TextAnalysis",
                description: "Analyzes text content"),
            "text-analysis")
            .StoreResultAs("text_analysis_result");

        var structureAnalysisNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var content = args["document_content"]?.ToString() ?? string.Empty;

                    var structure = new StructureAnalysisResult
                    {
                        Sections = IdentifySections(content),
                        Headers = ExtractHeaders(content),
                        ListCount = CountLists(content),
                        TableCount = CountTables(content),
                        Formatting = AnalyzeFormatting(content)
                    };

                    args["structure_analysis"] = structure;
                    return "Structure analysis completed";
                },
                functionName: "StructureAnalysis",
                description: "Analyzes document structure"),
            "structure-analysis")
            .StoreResultAs("structure_analysis_result");

        var semanticAnalysisNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var content = args["document_content"]?.ToString() ?? string.Empty;

                    var semantic = new SemanticAnalysisResult
                    {
                        Topics = ExtractTopics(content),
                        Themes = IdentifyThemes(content),
                        Relationships = FindRelationships(content),
                        Summary = GenerateSummary(content),
                        KeyInsights = ExtractInsights(content)
                    };

                    args["semantic_analysis"] = semantic;
                    return "Semantic analysis completed";
                },
                functionName: "SemanticAnalysis",
                description: "Performs semantic analysis"),
            "semantic-analysis")
            .StoreResultAs("semantic_analysis_result");

        // Aggregation node
        var aggregationNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var metadata = (DocumentMetadata)args["document_metadata"]!;
                    var textAnalysis = (TextAnalysisResult?)args["text_analysis"];
                    var structureAnalysis = (StructureAnalysisResult?)args["structure_analysis"];
                    var semanticAnalysis = (SemanticAnalysisResult?)args["semantic_analysis"];

                    var result = new DocumentAnalysisResult
                    {
                        DocumentId = Guid.NewGuid().ToString(),
                        Metadata = metadata,
                        Classification = (DocumentClassification?)args["document_classification"] ?? new(),
                        TextAnalysis = textAnalysis,
                        StructureAnalysis = structureAnalysis,
                        SemanticAnalysis = semanticAnalysis,
                        ProcessingStatus = "completed",
                        AnalysisTimestamp = DateTime.UtcNow
                    };

                    args["comprehensive_analysis"] = result;
                    args["processing_status"] = "completed";

                    return "Results aggregation completed";
                },
                functionName: "AggregateResults",
                description: "Aggregates all analysis results"),
            "aggregate-results");

        // Build pipeline
        executor.AddNode(ingestionNode);
        executor.AddNode(textAnalysisNode);
        executor.AddNode(structureAnalysisNode);
        executor.AddNode(semanticAnalysisNode);
        executor.AddNode(aggregationNode);

        executor.SetStartNode(ingestionNode.NodeId);
        
        // Parallel execution from ingestion
        executor.Connect(ingestionNode.NodeId, textAnalysisNode.NodeId);
        executor.Connect(ingestionNode.NodeId, structureAnalysisNode.NodeId);
        executor.Connect(ingestionNode.NodeId, semanticAnalysisNode.NodeId);
        
        // All converge to aggregation
        executor.Connect(textAnalysisNode.NodeId, aggregationNode.NodeId);
        executor.Connect(structureAnalysisNode.NodeId, aggregationNode.NodeId);
        executor.Connect(semanticAnalysisNode.NodeId, aggregationNode.NodeId);

        return executor;
    }

    private FunctionGraphNode CreateIngestionNode()
    {
        return new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var documentPath = args["document_path"]?.ToString() ?? string.Empty;

                    if (!File.Exists(documentPath))
                    {
                        args["processing_status"] = "failed";
                        args["error_message"] = "Document not found";
                        return "Document not found";
                    }

                    var content = await File.ReadAllTextAsync(documentPath);
                    
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        args["processing_status"] = "failed";
                        args["error_message"] = "Empty document";
                        return "Empty document";
                    }

                    var fileInfo = new FileInfo(documentPath);
                    var metadata = new DocumentMetadata
                    {
                        FileName = fileInfo.Name,
                        FileSize = fileInfo.Length,
                        FileExtension = fileInfo.Extension,
                        IngestionTimestamp = DateTime.UtcNow,
                        ContentLength = content.Length,
                        LineCount = content.Split('\n').Length,
                        WordCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                    };

                    args["document_content"] = content;
                    args["document_metadata"] = metadata;
                    args["processing_status"] = "ingested";

                    return $"Advanced ingestion completed: {fileInfo.Name}";
                },
                functionName: "AdvancedIngestion",
                description: "Advanced document ingestion with validation"),
            "advanced-ingestion");
    }

    // Helper methods
    private static double CalculateReadabilityScore(string content) => 75.0;
    private static double AnalyzeSentiment(string content) => 0.65;
    private static string DetectLanguage(string content) => "en";
    private static List<string> ExtractKeyPhrases(string content) => new() { "key", "phrase" };
    private static List<string> ExtractNamedEntities(string content) => new() { "Entity1" };
    private static List<string> IdentifySections(string content) => new() { "Introduction", "Body", "Conclusion" };
    private static List<string> ExtractHeaders(string content) => new() { "Header 1" };
    private static int CountLists(string content) => content.Split('\n').Count(l => l.TrimStart().StartsWith("-"));
    private static int CountTables(string content) => 0;
    private static Dictionary<string, object> AnalyzeFormatting(string content) => new();
    private static List<string> ExtractTopics(string content) => new() { "Topic 1", "Topic 2" };
    private static List<string> IdentifyThemes(string content) => new() { "Theme 1" };
    private static Dictionary<string, List<string>> FindRelationships(string content) => new();
    private static string GenerateSummary(string content) => content.Length > 100 ? content[..100] + "..." : content;
    private static List<string> ExtractInsights(string content) => new() { "Insight 1" };
}
```

### 4. Error Handling Pipeline

```csharp
public class ResilientDocumentPipelineBuilder
{
    private readonly Kernel _kernel;

    public ResilientDocumentPipelineBuilder(Kernel kernel)
    {
        _kernel = kernel;
    }

    public GraphExecutor CreateResilientPipeline()
    {
        var executor = new GraphExecutor("ResilientDocumentPipeline", "Error handling and recovery");

        // Resilient ingestion with error handling
        var ingestionNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    try
                    {
                        var documentPath = args["document_path"]?.ToString() ?? string.Empty;

                        if (!File.Exists(documentPath))
                        {
                            args["error_type"] = "file_not_found";
                            args["error_message"] = $"Document not found: {documentPath}";
                            args["processing_status"] = "failed";
                            return "Document not found";
                        }

                        var content = await File.ReadAllTextAsync(documentPath);

                        if (string.IsNullOrWhiteSpace(content))
                        {
                            args["error_type"] = "empty_content";
                            args["error_message"] = "Document content is empty";
                            args["processing_status"] = "failed";
                            return "Empty document";
                        }

                        args["document_content"] = content;
                        args["processing_status"] = "ingested";
                        args["error_type"] = "none";

                        return "Document ingested successfully";
                    }
                    catch (IOException ex)
                    {
                        args["error_type"] = "io_error";
                        args["error_message"] = ex.Message;
                        args["processing_status"] = "failed";
                        return $"IO error: {ex.Message}";
                    }
                    catch (Exception ex)
                    {
                        args["error_type"] = "unknown_error";
                        args["error_message"] = ex.Message;
                        args["processing_status"] = "failed";
                        return $"Error: {ex.Message}";
                    }
                },
                functionName: "ResilientIngestion",
                description: "Resilient document ingestion"),
            "resilient-ingestion");

        // Conditional routing based on status
        var routerNode = new ConditionalGraphNode(
            "ingestion-router",
            "Route based on ingestion status")
        {
            ConditionExpression = "processing_status == 'ingested'",
            TrueNodeId = "process-content",
            FalseNodeId = "handle-error"
        };

        // Content processor for successful ingestion
        var processNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var content = args["document_content"]?.ToString() ?? string.Empty;

                    // Process content
                    var processedContent = content.Trim();
                    args["processed_content"] = processedContent;
                    args["processing_status"] = "processed";

                    return "Content processing completed";
                },
                functionName: "ProcessContent",
                description: "Process document content"),
            "process-content");

        // Error handler
        var errorNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var errorType = args["error_type"]?.ToString() ?? "unknown";
                    var errorMessage = args["error_message"]?.ToString() ?? "Unknown error";

                    var recoveryAction = errorType switch
                    {
                        "file_not_found" => "Request document resubmission",
                        "empty_content" => "Skip processing and notify user",
                        "io_error" => "Retry with exponential backoff",
                        _ => "Manual intervention required"
                    };

                    args["recovery_action"] = recoveryAction;
                    args["processing_status"] = "error_handled";

                    return $"Error handled. Recovery: {recoveryAction}";
                },
                functionName: "HandleError",
                description: "Handle processing errors"),
            "handle-error");

        // Build pipeline
        executor.AddNode(ingestionNode);
        executor.AddNode(routerNode);
        executor.AddNode(processNode);
        executor.AddNode(errorNode);

        executor.SetStartNode(ingestionNode.NodeId);
        executor.Connect(ingestionNode.NodeId, routerNode.NodeId);

        return executor;
    }
}
```

### 5. Batch Processing Pipeline

```csharp
public class BatchDocumentPipelineBuilder
{
    private readonly Kernel _kernel;

    public BatchDocumentPipelineBuilder(Kernel kernel)
    {
        _kernel = kernel;
    }

    public GraphExecutor CreateBatchPipeline()
    {
        var executor = new GraphExecutor("BatchDocumentPipeline", "Multi-document batch processing");

        // Batch processor node
        var batchNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var documentPaths = args["document_paths"] as string[] ?? Array.Empty<string>();
                    var results = new List<DocumentAnalysisResult>();

                    // Process documents in parallel
                    var tasks = documentPaths.Select(async (path, index) =>
                    {
                        try
                        {
                            var content = await File.ReadAllTextAsync(path);
                            var fileInfo = new FileInfo(path);

                            return new DocumentAnalysisResult
                            {
                                DocumentId = $"doc_{index}",
                                Metadata = new DocumentMetadata
                                {
                                    FileName = fileInfo.Name,
                                    FileSize = fileInfo.Length,
                                    ContentLength = content.Length,
                                    WordCount = content.Split(' ').Length
                                },
                                ProcessingStatus = "processed",
                                AnalysisTimestamp = DateTime.UtcNow
                            };
                        }
                        catch (Exception ex)
                        {
                            return new DocumentAnalysisResult
                            {
                                DocumentId = $"doc_{index}",
                                Metadata = new DocumentMetadata { FileName = path },
                                ProcessingStatus = "failed",
                                ErrorMessage = ex.Message,
                                AnalysisTimestamp = DateTime.UtcNow
                            };
                        }
                    });

                    var taskResults = await Task.WhenAll(tasks);
                    results.AddRange(taskResults);

                    args["batch_results"] = results;
                    args["total_documents"] = documentPaths.Length;
                    args["successful_documents"] = results.Count(r => r.ProcessingStatus == "processed");
                    args["failed_documents"] = results.Count(r => r.ProcessingStatus == "failed");

                    return $"Batch completed: {results.Count} documents";
                },
                functionName: "BatchProcess",
                description: "Process multiple documents in batch"),
            "batch-process");

        // Batch analyzer node
        var analyzerNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var results = args["batch_results"] as List<DocumentAnalysisResult> ?? new();
                    var total = Convert.ToInt32(args["total_documents"]);
                    var successful = Convert.ToInt32(args["successful_documents"]);
                    var failed = Convert.ToInt32(args["failed_documents"]);

                    var successRate = total > 0 ? (double)successful / total : 0;
                    var totalSize = results
                        .Where(r => r.ProcessingStatus == "processed")
                        .Sum(r => r.Metadata.FileSize);

                    var analysis = new BatchAnalysisResult
                    {
                        TotalDocuments = total,
                        SuccessfulDocuments = successful,
                        FailedDocuments = failed,
                        SuccessRate = successRate,
                        TotalSizeBytes = totalSize,
                        ProcessingTimestamp = DateTime.UtcNow
                    };

                    args["batch_analysis"] = analysis;
                    args["processing_status"] = "batch_completed";

                    return "Batch analysis completed";
                },
                functionName: "AnalyzeBatch",
                description: "Analyze batch processing results"),
            "analyze-batch");

        // Build pipeline
        executor.AddNode(batchNode);
        executor.AddNode(analyzerNode);

        executor.SetStartNode(batchNode.NodeId);
        executor.Connect(batchNode.NodeId, analyzerNode.NodeId);

        return executor;
    }
}

public class BatchAnalysisResult
{
    public int TotalDocuments { get; set; }
    public int SuccessfulDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public double SuccessRate { get; set; }
    public long TotalSizeBytes { get; set; }
    public DateTime ProcessingTimestamp { get; set; }
}
```

---

## Service Layer Integration

```csharp
public interface IDocumentAnalysisService
{
    Task<DocumentAnalysisResult> AnalyzeDocumentAsync(string documentPath, CancellationToken cancellationToken = default);
    Task<BatchAnalysisResult> AnalyzeBatchAsync(string[] documentPaths, CancellationToken cancellationToken = default);
}

public class DocumentAnalysisService : IDocumentAnalysisService
{
    private readonly Kernel _kernel;
    private readonly GraphExecutor _basicPipeline;
    private readonly GraphExecutor _batchPipeline;

    public DocumentAnalysisService(Kernel kernel)
    {
        _kernel = kernel;
        
        var basicBuilder = new DocumentAnalysisPipelineBuilder(kernel);
        _basicPipeline = basicBuilder.CreateBasicPipeline();

        var batchBuilder = new BatchDocumentPipelineBuilder(kernel);
        _batchPipeline = batchBuilder.CreateBatchPipeline();
    }

    public async Task<DocumentAnalysisResult> AnalyzeDocumentAsync(
        string documentPath,
        CancellationToken cancellationToken = default)
    {
        var arguments = new KernelArguments
        {
            ["document_path"] = documentPath
        };

        await _basicPipeline.ExecuteAsync(_kernel, arguments, cancellationToken);

        var state = arguments.GetOrCreateGraphState();
        var status = args["processing_status"]?.ToString() ?? "unknown";
        
        return new DocumentAnalysisResult
        {
            DocumentId = Guid.NewGuid().ToString(),
            Metadata = (DocumentMetadata?)arguments["document_metadata"] ?? new(),
            Classification = (DocumentClassification?)arguments["document_classification"] ?? new(),
            TextAnalysis = (TextAnalysisResult?)arguments["text_analysis"],
            ProcessingStatus = status,
            AnalysisTimestamp = DateTime.UtcNow
        };
    }

    public async Task<BatchAnalysisResult> AnalyzeBatchAsync(
        string[] documentPaths,
        CancellationToken cancellationToken = default)
    {
        var arguments = new KernelArguments
        {
            ["document_paths"] = documentPaths
        };

        await _batchPipeline.ExecuteAsync(_kernel, arguments, cancellationToken);

        return (BatchAnalysisResult?)arguments["batch_analysis"] ?? new();
    }
}
```

---

## Dependency Injection Setup

```csharp
public static class DocumentAnalysisServiceExtensions
{
    public static IServiceCollection AddDocumentAnalysisServices(
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

        // Register document analysis service
        services.AddScoped<IDocumentAnalysisService, DocumentAnalysisService>();

        return services;
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentAnalysisService _service;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentAnalysisService service, ILogger<DocumentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> AnalyzeDocument(
        [FromBody] AnalyzeDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AnalyzeDocumentAsync(request.DocumentPath, cancellationToken);
        return Ok(result);
    }

    [HttpPost("analyze/batch")]
    public async Task<IActionResult> AnalyzeBatch(
        [FromBody] AnalyzeBatchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AnalyzeBatchAsync(request.DocumentPaths, cancellationToken);
        return Ok(result);
    }
}

public record AnalyzeDocumentRequest(string DocumentPath);
public record AnalyzeBatchRequest(string[] DocumentPaths);
```

---

## Testing

```csharp
using Xunit;

public class DocumentPipelineTests
{
    [Fact]
    public async Task BasicPipeline_ProcessesDocument_Successfully()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var builder = new DocumentAnalysisPipelineBuilder(kernel);
        var pipeline = builder.CreateBasicPipeline();

        // Create test file
        var testPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(testPath, "This is a test document for analysis.");

        try
        {
            var arguments = new KernelArguments { ["document_path"] = testPath };

            // Act
            await pipeline.ExecuteAsync(kernel, arguments);

            // Assert
            Assert.Equal("analyzed", arguments["processing_status"]?.ToString());
            Assert.NotNull(arguments["document_metadata"]);
            Assert.NotNull(arguments["text_analysis"]);
        }
        finally
        {
            File.Delete(testPath);
        }
    }

    [Fact]
    public async Task ResilientPipeline_HandlesFileNotFound()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var builder = new ResilientDocumentPipelineBuilder(kernel);
        var pipeline = builder.CreateResilientPipeline();

        var arguments = new KernelArguments { ["document_path"] = "nonexistent.txt" };

        // Act
        await pipeline.ExecuteAsync(kernel, arguments);

        // Assert
        Assert.Equal("failed", arguments["processing_status"]?.ToString());
        Assert.Equal("file_not_found", arguments["error_type"]?.ToString());
    }

    [Fact]
    public async Task BatchPipeline_ProcessesMultipleDocuments()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var builder = new BatchDocumentPipelineBuilder(kernel);
        var pipeline = builder.CreateBatchPipeline();

        // Create test files
        var testPaths = new string[3];
        for (int i = 0; i < 3; i++)
        {
            testPaths[i] = Path.GetTempFileName();
            await File.WriteAllTextAsync(testPaths[i], $"Document {i} content");
        }

        try
        {
            var arguments = new KernelArguments { ["document_paths"] = testPaths };

            // Act
            await pipeline.ExecuteAsync(kernel, arguments);

            // Assert
            Assert.Equal("batch_completed", arguments["processing_status"]?.ToString());
            Assert.Equal(3, Convert.ToInt32(arguments["total_documents"]));
        }
        finally
        {
            foreach (var path in testPaths)
                File.Delete(path);
        }
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

### Pipeline Design

1. **Modular Stages**: Keep processing stages focused and single-purpose
2. **Error Isolation**: Handle errors at each stage independently
3. **State Management**: Use consistent state keys across pipeline stages
4. **Logging**: Add comprehensive logging at each stage

### Performance

1. **Parallel Processing**: Use parallel execution for independent tasks
2. **Batch Processing**: Process multiple documents together when possible
3. **Streaming**: Stream large documents instead of loading entirely
4. **Caching**: Cache analysis results for repeated documents

### Error Handling

1. **Graceful Degradation**: Continue processing when individual documents fail
2. **Recovery Actions**: Define specific recovery strategies per error type
3. **Retry Logic**: Implement exponential backoff for transient failures
4. **Error Logging**: Log detailed error information for debugging

### Document Processing

1. **Validation**: Validate documents before processing
2. **Format Detection**: Automatically detect and handle different formats
3. **Size Limits**: Set appropriate limits for document size
4. **Content Security**: Sanitize document content before processing

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [Multi-Agent](template-skg-multi-agent.md) - Coordinated processing
- [Chain of Thought](template-skg-chain-of-thought.md) - Reasoning pipelines
- [Checkpointing](template-skg-checkpointing.md) - State persistence

---

## External References

- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [Document Processing Best Practices](https://learn.microsoft.com/azure/ai-services/document-intelligence/)
- [Pipeline Design Patterns](https://docs.microsoft.com/azure/architecture/patterns/pipes-and-filters)
