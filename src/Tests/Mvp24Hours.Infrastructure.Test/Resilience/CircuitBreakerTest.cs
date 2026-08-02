//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Exceptions;
using Mvp24Hours.Infrastructure.Resilience.Implementations;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Resilience;

#pragma warning disable CS0618 // Obsolete CircuitBreaker retained for coverage until NativeResiliencePipeline migration

[Trait("Category", "Unit")]
public class CircuitBreakerTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new CircuitBreaker<string>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void State_ShouldStartClosed()
    {
        var breaker = new CircuitBreaker<string>(GenericResilienceTestHelpers.CreateCircuitBreakerOptions());
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        var breaker = new CircuitBreaker<string>(GenericResilienceTestHelpers.CreateCircuitBreakerOptions());

        Func<Task> act = () => breaker.ExecuteAsync(
            (Func<object?, CancellationToken, Task<string>>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldKeepCircuitClosed()
    {
        var breaker = new CircuitBreaker<string>(
            GenericResilienceTestHelpers.CreateCircuitBreakerOptions(),
            "DemoOp");

        string result = await breaker.ExecuteAsync(_ => Task.FromResult("ok"));

        result.Should().Be("ok");
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_AfterEnoughFailures_ShouldOpenCircuit()
    {
        CircuitBreakerStateChangeInfo? breakInfo = null;
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 0.5,
            minimumThroughput: 2,
            breakDuration: TimeSpan.FromSeconds(30));
        options.OnBreak = info => breakInfo = info;

        var breaker = new CircuitBreaker<string>(options, "FailingOp");

        for (int i = 0; i < 2; i++)
        {
            Func<Task> act = () => breaker.ExecuteAsync(
                GenericResilienceTestHelpers.AlwaysFail<string>());
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        breaker.State.Should().Be(CircuitBreakerState.Open);
        breakInfo.Should().NotBeNull();
        breakInfo!.OperationName.Should().Be("FailingOp");
        breakInfo.NewState.Should().Be(CircuitBreakerState.Open);
        breakInfo.BreakDuration.Should().Be(TimeSpan.FromSeconds(30));

        Func<Task> blocked = () => breaker.ExecuteAsync(_ => Task.FromResult("ok"));
        await blocked.Should().ThrowAsync<CircuitBreakerOpenException>()
            .WithMessage("*Circuit breaker is open*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenShouldCountAsFailureReturnsFalse_ShouldNotOpen()
    {
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 0.5,
            minimumThroughput: 2);
        options.ShouldCountAsFailure = _ => false;

        var breaker = new CircuitBreaker<string>(options);

        for (int i = 0; i < 3; i++)
        {
            Func<Task> act = () => breaker.ExecuteAsync(
                GenericResilienceTestHelpers.AlwaysFail<string>());
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Isolate_ShouldOpenCircuitAndInvokeOnBreak()
    {
        CircuitBreakerStateChangeInfo? breakInfo = null;
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions();
        options.OnBreak = info => breakInfo = info;

        var breaker = new CircuitBreaker<string>(options, "Isolated");
        breaker.Isolate();

        breaker.State.Should().Be(CircuitBreakerState.Open);
        breakInfo.Should().NotBeNull();
        breakInfo!.Reason.Should().Be("Manually isolated");
        breakInfo.OperationName.Should().Be("Isolated");
    }

    [Fact]
    public void Isolate_WhenAlreadyOpen_ShouldNotInvokeOnBreakAgain()
    {
        int breakCount = 0;
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions();
        options.OnBreak = _ => breakCount++;

        var breaker = new CircuitBreaker<string>(options);
        breaker.Isolate();
        breaker.Isolate();

        breakCount.Should().Be(1);
    }

    [Fact]
    public void Reset_ShouldCloseCircuitAndInvokeOnReset()
    {
        CircuitBreakerStateChangeInfo? resetInfo = null;
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions();
        options.OnReset = info => resetInfo = info;

        var breaker = new CircuitBreaker<string>(options, "ResetOp");
        breaker.Isolate();
        breaker.Reset();

        breaker.State.Should().Be(CircuitBreakerState.Closed);
        resetInfo.Should().NotBeNull();
        resetInfo!.Reason.Should().Be("Manually reset");
        resetInfo.NewState.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Reset_WhenAlreadyClosed_ShouldNotInvokeOnReset()
    {
        int resetCount = 0;
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions();
        options.OnReset = _ => resetCount++;

        var breaker = new CircuitBreaker<string>(options);
        breaker.Reset();

        resetCount.Should().Be(0);
    }

    [Fact]
    public async Task State_AfterBreakDuration_ShouldTransitionToHalfOpen()
    {
        CircuitBreakerStateChangeInfo? halfOpenInfo = null;
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions(
            breakDuration: TimeSpan.FromMilliseconds(80));
        options.OnHalfOpen = info => halfOpenInfo = info;

        var breaker = new CircuitBreaker<string>(options, "HalfOpenOp");
        breaker.Isolate();
        breaker.State.Should().Be(CircuitBreakerState.Open);

        await Task.Delay(120);

        breaker.State.Should().Be(CircuitBreakerState.HalfOpen);
        halfOpenInfo.Should().NotBeNull();
        halfOpenInfo!.NewState.Should().Be(CircuitBreakerState.HalfOpen);
        halfOpenInfo.Reason.Should().Be("Break duration elapsed");
    }

    [Fact]
    public async Task ExecuteAsync_AfterSamplingExpires_SuccessShouldCloseFromHalfOpen()
    {
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions(
            failureRatio: 1.0,
            minimumThroughput: 2,
            samplingDuration: TimeSpan.FromMilliseconds(50),
            breakDuration: TimeSpan.FromMilliseconds(50));

        var breaker = new CircuitBreaker<string>(options);

        for (int i = 0; i < 2; i++)
        {
            Func<Task> act = () => breaker.ExecuteAsync(
                GenericResilienceTestHelpers.AlwaysFail<string>());
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        breaker.State.Should().Be(CircuitBreakerState.Open);

        await Task.Delay(120);

        string result = await breaker.ExecuteAsync(_ => Task.FromResult("recovered"));

        result.Should().Be("recovered");
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public async Task ExecuteAsync_WithContext_ShouldPassContextToOperation()
    {
        var breaker = new CircuitBreaker<int>(GenericResilienceTestHelpers.CreateCircuitBreakerOptions());
        object? received = null;

        int result = await breaker.ExecuteAsync(
            (ctx, _) =>
            {
                received = ctx;
                return Task.FromResult(7);
            },
            context: "payload");

        result.Should().Be(7);
        received.Should().Be("payload");
    }

    [Fact]
    public async Task VoidCircuitBreaker_ShouldDelegateToInner()
    {
        CircuitBreakerOptions options = GenericResilienceTestHelpers.CreateCircuitBreakerOptions();
        var breaker = new CircuitBreaker(options, "VoidOp");

        await breaker.ExecuteAsync(_ => Task.CompletedTask);
        breaker.State.Should().Be(CircuitBreakerState.Closed);

        breaker.Isolate();
        breaker.State.Should().Be(CircuitBreakerState.Open);

        Func<Task> blocked = () => breaker.ExecuteAsync(_ => Task.CompletedTask);
        await blocked.Should().ThrowAsync<CircuitBreakerOpenException>();

        breaker.Reset();
        breaker.State.Should().Be(CircuitBreakerState.Closed);
    }
}

#pragma warning restore CS0618
