using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Saga;
using Mvp24Hours.Infrastructure.Testing.Logging;

namespace Mvp24Hours.Application.Pipe.Test.AdvancedFlow.Saga;

[Trait("Category", "Unit")]
public class PipelineSagaOrchestratorExtendedTest
{
    [Fact]
    public async Task ExecuteAsync_WithNoSteps_ShouldCompleteSuccessfully()
    {
        var saga = new PipelineSagaOrchestrator<string>();

        PipelineSagaResult<string> result = await saga.ExecuteAsync("ctx");

        result.IsSuccess.Should().BeTrue();
        result.State.Should().Be(SagaState.Completed);
        result.StepResults.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithStepFailureResult_ShouldFailWithoutRetry()
    {
        int attempts = 0;
        PipelineSagaOrchestrator<string> saga = new PipelineSagaOrchestrator<string>()
            .AddStep("fail", (_, _) =>
            {
                attempts++;
                return Task.FromResult(SagaStepResult.Failure("step failed"));
            });

        PipelineSagaResult<string> result = await saga.ExecuteAsync("ctx");

        result.IsSuccess.Should().BeFalse();
        result.FailedStepId.Should().Be("fail");
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithAutoCompensateDisabled_ShouldNotCompensate()
    {
        bool compensated = false;
        PipelineSagaOrchestrator<TestSagaContext> saga = new PipelineSagaOrchestrator<TestSagaContext>(new PipelineSagaOptions { AutoCompensateOnFailure = false })
            .AddStep("first", (_, _) => Task.FromResult(SagaStepResult.Success()), (_, _) =>
            {
                compensated = true;
                return Task.FromResult(SagaStepResult.Success());
            })
            .AddStep("second", (_, _) => Task.FromResult(SagaStepResult.Failure("boom")));

        PipelineSagaResult<TestSagaContext> result = await saga.ExecuteAsync(new TestSagaContext());

        result.State.Should().Be(SagaState.Failed);
        compensated.Should().BeFalse();
        result.CompensationResults.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompensationFailsAndContinueOnError_ShouldMarkCompensationFailed()
    {
        PipelineSagaOrchestrator<string> saga = new PipelineSagaOrchestrator<string>(new PipelineSagaOptions
        {
            AutoCompensateOnFailure = true,
            ContinueCompensationOnError = true
        })
            .AddStep("first", (_, _) => Task.FromResult(SagaStepResult.Success()), (_, _) =>
                Task.FromResult(SagaStepResult.Failure("comp failed")))
            .AddStep("second", (_, _) => Task.FromResult(SagaStepResult.Failure("step failed")));

        PipelineSagaResult<string> result = await saga.ExecuteAsync("ctx");

        result.State.Should().Be(SagaState.CompensationFailed);
        result.CompensationResults.Should().ContainSingle(r => !r.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompensationThrowsAndStopOnError_ShouldStopCompensation()
    {
        PipelineSagaOrchestrator<string> saga = new PipelineSagaOrchestrator<string>(new PipelineSagaOptions
        {
            AutoCompensateOnFailure = true,
            ContinueCompensationOnError = false
        })
            .AddStep("first", (_, _) => Task.FromResult(SagaStepResult.Success()), (_, _) =>
                throw new InvalidOperationException("comp exception"))
            .AddStep("second", (_, _) => Task.FromResult(SagaStepResult.Success()), (_, _) =>
                Task.FromResult(SagaStepResult.Success()))
            .AddStep("third", (_, _) => Task.FromResult(SagaStepResult.Failure("step failed")));

        PipelineSagaResult<string> result = await saga.ExecuteAsync("ctx");

        result.State.Should().Be(SagaState.CompensationFailed);
        result.CompensationResults.Should().HaveCount(2);
        result.CompensationResults.Should().Contain(r => !r.IsSuccess);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ShouldReturnCancelledResult()
    {
        using var cts = new CancellationTokenSource();
        PipelineSagaOrchestrator<string> saga = new PipelineSagaOrchestrator<string>()
            .AddStep("slow", async (_, token) =>
            {
                cts.Cancel();
                token.ThrowIfCancellationRequested();
                return SagaStepResult.Success();
            });

        PipelineSagaResult<string> result = await saga.ExecuteAsync("ctx", cts.Token);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task ExecuteAsync_WithStatePersistence_ShouldSaveAndDeleteOnSuccess()
    {
        var store = new InMemorySagaStateStore<TestSagaContext>();
        PipelineSagaOrchestrator<TestSagaContext> saga = new PipelineSagaOrchestrator<TestSagaContext>(
            new PipelineSagaOptions { EnableStatePersistence = true },
            store).WithSagaId("persisted-saga")
            .AddStep("only", (_, _) => Task.FromResult(SagaStepResult.Success()));

        PipelineSagaResult<TestSagaContext> result = await saga.ExecuteAsync(new TestSagaContext());

        result.IsSuccess.Should().BeTrue();
        store.Count.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatePersistenceOnFailure_ShouldKeepState()
    {
        var store = new InMemorySagaStateStore<string>();
        PipelineSagaOrchestrator<string> saga = new PipelineSagaOrchestrator<string>(
            new PipelineSagaOptions { EnableStatePersistence = true },
            store).WithSagaId("failed-saga")
            .AddStep("fail", (_, _) => Task.FromResult(SagaStepResult.Failure("failed")));

        PipelineSagaResult<string> result = await saga.ExecuteAsync("ctx");

        result.IsSuccess.Should().BeFalse();
        store.Count.Should().BeGreaterThan(0);
        (await store.LoadStateAsync("failed-saga")).Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithStepDelay_ShouldExecuteAllSteps()
    {
        var order = new List<int>();
        PipelineSagaOrchestrator<int> saga = new PipelineSagaOrchestrator<int>(new PipelineSagaOptions { StepDelay = TimeSpan.FromMilliseconds(1) })
            .AddStep("one", (ctx, _) => { order.Add(ctx); return Task.FromResult(SagaStepResult.Success()); })
            .AddStep("two", (ctx, _) => { order.Add(ctx + 1); return Task.FromResult(SagaStepResult.Success()); });

        PipelineSagaResult<int> result = await saga.ExecuteAsync(1);

        result.IsSuccess.Should().BeTrue();
        order.Should().Equal(1, 2);
    }

    [Fact]
    public void AddStep_WithNullStep_ShouldThrowArgumentNullException()
    {
        var saga = new PipelineSagaOrchestrator<string>();

        Action act = () => saga.AddStep((IPipelineSagaStep<string>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PipelineSagaStepBase_DefaultCompensate_ShouldReturnSuccess()
    {
        var step = new DefaultCompensatingStep();

        SagaStepResult result = await step.CompensateAsync("ctx", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InMemorySagaStateStore_ShouldSaveLoadAndDelete()
    {
        var store = new InMemorySagaStateStore<string>();
        var state = new SagaPersistedState<string>
        {
            SagaId = "saga-1",
            Context = "ctx",
            State = SagaState.Running
        };

        await store.SaveStateAsync("saga-1", state);
        store.Count.Should().Be(1);

        SagaPersistedState<string>? loaded = await store.LoadStateAsync("saga-1");
        loaded!.Context.Should().Be("ctx");

        await store.DeleteStateAsync("saga-1");
        store.Count.Should().Be(0);
    }

    private sealed class DefaultCompensatingStep : PipelineSagaStepBase<string>
    {
        public DefaultCompensatingStep()
            : base("default")
        {
        }

        public override Task<SagaStepResult> ExecuteAsync(string context, CancellationToken cancellationToken)
        {
            return Task.FromResult(SagaStepResult.Success());
        }
    }
}
