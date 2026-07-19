using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.Test.Support;
using Mvp24Hours.Infrastructure.Caching.Warming;

namespace Mvp24Hours.Infrastructure.Caching.Test.Warming;

[Trait("Category", "Unit")]
public class CacheWarmerTest
{
    [Fact]
    public async Task WarmUpAsync_ShouldExecuteOperationsByPriorityThenName()
    {
        var executionOrder = new List<string>();
        ICacheWarmupOperation[] operations =
        [
            new TestWarmupOperation("Beta", 2, executionOrder),
            new TestWarmupOperation("Alpha", 1, executionOrder),
            new TestWarmupOperation("Gamma", 1, executionOrder)
        ];
        var warmer = new CacheWarmer(operations);

        await warmer.WarmUpAsync();

        executionOrder.Should().ContainInOrder("Alpha", "Gamma", "Beta");
    }

    [Fact]
    public async Task WarmUpAsync_OperationFailure_ShouldContinueWithOthers()
    {
        var executed = new List<string>();
        ICacheWarmupOperation[] operations =
        [
            new FailingWarmupOperation("Fail", executed),
            new TestWarmupOperation("Success", 1, executed)
        ];
        var warmer = new CacheWarmer(operations);

        await warmer.WarmUpAsync();

        executed.Should().ContainInOrder("Fail", "Success");
    }

    [Fact]
    public async Task WarmUpAsync_NoOperations_ShouldComplete()
    {
        var warmer = new CacheWarmer([]);

        Func<Task> act = async () => await warmer.WarmUpAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WarmUpAsync_CancellationRequested_ShouldStop()
    {
        using var cts = new CancellationTokenSource();
        ICacheWarmupOperation[] operations =
        [
            new TestWarmupOperation("First", 1, []),
            new BlockingWarmupOperation("Second", cts)
        ];
        var warmer = new CacheWarmer(operations);
        await cts.CancelAsync();

        await warmer.WarmUpAsync(cts.Token);
    }

    private sealed class TestWarmupOperation(string name, int priority, List<string> executionOrder) : ICacheWarmupOperation
    {
        public string Name { get; } = name;

        public int Priority { get; } = priority;

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            executionOrder.Add(Name);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingWarmupOperation(string name, List<string> executionOrder) : ICacheWarmupOperation
    {
        public string Name { get; } = name;

        public int Priority => 0;

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            executionOrder.Add(Name);
            throw new InvalidOperationException("warmup failed");
        }
    }

    private sealed class BlockingWarmupOperation(string name, CancellationTokenSource cts) : ICacheWarmupOperation
    {
        public string Name { get; } = name;

        public int Priority => 2;

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            cts.Cancel();
            return Task.CompletedTask;
        }
    }
}

[Trait("Category", "Unit")]
public class CacheWarmupHostedServiceTest
{
    [Fact]
    public async Task StartAsync_ShouldInvokeWarmer()
    {
        var warmer = new Mock<ICacheWarmer>();
        var service = new CacheWarmupHostedService(warmer.Object);

        await service.StartAsync(CancellationToken.None);

        warmer.Verify(x => x.WarmUpAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WarmerThrows_ShouldNotThrow()
    {
        var warmer = new Mock<ICacheWarmer>();
        warmer.Setup(x => x.WarmUpAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("startup warmup failed"));
        var service = new CacheWarmupHostedService(warmer.Object);

        Func<Task> act = async () => await service.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldComplete()
    {
        var service = new CacheWarmupHostedService(new Mock<ICacheWarmer>().Object);

        await service.StopAsync(CancellationToken.None);
    }
}
