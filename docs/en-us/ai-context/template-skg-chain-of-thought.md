# Chain of Thought Template - Semantic Kernel Graph

> **Purpose**: This template provides AI agents with patterns for implementing Chain-of-Thought (CoT) reasoning using Semantic Kernel Graph.

---

## Overview

Chain-of-Thought enables AI to break down complex problems into logical reasoning steps. This template covers:
- Step-by-step reasoning patterns
- Reasoning validation and confidence scoring
- Backtracking mechanisms
- Custom reasoning templates
- Performance optimization

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Complex problem solving | ✅ Recommended |
| Multi-step analysis | ✅ Recommended |
| Decision making | ✅ Recommended |
| Math/logic problems | ✅ Recommended |
| Simple Q&A | ⚠️ Use Chat Completion |
| Tool-heavy workflows | ⚠️ Use ReAct Agent |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SemanticKernel" Version="1.*" />
  <PackageReference Include="SemanticKernel.Graph" Version="1.*" />
</ItemGroup>
```

---

## Chain of Thought Pattern

```
┌─────────────────┐
│ Problem Input   │
└────────┬────────┘
         ▼
┌─────────────────┐
│ Step 1: Analyze │──► Validate ──► Confidence: 0.85
└────────┬────────┘
         ▼
┌─────────────────┐
│ Step 2: Reason  │──► Validate ──► Confidence: 0.78
└────────┬────────┘
         ▼
┌─────────────────┐
│ Step 3: Deduce  │──► Validate ──► Confidence: 0.82
└────────┬────────┘
         ▼
┌─────────────────┐
│ Final Answer    │
└─────────────────┘
```

---

## Implementation Patterns

### 1. Basic Chain of Thought

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Graph.Core;
using SemanticKernel.Graph.Nodes;

public class ChainOfThoughtBuilder
{
    public static GraphExecutor CreateChainOfThought(Kernel kernel, int maxSteps = 5)
    {
        var executor = new GraphExecutor("ChainOfThought", "Step-by-step reasoning");

        // Initial analysis step
        var analysisNode = new FunctionGraphNode(
            CreateAnalysisFunction(kernel),
            "analysis",
            "Initial problem analysis")
            .StoreResultAs("step_1");

        // Reasoning steps
        var reasoningNodes = new List<FunctionGraphNode>();
        for (int i = 2; i <= maxSteps; i++)
        {
            var node = new FunctionGraphNode(
                CreateReasoningStepFunction(kernel, i),
                $"reasoning-{i}",
                $"Reasoning step {i}")
                .StoreResultAs($"step_{i}");
            reasoningNodes.Add(node);
        }

        // Conclusion step
        var conclusionNode = new FunctionGraphNode(
            CreateConclusionFunction(kernel, maxSteps),
            "conclusion",
            "Final conclusion")
            .StoreResultAs("final_answer");

        // Build graph
        executor.AddNode(analysisNode);
        foreach (var node in reasoningNodes)
            executor.AddNode(node);
        executor.AddNode(conclusionNode);

        // Connect sequentially
        executor.SetStartNode(analysisNode.NodeId);
        
        var previousNode = analysisNode;
        foreach (var node in reasoningNodes)
        {
            executor.Connect(previousNode.NodeId, node.NodeId);
            previousNode = node;
        }
        executor.Connect(previousNode.NodeId, conclusionNode.NodeId);

        return executor;
    }

    private static KernelFunction CreateAnalysisFunction(Kernel kernel)
    {
        return kernel.CreateFunctionFromPrompt(
            """
            You are solving a problem step by step. This is Step 1: Initial Analysis.

            Problem: {{$problem}}

            Analyze the problem by:
            1. Identifying the key elements
            2. Understanding what is being asked
            3. Noting any constraints or conditions

            Step 1 Analysis:
            """,
            functionName: "Analysis",
            description: "Initial problem analysis");
    }

    private static KernelFunction CreateReasoningStepFunction(Kernel kernel, int stepNumber)
    {
        return kernel.CreateFunctionFromPrompt(
            $"""
            You are solving a problem step by step. This is Step {stepNumber}.

            Problem: {{{{$problem}}}}

            Previous reasoning:
            {{{{$previous_steps}}}}

            Continue the reasoning by building on previous steps.
            Focus on logical deductions and connections.

            Step {stepNumber}:
            """,
            functionName: $"ReasoningStep{stepNumber}",
            description: $"Reasoning step {stepNumber}");
    }

    private static KernelFunction CreateConclusionFunction(Kernel kernel, int totalSteps)
    {
        return kernel.CreateFunctionFromPrompt(
            """
            Based on all the reasoning steps, provide a final answer.

            Problem: {{$problem}}

            All reasoning steps:
            {{$all_steps}}

            Final Answer (be concise and clear):
            """,
            functionName: "Conclusion",
            description: "Final conclusion");
    }
}
```

