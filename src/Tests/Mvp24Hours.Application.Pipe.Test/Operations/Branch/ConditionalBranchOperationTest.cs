//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using Mvp24Hours.Infrastructure.Pipe.Operations.Branch;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Operations.Branch;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class ConditionalBranchOperationTest
{
    [Fact, Priority(1)]
    public void ConditionalBranch_MatchingCase_ShouldExecuteMatchedBranch()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "A");

        var branch = new ConditionalBranchOperation()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A",
                new SetContentOperation("branch-result", "A-branch"))
            .AddCase("caseB", msg => msg.GetContent<string>("type") == "B",
                new SetContentOperation("branch-result", "B-branch"));

        var pipeline = new Pipeline();
        pipeline.Add(branch);
        pipeline.Execute(input);

        pipeline.GetMessage().GetContent<string>("branch-result").Should().Be("A-branch");
    }

    [Fact, Priority(2)]
    public void ConditionalBranch_NoMatch_ShouldExecuteDefaultBranch()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "X");

        var branch = new ConditionalBranchOperation()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A",
                new SetContentOperation("result", "A"))
            .SetDefault(new SetContentOperation("result", "default"));

        var pipeline = new Pipeline();
        pipeline.Add(branch);
        pipeline.Execute(input);

        pipeline.GetMessage().GetContent<string>("result").Should().Be("default");
    }

    [Fact, Priority(3)]
    public void ConditionalBranch_NoMatchAndNoDefault_ShouldNotSetAnyContent()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "X");

        var branch = new ConditionalBranchOperation()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A",
                new SetContentOperation("result", "A"));

        var pipeline = new Pipeline();
        pipeline.Add(branch);
        pipeline.Execute(input);

        pipeline.GetMessage().HasContent("result").Should().BeFalse();
    }

    [Fact, Priority(4)]
    public void ConditionalBranch_EvaluateBranch_ShouldReturnMatchingKey()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "B");

        var branch = new ConditionalBranchOperation()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A",
                new SetContentOperation("r", "A"))
            .AddCase("caseB", msg => msg.GetContent<string>("type") == "B",
                new SetContentOperation("r", "B"));

        string key = branch.EvaluateBranch(input);

        key.Should().Be("caseB");
    }

    [Fact, Priority(5)]
    public void ConditionalBranch_AddCase_EmptyKey_ShouldThrow()
    {
        var branch = new ConditionalBranchOperation();

        Action act = () => branch.AddCase(string.Empty, _ => true, new SetContentOperation("r", "v"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact, Priority(6)]
    public void ConditionalBranch_AddCase_NullCondition_ShouldThrow()
    {
        var branch = new ConditionalBranchOperation();

        Action act = () => branch.AddCase("key", null!, new SetContentOperation("r", "v"));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(7)]
    public void ConditionalBranch_AddCase_EmptyOperations_ShouldThrow()
    {
        var branch = new ConditionalBranchOperation();

        Action act = () => branch.AddCase("key", _ => true);

        act.Should().Throw<ArgumentException>();
    }

    [Fact, Priority(8)]
    public void ConditionalBranch_Rollback_ShouldRollbackExecutedBranch()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "A");
        var rollbackOp = new RollbackTrackingOperation("A");

        var branch = new ConditionalBranchOperation()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A", rollbackOp);

        var pipeline = new Pipeline() { ForceRollbackOnFalure = true };
        pipeline.Add(branch);
        pipeline.Add(new FailOperation());
        pipeline.Execute(input);

        rollbackOp.RollbackCalled.Should().BeTrue();
    }

    [Fact, Priority(9)]
    public void ConditionalBranch_Branches_ShouldReturnAllAdded()
    {
        var branch = new ConditionalBranchOperation()
            .AddCase("case1", _ => true, new SetContentOperation("r", "v1"))
            .AddCase("case2", _ => false, new SetContentOperation("r", "v2"));

        branch.Branches.Should().HaveCount(2);
        branch.Branches.Should().ContainKey("case1");
        branch.Branches.Should().ContainKey("case2");
    }

    [Fact, Priority(10)]
    public void ConditionalBranch_DefaultBranch_ShouldBeAccessible()
    {
        var defOp = new SetContentOperation("r", "def");
        var branch = new ConditionalBranchOperation()
            .SetDefault(defOp);

        branch.DefaultBranch.Should().NotBeNull();
        branch.DefaultBranch!.Should().HaveCount(1);
    }

    [Fact, Priority(11)]
    public void ConditionalBranch_LockedMessage_ShouldSkipNonRequiredOperations()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "A");

        bool innerOpRan = false;
        var innerOp = new ActionOperation(_ => innerOpRan = true);

        var branch = new ConditionalBranchOperation()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A", innerOp);

        input.SetLock();
        branch.Execute(input);

        innerOpRan.Should().BeFalse();
    }

    // ─── Async branch ─────────────────────────────────────────────────────────

    [Fact, Priority(12)]
    public async Task ConditionalBranchAsync_MatchingCase_ShouldExecuteMatchedBranch()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "B");

        var branch = new ConditionalBranchOperationAsync()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A",
                new AsyncSetContentOperation("result", "A-async"))
            .AddCase("caseB", msg => msg.GetContent<string>("type") == "B",
                new AsyncSetContentOperation("result", "B-async"));

        var pipeline = new PipelineAsync();
        pipeline.Add(branch);
        await pipeline.ExecuteAsync(input);

        pipeline.GetMessage().GetContent<string>("result").Should().Be("B-async");
    }

    [Fact, Priority(13)]
    public async Task ConditionalBranchAsync_NoMatch_ShouldExecuteDefaultBranch()
    {
        IPipelineMessage input = new PipelineMessage();
        input.AddContent("type", "Z");

        var branch = new ConditionalBranchOperationAsync()
            .AddCase("caseA", msg => msg.GetContent<string>("type") == "A",
                new AsyncSetContentOperation("result", "A"))
            .SetDefault(new AsyncSetContentOperation("result", "default-async"));

        var pipeline = new PipelineAsync();
        pipeline.Add(branch);
        await pipeline.ExecuteAsync(input);

        pipeline.GetMessage().GetContent<string>("result").Should().Be("default-async");
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private class SetContentOperation(string key, string value) : OperationBase
    {
        public override void Execute(IPipelineMessage input) => input.AddContent(key, value);
        public override void Rollback(IPipelineMessage input) => input.AddContent($"{key}-rollback", true);
    }

    private class RollbackTrackingOperation(string tag) : OperationBase
    {
        public bool RollbackCalled { get; private set; }
        public override void Execute(IPipelineMessage input) => input.AddContent($"track-{tag}", true);
        public override void Rollback(IPipelineMessage input) => RollbackCalled = true;
    }

    private class FailOperation : OperationBase
    {
        public override void Execute(IPipelineMessage input) => input.SetFailure();
    }

    private class ActionOperation(Action<IPipelineMessage> action) : OperationBase
    {
        public override void Execute(IPipelineMessage input) => action(input);
    }

    private class AsyncSetContentOperation(string key, string value) : OperationBaseAsync
    {
        public override Task ExecuteAsync(IPipelineMessage input)
        {
            input.AddContent(key, value);
            return Task.CompletedTask;
        }
    }
}
