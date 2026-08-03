using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;

namespace Mvp24Hours.Application.RabbitMQ.Test.Scheduling;

[Trait("Category", "Unit")]
public class MessageSchedulerCoverageTest
{
    private static MessageScheduler CreateScheduler(
        IScheduledMessageStore? store = null,
        IMvpRabbitMQClient? client = null,
        MessageSchedulerOptions? options = null,
        ILogger<MessageScheduler>? logger = null)
    {
        return new MessageScheduler(
            store ?? new InMemoryScheduledMessageStore(),
            client ?? RabbitMQTestHelpers.CreateInMemoryBus(),
            Options.Create(options ?? new MessageSchedulerOptions()),
            logger);
    }

    [Fact]
    public void Constructor_WithNullStore_ShouldThrow()
    {
        Action act = () => new MessageScheduler(
            null!,
            Mock.Of<IMvpRabbitMQClient>(),
            Options.Create(new MessageSchedulerOptions()));

        act.Should().Throw<ArgumentNullException>().WithParameterName("store");
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrow()
    {
        Action act = () => new MessageScheduler(
            new InMemoryScheduledMessageStore(),
            null!,
            Options.Create(new MessageSchedulerOptions()));

        act.Should().Throw<ArgumentNullException>().WithParameterName("client");
    }

    [Fact]
    public async Task ScheduleMessageAsync_WithDateTimeOverload_ShouldPersistMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);

        Guid id = await scheduler.ScheduleMessageAsync(
            DateTime.UtcNow.AddMinutes(10),
            new TestOrderEvent { Name = "datetime" },
            routingKey: "order-event");

