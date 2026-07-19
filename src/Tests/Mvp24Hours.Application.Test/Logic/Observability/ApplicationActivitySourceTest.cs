using System.Diagnostics;
using Mvp24Hours.Application.Logic.Observability;

namespace Mvp24Hours.Application.Test.Logic.Observability;

[Trait("Category", "Unit")]
public class ApplicationActivitySourceTest
{
    [Fact]
    public void StartQueryActivity_WithListener_ShouldSetTags()
    {
        using ActivityListener listener = CreateListener();
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = ApplicationActivitySource.StartQueryActivity("ProductService", "List", "Product");

        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == ApplicationActivitySource.TagNames.OperationType && t.Value == "Query");
        activity.Tags.Should().Contain(t => t.Key == ApplicationActivitySource.TagNames.EntityType && t.Value == "Product");
    }

    [Fact]
    public void StartCommandActivity_WithListener_ShouldSetCommandType()
    {
        using ActivityListener listener = CreateListener();
        ActivitySource.AddActivityListener(listener);

        using Activity? activity = ApplicationActivitySource.StartCommandActivity("OrderService", "Add", "Order");

        activity.Should().NotBeNull();
        activity!.Tags.Should().Contain(t => t.Key == ApplicationActivitySource.TagNames.OperationType && t.Value == "Command");
    }

    [Fact]
    public void SetCorrelationContext_ShouldApplyAllTags()
    {
        using ActivityListener listener = CreateListener();
        ActivitySource.AddActivityListener(listener);
        using Activity? activity = ApplicationActivitySource.StartQueryActivity("Svc", "Get");

        ApplicationActivitySource.SetCorrelationContext(activity, "corr", "cause", "user-1", "tenant-1");

        activity!.Tags.Should().Contain(t => t.Key == ApplicationActivitySource.TagNames.CorrelationId && t.Value == "corr");
        activity.Tags.Should().Contain(t => t.Key == ApplicationActivitySource.TagNames.TenantId && t.Value == "tenant-1");
    }

    [Fact]
    public void SetSuccess_ShouldMarkActivityOk()
    {
        using ActivityListener listener = CreateListener();
        ActivitySource.AddActivityListener(listener);
        using Activity? activity = ApplicationActivitySource.StartCommandActivity("Svc", "Modify");

        ApplicationActivitySource.SetSuccess(activity);

        activity!.GetTagItem(ApplicationActivitySource.TagNames.IsSuccess).Should().Be(true);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SetError_ShouldMarkActivityErrorAndAddEvent()
    {
        using ActivityListener listener = CreateListener();
        ActivitySource.AddActivityListener(listener);
        using Activity? activity = ApplicationActivitySource.StartCommandActivity("Svc", "Remove");
        var exception = new InvalidOperationException("boom");

        ApplicationActivitySource.SetError(activity, exception);

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.Events.Should().Contain(e => e.Name == "exception");
    }

    [Fact]
    public void RecordEvent_ShouldAddCustomEvent()
    {
        using ActivityListener listener = CreateListener();
        ActivitySource.AddActivityListener(listener);
        using Activity? activity = ApplicationActivitySource.StartQueryActivity("Svc", "Count");

        ApplicationActivitySource.RecordEvent(activity, "cache.miss", ("key", "products"));

        activity!.Events.Should().Contain(e => e.Name == "cache.miss");
    }

    private static ActivityListener CreateListener()
    {
        return new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApplicationActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
    }
}
