using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.Pipe.Test.Operations;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Builders;
using Mvp24Hours.Infrastructure.Pipe.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using Mvp24Hours.Infrastructure.Pipe.Operations.Composition;

namespace Mvp24Hours.Application.Pipe.Test.Builders;

[Trait("Category", "Unit")]
public class SubPipelineBuilderTest
{
    [Fact]
    public void BeginScope_Should_BuildFluentSyncScope()
    {
        var pipeline = new Pipeline();

        IPipeline result = pipeline
            .BeginScope("sync-scope")
            .Add(new TrackingOperation("scoped-sync"))
            .EndScope();

        result.Should().BeSameAs(pipeline);
        pipeline.GetOperations().Should().ContainSingle(op => op is SubPipelineOperation);
    }

    [Fact]
    public async Task BeginScopeAsync_Should_BuildFluentAsyncScope()
    {
        var pipeline = new PipelineAsync();

        IPipelineAsync result = pipeline
            .BeginScope("async-scope")
            .Add(new AsyncScopedOperation("scoped-async"))
            .EndScope();

        result.Should().BeSameAs(pipeline);
        pipeline.GetOperations().Should().ContainSingle(op => op is SubPipelineOperationAsync);

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        await pipeline.ExecuteAsync(message);
        message.GetContent<bool>("scoped-async").Should().BeTrue();
    }

    [Fact]
    public void Add_WithServiceProvider_Should_ResolveFromDi()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DiSyncOperation>();
        using ServiceProvider provider = services.BuildServiceProvider();

        var pipeline = new Pipeline();
        ISubPipelineBuilder builder = CreateSyncBuilder(pipeline, provider, "di-scope");

        builder.Add<DiSyncOperation>().EndScope();

        SubPipelineOperation subPipeline = pipeline.GetOperations().OfType<SubPipelineOperation>().Single();
        subPipeline.Operations.Single().Should().BeSameAs(provider.GetRequiredService<DiSyncOperation>());
    }

    [Fact]
    public void Add_WithoutServiceProvider_Should_UseActivator()
    {
        var pipeline = new Pipeline();

        pipeline.BeginScope("activator-scope")
            .Add<ActivatableSyncOperation>()
            .EndScope();

        SubPipelineOperation subPipeline = pipeline.GetOperations().OfType<SubPipelineOperation>().Single();
        subPipeline.Operations.Single().Should().BeOfType<ActivatableSyncOperation>();
    }

    [Fact]
    public async Task AddAsync_WithServiceProvider_Should_ResolveFromDi()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DiAsyncOperation>();
        using ServiceProvider provider = services.BuildServiceProvider();

        var pipeline = new PipelineAsync();
        ISubPipelineBuilderAsync builder = CreateAsyncBuilder(pipeline, provider, "di-async-scope");

        builder.Add<DiAsyncOperation>().EndScope();

        SubPipelineOperationAsync subPipeline = pipeline.GetOperations().OfType<SubPipelineOperationAsync>().Single();
        subPipeline.Operations.Single().Should().BeSameAs(provider.GetRequiredService<DiAsyncOperation>());
    }

    [Fact]
    public async Task AddAsync_WithoutServiceProvider_Should_UseActivator()
    {
        var pipeline = new PipelineAsync();

        pipeline.BeginScope("activator-async-scope")
            .Add<ActivatableAsyncOperation>()
            .EndScope();

        SubPipelineOperationAsync subPipeline = pipeline.GetOperations().OfType<SubPipelineOperationAsync>().Single();
        subPipeline.Operations.Single().Should().BeOfType<ActivatableAsyncOperation>();
    }

    [Fact]
    public void Add_Generic_Should_ThrowWhenTypeIsAbstract()
    {
        var pipeline = new Pipeline();
        ISubPipelineBuilder builder = pipeline.BeginScope("abstract-scope");

        Action act = () => builder.Add<AbstractSyncOperation>();

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Operation not found*");
    }

    [Fact]
    public void Add_Generic_Should_ThrowWhenTypeIsNotRegistered()
    {
        var services = new ServiceCollection();
        using ServiceProvider provider = services.BuildServiceProvider();

        var pipeline = new Pipeline();
        ISubPipelineBuilder builder = CreateSyncBuilder(pipeline, provider, "unregistered-scope");

        Action act = () => builder.Add<IUnregisteredSyncOperationMarker>();

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Operation not found*");
    }

    [Fact]
    public void AddAsync_Generic_Should_ThrowWhenTypeIsAbstract()
    {
        var pipeline = new PipelineAsync();
        ISubPipelineBuilderAsync builder = pipeline.BeginScope("abstract-async-scope");

        Action act = () => builder.Add<AbstractAsyncOperation>();

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Operation not found*");
    }

    [Fact]
    public void AddAsync_Generic_Should_ThrowWhenTypeIsNotRegistered()
    {
        var services = new ServiceCollection();
        using ServiceProvider provider = services.BuildServiceProvider();

        var pipeline = new PipelineAsync();
        ISubPipelineBuilderAsync builder = CreateAsyncBuilder(pipeline, provider, "unregistered-async-scope");

        Action act = () => builder.Add<IUnregisteredAsyncOperationMarker>();

        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*Operation not found*");
    }

    [Fact]
    public void Add_WithNullOperation_Should_Throw()
    {
        var pipeline = new Pipeline();
        ISubPipelineBuilder builder = pipeline.BeginScope("null-sync");

        Action act = () => builder.Add((IOperation)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void AddAsync_WithNullOperation_Should_Throw()
    {
        var pipeline = new PipelineAsync();
        ISubPipelineBuilderAsync builder = pipeline.BeginScope("null-async");

        Action act = () => builder.Add((IOperationAsync)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void BeginScope_Should_ExposeScopeName()
    {
        var pipeline = new Pipeline();

        ISubPipelineBuilder builder = pipeline.BeginScope("named-scope");

        builder.Name.Should().Be("named-scope");
    }

    private static SubPipelineBuilder CreateSyncBuilder(Pipeline pipeline, IServiceProvider provider, string? name)
    {
        ConstructorInfo constructor = typeof(SubPipelineBuilder).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(Pipeline), typeof(IServiceProvider), typeof(string)],
            modifiers: null)!;

        return (SubPipelineBuilder)constructor.Invoke([pipeline, provider, name]);
    }

    private static SubPipelineBuilderAsync CreateAsyncBuilder(PipelineAsync pipeline, IServiceProvider provider, string? name)
    {
        ConstructorInfo constructor = typeof(SubPipelineBuilderAsync).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(PipelineAsync), typeof(IServiceProvider), typeof(string)],
            modifiers: null)!;

        return (SubPipelineBuilderAsync)constructor.Invoke([pipeline, provider, name]);
    }

    private sealed class DiSyncOperation : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
        }
    }

    private sealed class DiAsyncOperation : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class ActivatableSyncOperation : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
        }
    }

    private sealed class ActivatableAsyncOperation : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            return Task.CompletedTask;
        }
    }

    private abstract class AbstractSyncOperation : OperationBase
    {
        public override void Execute(IPipelineMessage input)
        {
        }
    }

    private abstract class AbstractAsyncOperation : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            return Task.CompletedTask;
        }
    }

    private interface IUnregisteredSyncOperationMarker : IOperation
    {
    }

    private interface IUnregisteredAsyncOperationMarker : IOperationAsync
    {
    }

    private sealed class AsyncScopedOperation(string name) : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            input.AddContent(name, true);
            return Task.CompletedTask;
        }
    }
}
