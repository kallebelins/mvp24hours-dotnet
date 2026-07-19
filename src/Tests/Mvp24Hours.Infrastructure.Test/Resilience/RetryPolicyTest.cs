//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Implementations;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.Resilience;

#pragma warning disable CS0618 // Obsolete RetryPolicy retained for coverage until NativeResiliencePipeline migration

[Trait("Category", "Unit")]
public class RetryPolicyTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new RetryPolicy<string>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public async Task ExecuteAsync_OnSuccess_ShouldReturnResultWithoutRetry()
    {
        int attempts = 0;
        var policy = new RetryPolicy<string>(GenericResilienceTestHelpers.CreateRetryOptions());

        string result = await policy.ExecuteAsync(_ =>
        {
            attempts++;
            return Task.FromResult("ok");
        });

        result.Should().Be("ok");
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithTransientFailures_ShouldRetryAndSucceed()
    {
        var retries = new List<RetryAttemptInfo>();
        RetryOptions options = GenericResilienceTestHelpers.CreateRetryOptions(maxRetries: 3);
        options.OnRetry = info => retries.Add(info);

        var policy = new RetryPolicy<string>(options);
        string result = await policy.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceed(2, "recovered"));

        result.Should().Be("recovered");
        retries.Should().HaveCount(2);
        retries[0].AttemptNumber.Should().Be(1);
        retries[1].AttemptNumber.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRetriesExhausted_ShouldThrowAfterAllAttempts()
    {
        var retries = new List<RetryAttemptInfo>();
        RetryOptions options = GenericResilienceTestHelpers.CreateRetryOptions(maxRetries: 2);
        options.OnRetry = info => retries.Add(info);

        var policy = new RetryPolicy<string>(options);

        Func<Task> act = () => policy.ExecuteAsync(
            GenericResilienceTestHelpers.AlwaysFail<string>());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("always fail");
        // Initial attempt + 2 retries recorded via OnRetry (final failure escapes without OnRetryExhausted).
        retries.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithContext_ShouldPassContext()
    {
        var policy = new RetryPolicy<int>(GenericResilienceTestHelpers.CreateRetryOptions());
        object? received = null;

        int result = await policy.ExecuteAsync(
            (ctx, _) =>
            {
                received = ctx;
                return Task.FromResult(5);
            },
            context: "ctx");

        result.Should().Be(5);
        received.Should().Be("ctx");
    }

    [Fact]
    public async Task Constructor_WithDefaultParameters_ShouldRetry()
    {
        var policy = new RetryPolicy<string>(
            maxRetries: 2,
            initialDelay: TimeSpan.FromMilliseconds(1),
            useExponentialBackoff: false);

        // Default ShouldRetryOnException is null → IsTransientException only.
        // TimeoutException is considered transient.
        string result = await policy.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceed(1, "ok", new TimeoutException("slow")));

        result.Should().Be("ok");
    }

    [Fact]
    public async Task VoidRetryPolicy_ShouldDelegateToInner()
    {
        int attempts = 0;
        var policy = new RetryPolicy(GenericResilienceTestHelpers.CreateRetryOptions(maxRetries: 2));

        await policy.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts < 2)
            {
                throw new InvalidOperationException("retry me");
            }

            return Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public async Task VoidRetryPolicy_WithDefaultConstructor_ShouldSucceed()
    {
        var policy = new RetryPolicy(
            maxRetries: 1,
            initialDelay: TimeSpan.FromMilliseconds(1));

        await policy.ExecuteAsync(
            GenericResilienceTestHelpers.FailThenSucceedVoid(1, new TimeoutException()));
    }

    [Fact]
    public async Task VoidRetryPolicy_WithContext_ShouldPassContext()
    {
        var policy = new RetryPolicy(GenericResilienceTestHelpers.CreateRetryOptions());
        object? received = null;

        await policy.ExecuteAsync(
            (ctx, _) =>
            {
                received = ctx;
                return Task.CompletedTask;
            },
            context: 42);

        received.Should().Be(42);
    }
}

#pragma warning restore CS0618
