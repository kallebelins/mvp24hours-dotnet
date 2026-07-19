//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Typed;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class OperationChainTest
{
    [Fact, Priority(1)]
    public void OperationChain_Start_ShouldExecuteIdentity()
    {
        IOperationResult<int> result = OperationChain.Start<int>().Finally(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact, Priority(2)]
    public void OperationChain_Then_ShouldTransformValue()
    {
        IOperationResult<string> result = OperationChain.Start<int>()
            .Then(x => x * 2)
            .Then(x => x.ToString())
            .Finally(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("10");
    }

    [Fact, Priority(3)]
    public void OperationChain_Then_WithResult_ShouldPropagateFailure()
    {
        Func<int, IOperationResult<string>> failStep = _ => OperationResult<string>.Failure("step failed");

        IOperationResult<string> result = OperationChain.Start<int>()
            .Then(failStep)
            .Finally(1);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("step failed");
    }

    [Fact, Priority(4)]
    public void OperationChain_Then_WhenTransformThrows_ShouldReturnFailure()
    {
        Func<int, string> throwingTransform = _ => throw new Exception("transform error");

        IOperationResult<string> result = OperationChain.Start<int>()
            .Then(throwingTransform)
            .Finally(1);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("transform error");
    }

    [Fact, Priority(5)]
    public void OperationChain_Pipe_ShouldApplyInitialTransformation()
    {
        IOperationResult<string> result = OperationChain.Pipe<int, string>(x => $"val:{x}")
            .Finally(7);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("val:7");
    }

    [Fact, Priority(6)]
    public void OperationChain_Tap_ShouldPerformSideEffectWithoutChangingType()
    {
        var sideEffects = new List<int>();

        IOperationResult<int> result = OperationChain.Start<int>()
            .Then(x => x + 1)
            .Tap(x => sideEffects.Add(x))
            .Finally(9);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
        sideEffects.Should().Contain(10);
    }

    [Fact, Priority(7)]
    public void OperationChain_Tap_WhenActionThrows_ShouldReturnFailure()
    {
        Action<int> throwingAction = _ => throw new Exception("tap error");

        IOperationResult<int> result = OperationChain.Start<int>()
            .Tap(throwingAction)
            .Finally(1);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("tap error");
    }

    [Fact, Priority(8)]
    public void OperationChain_When_ConditionTrue_ShouldExecuteBranch()
    {
        var log = new List<string>();

        IOperationResult<int> result = OperationChain.Start<int>()
            .When(
                x => x > 5,
                chain => chain.Tap(x => log.Add($"big:{x}"))
            )
            .Finally(10);

        result.IsSuccess.Should().BeTrue();
        log.Should().Contain("big:10");
    }

    [Fact, Priority(9)]
    public void OperationChain_When_ConditionFalse_ShouldSkipBranch()
    {
        var log = new List<string>();

        IOperationResult<int> result = OperationChain.Start<int>()
            .When(
                x => x > 100,
                chain => chain.Tap(x => log.Add($"big:{x}"))
            )
            .Finally(3);

        result.IsSuccess.Should().BeTrue();
        log.Should().BeEmpty();
    }

    [Fact, Priority(10)]
    public async Task OperationChain_FinallyAsync_ShouldExecuteAsync()
    {
        IOperationResult<int> result = await OperationChain.Start<int>()
            .ThenAsync<int>(async (x, ct) =>
            {
                await Task.Delay(1, ct);
                return x * 3;
            })
            .FinallyAsync(4);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(12);
    }

    [Fact, Priority(11)]
    public void OperationChain_Finally_WithAsyncOperations_ShouldThrow()
    {
        var chain = OperationChain.Start<int>()
            .ThenAsync<int>(async (x, _) => { await Task.Yield(); return x; });

        Action act = () => chain.Finally(1);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact, Priority(12)]
    public void OperationChain_Then_NullTransform_ShouldThrow()
    {
        Action act = () => OperationChain.Start<int>().Then<string>((Func<int, string>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(13)]
    public void OperationChain_Pipe_NullTransform_ShouldThrow()
    {
        Action act = () => OperationChain.Pipe<int, string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(14)]
    public void OperationChain_Then_WithTypedOperation_ShouldExecute()
    {
        var op = new MultiplyOperation(3);

        IOperationResult<int> result = OperationChain.Start<int>()
            .Then(op)
            .Finally(4);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(12);
    }

    [Fact, Priority(15)]
    public async Task OperationChain_ThenAsync_WithTypedAsyncOperation_ShouldExecute()
    {
        var op = new AsyncMultiplyOperation(2);

        IOperationResult<int> result = await OperationChain.Start<int>()
            .ThenAsync(op)
            .FinallyAsync(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    private sealed class MultiplyOperation(int factor) : ITypedOperation<int, int>
    {
        public bool IsRequired => false;
        public IOperationResult<int> Execute(int input) => OperationResult<int>.Success(input * factor);
        public void Rollback(int input) { }
    }

    private sealed class AsyncMultiplyOperation(int factor) : ITypedOperationAsync<int, int>
    {
        public bool IsRequired => false;
        public async Task<IOperationResult<int>> ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return OperationResult<int>.Success(input * factor);
        }
        public Task RollbackAsync(int input, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
