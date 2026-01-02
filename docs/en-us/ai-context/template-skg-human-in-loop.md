# Human-in-the-Loop Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for implementing Human-in-the-Loop (HITL) workflows using Semantic Kernel Graph.

---

## Overview

Human-in-the-Loop enables human intervention during AI workflow execution for approval, validation, or decision-making. This template covers:
- Human approval workflows
- Confidence-based gates
- Multiple interaction channels
- Timeout and SLA policies
- Batch approval systems
- Audit trails

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| High-risk decisions | ✅ Recommended |
| Quality validation | ✅ Recommended |
| Regulatory compliance | ✅ Recommended |
| Content moderation | ✅ Recommended |
| Fully automated workflows | ⚠️ Use Graph Executor |
| Real-time processing | ⚠️ Consider async patterns |

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

## HITL Architecture

```
┌────────────────────────────────────────────────────────────┐
│                    HITL Workflow                            │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐     ┌──────────────────┐                 │
│  │  AI Process  │────▶│ Confidence Gate  │                 │
│  └──────────────┘     └────────┬─────────┘                 │
│                                │                            │
│                    ┌───────────┴───────────┐               │
│                    ▼                       ▼               │
│           [High Confidence]       [Low Confidence]         │
│                    │                       │               │
│                    │           ┌───────────▼────────────┐  │
│                    │           │  Human Approval Node   │  │
│                    │           │  - Console Channel     │  │
│                    │           │  - Web API Channel     │  │
│                    │           │  - Email Channel       │  │
│                    │           └───────────┬────────────┘  │
│                    │                       │               │
│                    │           ┌───────────┼───────────┐   │
│                    │           ▼           ▼           ▼   │
│                    │       [Approve]   [Reject]   [Modify] │
│                    │           │           │           │   │
│                    └───────────┴───────────┴───────────┘   │
│                                │                           │
│                    ┌───────────▼───────────┐               │
│                    │    Continue/Stop      │               │
│                    └───────────────────────┘               │
└────────────────────────────────────────────────────────────┘
```

---

## Core Components

### Interaction Channel Types

```csharp
/// <summary>
/// Types of human interaction channels available.
/// </summary>
public enum HumanInteractionChannelType
{
    Console,
    WebApi,
    Email,
    Slack,
    Teams,
    Custom
}

/// <summary>
/// Timeout actions when human response is not received.
/// </summary>
public enum TimeoutAction
{
    Approve,
    Reject,
    Escalate,
    UseDefault,
    Skip
}

/// <summary>
/// Confidence gate operational modes.
/// </summary>
public enum ConfidenceGateMode
{
    Permissive,   // Allow with warnings
    Strict,       // Require human approval
    Learning      // Adjust thresholds over time
}
```

### Configuration Models

```csharp
/// <summary>
/// Configuration for human interaction timeouts.
/// </summary>
public class HumanInteractionTimeout
{
    /// <summary>
    /// Primary timeout for human response.
    /// </summary>
    public TimeSpan PrimaryTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Warning timeout to notify pending expiration.
    /// </summary>
    public TimeSpan WarningTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Default action when timeout occurs.
    /// </summary>
    public TimeoutAction DefaultAction { get; set; } = TimeoutAction.Reject;

    /// <summary>
    /// Enable escalation on timeout.
    /// </summary>
    public bool EnableEscalation { get; set; } = true;

    /// <summary>
    /// Timeout for escalation response.
    /// </summary>
    public TimeSpan EscalationTimeout { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Configuration for batch approval processing.
/// </summary>
public class BatchApprovalOptions
{
    public int MaxBatchSize { get; set; } = 10;
    public TimeSpan BatchFormationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public bool AllowPartialApproval { get; set; } = true;
    public bool GroupByInterruptionType { get; set; } = true;
    public bool GroupByPriority { get; set; } = true;
}

/// <summary>
/// Audit configuration for compliance tracking.
/// </summary>
public class AuditConfiguration
{
    public bool TrackUserActions { get; set; } = true;
    public bool TrackTiming { get; set; } = true;
    public bool TrackContext { get; set; } = true;
    public bool EnableAuditLogging { get; set; } = true;
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(365);
}
```

---

## Implementation Patterns

### 1. Console Interaction Channel

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;

