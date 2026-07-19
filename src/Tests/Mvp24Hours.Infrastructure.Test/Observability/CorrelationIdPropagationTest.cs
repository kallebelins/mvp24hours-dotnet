//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Mvp24Hours.Infrastructure.Observability;

namespace Mvp24Hours.Infrastructure.Test.Observability;

[Trait("Category", "Unit")]
public class CorrelationIdPropagationTest
{
    [Fact]
    public void GetCorrelationId_WithoutActivity_ShouldReturn32CharHexGuid()
    {
        string correlationId = CorrelationIdPropagation.GetCorrelationId();

        correlationId.Should().NotBeNullOrWhiteSpace();
        correlationId.Should().HaveLength(32);
        correlationId.Should().MatchRegex("^[0-9a-fA-F]{32}$");
    }

    [Fact]
    public void GetCorrelationId_WithBaggage_ShouldReturnBaggageValue()
    {
        using var activity = new Activity("test");
        activity.Start();
        try
        {
            activity.SetBaggage(CorrelationIdPropagation.CorrelationIdBaggageKey, "baggage-correlation-id");

            string correlationId = CorrelationIdPropagation.GetCorrelationId();

            correlationId.Should().Be("baggage-correlation-id");
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void GetCorrelationId_WithActivityIdAndNoBaggage_ShouldReturnActivityId()
    {
        using var source = new ActivitySource("Test.Correlation");
        using var listener = CreateAllDataListener(source.Name);
        using Activity? activity = source.StartActivity("test");

        activity.Should().NotBeNull();
        activity!.Id.Should().NotBeNullOrEmpty();

        string correlationId = CorrelationIdPropagation.GetCorrelationId();

        correlationId.Should().Be(activity.Id);
    }

    [Fact]
    public void SetCorrelationId_WithValidValue_ShouldSetBaggage()
    {
        using var activity = new Activity("test");
        activity.Start();
        try
        {
            CorrelationIdPropagation.SetCorrelationId("custom-id-123");

            activity.GetBaggageItem(CorrelationIdPropagation.CorrelationIdBaggageKey)
                .Should().Be("custom-id-123");
        }
        finally
        {
            activity.Stop();
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetCorrelationId_WithNullOrWhitespace_ShouldBeNoOp(string? correlationId)
    {
        using var activity = new Activity("test");
        activity.Start();
        try
        {
            CorrelationIdPropagation.SetCorrelationId(correlationId!);

            activity.GetBaggageItem(CorrelationIdPropagation.CorrelationIdBaggageKey)
                .Should().BeNull();
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void EnsureCorrelationId_WhenAlreadySet_ShouldReturnExistingId()
    {
        using var activity = new Activity("test");
        activity.Start();
        try
        {
            activity.SetBaggage(CorrelationIdPropagation.CorrelationIdBaggageKey, "existing-id");

            string first = CorrelationIdPropagation.EnsureCorrelationId();
            string second = CorrelationIdPropagation.EnsureCorrelationId();

            first.Should().Be("existing-id");
            second.Should().Be("existing-id");
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void EnsureCorrelationId_WhenMissing_ShouldGenerateAndSetId()
    {
        using var activity = new Activity("test");
        activity.Start();
        try
        {
            string correlationId = CorrelationIdPropagation.EnsureCorrelationId();

            correlationId.Should().HaveLength(32);
            activity.GetBaggageItem(CorrelationIdPropagation.CorrelationIdBaggageKey)
                .Should().Be(correlationId);
        }
        finally
        {
            activity.Stop();
        }
    }

    [Fact]
    public void AddCorrelationIdHeader_WithNullHeaders_ShouldThrowArgumentNullException()
    {
        Action act = () => CorrelationIdPropagation.AddCorrelationIdHeader(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("headers");
    }

    [Fact]
    public void AddCorrelationIdHeader_ShouldAddXCorrelationIdHeader()
    {
        var headers = new Dictionary<string, string>();

        CorrelationIdPropagation.AddCorrelationIdHeader(headers, "explicit-correlation-id");

        headers.Should().ContainKey(CorrelationIdPropagation.CorrelationIdHeaderName);
        headers[CorrelationIdPropagation.CorrelationIdHeaderName].Should().Be("explicit-correlation-id");
    }

    [Fact]
    public void AddCorrelationIdHeader_WithoutExplicitId_ShouldUseContextCorrelationId()
    {
        using var activity = new Activity("test");
        activity.Start();
        try
        {
            activity.SetBaggage(CorrelationIdPropagation.CorrelationIdBaggageKey, "from-context");
            var headers = new Dictionary<string, string>();

            CorrelationIdPropagation.AddCorrelationIdHeader(headers);

            headers[CorrelationIdPropagation.CorrelationIdHeaderName].Should().Be("from-context");
        }
        finally
        {
            activity.Stop();
        }
    }

    private static ActivityListener CreateAllDataListener(string sourceName)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
