using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;

public sealed class TestOrderBatchConsumer : IBatchConsumer<TestOrderEvent>
{
    public static int ProcessedCount { get; private set; }

    public static void Reset()
    {
        ProcessedCount = 0;
    }

    public Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(
        IBatchConsumeContext<TestOrderEvent> context,
        CancellationToken cancellationToken = default)
    {
        ProcessedCount += context.Messages.Count();

        IEnumerable<IBatchMessageResult> results = context.Messages
            .Select(item => BatchMessageResult.Ack(item.DeliveryTag));

        return Task.FromResult<IEnumerable<IBatchMessageResult>?>(results);
    }
}
