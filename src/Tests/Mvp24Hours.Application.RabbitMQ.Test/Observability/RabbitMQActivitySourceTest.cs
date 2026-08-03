using System.Diagnostics;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;

namespace Mvp24Hours.Application.RabbitMQ.Test.Observability;

[Trait("Category", "Unit")]
public class RabbitMQActivitySourceTest : IDisposable
{
    private readonly List<Activity> _startedActivities = [];
    private readonly ActivityListener _listener;

    public RabbitMQActivitySourceTest()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RabbitMQActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => _startedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        foreach (Activity activity in _startedActivities)
        {
            activity.Dispose();
        }
    }

    [Fact]
    public void Constants_ShouldExposeExpectedValues()
    {
        RabbitMQActivitySource.SourceName.Should().Be("Mvp24Hours.RabbitMQ");
        RabbitMQActivitySource.Version.Should().Be("1.0.0");
        RabbitMQActivitySource.Source.Name.Should().Be(RabbitMQActivitySource.SourceName);
        RabbitMQActivitySource.ActivityNames.Publish.Should().Be("Mvp24Hours.RabbitMQ.Publish");
        RabbitMQActivitySource.ActivityNames.Scheduled.Should().Be("Mvp24Hours.RabbitMQ.Scheduled");
        RabbitMQActivitySource.Tags.MessagingSystem.Should().Be("messaging.system");
    }

    [Fact]
    public void StartPublishActivity_WithRoutingKey_ShouldSetTags()
    {
        using Activity? activity = RabbitMQActivitySource.StartPublishActivity("OrderEvent", "orders", "order.created");

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Producer);
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingSystem).Should().Be("rabbitmq");
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingOperation).Should().Be("send");
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingDestination).Should().Be("orders");
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingRoutingKey).Should().Be("order.created");
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingMessageType).Should().Be("OrderEvent");
    }

    [Fact]
    public void StartPublishActivity_WithoutRoutingKey_ShouldOmitRoutingKeyTag()
    {
        using Activity? activity = RabbitMQActivitySource.StartPublishActivity("OrderEvent", "orders");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RabbitMQActivitySource.Tags.MessagingRoutingKey).Should().BeNull();
    }

    [Fact]
    public void StartConsumeActivity_ShouldSetConsumerTags()
    {
        using Activity? activity = RabbitMQActivitySource.StartConsumeActivity("OrderEvent", "order-queue");

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Consumer);
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingConsumerQueue).Should().Be("order-queue");
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingOperation).Should().Be("receive");
    }

    [Fact]
    public void StartRequestActivity_ShouldSetRequestTags()
    {
        using Activity? activity = RabbitMQActivitySource.StartRequestActivity("GetOrder", "OrderResponse", "rpc-queue");

        activity.Should().NotBeNull();
        activity!.Kind.Should().Be(ActivityKind.Client);
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingDestination).Should().Be("rpc-queue");
        activity.GetTagItem("messaging.response_type").Should().Be("OrderResponse");
    }

    [Fact]
    public void StartBatchActivity_ShouldSetBatchSizeTag()
    {
        using Activity? activity = RabbitMQActivitySource.StartBatchActivity("OrderEvent", 25, "batch-queue");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RabbitMQActivitySource.Tags.BatchSize).Should().Be(25);
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingOperation).Should().Be("process");
    }

    [Fact]
    public void StartSagaActivity_ShouldSetCorrelationAndSagaType()
    {
        using Activity? activity = RabbitMQActivitySource.StartSagaActivity("OrderSaga", "corr-123");

        activity.Should().NotBeNull();
        activity!.GetTagItem(RabbitMQActivitySource.Tags.MessagingCorrelationId).Should().Be("corr-123");
        activity.GetTagItem("saga.type").Should().Be("OrderSaga");
    }

    [Fact]
    public void RecordException_WithNullActivity_ShouldNotThrow()
    {
        Action act = () => RabbitMQActivitySource.RecordException(null, new InvalidOperationException("fail"));

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_WithActivity_ShouldSetErrorStatusAndTags()
    {
        using Activity? activity = RabbitMQActivitySource.StartPublishActivity("OrderEvent", "orders");
        var exception = new InvalidOperationException("publish failed");

        RabbitMQActivitySource.RecordException(activity, exception);

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem(RabbitMQActivitySource.Tags.ErrorType).Should().Be(typeof(InvalidOperationException).FullName);
        activity.GetTagItem(RabbitMQActivitySource.Tags.ErrorMessage).Should().Be("publish failed");
        activity.Events.Should().ContainSingle(e => e.Name == "exception");
    }

    [Fact]
    public void SetSuccess_WithNullActivity_ShouldNotThrow()
    {
        Action act = () => RabbitMQActivitySource.SetSuccess(null);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetSuccess_WithActivity_ShouldMarkOk()
    {
        using Activity? activity = RabbitMQActivitySource.StartPublishActivity("OrderEvent", "orders");

        RabbitMQActivitySource.SetSuccess(activity);

        activity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void EnrichWithIds_ShouldSetProvidedTagsOnly()
    {
        using Activity? activity = RabbitMQActivitySource.StartPublishActivity("OrderEvent", "orders");

        RabbitMQActivitySource.EnrichWithIds(activity, "msg-1", null, "cause-1");

        activity!.GetTagItem(RabbitMQActivitySource.Tags.MessagingMessageId).Should().Be("msg-1");
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingCorrelationId).Should().BeNull();
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingCausationId).Should().Be("cause-1");
    }

    [Fact]
    public void EnrichWithIds_WithNullActivity_ShouldNotThrow()
    {
        Action act = () => RabbitMQActivitySource.EnrichWithIds(null, "id", "corr", "cause");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnrichWithTenancy_ShouldSetTenantAndUserTags()
    {
        using Activity? activity = RabbitMQActivitySource.StartPublishActivity("OrderEvent", "orders");

        RabbitMQActivitySource.EnrichWithTenancy(activity, "tenant-a", "user-b");

        activity!.GetTagItem(RabbitMQActivitySource.Tags.TenantId).Should().Be("tenant-a");
        activity.GetTagItem(RabbitMQActivitySource.Tags.UserId).Should().Be("user-b");
    }

    [Fact]
    public void EnrichWithDeliveryInfo_ShouldSetRedeliveryTags()
    {
        using Activity? activity = RabbitMQActivitySource.StartConsumeActivity("OrderEvent", "order-queue");

        RabbitMQActivitySource.EnrichWithDeliveryInfo(activity, true, 3);

        activity!.GetTagItem(RabbitMQActivitySource.Tags.MessagingRedelivered).Should().Be(true);
        activity.GetTagItem(RabbitMQActivitySource.Tags.MessagingRedeliveryCount).Should().Be(3);
    }
}
