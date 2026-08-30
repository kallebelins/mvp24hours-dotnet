//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Pipe.Context;

namespace Mvp24Hours.Application.Pipe.Test.Context;

[Trait("Category", "Unit")]
public class PipelineContextServiceExtensionsTest
{
    [Fact]
    public void AddPipelineContext_Parameterless_RegistersContextAccessorAndPropagationMiddleware()
    {
        var services = new ServiceCollection();

        services.AddPipelineContext();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IPipelineContextAccessor>().Should().NotBeNull();
        provider.GetRequiredService<ContextPropagationMiddleware>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineContext_WithNullServices_Throws()
    {
        Action act = () => ((IServiceCollection)null!).AddPipelineContext(_ => { });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPipelineContext_WithNullConfigure_Throws()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddPipelineContext((Action<PipelineContextOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddPipelineContext_WithPropagationDisabled_DoesNotRegisterPropagationMiddleware()
    {
        var services = new ServiceCollection();

        services.AddPipelineContext(options => options.EnableContextPropagation = false);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<ContextPropagationMiddleware>().Should().BeNull();
    }

    [Fact]
    public void AddPipelineContext_WithOperationActivityTracingEnabled_RegistersOperationActivityMiddleware()
    {
        var services = new ServiceCollection();

        services.AddPipelineContext(options => options.EnableOperationActivityTracing = true);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<OperationActivityMiddleware>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineContext_WithOperationActivityTracingDisabled_DoesNotRegisterOperationActivityMiddleware()
    {
        var services = new ServiceCollection();

        services.AddPipelineContext(options => options.EnableOperationActivityTracing = false);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<OperationActivityMiddleware>().Should().BeNull();
    }

    [Fact]
    public void AddPipelineContextWithTracing_EnablesActivityTracingWithDefaultRootActivityName()
    {
        var services = new ServiceCollection();

        services.AddPipelineContextWithTracing();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ContextPropagationOptions>().RootActivityName.Should().Be("Pipeline.Execute");
        provider.GetRequiredService<OperationActivityMiddleware>().Should().NotBeNull();
    }

    [Fact]
    public void AddPipelineContextWithTracing_WithCustomActivitySourceName_UsesProvidedName()
    {
        var services = new ServiceCollection();

        services.AddPipelineContextWithTracing("Custom.Source");
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ContextPropagationOptions>().RootActivityName.Should().Be("Custom.Source");
    }

    [Fact]
    public void AddPipelineContextWithSnapshots_EnablesAllSnapshotCaptureFlags()
    {
        var services = new ServiceCollection();

        services.AddPipelineContextWithSnapshots();
        ServiceProvider provider = services.BuildServiceProvider();

        ContextPropagationOptions options = provider.GetRequiredService<ContextPropagationOptions>();
        options.CaptureInitialSnapshot.Should().BeTrue();
        options.CaptureFinalSnapshot.Should().BeTrue();
        options.CaptureErrorSnapshot.Should().BeTrue();
    }

    [Fact]
    public void PipelineContextOptions_Defaults_MatchDocumentedValues()
    {
        var options = new PipelineContextOptions();

        options.EnableContextPropagation.Should().BeTrue();
        options.ContextPropagationOrder.Should().Be(-1000);
        options.EnableActivityTracing.Should().BeTrue();
        options.StoreContextInMessage.Should().BeTrue();
        options.CaptureInitialSnapshot.Should().BeFalse();
        options.CaptureFinalSnapshot.Should().BeFalse();
        options.CaptureErrorSnapshot.Should().BeTrue();
        options.EnableOperationActivityTracing.Should().BeFalse();
        options.OperationActivityOrder.Should().Be(-900);
        options.IncludeContentCountInTracing.Should().BeTrue();
    }
}
