//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Observability.Helpers;
using Mvp24Hours.Infrastructure.Testing.Logging;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Test.Observability;

[Trait("Category", "Unit")]
public class ObservabilityHelperTest
{
    private readonly FakeLogger<ObservabilityHelperTest> _logger = new();
    private readonly ActivitySource _activitySource = new("Test.ObservabilityHelper", "1.0.0");

    [Fact]
    public async Task ExecuteWithObservabilityAsync_OnSuccess_ShouldReturnResultAndRecordSuccessMetrics()
    {
        using var listener = new FakeActivityListener("Test.ObservabilityHelper");
        bool? success = null;
        double duration = -1;

        int result = await ObservabilityHelper.ExecuteWithObservabilityAsync(
            _logger,
            _activitySource,
            "async-success",
            _ => Task.FromResult(42),
            (s, d) =>
            {
                success = s;
                duration = d;
            });

        result.Should().Be(42);
        success.Should().BeTrue();
        duration.Should().BeGreaterThanOrEqualTo(0);
        listener.HasActivity("async-success").Should().BeTrue();
        _logger.ContainsLog(LogLevel.Information, "completed successfully").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteWithObservabilityAsync_OnFailure_ShouldRethrowAndRecordFailureMetrics()
    {
        using var listener = new FakeActivityListener("Test.ObservabilityHelper");
        bool? success = null;
        var expected = new InvalidOperationException("async failed");

        Func<Task> act = () => ObservabilityHelper.ExecuteWithObservabilityAsync<int>(
            _logger,
            _activitySource,
            "async-failure",
            _ => throw expected,
            (s, _) => success = s);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("async failed");
        success.Should().BeFalse();
        // FakeActivityListener records string Tags; assert string error tags set by the helper
        RecordedActivity activity = listener.GetActivities("async-failure").Single();
        activity.GetTag("error.message").Should().Be("async failed");
        activity.GetTag("error.type").Should().Be(nameof(InvalidOperationException));
        _logger.ContainsException<InvalidOperationException>().Should().BeTrue();
    }

    [Fact]
    public void ExecuteWithObservability_OnSuccess_ShouldReturnResultAndRecordSuccessMetrics()
    {
        using var listener = new FakeActivityListener("Test.ObservabilityHelper");
        bool? success = null;

        string result = ObservabilityHelper.ExecuteWithObservability(
            _logger,
            _activitySource,
            "sync-success",
            () => "done",
            (s, _) => success = s);

        result.Should().Be("done");
        success.Should().BeTrue();
        listener.HasActivity("sync-success").Should().BeTrue();
    }

    [Fact]
    public void ExecuteWithObservability_OnFailure_ShouldRethrowAndRecordFailureMetrics()
    {
        using var listener = new FakeActivityListener("Test.ObservabilityHelper");
        bool? success = null;
        var expected = new InvalidOperationException("sync failed");

        Action act = () => ObservabilityHelper.ExecuteWithObservability<int>(
            _logger,
            _activitySource,
            "sync-failure",
            () => throw expected,
            (s, _) => success = s);

        act.Should().Throw<InvalidOperationException>().WithMessage("sync failed");
        success.Should().BeFalse();
        RecordedActivity activity = listener.GetActivities("sync-failure").Single();
        activity.HasError.Should().BeFalse(); // helper sets tags, not ActivityStatusCode.Error
        activity.GetTag("error.message").Should().Be("sync failed");
        activity.GetTag("error.type").Should().Be(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task ExecuteWithObservabilityAsync_VoidOverload_ShouldComplete()
    {
        using var listener = new FakeActivityListener("Test.ObservabilityHelper");
        bool executed = false;

        await ObservabilityHelper.ExecuteWithObservabilityAsync(
            _logger,
            _activitySource,
            "void-async",
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        executed.Should().BeTrue();
        listener.HasActivity("void-async").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteWithObservabilityAsync_ShouldForwardCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;

        await ObservabilityHelper.ExecuteWithObservabilityAsync(
            _logger,
            _activitySource,
            "token-forward",
            token =>
            {
                capturedToken = token;
                return Task.FromResult(true);
            },
            cancellationToken: cts.Token);

        capturedToken.Should().Be(cts.Token);
    }
}