/// <summary>
/// Console-based human interaction channel for development and testing.
/// </summary>
public class ConsoleHumanInteractionChannel : IHumanInteractionChannel
{
    public HumanInteractionChannelType ChannelType => HumanInteractionChannelType.Console;
    public string ChannelName => "Console Interaction Channel";
    public bool IsAvailable => true;
    public bool SupportsBatchOperations => true;

    private bool _enableColors = true;
    private bool _showTimestamps = true;

    public Task InitializeAsync(Dictionary<string, object>? settings = null)
    {
        if (settings != null)
        {
            if (settings.TryGetValue("enable_colors", out var colors))
                _enableColors = Convert.ToBoolean(colors);
            if (settings.TryGetValue("show_timestamps", out var timestamps))
                _showTimestamps = Convert.ToBoolean(timestamps);
        }
        return Task.CompletedTask;
    }

    public async Task<HumanInterruptionResponse> SendInterruptionRequestAsync(
        HumanInterruptionRequest request,
        CancellationToken cancellationToken = default)
    {
        WriteHeader(request);
        WriteOptions(request.Options);

        var response = await WaitForUserInputAsync(request, cancellationToken);
        
        return response;
    }

    private void WriteHeader(HumanInterruptionRequest request)
    {
        var timestamp = _showTimestamps ? $"[{DateTime.Now:HH:mm:ss}] " : "";
        
        Console.WriteLine();
        Console.WriteLine($"{timestamp}═══════════════════════════════════════");
        Console.WriteLine($"{timestamp}📋 {request.Title}");
        Console.WriteLine($"{timestamp}═══════════════════════════════════════");
        Console.WriteLine($"{timestamp}{request.Description}");
        Console.WriteLine();
        
        if (request.Context.Any())
        {
            Console.WriteLine($"{timestamp}Context:");
            foreach (var item in request.Context)
            {
                Console.WriteLine($"{timestamp}  • {item.Key}: {item.Value}");
            }
            Console.WriteLine();
        }
    }

    private void WriteOptions(List<HumanInteractionOption> options)
    {
        Console.WriteLine("Available Options:");
        foreach (var option in options)
        {
            var defaultMarker = option.IsDefault ? " (default)" : "";
            Console.WriteLine($"  [{option.OptionId}] {option.DisplayText}{defaultMarker}");
            if (!string.IsNullOrEmpty(option.Description))
                Console.WriteLine($"      {option.Description}");
        }
        Console.WriteLine();
    }

    private async Task<HumanInterruptionResponse> WaitForUserInputAsync(
        HumanInterruptionRequest request,
        CancellationToken cancellationToken)
    {
        Console.Write("Your choice: ");
        var input = Console.ReadLine()?.Trim().ToLower();

        var selectedOption = request.Options.FirstOrDefault(o => 
            o.OptionId.ToLower() == input || o.DisplayText.ToLower().StartsWith(input ?? ""));

        if (selectedOption == null)
        {
            selectedOption = request.Options.FirstOrDefault(o => o.IsDefault);
        }

        return new HumanInterruptionResponse
        {
            RequestId = request.RequestId,
            Status = HumanInterruptionStatus.Completed,
            SelectedOption = selectedOption,
            Value = selectedOption?.Value,
            Timestamp = DateTime.UtcNow,
            UserId = Environment.UserName
        };
    }
}

public class HumanInterruptionRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<HumanInteractionOption> Options { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public string? Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class HumanInterruptionResponse
{
    public string RequestId { get; set; } = string.Empty;
    public HumanInterruptionStatus Status { get; set; }
    public HumanInteractionOption? SelectedOption { get; set; }
    public object? Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string? Comments { get; set; }
}

public class HumanInteractionOption
{
    public string OptionId { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public object? Value { get; set; }
    public bool IsDefault { get; set; }
    public string? Description { get; set; }
}

public enum HumanInterruptionStatus
{
    Pending,
    Completed,
    TimedOut,
    Cancelled,
    Escalated
}
```

### 2. Human Approval Node

```csharp
/// <summary>
/// Node that pauses execution for human approval.
/// </summary>
public class HumanApprovalNode
{
    private readonly string _nodeId;
    private readonly string _title;
    private readonly string _description;
    private readonly IHumanInteractionChannel _channel;
    private readonly List<HumanInteractionOption> _options = new();

