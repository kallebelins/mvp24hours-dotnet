using Mvp24Hours.Infrastructure.Pipe.Observability;

namespace Mvp24Hours.Application.Pipe.Test.Observability;

[Trait("Category", "Unit")]
public class PipelineObserverManagerTest
{
    [Fact]
    public void Register_WithNullObserver_ShouldThrow()
    {
        var manager = new PipelineObserverManager();

        Action act = () => manager.Register((IPipelineObserver)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_ShouldIgnoreDuplicateObservers()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserver();

        manager.Register(observer);
        manager.Register(observer);

        observer.PipelineStartCount.Should().Be(0);
    }

    [Fact]
    public void Unregister_WithNullObserver_ShouldNotThrow()
    {
        var manager = new PipelineObserverManager();

        Action act = () => manager.Unregister((IPipelineObserver)null!);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task NotifyPipelineStartAsync_ShouldInvokeAsyncAndSyncObservers()
    {
        var manager = new PipelineObserverManager();
        var asyncObserver = new TestPipelineObserver();
        var syncObserver = new TestPipelineObserverSync();
        manager.Register(asyncObserver);
        manager.Register(syncObserver);

        await manager.NotifyPipelineStartAsync(new PipelineStartEventArgs
        {
            PipelineId = "pipe-1",
            PipelineName = "TestPipeline",
            OperationCount = 2
        });

        asyncObserver.PipelineStartCount.Should().Be(1);
        syncObserver.PipelineStartCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyPipelineCompleteAsync_ShouldInvokeObservers()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserver();
        manager.Register(observer);

        await manager.NotifyPipelineCompleteAsync(new PipelineCompleteEventArgs
        {
            PipelineId = "pipe-1",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(10)
        });

        observer.PipelineCompleteCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyOperationStartAsync_ShouldInvokeObservers()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserver();
        manager.Register(observer);

        await manager.NotifyOperationStartAsync(new OperationStartEventArgs
        {
            PipelineId = "pipe-1",
            OperationName = "Step1",
            OperationIndex = 0
        });

        observer.OperationStartCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyOperationEndAsync_ShouldInvokeObservers()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserver();
        manager.Register(observer);

        await manager.NotifyOperationEndAsync(new OperationEndEventArgs
        {
            PipelineId = "pipe-1",
            OperationName = "Step1",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(5)
        });

        observer.OperationEndCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyOperationFailureAsync_ShouldInvokeObservers()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserver();
        manager.Register(observer);

        await manager.NotifyOperationFailureAsync(new OperationFailureEventArgs
        {
            PipelineId = "pipe-1",
            OperationName = "Step1",
            Exception = new InvalidOperationException("failed")
        });

        observer.OperationFailureCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyRollbackStartAsync_ShouldInvokeObservers()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserver();
        manager.Register(observer);

        await manager.NotifyRollbackStartAsync(new RollbackEventArgs
        {
            PipelineId = "pipe-1",
            OperationName = "Step1",
            TotalOperationsToRollback = 1
        });

        observer.RollbackStartCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyRollbackCompleteAsync_ShouldInvokeObservers()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserver();
        manager.Register(observer);

        await manager.NotifyRollbackCompleteAsync(new RollbackEventArgs
        {
            PipelineId = "pipe-1",
            OperationName = "Step1",
            Success = true
        });

        observer.RollbackCompleteCount.Should().Be(1);
    }

    [Fact]
    public async Task NotifyPipelineStartAsync_WhenObserverThrows_ShouldNotPropagate()
    {
        var manager = new PipelineObserverManager();
        manager.Register(new ThrowingPipelineObserver());
        manager.Register(new TestPipelineObserverSync());

        Func<Task> act = () => manager.NotifyPipelineStartAsync(new PipelineStartEventArgs { PipelineId = "pipe-1" });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Unregister_ShouldRemoveRegisteredObserver()
    {
        var manager = new PipelineObserverManager();
        var observer = new TestPipelineObserverSync();
        manager.Register(observer);

        manager.Unregister(observer);

        await manager.NotifyPipelineStartAsync(new PipelineStartEventArgs { PipelineId = "pipe-1" });

        observer.PipelineStartCount.Should().Be(0);
    }

    private sealed class TestPipelineObserver : IPipelineObserver
    {
        public int PipelineStartCount { get; private set; }
        public int PipelineCompleteCount { get; private set; }
        public int OperationStartCount { get; private set; }
        public int OperationEndCount { get; private set; }
        public int OperationFailureCount { get; private set; }
        public int RollbackStartCount { get; private set; }
        public int RollbackCompleteCount { get; private set; }

        public Task OnPipelineStartAsync(PipelineStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            PipelineStartCount++;
            return Task.CompletedTask;
        }

        public Task OnPipelineCompleteAsync(PipelineCompleteEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            PipelineCompleteCount++;
            return Task.CompletedTask;
        }

        public Task OnOperationStartAsync(OperationStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            OperationStartCount++;
            return Task.CompletedTask;
        }

        public Task OnOperationEndAsync(OperationEndEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            OperationEndCount++;
            return Task.CompletedTask;
        }

        public Task OnOperationFailureAsync(OperationFailureEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            OperationFailureCount++;
            return Task.CompletedTask;
        }

        public Task OnRollbackStartAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            RollbackStartCount++;
            return Task.CompletedTask;
        }

        public Task OnRollbackCompleteAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            RollbackCompleteCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestPipelineObserverSync : IPipelineObserverSync
    {
        public int PipelineStartCount { get; private set; }

        public void OnPipelineStart(PipelineStartEventArgs eventArgs)
        {
            PipelineStartCount++;
        }

        public void OnPipelineComplete(PipelineCompleteEventArgs eventArgs) { }
        public void OnOperationStart(OperationStartEventArgs eventArgs) { }
        public void OnOperationEnd(OperationEndEventArgs eventArgs) { }
        public void OnOperationFailure(OperationFailureEventArgs eventArgs) { }
        public void OnRollbackStart(RollbackEventArgs eventArgs) { }
        public void OnRollbackComplete(RollbackEventArgs eventArgs) { }
    }

    private sealed class ThrowingPipelineObserver : IPipelineObserver
    {
        public Task OnPipelineStartAsync(PipelineStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("observer failure");
        }

        public Task OnPipelineCompleteAsync(PipelineCompleteEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task OnOperationStartAsync(OperationStartEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task OnOperationEndAsync(OperationEndEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task OnOperationFailureAsync(OperationFailureEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task OnRollbackStartAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task OnRollbackCompleteAsync(RollbackEventArgs eventArgs, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
