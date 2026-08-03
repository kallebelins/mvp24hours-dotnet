using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Logic.Events;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Events;

[Trait("Category", "Unit")]
public class ApplicationEventOutboxProcessorTest
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        Action act = () => _ = new ApplicationEventOutboxProcessor(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void ApplicationEventOutboxProcessorOptions_ShouldUseExpectedDefaults()
    {
        var options = new ApplicationEventOutboxProcessorOptions();

        options.PollingIntervalMs.Should().Be(5000);
        options.BatchSize.Should().Be(100);
        options.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOutboxNotRegistered_ShouldCompleteWithoutError()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();

        await RunProcessorAsync(provider, pollingIntervalMs: 50, runDurationMs: 150);

        // No exception means the processor skipped gracefully.
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingEvent_ShouldDispatchAndMarkAsDispatched()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        var handler = new CapturingEventHandler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IApplicationEventOutbox>(outbox);
        services.AddSingleton<IApplicationEventHandler<TestApplicationEvent>>(handler);
        ServiceProvider provider = services.BuildServiceProvider();

        await outbox.AddAsync(new TestApplicationEvent { Payload = "processor-dispatch" });

        await RunProcessorAsync(provider, pollingIntervalMs: 50, runDurationMs: 300);

        handler.Handled.Should().ContainSingle(e => e.Payload == "processor-dispatch");
        outbox.GetByStatus(ApplicationEventOutboxStatus.Dispatched).Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownEventType_ShouldMarkAsFailed()
    {
        Guid entryId = Guid.NewGuid();
        var mockOutbox = new Mock<IApplicationEventOutbox>();
        mockOutbox.Setup(o => o.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ApplicationEventOutboxEntry
                {
                    Id = entryId,
                    EventType = "Unknown.EventType, UnknownAssembly",
                    Payload = "{}",
                    Status = ApplicationEventOutboxStatus.Pending
                }
            ]);
        mockOutbox.Setup(o => o.MarkAsFailedAsync(entryId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockOutbox.Object);
        ServiceProvider provider = services.BuildServiceProvider();

        await RunProcessorAsync(provider, pollingIntervalMs: 50, runDurationMs: 200);

        mockOutbox.Verify(
            o => o.MarkAsFailedAsync(entryId, It.Is<string>(m => m.Contains("Unknown event type")), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPayload_ShouldMarkAsFailed()
    {
        Guid entryId = Guid.NewGuid();
        string eventType = typeof(TestApplicationEvent).AssemblyQualifiedName!;
        var mockOutbox = new Mock<IApplicationEventOutbox>();
        mockOutbox.Setup(o => o.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ApplicationEventOutboxEntry
                {
                    Id = entryId,
                    EventType = eventType,
                    Payload = "{ invalid json",
                    Status = ApplicationEventOutboxStatus.Pending
                }
            ]);
        mockOutbox.Setup(o => o.MarkAsFailedAsync(entryId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockOutbox.Object);
        ServiceProvider provider = services.BuildServiceProvider();

        await RunProcessorAsync(provider, pollingIntervalMs: 50, runDurationMs: 200);

        mockOutbox.Verify(
            o => o.MarkAsFailedAsync(entryId, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerFails_ShouldStillMarkAsDispatched()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IApplicationEventOutbox>(outbox);
        services.AddSingleton<IApplicationEventHandler<TestApplicationEvent>>(new FailingEventHandler());
        ServiceProvider provider = services.BuildServiceProvider();

        await outbox.AddAsync(new TestApplicationEvent { Payload = "handler-fail" });

        await RunProcessorAsync(provider, pollingIntervalMs: 50, runDurationMs: 300);

        outbox.GetByStatus(ApplicationEventOutboxStatus.Dispatched).Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectBatchSize()
    {
        var mockOutbox = new Mock<IApplicationEventOutbox>();
        mockOutbox.Setup(o => o.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        mockOutbox.Setup(o => o.GetPendingAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(mockOutbox.Object);
        ServiceProvider provider = services.BuildServiceProvider();

        await RunProcessorAsync(
            provider,
            options: new ApplicationEventOutboxProcessorOptions { PollingIntervalMs = 50, BatchSize = 25 },
            runDurationMs: 200);

        mockOutbox.Verify(o => o.GetPendingAsync(25, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    private static async Task RunProcessorAsync(
        IServiceProvider serviceProvider,
        int pollingIntervalMs = 50,
        int runDurationMs = 200,
        ApplicationEventOutboxProcessorOptions? options = null)
    {
        options ??= new ApplicationEventOutboxProcessorOptions { PollingIntervalMs = pollingIntervalMs };
        options.PollingIntervalMs = pollingIntervalMs;

        var processor = new ApplicationEventOutboxProcessor(
            serviceProvider,
            Options.Create(options),
            NullLogger<ApplicationEventOutboxProcessor>.Instance);

        using var cts = new CancellationTokenSource();
        await processor.StartAsync(cts.Token);
        await Task.Delay(runDurationMs);
        cts.Cancel();
        await processor.StopAsync(CancellationToken.None);
    }
}
