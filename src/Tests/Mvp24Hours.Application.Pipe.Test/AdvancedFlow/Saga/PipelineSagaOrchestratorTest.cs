using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Saga;

namespace Mvp24Hours.Application.Pipe.Test.AdvancedFlow.Saga;

[Trait("Category", "Unit")]
public class PipelineSagaOrchestratorTest
{
    [Fact]
    public async Task ExecuteAsync_Should_CompleteAllStepsSuccessfully()
    {
        var context = new TestSagaContext();
        PipelineSagaOrchestrator<TestSagaContext> saga = new PipelineSagaOrchestrator<TestSagaContext>()
            .AddStep("step1", (ctx, _) =>
            {
                ctx.Value = 1;
                return Task.FromResult(SagaStepResult.Success());
            })
            .AddStep("step2", (ctx, _) =>
            {
                ctx.Value += 2;
                return Task.FromResult(SagaStepResult.Success());
            });

        PipelineSagaResult<TestSagaContext> result = await saga.ExecuteAsync(context);

        result.IsSuccess.Should().BeTrue();
        result.State.Should().Be(SagaState.Completed);
        context.Value.Should().Be(3);
        result.StepResults.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CompensateCompletedStepsOnFailure()
    {
        var context = new TestSagaContext();
        var compensated = new List<string>();
        PipelineSagaOrchestrator<TestSagaContext> saga = new PipelineSagaOrchestrator<TestSagaContext>(new PipelineSagaOptions { AutoCompensateOnFailure = true })
            .AddStep("reserve", (ctx, _) =>
            {
                ctx.Log.Add("reserve");
                return Task.FromResult(SagaStepResult.Success());
            }, (ctx, _) =>
            {
                compensated.Add("reserve");
                return Task.FromResult(SagaStepResult.Success());
            })
            .AddStep("pay", (ctx, _) =>
            {
                ctx.Log.Add("pay");
                return Task.FromResult(SagaStepResult.Success());
            }, (ctx, _) =>
            {
                compensated.Add("pay");
                return Task.FromResult(SagaStepResult.Success());
            })
            .AddStep("ship", (_, _) => Task.FromResult(SagaStepResult.Failure("shipping failed")));

        PipelineSagaResult<TestSagaContext> result = await saga.ExecuteAsync(context);

        result.IsSuccess.Should().BeFalse();
        result.FailedStepId.Should().Be("ship");
        compensated.Should().Equal("pay", "reserve");
        result.CompensationResults.Should().HaveCount(2);
        result.State.Should().Be(SagaState.CompensationCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_Should_RetryStepBeforeFailing()
    {
        int attempts = 0;
        PipelineSagaOrchestrator<string> saga = new PipelineSagaOrchestrator<string>()
            .AddStep(new RetryingSagaStep(() => Interlocked.Increment(ref attempts)));

        PipelineSagaResult<string> result = await saga.ExecuteAsync("ctx");

        result.IsSuccess.Should().BeTrue();
        attempts.Should().Be(2);
    }

    [Fact]
    public void WithSagaId_Should_SetCustomId()
    {
        PipelineSagaOrchestrator<string> saga = new PipelineSagaOrchestrator<string>().WithSagaId("custom-id");

        saga.SagaId.Should().Be("custom-id");
    }

    private sealed class RetryingSagaStep(Func<int> attemptCounter) : PipelineSagaStepBase<string>("retry-step")
    {
        public override int MaxRetries => 1;
        public override TimeSpan RetryDelay => TimeSpan.Zero;

        public override Task<SagaStepResult> ExecuteAsync(string context, CancellationToken cancellationToken)
        {
            if (attemptCounter() == 1)
            {
                throw new InvalidOperationException("transient");
            }

            return Task.FromResult(SagaStepResult.Success());
        }
    }
}