### 2. Chain of Thought with Validation

```csharp
public class ValidatedChainOfThought
{
    public static GraphExecutor CreateValidatedCoT(Kernel kernel)
    {
        var executor = new GraphExecutor("ValidatedCoT", "Chain of thought with validation");

        // Problem analysis
        var analysisNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Analyze this problem and identify key components:

                Problem: {{$problem}}

                Provide your analysis:
                """,
                functionName: "Analyze"),
            "analysis")
            .StoreResultAs("analysis");

        // Reasoning with self-validation
        var reasoningNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Based on the analysis, reason through the problem step by step.

                Problem: {{$problem}}
                Analysis: {{$analysis}}

                For each step:
                1. State your reasoning
                2. Rate your confidence (0-100%)
                3. Check if the step is logical

                Reasoning Steps:
                """,
                functionName: "Reason"),
            "reasoning")
            .StoreResultAs("reasoning");

        // Validation node
        var validationNode = new FunctionGraphNode(
            kernel.CreateFunctionFromMethod(
                (KernelArguments args) =>
                {
                    var reasoning = args.GetOrCreateGraphState().GetValue<string>("reasoning") ?? "";
                    var validation = ValidateReasoning(reasoning);
                    
                    args["validation_result"] = validation;
                    args["is_valid"] = validation.IsValid;
                    args["confidence"] = validation.AverageConfidence;
                    
                    return validation.IsValid 
                        ? $"Valid reasoning (confidence: {validation.AverageConfidence:P})"
                        : $"Invalid reasoning: {validation.Issues}";
                },
                functionName: "Validate",
                description: "Validates reasoning"),
            "validation")
            .StoreResultAs("validation");

        // Conditional routing based on validation
        var conclusionNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                Provide the final answer based on validated reasoning.

                Problem: {{$problem}}
                Reasoning: {{$reasoning}}
                Validation: {{$validation}}

                Final Answer:
                """,
                functionName: "Conclude"),
            "conclusion")
            .StoreResultAs("final_answer");

        var retryNode = new FunctionGraphNode(
            kernel.CreateFunctionFromPrompt(
                """
                The previous reasoning had issues. Please try again with a different approach.

                Problem: {{$problem}}
                Previous attempt: {{$reasoning}}
                Issues: {{$validation}}

                New Reasoning (be more careful):
                """,
                functionName: "Retry"),
            "retry")
            .StoreResultAs("reasoning");

        // Build graph
        executor.AddNode(analysisNode);
        executor.AddNode(reasoningNode);
        executor.AddNode(validationNode);
        executor.AddNode(conclusionNode);
        executor.AddNode(retryNode);

        executor.SetStartNode(analysisNode.NodeId);
        executor.Connect(analysisNode.NodeId, reasoningNode.NodeId);
        executor.Connect(reasoningNode.NodeId, validationNode.NodeId);

        // Conditional routing
        executor.AddEdge(ConditionalEdge.Create(
            validationNode, conclusionNode,
            ctx => ctx.TryGetValue("is_valid", out var v) && v is true));

        executor.AddEdge(ConditionalEdge.Create(
            validationNode, retryNode,
            ctx => !ctx.TryGetValue("is_valid", out var v) || v is not true));

        executor.Connect(retryNode.NodeId, validationNode.NodeId);

        return executor;
    }

    private static ValidationResult ValidateReasoning(string reasoning)
    {
        var result = new ValidationResult();

        // Check for confidence indicators
        var confidenceMatches = System.Text.RegularExpressions.Regex.Matches(
            reasoning, @"(\d+)%");
        
        if (confidenceMatches.Count > 0)
        {
            var confidences = confidenceMatches
                .Select(m => double.Parse(m.Groups[1].Value) / 100)
                .ToList();
            result.AverageConfidence = confidences.Average();
        }
        else
        {
            result.AverageConfidence = 0.5; // Default if no confidence stated
        }

        // Basic validation checks
        result.IsValid = reasoning.Length > 50 && result.AverageConfidence >= 0.6;

        if (!result.IsValid)
        {
            result.Issues = result.AverageConfidence < 0.6 
                ? "Low confidence in reasoning steps"
                : "Reasoning too brief";
        }

        return result;
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public double AverageConfidence { get; set; }
        public string Issues { get; set; } = string.Empty;
    }
}
```

