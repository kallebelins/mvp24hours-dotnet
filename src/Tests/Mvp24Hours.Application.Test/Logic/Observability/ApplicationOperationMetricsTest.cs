using System.Diagnostics.Metrics;
using Mvp24Hours.Application.Contract.Observability;
using Mvp24Hours.Application.Logic.Observability;

namespace Mvp24Hours.Application.Test.Logic.Observability;

[Trait("Category", "Unit")]
public class ApplicationOperationMetricsTest
{
    [Fact]
    public void RecordOperationLifecycle_ShouldEmitMeasurements()
    {
        int measurementCount = 0;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == ApplicationOperationMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) => Interlocked.Increment(ref measurementCount));
        listener.SetMeasurementEventCallback<double>((_, _, _, _) => Interlocked.Increment(ref measurementCount));
        listener.Start();

        using var metrics = new ApplicationOperationMetrics();
        metrics.RecordOperationStart("ProductService", "List", "Query");
        metrics.RecordOperationSuccess("ProductService", "List", "Query", 15);

        measurementCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordOperationFailure_ShouldNotThrow()
    {
        using var metrics = new ApplicationOperationMetrics();

        Action act = () => metrics.RecordOperationFailure(
            "OrderService",
            "Add",
            "Command",
            50,
            typeof(InvalidOperationException).FullName!);

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordOperation_WithDisabledOptions_ShouldNoOp()
    {
        int measurementCount = 0;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == ApplicationOperationMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) => Interlocked.Increment(ref measurementCount));
        listener.Start();

        using var metrics = new ApplicationOperationMetrics(new OperationMetricsOptions { Enabled = false });
        metrics.RecordOperationStart("Svc", "Op", "Query");
        metrics.RecordOperationSuccess("Svc", "Op", "Query", 1);

        measurementCount.Should().Be(0);
    }

    [Fact]
    public void NullOperationMetrics_ShouldNotThrow()
    {
        IOperationMetrics metrics = NullOperationMetrics.Instance;

        Action act = () =>
        {
            metrics.RecordOperationStart("Svc", "Op", "Query");
            metrics.RecordOperationSuccess("Svc", "Op", "Query", 1);
            metrics.RecordOperationFailure("Svc", "Op", "Query", 1, "Error");
        };

        act.Should().NotThrow();
    }
}
