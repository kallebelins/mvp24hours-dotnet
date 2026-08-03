using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Checkpoint;
using Mvp24Hours.Infrastructure.Pipe.Typed;

namespace Mvp24Hours.Application.Pipe.Test.AdvancedFlow.Checkpoint;

[Trait("Category", "Unit")]
public class CheckpointablePipelineTest
{
    private sealed class TestCheckpointState
    {
        public int Value { get; set; }

        public List<string> Log { get; init; } = [];
    }

    private static CheckpointablePipeline<TestCheckpointState> CreatePipeline(
        InMemoryCheckpointStore store,
        CheckpointOptions? options = null)
    {
        return new CheckpointablePipeline<TestCheckpointState>("test-pipeline", store, options);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenPipelineNameIsNull()
    {
        var store = new InMemoryCheckpointStore();

        Action act = () => new CheckpointablePipeline<TestCheckpointState>(null!, store);

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipelineName");
    }

    [Fact]
    public void Constructor_Should_ThrowWhenCheckpointStoreIsNull()
    {
        Action act = () => new CheckpointablePipeline<TestCheckpointState>("pipeline", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("checkpointStore");
    }

    [Fact]
    public async Task ExecuteAsync_Should_RunSyncStepsAndReturnFinalState()
    {
        var store = new InMemoryCheckpointStore();
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, new CheckpointOptions { Enabled = false })
            .AddStep("add", state =>
            {
                state.Value += 1;
                state.Log.Add("add");
                return OperationResult<TestCheckpointState>.Success(state);
            })
            .AddStep("double", state =>
            {
                state.Value *= 2;
                state.Log.Add("double");
                return OperationResult<TestCheckpointState>.Success(state);
            });

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState { Value = 3 });

        result.IsSuccess.Should().BeTrue();
        result.State!.Value.Should().Be(8);
        result.StepResults.Should().HaveCount(2);
        result.StepResults.Select(s => s.StepId).Should().Equal("add", "double");
        result.PipelineExecutionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Should_RunAsyncSteps()
    {
        var store = new InMemoryCheckpointStore();
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, new CheckpointOptions { Enabled = false })
            .AddStep("async-add", async (state, ct) =>
            {
                await Task.Delay(1, ct);
                state.Value += 5;
                return OperationResult<TestCheckpointState>.Success(state);
            }, name: "Async Add");

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState());

