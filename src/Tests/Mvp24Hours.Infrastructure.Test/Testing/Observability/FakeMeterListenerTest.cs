//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics.Metrics;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Test.Testing.Observability;

[Trait("Category", "Unit")]
public class FakeMeterListenerTest
{
    [Fact]
    public void CounterAdd_FromMatchingMeter_ShouldRecordMeasurement()
    {
        using FakeMeterListener listener = new("TestMetrics.*");
        using Meter meter = new("TestMetrics.App");
        Counter<long> counter = meter.CreateCounter<long>("requests_total");

        counter.Add(3, new KeyValuePair<string, object?>("route", "/api/users"));

        listener.MeasurementCount.Should().Be(1);
        listener.HasMeasurement("requests_total").Should().BeTrue();
        listener.GetMeasurements("requests_total").First().Value.Should().Be(3);
    }

    [Fact]
    public void CounterAdd_FromNonMatchingMeter_ShouldNotRecordMeasurement()
    {
        using FakeMeterListener listener = new("TestMetrics.*");
        using Meter meter = new("OtherMetrics.App");
        Counter<long> counter = meter.CreateCounter<long>("ignored_total");

        counter.Add(1);

        listener.MeasurementCount.Should().Be(0);
        listener.HasMeasurement("ignored_total").Should().BeFalse();
    }

    [Fact]
    public void GetSum_ShouldAggregateCounterValues()
    {
        using FakeMeterListener listener = new("Billing.*");
        using Meter meter = new("Billing.Service");
        Counter<double> counter = meter.CreateCounter<double>("amount_processed");

        counter.Add(10.5);
        counter.Add(4.5);

        listener.GetSum("amount_processed").Should().Be(15);
        listener.GetCount("amount_processed").Should().Be(2);
    }

    [Fact]
    public void Clear_ShouldRemoveRecordedMeasurements()
    {
        using FakeMeterListener listener = new();
        using Meter meter = new("ClearTest");
        Counter<int> counter = meter.CreateCounter<int>("events");

        counter.Add(1);
        listener.Clear();

        listener.MeasurementCount.Should().Be(0);
        listener.HasMeasurement("events").Should().BeFalse();
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldBeIdempotent()
    {
        FakeMeterListener listener = new();
        using Meter meter = new("DisposeTest");
        Counter<int> counter = meter.CreateCounter<int>("ops");
        counter.Add(1);

        listener.Dispose();
        Action secondDispose = () => listener.Dispose();

        secondDispose.Should().NotThrow();
    }

    [Fact]
    public void WildcardFilter_ShouldMatchMeterPrefixCaseInsensitively()
    {
        using FakeMeterListener listener = new("testmetrics.*");
        using Meter meter = new("TestMetrics.Worker");
        Counter<long> counter = meter.CreateCounter<long>("jobs_completed");

        counter.Add(7);

        listener.HasMeasurementFromMeter("TestMetrics.Worker").Should().BeTrue();
        listener.GetMeasurementsFromMeter("TestMetrics.Worker").Should().HaveCount(1);
    }

    [Fact]
    public void MeasurementRecordedEvent_ShouldFireWhenCounterIsIncremented()
    {
        using FakeMeterListener listener = new("Events.*");
        RecordedMeasurement? captured = null;
        listener.MeasurementRecorded += (_, measurement) => captured = measurement;
        using Meter meter = new("Events.App");
        Counter<int> counter = meter.CreateCounter<int>("clicks");

        counter.Add(2, new KeyValuePair<string, object?>("button", "submit"));

        captured.Should().NotBeNull();
        captured!.InstrumentName.Should().Be("clicks");
        captured.GetTag("button").Should().Be("submit");
    }

    [Fact]
    public void GetLastMeasurement_ShouldReturnMostRecentValue()
    {
        using FakeMeterListener listener = new();
        using Meter meter = new("Latest");
        Counter<int> counter = meter.CreateCounter<int>("score");

        counter.Add(1);
        counter.Add(5);

        listener.GetLastMeasurement("score")!.Value.Should().Be(5);
    }
}
