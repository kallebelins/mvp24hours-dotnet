using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Priority;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.AdvancedFlow.Priority;

[Collection("PipeTestCollection")]
[Trait("Category", "Unit")]
public class PriorityPipelineTest
{
    [Fact]
    public void Execute_Should_RunOperationsInPriorityOrder()
    {
        TrackingOperation.ExecutionOrder.Clear();
        PriorityPipeline pipeline = new PriorityPipeline()
            .Add(new TrackingOperation("low"), PriorityLevel.Low)
            .Add(new TrackingOperation("critical"), PriorityLevel.Critical)
            .Add(new TrackingOperation("normal"), PriorityLevel.Normal);

        pipeline.Execute(PipeTestHelpers.CreateMessage());

        TrackingOperation.ExecutionOrder.Should().Equal("critical", "normal", "low");
    }

    [Fact]
    public void GetOperationsInOrder_Should_ReturnSortedOperations()
    {
        PriorityPipeline pipeline = new PriorityPipeline()
            .Add(new TrackingOperation("a"), 10)
            .Add(new TrackingOperation("b"), 100);

        var order = pipeline.GetOperationsInOrder().Select(x => x.Priority).ToList();

        order.Should().Equal(100, 10);
    }

    [Fact]
    public void OperationPriorityHelper_Should_ReadAttributeAndInterface()
    {
        OperationPriorityHelper.GetPriority(new AttributedOperation()).Should().Be((int)PriorityLevel.High);
        OperationPriorityHelper.GetPriority(new PrioritizedWrapperOperation()).Should().Be(999);
        OperationPriorityHelper.GetGroup(new PrioritizedWrapperOperation()).Should().Be("billing");
    }

    [Fact]
    public void OperationPriorityComparer_Should_SortDescending()
    {
        IPrioritizedOperation high = new PrioritizedOperation<object>(new object(), PriorityLevel.High);
        IPrioritizedOperation low = new PrioritizedOperation<object>(new object(), PriorityLevel.Low);

        OperationPriorityComparer.Instance.Compare(high, low).Should().BeLessThan(0);
    }

    [Fact]
    public void Execute_Should_BreakOnFailWhenConfigured()
    {
        TrackingOperation.ExecutionOrder.Clear();
        PriorityPipeline pipeline = new PriorityPipeline { IsBreakOnFail = true }
            .Add(new FaultyOperation(), PriorityLevel.Critical)
            .Add(new TrackingOperation("skipped"), PriorityLevel.Normal);

        pipeline.Execute(PipeTestHelpers.CreateMessage());

        TrackingOperation.ExecutionOrder.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_MixSyncAndAsyncByPriority()
    {
        TrackingOperation.ExecutionOrder.Clear();
        PriorityPipeline pipeline = new PriorityPipeline()
            .Add(new TrackingOperation("sync-low"), PriorityLevel.Low)
            .AddAsync(new AsyncTrackingOperation("async-high"), PriorityLevel.High);

        await pipeline.ExecuteAsync(PipeTestHelpers.CreateMessage());

        TrackingOperation.ExecutionOrder.Should().Equal("async-high", "sync-low");
    }

    [OperationPriority(PriorityLevel.High, Group = "validation")]
    private sealed class AttributedOperation : OperationBase
    {
        public override void Execute(Core.Contract.Infrastructure.Pipe.IPipelineMessage input) { }
    }

    private sealed class PrioritizedWrapperOperation : OperationBase, IPrioritizedOperation
    {
        public int Priority => 999;
        public string? Group => "billing";

        public override void Execute(Core.Contract.Infrastructure.Pipe.IPipelineMessage input) { }
    }

    private sealed class AsyncTrackingOperation(string name) : OperationBaseAsync
    {
        public override Task ExecuteAsync(Core.Contract.Infrastructure.Pipe.IPipelineMessage input)
        {
            TrackingOperation.ExecutionOrder.Add(name);
            return Task.CompletedTask;
        }
    }
}
