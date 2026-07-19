using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;

namespace Mvp24Hours.Application.RabbitMQ.Test.Observability;

public class ObservabilityTest
{
    [Fact]
    public async Task RabbitMQDiagnostics_WithConnectedStatus_ShouldBeHealthy()
    {
        var metrics = new RabbitMQMetrics();
        var diagnostics = new RabbitMQDiagnostics(metrics);
        diagnostics.UpdateConnectionInfo(new ConnectionInfo
        {
            Status = ConnectionStatus.Connected,
            HostName = "localhost"
        });

        RabbitMQHealthStatus status = await diagnostics.GetStatusAsync();

        status.IsHealthy.Should().BeTrue();
        status.HealthStatus.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
        status.ConnectionStatus.Should().Be(ConnectionStatus.Connected);
    }

    [Fact]
    public void RabbitMQMetrics_IncrementCounters_ShouldUpdateSnapshot()
    {
        var metrics = new RabbitMQMetrics();

        metrics.IncrementMessagesSent("orders");
        metrics.IncrementMessagesReceived("orders-queue");
        metrics.IncrementPublisherConfirms();

        RabbitMQMetricsSnapshot snapshot = metrics.GetSnapshot();

        snapshot.MessagesSent.Should().Be(1);
        snapshot.MessagesReceived.Should().Be(1);
        snapshot.PublisherConfirms.Should().Be(1);
    }

    [Fact]
    public void RabbitMQDiagnostics_RecordError_ShouldKeepHistory()
    {
        var diagnostics = new RabbitMQDiagnostics(maxErrorHistory: 5);
        diagnostics.RecordError(new InvalidOperationException("first"), "consume");
        diagnostics.RecordError(new InvalidOperationException("second"), "publish");

        IReadOnlyList<ErrorInfo> history = diagnostics.GetErrorHistory(maxCount: 2);

        history.Should().HaveCount(2);
        history[^1].Message.Should().Be("second");
    }

    [Fact]
    public void RabbitMQStructuredLogger_LogMessagePublished_ShouldNotThrow()
    {
        var logger = new RabbitMQStructuredLogger(RabbitMQTestHelpers.CreateNullLogger<RabbitMQStructuredLogger>());

        Action act = () => logger.LogMessagePublished("msg-1", "exchange", "route", 128);

        act.Should().NotThrow();
    }

    [Fact]
    public void BaggagePropagation_InjectAndExtract_ShouldRoundTrip()
    {
        var headers = new Dictionary<string, object>();
        BaggagePropagation.InjectBaggage(headers, new BaggageContext
        {
            CorrelationId = "corr-1",
            TenantId = "tenant-a"
        });

        BaggageContext extracted = BaggagePropagation.ExtractBaggage(headers);

        extracted.CorrelationId.Should().Be("corr-1");
        extracted.TenantId.Should().Be("tenant-a");
    }
}
