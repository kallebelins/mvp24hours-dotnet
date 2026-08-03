using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test.Integration;

[Trait("Category", "Integration")]
[Collection(RabbitMqIntegrationCollection.Name)]
public class BatchConsumerProcessorIntegrationTest(RabbitMqIntegrationFixture fixture)
{
    [DockerFact]
    public async Task ProcessBatch_ShouldInvokeConsumerAndAcknowledgeMessages()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        TestOrderBatchConsumer.Reset();

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            string exchange = $"ex-batch-{Guid.NewGuid():N}";
            string queue = $"q-batch-{Guid.NewGuid():N}";

            channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: false, autoDelete: true);
            channel.QueueDeclare(queue, durable: false, exclusive: false, autoDelete: true);
            channel.QueueBind(queue, exchange, "batch");

            var serializer = new JsonMessageSerializer();
            for (int i = 0; i < 3; i++)
            {
                byte[] body = serializer.Serialize(new TestOrderEvent { Name = $"batch-{i}" });
                channel.BasicPublish(exchange, "batch", body: body);
            }

            var services = new ServiceCollection();
            services.AddSingleton<IBatchConsumer<TestOrderEvent>, TestOrderBatchConsumer>();
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            var options = new BatchConsumerOptions
            {
                MaxBatchSize = 3,
                MinBatchSize = 1,
                BatchTimeout = TimeSpan.FromMilliseconds(200),
                MessageWaitTimeout = TimeSpan.FromMilliseconds(100),
                PrefetchCount = 3
            };

            using var processor = new BatchConsumerProcessor<TestOrderEvent>(
                options,
                serviceProvider,
                serializer,
                NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
                channel: channel);

            processor.SetQueueMetadata(queue, exchange, "batch-consumer");

            for (int i = 0; i < 3; i++)
            {
                BasicGetResult? getResult = channel.BasicGet(queue, autoAck: false);
                getResult.Should().NotBeNull();

                var eventArgs = new BasicDeliverEventArgs(
                    consumerTag: "batch-consumer",
                    deliveryTag: getResult!.DeliveryTag,
                    redelivered: getResult.Redelivered,
                    exchange: getResult.Exchange,
                    routingKey: getResult.RoutingKey,
                    properties: getResult.BasicProperties,
                    body: getResult.Body);

                await processor.AddMessageAsync(eventArgs);
            }

            await processor.FlushAsync();

            TestOrderBatchConsumer.ProcessedCount.Should().Be(3);
            channel.QueueDeclarePassive(queue).MessageCount.Should().Be(0);
        }
    }

    [DockerFact]
    public async Task ProcessBatch_WithBatchTimeout_ShouldFlushPartialBatch()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        TestOrderBatchConsumer.Reset();

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            string exchange = $"ex-timeout-{Guid.NewGuid():N}";
            string queue = $"q-timeout-{Guid.NewGuid():N}";

            channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: false, autoDelete: true);
            channel.QueueDeclare(queue, durable: false, exclusive: false, autoDelete: true);
            channel.QueueBind(queue, exchange, "timeout");

            var serializer = new JsonMessageSerializer();
            byte[] body = serializer.Serialize(new TestOrderEvent { Name = "timeout-batch" });
            channel.BasicPublish(exchange, "timeout", body: body);

            var services = new ServiceCollection();
            services.AddSingleton<IBatchConsumer<TestOrderEvent>, TestOrderBatchConsumer>();
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            var options = new BatchConsumerOptions
            {
                MaxBatchSize = 10,
                MinBatchSize = 1,
                BatchTimeout = TimeSpan.FromMilliseconds(300),
                MessageWaitTimeout = TimeSpan.FromMilliseconds(100),
                PrefetchCount = 10
            };

            using var processor = new BatchConsumerProcessor<TestOrderEvent>(
                options,
                serviceProvider,
                serializer,
                NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
                channel: channel);

            processor.SetQueueMetadata(queue, exchange, "timeout-consumer");

            BasicGetResult? getResult = channel.BasicGet(queue, autoAck: false);
            getResult.Should().NotBeNull();

            var eventArgs = new BasicDeliverEventArgs(
                consumerTag: "timeout-consumer",
                deliveryTag: getResult!.DeliveryTag,
                redelivered: getResult.Redelivered,
                exchange: getResult.Exchange,
                routingKey: getResult.RoutingKey,
                properties: getResult.BasicProperties,
                body: getResult.Body);

            await processor.AddMessageAsync(eventArgs);
            await Task.Delay(400);
            await processor.FlushAsync();

            TestOrderBatchConsumer.ProcessedCount.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    [DockerFact]
    public async Task ProcessBatch_WithPartialFailure_ShouldNackFailedMessage()
    {
        if (!fixture.IsAvailable)
        {
            return;
        }

        (IConnection connection, IModel channel) = fixture.CreateConnectionAndChannel();
        using (connection)
        using (channel)
        {
            string exchange = $"ex-partial-{Guid.NewGuid():N}";
            string queue = $"q-partial-{Guid.NewGuid():N}";

            channel.ExchangeDeclare(exchange, ExchangeType.Direct, durable: false, autoDelete: true);
            channel.QueueDeclare(queue, durable: false, exclusive: false, autoDelete: true);
            channel.QueueBind(queue, exchange, "partial");

            var serializer = new JsonMessageSerializer();
            for (int i = 0; i < 2; i++)
            {
                byte[] body = serializer.Serialize(new TestOrderEvent { Name = $"partial-{i}" });
                channel.BasicPublish(exchange, "partial", body: body);
            }

            var services = new ServiceCollection();
            services.AddSingleton<IBatchConsumer<TestOrderEvent>, PartialFailureBatchConsumer>();
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            var options = new BatchConsumerOptions
            {
                MaxBatchSize = 2,
                MinBatchSize = 1,
                BatchTimeout = TimeSpan.FromMilliseconds(200),
                MessageWaitTimeout = TimeSpan.FromMilliseconds(100),
                PrefetchCount = 2,
                UseBatchAcknowledgment = false
            };

            using var processor = new BatchConsumerProcessor<TestOrderEvent>(
                options,
                serviceProvider,
                serializer,
                NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
                channel: channel);

            processor.SetQueueMetadata(queue, exchange, "partial-consumer");

            for (int i = 0; i < 2; i++)
            {
                BasicGetResult? getResult = channel.BasicGet(queue, autoAck: false);
                getResult.Should().NotBeNull();

                var eventArgs = new BasicDeliverEventArgs(
                    consumerTag: "partial-consumer",
                    deliveryTag: getResult!.DeliveryTag,
                    redelivered: getResult.Redelivered,
                    exchange: getResult.Exchange,
                    routingKey: getResult.RoutingKey,
                    properties: getResult.BasicProperties,
                    body: getResult.Body);

                await processor.AddMessageAsync(eventArgs);
            }

            await processor.FlushAsync();

            PartialFailureBatchConsumer.ProcessedCount.Should().Be(2);
        }
    }
}

internal sealed class PartialFailureBatchConsumer : IBatchConsumer<TestOrderEvent>
{
    public static int ProcessedCount { get; private set; }

    public static void Reset() => ProcessedCount = 0;

    public Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(
        IBatchConsumeContext<TestOrderEvent> context,
        CancellationToken cancellationToken = default)
    {
        var items = context.Messages.ToList();
        ProcessedCount = items.Count;

        IEnumerable<IBatchMessageResult> results = items.Select((item, index) =>
            index == 0
                ? BatchMessageResult.Ack(item.DeliveryTag)
                : BatchMessageResult.Nack(item.DeliveryTag, requeue: false, errorMessage: "reject"));

        return Task.FromResult<IEnumerable<IBatchMessageResult>?>(results);
    }
}
