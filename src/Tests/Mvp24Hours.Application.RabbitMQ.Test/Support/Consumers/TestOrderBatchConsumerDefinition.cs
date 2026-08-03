using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;

public sealed class TestOrderBatchConsumerDefinition : BatchConsumerDefinition<TestOrderBatchConsumer>
{
    public TestOrderBatchConsumerDefinition()
    {
        BatchOptions!.MaxBatchSize = 25;
        BatchOptions.MinBatchSize = 2;
        BatchOptions.BatchTimeout = TimeSpan.FromSeconds(3);
        BatchOptions.EnableParallelProcessing = true;
        BatchOptions.MaxDegreeOfParallelism = 4;
        BatchOptions.PrefetchCount = 25;
    }
}
