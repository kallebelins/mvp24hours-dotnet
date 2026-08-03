using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology.Contract;
using RabbitMQ.Client;

namespace Mvp24Hours.Application.RabbitMQ.Test.Topology;

[Trait("Category", "Unit")]
public class TopologyBuilderTest
{
    private static Mock<IModel> CreateChannelMock()
    {
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.QueueDelete(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(1u);
        channelMock.Setup(c => c.QueuePurge(It.IsAny<string>())).Returns(1u);
        return channelMock;
    }

    [Fact]
    public void Constructor_WithNullNameFormatter_ShouldThrow()
    {
        Action act = () => new TopologyBuilder(
            null!,
            RoutingKeyConvention.Instance,
            new TopologyBuilderOptions());

        act.Should().Throw<ArgumentNullException>().WithParameterName("nameFormatter");
    }

    [Fact]
    public void DeclareExchange_ShouldInvokeChannelExchangeDeclare()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var builder = new TopologyBuilder();

        builder.DeclareExchange(channelMock.Object, "orders", ExchangeType.Topic, durable: false, autoDelete: true);

        channelMock.Verify(
            c => c.ExchangeDeclare("orders", ExchangeType.Topic, false, true, null),
            Times.Once);
    }

    [Fact]
    public void DeclareQueue_ShouldInvokeChannelQueueDeclare()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var builder = new TopologyBuilder();

        builder.DeclareQueue(channelMock.Object, "orders-queue", durable: false, exclusive: true, autoDelete: true);

