using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Consumers;

[Trait("Category", "Unit")]
public class BatchProcessingHelperExtendedTest
{
    [Fact]
    public async Task ProcessWithRetryAsync_ShouldRetryUntilSuccess()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(1);
        int attempts = 0;

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessWithRetryAsync(
            context,
            (_, _) =>
            {
                attempts++;
                return Task.FromResult(attempts >= 2);
            },
            maxRetries: 3,
            retryDelay: TimeSpan.FromMilliseconds(1));

        results.AllSucceeded().Should().BeTrue();
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ProcessWithRetryAsync_WhenExhausted_ShouldNackWithoutRequeue()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(1);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessWithRetryAsync(
            context,
            (_, _) => Task.FromException<bool>(new InvalidOperationException("permanent")),
            maxRetries: 1,
            retryDelay: TimeSpan.FromMilliseconds(1));

        IBatchMessageResult result = results.Single();
        result.Success.Should().BeFalse();
        result.Requeue.Should().BeFalse();
        result.ErrorMessage.Should().Contain("permanent");
    }

    [Fact]
    public async Task ProcessAsTransactionAsync_WhenBatchFails_ShouldNackAll()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(2);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessAsTransactionAsync(
            context,
            (_, _) => Task.FromResult(false));

        results.Should().HaveCount(2);
        results.AllSucceeded().Should().BeFalse();
        results.Should().OnlyContain(r => r.Requeue);
    }

    [Fact]
    public async Task ProcessAsTransactionAsync_WhenExceptionThrown_ShouldNackAllWithMessage()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(2);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessAsTransactionAsync(
            context,
            (_, _) => Task.FromException<bool>(new InvalidOperationException("tx failed")));

        results.Should().HaveCount(2);
        results.Should().Contain(r => r.ErrorMessage == "tx failed");
    }

    [Fact]
    public async Task ProcessByGroupAsync_WhenGroupFails_ShouldNackGroupItems()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(2);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessByGroupAsync(
            context,
            item => item.Message.Name,
            (_, _, _) => Task.FromException<bool>(new InvalidOperationException("group fail")));

        results.Should().HaveCount(2);
        results.AnyFailed().Should().BeTrue();
    }

    [Fact]
    public async Task ProcessSequentiallyAsync_WhenProcessorReturnsFalse_ShouldNackItem()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(2);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessSequentiallyAsync(
            context,
            (item, _) => Task.FromResult(item.DeliveryTag == 1));

        results.Single(r => r.DeliveryTag == 1).Success.Should().BeTrue();
        results.Single(r => r.DeliveryTag == 2).Success.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessInParallelAsync_WithZeroParallelism_ShouldUseProcessorCount()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(4);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessInParallelAsync(
            context,
            (_, _) => Task.FromResult(true),
            maxDegreeOfParallelism: 0);

        results.Should().HaveCount(4);
        results.AllSucceeded().Should().BeTrue();
    }

    private static IBatchConsumeContext<TestOrderEvent> CreateBatchContext(int count)
    {
        var items = new List<IBatchMessageItem<TestOrderEvent>>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new BatchMessageItem<TestOrderEvent>(
                new TestOrderEvent { Name = $"item-{i}" },
                RabbitMQTestHelpers.CreateDeliverEventArgs(deliveryTag: (ulong)(i + 1))));
        }

        return new BatchConsumeContext<TestOrderEvent>(
            items,
            new ServiceCollection().BuildServiceProvider());
    }
}
