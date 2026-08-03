using Moq;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology;
using RabbitMQ.Client;

namespace Mvp24Hours.Application.RabbitMQ.Test.Topology;

[Trait("Category", "Unit")]
public sealed class TopicExchangeHelperTest
{
    private readonly Mock<IModel> _channel = new();

    [Fact]
    public void DeclareTopicExchange_ShouldCallExchangeDeclare()
    {
        var arguments = new Dictionary<string, object> { ["alternate-exchange"] = "dead-letter" };

        TopicExchangeHelper.DeclareTopicExchange(
            _channel.Object,
            "orders",
            durable: false,
            autoDelete: true,
            arguments);

        _channel.Verify(
            channel => channel.ExchangeDeclare(
                "orders",
                ExchangeType.Topic,
                false,
                true,
                arguments),
            Times.Once);
    }

    [Theory]
    [InlineData("orders.created", "orders.*", true)]
    [InlineData("orders.items.added", "orders.#", true)]
    [InlineData("orders.created", "products.*", false)]
    [InlineData("", "orders.*", false)]
    public void Matches_ShouldEvaluateTopicPatterns(
        string routingKey,
        string pattern,
        bool expected)
    {
        TopicExchangeHelper.Matches(routingKey, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("orders.*", true)]
    [InlineData("orders.#", true)]
    [InlineData("bad pattern", false)]
    public void IsValidPattern_ShouldValidateWildcards(string pattern, bool expected)
    {
        TopicExchangeHelper.IsValidPattern(pattern).Should().Be(expected);
    }

    [Fact]
    public void PatternBuilders_ShouldCreateExpectedSegments()
    {
        TopicExchangeHelper.CreateCategoryPattern("orders")
            .Should().Be("orders.*");
        TopicExchangeHelper.CreateNamespacePattern("sales.orders")
            .Should().Be("sales.orders.#");
        TopicExchangeHelper.CreateEventTypePattern("created")
            .Should().Be("*.*.created");
        TopicExchangeHelper.BuildRoutingKey("orders", "created")
            .Should().Be("orders.created");
        TopicExchangeHelper.ParseRoutingKey("orders.created")
            .Should().Equal("orders", "created");
    }

    [Fact]
    public void BindQueueToMultiplePatterns_ShouldBindEachPattern()
    {
        TopicExchangeHelper.BindQueueToMultiplePatterns(
            _channel.Object,
            "queue-a",
            "orders",
            ["orders.*", "orders.#"]);

        _channel.Invocations.Count(invocation => invocation.Method.Name == nameof(IModel.QueueBind))
            .Should().Be(2);
    }

    [Fact]
    public void BindQueueToAllTopics_ShouldUseHashWildcard()
    {
        TopicExchangeHelper.BindQueueToAllTopics(_channel.Object, "queue-a", "orders");

        _channel.Invocations.Count(invocation => invocation.Method.Name == nameof(IModel.QueueBind))
            .Should().Be(1);
    }
}
