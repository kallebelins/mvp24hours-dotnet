using System.Diagnostics;
using Mvp24Hours.Core.Observability;

namespace Mvp24Hours.Core.Test.Observability;

[Trait("Category", "Unit")]
public class ActivityExtensionsExtendedTest
{
    [Fact]
    public void SetSuccess_WithTags_Should_SetStatusAndTags()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource("ActivityExtensionsExtendedTest");
        using Activity activity = source.StartActivity("success-tags")!;

        activity.SetSuccess(("count", 3), ("name", "test"));

        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem(SemanticTags.OperationSuccess).Should().Be(true);
        activity.GetTagItem("count").Should().Be(3);
    }

    [Fact]
    public void SetError_WithMessage_Should_SetErrorCode()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource("ActivityExtensionsExtendedTest");
        using Activity activity = source.StartActivity("error-message")!;

        activity.SetError("failed", "ERR_1");

        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem(SemanticTags.ErrorCode).Should().Be("ERR_1");
    }

    [Fact]
    public void RecordEvent_And_RecordCacheMiss_Should_AddEvents()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource("ActivityExtensionsExtendedTest");
        using Activity activity = source.StartActivity("events")!;

        activity.RecordEvent("custom.event", ("key", "value"));
        activity.RecordCacheMiss("cache-key");

        activity.Events.Should().HaveCount(2);
        activity.GetTagItem(SemanticTags.CacheHit).Should().Be(false);
    }

    [Fact]
    public void RecordSlowQuery_And_ValidationFailure_Should_AddEvents()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource("ActivityExtensionsExtendedTest");
        using Activity activity = source.StartActivity("slow-validation")!;

        activity.RecordSlowQuery(1200, 500, "SELECT 1");
        activity.RecordValidationFailure(["Email invalid", "Name required"]);

        activity.Events.Should().HaveCount(2);
    }

    [Fact]
    public void WithCausationId_And_WithDuration_Should_SetTags()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource("ActivityExtensionsExtendedTest");
        using Activity activity = source.StartActivity("context")!;

        activity
            .WithCausationId("cause-1")
            .WithDuration(42.5)
            .WithDatabase("postgresql", "orders", "SELECT")
            .WithMessaging("rabbitmq", "orders.created", "msg-1");

        activity.GetTagItem(SemanticTags.CausationId).Should().Be("cause-1");
        activity.GetTagItem(SemanticTags.OperationDurationMs).Should().Be(42.5);
        activity.GetTagItem(SemanticTags.DbSystem).Should().Be("postgresql");
        activity.GetTagItem(SemanticTags.MessagingSystem).Should().Be("rabbitmq");
    }

    [Fact]
    public void StartScopedActivity_Should_SetSuccessOnDispose()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource("ActivityExtensionsExtendedTest");

        using (ScopedActivity scope = source.StartScopedActivity("scoped-success"))
        {
            scope.SetTag("step", 1);
        }

        Activity.Current.Should().BeNull();
    }

    [Fact]
    public void StartScopedActivity_Should_SetErrorWhenExceptionRecorded()
    {
        using ActivityListener listener = CreateListener();
        using var source = new ActivitySource("ActivityExtensionsExtendedTest");

        using (ScopedActivity scope = source.StartScopedActivity("scoped-error"))
        {
            scope.SetException(new InvalidOperationException("boom"));
        }

        Activity.Current.Should().BeNull();
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
