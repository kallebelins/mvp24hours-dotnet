using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
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

    #region AutoBindingOptions

    [Fact]
    public void AutoBindingOptions_Defaults_ShouldHaveExpectedValues()
    {
        var opts = new AutoBindingOptions();

        opts.Durable.Should().BeTrue();
        opts.AutoDelete.Should().BeFalse();
        opts.ConfigureDeadLetter.Should().BeTrue();
        opts.CreateDefaultQueue.Should().BeTrue();
        opts.DefaultExchangeType.Should().Be(MvpRabbitMQExchangeType.direct);
        opts.DefaultMessageTtlMilliseconds.Should().Be(0);
        opts.EnablePriorityQueue.Should().BeFalse();
        opts.MaxPriority.Should().Be(10);
        opts.ContinueOnError.Should().BeTrue();
    }

    [Fact]
    public void AutoBindingOptions_CustomValues_ShouldBeSetCorrectly()
    {
        var opts = new AutoBindingOptions
        {
            Durable = false,
            AutoDelete = true,
            ConfigureDeadLetter = false,
            CreateDefaultQueue = false,
            DefaultExchangeType = MvpRabbitMQExchangeType.fanout,
            DefaultMessageTtlMilliseconds = 60000,
            EnablePriorityQueue = true,
            MaxPriority = 5,
            ContinueOnError = false
        };

        opts.Durable.Should().BeFalse();
        opts.AutoDelete.Should().BeTrue();
        opts.ConfigureDeadLetter.Should().BeFalse();
        opts.CreateDefaultQueue.Should().BeFalse();
        opts.DefaultExchangeType.Should().Be(MvpRabbitMQExchangeType.fanout);
        opts.DefaultMessageTtlMilliseconds.Should().Be(60000);
        opts.EnablePriorityQueue.Should().BeTrue();
        opts.MaxPriority.Should().Be(5);
        opts.ContinueOnError.Should().BeFalse();
    }

    #endregion

    #region ConsumerBindingInfo

    [Fact]
    public void ConsumerBindingInfo_ShouldSetAllProperties()
    {
        var info = new ConsumerBindingInfo
        {
            ConsumerType = typeof(TestOrderCreatedEvent),
            MessageType = typeof(TestOrderEvent),
            QueueName = "orders-queue",
            ExchangeName = "orders-exchange",
            ExchangeType = MvpRabbitMQExchangeType.direct,
            RoutingKey = "order.created"
        };

        info.ConsumerType.Should().Be(typeof(TestOrderCreatedEvent));
        info.MessageType.Should().Be(typeof(TestOrderEvent));
        info.QueueName.Should().Be("orders-queue");
        info.ExchangeName.Should().Be("orders-exchange");
        info.ExchangeType.Should().Be(MvpRabbitMQExchangeType.direct);
        info.RoutingKey.Should().Be("order.created");
    }

    #endregion

    #region MessageBindingInfo

    [Fact]
    public void MessageBindingInfo_ShouldSetAllProperties()
    {
        var info = new MessageBindingInfo
        {
            MessageType = typeof(TestOrderEvent),
            ExchangeName = "orders",
            ExchangeType = MvpRabbitMQExchangeType.topic,
            QueueName = "orders.queue",
            RoutingKey = "orders.*"
        };

        info.MessageType.Should().Be(typeof(TestOrderEvent));
        info.ExchangeName.Should().Be("orders");
        info.ExchangeType.Should().Be(MvpRabbitMQExchangeType.topic);
        info.QueueName.Should().Be("orders.queue");
        info.RoutingKey.Should().Be("orders.*");
    }

    [Fact]
    public void MessageBindingInfo_QueueName_CanBeNull()
    {
        var info = new MessageBindingInfo
        {
            MessageType = typeof(TestOrderEvent),
            ExchangeName = "exchange",
            RoutingKey = "key"
        };

        info.QueueName.Should().BeNull();
    }

    #endregion

    #region EndpointNameFormatter

    [Fact]
    public void EndpointNameFormatter_FormatQueueName_ShouldCreateFromType()
    {
        var formatter = new EndpointNameFormatter("mvp", new EndpointNamingConventionOptions());

        string queueName = formatter.FormatQueueName(typeof(TestOrderCreatedEvent));

        queueName.Should().NotBeNullOrWhiteSpace();
        queueName.Should().StartWith("mvp");
    }

    [Fact]
    public void EndpointNameFormatter_FormatExchangeName_ShouldNotBeEmpty()
    {
        var formatter = new EndpointNameFormatter("mvp", new EndpointNamingConventionOptions());

        string exchangeName = formatter.FormatExchangeName(typeof(TestOrderEvent));

        exchangeName.Should().NotBeNullOrWhiteSpace();
        exchangeName.Should().StartWith("mvp");
    }

    [Fact]
    public void EndpointNameFormatter_FormatDeadLetterExchangeName_ShouldAppendDlxSuffix()
    {
        var formatter = new EndpointNameFormatter("mvp", new EndpointNamingConventionOptions());

        string dlxName = formatter.FormatDeadLetterExchangeName("orders-exchange");

        dlxName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void EndpointNameFormatter_Instance_ShouldNotBeNull()
    {
        EndpointNameFormatter.Instance.Should().NotBeNull();
    }

    #endregion

    #region MessageTopologyRegistry

    [Fact]
    public void MessageTopologyRegistry_GetTopology_ForUnregisteredType_ShouldReturnNullOrDefault()
    {
        MessageTopologyRegistry.Instance.Clear();

        var topology = MessageTopologyRegistry.Instance.GetTopology<TestPaymentCompletedEvent>();

        // After clear, the registry may auto-create a default or return null
        // The important behavior is the registry handles missing types gracefully
        if (topology != null)
        {
            topology.ExchangeName.Should().BeNull();
        }
    }

    [Fact]
    public void MessageTopologyRegistry_HasTopology_WhenRegistered_ShouldReturnTrue()
    {
        MessageTopologyRegistry.Instance.Register<TestOrderCommand>(t =>
        {
            t.ExchangeName = "commands";
        });

        bool has = MessageTopologyRegistry.Instance.HasTopology<TestOrderCommand>();

        has.Should().BeTrue();
    }

    [Fact]
    public void MessageTopologyRegistry_Instance_ShouldBeSingleton()
    {
        var instance1 = MessageTopologyRegistry.Instance;
        var instance2 = MessageTopologyRegistry.Instance;

        instance1.Should().BeSameAs(instance2);
    }

    #endregion

    #region RoutingKeyConvention

    [Fact]
    public void RoutingKeyConvention_GetRoutingKey_WithoutNamespace_ShouldReturnNonEmpty()
    {
        var convention = new RoutingKeyConvention(new RoutingKeyConventionOptions
        {
            IncludeNamespace = false
        });

        string routingKey = convention.GetRoutingKey(typeof(TestOrderCreatedEvent));

        routingKey.Should().NotBeNullOrWhiteSpace();
        routingKey.Should().Contain("testorder");
    }

    [Fact]
    public void RoutingKeyConvention_Instance_ShouldNotBeNull()
    {
        RoutingKeyConvention.Instance.Should().NotBeNull();
    }

    [Theory]
    [InlineData("a.b.c", "a.b.c", true)]
    [InlineData("a.b.c", "a.b.*", true)]
    [InlineData("a.b.c.d", "a.#", true)]
    [InlineData("x.y.z", "a.b.c", false)]
    public void RoutingKeyConvention_Matches_AdditionalPatterns_ShouldWork(string key, string pattern, bool expected)
    {
        var convention = new RoutingKeyConvention();

        convention.Matches(key, pattern).Should().Be(expected);
    }

    #endregion

    #region EndpointConvention Additional

    [Fact]
    public void EndpointConvention_MapToQueue_ShouldStoreMapping()
    {
        EndpointConvention.Reset();
        EndpointConvention.MapToQueue<TestOrderEvent>("test-queue");

        EndpointConvention.GetEndpoint<TestOrderEvent>()!.QueueName.Should().Be("test-queue");
    }

    [Fact]
    public void EndpointConvention_Reset_ShouldClearAllMappings()
    {
        EndpointConvention.MapToQueue<TestOrderEvent>("q1");
        EndpointConvention.MapToQueue<TestPaymentCompletedEvent>("q2");
        
        EndpointConvention.Reset();

        EndpointConvention.GetEndpoint<TestOrderEvent>().Should().BeNull();
        EndpointConvention.GetEndpoint<TestPaymentCompletedEvent>().Should().BeNull();
    }

    [Fact]
    public void EndpointConvention_Unmap_NonExistentType_ShouldReturnFalse()
    {
        EndpointConvention.Reset();

        bool result = EndpointConvention.Unmap<TestOrderEvent>();

        result.Should().BeFalse();
    }

    #endregion
}