### 3. Domain-Specific Chain of Thought

```csharp
public class DomainSpecificCoT
{
    public enum ReasoningDomain
    {
        Mathematical,
        Analytical,
        DecisionMaking,
        ProblemSolving
    }

    public static GraphExecutor CreateDomainCoT(Kernel kernel, ReasoningDomain domain)
    {
        var template = GetDomainTemplate(domain);
        var executor = new GraphExecutor($"{domain}CoT", $"Chain of thought for {domain}");

        var steps = template.Steps.Select((step, index) =>
            new FunctionGraphNode(
                kernel.CreateFunctionFromPrompt(
                    step.PromptTemplate,
                    functionName: step.Name,
                    description: step.Description),
                $"step-{index}")
                .StoreResultAs(step.OutputKey))
            .ToList();

        foreach (var step in steps)
            executor.AddNode(step);

        executor.SetStartNode(steps[0].NodeId);

        for (int i = 0; i < steps.Count - 1; i++)
            executor.Connect(steps[i].NodeId, steps[i + 1].NodeId);

        return executor;
    }

    private static DomainTemplate GetDomainTemplate(ReasoningDomain domain)
    {
        return domain switch
        {
            ReasoningDomain.Mathematical => new DomainTemplate
            {
                Steps = new[]
                {
                    new StepTemplate
                    {
                        Name = "Identify",
                        Description = "Identify known values and unknowns",
                        OutputKey = "identification",
                        PromptTemplate = """
                            Mathematical Problem: {{$problem}}

                            Step 1 - Identify:
                            - Known values: List all given numbers and facts
                            - Unknown values: What we need to find
                            - Relevant formulas: What mathematical concepts apply

                            Identification:
                            """
                    },
                    new StepTemplate
                    {
                        Name = "Setup",
                        Description = "Set up equations and approach",
                        OutputKey = "setup",
                        PromptTemplate = """
                            Problem: {{$problem}}
                            Identification: {{$identification}}

                            Step 2 - Setup:
                            - Write the equation(s) to solve
                            - Choose the solving method
                            - Note any simplifications

                            Setup:
                            """
                    },
                    new StepTemplate
                    {
                        Name = "Solve",
                        Description = "Execute calculations",
                        OutputKey = "solution",
                        PromptTemplate = """
                            Problem: {{$problem}}
                            Setup: {{$setup}}

                            Step 3 - Solve:
                            - Execute calculations step by step
                            - Show all work
                            - Verify each step

                            Solution:
                            """
                    },
                    new StepTemplate
                    {
                        Name = "Verify",
                        Description = "Check the answer",
                        OutputKey = "final_answer",
                        PromptTemplate = """
                            Problem: {{$problem}}
                            Solution: {{$solution}}

                            Step 4 - Verify:
                            - Check if the answer makes sense
                            - Substitute back to verify
                            - State the final answer clearly

                            Final Answer:
                            """
                    }
                }
            },

            ReasoningDomain.DecisionMaking => new DomainTemplate
            {
                Steps = new[]
                {
                    new StepTemplate
                    {
                        Name = "FrameProblem",
                        Description = "Define the decision to be made",
                        OutputKey = "framing",
                        PromptTemplate = """
                            Decision Context: {{$problem}}

                            Step 1 - Frame the Decision:
                            - What decision needs to be made?
                            - What are the constraints?
                            - Who are the stakeholders?
                            - What is the timeline?

                            Problem Framing:
                            """
                    },
                    new StepTemplate
                    {
                        Name = "GenerateOptions",
                        Description = "Generate possible options",
                        OutputKey = "options",
                        PromptTemplate = """
                            Context: {{$problem}}
                            Framing: {{$framing}}

                            Step 2 - Generate Options:
                            List at least 3 viable options, including:
                            - The obvious choice
                            - A creative alternative
                            - A conservative option

                            Options:
                            """
                    },
                    new StepTemplate
                    {
                        Name = "EvaluateOptions",
                        Description = "Evaluate pros and cons",
                        OutputKey = "evaluation",
                        PromptTemplate = """
                            Context: {{$problem}}
                            Options: {{$options}}

                            Step 3 - Evaluate Options:
                            For each option, analyze:
                            - Pros (benefits)
                            - Cons (risks/costs)
                            - Probability of success
                            - Alignment with goals

                            Evaluation:
                            """
                    },
                    new StepTemplate
                    {
                        Name = "Decide",
                        Description = "Make the decision",
                        OutputKey = "final_answer",
                        PromptTemplate = """
                            Context: {{$problem}}
                            Evaluation: {{$evaluation}}

                            Step 4 - Decision:
                            - State the recommended decision
                            - Explain the reasoning
                            - Suggest next steps
                            - Note any contingencies

                            Decision:
                            """
                    }
                }
            },

            _ => GetDefaultTemplate()
        };
    }

    private static DomainTemplate GetDefaultTemplate()
    {
        return new DomainTemplate
        {
            Steps = new[]
            {
                new StepTemplate
                {
                    Name = "Understand",
                    OutputKey = "understanding",
                    PromptTemplate = "Problem: {{$problem}}\n\nStep 1 - Understand:\n"
                },
                new StepTemplate
                {
                    Name = "Plan",
                    OutputKey = "plan",
                    PromptTemplate = "Understanding: {{$understanding}}\n\nStep 2 - Plan:\n"
                },
                new StepTemplate
                {
                    Name = "Execute",
                    OutputKey = "execution",
                    PromptTemplate = "Plan: {{$plan}}\n\nStep 3 - Execute:\n"
                },
                new StepTemplate
                {
                    Name = "Review",
                    OutputKey = "final_answer",
                    PromptTemplate = "Execution: {{$execution}}\n\nStep 4 - Final Answer:\n"
                }
            }
        };
    }

    private class DomainTemplate
    {
        public StepTemplate[] Steps { get; set; } = Array.Empty<StepTemplate>();
    }

    private class StepTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OutputKey { get; set; } = string.Empty;
        public string PromptTemplate { get; set; } = string.Empty;
    }
}
```

