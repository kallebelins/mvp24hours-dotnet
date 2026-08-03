using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Application.Pipe.Test.Operations;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Observability;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Observability;

[Trait("Category", "Unit")]
public class ObservabilityTest
{
    [Fact]
    public void PipelineMetrics_Should_TrackPipelineAndOperationExecutions()
    {
        var metrics = new PipelineMetrics();

        metrics.RecordPipelineStart("p1", "OrdersPipeline");
        metrics.RecordOperationStart("p1", "Validate", 0);
        metrics.RecordOperationEnd("p1", "Validate", TimeSpan.FromMilliseconds(12), success: true);
        metrics.RecordPipelineEnd("p1", success: true);

        PipelineMetricsSnapshot snapshot = metrics.GetSnapshot();

        snapshot.TotalPipelineExecutions.Should().Be(1);
        snapshot.SuccessfulPipelineExecutions.Should().Be(1);
        snapshot.TotalOperationsExecuted.Should().Be(1);
        snapshot.PipelineSuccessRate.Should().Be(1);
        metrics.GetPipelineMetrics("OrdersPipeline")!.TotalExecutions.Should().Be(1);
    }

    [Fact]
    public void PipelineMetrics_Should_RecordFailureAndException()
    {
        var metrics = new PipelineMetrics();
        metrics.RecordPipelineStart("p2", "BillingPipeline");
        metrics.RecordOperationFailure("p2", "Charge", new InvalidOperationException("declined"));
        metrics.RecordOperationEnd("p2", "Charge", TimeSpan.FromMilliseconds(5), success: false);
        metrics.RecordPipelineEnd("p2", success: false);

        OperationMetrics? opMetrics = metrics.GetOperationMetrics("Charge");

        opMetrics!.FailedExecutions.Should().Be(1);
        opMetrics.LastExceptionType.Should().Contain(nameof(InvalidOperationException));
    }

    [Fact]
    public void PipelineMetrics_Reset_Should_ClearAllData()
    {
        var metrics = new PipelineMetrics();
        metrics.RecordPipelineStart("p1", "Test");
        metrics.RecordPipelineEnd("p1", success: true);

        metrics.Reset();

        metrics.GetSnapshot().TotalPipelineExecutions.Should().Be(0);
    }

    [Fact]
    public async Task PipelineHealthCheck_Should_ReturnHealthyWhenInsufficientData()
    {
        var metrics = new PipelineMetrics();
        var healthCheck = new PipelineHealthCheck(metrics, new PipelineHealthCheckOptions { MinimumExecutionsForEvaluation = 10 });

        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult result =
            await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        result.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
    }

    [Fact]
    public async Task PipelineHealthCheck_Should_ReturnUnhealthyWhenBelowCriticalThreshold()
    {
        var metrics = new PipelineMetrics();
        for (int i = 0; i < 10; i++)
        {
            metrics.RecordPipelineStart($"p{i}", "CriticalPipeline");
            metrics.RecordPipelineEnd($"p{i}", success: i < 2);
        }

        var healthCheck = new PipelineHealthCheck(metrics, new PipelineHealthCheckOptions
        {
            MinimumExecutionsForEvaluation = 5,
            MinimumSuccessRate = 0.95,
            CriticalSuccessRate = 0.80
        });

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

        result.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
    }

    [Fact]
    public void PipelineVisualizer_Should_GenerateMermaidAndAscii()
    {
        var pipeline = new Pipeline();
        pipeline.Add<OperationTest>();
        pipeline.AddInterceptors(_ => { }, Core.Enums.Infrastructure.PipelineInterceptorType.PreOperation);

        var visualizer = new PipelineVisualizer();
        string mermaid = visualizer.ToMermaid(pipeline, new PipelineVisualizationOptions { Title = "Test Pipeline", IncludeInterceptors = true });
        string ascii = visualizer.ToAscii(pipeline);
        PipelineStructure structure = visualizer.GetStructure(pipeline);

        mermaid.Should().Contain("flowchart");
        mermaid.Should().Contain("OperationTest");
        ascii.Should().Contain("START");
        structure.Operations.Should().NotBeEmpty();
    }

    [Fact]
    public void PipelineVisualizer_Should_GenerateDotJsonAndAsyncPipelineFormats()
    {
        var syncPipeline = new Pipeline();
        syncPipeline.Add<RequiredValidationOperation>();
        syncPipeline.Add<ConditionalBranchOperation>();
        syncPipeline.Add<ParallelGroupOperation>();

        var asyncPipeline = new PipelineAsync();
        asyncPipeline.Add<SubPipelineTestOperationAsync>();

        var metrics = new PipelineMetrics();
        metrics.RecordOperationStart("p1", "RequiredValidationOperation", 0);
        metrics.RecordOperationEnd("p1", "RequiredValidationOperation", TimeSpan.FromMilliseconds(25), success: true);

        var visualizer = new PipelineVisualizer();
        var options = new PipelineVisualizationOptions
        {
            Title = "Coverage Pipeline",
            Direction = DiagramDirection.LeftToRight,
            IncludeInterceptors = true,
            IncludeMetrics = true,
            MetricsProvider = metrics,
            HighlightRequiredOperations = true,
            ShowOperationTypes = true,
            UseShortNames = false
        };

        string dot = visualizer.ToDot(syncPipeline, options);
        var bottomUpOptions = new PipelineVisualizationOptions
        {
            Title = options.Title,
            Direction = DiagramDirection.BottomToTop,
            IncludeInterceptors = options.IncludeInterceptors,
            IncludeMetrics = options.IncludeMetrics,
            MetricsProvider = options.MetricsProvider,
            HighlightRequiredOperations = options.HighlightRequiredOperations,
            ShowOperationTypes = options.ShowOperationTypes,
            UseShortNames = options.UseShortNames
        };
        string ascii = visualizer.ToAscii(syncPipeline, bottomUpOptions);
        string json = visualizer.ToJson(syncPipeline, options);
        string asyncMermaid = visualizer.ToMermaid(asyncPipeline, options);
        PipelineStructure asyncStructure = visualizer.GetStructure(asyncPipeline);

        dot.Should().Contain("digraph");
        dot.Should().Contain("hexagon");
        ascii.Should().Contain("END");
        json.Should().Contain("operations");
        asyncMermaid.Should().Contain("flowchart LR");
        asyncStructure.Operations.Should().NotBeEmpty();
    }

