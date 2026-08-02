using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;

namespace Mvp24Hours.Application.Pipe.Test.Typed;

[Trait("Category", "Unit")]
public class TypedPipelineTest
{
    [Fact]
    public void TypedPipeline_Should_ChainOperationsAndReturnOutput()
    {
        var pipeline = new TypedPipeline<int, int>()
            .Add((input) => OperationResult<int>.Success(input + 1))
            .Add((input) => OperationResult<int>.Success(input * 2)) as TypedPipeline<int, int>;

        IOperationResult<int> result = pipeline!.Execute(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(12);
        pipeline.OperationCount.Should().Be(2);
    }

    [Fact]
    public void TypedPipeline_Should_AddTypedOperationAndRollbackOnFailure()
    {
        IncrementOperation.RollbackLog.Clear();
        ITypedPipeline<int, int> pipeline = new TypedPipeline<int, int> { ForceRollbackOnFailure = true, IsBreakOnFail = true }
            .Add(new IncrementOperation())
            .Add(new FailingIncrementOperation());

        IOperationResult<int> result = pipeline.Execute(1);

        result.IsFailure.Should().BeTrue();
        IncrementOperation.RollbackLog.Should().Contain(1);
    }

    [Fact]
    public void TypedPipeline_Should_PropagateExceptionWhenConfigured()
    {
        ITypedPipeline<int, int> pipeline = new TypedPipeline<int, int> { AllowPropagateException = true, IsBreakOnFail = true }
            .Add(new ThrowingTypedOperation());

        Action act = () => pipeline.Execute(1);

        act.Should().Throw<InvalidOperationException>().WithMessage("typed failure");
    }

    [Fact]
    public void TypedPipeline_Should_AddActionOperation()
    {
        TypedPipeline<List<int>, List<int>> pipeline = new TypedPipeline<List<int>, List<int>>()
            .Add(list => list.Add(42));

        IOperationResult<List<int>> result = pipeline.Execute([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(42);
    }

    [Fact]
    public void TypedOperationBase_Should_WrapExecuteCore()
    {
        SideEffectOperation.Effects.Clear();
        var operation = new SideEffectOperation();

        IOperationResult<object> result = operation.Execute(7);

        result.IsSuccess.Should().BeTrue();
        SideEffectOperation.Effects.Should().Contain(7);
    }

    [Fact]
    public async Task TypedPipelineAsync_Should_ChainOperations()
    {
        ITypedPipelineAsync<int, int> pipeline = new TypedPipelineAsync<int, int>()
            .Add(async (input, ct) =>
            {
                await Task.Delay(1, ct);
                return OperationResult<int>.Success(input + 3);
            });

        IOperationResult<int> result = await pipeline.ExecuteAsync(10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(13);
    }

    [Fact]
    public async Task TypedPipelineAsync_Should_RollbackOnFailure()
    {
        AsyncIncrementOperation.RollbackLog.Clear();
        ITypedPipelineAsync<int, int> pipeline = new TypedPipelineAsync<int, int> { ForceRollbackOnFailure = true, IsBreakOnFail = true }
            .Add(new AsyncIncrementOperation())
            .Add(new AsyncFailingIncrementOperation());

        IOperationResult<int> result = await pipeline.ExecuteAsync(1);

        result.IsFailure.Should().BeTrue();
        AsyncIncrementOperation.RollbackLog.Should().Contain(1);
    }

    [Fact]
    public async Task TypedOperationBaseAsync_Should_WrapExecuteCoreAsync()
    {
        AsyncSideEffectOperation.Effects.Clear();
        var operation = new AsyncSideEffectOperation();

        IOperationResult<object> result = await operation.ExecuteAsync(9);

        result.IsSuccess.Should().BeTrue();
        AsyncSideEffectOperation.Effects.Should().Contain(9);
    }

    [Fact]
    public void TypedPipeline_Add_Should_ThrowWhenOperationIsNull()
    {
        var pipeline = new TypedPipeline<int, int>();

        Action act = () => pipeline.Add((Func<int, IOperationResult<int>>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class IncrementOperation : TypedOperationBase<int, int>
    {
        public static List<int> RollbackLog { get; } = [];

        public override IOperationResult<int> Execute(int input)
        {
            return Success(input + 1);
        }

        public override void Rollback(int input)
        {
            RollbackLog.Add(input);
        }
    }

    private sealed class FailingIncrementOperation : TypedOperationBase<int, int>
    {
        public override IOperationResult<int> Execute(int input)
        {
            return Failure("failed step");
        }
    }

    private sealed class ThrowingTypedOperation : TypedOperationBase<int, int>
    {
        public override IOperationResult<int> Execute(int input)
        {
            throw new InvalidOperationException("typed failure");
        }
    }

    private sealed class SideEffectOperation : TypedOperationBase<int>
    {
        public static List<int> Effects { get; } = [];

        protected override void ExecuteCore(int input)
        {
            Effects.Add(input);
        }
    }

    private sealed class AsyncIncrementOperation : TypedOperationBaseAsync<int, int>
    {
        public static List<int> RollbackLog { get; } = [];

        public override Task<IOperationResult<int>> ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            return SuccessAsync(input + 1);
        }

        public override Task RollbackAsync(int input, CancellationToken cancellationToken = default)
        {
            RollbackLog.Add(input);
            return Task.CompletedTask;
        }
    }

    private sealed class AsyncFailingIncrementOperation : TypedOperationBaseAsync<int, int>
    {
        public override Task<IOperationResult<int>> ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            return FailureAsync("async failed");
        }
    }

    private sealed class AsyncSideEffectOperation : TypedOperationBaseAsync<int>
    {
        public static List<int> Effects { get; } = [];

        protected override Task ExecuteCoreAsync(int input, CancellationToken cancellationToken = default)
        {
            Effects.Add(input);
            return Task.CompletedTask;
        }
    }
}