### 4. Chain of Thought with Backtracking

```csharp
public class BacktrackingCoT
{
    private readonly Kernel _kernel;
    private readonly int _maxBacktracks;
    private readonly double _minConfidence;

    public BacktrackingCoT(Kernel kernel, int maxBacktracks = 3, double minConfidence = 0.6)
    {
        _kernel = kernel;
        _maxBacktracks = maxBacktracks;
        _minConfidence = minConfidence;
    }

    public async Task<CoTResult> SolveAsync(string problem, CancellationToken cancellationToken = default)
    {
        var result = new CoTResult { Problem = problem };
        var steps = new List<ReasoningStep>();
        var backtrackCount = 0;

        // Step 1: Initial analysis
        var analysis = await AnalyzeAsync(problem, cancellationToken);
        steps.Add(new ReasoningStep { StepNumber = 1, Content = analysis, Type = "Analysis" });

        // Iterative reasoning with backtracking
        var currentStep = 2;
        while (currentStep <= 5 && backtrackCount < _maxBacktracks)
        {
            var stepResult = await ReasonStepAsync(problem, steps, currentStep, cancellationToken);
            
            // Validate step
            var confidence = await ValidateStepAsync(stepResult, cancellationToken);
            
            if (confidence >= _minConfidence)
            {
                steps.Add(new ReasoningStep
                {
                    StepNumber = currentStep,
                    Content = stepResult,
                    Confidence = confidence,
                    Type = "Reasoning"
                });
                currentStep++;
            }
            else
            {
                // Backtrack
                backtrackCount++;
                result.BacktrackEvents.Add($"Step {currentStep}: Low confidence ({confidence:P}), retrying");
                
                // Try alternative approach
                var alternative = await RetryWithAlternativeAsync(problem, steps, currentStep, cancellationToken);
                var altConfidence = await ValidateStepAsync(alternative, cancellationToken);
                
                if (altConfidence >= _minConfidence)
                {
                    steps.Add(new ReasoningStep
                    {
                        StepNumber = currentStep,
                        Content = alternative,
                        Confidence = altConfidence,
                        Type = "Alternative"
                    });
                    currentStep++;
                }
            }
        }

        // Conclusion
        var conclusion = await ConcludeAsync(problem, steps, cancellationToken);
        steps.Add(new ReasoningStep { StepNumber = currentStep, Content = conclusion, Type = "Conclusion" });

        result.Steps = steps;
        result.FinalAnswer = conclusion;
        result.TotalBacktracks = backtrackCount;
        result.Success = backtrackCount < _maxBacktracks;

        return result;
    }

    private async Task<string> AnalyzeAsync(string problem, CancellationToken cancellationToken)
    {
        var function = _kernel.CreateFunctionFromPrompt(
            """
            Analyze this problem:
            {{$problem}}

            Identify:
            - Key information
            - What is being asked
            - Relevant approach

            Analysis:
            """);

        var result = await _kernel.InvokeAsync(function, 
            new KernelArguments { ["problem"] = problem }, 
            cancellationToken);
        
        return result.GetValue<string>() ?? "";
    }

    private async Task<string> ReasonStepAsync(
        string problem, 
        List<ReasoningStep> previousSteps, 
        int stepNumber,
        CancellationToken cancellationToken)
    {
        var previousContext = string.Join("\n", 
            previousSteps.Select(s => $"Step {s.StepNumber}: {s.Content}"));

        var function = _kernel.CreateFunctionFromPrompt(
            $"""
            Problem: {{{{$problem}}}}

            Previous reasoning:
            {previousContext}

            Continue with Step {stepNumber}. Build on previous steps logically.

            Step {stepNumber}:
            """);

        var result = await _kernel.InvokeAsync(function,
            new KernelArguments { ["problem"] = problem },
            cancellationToken);

        return result.GetValue<string>() ?? "";
    }

    private async Task<double> ValidateStepAsync(string step, CancellationToken cancellationToken)
    {
        var function = _kernel.CreateFunctionFromPrompt(
            """
            Rate the quality of this reasoning step from 0 to 100:
            {{$step}}

            Consider: logical flow, evidence, clarity.
            
            Return only a number:
            """);

        var result = await _kernel.InvokeAsync(function,
            new KernelArguments { ["step"] = step },
            cancellationToken);

        var scoreText = result.GetValue<string>() ?? "50";
        return double.TryParse(scoreText.Trim(), out var score) ? score / 100 : 0.5;
    }

    private async Task<string> RetryWithAlternativeAsync(
        string problem,
        List<ReasoningStep> previousSteps,
        int stepNumber,
        CancellationToken cancellationToken)
    {
        var function = _kernel.CreateFunctionFromPrompt(
            """
            The previous reasoning approach wasn't strong enough.
            Try a different approach for this step.

            Problem: {{$problem}}
            
            Use a different method or perspective.

            Alternative Step:
            """);

        var result = await _kernel.InvokeAsync(function,
            new KernelArguments { ["problem"] = problem },
            cancellationToken);

        return result.GetValue<string>() ?? "";
    }

    private async Task<string> ConcludeAsync(
        string problem,
        List<ReasoningStep> steps,
        CancellationToken cancellationToken)
    {
        var allSteps = string.Join("\n", steps.Select(s => $"Step {s.StepNumber}: {s.Content}"));

        var function = _kernel.CreateFunctionFromPrompt(
            """
            Based on all reasoning, provide the final answer.

            Problem: {{$problem}}

            Reasoning:
            {{$steps}}

            Final Answer:
            """);

        var result = await _kernel.InvokeAsync(function,
            new KernelArguments { ["problem"] = problem, ["steps"] = allSteps },
            cancellationToken);

        return result.GetValue<string>() ?? "";
    }
}

public class CoTResult
{
    public string Problem { get; set; } = string.Empty;
    public List<ReasoningStep> Steps { get; set; } = new();
    public string FinalAnswer { get; set; } = string.Empty;
    public int TotalBacktracks { get; set; }
    public List<string> BacktrackEvents { get; set; } = new();
    public bool Success { get; set; }
}

public class ReasoningStep
{
    public int StepNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string Type { get; set; } = string.Empty;
}
```

