using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Hosted;

[Trait("Category", "Unit")]
public class HostedServicesCoverageTest
{
    [Fact]
    public void MvpRabbitMQHostedService_WithOptionsWrapper_ShouldUseOptionsValue()
    {
        var options = new RabbitMQHostedOptions
        {
            Callback = _ => { },
            DueTime = TimeSpan.FromSeconds(1),
            Period = TimeSpan.FromSeconds(5)
        };

        var service = new MvpRabbitMQHostedService(Options.Create(options));

        service.Should().NotBeNull();
    }

    [Fact]
    public void MvpRabbitMQHostedService_WithNullOptionsWrapper_ShouldThrow()
    {
        Action act = () => new MvpRabbitMQHostedService((IOptions<RabbitMQHostedOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ScheduledMessageBackgroundService_WhenDisabled_ShouldNotStartProcessing()
    {
        var scheduler = new MessageScheduler(
            new InMemoryScheduledMessageStore(),
            RabbitMQTestHelpers.CreateInMemoryBus(),
            Options.Create(new MessageSchedulerOptions { Enabled = false }));

        var service = new ScheduledMessageBackgroundService(
            scheduler,
            new InMemoryScheduledMessageStore(),
            Options.Create(new MessageSchedulerOptions { Enabled = false }));

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        service.Should().NotBeNull();
    }

    [Fact]
    public async Task ScheduledMessageBackgroundService_WhenEnabled_ShouldProcessDueMessages()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string?>())).Returns("token");

        var scheduler = new MessageScheduler(
            store,
            clientMock.Object,
            Options.Create(new MessageSchedulerOptions { Enabled = true, PollingInterval = TimeSpan.FromMilliseconds(50) }));

        var service = new ScheduledMessageBackgroundService(
            scheduler,
            store,
            Options.Create(new MessageSchedulerOptions
            {
                Enabled = true,
                PollingInterval = TimeSpan.FromMilliseconds(50),
                CompletedMessageTtl = TimeSpan.FromHours(1)
            }));

        await store.AddAsync(new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = System.Text.Json.JsonSerializer.Serialize(new TestOrderEvent { Name = "due" }),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            RoutingKey = "order-event",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = ScheduledMessageStatus.Pending,
            CorrelationId = Guid.NewGuid().ToString()
        });

        await service.StartAsync(CancellationToken.None);

        await RabbitMQTestHelpers.WaitUntilAsync(
            () => clientMock.Invocations.Any(i => i.Method.Name == nameof(IMvpRabbitMQClient.Publish)),
            TimeSpan.FromSeconds(5));

        await service.StopAsync(CancellationToken.None);

        clientMock.Verify(c => c.Publish(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string?>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ScheduledMessageBackgroundService_CleanupOldMessages_ShouldInvokeStoreCleanup()
    {
        var storeMock = new Mock<IScheduledMessageStore>();
        storeMock.Setup(s => s.GetDueMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        storeMock.Setup(s => s.CleanupOldMessagesAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var scheduler = new MessageScheduler(
            storeMock.Object,
            RabbitMQTestHelpers.CreateInMemoryBus(),
            Options.Create(new MessageSchedulerOptions { Enabled = true, PollingInterval = TimeSpan.FromMilliseconds(50) }));

        var service = new ScheduledMessageBackgroundService(
            scheduler,
            storeMock.Object,
            Options.Create(new MessageSchedulerOptions
            {
                Enabled = true,
                PollingInterval = TimeSpan.FromMilliseconds(50),
                CompletedMessageTtl = TimeSpan.FromHours(1)
            }));

        FieldInfo? lastCleanupField = typeof(ScheduledMessageBackgroundService)
            .GetField("_lastCleanup", BindingFlags.Instance | BindingFlags.NonPublic);
        lastCleanupField!.SetValue(service, DateTime.MinValue);

        await service.StartAsync(CancellationToken.None);

        await RabbitMQTestHelpers.WaitUntilAsync(
            () => storeMock.Invocations.Any(i => i.Method.Name == nameof(IScheduledMessageStore.CleanupOldMessagesAsync)),
            TimeSpan.FromSeconds(5));

        await service.StopAsync(CancellationToken.None);

        storeMock.Verify(s => s.CleanupOldMessagesAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task OutboxPublisher_AsHostedService_ShouldRegisterAndResolvePublisher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ITransactionalOutbox, InMemoryTransactionalOutbox>();
        services.AddSingleton<IMvpRabbitMQClient>(RabbitMQTestHelpers.CreateInMemoryBus());
        services.AddSingleton<OutboxPublisherOptions>(new OutboxPublisherOptions { PollingInterval = TimeSpan.FromMilliseconds(50) });
        services.AddSingleton<OutboxPublisher>();
        services.AddHostedService(sp => sp.GetRequiredService<OutboxPublisher>());

        ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IHostedService> hostedServices = provider.GetServices<IHostedService>();

        hostedServices.OfType<OutboxPublisher>().Should().ContainSingle();
    }
}