    public HumanInteractionTimeout? TimeoutConfiguration { get; set; }
    public AuditConfiguration? AuditConfiguration { get; set; }
    public bool EnableAuditTrail { get; set; } = true;

    public event EventHandler<AuditEvent>? AuditEventRaised;

    public HumanApprovalNode(
        string nodeId,
        string title,
        string description,
        IHumanInteractionChannel channel)
    {
        _nodeId = nodeId;
        _title = title;
        _description = description;
        _channel = channel;

        // Default options
        AddApproveOption();
        AddRejectOption();
    }

    public HumanApprovalNode AddOption(HumanInteractionOption option)
    {
        _options.Add(option);
        return this;
    }

    public HumanApprovalNode AddApproveOption(string displayText = "Approve")
    {
        _options.Add(new HumanInteractionOption
        {
            OptionId = "approve",
            DisplayText = displayText,
            Value = true,
            IsDefault = true,
            Description = "Approve and continue processing"
        });
        return this;
    }

    public HumanApprovalNode AddRejectOption(string displayText = "Reject")
    {
        _options.Add(new HumanInteractionOption
        {
            OptionId = "reject",
            DisplayText = displayText,
            Value = false,
            Description = "Reject and stop processing"
        });
        return this;
    }

    public HumanApprovalNode AddModifyOption(string displayText = "Request Modifications")
    {
        _options.Add(new HumanInteractionOption
        {
            OptionId = "modify",
            DisplayText = displayText,
            Value = "modify",
            Description = "Request changes before approval"
        });
        return this;
    }

    public async Task<HumanApprovalResult> RequestApprovalAsync(
        KernelArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        var request = new HumanInterruptionRequest
        {
            Title = _title,
            Description = _description,
            Options = _options,
            Context = BuildContext(arguments)
        };

        HumanInterruptionResponse response;
        
        try
        {
            if (TimeoutConfiguration != null)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeoutConfiguration.PrimaryTimeout);

                response = await _channel.SendInterruptionRequestAsync(request, timeoutCts.Token);
            }
            else
            {
                response = await _channel.SendInterruptionRequestAsync(request, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred
            response = HandleTimeout(request);
        }

        var result = new HumanApprovalResult
        {
            NodeId = _nodeId,
            RequestId = request.RequestId,
            IsApproved = response.Value is true,
            SelectedOption = response.SelectedOption?.OptionId,
            Value = response.Value,
            UserId = response.UserId,
            Comments = response.Comments,
            Duration = DateTime.UtcNow - startTime,
            Status = response.Status
        };

        // Raise audit event
        if (EnableAuditTrail)
        {
            RaiseAuditEvent(result);
        }

        // Store result in arguments
        arguments["approval_result"] = result;
        arguments["is_approved"] = result.IsApproved;

        return result;
    }

    private Dictionary<string, object> BuildContext(KernelArguments arguments)
    {
        var context = new Dictionary<string, object>();
        
        foreach (var kvp in arguments)
        {
            if (kvp.Value != null)
                context[kvp.Key] = kvp.Value;
        }

        return context;
    }

    private HumanInterruptionResponse HandleTimeout(HumanInterruptionRequest request)
    {
        var action = TimeoutConfiguration?.DefaultAction ?? TimeoutAction.Reject;
        
        return new HumanInterruptionResponse
        {
            RequestId = request.RequestId,
            Status = HumanInterruptionStatus.TimedOut,
            Value = action switch
            {
                TimeoutAction.Approve => true,
                TimeoutAction.Reject => false,
                TimeoutAction.Skip => null,
                _ => false
            },
            Timestamp = DateTime.UtcNow
        };
    }

