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
public class PolicyWrapTest
{
    [Fact]
    public void Constructor_WithNullPolicies_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new PolicyWrap((IEnumerable<IHttpResiliencePolicy>)null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("policies");
    }

    [Fact]
    public void Constructor_WithEmptyPolicies_ShouldThrowArgumentException()
    {
        Action act = () => _ = new PolicyWrap([]);
        act.Should().Throw<ArgumentException>().WithParameterName("policies");
    }

    [Fact]
    public void Constructor_WithSinglePolicy_ShouldExposePolicy()
    {
        var retry = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());
        var wrap = new PolicyWrap(retry);

        wrap.Policies.Should().ContainSingle().Which.Should().BeSameAs(retry);
        wrap.PolicyName.Should().Contain("RetryPolicy");
    }

    [Fact]
    public void PolicyName_ShouldIncludeInnerPolicyNames()
    {
        var retry = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());
        var timeout = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions());
        var wrap = new PolicyWrap([retry, timeout]);

        wrap.PolicyName.Should().Be("PolicyWrap(RetryPolicy, TimeoutPolicy)");
    }

    [Fact]
    public void GetPollyPolicy_ShouldReturnWrappedPolicy()
    {
        var wrap = PolicyWrap.Combine(
            new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions()),
            new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions()));

        IAsyncPolicy<HttpResponseMessage> polly = wrap.GetPollyPolicy();
        polly.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequestFactory_ShouldThrowArgumentNullException()
    {
        var wrap = new PolicyWrap(new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions()));

        Func<Task> act = () => wrap.ExecuteAsync(
            null!,
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("requestFactory");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSendAsync_ShouldThrowArgumentNullException()
    {
        var wrap = new PolicyWrap(new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions()));

        Func<Task> act = () => wrap.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sendAsync");
    }

    [Fact]
    public async Task ExecuteAsync_WithRetryAndTimeout_ShouldRetryUntilSuccess()
    {
        var wrap = PolicyWrap.Combine(
            new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(maxRetries: 3)),
            new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions(timeout: TimeSpan.FromSeconds(5))));

        int attempts = 0;
        HttpResponseMessage response = await wrap.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Counting(
                ResilienceTestHelpers.FailThenSucceed(2),
                _ => attempts++));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInnerTimesOut_ShouldSurfaceTimeout()
    {
        var wrap = PolicyWrap.Combine(
            new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(maxRetries: 0)),
            new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions(timeout: TimeSpan.FromMilliseconds(40))));

        Func<Task> act = () => wrap.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.Delay(TimeSpan.FromSeconds(5)));

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public void Wrap_ShouldAppendPolicy()
    {
        var retry = new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions());
        var timeout = new TimeoutPolicy(ResilienceTestHelpers.CreateTimeoutOptions());
        PolicyWrap wrap = new PolicyWrap(retry).Wrap(timeout);

        wrap.Policies.Should().HaveCount(2);
        wrap.Policies[0].Should().BeSameAs(retry);
        wrap.Policies[1].Should().BeSameAs(timeout);
    }

    [Fact]
    public void Wrap_WithNullPolicy_ShouldThrowArgumentNullException()
    {
        var wrap = new PolicyWrap(new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions()));
        Action act = () => wrap.Wrap(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("policy");
    }

    [Fact]
    public void Combine_WithNullOrEmpty_ShouldThrowArgumentException()
    {
        Action nullAct = () => PolicyWrap.Combine(null!);
        Action emptyAct = () => PolicyWrap.Combine();

        nullAct.Should().Throw<ArgumentException>().WithParameterName("policies");
        emptyAct.Should().Throw<ArgumentException>().WithParameterName("policies");
    }

    [Fact]
    public async Task ExecuteAsync_WithSinglePolicy_ShouldDelegate()
    {
        var wrap = new PolicyWrap(new RetryPolicy(ResilienceTestHelpers.CreateRetryOptions(maxRetries: 1)));

        HttpResponseMessage response = await wrap.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.FailThenSucceed(1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
