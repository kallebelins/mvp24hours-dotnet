using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Observability;

[Trait("Category", "Unit")]
public class ObserverManagerExtendedCoverageTest
{
    [Fact]
    public async Task NotifyPostConsumeAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<IConsumeObserver>();
        var manager = new ObserverManager([observer.Object]);
        var context = new ConsumeObserverContext { MessageType = "OrderCreated" };

        await manager.NotifyPostConsumeAsync(context);

        observer.Verify(o => o.PostConsumeAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyConsumeFaultAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<IConsumeObserver>();
        var manager = new ObserverManager([observer.Object]);
        var context = new ConsumeObserverContext();
        var exception = new InvalidOperationException("consume fault");

        await manager.NotifyConsumeFaultAsync(context, exception);

        observer.Verify(
            o => o.ConsumeFaultAsync(context, exception, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyPrePublishAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<IPublishObserver>();
        var manager = new ObserverManager(publishObservers: [observer.Object]);
        var context = new PublishObserverContext { Exchange = "orders" };

        await manager.NotifyPrePublishAsync(context);

        observer.Verify(o => o.PrePublishAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyPostPublishAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<IPublishObserver>();
        var manager = new ObserverManager(publishObservers: [observer.Object]);
        var context = new PublishObserverContext { RoutingKey = "created" };

        await manager.NotifyPostPublishAsync(context);

        observer.Verify(o => o.PostPublishAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyPreSendAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<ISendObserver>();
        var manager = new ObserverManager(sendObservers: [observer.Object]);
        var context = new SendObserverContext { DestinationQueue = "orders-queue" };

        await manager.NotifyPreSendAsync(context);

        observer.Verify(o => o.PreSendAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyPostSendAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<ISendObserver>();
        var manager = new ObserverManager(sendObservers: [observer.Object]);
        var context = new SendObserverContext();

        await manager.NotifyPostSendAsync(context);

        observer.Verify(o => o.PostSendAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifySendFaultAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<ISendObserver>();
        var manager = new ObserverManager(sendObservers: [observer.Object]);
        var context = new SendObserverContext();
        var exception = new InvalidOperationException("send fault");

        await manager.NotifySendFaultAsync(context, exception);

        observer.Verify(
            o => o.SendFaultAsync(context, exception, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyConnectedAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<IConnectionObserver>();
        var manager = new ObserverManager(connectionObservers: [observer.Object]);
        var context = new ConnectionObserverContext { HostName = "localhost" };

        await manager.NotifyConnectedAsync(context);

        observer.Verify(o => o.OnConnectedAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyDisconnectedAsync_ShouldInvokeAllObservers()
    {
        var observer = new Mock<IConnectionObserver>();
        var manager = new ObserverManager(connectionObservers: [observer.Object]);
        var context = new ConnectionObserverContext();

        await manager.NotifyDisconnectedAsync(context, "connection lost");

        observer.Verify(
            o => o.OnDisconnectedAsync(context, "connection lost", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyPostSendAsync_WhenObserverThrowsWithLogger_ShouldNotPropagate()
    {
        var failingObserver = new Mock<ISendObserver>();
        failingObserver
            .Setup(o => o.PostSendAsync(It.IsAny<SendObserverContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("observer failed"));

        var logger = new Mock<ILogger<ObserverManager>>();
        var manager = new ObserverManager(sendObservers: [failingObserver.Object], logger: logger.Object);

        Func<Task> act = () => manager.NotifyPostSendAsync(new SendObserverContext());

        await act.Should().NotThrowAsync();
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Observer error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NullObserverManager_AllNotifyMethods_ShouldComplete()
    {
        IObserverManager manager = NullObserverManager.Instance;
        var consumeContext = new ConsumeObserverContext();
        var publishContext = new PublishObserverContext();
        var sendContext = new SendObserverContext();
        var connectionContext = new ConnectionObserverContext();
        var exception = new Exception("fault");

        await manager.NotifyPreConsumeAsync(consumeContext);
        await manager.NotifyPostConsumeAsync(consumeContext);
        await manager.NotifyConsumeFaultAsync(consumeContext, exception);
        await manager.NotifyPrePublishAsync(publishContext);
        await manager.NotifyPostPublishAsync(publishContext);
        await manager.NotifyPublishFaultAsync(publishContext, exception);
        await manager.NotifyPreSendAsync(sendContext);
        await manager.NotifyPostSendAsync(sendContext);
        await manager.NotifySendFaultAsync(sendContext, exception);
        await manager.NotifyConnectedAsync(connectionContext);
        await manager.NotifyDisconnectedAsync(connectionContext, "shutdown");

        Assert.True(true);
    }
}
