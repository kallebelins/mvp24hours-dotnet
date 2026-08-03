//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics.Metrics;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Observability;
using AssertionException = Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException;

namespace Mvp24Hours.Infrastructure.Test.Testing.Assertions;

[Trait("Category", "Unit")]
public class MetricAssertionsTest
{
    private static FakeMeterListener CreateListenerWithCounter(
        string meterName,
        string instrumentName,
        params (double Value, KeyValuePair<string, object?>[] Tags)[] measurements)
    {
        FakeMeterListener listener = new("MetricAssertions.*");
        using Meter meter = new(meterName);
        Counter<double> counter = meter.CreateCounter<double>(instrumentName);

        foreach ((double value, KeyValuePair<string, object?>[] tags) in measurements)
        {
            counter.Add(value, tags);
        }

        return listener;
    }

    [Fact]
    public void AssertMetricRecorded_ShouldPassWhenInstrumentExists()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "requests_total", (1, []));

        Action act = () => MetricAssertions.AssertMetricRecorded(listener, "requests_total");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertMetricRecorded_ShouldThrowWhenInstrumentMissing()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "other_total", (1, []));

        Action act = () => MetricAssertions.AssertMetricRecorded(listener, "requests_total");

        act.Should().Throw<AssertionException>().WithMessage("*requests_total*");
    }

    [Fact]
    public void AssertMetricFromMeter_ShouldPassWhenMeterMatches()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.Billing", "amount", (10, []));

        Action act = () => MetricAssertions.AssertMetricFromMeter(listener, "MetricAssertions.Billing");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertMetricFromMeter_ShouldThrowWhenMeterMissing()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "events", (1, []));

        Action act = () => MetricAssertions.AssertMetricFromMeter(listener, "Other.Meter");

        act.Should().Throw<AssertionException>().WithMessage("*Other.Meter*");
    }

    [Fact]
    public void AssertCounterValue_ShouldPassWhenSumMatches()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "orders_total",
            (3, []),
            (7, []));

        Action act = () => MetricAssertions.AssertCounterValue(listener, "orders_total", 10);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertCounterValue_ShouldThrowWhenSumMismatch()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "orders_total", (5, []));

        Action act = () => MetricAssertions.AssertCounterValue(listener, "orders_total", 10);

        act.Should().Throw<AssertionException>().WithMessage("*expected total value 10*");
    }

    [Fact]
    public void AssertCounterValueAtLeast_ShouldPassWhenAboveMinimum()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "hits", (5, []));

        Action act = () => MetricAssertions.AssertCounterValueAtLeast(listener, "hits", 3);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertCounterValueAtLeast_ShouldThrowWhenBelowMinimum()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "hits", (2, []));

        Action act = () => MetricAssertions.AssertCounterValueAtLeast(listener, "hits", 5);

        act.Should().Throw<AssertionException>().WithMessage("*at least 5*");
    }

    [Fact]
    public void AssertMeasurementCount_ShouldPassWhenCountMatches()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "clicks",
            (1, []),
            (1, []));

        Action act = () => MetricAssertions.AssertMeasurementCount(listener, "clicks", 2);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertMeasurementCount_ShouldThrowWhenCountMismatch()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "clicks", (1, []));

        Action act = () => MetricAssertions.AssertMeasurementCount(listener, "clicks", 3);

        act.Should().Throw<AssertionException>().WithMessage("*Expected 3 measurement*");
    }

    [Fact]
    public void AssertMetricHasTag_ShouldPassWhenTagExistsWithValue()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "queries_total",
            (1, [new KeyValuePair<string, object?>("operation", "GetById")]));

        Action act = () => MetricAssertions.AssertMetricHasTag(listener, "queries_total", "operation", "GetById");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertMetricHasTag_ShouldPassWhenOnlyPresenceRequired()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "queries_total",
            (1, [new KeyValuePair<string, object?>("region", "us-east")]));

        Action act = () => MetricAssertions.AssertMetricHasTag(listener, "queries_total", "region");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertMetricHasTag_ShouldThrowWhenTagMissing()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "queries_total", (1, []));

        Action act = () => MetricAssertions.AssertMetricHasTag(listener, "queries_total", "missing");

        act.Should().Throw<AssertionException>().WithMessage("*does not have tag 'missing'*");
    }

    [Fact]
    public void AssertMetricHasTag_ShouldThrowWhenTagValueMismatch()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "queries_total",
            (1, [new KeyValuePair<string, object?>("env", "dev")]));

        Action act = () => MetricAssertions.AssertMetricHasTag(listener, "queries_total", "env", "prod");

        act.Should().Throw<AssertionException>().WithMessage("*expected 'prod'*");
    }

    [Fact]
    public void AssertAverageValue_ShouldPassWhenAverageMatches()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "latency_ms",
            (10, []),
            (20, []));

        Action act = () => MetricAssertions.AssertAverageValue(listener, "latency_ms", 15);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertAverageValue_ShouldThrowWhenNoMeasurements()
    {
        using FakeMeterListener listener = new("MetricAssertions.*");

        Action act = () => MetricAssertions.AssertAverageValue(listener, "missing", 1);

        act.Should().Throw<AssertionException>().WithMessage("*No measurements found*");
    }

    [Fact]
    public void AssertAverageValue_ShouldThrowWhenAverageMismatch()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "latency_ms",
            (10, []),
            (30, []));

        Action act = () => MetricAssertions.AssertAverageValue(listener, "latency_ms", 25);

        act.Should().Throw<AssertionException>().WithMessage("*expected average 25*");
    }

    [Fact]
    public void AssertValueInRange_ShouldPassWhenAllValuesInRange()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "score",
            (5, []),
            (8, []));

        Action act = () => MetricAssertions.AssertValueInRange(listener, "score", 1, 10);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertValueInRange_ShouldThrowWhenValueOutOfRange()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "score",
            (5, []),
            (15, []));

        Action act = () => MetricAssertions.AssertValueInRange(listener, "score", 1, 10);

        act.Should().Throw<AssertionException>().WithMessage("*outside range*");
    }

    [Fact]
    public void AssertNoMeasurementsRecorded_ShouldPassWhenEmpty()
    {
        using FakeMeterListener listener = new("MetricAssertions.*");

        Action act = () => MetricAssertions.AssertNoMeasurementsRecorded(listener);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoMeasurementsRecorded_ShouldThrowWhenMeasurementsExist()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "events", (1, []));

        Action act = () => MetricAssertions.AssertNoMeasurementsRecorded(listener);

        act.Should().Throw<AssertionException>().WithMessage("*Expected no measurements*");
    }

    [Fact]
    public void AssertMeasurementWithValue_ShouldPassWhenValueExists()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "batch_size",
            (100, []),
            (200, []));

        Action act = () => MetricAssertions.AssertMeasurementWithValue(listener, "batch_size", 200);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertMeasurementWithValue_ShouldThrowWhenValueMissing()
    {
        using FakeMeterListener listener = CreateListenerWithCounter("MetricAssertions.App", "batch_size", (100, []));

        Action act = () => MetricAssertions.AssertMeasurementWithValue(listener, "batch_size", 999);

        act.Should().Throw<AssertionException>().WithMessage("*No measurement*999*");
    }

    [Fact]
    public void GetMeasurement_ShouldReturnFirstMatchingMeasurement()
    {
        using FakeMeterListener listener = CreateListenerWithCounter(
            "MetricAssertions.App",
            "items",
            (1, [new KeyValuePair<string, object?>("kind", "a")]));

        RecordedMeasurement measurement = MetricAssertions.GetMeasurement(listener, "items");

        measurement.InstrumentName.Should().Be("items");
        measurement.GetTag("kind").Should().Be("a");
    }

    [Fact]
    public void GetMeasurement_ShouldThrowWhenInstrumentMissing()
    {
        using FakeMeterListener listener = new("MetricAssertions.*");

        Action act = () => MetricAssertions.GetMeasurement(listener, "missing");

        act.Should().Throw<AssertionException>().WithMessage("*No measurement found*");
    }

    [Fact]
    public void NullArguments_ShouldThrowArgumentNullException()
    {
        using FakeMeterListener listener = new();

        Action nullListener = () => MetricAssertions.AssertMetricRecorded(null!, "x");
        Action nullInstrument = () => MetricAssertions.AssertMetricRecorded(listener, null!);
        Action nullMeter = () => MetricAssertions.AssertMetricFromMeter(listener, null!);
        Action nullTagKey = () => MetricAssertions.AssertMetricHasTag(listener, "x", null!);

        nullListener.Should().Throw<ArgumentNullException>().WithParameterName("listener");
        nullInstrument.Should().Throw<ArgumentNullException>().WithParameterName("instrumentName");
        nullMeter.Should().Throw<ArgumentNullException>().WithParameterName("meterName");
        nullTagKey.Should().Throw<ArgumentNullException>().WithParameterName("tagKey");
    }
}