    [Fact]
    public void PipelineVisualizer_EmptyPipeline_ShouldRenderStartToEnd()
    {
        var visualizer = new PipelineVisualizer();
        string mermaid = visualizer.ToMermaid(new Pipeline());

        mermaid.Should().Contain("Start --> End");
    }

    [Fact]
    public void PipelineVisualizer_AsyncPipeline_Should_GenerateDotAsciiAndJson()
    {
        var asyncPipeline = new PipelineAsync();
        asyncPipeline.Add<RequiredValidationOperationAsync>();
        asyncPipeline.Add<ParallelGroupOperationAsync>();
        asyncPipeline.AddInterceptors(_ => { }, PipelineInterceptorType.PreOperation);

        var metrics = new PipelineMetrics();
        metrics.RecordOperationStart("p-async", "RequiredValidationOperationAsync", 0);
        metrics.RecordOperationEnd("p-async", "RequiredValidationOperationAsync", TimeSpan.FromMilliseconds(18), success: true);

        var visualizer = new PipelineVisualizer();
        var options = new PipelineVisualizationOptions
        {
            Title = "Async Coverage Pipeline",
            Direction = DiagramDirection.RightToLeft,
            IncludeInterceptors = true,
            IncludeMetrics = true,
            MetricsProvider = metrics,
            HighlightRequiredOperations = true,
            ShowOperationTypes = true
        };

        string dot = visualizer.ToDot(asyncPipeline, options);
        string ascii = visualizer.ToAscii(asyncPipeline, options);
        string json = visualizer.ToJson(asyncPipeline, options);
        string mermaid = visualizer.ToMermaid(asyncPipeline, options);

        dot.Should().Contain("rankdir=RL");
        dot.Should().Contain("hexagon");
        dot.Should().Contain("parallelogram");
        dot.Should().Contain("PreOperation Interceptors");
        ascii.Should().Contain("(Validation)");
        ascii.Should().Contain("(Parallel)");
        ascii.Should().Contain("Interceptors:");
        json.Should().Contain("\"category\": \"Validation\"");
        json.Should().Contain("\"category\": \"Parallel\"");
        mermaid.Should().Contain("flowchart RL");
        mermaid.Should().Contain("Validation");
        mermaid.Should().Contain("Parallel");
    }

    [Fact]
    public void PipelineVisualizer_SyncPipeline_Should_RenderValidationAndParallelCategoriesWithInterceptors()
    {
        var pipeline = new Pipeline();
        pipeline.Add<RequiredValidationOperation>();
        pipeline.Add<ParallelGroupOperation>();
        pipeline.AddInterceptors(_ => { }, PipelineInterceptorType.PostOperation);

        var visualizer = new PipelineVisualizer();
        var options = new PipelineVisualizationOptions
        {
            Title = "Category Pipeline",
            Direction = DiagramDirection.RightToLeft,
            IncludeInterceptors = true,
            ShowOperationTypes = true
        };

        string dot = visualizer.ToDot(pipeline, options);
        string ascii = visualizer.ToAscii(pipeline, options);
        string mermaid = visualizer.ToMermaid(pipeline, options);

        dot.Should().Contain("rankdir=RL");
        dot.Should().Contain("hexagon");
        dot.Should().Contain("parallelogram");
        dot.Should().Contain("PostOperation Interceptors");
        ascii.Should().Contain("(Validation)");
        ascii.Should().Contain("(Parallel)");
        mermaid.Should().Contain("flowchart RL");
        mermaid.Should().Contain("Validation");
        mermaid.Should().Contain("Parallel");
        mermaid.Should().Contain("PostOperation Interceptors");
    }

    private sealed class RequiredValidationOperation : OperationBase
    {
        public override bool IsRequired => true;

        public override void Execute(IPipelineMessage input)
        {
        }
    }

    private sealed class ConditionalBranchOperation : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
        }
    }

    private sealed class ParallelGroupOperation : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
        }
    }

    private sealed class SubPipelineTestOperationAsync : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RequiredValidationOperationAsync : OperationBaseAsync
    {
        public override bool IsRequired => true;

        public override Task ExecuteAsync(IPipelineMessage input)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ParallelGroupOperationAsync : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void PipelineHealthMonitor_Should_ReportOverallHealth()
    {
        var metrics = new PipelineMetrics();
        metrics.RecordPipelineStart("p1", "MonitorPipeline");
        metrics.RecordPipelineEnd("p1", success: true);

        var monitor = new PipelineHealthMonitor(metrics, new PipelineHealthCheckOptions { MinimumExecutionsForEvaluation = 1 });

        monitor.GetPipelineHealth("MonitorPipeline").Should().NotBeNull();
        monitor.GetOverallHealth().Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
    }
}
