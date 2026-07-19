using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology;

namespace Mvp24Hours.Application.RabbitMQ.Test.Topology;

public class TopologyTest
{
    [Fact]
    public void RoutingKeyConvention_GetRoutingKey_ShouldIncludeNamespaceSegments()
    {
        var convention = new RoutingKeyConvention(new RoutingKeyConventionOptions
        {
            IncludeNamespace = true,
            NamespaceDepth = 2,
            IncludeMessageTypeCategory = false
        });

        string routingKey = convention.GetRoutingKey(typeof(TestOrderCreatedEvent));

        routingKey.Should().Contain("test.support");
        routingKey.Should().Contain("testordercreated");
    }

    [Theory]
    [InlineData("orders.created", "orders.*", true)]
    [InlineData("orders.created", "payments.*", false)]
    [InlineData("orders.created.v1", "orders.#", true)]
    [InlineData("orders", "orders.created", false)]
    public void RoutingKeyConvention_Matches_ShouldSupportTopicPatterns(string routingKey, string pattern, bool expected)
    {
        var convention = new RoutingKeyConvention();

        convention.Matches(routingKey, pattern).Should().Be(expected);
    }

    [Fact]
    public void EndpointNameFormatter_FormatQueueNameFromMessage_ShouldAppendQueueSuffix()
    {
        var formatter = new EndpointNameFormatter("mvp", new EndpointNamingConventionOptions());

        string queueName = formatter.FormatQueueNameFromMessage(typeof(TestOrderEvent));

        queueName.Should().StartWith("mvp");
        queueName.Should().EndWith(".queue");
    }

    [Fact]
    public void EndpointConvention_MapToExchange_ShouldOverrideRouting()
    {
        EndpointConvention.Reset();
        EndpointConvention.MapToExchange<TestOrderEvent>("custom-exchange", "custom.route");

        EndpointConvention.GetExchangeName<TestOrderEvent>().Should().Be("custom-exchange");
        EndpointConvention.GetRoutingKey<TestOrderEvent>().Should().Be("custom.route");
    }

    [Fact]
    public void MessageTopologyRegistry_RegisterAndGet_ShouldReturnConfiguredTopology()
    {
        MessageTopologyRegistry.Instance.Clear();
        MessageTopologyRegistry.Instance.Register<TestOrderEvent>(topology =>
        {
            topology.ExchangeName = "orders";
            topology.RoutingKey = "order.created";
        });

        var topology = MessageTopologyRegistry.Instance.GetTopology<TestOrderEvent>();

        topology.ExchangeName.Should().Be("orders");
        topology.RoutingKey.Should().Be("order.created");
        MessageTopologyRegistry.Instance.HasTopology<TestOrderEvent>().Should().BeTrue();
    }

    [Fact]
    public void EndpointConvention_Unmap_ShouldRemoveMapping()
    {
        EndpointConvention.Reset();
        EndpointConvention.MapToQueue<TestOrderEvent>("orders-queue");

        EndpointConvention.Unmap<TestOrderEvent>().Should().BeTrue();
        EndpointConvention.GetEndpoint<TestOrderEvent>().Should().BeNull();
    }
}
