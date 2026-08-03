using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Builders;
using Mvp24Hours.Infrastructure.Pipe.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Extensions;

[Trait("Category", "Unit")]
public class PipelineFluentExtensionsTest
{
    [Fact]
    public void BeginParallel_Should_ReturnBuilder()
    {
        var pipeline = new Pipeline();

        var builder = pipeline.BeginParallel();

        builder.Should().NotBeNull();
    }

    [Fact]
    public void AddParallel_WithOperations_Should_AddGroup()
    {
        TrackingOperation.ExecutionOrder.Clear();
        var pipeline = new Pipeline();
        IOperation[] operations = [new TrackingOperation("p1"), new TrackingOperation("p2")];

        pipeline.AddParallel(operations, maxDegreeOfParallelism: 2);

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        pipeline.Execute(message);

        TrackingOperation.ExecutionOrder.Should().HaveCount(2);
    }

    [Fact]
    public void AddParallel_WithConfigure_Should_ExecuteConfiguredOperations()
    {
        TrackingOperation.ExecutionOrder.Clear();
        var pipeline = new Pipeline();

        pipeline.AddParallel(builder => builder
            .Add(new TrackingOperation("cfg1"))
            .Add(new TrackingOperation("cfg2")));

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        pipeline.Execute(message);

        TrackingOperation.ExecutionOrder.Should().HaveCount(2);
    }

    [Fact]
    public void AddParallel_Should_Throw_WhenConfigureIsNull()
    {
        var pipeline = new Pipeline();

        Action act = () => pipeline.AddParallel((Action<IParallelOperationBuilder<IPipeline>>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BeginSwitch_Should_AddConditionalBranch()
    {
        var pipeline = new Pipeline();

        pipeline.AddSwitch(builder => builder
            .Case("matched", _ => true, branch => branch.Add(new TrackingOperation("matched"))));

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        pipeline.Execute(message);

        message.GetContent<bool>("matched").Should().BeTrue();
    }

    [Fact]
    public void AddScope_Should_ExecuteScopedOperations()
    {
        var pipeline = new Pipeline();

        pipeline.AddScope("scope-a", builder => builder.Add(new TrackingOperation("scoped")));

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        pipeline.Execute(message);

        message.GetContent<bool>("scoped").Should().BeTrue();
    }

    [Fact]
    public async Task BeginParallelAsync_Should_ExecuteAsyncOperations()
    {
        var pipeline = new PipelineAsync();

        pipeline.AddParallel(builder => builder.Add(new AsyncTrackingOperation("async1")));

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        await pipeline.ExecuteAsync(message);

        message.GetContent<bool>("async1").Should().BeTrue();
    }

    private sealed class AsyncTrackingOperation(string name) : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            input.AddContent(name, true);
            return Task.CompletedTask;
        }
    }
}
