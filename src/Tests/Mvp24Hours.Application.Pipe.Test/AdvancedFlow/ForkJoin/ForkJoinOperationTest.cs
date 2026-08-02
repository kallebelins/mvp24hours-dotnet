//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.ForkJoin;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.AdvancedFlow.ForkJoin;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class ForkJoinOperationTest
{
    [Fact, Priority(1)]
    public void ForkJoin_SyncBranch_ShouldProcessAllBranchesAndJoin()
    {
        var forkJoin = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branch: n => OperationResult<int>.Success(n * 2),
            join: results => OperationResult<int>.Success(results.Sum(r => r.Value))
        );

        IOperationResult<int> result = forkJoin.Execute([1, 2, 3]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(12); // (1+2+3)*2
    }

    [Fact, Priority(2)]
    public async Task ForkJoin_AsyncBranch_ShouldProcessAllBranchesAndJoin()
    {
        var forkJoin = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branchAsync: async (n, ct) =>
            {
                await Task.Delay(1, ct);
                return OperationResult<int>.Success(n * 3);
            },
            join: results => OperationResult<int>.Success(results.Sum(r => r.Value))
        );

        IOperationResult<int> result = await forkJoin.ExecuteAsync([1, 2, 3]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(18); // (1+2+3)*3
    }

    [Fact, Priority(3)]
    public void ForkJoin_NullFork_ShouldThrow()
    {
        Action act = () => _ = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: null!,
            branch: n => OperationResult<int>.Success(n),
            join: results => OperationResult<int>.Success(0)
        );

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(4)]
    public void ForkJoin_NullBranch_ShouldThrow()
    {
        Action act = () => _ = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branch: null!,
            join: results => OperationResult<int>.Success(0)
        );

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(5)]
    public void ForkJoin_NullJoin_ShouldThrow()
    {
        Action act = () => _ = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branch: n => OperationResult<int>.Success(n),
            join: null!
        );

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(6)]
    public void ForkJoin_BranchFails_ShouldIncludeFailureInJoin()
    {
        var forkJoin = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branch: n => n == 2
                ? OperationResult<int>.Failure("failed at 2")
                : OperationResult<int>.Success(n),
            join: results => OperationResult<int>.Success(results.Count(r => r.IsSuccess))
        );

        IOperationResult<int> result = forkJoin.Execute([1, 2, 3]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2); // 2 successes out of 3
    }

    [Fact, Priority(7)]
    public void ForkJoin_WithPreserveOrder_ShouldReturnResultsInOrder()
    {
        var options = new ForkJoinOptions { PreserveOrder = true };
        var forkJoin = new ForkJoinOperation<IEnumerable<int>, int, int, List<int>>(
            fork: inputs => inputs,
            branch: n => OperationResult<int>.Success(n * 10),
            join: results => OperationResult<List<int>>.Success([.. results.Select(r => r.Value)]),
            options: options
        );

        IOperationResult<List<int>> result = forkJoin.Execute([1, 2, 3]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(10, 20, 30);
    }

    [Fact, Priority(8)]
    public void ForkJoin_WithMaxDegreeOfParallelism_ShouldLimit()
    {
        var options = new ForkJoinOptions { MaxDegreeOfParallelism = 1 };
        var forkJoin = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branch: n => OperationResult<int>.Success(n),
            join: results => OperationResult<int>.Success(results.Sum(r => r.Value)),
            options: options
        );

        IOperationResult<int> result = forkJoin.Execute([1, 2, 3]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(6);
    }

    [Fact, Priority(9)]
    public void ForkJoin_Simplified_ShouldProcessCollection()
    {
        var forkJoin = new ForkJoinOperation<string>(
            branch: s => OperationResult<string>.Success(s.ToUpper())
        );

        IOperationResult<IReadOnlyList<string>> result = forkJoin.Execute(["hello", "world"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("HELLO");
        result.Value.Should().Contain("WORLD");
    }

    [Fact, Priority(10)]
    public async Task ForkJoin_Simplified_Async_ShouldProcessCollection()
    {
        var forkJoin = new ForkJoinOperation<int>(
            branchAsync: async (n, ct) =>
            {
                await Task.Delay(1, ct);
                return OperationResult<int>.Success(n + 100);
            }
        );

        IOperationResult<IReadOnlyList<int>> result = await forkJoin.ExecuteAsync([1, 2, 3]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(101);
        result.Value.Should().Contain(102);
        result.Value.Should().Contain(103);
    }

    [Fact, Priority(11)]
    public void ForkJoin_EmptyInput_ShouldReturnEmptyResults()
    {
        var forkJoin = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branch: n => OperationResult<int>.Success(n),
            join: results => OperationResult<int>.Success(results.Count)
        );

        IOperationResult<int> result = forkJoin.Execute([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact, Priority(12)]
    public async Task ForkJoin_AsyncBranchTimeout_ShouldReturnFailureForSlow()
    {
        var options = new ForkJoinOptions { BranchTimeout = TimeSpan.FromMilliseconds(50) };
        var forkJoin = new ForkJoinOperation<IEnumerable<int>, int, int, int>(
            fork: inputs => inputs,
            branchAsync: async (n, ct) =>
            {
                if (n == 2)
                {
                    await Task.Delay(5000, ct); // very slow
                }
                return OperationResult<int>.Success(n);
            },
            join: results => OperationResult<int>.Success(results.Count(r => r.IsFailure)),
            options: options
        );

        IOperationResult<int> result = await forkJoin.ExecuteAsync([1, 2]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(1); // at least one branch should fail (timeout)
    }
}
