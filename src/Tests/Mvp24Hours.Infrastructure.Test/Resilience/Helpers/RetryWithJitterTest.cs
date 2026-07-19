//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Helpers;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Helpers;

[Trait("Category", "Unit")]
public class RetryWithJitterTest
{
    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        Func<Task> act = () => RetryWithJitter.ExecuteAsync<string>(
            null!,
            maxRetries: 1,
            initialDelay: TimeSpan.FromMilliseconds(1));

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldReturnWithoutRetry()
    {
        string result = await RetryWithJitter.ExecuteAsync(
            GenericResilienceTestHelpers.Succeed("ok"),
            maxRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(1));

        result.Should().Be("ok");
    }

    [Theory]
    [InlineData(JitterStrategy.Full)]
    [InlineData(JitterStrategy.Equal)]
    [InlineData(JitterStrategy.DecorrelatedJitter)]
    [InlineData(JitterStrategy.None)]
    public async Task ExecuteAsync_WithJitterStrategies_ShouldEventuallySucceed(JitterStrategy strategy)
    {
        int attempts = 0;

        string result = await RetryWithJitter.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts < 2)
                {
                    throw new TimeoutException("transient");
                }

                return Task.FromResult("ok");
            },
            maxRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(1),
            jitterStrategy: strategy,
            jitterFactor: 0.5);

        result.Should().Be("ok");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRetriesExhausted_ShouldThrow()
    {
        Func<Task> act = () => RetryWithJitter.ExecuteAsync(
            GenericResilienceTestHelpers.AlwaysFail<string>(new TimeoutException("fail")),
            maxRetries: 1,
            initialDelay: TimeSpan.FromMilliseconds(1),
            jitterStrategy: JitterStrategy.Full);

        await act.Should().ThrowAsync<TimeoutException>().WithMessage("fail");
    }

    [Fact]
    public async Task ExecuteAsync_WithCustomShouldRetry_ShouldRespectPredicate()
    {
        int attempts = 0;

        Func<Task> act = () => RetryWithJitter.ExecuteAsync(
            _ =>
            {
                attempts++;
                throw new InvalidOperationException("no retry");
            },
            maxRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(1),
            shouldRetryOnException: _ => false);

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_VoidOverload_ShouldWork()
    {
        int attempts = 0;

        await RetryWithJitter.ExecuteAsync(
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
            initialDelay: TimeSpan.FromMilliseconds(1),
            jitterStrategy: JitterStrategy.Equal);

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_VoidWithNullOperation_ShouldThrow()
    {
        Func<Task> act = () => RetryWithJitter.ExecuteAsync(
            null!,
            maxRetries: 1);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("operation");
    }
}
