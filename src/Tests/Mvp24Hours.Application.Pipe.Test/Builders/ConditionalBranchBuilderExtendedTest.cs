using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Builders;

[Trait("Category", "Unit")]
public class ConditionalBranchBuilderExtendedTest
{
    [Fact]
    public async Task BeginSwitch_AsyncPipeline_ShouldExecuteMatchedCase()
    {
        var pipeline = new PipelineAsync();
        pipeline.AddSwitch(builder => builder
            .Case("matched", _ => true, branch => branch.Add(new AsyncFlagOperation("matched")))
            .Default(branch => branch.Add(new AsyncFlagOperation("default"))));

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        await pipeline.ExecuteAsync(message);

        message.GetContent<bool>("matched").Should().BeTrue();
    }

    [Fact]
    public void AddParallel_WithNullConfigure_ShouldThrow()
    {
        var pipeline = new Pipeline();

        Action act = () => pipeline.AddParallel((Action<IParallelOperationBuilder<IPipeline>>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BeginScope_ShouldAddSubPipelineOperation()
    {
        TrackingOperation.ExecutionOrder.Clear();
        var pipeline = new Pipeline();

        pipeline.BeginScope("scope-test")
            .Add(new TrackingOperation("scoped"))
            .EndScope();

        pipeline.GetOperations().Should().ContainSingle();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        pipeline.Execute(message);
        TrackingOperation.ExecutionOrder.Should().Contain("scoped");
    }

    private sealed class AsyncFlagOperation(string key) : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage message)
        {
            message.AddContent(key, true);
            return Task.CompletedTask;
        }
    }
}
