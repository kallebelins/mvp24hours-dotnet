using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using PipeFactory = Mvp24Hours.Infrastructure.Pipe.Typed.Pipe;

namespace Mvp24Hours.Application.Pipe.Test.Typed;

[Trait("Category", "Unit")]
public class TypedPipelineFluentExtensionsTest
{
    [Fact]
    public void Create_Should_ReturnEmptyTypedPipeline()
    {
        TypedPipeline<int, string> pipeline = TypedPipelineFluentExtensions.Create<int, string>();

        pipeline.Should().NotBeNull();
        pipeline.OperationCount.Should().Be(0);
    }

    [Fact]
    public void CreateAsync_Should_ReturnEmptyTypedPipelineAsync()
    {
        TypedPipelineAsync<int, string> pipeline = TypedPipelineFluentExtensions.CreateAsync<int, string>();

        pipeline.Should().NotBeNull();
        pipeline.OperationCount.Should().Be(0);
    }

    [Fact]
    public void Pipe_Create_Should_ReturnEmptyTypedPipeline()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        pipeline.OperationCount.Should().Be(0);
    }

    [Fact]
    public void Pipe_CreateAsync_Should_ReturnEmptyTypedPipelineAsync()
    {
        TypedPipelineAsync<int, int> pipeline = PipeFactory.CreateAsync<int, int>();

        pipeline.OperationCount.Should().Be(0);
    }

    [Fact]
    public void Pipe_From_Should_StartOperationChain()
    {
        IOperationResult<int> result = PipeFactory.From<int>().Finally(7);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(7);
    }

    [Fact]
    public void Pipe_From_WithTransform_Should_ApplyInitialTransformation()
    {
        IOperationResult<string> result = PipeFactory.From<int, string>(x => $"num:{x}").Finally(4);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("num:4");
    }

    [Fact]
    public void Then_Should_ChainTransformationOntoExistingPipeline()
    {
        var basePipeline = (TypedPipeline<int, int>)new TypedPipeline<int, int>()
            .Add(input => OperationResult<int>.Success(input + 1));

        TypedPipeline<int, string> pipeline = basePipeline.Then(x => x.ToString());

        IOperationResult<string> result = pipeline.Execute(4);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("5");
    }

    [Fact]
    public void Then_Should_PropagateFailureFromBasePipeline()
    {
        var basePipeline = (TypedPipeline<int, int>)new TypedPipeline<int, int> { IsBreakOnFail = true }
            .Add(_ => OperationResult<int>.Failure("base failed"));

        TypedPipeline<int, string> pipeline = basePipeline.Then(x => x.ToString());

        IOperationResult<string> result = pipeline.Execute(1);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("base failed");
    }

    [Fact]
    public void Then_Should_ReturnFailureWhenTransformThrows()
    {
        var basePipeline = (TypedPipeline<int, int>)new TypedPipeline<int, int> { IsBreakOnFail = true }
            .Add(input => OperationResult<int>.Success(input));

        TypedPipeline<int, string> pipeline = basePipeline.Then<int, int, string>(_ => throw new InvalidOperationException("transform failed"));

        IOperationResult<string> result = pipeline.Execute(1);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("transform failed");
    }

    [Fact]
    public void Then_Should_CopyPipelineConfiguration()
    {
        var basePipeline = (TypedPipeline<int, int>)new TypedPipeline<int, int>
        {
            IsBreakOnFail = true,
            ForceRollbackOnFailure = true,
            AllowPropagateException = true
        }.Add(input => OperationResult<int>.Success(input));

        TypedPipeline<int, string> pipeline = basePipeline.Then<int, int, string>(x => x.ToString());

        pipeline.IsBreakOnFail.Should().BeTrue();
        pipeline.ForceRollbackOnFailure.Should().BeTrue();
        pipeline.AllowPropagateException.Should().BeTrue();
    }

    [Fact]
    public void Then_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipeline<int, int>? pipeline = null;

        Action act = () => pipeline!.Then<int, int, string>(x => x.ToString());

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void Then_Should_ThrowWhenTransformIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.Then<int, int, string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("transform");
    }

    [Fact]
    public async Task ThenAsync_Should_ChainAsyncTransformation()
    {
        var basePipeline = (TypedPipelineAsync<int, int>)new TypedPipelineAsync<int, int>()
            .Add(async (input, ct) =>
            {
                await Task.Delay(1, ct);
                return OperationResult<int>.Success(input * 2);
            });

        TypedPipelineAsync<int, string> pipeline = basePipeline.ThenAsync(async (value, ct) =>
        {
            await Task.Delay(1, ct);
            return $"v:{value}";
        });

        IOperationResult<string> result = await pipeline.ExecuteAsync(3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("v:6");
    }

    [Fact]
    public async Task ThenAsync_Should_PropagateFailureFromBasePipeline()
    {
        var basePipeline = (TypedPipelineAsync<int, int>)new TypedPipelineAsync<int, int> { IsBreakOnFail = true }
            .Add(async (_, _) => await Task.FromResult(OperationResult<int>.Failure("async base failed")));

        (await basePipeline.ExecuteAsync(1)).IsFailure.Should().BeTrue();

        TypedPipelineAsync<int, string> pipeline = basePipeline.ThenAsync(async (value, _) =>
        {
            await Task.CompletedTask;
            return value.ToString();
        });

        IOperationResult<string> result = await pipeline.ExecuteAsync(1);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("async base failed");
    }

    [Fact]
    public void ThenAsync_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipelineAsync<int, int>? pipeline = null;

        Action act = () => pipeline!.ThenAsync<int, int, string>((value, _) => Task.FromResult(value.ToString()));

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void ThenAsync_Should_ThrowWhenTransformIsNull()
    {
        TypedPipelineAsync<int, int> pipeline = PipeFactory.CreateAsync<int, int>();

        Action act = () => pipeline.ThenAsync<int, int, string>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("transform");
    }

    [Fact]
    public void OnError_WithFallbackValue_Should_ReturnFallbackOnFailure()
    {
        var basePipeline = (TypedPipeline<int, int>)new TypedPipeline<int, int> { IsBreakOnFail = true }
            .Add(_ => OperationResult<int>.Failure("failed"));

        TypedPipeline<int, int> pipeline = basePipeline.OnError(-1);

        IOperationResult<int> result = pipeline.Execute(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(-1);
    }

    [Fact]
    public void OnError_WithFallbackFactory_Should_UseFactoryOnFailure()
    {
        var basePipeline = (TypedPipeline<int, int>)new TypedPipeline<int, int> { IsBreakOnFail = true }
            .Add(_ => OperationResult<int>.Failure("failed"));

        TypedPipeline<int, int> pipeline = basePipeline.OnError(input => input + 100);

        IOperationResult<int> result = pipeline.Execute(7);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(107);
    }

    [Fact]
    public void OnError_WithFallbackFactory_Should_ReturnFailureWhenFactoryThrows()
    {
        var basePipeline = (TypedPipeline<int, int>)new TypedPipeline<int, int> { IsBreakOnFail = true }
            .Add(_ => OperationResult<int>.Failure("failed"));

        TypedPipeline<int, int> pipeline = basePipeline.OnError<int, int>(_ => throw new InvalidOperationException("factory failed"));

        IOperationResult<int> result = pipeline.Execute(1);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("factory failed");
    }

    [Fact]
    public void OnError_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipeline<int, int>? pipeline = null;

        Action act = () => pipeline!.OnError(0);

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void OnError_Should_ThrowWhenFallbackFactoryIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.OnError<int, int>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("fallbackFactory");
    }

    [Fact]
    public void WithBreakOnFail_Should_ConfigureSyncPipeline()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>().WithBreakOnFail(false);

        pipeline.IsBreakOnFail.Should().BeFalse();
    }

    [Fact]
    public void WithRollbackOnFailure_Should_ConfigureSyncPipeline()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>().WithRollbackOnFailure();

        pipeline.ForceRollbackOnFailure.Should().BeTrue();
    }

    [Fact]
    public void WithBreakOnFailAsync_Should_ConfigureAsyncPipeline()
    {
        TypedPipelineAsync<int, int> pipeline = PipeFactory.CreateAsync<int, int>().WithBreakOnFail(false);

        pipeline.IsBreakOnFail.Should().BeFalse();
    }

    [Fact]
    public void WithRollbackOnFailureAsync_Should_ConfigureAsyncPipeline()
    {
        TypedPipelineAsync<int, int> pipeline = PipeFactory.CreateAsync<int, int>().WithRollbackOnFailure();

        pipeline.ForceRollbackOnFailure.Should().BeTrue();
    }

    [Fact]
    public void WithBreakOnFail_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipeline<int, int>? pipeline = null;

        Action act = () => pipeline!.WithBreakOnFail();

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void Tap_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipeline<int, int>? pipeline = null;

        Action act = () => pipeline!.Tap(_ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void Tap_Should_ThrowWhenActionIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.Tap(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public void TapAsync_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipelineAsync<int, int>? pipeline = null;

        Action act = () => pipeline!.TapAsync<int, int>((_, _) => Task.CompletedTask);

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void TapAsync_Should_ThrowWhenActionIsNull()
    {
        TypedPipelineAsync<int, int> pipeline = PipeFactory.CreateAsync<int, int>();

        Action act = () => pipeline.TapAsync<int, int>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("action");
    }

    [Fact]
    public void When_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipeline<int, int>? pipeline = null;

        Action act = () => pipeline!.When(_ => true, _ => OperationResult<int>.Success(1));

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void When_Should_ThrowWhenConditionIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.When(null!, _ => OperationResult<int>.Success(1));

        act.Should().Throw<ArgumentNullException>().WithParameterName("condition");
    }

    [Fact]
    public void When_Should_ThrowWhenOperationIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.When(_ => true, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public void Branch_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipeline<int, int>? pipeline = null;

        Action act = () => pipeline!.Branch(
            _ => true,
            value => OperationResult<int>.Success(value),
            value => OperationResult<int>.Success(value));

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void Branch_Should_ThrowWhenConditionIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.Branch(
            null!,
            value => OperationResult<int>.Success(value),
            value => OperationResult<int>.Success(value));

        act.Should().Throw<ArgumentNullException>().WithParameterName("condition");
    }

    [Fact]
    public void Branch_Should_ThrowWhenThenOperationIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.Branch(
            _ => true,
            null!,
            value => OperationResult<int>.Success(value));

        act.Should().Throw<ArgumentNullException>().WithParameterName("thenOperation");
    }

    [Fact]
    public void Branch_Should_ThrowWhenElseOperationIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.Branch(
            _ => true,
            value => OperationResult<int>.Success(value),
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("elseOperation");
    }

    [Fact]
    public void Ensure_Should_ThrowWhenPipelineIsNull()
    {
        TypedPipeline<int, int>? pipeline = null;

        Action act = () => pipeline!.Ensure(_ => true, "invalid");

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void Ensure_Should_ThrowWhenPredicateIsNull()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();

        Action act = () => pipeline.Ensure(null!, "invalid");

        act.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
    }

    [Fact]
    public void Tap_Should_AddOperationToPipeline()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();
        TypedPipeline<int, int> result = pipeline.Tap(_ => { });

        result.OperationCount.Should().Be(1);
        result.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void When_Should_AddOperationToPipeline()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();
        TypedPipeline<int, int> result = pipeline.When(_ => true, v => OperationResult<int>.Success(v));

        result.OperationCount.Should().Be(1);
    }

    [Fact]
    public void Branch_Should_AddOperationToPipeline()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();
        TypedPipeline<int, int> result = pipeline.Branch(
            _ => true,
            v => OperationResult<int>.Success(v),
            v => OperationResult<int>.Success(v));

        result.OperationCount.Should().Be(1);
    }

    [Fact]
    public void Ensure_Should_AddOperationToPipeline()
    {
        TypedPipeline<int, int> pipeline = PipeFactory.Create<int, int>();
        TypedPipeline<int, int> result = pipeline.Ensure(_ => true, "invalid");

        result.OperationCount.Should().Be(1);
    }

    [Fact]
    public void Pipe_From_Tap_Should_ExecuteSideEffectOnSuccess()
    {
        int captured = 0;
        IOperationResult<int> result = PipeFactory.From<int>()
            .Then(x => x * 2)
            .Tap(value => captured = value)
            .Finally(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
        captured.Should().Be(10);
    }

    [Fact]
    public void Pipe_From_When_Should_ExecuteConditionalBranch()
    {
        IOperationResult<int> result = PipeFactory.From<int>()
            .Then(x => x)
            .When(
                v => v > 0,
                chain => chain.Then(v => v + 100))
            .Finally(3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(103);
    }

    [Fact]
    public void Pipe_From_When_Should_SkipBranchWhenConditionFalse()
    {
        IOperationResult<int> result = PipeFactory.From<int>()
            .Then(x => x)
            .When(
                v => v > 10,
                chain => chain.Then(v => v + 100))
            .Finally(3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
    }
}