    private void RaiseAuditEvent(HumanApprovalResult result)
    {
        AuditEventRaised?.Invoke(this, new AuditEvent
        {
            Timestamp = DateTime.UtcNow,
            UserId = result.UserId,
            Action = result.SelectedOption ?? "unknown",
            Context = new Dictionary<string, object>
            {
                ["node_id"] = _nodeId,
                ["request_id"] = result.RequestId,
                ["is_approved"] = result.IsApproved,
                ["status"] = result.Status.ToString()
            },
            Duration = result.Duration
        });
    }
}

public class HumanApprovalResult
{
    public string NodeId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? SelectedOption { get; set; }
    public object? Value { get; set; }
    public string? UserId { get; set; }
    public string? Comments { get; set; }
    public TimeSpan Duration { get; set; }
    public HumanInterruptionStatus Status { get; set; }
}

public class AuditEvent
{
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public TimeSpan Duration { get; set; }
}
```

### 3. Confidence Gate Node

```csharp
/// <summary>
/// Node that routes execution based on confidence scores.
/// </summary>
public class ConfidenceGateNode
{
    private readonly double _threshold;
    private readonly string _nodeId;
    private Func<KernelArguments, double>? _confidenceSource;
    private Dictionary<string, Func<KernelArguments, double>>? _weightedSources;

    public ConfidenceGateMode Mode { get; set; } = ConfidenceGateMode.Strict;
    public bool AllowManualBypass { get; set; }
    public bool RequireHumanApproval { get; set; } = true;
    public bool EnableUncertaintyAnalysis { get; set; }
    public double UncertaintyThreshold { get; set; } = 0.3;

    public string? HighConfidenceNodeId { get; set; }
    public string? LowConfidenceNodeId { get; set; }

    public ConfidenceGateNode(double threshold, string nodeId)
    {
        _threshold = threshold;
        _nodeId = nodeId;
    }

    public ConfidenceGateNode SetConfidenceSource(Func<KernelArguments, double> source)
    {
        _confidenceSource = source;
        return this;
    }

    public ConfidenceGateNode SetWeightedSources(Dictionary<string, Func<KernelArguments, double>> sources)
    {
        _weightedSources = sources;
        return this;
    }

    public ConfidenceGateResult Evaluate(KernelArguments arguments)
    {
        var confidence = CalculateConfidence(arguments);
        var passed = confidence >= _threshold;

        var result = new ConfidenceGateResult
        {
            NodeId = _nodeId,
            Confidence = confidence,
            Threshold = _threshold,
            Passed = passed,
            Mode = Mode,
            NextNodeId = passed ? HighConfidenceNodeId : LowConfidenceNodeId,
            RequiresHumanApproval = !passed && RequireHumanApproval
        };

        if (EnableUncertaintyAnalysis)
        {
            result.UncertaintyScore = CalculateUncertainty(arguments);
            result.IsHighUncertainty = result.UncertaintyScore > UncertaintyThreshold;
        }

        // Store result
        arguments["confidence_gate_result"] = result;
        arguments["confidence_score"] = confidence;
        arguments["confidence_passed"] = passed;

        return result;
    }

    private double CalculateConfidence(KernelArguments arguments)
    {
        if (_weightedSources != null && _weightedSources.Any())
        {
            return _weightedSources.Sum(kvp => kvp.Value(arguments));
        }

        return _confidenceSource?.Invoke(arguments) ?? 0.5;
    }

    private double CalculateUncertainty(KernelArguments arguments)
    {
        var confidence = CalculateConfidence(arguments);
        // Simple uncertainty: distance from extremes (0 or 1)
        return 1 - Math.Abs(confidence - 0.5) * 2;
    }
}

public class ConfidenceGateResult
{
    public string NodeId { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public double Threshold { get; set; }
    public bool Passed { get; set; }
    public ConfidenceGateMode Mode { get; set; }
    public string? NextNodeId { get; set; }
    public bool RequiresHumanApproval { get; set; }
    public double UncertaintyScore { get; set; }
    public bool IsHighUncertainty { get; set; }
}
```

### 4. HITL Workflow Builder

```csharp
/// <summary>
/// Builder for creating HITL-enabled graph workflows.
/// </summary>
public class HITLWorkflowBuilder
{
    private readonly Kernel _kernel;
    private readonly IHumanInteractionChannel _channel;
    private readonly GraphExecutor _executor;

    public HITLWorkflowBuilder(Kernel kernel, IHumanInteractionChannel channel)
    {
        _kernel = kernel;
        _channel = channel;
        _executor = new GraphExecutor("HITLWorkflow", "Workflow with human-in-the-loop");
    }

    public GraphExecutor Build()
    {
        return _executor;
    }

    /// <summary>
    /// Adds a processing node.
    /// </summary>
    public HITLWorkflowBuilder AddProcessingNode(
        string nodeId,
        string description,
        Func<KernelArguments, Task<string>> processor)
    {
        var node = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(processor, nodeId, description),
            nodeId,
            description);

