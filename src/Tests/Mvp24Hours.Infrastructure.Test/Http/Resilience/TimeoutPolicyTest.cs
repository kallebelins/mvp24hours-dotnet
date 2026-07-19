//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Http.Resilience;
using Mvp24Hours.Infrastructure.Test.Support;
using Polly;
using Polly.Timeout;
using TimeoutPolicy = Mvp24Hours.Infrastructure.Http.Resilience.TimeoutPolicy;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class TimeoutPolicyTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new TimeoutPolicy(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void PolicyName_ShouldBeTimeoutPolicy()
    {
        var policy = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions());
        policy.PolicyName.Should().Be("TimeoutPolicy");
    }

    [Fact]
    public void GetPollyPolicy_ShouldReturnConfiguredPolicy()
    {
        var policy = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions());
        IAsyncPolicy<HttpResponseMessage> polly = policy.GetPollyPolicy();
        polly.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequestFactory_ShouldThrowArgumentNullException()
    {
        var policy = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            null!,
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("requestFactory");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSendAsync_ShouldThrowArgumentNullException()
    {
        var policy = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sendAsync");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldIgnoreTimeout()
    {
        var policy = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions(
            timeout: TimeSpan.FromMilliseconds(20),
            enabled: false));

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Delay(TimeSpan.FromMilliseconds(50)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompletesWithinTimeout_ShouldReturnResponse()
    {
        var policy = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions(
            timeout: TimeSpan.FromSeconds(2)));

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExecuteAsync_WithOptimisticTimeout_ShouldThrowTimeoutRejectedException()
    {
        var policy = new TimeoutPolicy(
            ResilienceTestHelpers.CreateTimeoutOptions(timeout: TimeSpan.FromMilliseconds(50)),
            TimeoutStrategy.Optimistic);

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Delay(TimeSpan.FromSeconds(5)));

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithPessimisticTimeout_ShouldThrowTimeoutRejectedException()
    {
        var policy = new TimeoutPolicy(
            ResilienceTestHelpers.CreateTimeoutOptions(timeout: TimeSpan.FromMilliseconds(50)),
            TimeoutStrategy.Pessimistic);

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            async (_, _) =>
            {
                // Ignore cancellation to force hard timeout
                await Task.Delay(TimeSpan.FromSeconds(5));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }
}
