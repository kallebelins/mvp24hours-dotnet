using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using Mvp24Hours.Infrastructure.Pipe.Validation;

namespace Mvp24Hours.Application.Pipe.Test.Extensions;

[Trait("Category", "Unit")]
public class PipelineFluentExtensionsTest
{
    [Fact]
    public void BeginParallel_Should_ReturnBuilder()
    {
        var pipeline = new Pipeline();

        IParallelOperationBuilder<IPipeline> builder = pipeline.BeginParallel();

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

    [Fact]
    public void AddPipeline_Should_ComposeOperationsFromOtherPipeline()
    {
        var source = new Pipeline();
        source.Add(new TrackingOperation("composed"));

        var pipeline = new Pipeline();
        pipeline.Add(new TrackingOperation("host"));
        pipeline.AddPipeline(source);

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        pipeline.Execute(message);

        message.GetContent<bool>("host").Should().BeTrue();
        message.GetContent<bool>("composed").Should().BeTrue();
    }

    [Fact]
    public void AddPipeline_Should_Throw_WhenOtherPipelineIsNull()
    {
        var pipeline = new Pipeline();

        Action act = () => pipeline.AddPipeline(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddPipelineAsync_Should_ComposeOperationsFromOtherPipeline()
    {
        var source = new PipelineAsync();
        source.Add(new AsyncTrackingOperation("composed-async"));

        var pipeline = new PipelineAsync();
        pipeline.Add(new AsyncTrackingOperation("host-async"));
        pipeline.AddPipeline(source);

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        await pipeline.ExecuteAsync(message);

        message.GetContent<bool>("host-async").Should().BeTrue();
        message.GetContent<bool>("composed-async").Should().BeTrue();
    }

    [Fact]
    public void Validate_Should_ReturnValidationResult()
    {
        var pipeline = new Pipeline();
        pipeline.Add(new TrackingOperation("valid"));

        PipelineValidationResult result = pipeline.Validate();

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnValidationResult()
    {
        var pipeline = new PipelineAsync();
        pipeline.Add(new AsyncTrackingOperation("valid-async"));

        PipelineValidationResult result = pipeline.Validate();

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ExecuteValidated_Should_RunPipelineWhenValid()
    {
        TrackingOperation.ExecutionOrder.Clear();
        var pipeline = new Pipeline();
        pipeline.Add(new TrackingOperation("validated"));

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        pipeline.ExecuteValidated(message);

        message.GetContent<bool>("validated").Should().BeTrue();
    }

    [Fact]
    public void ExecuteValidated_Should_Throw_WhenValidationFails()
    {
        var pipeline = new Pipeline();
        DefaultPipelineValidator validator = new DefaultPipelineValidator().RequireAtLeastOneOperation();

        Action act = () => pipeline.ExecuteValidated(validator: validator);

        act.Should().Throw<PipelineValidationException>();
    }

    [Fact]
    public async Task ExecuteValidatedAsync_Should_RunPipelineWhenValid()
    {
        var pipeline = new PipelineAsync();
        pipeline.Add(new AsyncTrackingOperation("validated-async"));

        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        await pipeline.ExecuteValidatedAsync(message);

        message.GetContent<bool>("validated-async").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteValidatedAsync_Should_Throw_WhenValidationFails()
    {
        var pipeline = new PipelineAsync();
        DefaultPipelineValidator validator = new DefaultPipelineValidator().RequireAtLeastOneOperation();

        Func<Task> act = () => pipeline.ExecuteValidatedAsync(validator: validator);

        await act.Should().ThrowAsync<PipelineValidationException>();
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
