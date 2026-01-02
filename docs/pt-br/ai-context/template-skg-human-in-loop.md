# Template Human-in-the-Loop - Semantic Kernel Graph

> **Propósito**: Este template fornece padrões para workflows que requerem aprovação ou intervenção humana.

---

## Visão Geral

Human-in-the-Loop permite supervisão humana em workflows de IA:
- Aprovação de ações críticas
- Validação de resultados
- Timeout e políticas de SLA
- Múltiplos canais de aprovação

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Decisões críticas | ✅ Recomendado |
| Compliance e auditoria | ✅ Recomendado |
| Aprovação de conteúdo | ✅ Recomendado |
| Automação total | ⚠️ Use Graph Executor |

---

## Implementação

```csharp
using SemanticKernel.Graph.Nodes;
using System.Threading.Channels;

public class HumanApprovalWorkflow
{
    private readonly Kernel _kernel;
    private readonly Channel<ApprovalRequest> _requestChannel;
    private readonly Channel<ApprovalResponse> _responseChannel;

    public HumanApprovalWorkflow(Kernel kernel)
    {
        _kernel = kernel;
        _requestChannel = Channel.CreateBounded<ApprovalRequest>(10);
        _responseChannel = Channel.CreateBounded<ApprovalResponse>(10);
    }

    public ChannelReader<ApprovalRequest> PendingApprovals => _requestChannel.Reader;

    public async Task SubmitApprovalAsync(ApprovalResponse response)
    {
        await _responseChannel.Writer.WriteAsync(response);
    }

    public async Task<WorkflowResult> ExecuteAsync(string content)
    {
        var graph = new GraphExecutor("ApprovalWorkflow", "Workflow com aprovação");

        // Nó de análise
        var analyzeNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromPrompt("""
                Analise o conteúdo e identifique riscos:
                
                Conteúdo: {{$content}}
                
                Análise de risco:
                - Nível (LOW/MEDIUM/HIGH/CRITICAL)
                - Motivos
                - Recomendação (APPROVE/REVIEW/REJECT)
                """),
            nodeId: "analyze"
        ).StoreResultAs("analysis");

        // Nó de decisão
        var decisionNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (string analysis) =>
                {
                    if (analysis.Contains("CRITICAL") || analysis.Contains("HIGH"))
                        return "NEEDS_APPROVAL";
                    if (analysis.Contains("REVIEW"))
                        return "NEEDS_REVIEW";
                    return "AUTO_APPROVE";
                },
                "DecideRoute"
            ),
            nodeId: "decide"
        ).StoreResultAs("decision");

        // Nó de aprovação humana
        var approvalNode = new HumanApprovalGraphNode(
            "human_approval",
            _requestChannel.Writer,
            _responseChannel.Reader,
            timeout: TimeSpan.FromMinutes(30)
        );

        // Nó de execução
        var executeNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (string content, string approval) =>
                    $"Executado com aprovação: {approval}",
                "Execute"
            ),
            nodeId: "execute"
        ).StoreResultAs("result");

        // Nó de rejeição
        var rejectNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (string reason) => $"Rejeitado: {reason}",
                "Reject"
            ),
            nodeId: "reject"
        ).StoreResultAs("result");

        graph.AddNode(analyzeNode);
        graph.AddNode(decisionNode);
        graph.AddNode(approvalNode);
        graph.AddNode(executeNode);
        graph.AddNode(rejectNode);

        graph.Connect("analyze", "decide");
        graph.Connect("decide", "execute", ctx =>
            ctx.State.GetValue<string>("decision") == "AUTO_APPROVE");
        graph.Connect("decide", "human_approval", ctx =>
            ctx.State.GetValue<string>("decision") != "AUTO_APPROVE");
        graph.Connect("human_approval", "execute", ctx =>
            ctx.State.GetValue<bool>("approved"));
        graph.Connect("human_approval", "reject", ctx =>
            !ctx.State.GetValue<bool>("approved"));

        graph.SetStartNode("analyze");

        var result = await graph.ExecuteAsync(_kernel, new KernelArguments
        {
            ["content"] = content
        });

        return new WorkflowResult
        {
            Output = result.State.GetValue<string>("result"),
            Analysis = result.State.GetValue<string>("analysis"),
            RequiredApproval = result.State.GetValue<string>("decision") != "AUTO_APPROVE"
        };
    }
}

public class HumanApprovalGraphNode : IGraphNode
{
    private readonly ChannelWriter<ApprovalRequest> _requestWriter;
    private readonly ChannelReader<ApprovalResponse> _responseReader;
    private readonly TimeSpan _timeout;

    public string NodeId { get; }

    public HumanApprovalGraphNode(
        string nodeId,
        ChannelWriter<ApprovalRequest> requestWriter,
        ChannelReader<ApprovalResponse> responseReader,
        TimeSpan timeout)
    {
        NodeId = nodeId;
        _requestWriter = requestWriter;
        _responseReader = responseReader;
        _timeout = timeout;
    }

    public async Task<GraphNodeResult> ExecuteAsync(GraphExecutionContext context, CancellationToken cancellationToken)
    {
        var request = new ApprovalRequest
        {
            WorkflowId = context.WorkflowId,
            Content = context.State.GetValue<string>("content"),
            Analysis = context.State.GetValue<string>("analysis"),
            RequestTime = DateTimeOffset.UtcNow
        };

        await _requestWriter.WriteAsync(request, cancellationToken);

        using var cts = new CancellationTokenSource(_timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

        try
        {
            var response = await _responseReader.ReadAsync(linked.Token);
            context.State.SetValue("approved", response.Approved);
            context.State.SetValue("approval_comments", response.Comments ?? "");
            return GraphNodeResult.Success();
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            context.State.SetValue("approved", false);
            context.State.SetValue("approval_comments", "Timeout - aprovação não recebida");
            return GraphNodeResult.Success();
        }
    }
}

public class ApprovalRequest
{
    public string WorkflowId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public DateTimeOffset RequestTime { get; set; }
}

public class ApprovalResponse
{
    public string WorkflowId { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public string? Comments { get; set; }
    public string? ApprovedBy { get; set; }
}

public class WorkflowResult
{
    public string Output { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public bool RequiredApproval { get; set; }
}
```

---

## Boas Práticas

1. **Timeout**: Sempre defina timeout para aprovações
2. **Escalação**: Implemente escalação quando timeout expirar
3. **Auditoria**: Registre todas as decisões de aprovação
4. **Múltiplos Canais**: Suporte email, web, mobile

---

## Templates Relacionados

- [Graph Executor](template-skg-graph-executor.md) - Execução de grafos
- [Checkpointing](template-skg-checkpointing.md) - Persistência de estado

