using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Topology;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mvp24Hours.Application.RabbitMQ.Test.Integration;

[Trait("Category", "Integration")]
[Collection(RabbitMqIntegrationCollection.Name)]
public class TopologyBuilderIntegrationTest(RabbitMqIntegrationFixture fixture)
{
    [DockerFact]
    public void DeclareExchangeQueueAndBind_ShouldRouteMessageToQueue()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            var builder = new TopologyBuilder();
            string exchange = $"ex-{Guid.NewGuid():N}";
            string queue = $"q-{Guid.NewGuid():N}";
            const string routingKey = "integration.route";

            builder.DeclareExchange(channel, exchange, ExchangeType.Direct);
            builder.DeclareQueue(channel, queue);
            builder.BindQueue(channel, queue, exchange, routingKey);

            byte[] body = "topology-integration"u8.ToArray();
            channel.BasicPublish(exchange, routingKey, body: body);

            BasicGetResult? received = channel.BasicGet(queue, autoAck: true);
            received.Should().NotBeNull();
            received!.Body.ToArray().Should().Equal(body);
        }
    }

    [DockerFact]
    public void ConfigureTopologyForMessage_ShouldDeclareExchangeOnBroker()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            var builder = new TopologyBuilderOptions
            {
                AutoConfigureDeadLetter = false
            };
            var topologyBuilder = new TopologyBuilder(
                EndpointNameFormatter.Instance,
                RoutingKeyConvention.Instance,
                builder);

            topologyBuilder.ConfigureTopologyForMessage(channel, typeof(TestOrderEvent));

            string exchangeName = EndpointNameFormatter.Instance.FormatExchangeName(typeof(TestOrderEvent));
            Action act = () => channel.ExchangeDeclarePassive(exchangeName);
            act.Should().NotThrow();
        }
    }

    [DockerFact]
    public void PurgeQueue_ShouldRemoveMessages()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            var builder = new TopologyBuilder();
            string exchange = $"ex-{Guid.NewGuid():N}";
            string queue = $"q-{Guid.NewGuid():N}";

            builder.DeclareExchange(channel, exchange, ExchangeType.Fanout);
            builder.DeclareQueue(channel, queue);
            builder.BindQueue(channel, queue, exchange, string.Empty);
            channel.BasicPublish(exchange, string.Empty, body: "purge-me"u8.ToArray());

            uint purged = builder.PurgeQueue(channel, queue);

            purged.Should().BeGreaterThan(0);
            channel.QueueDeclarePassive(queue).MessageCount.Should().Be(0);
        }
    }

    [DockerFact]
    public void BindExchange_ShouldRouteFromSourceToDestination()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            var builder = new TopologyBuilder();
            string source = $"ex-src-{Guid.NewGuid():N}";
            string destination = $"ex-dest-{Guid.NewGuid():N}";
            string queue = $"q-{Guid.NewGuid():N}";
            const string routingKey = "bound.route";

            builder.DeclareExchange(channel, source, ExchangeType.Direct);
            builder.DeclareExchange(channel, destination, ExchangeType.Direct);
            builder.BindExchange(channel, destination, source, routingKey);
            builder.DeclareQueue(channel, queue);
            builder.BindQueue(channel, queue, destination, routingKey);

            channel.BasicPublish(source, routingKey, body: "exchange-bind"u8.ToArray());

            BasicGetResult? received = channel.BasicGet(queue, autoAck: true);
            received.Should().NotBeNull();
            received!.Body.ToArray().Should().Equal("exchange-bind"u8.ToArray());
        }
    }

    [DockerFact]
    public void ConfigureTopologyForConsumer_ShouldDeclareQueueWithDeadLetter()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            var options = new TopologyBuilderOptions { AutoConfigureDeadLetter = true };
            var builder = new TopologyBuilder(
                EndpointNameFormatter.Instance,
                RoutingKeyConvention.Instance,
                options);

            builder.ConfigureTopologyForConsumer(channel, typeof(TopologyIntegrationOrderConsumer));

            string queueName = EndpointNameFormatter.Instance.FormatQueueName(typeof(TopologyIntegrationOrderConsumer));
            Action act = () => channel.QueueDeclarePassive(queueName);
            act.Should().NotThrow();
        }
    }

    [DockerFact]
    public void DeleteQueueAndExchange_ShouldRemoveTopology()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            var builder = new TopologyBuilder();
            string exchange = $"ex-del-{Guid.NewGuid():N}";
            string queue = $"q-del-{Guid.NewGuid():N}";

            builder.DeclareExchange(channel, exchange, ExchangeType.Direct);
            builder.DeclareQueue(channel, queue);

            builder.DeleteQueue(channel, queue);
            builder.DeleteExchange(channel, exchange);

            Action queueAct = () => channel.QueueDeclarePassive(queue);
            Action exchangeAct = () => channel.ExchangeDeclarePassive(exchange);

            queueAct.Should().Throw<OperationInterruptedException>();
            exchangeAct.Should().Throw<OperationInterruptedException>();
        }
    }

    [DockerFact]
    public void UnbindQueue_ShouldStopRoutingMessages()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            var builder = new TopologyBuilder();
            string exchange = $"ex-unbind-{Guid.NewGuid():N}";
            string queue = $"q-unbind-{Guid.NewGuid():N}";
            const string routingKey = "unbind.route";

            builder.DeclareExchange(channel, exchange, ExchangeType.Direct);
            builder.DeclareQueue(channel, queue);
            builder.BindQueue(channel, queue, exchange, routingKey);
            builder.UnbindQueue(channel, queue, exchange, routingKey);

            channel.BasicPublish(exchange, routingKey, body: "should-not-arrive"u8.ToArray());

            BasicGetResult? received = channel.BasicGet(queue, autoAck: true);
            received.Should().BeNull();
        }
    }
}

internal sealed class TopologyIntegrationOrderConsumer : Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract.IMessageConsumer<TestOrderEvent>
{
    public Task ConsumeAsync(Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract.IConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