        (await store.GetByIdAsync(id)).Should().NotBeNull();
    }

    [Fact]
    public async Task ScheduleMessageAsync_WithNullMessage_ShouldThrow()
    {
        MessageScheduler scheduler = CreateScheduler();

        Func<Task> act = () => scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(5),
            (TestOrderEvent)null!,
            new ScheduleMessageOptions { RoutingKey = "order" });

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ScheduleMessageAsync_WithFullOptions_ShouldPersistAllFields()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);

        Guid id = await scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(5),
            new TestOrderEvent { Name = "full" },
            new ScheduleMessageOptions
            {
                RoutingKey = "order-event",
                Exchange = "orders",
                CorrelationId = "corr-42",
                Priority = 8,
                TtlMilliseconds = 30_000,
                Headers = new Dictionary<string, object> { ["x-region"] = "us" }
            });

        ScheduledMessage? stored = await store.GetByIdAsync(id);
        stored.Should().NotBeNull();
        stored!.Exchange.Should().Be("orders");
        stored.CorrelationId.Should().Be("corr-42");
        stored.Priority.Should().Be(8);
        stored.TtlMilliseconds.Should().Be(30_000);
        stored.Headers.Should().ContainKey("x-region");
    }

    [Fact]
    public async Task ScheduleRecurringCron_WithValidExpression_ShouldPersistActiveMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);

        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            "0 12 * * *",
            new TestOrderEvent { Name = "cron" },
            routingKey: "order-event",
            maxExecutions: 5);

        ScheduledMessage? stored = await store.GetByIdAsync(id);
        stored.Should().NotBeNull();
        stored!.IsRecurring.Should().BeTrue();
        stored.RecurringSchedule!.Type.Should().Be(RecurringScheduleType.Cron);
        stored.RecurringSchedule.CronExpression.Should().Be("0 12 * * *");
    }

    [Fact]
    public async Task CancelScheduledMessage_WhenNotFound_ShouldReturnFalse()
    {
        MessageScheduler scheduler = CreateScheduler();

        bool cancelled = await scheduler.CancelScheduledMessageAsync(Guid.NewGuid());

        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task CancelScheduledMessage_WhenAlreadyCompleted_ShouldReturnFalse()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ScheduledMessageStatus.Completed
        };
        await store.AddAsync(message);

        bool cancelled = await scheduler.CancelScheduledMessageAsync(message.Id);

        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task CancelScheduledMessage_WhenProcessing_ShouldReturnFalse()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ScheduledMessageStatus.Processing
        };
        await store.AddAsync(message);

        bool cancelled = await scheduler.CancelScheduledMessageAsync(message.Id);

        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task PauseRecurringMessage_WhenNotRecurring_ShouldReturnFalse()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ScheduledMessageStatus.Pending
        };
        await store.AddAsync(message);

        bool paused = await scheduler.PauseRecurringMessageAsync(message.Id);

        paused.Should().BeFalse();
    }

    [Fact]
    public async Task ResumeRecurringMessage_WhenNotPaused_ShouldReturnFalse()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            TimeSpan.FromMinutes(5),
            new TestOrderEvent { Name = "active" },
            routingKey: "order-event");

        bool resumed = await scheduler.ResumeRecurringMessageAsync(id);

        resumed.Should().BeFalse();
    }

    [Fact]
    public async Task ResumeRecurringMessage_WithCronSchedule_ShouldRecalculateNextExecution()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            "0 14 * * *",
            new TestOrderEvent { Name = "cron-resume" },
            routingKey: "order-event");

        (await scheduler.PauseRecurringMessageAsync(id)).Should().BeTrue();
        (await scheduler.ResumeRecurringMessageAsync(id)).Should().BeTrue();

        ScheduledMessage? stored = await store.GetByIdAsync(id);
        stored!.Status.Should().Be(ScheduledMessageStatus.Active);
        stored.NextExecutionTime.Should().NotBeNull();
    }

    [Fact]
    public async Task GetScheduledMessageAsync_ShouldDelegateToStore()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        Guid id = await scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(5),
            new TestOrderEvent { Name = "get" },
            new ScheduleMessageOptions { RoutingKey = "order" });

        ScheduledMessage? result = await scheduler.GetScheduledMessageAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithPriority_ShouldPublishWithPriority()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<byte>(),
                It.IsAny<string?>()))
            .Returns("token");

        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object);
        ScheduledMessage dueMessage = CreateDueMessage(priority: 9);
        await store.AddAsync(dueMessage);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(1);
        clientMock.Verify(c => c.Publish(
            It.IsAny<object>(),
            dueMessage.RoutingKey,
            (byte)9,
            dueMessage.CorrelationId), Times.Once);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithHeadersOnly_ShouldPublishWithHeaders()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>(),
                It.IsAny<string?>()))
            .Returns("token");

        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object);
        ScheduledMessage dueMessage = CreateDueMessage(headers: new Dictionary<string, object> { ["x-test"] = "1" });
        await store.AddAsync(dueMessage);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(1);
        clientMock.Verify(c => c.Publish(
            It.IsAny<object>(),
            dueMessage.RoutingKey,
            It.IsAny<IDictionary<string, object>>(),
            dueMessage.CorrelationId), Times.Once);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WithTtlOnly_ShouldPublishWithTtl()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.PublishWithTtl(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>()))
            .Returns("token");

        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object);
        ScheduledMessage dueMessage = CreateDueMessage(ttlMilliseconds: 15_000);
        await store.AddAsync(dueMessage);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(1);
        clientMock.Verify(c => c.PublishWithTtl(
            It.IsAny<object>(),
            dueMessage.RoutingKey,
            15_000,
            dueMessage.CorrelationId), Times.Once);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_RecurringMaxExecutions_ShouldCompleteAfterLimit()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string?>())).Returns("token");

        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object);
        ScheduledMessage recurring = CreateDueRecurringMessage(maxExecutions: 1);
        await store.AddAsync(recurring);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(1);
        (await store.GetByIdAsync(recurring.Id))!.Status.Should().Be(ScheduledMessageStatus.Completed);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_RecurringEndDateReached_ShouldComplete()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string?>())).Returns("token");

        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object);
        ScheduledMessage recurring = CreateDueRecurringMessage(endDate: DateTimeOffset.UtcNow.AddMinutes(-1));
        await store.AddAsync(recurring);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(1);
        (await store.GetByIdAsync(recurring.Id))!.Status.Should().Be(ScheduledMessageStatus.Completed);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenPublishFailsBelowMaxRetries_ShouldReschedule()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Throws(new InvalidOperationException("broker down"));

        MessageSchedulerOptions options = new() { MaxRetryCount = 3, BaseRetryDelayMs = 1000 };
        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object, options);
        ScheduledMessage dueMessage = CreateDueMessage();
        await store.AddAsync(dueMessage);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(0);
        ScheduledMessage? stored = await store.GetByIdAsync(dueMessage.Id);
        stored!.RetryCount.Should().Be(1);
        stored.Status.Should().Be(ScheduledMessageStatus.Pending);
        stored.LastError.Should().Contain("broker down");
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenPublishFailsAtMaxRetries_ShouldMarkFailed()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Throws(new InvalidOperationException("broker down"));

        MessageSchedulerOptions options = new() { MaxRetryCount = 1, BaseRetryDelayMs = 1000 };
        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object, options);
        ScheduledMessage dueMessage = CreateDueMessage();
        dueMessage.RetryCount = 1;
        await store.AddAsync(dueMessage);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(0);
        (await store.GetByIdAsync(dueMessage.Id))!.Status.Should().Be(ScheduledMessageStatus.Failed);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_WhenAlreadyProcessing_ShouldSkipMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        MessageScheduler scheduler = CreateScheduler(store, clientMock.Object);
        ScheduledMessage dueMessage = CreateDueMessage();
        await store.AddAsync(dueMessage);
        await store.MarkAsProcessingAsync(dueMessage.Id);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(0);
        clientMock.Verify(c => c.Publish(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    private static ScheduledMessage CreateDueMessage(
        byte? priority = null,
        Dictionary<string, object>? headers = null,
        int? ttlMilliseconds = null)
    {
        return new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = System.Text.Json.JsonSerializer.Serialize(new TestOrderEvent { Name = "due" }),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            RoutingKey = "order-event",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = ScheduledMessageStatus.Pending,
            CorrelationId = Guid.NewGuid().ToString(),
            Priority = priority,
            Headers = headers,
            TtlMilliseconds = ttlMilliseconds
        };
    }

    private static ScheduledMessage CreateDueRecurringMessage(int? maxExecutions = null, DateTimeOffset? endDate = null)
    {
        return new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = System.Text.Json.JsonSerializer.Serialize(new TestOrderEvent { Name = "recurring-due" }),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            RoutingKey = "order-event",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            NextExecutionTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = ScheduledMessageStatus.Active,
            CorrelationId = Guid.NewGuid().ToString(),
            RecurringSchedule = new RecurringSchedule
            {
                Type = RecurringScheduleType.Interval,
                Interval = TimeSpan.FromMinutes(5),
                MaxExecutions = maxExecutions,
                EndDate = endDate
            }
        };
    }
}
