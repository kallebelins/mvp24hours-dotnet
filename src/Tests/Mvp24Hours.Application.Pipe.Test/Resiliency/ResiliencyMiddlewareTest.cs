using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.Infrastructure.RateLimiting;
using Mvp24Hours.Infrastructure.Pipe.Resiliency;

namespace Mvp24Hours.Application.Pipe.Test.Resiliency;

[Trait("Category", "Unit")]
public class ResiliencyMiddlewareTest
{
    [Fact]
    public async Task InMemoryDeadLetterStore_Should_StoreAndQueryEntries()
    {
        var store = new InMemoryDeadLetterStore(maxItems: 10);
        var deadLetter = new DeadLetterOperation
        {
            OperationName = "TestOp",
            Reason = DeadLetterReason.Timeout,
            ErrorMessage = "timed out"
        };

        await store.StoreAsync(deadLetter);
        DeadLetterOperation? loaded = await store.GetByIdAsync(deadLetter.Id);
        long count = await store.GetCountAsync("TestOp", DeadLetterReason.Timeout);

        loaded.Should().NotBeNull();
        count.Should().Be(1);
        store.GetAllSync().Should().ContainSingle();
    }

    [Fact]
    public async Task InMemoryDeadLetterStore_Should_AcknowledgeAndPurge()
    {
        var store = new InMemoryDeadLetterStore();
        var deadLetter = new DeadLetterOperation { OperationName = "Op", Reason = DeadLetterReason.Unknown };
        await store.StoreAsync(deadLetter);

        bool acknowledged = await store.AcknowledgeAsync(deadLetter.Id, "tester");
        int purged = await store.PurgeAcknowledgedAsync(TimeSpan.Zero);

        acknowledged.Should().BeTrue();
        purged.Should().Be(1);
        store.Count.Should().Be(0);
    }

    [Fact]
    public async Task DeadLetterPipelineMiddleware_Should_StoreUnhandledException()
    {
        var store = new InMemoryDeadLetterStore();
        DeadLetterOperation? captured = null;
        var options = new DeadLetterOptions
        {
            DeadLetterOnAllExceptions = true,
            OnDeadLettered = dl => captured = dl
        };
        var middleware = new DeadLetterPipelineMiddleware(store, options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () => throw new InvalidOperationException("pipeline failed"));

        captured.Should().NotBeNull();
        captured!.Reason.Should().Be(DeadLetterReason.NonRetryableException);
        store.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeadLetterPipelineMiddleware_Should_DeadLetterFaultyMessage()
    {
        var store = new InMemoryDeadLetterStore();
        var middleware = new DeadLetterPipelineMiddleware(store, new DeadLetterOptions { DeadLetterOnFaulty = true });
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () =>
        {
            message.SetFailure();
            return Task.CompletedTask;
        });

        store.Count.Should().Be(1);
    }

    [Fact]
    public async Task DeadLetterPipelineMiddleware_Should_MapRetryExhaustedException()
    {
        var store = new InMemoryDeadLetterStore();
        var middleware = new DeadLetterPipelineMiddleware(store);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () => throw new RetryExhaustedException(3, new InvalidOperationException("inner")));

        (await store.GetAllAsync(reason: DeadLetterReason.MaxRetriesExceeded)).Should().ContainSingle();
    }

    [Fact]
    public async Task BulkheadPipelineMiddleware_Should_RejectWhenAtCapacity()
    {
        using var middleware = new BulkheadPipelineMiddleware(new BulkheadOptions
        {
            Key = "test-bulkhead",
            MaxConcurrency = 1,
            QueueLimit = 0
        });

        IPipelineMessage message = PipeTestHelpers.CreateMessage().WithCurrentOperation(new TestBulkheadOperation("test-bulkhead", 1, 0));
        var entered = new TaskCompletionSource();
        var release = new TaskCompletionSource();

        var first = Task.Run(async () => await middleware.ExecuteAsync(message, async () =>
            {
                entered.TrySetResult();
                await release.Task;
            }));

        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Func<Task> second = () => middleware.ExecuteAsync(message, () => Task.CompletedTask);

        await second.Should().ThrowAsync<PipelineBulkheadRejectedException>();
        release.TrySetResult();
        await first;
    }

    [Fact]
    public async Task RateLimitingPipelineMiddleware_Should_RejectWhenLimitExceeded()
    {
        using var provider = new NativeRateLimiterProvider();
        var options = new RateLimitingPipelineOptions
        {
            DefaultRateLimiterOptions = new NativeRateLimiterOptions
            {
                Algorithm = RateLimitingAlgorithm.FixedWindow,
                PermitLimit = 1,
                Window = TimeSpan.FromMinutes(1)
            }
        };
        using var middleware = new RateLimitingPipelineMiddleware(provider, options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage().WithCurrentOperation(new TestRateLimitedOperation());

        await middleware.ExecuteAsync(message, () => Task.CompletedTask);

        Func<Task> act = () => middleware.ExecuteAsync(message, () => Task.CompletedTask);

        await act.Should().ThrowAsync<RateLimitExceededException>();
    }
}