        _executor.AddNode(node);
        return this;
    }

    /// <summary>
    /// Adds a confidence gate that routes based on confidence scores.
    /// </summary>
    public HITLWorkflowBuilder AddConfidenceGate(
        string nodeId,
        double threshold,
        string confidenceKey,
        string highConfidenceNodeId,
        string lowConfidenceNodeId)
    {
        var gate = new ConfidenceGateNode(threshold, nodeId);
        gate.SetConfidenceSource(args => 
            args.TryGetValue(confidenceKey, out var conf) ? Convert.ToDouble(conf) : 0.5);
        gate.HighConfidenceNodeId = highConfidenceNodeId;
        gate.LowConfidenceNodeId = lowConfidenceNodeId;

        var gateNode = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var result = gate.Evaluate(args);
                    return result.Passed ? "High confidence - proceeding" : "Low confidence - review needed";
                },
                nodeId,
                $"Confidence gate (threshold: {threshold})"),
            nodeId);

        _executor.AddNode(gateNode);
        return this;
    }

    /// <summary>
    /// Adds a human approval node.
    /// </summary>
    public HITLWorkflowBuilder AddHumanApproval(
        string nodeId,
        string title,
        string description,
        HumanInteractionTimeout? timeout = null)
    {
        var approvalNode = new HumanApprovalNode(nodeId, title, description, _channel);
        
        if (timeout != null)
            approvalNode.TimeoutConfiguration = timeout;

        var node = new FunctionGraphNode(
            _kernel.CreateFunctionFromMethod(
                async (KernelArguments args) =>
                {
                    var result = await approvalNode.RequestApprovalAsync(args);
                    return result.IsApproved ? "Approved" : "Rejected";
                },
                nodeId,
                description),
            nodeId);

        _executor.AddNode(node);
        return this;
    }

    /// <summary>
    /// Adds conditional routing based on approval result.
    /// </summary>
    public HITLWorkflowBuilder AddConditionalRouting(
        string sourceNodeId,
        string approvedNodeId,
        string rejectedNodeId)
    {
        // Connect based on approval result
        _executor.ConnectConditional(sourceNodeId, approvedNodeId, 
            args => args.TryGetValue("is_approved", out var approved) && (bool)approved!);
        _executor.ConnectConditional(sourceNodeId, rejectedNodeId,
            args => !args.TryGetValue("is_approved", out var approved) || !(bool)approved!);

        return this;
    }

    /// <summary>
    /// Connects nodes sequentially.
    /// </summary>
    public HITLWorkflowBuilder Connect(string fromNodeId, string toNodeId)
    {
        _executor.Connect(fromNodeId, toNodeId);
        return this;
    }

    /// <summary>
    /// Sets the start node.
    /// </summary>
    public HITLWorkflowBuilder SetStartNode(string nodeId)
    {
        _executor.SetStartNode(nodeId);
        return this;
    }
}
```

### 5. Complete HITL Workflow Example

```csharp
public class HITLWorkflowExample
{
    private readonly Kernel _kernel;
    private readonly IHumanInteractionChannel _channel;

    public HITLWorkflowExample(Kernel kernel)
    {
        _kernel = kernel;
        _channel = new ConsoleHumanInteractionChannel();
    }

