//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using Mvp24Hours.Infrastructure.Pipe.Operations.Parallel;
using System.Collections.Concurrent;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Operations.Parallel;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class ParallelOperationGroupTest
{
    [Fact, Priority(1)]
    public void ParallelOperationGroup_ShouldExecuteAllOperations()
    {
        var executed = new ConcurrentBag<string>();

        var group = new ParallelOperationGroup([
            new TrackOp("op1", executed),
            new TrackOp("op2", executed),
            new TrackOp("op3", executed)
        ]);

        var pipeline = new Pipeline();
        pipeline.Add(group);
        pipeline.Execute();

        executed.Should().HaveCount(3);
        executed.Should().Contain("op1");
        executed.Should().Contain("op2");
        executed.Should().Contain("op3");
    }

    [Fact, Priority(2)]
    public void ParallelOperationGroup_RequireAllSuccess_WhenOneFails_ShouldThrowAggregate()
    {
        var group = new ParallelOperationGroup([
            new TrackOp("op1", new()),
            new ThrowOp("fail!")
        ], requireAllSuccess: true);

        IPipelineMessage msg = new PipelineMessage();
        Action act = () => group.Execute(msg);

        act.Should().Throw<AggregateException>();
    }

    [Fact, Priority(3)]
    public void ParallelOperationGroup_NotRequireAllSuccess_WhenOneFails_ShouldStoreExceptions()
    {
        var group = new ParallelOperationGroup([
            new TrackOp("op1", new()),
            new ThrowOp("partial fail")
        ], requireAllSuccess: false);

        var pipeline = new Pipeline();
        pipeline.Add(group);
        pipeline.Execute();

        var exceptions = pipeline.GetMessage().GetContent<List<Exception>>("ParallelOperationExceptions");
        exceptions.Should().NotBeNull();
        exceptions.Should().HaveCount(1);
    }

    [Fact, Priority(4)]
    public void ParallelOperationGroup_NullOperations_ShouldThrow()
    {
        Action act = () => _ = new ParallelOperationGroup(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(5)]
    public void ParallelOperationGroup_MaxDegreeOfParallelism_ShouldBeSet()
    {
        var group = new ParallelOperationGroup([new TrackOp("op1", new())], maxDegreeOfParallelism: 2);

        group.MaxDegreeOfParallelism.Should().Be(2);
    }

    [Fact, Priority(6)]
    public void ParallelOperationGroup_Operations_ShouldBeAccessible()
    {
        var ops = new IOperation[] { new TrackOp("a", new()), new TrackOp("b", new()) };
        var group = new ParallelOperationGroup(ops);

        group.Operations.Should().HaveCount(2);
    }

    [Fact, Priority(7)]
    public void ParallelOperationGroup_Rollback_ShouldCallRollbackOnAllOperations()
    {
        var rollbackTracked = new ConcurrentBag<string>();
        var group = new ParallelOperationGroup([
            new RollbackTrackOp("r1", rollbackTracked),
            new RollbackTrackOp("r2", rollbackTracked)
        ]);

        IPipelineMessage msg = new PipelineMessage();
        group.Rollback(msg);

        rollbackTracked.Should().Contain("r1");
        rollbackTracked.Should().Contain("r2");
    }

    [Fact, Priority(8)]
    public async Task ParallelOperationGroupAsync_ShouldExecuteAllAsync()
    {
        var executed = new ConcurrentBag<string>();

        var group = new ParallelOperationGroupAsync([
            new AsyncTrackOp("a1", executed),
            new AsyncTrackOp("a2", executed),
            new AsyncTrackOp("a3", executed)
        ]);

        var pipeline = new PipelineAsync();
        pipeline.Add(group);
        await pipeline.ExecuteAsync();

        executed.Should().HaveCount(3);
        executed.Should().Contain("a1");
        executed.Should().Contain("a2");
        executed.Should().Contain("a3");
    }

    [Fact, Priority(9)]
    public async Task ParallelOperationGroupAsync_RequireAllSuccess_WhenOneFails_ShouldThrow()
    {
        var group = new ParallelOperationGroupAsync([
            new AsyncTrackOp("ok", new()),
            new AsyncThrowOp("async fail")
        ], requireAllSuccess: true);

        IPipelineMessage msg = new PipelineMessage();
        Func<Task> act = () => group.ExecuteAsync(msg);

        // async version rethrows inner exception (await unwraps AggregateException)
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact, Priority(10)]
    public async Task ParallelOperationGroupAsync_NotRequireAllSuccess_ShouldStoreExceptions()
    {
        var group = new ParallelOperationGroupAsync([
            new AsyncTrackOp("ok", new()),
            new AsyncThrowOp("soft fail")
        ], requireAllSuccess: false);

        var pipeline = new PipelineAsync();
        pipeline.Add(group);
        await pipeline.ExecuteAsync();

        var exceptions = pipeline.GetMessage().GetContent<List<Exception>>("ParallelOperationExceptions");
        exceptions.Should().NotBeNull();
        exceptions.Should().HaveCount(1);
    }

    [Fact, Priority(11)]
    public async Task ParallelOperationGroupAsync_MaxDegreeOfParallelism_ShouldLimit()
    {
        var executed = new ConcurrentBag<string>();
        var group = new ParallelOperationGroupAsync([
            new AsyncTrackOp("x1", executed),
            new AsyncTrackOp("x2", executed)
        ], maxDegreeOfParallelism: 1);

        await group.ExecuteAsync(new PipelineMessage());

        executed.Should().HaveCount(2);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private class TrackOp(string name, ConcurrentBag<string> log) : OperationBase
    {
        public override void Execute(IPipelineMessage input) => log.Add(name);
    }

    private class ThrowOp(string msg) : OperationBase
    {
        public override void Execute(IPipelineMessage input) => throw new InvalidOperationException(msg);
    }

    private class RollbackTrackOp(string name, ConcurrentBag<string> log) : OperationBase
    {
        public override void Execute(IPipelineMessage input) { }
        public override void Rollback(IPipelineMessage input) => log.Add(name);
    }

    private class AsyncTrackOp(string name, ConcurrentBag<string> log) : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            log.Add(name);
            return Task.CompletedTask;
        }
    }

    private class AsyncThrowOp(string msg) : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input) => throw new InvalidOperationException(msg);
    }
}
