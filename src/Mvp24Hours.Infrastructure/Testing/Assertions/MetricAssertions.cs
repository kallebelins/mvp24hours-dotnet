//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Infrastructure.Testing.Observability;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Assertions
{
    /// <summary>
    /// Provides assertion helpers for metrics captured by FakeMeterListener.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These helpers provide fluent assertions for verifying metrics
    /// (counters, histograms, gauges) in tests. They work with <see cref="FakeMeterListener"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var listener = new FakeMeterListener("Mvp24Hours.*");
    /// 
    /// await service.ProcessAsync();
    /// 
    /// MetricAssertions.AssertMetricRecorded(listener, "mvp24hours.pipe.operations_total");
    /// MetricAssertions.AssertCounterValue(listener, "mvp24hours.pipe.operations_total", 5);
    /// MetricAssertions.AssertMetricHasTag(listener, "mvp24hours.data.queries_total", "operation", "GetById");
    /// </code>
    /// </example>
    public static class MetricAssertions
    {
        /// <summary>
        /// Asserts that a measurement was recorded for the specified instrument.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The expected instrument name.</param>
        /// <exception cref="AssertionException">Thrown when no matching measurement was found.</exception>
        public static void AssertMetricRecorded(FakeMeterListener listener, string instrumentName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            if (!listener.HasMeasurement(instrumentName))
            {
                var instruments = string.Join(", ", listener.Measurements.Select(m => m.InstrumentName).Distinct());
                throw new AssertionException(
                    $"Expected a measurement for instrument '{instrumentName}', but none was found.\n" +
                    $"Recorded instruments: [{instruments}]");
            }
        }

        /// <summary>
        /// Asserts that a measurement was recorded from the specified meter.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="meterName">The expected meter name.</param>
        /// <exception cref="AssertionException">Thrown when no matching measurement was found.</exception>
        public static void AssertMetricFromMeter(FakeMeterListener listener, string meterName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(meterName)) throw new ArgumentNullException(nameof(meterName));

            if (!listener.HasMeasurementFromMeter(meterName))
            {
                var meters = string.Join(", ", listener.Measurements.Select(m => m.MeterName).Distinct());
                throw new AssertionException(
                    $"Expected a measurement from meter '{meterName}', but none was found.\n" +
                    $"Recorded meters: [{meters}]");
            }
        }

        /// <summary>
        /// Asserts that the total value (sum) of a counter equals the expected value.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The counter instrument name.</param>
        /// <param name="expectedValue">The expected total value.</param>
        /// <param name="tolerance">Optional tolerance for floating-point comparison.</param>
        /// <exception cref="AssertionException">Thrown when the value doesn't match.</exception>
        public static void AssertCounterValue(FakeMeterListener listener, string instrumentName, double expectedValue, double tolerance = 0.0001)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            var actualValue = listener.GetSum(instrumentName);
            if (Math.Abs(actualValue - expectedValue) > tolerance)
            {
                throw new AssertionException(
                    $"Counter '{instrumentName}' expected total value {expectedValue}, but was {actualValue}.");
            }
        }

        /// <summary>
        /// Asserts that a counter value is at least the specified minimum.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The counter instrument name.</param>
        /// <param name="minimumValue">The minimum expected value.</param>
        /// <exception cref="AssertionException">Thrown when the value is below the minimum.</exception>
        public static void AssertCounterValueAtLeast(FakeMeterListener listener, string instrumentName, double minimumValue)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            var actualValue = listener.GetSum(instrumentName);
            if (actualValue < minimumValue)
            {
                throw new AssertionException(
                    $"Counter '{instrumentName}' expected at least {minimumValue}, but was {actualValue}.");
            }
        }

        /// <summary>
        /// Asserts that exactly the specified number of measurements were recorded for the instrument.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The instrument name.</param>
        /// <param name="expectedCount">The expected count.</param>
        /// <exception cref="AssertionException">Thrown when the count doesn't match.</exception>
        public static void AssertMeasurementCount(FakeMeterListener listener, string instrumentName, int expectedCount)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            var actualCount = listener.GetCount(instrumentName);
            if (actualCount != expectedCount)
            {
                throw new AssertionException(
                    $"Expected {expectedCount} measurement(s) for '{instrumentName}', but found {actualCount}.");
            }
        }

        /// <summary>
        /// Asserts that a measurement has the specified tag.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The instrument name.</param>
        /// <param name="tagKey">The tag key.</param>
        /// <param name="expectedValue">Optional expected value. If null, only checks for presence.</param>
        /// <exception cref="AssertionException">Thrown when the tag is not found or doesn't match.</exception>
        public static void AssertMetricHasTag(FakeMeterListener listener, string instrumentName, string tagKey, object? expectedValue = null)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));
            if (string.IsNullOrEmpty(tagKey)) throw new ArgumentNullException(nameof(tagKey));

            var measurement = listener.GetMeasurements(instrumentName).FirstOrDefault();
            if (measurement == null)
            {
                throw new AssertionException(
                    $"No measurement found for instrument '{instrumentName}'.");
            }

            if (!measurement.HasTag(tagKey))
            {
                var existingTags = string.Join(", ", measurement.Tags.Keys);
                throw new AssertionException(
                    $"Measurement for '{instrumentName}' does not have tag '{tagKey}'.\n" +
                    $"Existing tags: [{existingTags}]");
            }

            if (expectedValue != null)
            {
                var actualValue = measurement.GetTag(tagKey);
                if (!Equals(actualValue, expectedValue))
                {
                    throw new AssertionException(
                        $"Measurement for '{instrumentName}' tag '{tagKey}' expected '{expectedValue}' but was '{actualValue}'.");
                }
            }
        }

        /// <summary>
        /// Asserts that the average value of measurements equals the expected value.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The instrument name.</param>
        /// <param name="expectedAverage">The expected average value.</param>
        /// <param name="tolerance">Tolerance for floating-point comparison.</param>
        /// <exception cref="AssertionException">Thrown when the average doesn't match.</exception>
        public static void AssertAverageValue(FakeMeterListener listener, string instrumentName, double expectedAverage, double tolerance = 0.0001)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            var actualAverage = listener.GetAverage(instrumentName);
            if (!actualAverage.HasValue)
            {
                throw new AssertionException(
                    $"No measurements found for instrument '{instrumentName}'.");
            }

            if (Math.Abs(actualAverage.Value - expectedAverage) > tolerance)
            {
                throw new AssertionException(
                    $"Instrument '{instrumentName}' expected average {expectedAverage}, but was {actualAverage.Value}.");
            }
        }

        /// <summary>
        /// Asserts that a histogram/gauge value is within the specified range.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The instrument name.</param>
        /// <param name="minValue">The minimum expected value.</param>
        /// <param name="maxValue">The maximum expected value.</param>
        /// <exception cref="AssertionException">Thrown when any value is outside the range.</exception>
        public static void AssertValueInRange(FakeMeterListener listener, string instrumentName, double minValue, double maxValue)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            var measurements = listener.GetMeasurements(instrumentName);
            if (!measurements.Any())
            {
                throw new AssertionException(
                    $"No measurements found for instrument '{instrumentName}'.");
            }

            var outOfRange = measurements.Where(m => m.Value < minValue || m.Value > maxValue).ToList();
            if (outOfRange.Any())
            {
                var values = string.Join(", ", outOfRange.Select(m => m.Value));
                throw new AssertionException(
                    $"Instrument '{instrumentName}' has values outside range [{minValue}, {maxValue}]: [{values}]");
            }
        }

        /// <summary>
        /// Asserts that no measurements were recorded.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <exception cref="AssertionException">Thrown when any measurement was recorded.</exception>
        public static void AssertNoMeasurementsRecorded(FakeMeterListener listener)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));

            if (listener.MeasurementCount > 0)
            {
                var measurements = string.Join("\n", listener.Measurements.Take(10).Select(m => $"  - {m}"));
                var more = listener.MeasurementCount > 10 ? $"\n  ... and {listener.MeasurementCount - 10} more" : "";
                throw new AssertionException(
                    $"Expected no measurements to be recorded, but found {listener.MeasurementCount}:\n{measurements}{more}");
            }
        }

        /// <summary>
        /// Asserts that a measurement was recorded with a specific value.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The instrument name.</param>
        /// <param name="expectedValue">The expected value.</param>
        /// <param name="tolerance">Tolerance for floating-point comparison.</param>
        /// <exception cref="AssertionException">Thrown when no measurement with that value is found.</exception>
        public static void AssertMeasurementWithValue(FakeMeterListener listener, string instrumentName, double expectedValue, double tolerance = 0.0001)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            var measurements = listener.GetMeasurements(instrumentName);
            if (!measurements.Any())
            {
                throw new AssertionException(
                    $"No measurements found for instrument '{instrumentName}'.");
            }

            var hasMatch = measurements.Any(m => Math.Abs(m.Value - expectedValue) <= tolerance);
            if (!hasMatch)
            {
                var values = string.Join(", ", measurements.Select(m => m.Value));
                throw new AssertionException(
                    $"No measurement for '{instrumentName}' with value {expectedValue} (tolerance: {tolerance}).\n" +
                    $"Recorded values: [{values}]");
            }
        }

        /// <summary>
        /// Gets the first recorded measurement for the specified instrument for further assertions.
        /// </summary>
        /// <param name="listener">The fake meter listener to check.</param>
        /// <param name="instrumentName">The instrument name.</param>
        /// <returns>The recorded measurement.</returns>
        /// <exception cref="AssertionException">Thrown when no matching measurement is found.</exception>
        public static RecordedMeasurement GetMeasurement(FakeMeterListener listener, string instrumentName)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (string.IsNullOrEmpty(instrumentName)) throw new ArgumentNullException(nameof(instrumentName));

            var measurement = listener.GetMeasurements(instrumentName).FirstOrDefault();
            if (measurement == null)
            {
                var instruments = string.Join(", ", listener.Measurements.Select(m => m.InstrumentName).Distinct());
                throw new AssertionException(
                    $"No measurement found for instrument '{instrumentName}'.\n" +
                    $"Recorded instruments: [{instruments}]");
            }

            return measurement;
        }
    }
}

