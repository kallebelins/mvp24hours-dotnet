using System.Diagnostics;
using Mvp24Hours.Core.Observability;

namespace Mvp24Hours.Core.Test.Observability;

[Trait("Category", "Unit")]
public class TracePropagationTest
{
    [Fact]
    public void InjectTraceContext_Should_AddW3CHeaders()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("TracePropagationTest");
        using Activity? activity = source.StartActivity("inject");
        activity!.SetBaggage("correlation.id", "corr-123");

        var headers = new Dictionary<string, string>();
        TracePropagation.InjectTraceContext(headers, activity);

        headers.Should().ContainKey(TracePropagation.TraceparentHeader);
        headers.Should().ContainKey(TracePropagation.BaggageHeader);
        headers[TracePropagation.CorrelationIdHeader].Should().Be("corr-123");
    }

    [Fact]
    public void ExtractTraceContext_Should_ParseTraceparentAndBaggage()
    {
        var headers = new Dictionary<string, string?>
        {
            [TracePropagation.TraceparentHeader] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            [TracePropagation.TracestateHeader] = "vendor=value",
            [TracePropagation.BaggageHeader] = "correlation.id=corr-999",
            [TracePropagation.CorrelationIdHeader] = "corr-999"
        };

        TraceContext? context = TracePropagation.ExtractTraceContext(headers);

        context.Should().NotBeNull();
        context!.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
        context.SpanId.Should().Be("b7ad6b7169203331");
        context.IsSampled.Should().BeTrue();
        context.CorrelationId.Should().Be("corr-999");
    }

    [Fact]
    public void ExtractTraceContext_Should_ReturnNull_WhenTraceparentMissing()
    {
        TracePropagation.ExtractTraceContext(new Dictionary<string, string?>()).Should().BeNull();
    }

    [Fact]
    public void ToActivityContext_Should_CreateValidContext()
    {
        var context = new TraceContext
        {
            TraceId = "0af7651916cd43dd8448eb211c80319c",
            SpanId = "b7ad6b7169203331",
            TraceFlags = ActivityTraceFlags.Recorded,
            TraceState = "vendor=value"
        };

        var activityContext = context.ToActivityContext();

        activityContext.TraceId.ToString().Should().Be("0af7651916cd43dd8448eb211c80319c");
        activityContext.SpanId.ToString().Should().Be("b7ad6b7169203331");
    }

    [Fact]
    public void StartActivityWithParent_Should_StartChildActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("TracePropagationParentTest");
        var parent = new TraceContext
        {
            TraceId = "0af7651916cd43dd8448eb211c80319c",
            SpanId = "b7ad6b7169203331",
            TraceFlags = ActivityTraceFlags.Recorded,
            CorrelationId = "corr-parent",
            Baggage = new Dictionary<string, string> { ["tenant.id"] = "t1" }
        };

        using Activity? activity = source.StartActivityWithParent("child", parent);

        activity.Should().NotBeNull();
        activity!.GetTagItem(SemanticTags.CorrelationId).Should().Be("corr-parent");
        activity.GetBaggageItem("tenant.id").Should().Be("t1");
    }
}