    public GraphExecutor CreateContentModerationWorkflow()
    {
        var builder = new HITLWorkflowBuilder(_kernel, _channel);

        // Stage 1: AI Content Analysis
        builder.AddProcessingNode(
            "analyze-content",
            "Analyze content for policy violations",
            async args =>
            {
                var content = args["content"]?.ToString() ?? string.Empty;
                
                // Simulate AI analysis
                var hasIssues = content.ToLower().Contains("spam") || 
                               content.ToLower().Contains("offensive");
                var confidence = hasIssues ? 0.3 : 0.9;

                args["analysis_result"] = hasIssues ? "potential_violation" : "clean";
                args["confidence"] = confidence;
                args["analysis_details"] = new
                {
                    ContentLength = content.Length,
                    HasIssues = hasIssues,
                    Confidence = confidence,
                    AnalyzedAt = DateTime.UtcNow
                };

                return $"Content analyzed: {(hasIssues ? "Issues detected" : "No issues")} (confidence: {confidence:P0})";
            });

        // Stage 2: Confidence Gate
        builder.AddConfidenceGate(
            "confidence-gate",
            threshold: 0.7,
            confidenceKey: "confidence",
            highConfidenceNodeId: "auto-approve",
            lowConfidenceNodeId: "human-review");

        // Stage 3a: Auto-approve for high confidence
        builder.AddProcessingNode(
            "auto-approve",
            "Auto-approve clean content",
            async args =>
            {
                args["decision"] = "approved";
                args["decision_type"] = "automatic";
                return "Content auto-approved";
            });

        // Stage 3b: Human review for low confidence
        builder.AddHumanApproval(
            "human-review",
            "Content Review Required",
            "Please review the flagged content for policy compliance",
            new HumanInteractionTimeout
            {
                PrimaryTimeout = TimeSpan.FromMinutes(15),
                DefaultAction = TimeoutAction.Reject
            });

        // Stage 4: Handle approval result
        builder.AddProcessingNode(
            "handle-approved",
            "Process approved content",
            async args =>
            {
                args["decision"] = "approved";
                args["decision_type"] = "human";
                return "Content approved by human reviewer";
            });

        builder.AddProcessingNode(
            "handle-rejected",
            "Process rejected content",
            async args =>
            {
                args["decision"] = "rejected";
                args["decision_type"] = "human";
                return "Content rejected by human reviewer";
            });

        // Build connections
        builder.SetStartNode("analyze-content");
        builder.Connect("analyze-content", "confidence-gate");
        builder.AddConditionalRouting("human-review", "handle-approved", "handle-rejected");

        return builder.Build();
    }
}
```

---

## Service Layer Integration

```csharp
public interface IHITLService
{
    Task<ContentModerationResult> ModerateContentAsync(string content, CancellationToken cancellationToken = default);
    Task<ApprovalStatus> GetApprovalStatusAsync(string requestId, CancellationToken cancellationToken = default);
}

public class HITLService : IHITLService
{
    private readonly Kernel _kernel;
    private readonly GraphExecutor _moderationWorkflow;
    private readonly ILogger<HITLService> _logger;

    public HITLService(Kernel kernel, ILoggerFactory loggerFactory)
    {
        _kernel = kernel;
        _logger = loggerFactory.CreateLogger<HITLService>();
        
        var example = new HITLWorkflowExample(kernel);
        _moderationWorkflow = example.CreateContentModerationWorkflow();
    }

    public async Task<ContentModerationResult> ModerateContentAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        var arguments = new KernelArguments
        {
            ["content"] = content
        };

        await _moderationWorkflow.ExecuteAsync(_kernel, arguments, cancellationToken);

        return new ContentModerationResult
        {
            Decision = arguments["decision"]?.ToString() ?? "unknown",
            DecisionType = arguments["decision_type"]?.ToString() ?? "unknown",
            Confidence = Convert.ToDouble(arguments["confidence"] ?? 0),
            AnalysisDetails = arguments["analysis_details"],
            Timestamp = DateTime.UtcNow
        };
    }

    public Task<ApprovalStatus> GetApprovalStatusAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        // Implementation would query approval store
        return Task.FromResult(new ApprovalStatus
        {
            RequestId = requestId,
            Status = "pending"
        });
    }
}

