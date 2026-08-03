using System.Diagnostics;
using System.Text;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;

namespace Mvp24Hours.Application.RabbitMQ.Test.Observability;

[Trait("Category", "Unit")]
public class BaggagePropagationTest
{
    [Fact]
    public void InjectTraceContext_WithNullHeaders_ShouldThrow()
    {
        Action act = () => BaggagePropagation.InjectTraceContext(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void InjectTraceContext_WithoutActivity_ShouldNotModifyHeaders()
    {
        var headers = new Dictionary<string, object>();

        BaggagePropagation.InjectTraceContext(headers, activity: null);

        headers.Should().BeEmpty();
    }

    [Fact]
    public void InjectTraceContext_WithActivity_ShouldInjectTraceParentAndBaggage()
    {
        using ActivityListener listener = CreateActivityListener();
        using var activity = new Activity("publish");
        activity.SetBaggage("correlation-id", "corr-trace");
        activity.Start();

        var headers = new Dictionary<string, object>();
        BaggagePropagation.InjectTraceContext(headers, activity);

        headers.Should().ContainKey(BaggagePropagation.Keys.TraceParent);
        headers.Should().ContainKey(BaggagePropagation.Keys.Baggage);
    }

    [Fact]
    public void ExtractTraceContext_WithValidTraceParent_ShouldParseContext()
    {
        using ActivityListener listener = CreateActivityListener();
        using var activity = new Activity("source");
        activity.Start();
        string traceParent = activity.Id!;

        var headers = new Dictionary<string, object>
        {
            [BaggagePropagation.Keys.TraceParent] = traceParent
        };

        ActivityContext context = BaggagePropagation.ExtractTraceContext(headers);

        context.TraceId.Should().Be(activity.TraceId);
    }

    [Fact]
    public void ExtractTraceContext_WithNullHeaders_ShouldReturnDefault()
    {
        ActivityContext context = BaggagePropagation.ExtractTraceContext(null);

        context.Should().Be(default(ActivityContext));
    }

    [Fact]
    public void InjectBaggage_AllFields_ShouldRoundTrip()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        var context = new BaggageContext
        {
            CorrelationId = "corr",
            CausationId = "cause",
            TenantId = "tenant",
            UserId = "user",
            ConversationId = "conv",
            MessageType = "OrderCreated",
            SourceService = "orders-api",
            Timestamp = timestamp
        };

        var headers = new Dictionary<string, object>();
        BaggagePropagation.InjectBaggage(headers, context);
        BaggageContext extracted = BaggagePropagation.ExtractBaggage(headers);

        extracted.CorrelationId.Should().Be("corr");
        extracted.CausationId.Should().Be("cause");
        extracted.TenantId.Should().Be("tenant");
        extracted.UserId.Should().Be("user");
        extracted.ConversationId.Should().Be("conv");
        extracted.MessageType.Should().Be("OrderCreated");
        extracted.SourceService.Should().Be("orders-api");
        extracted.Timestamp.Should().BeCloseTo(timestamp, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ExtractBaggage_W3CBaggageHeader_ShouldMergeValues()
    {
        var headers = new Dictionary<string, object>
        {
            [BaggagePropagation.Keys.Baggage] = "tenant-id=tenant-w3c,correlation-id=corr-w3c"
        };

        BaggageContext context = BaggagePropagation.ExtractBaggage(headers);

        context.TenantId.Should().Be("tenant-w3c");
        context.CorrelationId.Should().Be("corr-w3c");
    }

    [Fact]
    public void ExtractBaggage_ByteArrayHeader_ShouldDecodeUtf8()
    {
        var headers = new Dictionary<string, object>
        {
            [BaggagePropagation.Keys.CorrelationId] = Encoding.UTF8.GetBytes("byte-corr")
        };

        BaggageContext context = BaggagePropagation.ExtractBaggage(headers);

        context.CorrelationId.Should().Be("byte-corr");
    }

    [Fact]
    public void RestoreBaggageToActivity_ShouldSetActivityBaggage()
    {
        using ActivityListener listener = CreateActivityListener();
        using var activity = new Activity("consume");
        activity.Start();

        var context = new BaggageContext
        {
            CorrelationId = "corr-restore",
            TenantId = "tenant-restore",
            UserId = "user-restore",
            CausationId = "cause-restore",
            ConversationId = "conv-restore"
        };

        BaggagePropagation.RestoreBaggageToActivity(context);

        activity.GetBaggageItem("correlation-id").Should().Be("corr-restore");
        activity.GetBaggageItem("tenant-id").Should().Be("tenant-restore");
        activity.GetBaggageItem("user-id").Should().Be("user-restore");
    }

    [Fact]
    public void CreateFromCurrentActivity_ShouldMapKnownBaggageKeys()
    {
        using ActivityListener listener = CreateActivityListener();
        using var activity = new Activity("current");
        activity.SetBaggage("correlation-id", "c1");
        activity.SetBaggage("tenant-id", "t1");
        activity.SetBaggage("user-id", "u1");
        activity.SetBaggage("causation-id", "ca1");
        activity.SetBaggage("conversation-id", "co1");
        activity.Start();

        BaggageContext context = BaggagePropagation.CreateFromCurrentActivity();

        context.CorrelationId.Should().Be("c1");
        context.TenantId.Should().Be("t1");
        context.UserId.Should().Be("u1");
        context.CausationId.Should().Be("ca1");
        context.ConversationId.Should().Be("co1");
    }

    [Fact]
    public void GetOrCreateTraceId_WithoutActivity_ShouldReturnNonEmptyGuid()
    {
        string traceId = BaggagePropagation.GetOrCreateTraceId();

        traceId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParse(traceId, out _).Should().BeTrue();
    }

    [Fact]
    public void GetOrCreateSpanId_WithoutActivity_ShouldReturnNonEmptyGuid()
    {
        string spanId = BaggagePropagation.GetOrCreateSpanId();

        spanId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void BaggageContext_WithCorrelation_ShouldSetCorrelationId()
    {
        var context = BaggageContext.WithCorrelation("corr-factory");

        context.CorrelationId.Should().Be("corr-factory");
    }

    [Fact]
    public void BaggageContext_ForTenant_ShouldSetTenantId()
    {
        var context = BaggageContext.ForTenant("tenant-factory");

        context.TenantId.Should().Be("tenant-factory");
    }

    [Fact]
    public void BaggageContext_WithCausation_ShouldCopyExistingFields()
    {
        var original = new BaggageContext
        {
            CorrelationId = "corr",
            TenantId = "tenant",
            UserId = "user",
            ConversationId = "conv",
            MessageType = "type",
            SourceService = "service"
        };

        BaggageContext updated = original.WithCausation("new-cause");

        updated.CorrelationId.Should().Be("corr");
        updated.TenantId.Should().Be("tenant");
        updated.CausationId.Should().Be("new-cause");
        updated.Timestamp.Should().NotBeNull();
    }

    private static ActivityListener CreateActivityListener()
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