        result.IsSuccess.Should().BeTrue();
        result.State!.Value.Should().Be(5);
        result.StepResults.Single().StepName.Should().Be("Async Add");
    }

    [Fact]
    public async Task ExecuteAsync_Should_StopOnStepFailure()
    {
        var store = new InMemoryCheckpointStore();
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, new CheckpointOptions { Enabled = false })
            .AddStep("ok", state => OperationResult<TestCheckpointState>.Success(state))
            .AddStep("fail", _ => OperationResult<TestCheckpointState>.Failure("step failed"));

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState());

        result.IsSuccess.Should().BeFalse();
        result.FailedStepId.Should().Be("fail");
        result.ErrorMessage.Should().Contain("step failed");
        result.StepResults.Should().HaveCount(2);
        result.StepResults.Last().IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_Should_HandleStepException()
    {
        var store = new InMemoryCheckpointStore();
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, new CheckpointOptions { Enabled = false })
            .AddStep("throw", _ => throw new InvalidOperationException("boom"));

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState());

        result.IsSuccess.Should().BeFalse();
        result.FailedStepId.Should().Be("throw");
        result.ErrorMessage.Should().Contain("boom");
    }

    [Fact]
    public async Task ExecuteAsync_Should_SaveErrorCheckpointWhenConfigured()
    {
        var store = new InMemoryCheckpointStore();
        var options = new CheckpointOptions
        {
            Enabled = true,
            CleanupOnSuccess = false,
            CheckpointOnError = true
        };
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, options)
            .AddStep("fail", _ => OperationResult<TestCheckpointState>.Failure("processing error"));

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState { Value = 9 });

        result.IsSuccess.Should().BeFalse();
        IReadOnlyList<PipelineCheckpoint> checkpoints = await store.GetCheckpointsAsync(result.PipelineExecutionId!);
        checkpoints.Should().Contain(c => c.Status == CheckpointStatus.Failed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CleanupCheckpointsOnSuccess()
    {
        var store = new InMemoryCheckpointStore();
        var options = new CheckpointOptions
        {
            Enabled = true,
            CleanupOnSuccess = true
        };
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, options)
            .AddStep("done", state => OperationResult<TestCheckpointState>.Success(state));

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState());

        result.IsSuccess.Should().BeTrue();
        store.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnCancelledResult()
    {
        var store = new InMemoryCheckpointStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, new CheckpointOptions { Enabled = true })
            .AddStep("wait", async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return OperationResult<TestCheckpointState>.Success(new TestCheckpointState());
            });

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState(), cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.WasCancelled.Should().BeTrue();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task ResumeAsync_Should_ReturnFailureWhenCheckpointMissing()
    {
        var store = new InMemoryCheckpointStore();
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store);

        CheckpointableResult<TestCheckpointState> result = await pipeline.ResumeAsync("missing-exec");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No checkpoint found");
        result.PipelineExecutionId.Should().Be("missing-exec");
    }

    [Fact]
    public async Task ResumeAsync_Should_ReturnFailureWhenCheckpointNotResumable()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(new PipelineCheckpoint
        {
            CheckpointId = "cp-completed",
            PipelineExecutionId = "exec-completed",
            PipelineName = "test-pipeline",
            StepIndex = 0,
            Status = CheckpointStatus.Completed,
            CreatedAt = DateTime.UtcNow
        });

        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store);
        CheckpointableResult<TestCheckpointState> result = await pipeline.ResumeAsync("exec-completed");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not in a resumable state");
    }

    [Fact]
    public async Task ResumeAsync_Should_ReturnFailureWhenCheckpointExpired()
    {
        var store = new InMemoryCheckpointStore();
        var serializer = new JsonStateSerializer();
        var state = new TestCheckpointState { Value = 1 };
        await store.SaveCheckpointAsync(new PipelineCheckpoint
        {
            CheckpointId = "cp-expired",
            PipelineExecutionId = "exec-expired",
            PipelineName = "test-pipeline",
            StepIndex = 0,
            StateData = serializer.Serialize(state),
            Status = CheckpointStatus.Paused,
            CreatedAt = DateTime.UtcNow.AddHours(-3)
        });

        var options = new CheckpointOptions { CheckpointExpiration = TimeSpan.FromHours(1) };
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, options);

        CheckpointableResult<TestCheckpointState> result = await pipeline.ResumeAsync("exec-expired");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("expired");
        PipelineCheckpoint? updated = await store.GetCheckpointAsync("cp-expired");
        updated!.Status.Should().Be(CheckpointStatus.Expired);
    }

    [Fact]
    public async Task ResumeAsync_Should_ReturnFailureWhenStateCannotBeDeserialized()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(new PipelineCheckpoint
        {
            CheckpointId = "cp-empty-state",
            PipelineExecutionId = "exec-empty-state",
            PipelineName = "test-pipeline",
            StepIndex = 0,
            StateData = string.Empty,
            Status = CheckpointStatus.Failed,
            CreatedAt = DateTime.UtcNow
        });

        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store);
        CheckpointableResult<TestCheckpointState> result = await pipeline.ResumeAsync("exec-empty-state");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to deserialize checkpoint state");
    }

    [Fact]
    public async Task ResumeAsync_Should_ContinueFromNextStep()
    {
        var store = new InMemoryCheckpointStore();
        var serializer = new JsonStateSerializer();
        const string executionId = "exec-resume";
        var resumedState = new TestCheckpointState { Value = 10, Log = ["step1"] };
        await store.SaveCheckpointAsync(new PipelineCheckpoint
        {
            CheckpointId = "cp-resume",
            PipelineExecutionId = executionId,
            PipelineName = "test-pipeline",
            StepIndex = 0,
            StepId = "step1",
            StateData = serializer.Serialize(resumedState),
            Status = CheckpointStatus.Paused,
            CreatedAt = DateTime.UtcNow
        });

        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, new CheckpointOptions { Enabled = false })
            .AddStep("step1", state =>
            {
                state.Log.Add("step1-should-not-run");
                return OperationResult<TestCheckpointState>.Success(state);
            })
            .AddStep("step2", state =>
            {
                state.Value += 2;
                state.Log.Add("step2");
                return OperationResult<TestCheckpointState>.Success(state);
            });

        CheckpointableResult<TestCheckpointState> result = await pipeline.ResumeAsync(executionId);

        result.IsSuccess.Should().BeTrue();
        result.State!.Value.Should().Be(12);
        result.State.Log.Should().Equal("step1", "step2");
        result.StepResults.Should().ContainSingle(s => s.StepId == "step2");
        PipelineCheckpoint? updated = await store.GetCheckpointAsync("cp-resume");
        updated!.Status.Should().Be(CheckpointStatus.Resumed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_SaveCheckpointsWhenEnabled()
    {
        var store = new InMemoryCheckpointStore();
        var options = new CheckpointOptions
        {
            Enabled = true,
            CleanupOnSuccess = false,
            CheckpointInterval = 1
        };
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store, options)
            .AddStep("step1", state =>
            {
                state.Value += 1;
                state.Log.Add("step1");
                return OperationResult<TestCheckpointState>.Success(state);
            })
            .AddStep("step2", state =>
            {
                state.Value += 2;
                state.Log.Add("step2");
                return OperationResult<TestCheckpointState>.Success(state);
            });

        CheckpointableResult<TestCheckpointState> result = await pipeline.ExecuteAsync(new TestCheckpointState());

        result.IsSuccess.Should().BeTrue();
        result.State!.Value.Should().Be(3);
        IReadOnlyList<PipelineCheckpoint> checkpoints = await store.GetCheckpointsAsync(result.PipelineExecutionId!);
        checkpoints.Should().NotBeEmpty();
        checkpoints.Should().Contain(c => c.StepId == "step1");
    }

    [Fact]
    public async Task PauseAsync_Should_UpdateRunningCheckpointToPaused()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(new PipelineCheckpoint
        {
            CheckpointId = "cp-running",
            PipelineExecutionId = "exec-pause",
            PipelineName = "test-pipeline",
            StepIndex = 1,
            Status = CheckpointStatus.Running,
            CreatedAt = DateTime.UtcNow
        });

        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store);
        bool paused = await pipeline.PauseAsync("exec-pause");

        paused.Should().BeTrue();
        PipelineCheckpoint? updated = await store.GetCheckpointAsync("cp-running");
        updated!.Status.Should().Be(CheckpointStatus.Paused);
    }

    [Fact]
    public async Task PauseAsync_Should_ReturnFalseWhenNoRunningCheckpoint()
    {
        var store = new InMemoryCheckpointStore();
        CheckpointablePipeline<TestCheckpointState> pipeline = CreatePipeline(store);

        bool paused = await pipeline.PauseAsync("exec-missing");

        paused.Should().BeFalse();
    }
}
