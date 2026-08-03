using Mvp24Hours.Core.Contract.Application.Pipe;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Resolvers;

using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Resolvers;

[Trait("Category", "Unit")]
public class PipelineBuilderResolverTest
{
    [Fact]
    public void AddAndGet_Should_ResolveRegisteredBuilder()
    {
        var resolver = new PipelineBuilderResolver();

        resolver.Add<ITestPipelineBuilder, TestPipelineBuilderImpl>();
        ITestPipelineBuilder? builder = resolver.Get<ITestPipelineBuilder>();

        builder.Should().NotBeNull();
        builder.Should().BeOfType<TestPipelineBuilderImpl>();
    }

    [Fact]
    public void AddAndGet_WithSimpleKey_Should_ResolveBuilder()
    {
        var resolver = new PipelineBuilderResolver();

        resolver.Add<ITestPipelineBuilder, TestPipelineBuilderImpl>("custom-key", isSimpleKey: true);
        ITestPipelineBuilder? builder = resolver.Get<ITestPipelineBuilder>("custom-key", isSimpleKey: true);

        builder.Should().NotBeNull();
    }

    [Fact]
    public void Has_Should_ReturnTrue_WhenBuilderRegistered()
    {
        var resolver = new PipelineBuilderResolver();
        resolver.Add<ITestPipelineBuilder, TestPipelineBuilderImpl>();

        resolver.Get<ITestPipelineBuilder>().Should().NotBeNull();
        resolver.Has<ITestPipelineBuilder>("missing").Should().BeFalse();
        resolver.Has(typeof(ITestPipelineBuilder).FullName!, typeof(ITestPipelineBuilder)).Should().BeTrue();
    }

    [Fact]
    public void AddListAndGetList_Should_ReturnAllBuilders()
    {
        var resolver = new PipelineBuilderResolver();

        resolver
            .AddList<ITestPipelineBuilder, TestPipelineBuilderImpl>()
            .AddList<ITestPipelineBuilder, TestPipelineBuilderAlt>();

        List<ITestPipelineBuilder> builders = resolver.GetList<ITestPipelineBuilder>();

        builders.Should().HaveCount(2);
        resolver.HasList<ITestPipelineBuilder>(typeof(ITestPipelineBuilder).FullName!).Should().BeTrue();
    }

    [Fact]
    public void Get_Should_ReturnDefault_WhenBuilderMissing()
    {
        var resolver = new PipelineBuilderResolver();

        ITestPipelineBuilder? builder = resolver.Get<ITestPipelineBuilder>();

        builder.Should().BeNull();
        resolver.GetList<ITestPipelineBuilder>().Should().BeEmpty();
    }

    private interface ITestPipelineBuilder : IPipelineBuilder;

    private sealed class TestPipelineBuilderImpl : ITestPipelineBuilder
    {
        public IPipeline Builder(IPipeline pipeline) => pipeline;
    }

    private sealed class TestPipelineBuilderAlt : ITestPipelineBuilder
    {
        public IPipeline Builder(IPipeline pipeline) => pipeline.Add(new TrackingOperation("alt"));
    }

    private sealed class TrackingOperation(string name) : OperationBase
    {
        public override void Execute(IPipelineMessage input) => input.AddContent(name, true);
    }
}