public class ContentModerationResult
{
    public string Decision { get; set; } = string.Empty;
    public string DecisionType { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public object? AnalysisDetails { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ApprovalStatus
{
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
```

---

## Dependency Injection Setup

```csharp
public static class HITLServiceExtensions
{
    public static IServiceCollection AddHITLServices(
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

        // Register interaction channel
        services.AddSingleton<IHumanInteractionChannel, ConsoleHumanInteractionChannel>();

        // Register HITL service
        services.AddScoped<IHITLService, HITLService>();

        return services;
    }
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class ModerationController : ControllerBase
{
    private readonly IHITLService _hitlService;
    private readonly ILogger<ModerationController> _logger;

    public ModerationController(IHITLService hitlService, ILogger<ModerationController> logger)
    {
        _hitlService = hitlService;
        _logger = logger;
    }

    [HttpPost("moderate")]
    public async Task<IActionResult> ModerateContent(
        [FromBody] ModerateContentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _hitlService.ModerateContentAsync(request.Content, cancellationToken);
        return Ok(result);
    }

    [HttpGet("status/{requestId}")]
    public async Task<IActionResult> GetApprovalStatus(
        string requestId,
        CancellationToken cancellationToken)
    {
        var status = await _hitlService.GetApprovalStatusAsync(requestId, cancellationToken);
        return Ok(status);
    }

    [HttpPost("approve/{requestId}")]
    public async Task<IActionResult> ApproveRequest(
        string requestId,
        [FromBody] ApprovalDecision decision,
        CancellationToken cancellationToken)
    {
        // Handle approval submission
        return Ok(new { requestId, decision.Approved, decision.Comments });
    }
}

public record ModerateContentRequest(string Content);
public record ApprovalDecision(bool Approved, string? Comments);
```

---

## Testing

```csharp
using Xunit;

public class HITLTests
{
    [Fact]
    public async Task HighConfidenceContent_AutoApproved()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var example = new HITLWorkflowExample(kernel);
        var workflow = example.CreateContentModerationWorkflow();

        var arguments = new KernelArguments
        {
            ["content"] = "This is normal, clean content without any issues."
        };

        // Act
        await workflow.ExecuteAsync(kernel, arguments);

        // Assert
        Assert.Equal("approved", arguments["decision"]?.ToString());
        Assert.Equal("automatic", arguments["decision_type"]?.ToString());
    }

    [Fact]
    public void ConfidenceGate_RoutesCorrectly()
    {
        // Arrange
        var gate = new ConfidenceGateNode(0.7, "test-gate");
        gate.SetConfidenceSource(args => Convert.ToDouble(args["confidence"]));
        gate.HighConfidenceNodeId = "high";
        gate.LowConfidenceNodeId = "low";

        // Act - High confidence
        var highArgs = new KernelArguments { ["confidence"] = 0.9 };
        var highResult = gate.Evaluate(highArgs);

        // Act - Low confidence
        var lowArgs = new KernelArguments { ["confidence"] = 0.5 };
        var lowResult = gate.Evaluate(lowArgs);

        // Assert
        Assert.True(highResult.Passed);
        Assert.Equal("high", highResult.NextNodeId);
        Assert.False(lowResult.Passed);
        Assert.Equal("low", lowResult.NextNodeId);
    }

    [Fact]
    public void HumanApprovalNode_ConfiguresOptions()
    {
        // Arrange
        var channel = new ConsoleHumanInteractionChannel();
        var node = new HumanApprovalNode(
            "test-approval",
            "Test Approval",
            "Please approve",
            channel);

        // Act
        node.AddModifyOption("Request Changes");

        // Assert - node should have 3 options (approve, reject, modify)
        // (Would need to expose options for full testing)
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

### Approval Design

1. **Clear Context**: Provide all relevant information for decision-making
2. **Multiple Options**: Offer approve/reject/modify when appropriate
3. **Time Sensitivity**: Configure appropriate timeouts based on urgency
4. **Audit Trail**: Enable comprehensive logging for compliance

### Confidence Gates

1. **Appropriate Thresholds**: Set based on risk tolerance and domain
2. **Multiple Sources**: Combine confidence indicators for robustness
3. **Fallback Paths**: Always provide clear routing for low confidence
4. **Learning Mode**: Adjust thresholds based on feedback over time

### Channel Selection

1. **Development**: Console channel for testing
2. **Production**: Web API or email for real deployments
3. **Batch Processing**: Use batch manager for high-volume scenarios
4. **Integration**: Connect with existing approval systems

### Performance

1. **Async Processing**: Don't block on human responses
2. **Timeout Handling**: Always configure fallback actions
3. **Escalation**: Implement escalation chains for critical decisions
4. **Batch Grouping**: Group similar requests for efficiency

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [Multi-Agent](template-skg-multi-agent.md) - Coordinated agents
- [Checkpointing](template-skg-checkpointing.md) - State persistence
- [Streaming](template-skg-streaming.md) - Real-time events

---

## External References

- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)
- [Human-in-the-Loop Best Practices](https://learn.microsoft.com/azure/machine-learning/concept-human-in-loop)
- [Responsible AI Practices](https://www.microsoft.com/ai/responsible-ai)
