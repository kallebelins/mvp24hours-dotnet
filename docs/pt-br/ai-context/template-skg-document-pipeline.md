# Template Document Analysis Pipeline - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para pipelines de processamento de documentos usando grafos.

---

## Visão Geral

Pipeline de documentos orquestra processamento multi-etapas:
- Extração de conteúdo
- Análise e classificação
- Transformação
- Validação e saída

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Processamento de documentos | ✅ Recomendado |
| Extração de informações | ✅ Recomendado |
| Classificação automática | ✅ Recomendado |
| Q&A simples | ⚠️ Use RAG Básico |

---

## Implementação

```csharp
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;

public class DocumentPipeline
{
    private readonly Kernel _kernel;

    public async Task<DocumentResult> ProcessAsync(string document, string documentType)
    {
        var graph = new GraphExecutor("DocPipeline", "Pipeline de documentos");

        // Nó de extração
        var extractNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Extraia informações chave do documento:
                
                Tipo: {{$documentType}}
                Documento: {{$document}}
                
                Extraia em formato JSON:
                - título
                - entidades (pessoas, organizações, lugares)
                - datas importantes
                - valores monetários
                - tópicos principais
                """),
            nodeId: "extract"
        ).StoreResultAs("extracted");

        // Nó de classificação
        var classifyNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Classifique o documento:
                
                Conteúdo extraído: {{$extracted}}
                
                Classificações possíveis:
                - CONTRATO
                - RELATÓRIO
                - COMUNICAÇÃO
                - TÉCNICO
                - FINANCEIRO
                
                Classificação (com confiança 0-1):
                """),
            nodeId: "classify"
        ).StoreResultAs("classification");

        // Nó de resumo
        var summarizeNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Crie um resumo executivo:
                
                Documento: {{$document}}
                Informações extraídas: {{$extracted}}
                Classificação: {{$classification}}
                
                Resumo (máximo 200 palavras):
                """),
            nodeId: "summarize"
        ).StoreResultAs("summary");

        // Nó de validação
        var validateNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Valide a análise do documento:
                
                Extração: {{$extracted}}
                Classificação: {{$classification}}
                Resumo: {{$summary}}
                
                Verifique:
                1. Completude das informações
                2. Consistência da classificação
                3. Qualidade do resumo
                
                Resultado (VALID/INVALID com comentários):
                """),
            nodeId: "validate"
        ).StoreResultAs("validation");

        graph.AddNode(extractNode);
        graph.AddNode(classifyNode);
        graph.AddNode(summarizeNode);
        graph.AddNode(validateNode);

        graph.Connect("extract", "classify");
        graph.Connect("classify", "summarize");
        graph.Connect("summarize", "validate");

        graph.SetStartNode("extract");

        var result = await graph.ExecuteAsync(_kernel, new KernelArguments
        {
            ["document"] = document,
            ["documentType"] = documentType
        });

        return new DocumentResult
        {
            ExtractedInfo = result.State.GetValue<string>("extracted"),
            Classification = result.State.GetValue<string>("classification"),
            Summary = result.State.GetValue<string>("summary"),
            Validation = result.State.GetValue<string>("validation")
        };
    }
}

public class DocumentResult
{
    public string ExtractedInfo { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Validation { get; set; } = string.Empty;
}
```

---

## Boas Práticas

1. **Etapas Modulares**: Cada etapa deve ser independente
2. **Validação**: Sempre inclua etapa de validação
3. **Formato Estruturado**: Use JSON para saídas estruturadas
4. **Tratamento de Erros**: Implemente fallback para cada etapa

---

## Templates Relacionados

- [RAG Básico](template-sk-rag-basic.md) - Q&A sobre documentos
- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos

