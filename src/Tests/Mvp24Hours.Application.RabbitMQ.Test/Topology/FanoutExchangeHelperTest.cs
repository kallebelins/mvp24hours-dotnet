using Moq;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology;
using RabbitMQ.Client;

namespace Mvp24Hours.Application.RabbitMQ.Test.Topology;

[Trait("Category", "Unit")]
public class FanoutExchangeHelperTest
{
    private readonly Mock<IModel> _channel = new();

    [Fact]
    public void DeclareFanoutExchange_ShouldCallExchangeDeclare()
    {
        var arguments = new Dictionary<string, object> { ["x-max-length"] = 1000 };

        FanoutExchangeHelper.DeclareFanoutExchange(_channel.Object, "fanout", durable: false, autoDelete: true, arguments);

        _channel.Verify(c => c.ExchangeDeclare("fanout", ExchangeType.Fanout, false, true, arguments), Times.Once);
    }

    [Fact]
    public void DeclareFanoutExchange_WithNullChannel_ShouldThrow()
    {
        Action act = () => FanoutExchangeHelper.DeclareFanoutExchange(null!, "fanout");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BindQueueToFanout_ShouldCallQueueBind()
    {
        FanoutExchangeHelper.BindQueueToFanout(_channel.Object, "queue-a", "fanout");

        _channel.Invocations.Count(i => i.Method.Name == nameof(IModel.QueueBind)).Should().Be(1);
    }

    [Fact]
    public void BindQueuesToFanout_ShouldBindEachQueue()
    {
        FanoutExchangeHelper.BindQueuesToFanout(_channel.Object, ["q1", "q2"], "fanout");

        _channel.Invocations.Count(i => i.Method.Name == nameof(IModel.QueueBind)).Should().Be(2);
    }

    [Fact]
    public void SetupFanoutWithQueues_ShouldDeclareAndBindAllQueues()
    {
        FanoutExchangeHelper.SetupFanoutWithQueues(_channel.Object, "fanout", ["q1", "q2"]);

        _channel.Verify(c => c.ExchangeDeclare("fanout", ExchangeType.Fanout, true, false, null), Times.Once);
        _channel.Invocations.Count(i => i.Method.Name == nameof(IModel.QueueBind)).Should().Be(2);
    }

    [Fact]
    public void UnbindQueueFromFanout_ShouldCallQueueUnbind()
    {
        FanoutExchangeHelper.UnbindQueueFromFanout(_channel.Object, "queue-a", "fanout");

        _channel.Invocations.Count(i => i.Method.Name == nameof(IModel.QueueUnbind)).Should().Be(1);
    }

    [Fact]
    public void PublishToFanout_ShouldCallBasicPublish()
    {
        var propertiesMock = new Mock<IBasicProperties>();
        ReadOnlyMemory<byte> body = new([1, 2, 3]);

        FanoutExchangeHelper.PublishToFanout(_channel.Object, "fanout", body, propertiesMock.Object);

        _channel.Invocations.Count(i => i.Method.Name == nameof(IModel.BasicPublish)).Should().Be(1);
    }

    [Fact]
    public void BindQueuesToFanout_WithNullQueues_ShouldThrow()
    {
        Action act = () => FanoutExchangeHelper.BindQueuesToFanout(_channel.Object, null!, "fanout");

        act.Should().Throw<ArgumentNullException>();
    }
}