---

## Web API Integration

```csharp
[ApiController]
[Route("api/[controller]")]
public class ReasoningController : ControllerBase
{
    private readonly Kernel _kernel;

    public ReasoningController(Kernel kernel)
    {
        _kernel = kernel;
    }

    [HttpPost("solve")]
    public async Task<IActionResult> Solve(
        [FromBody] ReasoningRequest request,
        CancellationToken cancellationToken)
    {
        var cot = new BacktrackingCoT(_kernel);
        var result = await cot.SolveAsync(request.Problem, cancellationToken);

        return Ok(new
        {
            result.Problem,
            result.FinalAnswer,
            Steps = result.Steps.Select(s => new
            {
                s.StepNumber,
                s.Content,
                s.Confidence,
                s.Type
            }),
            result.TotalBacktracks,
            result.Success
        });
    }

    [HttpPost("solve/{domain}")]
    public async Task<IActionResult> SolveDomain(
        [FromRoute] string domain,
        [FromBody] ReasoningRequest request,
        CancellationToken cancellationToken)
    {
        var reasoningDomain = Enum.Parse<DomainSpecificCoT.ReasoningDomain>(domain, ignoreCase: true);
        var graph = DomainSpecificCoT.CreateDomainCoT(_kernel, reasoningDomain);

        var args = new KernelArguments { ["problem"] = request.Problem };
        await graph.ExecuteAsync(_kernel, args, cancellationToken);

        var answer = args.GetOrCreateGraphState().GetValue<string>("final_answer");
        return Ok(new { answer });
    }
}

public record ReasoningRequest(string Problem);
```

