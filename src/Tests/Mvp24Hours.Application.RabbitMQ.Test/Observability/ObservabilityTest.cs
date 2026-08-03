using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability.Extensions;

namespace Mvp24Hours.Application.RabbitMQ.Test.Observability;

[Trait("Category", "Unit")]
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

    [Fact]
    public async Task RabbitMQDiagnostics_WithDisconnectedStatus_ShouldBeUnhealthy()
    {
        var metrics = new RabbitMQMetrics();
        var diagnostics = new RabbitMQDiagnostics(metrics);
        diagnostics.UpdateConnectionInfo(new ConnectionInfo
        {
            Status = ConnectionStatus.Disconnected,
            HostName = "localhost"
        });

        RabbitMQHealthStatus status = await diagnostics.GetStatusAsync();

        status.IsHealthy.Should().BeFalse();
        status.HealthStatus.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task RabbitMQDiagnostics_Default_ShouldBeUnhealthy()
    {
        var diagnostics = new RabbitMQDiagnostics();

        RabbitMQHealthStatus status = await diagnostics.GetStatusAsync();

        status.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public void RabbitMQMetrics_IncrementError_ShouldTrackCount()
    {
        var metrics = new RabbitMQMetrics();

        metrics.IncrementError("validation");
        metrics.IncrementError("timeout");

        RabbitMQMetricsSnapshot snapshot = metrics.GetSnapshot();
        snapshot.ErrorsByType.Should().HaveCount(2);
    }

    [Fact]
    public void RabbitMQMetrics_IncrementNacks_ShouldTrackCount()
    {
        var metrics = new RabbitMQMetrics();

        metrics.IncrementPublisherNacks();
        metrics.IncrementPublisherNacks();

        RabbitMQMetricsSnapshot snapshot = metrics.GetSnapshot();
        snapshot.PublisherNacks.Should().Be(2);
    }

    [Fact]
    public void RabbitMQMetrics_Reset_ShouldClearCounters()
    {
        var metrics = new RabbitMQMetrics();
        metrics.IncrementMessagesSent("ex");
        metrics.IncrementMessagesReceived("q");

        metrics.Reset();

        RabbitMQMetricsSnapshot snapshot = metrics.GetSnapshot();
        snapshot.MessagesSent.Should().Be(0);
        snapshot.MessagesReceived.Should().Be(0);
    }

    [Fact]
    public void RabbitMQDiagnostics_RecordError_WithMaxHistory_ShouldEvict()
    {
        var diagnostics = new RabbitMQDiagnostics(maxErrorHistory: 3);

        for (int i = 0; i < 5; i++)
        {
            diagnostics.RecordError(new Exception($"error {i}"), "consume");
        }

        IReadOnlyList<ErrorInfo> history = diagnostics.GetErrorHistory(maxCount: 10);
        history.Count.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void RabbitMQStructuredLogger_LogMessageAcked_ShouldNotThrow()
    {
        var logger = new RabbitMQStructuredLogger(RabbitMQTestHelpers.CreateNullLogger<RabbitMQStructuredLogger>());

        Action act = () => logger.LogMessageAcked("msg-2", 1UL, TimeSpan.FromMilliseconds(50));

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQStructuredLogger_LogConnectionEvent_ShouldNotThrow()
    {
        var logger = new RabbitMQStructuredLogger(RabbitMQTestHelpers.CreateNullLogger<RabbitMQStructuredLogger>());

        Action act = () => logger.LogConnectionEvent("connected", "localhost", 5672);

        act.Should().NotThrow();
    }

    [Fact]
    public void RabbitMQStructuredLogger_LogError_ShouldNotThrow()
    {
        var logger = new RabbitMQStructuredLogger(RabbitMQTestHelpers.CreateNullLogger<RabbitMQStructuredLogger>());
        var ex = new Exception("test error");

        Action act = () => logger.LogError("operation", ex, "msg-ctx");

        act.Should().NotThrow();
    }

    [Fact]
    public void BaggagePropagation_EmptyHeaders_ShouldReturnEmptyContext()
    {
        var headers = new Dictionary<string, object>();

        BaggageContext ctx = BaggagePropagation.ExtractBaggage(headers);

        ctx.CorrelationId.Should().BeNullOrEmpty();
        ctx.TenantId.Should().BeNullOrEmpty();
    }

    [Fact]
    public void BaggagePropagation_InjectAndExtract_WithBothCorrelationAndTenant()
    {
        var headers = new Dictionary<string, object>();
        var baggage = new BaggageContext
        {
            CorrelationId = "corr-99",
            TenantId = "tenant-x"
        };
        BaggagePropagation.InjectBaggage(headers, baggage);

        BaggageContext extracted = BaggagePropagation.ExtractBaggage(headers);

        extracted.CorrelationId.Should().Be("corr-99");
        extracted.TenantId.Should().Be("tenant-x");
    }

    [Fact]
    public void ConnectionInfo_Status_ShouldHaveExpectedValues()
    {
        ConnectionStatus.Connected.Should().NotBe(ConnectionStatus.Disconnected);
        ConnectionStatus.Reconnecting.Should().NotBe(ConnectionStatus.Connected);
    }

    [Fact]
    public async Task ObserverManager_NotifyPreConsumeAsync_ShouldInvokeAllObservers()
    {
        var observer1 = new Mock<IConsumeObserver>();
        var observer2 = new Mock<IConsumeObserver>();
        var manager = new ObserverManager([observer1.Object, observer2.Object]);
        var context = new ConsumeObserverContext { MessageType = "TestOrderEvent", QueueName = "orders" };

        await manager.NotifyPreConsumeAsync(context);

        observer1.Verify(o => o.PreConsumeAsync(context, It.IsAny<CancellationToken>()), Times.Once);
        observer2.Verify(o => o.PreConsumeAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObserverManager_WhenObserverThrows_ShouldNotPropagate()
    {
        var failingObserver = new Mock<IConsumeObserver>();
        failingObserver
            .Setup(o => o.PreConsumeAsync(It.IsAny<ConsumeObserverContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("observer failed"));

        var manager = new ObserverManager([failingObserver.Object]);
        var context = new ConsumeObserverContext { MessageType = "TestOrderEvent" };

        Func<Task> act = () => manager.NotifyPreConsumeAsync(context);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ObserverManager_NotifyPublishFaultAsync_ShouldInvokeObservers()
    {
        var observer = new Mock<IPublishObserver>();
        var manager = new ObserverManager(publishObservers: [observer.Object]);
        var context = new PublishObserverContext { MessageType = "TestOrderEvent", Exchange = "orders" };
        var exception = new Exception("publish failed");

        await manager.NotifyPublishFaultAsync(context, exception);

        observer.Verify(
            o => o.PublishFaultAsync(context, exception, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NullObserverManager_ShouldCompleteWithoutSideEffects()
    {
        IObserverManager manager = NullObserverManager.Instance;
        var consumeContext = new ConsumeObserverContext();
        var publishContext = new PublishObserverContext();

        await manager.NotifyPreConsumeAsync(consumeContext);
        await manager.NotifyPostPublishAsync(publishContext);
        await manager.NotifyConnectedAsync(new ConnectionObserverContext());

        Assert.True(true);
    }

    [Fact]
    public void AddMvpRabbitMQObservability_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpRabbitMQObservability();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IObserverManager>().Should().NotBeNull();
        provider.GetRequiredService<IRabbitMQMetrics>().Should().NotBeNull();
        provider.GetRequiredService<IRabbitMQStructuredLogger>().Should().NotBeNull();
        provider.GetRequiredService<IRabbitMQDiagnostics>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvpRabbitMQObservers_ShouldRegisterObserverManagerSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpRabbitMQObservers();

        ServiceProvider provider = services.BuildServiceProvider();
        IObserverManager first = provider.GetRequiredService<IObserverManager>();
        IObserverManager second = provider.GetRequiredService<IObserverManager>();

        first.Should().BeSameAs(second);
        first.Should().BeOfType<ObserverManager>();
    }

    [Fact]
    public void AddMvpRabbitMQPrometheusMetrics_ShouldRegisterMetricsAndObservers()
    {
        var services = new ServiceCollection();
        services.AddMvpRabbitMQPrometheusMetrics();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRabbitMQMetrics>().Should().BeOfType<RabbitMQPrometheusMetrics>();
        provider.GetServices<IConsumeObserver>().Should().ContainSingle(o => o is RabbitMQPrometheusMetrics);
        provider.GetServices<IPublishObserver>().Should().ContainSingle(o => o is RabbitMQPrometheusMetrics);
    }

    [Fact]
    public void AddMvpRabbitMQObservabilityProduction_ShouldEnableProductionDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpRabbitMQObservabilityProduction();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IObserverManager>().Should().NotBeNull();
        provider.GetRequiredService<IRabbitMQMetrics>().Should().BeOfType<RabbitMQPrometheusMetrics>();
    }
}
