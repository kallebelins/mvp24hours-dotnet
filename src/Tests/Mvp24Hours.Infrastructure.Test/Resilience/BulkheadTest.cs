//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Implementations;

namespace Mvp24Hours.Infrastructure.Test.Resilience;

[Trait("Category", "Unit")]
public class BulkheadTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidMaxConcurrency_ShouldThrow(int maxConcurrency)
    {
        Action act = () => _ = new Bulkhead<int>(maxConcurrency);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxConcurrency");
    }

    [Fact]
    public void Constructor_ShouldExposeMaxConcurrency()
    {
        var bulkhead = new Bulkhead<string>(3);

        bulkhead.MaxConcurrency.Should().Be(3);
        bulkhead.CurrentConcurrency.Should().Be(0);
        bulkhead.QueuedOperations.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        var bulkhead = new Bulkhead<int>(1);

        Func<Task> act = () => bulkhead.ExecuteAsync(
            (Func<object?, CancellationToken, Task<int>>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteAsync_WhenSlotAvailable_ShouldExecuteAndReturnResult()
    {
        var bulkhead = new Bulkhead<string>(2);
        object? receivedContext = null;

        string result = await bulkhead.ExecuteAsync(
            (ctx, _) =>
            {
                receivedContext = ctx;
                return Task.FromResult("ok");
            },
            context: "ctx");

        result.Should().Be("ok");
        receivedContext.Should().Be("ctx");
        bulkhead.CurrentConcurrency.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLimitConcurrencyAndQueueWaitingOperations()
    {
        var bulkhead = new Bulkhead<int>(maxConcurrency: 1);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int started = 0;

        Task<int> first = bulkhead.ExecuteAsync(async _ =>
        {
            Interlocked.Increment(ref started);
            await gate.Task;
            return 1;
        });

        await WaitUntilAsync(() => started == 1);
        bulkhead.CurrentConcurrency.Should().Be(1);

        bool secondStarted = false;
        Task<int> second = bulkhead.ExecuteAsync(_ =>
        {
            secondStarted = true;
            return Task.FromResult(2);
        });

        await WaitUntilAsync(() => bulkhead.QueuedOperations == 1);
        secondStarted.Should().BeFalse();
        bulkhead.QueuedOperations.Should().Be(1);

        gate.SetResult();

        int[] results = await Task.WhenAll(first, second);
        results.Should().BeEquivalentTo([1, 2]);
        secondStarted.Should().BeTrue();
        bulkhead.CurrentConcurrency.Should().Be(0);
        bulkhead.QueuedOperations.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationThrows_ShouldReleaseSlot()
    {
        var bulkhead = new Bulkhead<int>(1);

        Func<Task> failing = () => bulkhead.ExecuteAsync(
            (Func<CancellationToken, Task<int>>)(_ => throw new InvalidOperationException("boom")));

        await failing.Should().ThrowAsync<InvalidOperationException>();
        bulkhead.CurrentConcurrency.Should().Be(0);

        int result = await bulkhead.ExecuteAsync(_ => Task.FromResult(42));
        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationWhileQueued_ShouldPropagate()
    {
        var bulkhead = new Bulkhead<int>(1);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();

        Task<int> first = bulkhead.ExecuteAsync(async ct =>
        {
            await gate.Task;
            ct.ThrowIfCancellationRequested();
            return 1;
        });

        await WaitUntilAsync(() => bulkhead.CurrentConcurrency == 1);

        Task<int> queued = bulkhead.ExecuteAsync(_ => Task.FromResult(2), cts.Token);
        await WaitUntilAsync(() => bulkhead.QueuedOperations == 1);

        await cts.CancelAsync();

        Func<Task> act = async () => await queued;
        await act.Should().ThrowAsync<OperationCanceledException>();

        gate.SetResult();
        (await first).Should().Be(1);
    }

    [Fact]
    public async Task VoidBulkhead_ExecuteAsync_ShouldDelegateToInner()
    {
        var bulkhead = new Bulkhead(2);
        bool executed = false;

        await bulkhead.ExecuteAsync(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
        bulkhead.MaxConcurrency.Should().Be(2);
        bulkhead.CurrentConcurrency.Should().Be(0);
    }

    [Fact]
    public async Task VoidBulkhead_ExecuteAsync_WithContext_ShouldPassContext()
    {
        var bulkhead = new Bulkhead(1);
        object? received = null;

        await bulkhead.ExecuteAsync(
            (ctx, _) =>
            {
                received = ctx;
                return Task.CompletedTask;
            },
            context: 99);

        received.Should().Be(99);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
    {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition was not met within timeout.");
            }

            await Task.Delay(10);
        }
    }
}