---

## Testing

```csharp
using Xunit;

public class ChainOfThoughtTests
{
    [Fact]
    public async Task MathematicalCoT_SolvesEquation()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var graph = DomainSpecificCoT.CreateDomainCoT(
            kernel, 
            DomainSpecificCoT.ReasoningDomain.Mathematical);
        
        var args = new KernelArguments 
        { 
            ["problem"] = "If 2x + 5 = 15, what is x?" 
        };

        // Act
        await graph.ExecuteAsync(kernel, args);
        var answer = args.GetOrCreateGraphState().GetValue<string>("final_answer");

        // Assert
        Assert.Contains("5", answer);
    }

    [Fact]
    public async Task BacktrackingCoT_HandlesLowConfidence()
    {
        // Arrange
        var kernel = CreateTestKernel();
        var cot = new BacktrackingCoT(kernel, maxBacktracks: 2, minConfidence: 0.7);

        // Act
        var result = await cot.SolveAsync("What is 2 + 2?");

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.FinalAnswer);
    }

    private Kernel CreateTestKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.AddGraphSupport();
        builder.AddOpenAIChatCompletion("gpt-4o", "test-key");
        return builder.Build();
    }
}
```

---

## Best Practices

### Reasoning Quality

1. **Explicit Steps**: Each step should be clearly defined
2. **Build on Previous**: Each step should reference and build on earlier steps
3. **Self-Validation**: Include confidence scoring
4. **Backtracking**: Allow retry with alternative approaches

### Performance

1. **Limit Steps**: Set maximum reasoning steps
2. **Early Exit**: Stop if confident answer is reached
3. **Caching**: Cache intermediate results when appropriate

### Debugging

```csharp
// Log each reasoning step
var graph = ChainOfThoughtBuilder.CreateChainOfThought(kernel, maxSteps: 4);
graph.OnNodeExecuted += (sender, e) =>
{
    Console.WriteLine($"Step {e.NodeId}: {e.Duration.TotalMilliseconds}ms");
    Console.WriteLine($"Output: {e.Result?.ToString()?.Substring(0, 100)}...");
};
```

---

## Related Templates

- [Graph Executor](template-skg-graph-executor.md) - Basic graph execution
- [ReAct Agent](template-skg-react-agent.md) - Reasoning with tools
- [Planners](template-sk-planners.md) - Task decomposition

---

## External References

- [Chain-of-Thought Prompting](https://arxiv.org/abs/2201.11903)
- [Self-Consistency Improves CoT](https://arxiv.org/abs/2203.11171)
- [Semantic Kernel Graph](https://github.com/kallebelins/semantic-kernel-graph)