        channelMock.Verify(
            c => c.QueueDeclare("orders-queue", false, true, true, null),
            Times.Once);
    }

    [Fact]
    public void BindQueue_WithNullRoutingKey_ShouldBindEmptyKey()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var builder = new TopologyBuilder();

        builder.BindQueue(channelMock.Object, "q", "ex", null!);

        channelMock.Verify(c => c.QueueBind("q", "ex", string.Empty, null), Times.Once);
    }

    [Fact]
    public void BindExchange_ShouldInvokeChannelExchangeBind()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var builder = new TopologyBuilder();

        builder.BindExchange(channelMock.Object, "dest", "source", "route.key");

        channelMock.Verify(c => c.ExchangeBind("dest", "source", "route.key", null), Times.Once);
    }

    [Fact]
    public void UnbindQueue_ShouldInvokeChannelQueueUnbind()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var builder = new TopologyBuilder();

        builder.UnbindQueue(channelMock.Object, "q", "ex", "rk");

        channelMock.Verify(c => c.QueueUnbind("q", "ex", "rk", null), Times.Once);
    }

    [Fact]
    public void UnbindExchange_ShouldInvokeChannelExchangeUnbind()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var builder = new TopologyBuilder();

        builder.UnbindExchange(channelMock.Object, "dest", "source", "rk");

        channelMock.Verify(c => c.ExchangeUnbind("dest", "source", "rk", null), Times.Once);
    }

    [Fact]
    public void DeleteExchange_ShouldInvokeChannelExchangeDelete()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var builder = new TopologyBuilder();

        builder.DeleteExchange(channelMock.Object, "ex-to-delete", ifUnused: true);

        channelMock.Verify(c => c.ExchangeDelete("ex-to-delete", true), Times.Once);
    }

    [Fact]
    public void DeleteQueue_ShouldReturnDeletedCount()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.QueueDelete("q", false, true)).Returns(3u);
        var builder = new TopologyBuilder();

        uint deleted = builder.DeleteQueue(channelMock.Object, "q", ifUnused: false, ifEmpty: true);

        deleted.Should().Be(3);
    }

    [Fact]
    public void PurgeQueue_ShouldReturnPurgedCount()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.QueuePurge("q")).Returns(5u);
        var builder = new TopologyBuilder();

        uint purged = builder.PurgeQueue(channelMock.Object, "q");

        purged.Should().Be(5);
    }

    [Fact]
    public void ConfigureTopologyForMessage_WithDeadLetter_ShouldDeclareDlxAndDlq()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var options = new TopologyBuilderOptions { AutoConfigureDeadLetter = true };
        var builder = new TopologyBuilder(
            EndpointNameFormatter.Instance,
            RoutingKeyConvention.Instance,
            options,
            NullLogger<TopologyBuilder>.Instance);

        MessageTopologyRegistry.Instance.Register<TopologyBuilderTestEvent>(t =>
        {
            t.ExchangeName = "orders-exchange";
            t.ExchangeType = MvpRabbitMQExchangeType.topic;
        });

        builder.ConfigureTopologyForMessage(channelMock.Object, typeof(TopologyBuilderTestEvent));

        channelMock.Verify(c => c.ExchangeDeclare(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.AtLeast(2));
        channelMock.Verify(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.AtLeastOnce);
        channelMock.Verify(c => c.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ConfigureTopologyForConsumer_WithMessageConsumer_ShouldDeclareQueueAndBind()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var options = new TopologyBuilderOptions { AutoConfigureDeadLetter = false };
        var builder = new TopologyBuilder(
            EndpointNameFormatter.Instance,
            RoutingKeyConvention.Instance,
            options);

        builder.ConfigureTopologyForConsumer(channelMock.Object, typeof(TopologyTestOrderConsumer));

        channelMock.Verify(c => c.ExchangeDeclare(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
        channelMock.Verify(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
        channelMock.Verify(c => c.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public void ConfigureTopologyForConsumer_WithoutMessageType_ShouldNotThrow()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var options = new TopologyBuilderOptions { AutoConfigureDeadLetter = false };
        var builder = new TopologyBuilder(
            EndpointNameFormatter.Instance,
            RoutingKeyConvention.Instance,
            options);

        Action act = () => builder.ConfigureTopologyForConsumer(channelMock.Object, typeof(string));

        act.Should().NotThrow();
        channelMock.Verify(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public void DeclareExchange_WithNullChannel_ShouldThrow()
    {
        var builder = new TopologyBuilder();

        Action act = () => builder.DeclareExchange(null!, "ex", ExchangeType.Direct);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AutoBindingHelper_AutoBindConsumer_ShouldDeclareExchangeQueueAndBind()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var helper = new AutoBindingHelper();

        ConsumerBindingInfo binding = helper.AutoBindConsumer<TopologyTestOrderConsumer>(channelMock.Object);

        binding.MessageType.Should().Be(typeof(TestOrderEvent));
        binding.QueueName.Should().NotBeNullOrWhiteSpace();
        binding.ExchangeName.Should().NotBeNullOrWhiteSpace();
        channelMock.Verify(
            c => c.ExchangeDeclare(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()),
            Times.Once);
        channelMock.Verify(
            c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()),
            Times.Once);
        channelMock.Verify(
            c => c.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public void AutoBindingHelper_AutoBindMessage_ShouldDeclareExchangeAndDefaultQueue()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var options = new AutoBindingOptions { CreateDefaultQueue = true, ConfigureDeadLetter = false };
        var helper = new AutoBindingHelper(
            EndpointNameFormatter.Instance,
            RoutingKeyConvention.Instance,
            new TopologyBuilder(),
            options);

        MessageBindingInfo binding = helper.AutoBindMessage<TopologyBuilderTestEvent>(channelMock.Object);

        binding.MessageType.Should().Be(typeof(TopologyBuilderTestEvent));
        binding.QueueName.Should().NotBeNullOrWhiteSpace();
        channelMock.Verify(c => c.ExchangeDeclare(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
        channelMock.Verify(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
        channelMock.Verify(c => c.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()), Times.Once);
    }

    [Fact]
    public void AutoBindingHelper_AutoBindConsumer_WithNullChannel_ShouldThrow()
    {
        var helper = new AutoBindingHelper();

        Action act = () => helper.AutoBindConsumer<TopologyTestOrderConsumer>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AutoBindingHelper_AutoBindConsumer_WithoutMessageConsumer_ShouldThrow()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var helper = new AutoBindingHelper();

        Action act = () => helper.AutoBindConsumer(channelMock.Object, typeof(string));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Could not determine message type*");
    }

    [Fact]
    public void AutoBindingHelper_AutoBindConsumersFromAssembly_ShouldBindDiscoveredConsumers()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        var helper = new AutoBindingHelper();

        IEnumerable<ConsumerBindingInfo> bindings = helper.AutoBindConsumersFromAssembly(
            channelMock.Object,
            typeof(AutoBindingTestOrderConsumer).Assembly);

        bindings.Should().Contain(b => b.ConsumerType == typeof(AutoBindingTestOrderConsumer));
        channelMock.Verify(
            c => c.QueueBind(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()),
            Times.AtLeastOnce);
    }

    private sealed class TopologyTestOrderConsumer : IMessageConsumer<TestOrderEvent>
    {
        public Task ConsumeAsync(IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TopologyBuilderTestEvent
    {
        public string Name { get; set; } = string.Empty;
    }
}

public sealed class AutoBindingTestOrderConsumer : IMessageConsumer<TestOrderEvent>
{
    public Task ConsumeAsync(IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
