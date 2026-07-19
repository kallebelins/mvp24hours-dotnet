//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Helpers;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Helpers;

[Trait("Category", "Unit")]
public class RetryHelperTest
{
    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        Func<Task> act = () => RetryHelper.ExecuteAsync<string>(
            null!,
            GenericResilienceTestHelpers.CreateRetryOptions());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Func<Task> act = () => RetryHelper.ExecuteAsync(
            GenericResilienceTestHelpers.Succeed("ok"),
            (RetryOptions)null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldReturnImmediately()
    {
        string result = await RetryHelper.ExecuteAsync(
            GenericResilienceTestHelpers.Succeed("ok"),
            GenericResilienceTestHelpers.CreateRetryOptions());

        result.Should().Be("ok");
    }

    [Fact]
    public async Task ExecuteAsync_WithParameters_ShouldRetryTransientFailures()
    {
        int attempts = 0;

        string result = await RetryHelper.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new TimeoutException("transient");
                }

                return Task.FromResult("ok");
            },
            maxRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(1),
            useExponentialBackoff: false);

        result.Should().Be("ok");
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNonTransient_ShouldNotRetry()
    {
        int attempts = 0;

        Func<Task> act = () => RetryHelper.ExecuteAsync(
            _ =>
            {
                attempts++;
                throw new InvalidOperationException("permanent");
            },
            maxRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(1));

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithConstantBackoff_ShouldInvokeOnRetryWithDelay()
    {
        var delays = new List<TimeSpan>();
        RetryOptions options = GenericResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(5),
            backoffType: RetryBackoffType.Constant);
        options.OnRetry = info => delays.Add(info.Delay);

        await RetryHelper.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceed(2, "ok"),
            options);

        delays.Should().HaveCount(2);
        delays.Should().AllSatisfy(d => d.Should().Be(TimeSpan.FromMilliseconds(5)));
    }

    [Fact]
    public async Task ExecuteAsync_WithLinearBackoff_ShouldIncreaseDelay()
    {
        var delays = new List<TimeSpan>();
        RetryOptions options = GenericResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(10),
            backoffType: RetryBackoffType.Linear,
            jitterFactor: 0);
        options.OnRetry = info => delays.Add(info.Delay);

        await RetryHelper.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceed(2, "ok"),
            options);

        delays.Should().HaveCount(2);
        delays[0].Should().Be(TimeSpan.FromMilliseconds(10));
        delays[1].Should().Be(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task ExecuteAsync_WithExponentialBackoff_ShouldDoubleDelay()
    {
        var delays = new List<TimeSpan>();
        RetryOptions options = GenericResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(10),
            backoffType: RetryBackoffType.Exponential,
            jitterFactor: 0);
        options.OnRetry = info => delays.Add(info.Delay);

        await RetryHelper.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceed(2, "ok"),
            options);

        delays.Should().HaveCount(2);
        delays[0].Should().Be(TimeSpan.FromMilliseconds(10));
        delays[1].Should().Be(TimeSpan.FromMilliseconds(20));
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxDelay_ShouldCapDelay()
    {
        var delays = new List<TimeSpan>();
        RetryOptions options = GenericResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 1,
            initialDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromMilliseconds(30),
            backoffType: RetryBackoffType.Exponential,
            jitterFactor: 0);
        options.OnRetry = info => delays.Add(info.Delay);

        await RetryHelper.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceed(1, "ok"),
            options);

        delays.Should().ContainSingle().Which.Should().Be(TimeSpan.FromMilliseconds(30));
    }

    [Fact]
    public async Task ExecuteAsync_WithDecorrelatedJitter_ShouldStayWithinMaxDelay()
    {
        var delays = new List<TimeSpan>();
        RetryOptions options = GenericResilienceTestHelpers.CreateRetryOptions(
            maxRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(10),
            maxDelay: TimeSpan.FromMilliseconds(50),
            backoffType: RetryBackoffType.DecorrelatedJitter,
            jitterFactor: 1.0);
        options.OnRetry = info => delays.Add(info.Delay);

        await RetryHelper.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceed(2, "ok"),
            options);

        delays.Should().HaveCount(2);
        delays.Should().AllSatisfy(d => d.Should().BeLessThanOrEqualTo(TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public async Task ExecuteAsync_VoidOverloads_ShouldWork()
    {
        int attempts = 0;

        await RetryHelper.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new TimeoutException();
                }

                return Task.CompletedTask;
            },
            maxRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(1));

        attempts.Should().Be(2);

        attempts = 0;
        await RetryHelper.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new InvalidOperationException();
                }

                return Task.CompletedTask;
            },
            GenericResilienceTestHelpers.CreateRetryOptions(maxRetries: 1));

        attempts.Should().Be(2);
    }

    [Fact]
    public void IsTransientException_ShouldRecognizeTimeoutAndNested()
    {
        RetryHelper.IsTransientException(null!).Should().BeFalse();
        RetryHelper.IsTransientException(new TimeoutException()).Should().BeTrue();
        RetryHelper.IsTransientException(new OperationCanceledException()).Should().BeTrue();
        RetryHelper.IsTransientException(new InvalidOperationException()).Should().BeFalse();
        RetryHelper.IsTransientException(
            new Exception("wrap", new TimeoutException())).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithNullOperation_ShouldThrow()
    {
        Func<Task> act1 = () => RetryHelper.ExecuteAsync(
            null!,
            maxRetries: 1);

        Func<Task> act2 = () => RetryHelper.ExecuteAsync(
            null!,
            GenericResilienceTestHelpers.CreateRetryOptions());

        await act1.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
        await act2.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }
}
