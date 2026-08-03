using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;

namespace Mvp24Hours.Application.RabbitMQ.Test.Consumers;

[Trait("Category", "Unit")]
public class BatchConsumerDefinitionExtendedTest
{
    [Fact]
    public void Constructor_ShouldDetectConsumerAndMessageTypes()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.ConsumerType.Should().Be(typeof(TestOrderBatchConsumer));
        definition.MessageType.Should().Be(typeof(TestOrderEvent));
        definition.IsBatchConsumer.Should().BeTrue();
        definition.BatchOptions.Should().NotBeNull();
    }

    [Fact]
    public void QueueAndRoute_ShouldConfigureEndpoint()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.QueueName.Should().Be("orders-batch");
        definition.Exchange.Should().Be("orders-exchange");
        definition.RoutingKey.Should().Be("orders.created");
    }

    [Fact]
    public void BatchSize_ShouldAutoConfigurePrefetchWhenNotExplicit()
    {
        PrefetchAutoBatchConsumerDefinition definition = new();

        definition.BatchOptions!.MaxBatchSize.Should().Be(40);
        definition.BatchOptions.MinBatchSize.Should().Be(3);
        definition.PrefetchCount.Should().Be(80);
        definition.BatchOptions.PrefetchCount.Should().Be(80);
    }

    [Fact]
    public void BatchTimeoutAndMessageWaitTimeout_ShouldConfigureOptions()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.BatchOptions!.BatchTimeout.Should().Be(TimeSpan.FromSeconds(5));
        definition.BatchOptions.MessageWaitTimeout.Should().Be(TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public void EnableParallelProcessing_ShouldConfigureParallelism()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.BatchOptions!.EnableParallelProcessing.Should().BeTrue();
        definition.BatchOptions.MaxDegreeOfParallelism.Should().Be(8);
    }

    [Fact]
    public void UseIndividualAcknowledgment_ShouldDisableBatchAck()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.BatchOptions!.UseBatchAcknowledgment.Should().BeFalse();
    }

    [Fact]
    public void PrefetchAndConcurrentAndRetry_ShouldConfigureConsumer()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.PrefetchCount.Should().Be(120);
        definition.ConcurrentConsumers.Should().Be(3);
        definition.MaxRetryCount.Should().Be(7);
        definition.BatchOptions!.MaxRetryAttempts.Should().Be(7);
    }

    [Fact]
    public void NoDeadLetter_ShouldDisableDeadLetterQueue()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.UseDeadLetterQueue.Should().BeFalse();
    }

    [Fact]
    public void RequeueOnFailure_ShouldConfigureBatchOptions()
    {
        ComprehensiveBatchConsumerDefinition definition = new();

        definition.BatchOptions!.RequeueOnFailure.Should().BeFalse();
    }

    [Fact]
    public void UseHighThroughputMode_ShouldApplyPreset()
    {
        HighThroughputBatchConsumerDefinition definition = new();

        definition.BatchOptions.Should().BeEquivalentTo(BatchConsumerOptions.HighThroughput);
        definition.PrefetchCount.Should().Be(BatchConsumerOptions.HighThroughput.PrefetchCount);
    }

    [Fact]
    public void UseLowLatencyMode_ShouldApplyPreset()
    {
        LowLatencyBatchConsumerDefinition definition = new();

        definition.BatchOptions.Should().BeEquivalentTo(BatchConsumerOptions.LowLatency);
        definition.PrefetchCount.Should().Be(BatchConsumerOptions.LowLatency.PrefetchCount);
    }

    [Fact]
    public void ConfigureBatchOptions_ShouldApplyCustomConfiguration()
    {
        CustomBatchConsumerDefinition definition = new();

        definition.BatchOptions!.MaxBatchSize.Should().Be(99);
        definition.BatchOptions.MinBatchSize.Should().Be(11);
    }

    [Fact]
    public void TestOrderBatchConsumerDefinition_ShouldExposeBatchConfiguration()
    {
        TestOrderBatchConsumerDefinition definition = new();

        definition.BatchOptions!.MaxBatchSize.Should().Be(25);
        definition.BatchOptions.MinBatchSize.Should().Be(2);
        definition.BatchOptions.EnableParallelProcessing.Should().BeTrue();
    }

    private sealed class ComprehensiveBatchConsumerDefinition : BatchConsumerDefinition<TestOrderBatchConsumer>
    {
        public ComprehensiveBatchConsumerDefinition()
        {
            Queue("orders-batch");
            ExchangeName("orders-exchange");
            Route("orders.created");
            BatchSize(maxSize: 50, minSize: 5);
            BatchTimeout(TimeSpan.FromSeconds(5));
            MessageWaitTimeout(TimeSpan.FromMilliseconds(250));
            EnableParallelProcessing(maxDegree: 8);
            UseIndividualAcknowledgment();
            Prefetch(120);
            Concurrent(3);
            Retry(7);
            NoDeadLetter();
            RequeueOnFailure(requeue: false);
        }
    }

    private sealed class PrefetchAutoBatchConsumerDefinition : BatchConsumerDefinition<TestOrderBatchConsumer>
    {
        public PrefetchAutoBatchConsumerDefinition()
        {
            BatchSize(maxSize: 40, minSize: 3);
        }
    }

    private sealed class HighThroughputBatchConsumerDefinition : BatchConsumerDefinition<TestOrderBatchConsumer>
    {
        public HighThroughputBatchConsumerDefinition()
        {
            UseHighThroughputMode();
        }
    }

    private sealed class LowLatencyBatchConsumerDefinition : BatchConsumerDefinition<TestOrderBatchConsumer>
    {
        public LowLatencyBatchConsumerDefinition()
        {
            UseLowLatencyMode();
        }
    }

    private sealed class CustomBatchConsumerDefinition : BatchConsumerDefinition<TestOrderBatchConsumer>
    {
        public CustomBatchConsumerDefinition()
        {
            ConfigureBatchOptions(options =>
            {
                options.MaxBatchSize = 99;
                options.MinBatchSize = 11;
            });
        }
    }
}
