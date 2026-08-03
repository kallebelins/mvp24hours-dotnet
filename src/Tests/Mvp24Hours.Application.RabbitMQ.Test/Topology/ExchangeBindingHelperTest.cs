using Moq;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology;
using RabbitMQ.Client;

namespace Mvp24Hours.Application.RabbitMQ.Test.Topology;

[Trait("Category", "Unit")]
public class ExchangeBindingHelperTest
{
    private readonly Mock<IModel> _channel = new();

    [Fact]
    public void BindExchanges_ShouldCallExchangeBind()
    {
        var arguments = new Dictionary<string, object> { ["x-match"] = "all" };

        ExchangeBindingHelper.BindExchanges(_channel.Object, "dest", "source", "route.key", arguments);

        _channel.Verify(c => c.ExchangeBind("dest", "source", "route.key", arguments), Times.Once);
    }

    [Fact]
    public void UnbindExchanges_ShouldCallExchangeUnbind()
    {
        ExchangeBindingHelper.UnbindExchanges(_channel.Object, "dest", "source", "route.key");

        _channel.Verify(c => c.ExchangeUnbind("dest", "source", "route.key", null), Times.Once);
    }

    [Fact]
    public void BindExchanges_WithNullChannel_ShouldThrow()
    {
        Action act = () => ExchangeBindingHelper.BindExchanges(null!, "dest", "source");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BindExchanges_WithNullDestination_ShouldThrow()
    {
        Action act = () => ExchangeBindingHelper.BindExchanges(_channel.Object, null!, "source");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateExchangeHierarchy_WithTopicExchange_ShouldDeclareAndBindWithHashRoutingKey()
    {
        ExchangeBindingHelper.CreateExchangeHierarchy(
            _channel.Object,
            "parent",
            ["child-a", "child-b"],
            MvpRabbitMQExchangeType.topic);

        _channel.Verify(c => c.ExchangeDeclare("parent", "topic", true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeDeclare("child-a", "topic", true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeDeclare("child-b", "topic", true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("child-a", "parent", "#", null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("child-b", "parent", "#", null), Times.Once);
    }

    [Fact]
    public void CreateExchangeHierarchy_WithDirectExchange_ShouldBindWithEmptyRoutingKey()
    {
        ExchangeBindingHelper.CreateExchangeHierarchy(
            _channel.Object,
            "parent",
            ["child"],
            MvpRabbitMQExchangeType.direct);

        _channel.Verify(c => c.ExchangeBind("child", "parent", string.Empty, null), Times.Once);
    }

    [Fact]
    public void CreateFanOutTopology_ShouldDeclareFanoutSourceAndBindDestinations()
    {
        ExchangeBindingHelper.CreateFanOutTopology(
            _channel.Object,
            "fanout-source",
            ["dest-a", "dest-b"]);

        _channel.Verify(c => c.ExchangeDeclare("fanout-source", ExchangeType.Fanout, true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeDeclare("dest-a", ExchangeType.Direct, true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeDeclare("dest-b", ExchangeType.Direct, true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("dest-a", "fanout-source", string.Empty, null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("dest-b", "fanout-source", string.Empty, null), Times.Once);
    }

    [Fact]
    public void CreateAggregationTopology_ShouldBindSourcesToDestination()
    {
        ExchangeBindingHelper.CreateAggregationTopology(
            _channel.Object,
            ["source-a", "source-b"],
            "aggregate",
            MvpRabbitMQExchangeType.topic);

        _channel.Verify(c => c.ExchangeDeclare("aggregate", "topic", true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("aggregate", "source-a", "#", null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("aggregate", "source-b", "#", null), Times.Once);
    }

    [Fact]
    public void CreateContentBasedRouter_ShouldBindEachRule()
    {
        var rules = new Dictionary<string, string>
        {
            ["orders"] = "order.*",
            ["payments"] = "payment.*"
        };

        ExchangeBindingHelper.CreateContentBasedRouter(_channel.Object, "router", rules);

        _channel.Verify(c => c.ExchangeDeclare("router", ExchangeType.Topic, true, false, null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("orders", "router", "order.*", null), Times.Once);
        _channel.Verify(c => c.ExchangeBind("payments", "router", "payment.*", null), Times.Once);
    }

    [Fact]
    public void SetupDeadLetterExchange_ShouldDeclareExchangeQueueAndBind()
    {
        ExchangeBindingHelper.SetupDeadLetterExchange(
            _channel.Object,
            "main",
            "dlx",
            "dlq");

        _channel.Verify(c => c.ExchangeDeclare("dlx", ExchangeType.Direct, true, false, null), Times.Once);
        _channel.Verify(c => c.QueueDeclare("dlq", true, false, false, null), Times.Once);
        _channel.Verify(c => c.QueueBind("dlq", "dlx", "dlq", null), Times.Once);
    }

    [Fact]
    public void CreateFanOutTopology_WithNullSourceExchange_ShouldThrow()
    {
        Action act = () => ExchangeBindingHelper.CreateFanOutTopology(_channel.Object, null!, ["dest"]);

        act.Should().Throw<ArgumentNullException>();
    }
}
