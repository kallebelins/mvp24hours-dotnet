using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Application.Pipe.Test.Operations;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Observability;

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
