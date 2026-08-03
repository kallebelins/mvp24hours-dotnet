using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Middleware;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using Mvp24Hours.Infrastructure.Pipe.Resolvers;

namespace Mvp24Hours.Application.Pipe.Test.Resolvers;

[Trait("Category", "Unit")]
public class PipelineBuilderResolverContainerTest
{
    [Fact]
    public void Add_ShouldRegisterAndReplaceResolver()
    {
        var container = new PipelineBuilderResolverContainer<IPipeline>();
        var first = new PipelineBuilderResolver();
        var second = new PipelineBuilderResolver();

        container.Add("key", first).Add("key", second);

        container.Get("key").Should().BeSameAs(second);
    }

    [Fact]
    public void GetDefault_WithConfiguredDefault_ShouldReturnDefaultResolver()
    {
        var resolver = new PipelineBuilderResolver();
        var container = new PipelineBuilderResolverContainer<IPipeline>("default-key");
        container.Add("default-key", resolver);

        container.GetDefault().Should().BeSameAs(resolver);
    }

    [Fact]
    public void GetDefault_WithoutDefaultKey_ShouldReturnNull()
    {
        var container = new PipelineBuilderResolverContainer<IPipeline>();

        container.GetDefault().Should().BeNull();
    }

    [Fact]
    public void Get_WithMissingKey_ShouldReturnNull()
    {
        var container = new PipelineBuilderResolverContainer<IPipeline>();

        container.Get("missing").Should().BeNull();
    }
}

[Trait("Category", "Unit")]
public class ConditionalBranchBuilderValidationTest
{
    [Fact]
    public void Case_WithEmptyKey_ShouldThrow()
    {
        var pipeline = new Pipeline();

        Action act = () => pipeline.AddSwitch(builder => builder.Case(
            "",
            _ => true,
            branch => branch.Add(new NoOpOperation())));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Case_WithNullCondition_ShouldThrow()
    {
        var pipeline = new Pipeline();

        Action act = () => pipeline.AddSwitch(builder => builder.Case(
            "key",
            null!,
            branch => branch.Add(new NoOpOperation())));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Default_WithNullConfigure_ShouldThrow()
    {
        var pipeline = new Pipeline();

        Action act = () => pipeline.AddSwitch(builder => builder.Default(null!));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EndSwitch_WithDefaultBranch_ShouldExecuteDefaultPath()
    {
        var pipeline = new Pipeline();
        pipeline.AddSwitch(builder => builder
            .Case("never", _ => false, branch => branch.Add(new FlagOperation("case")))
            .Default(branch => branch.Add(new FlagOperation("default")))
            .EndSwitch());

        var message = new PipelineMessage();
        pipeline.Execute(message);

        message.GetContent<bool>("default").Should().BeTrue();
        message.GetContent<bool>("case").Should().BeFalse();
    }

    [Fact]
    public async Task PipelineMiddlewareExecutor_WithNullMiddlewares_ShouldRunCoreAction()
    {
        bool executed = false;
        await PipelineMiddlewareExecutor.ExecuteAsync(
            null!,
            new PipelineMessage(),
            () =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        executed.Should().BeTrue();
    }

    [Fact]
    public void PipelineMiddlewareExecutor_Sync_WithNullMiddlewares_ShouldRunCoreAction()
    {
        bool executed = false;
        PipelineMiddlewareExecutor.Execute(
            null!,
            new PipelineMessage(),
            () => executed = true);

        executed.Should().BeTrue();
    }

    private sealed class NoOpOperation : OperationBase
    {
        public override void Execute(IPipelineMessage message) { }
    }

    private sealed class FlagOperation(string key) : OperationBase
    {
        public override void Execute(IPipelineMessage message)
        {
            message.AddContent(key, true);
        }
    }
}
