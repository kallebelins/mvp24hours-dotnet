//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Http.Resilience;
using Mvp24Hours.Infrastructure.Test.Support;
using Polly;
using Polly.Bulkhead;
using BulkheadPolicy = Mvp24Hours.Infrastructure.Http.Resilience.BulkheadPolicy;

namespace Mvp24Hours.Infrastructure.Test.Http.Resilience;

[Trait("Category", "Unit")]
public class BulkheadPolicyTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new BulkheadPolicy(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void PolicyName_ShouldBeBulkheadPolicy()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions());
        policy.PolicyName.Should().Be("BulkheadPolicy");
    }

    [Fact]
    public void GetPollyPolicy_ShouldReturnConfiguredPolicy()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions());
        IAsyncPolicy<HttpResponseMessage> polly = policy.GetPollyPolicy();
        polly.Should().NotBeNull();
    }

    [Fact]
    public void AvailableSlots_ShouldMatchOptionsInitially()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions
        {
            MaxParallelization = 3,
            MaxQueuedActions = 5
        });

        policy.AvailableParallelization.Should().Be(3);
        policy.AvailableQueueSlots.Should().Be(5);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequestFactory_ShouldThrowArgumentNullException()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            null!,
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("requestFactory");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullSendAsync_ShouldThrowArgumentNullException()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions());

        Func<Task> act = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("sendAsync");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldNotLimitConcurrency()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions
        {
            Enabled = false,
            MaxParallelization = 1,
            MaxQueuedActions = 0
        });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCapacityExceeded_ShouldReject()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions
        {
            MaxParallelization = 1,
            MaxQueuedActions = 0
        });

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task<HttpResponseMessage> first = policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            async (_, ct) =>
            {
                await gate.Task.WaitAsync(ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        // Give the first execution time to acquire the bulkhead slot
        await Task.Delay(50);

        Func<Task> second = () => policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.OK));

        await second.Should().ThrowAsync<BulkheadRejectedException>();

        gate.SetResult();
        HttpResponseMessage response = await first;
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ExecuteAsync_WithinCapacity_ShouldSucceed()
    {
        var policy = new BulkheadPolicy(new BulkheadPolicyOptions
        {
            MaxParallelization = 2,
            MaxQueuedActions = 1
        });

        HttpResponseMessage response = await policy.ExecuteAsync(
            ResilienceTestHelpers.RequestFactory(),
            ResilienceTestHelpers.RespondWith(HttpStatusCode.Accepted));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
