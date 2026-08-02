using Mvp24Hours.Application.Pipe.Test.Operations;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using Mvp24Hours.Infrastructure.Pipe.Operations.Parallel;

namespace Mvp24Hours.Application.Pipe.Test.Builders;

[Collection("PipeTestCollection")]
[Trait("Category", "Unit")]
public class ParallelOperationBuilderTest
{
    [Fact]
    public void BeginParallel_Should_AddParallelGroupToPipeline()
    {
        var pipeline = new Pipeline();
        pipeline.BeginParallel()
            .Add(new OperationTest())
            .Add(new TrackingOperation("parallel-a"))
            .WithMaxDegreeOfParallelism(2)
            .RequireAllSuccess(false)
            .EndParallel();

        pipeline.GetOperations().Should().ContainSingle(op => op is ParallelOperationGroup);
    }

    [Fact]
    public void AddParallel_Should_ExecuteOperationsConcurrently()
    {
        TrackingOperation.ExecutionOrder.Clear();
        var pipeline = new Pipeline();
        pipeline.AddParallel(builder => builder
            .Add(new TrackingOperation("p1"))
            .Add(new TrackingOperation("p2"))
            .WithMaxDegreeOfParallelism(2));

        pipeline.Execute(PipeTestHelpers.CreateMessage());

        TrackingOperation.ExecutionOrder.Should().HaveCount(2);
        TrackingOperation.ExecutionOrder.Should().Contain(["p1", "p2"]);
    }

    [Fact]
    public void AddParallel_WithOperationsEnumerable_Should_RegisterGroup()
    {
        var pipeline = new Pipeline();
        IOperation[] operations = [new OperationTest(), new TrackingOperation("x")];

        pipeline.AddParallel(operations, maxDegreeOfParallelism: 2, requireAllSuccess: true);

        pipeline.GetOperations().Should().ContainSingle(op => op is ParallelOperationGroup);
    }

    [Fact]
    public void ParallelBuilder_Add_Should_ThrowWhenOperationIsInvalid()
    {
        var pipeline = new Pipeline();
        IParallelOperationBuilder<IPipeline> builder = pipeline.BeginParallel();

        Action act = () => builder.Add(new object());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParallelBuilder_WithMaxDegreeOfParallelism_Should_ValidateInput()
    {
        var pipeline = new Pipeline();
        IParallelOperationBuilder<IPipeline> builder = pipeline.BeginParallel();

        Action act = () => builder.WithMaxDegreeOfParallelism(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task BeginParallelAsync_Should_AddParallelGroupToAsyncPipeline()
    {
        var pipeline = new PipelineAsync();
        pipeline.BeginParallel()
            .Add(new AsyncParallelTrackingOperation("async-1"))
            .EndParallel();

        pipeline.GetOperations().Should().ContainSingle(op => op is ParallelOperationGroupAsync);
        await pipeline.ExecuteAsync(PipeTestHelpers.CreateMessage());
    }

    private sealed class AsyncParallelTrackingOperation(string name) : OperationBaseAsync
    {
        public override Task ExecuteAsync(Core.Contract.Infrastructure.Pipe.IPipelineMessage input)
        {
            TrackingOperation.ExecutionOrder.Add(name);
            return Task.CompletedTask;
        }
    }
}
