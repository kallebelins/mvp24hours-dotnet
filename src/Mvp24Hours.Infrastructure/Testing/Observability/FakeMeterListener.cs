//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Testing.Observability
{
    /// <summary>
    /// Represents a recorded metric measurement.
    /// </summary>
    public sealed class RecordedMeasurement
    {
        /// <summary>
        /// Gets the meter name.
        /// </summary>
        public string MeterName { get; }

        /// <summary>
        /// Gets the instrument name.
        /// </summary>
        public string InstrumentName { get; }

        /// <summary>
        /// Gets the instrument type (Counter, Histogram, etc.).
        /// </summary>
        public string InstrumentType { get; }

        /// <summary>
        /// Gets the unit of measurement.
        /// </summary>
        public string? Unit { get; }

        /// <summary>
        /// Gets the measurement value as a double.
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Gets the tags associated with this measurement.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Tags { get; }

        /// <summary>
        /// Gets the timestamp when the measurement was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Creates a new recorded measurement.
        /// </summary>
        public RecordedMeasurement(
            string meterName,
            string instrumentName,
            string instrumentType,
            string? unit,
            double value,
            IEnumerable<KeyValuePair<string, object?>>? tags = null)
        {
            MeterName = meterName;
            InstrumentName = instrumentName;
            InstrumentType = instrumentType;
            Unit = unit;
            Value = value;
            Tags = tags?.ToDictionary(kv => kv.Key, kv => kv.Value)
                ?? new Dictionary<string, object?>();
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets a tag value.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <returns>The tag value, or null if not found.</returns>
        public object? GetTag(string key)
        {
            return Tags.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// Checks if a tag exists.
        /// </summary>
        /// <param name="key">The tag key.</param>
        /// <returns>True if the tag exists.</returns>
        public bool HasTag(string key)
        {
            return Tags.ContainsKey(key);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var tagsPart = Tags.Any()
                ? $" [{string.Join(", ", Tags.Select(t => $"{t.Key}={t.Value}"))}]"
                : "";
            return $"{MeterName}/{InstrumentName}: {Value}{(Unit != null ? $" {Unit}" : "")}{tagsPart}";
        }
    }

    /// <summary>
    /// A fake MeterListener for capturing metrics in tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// FakeMeterListener provides a way to capture and verify metrics
    /// (counters, histograms, gauges) in unit and integration tests.
    /// It automatically subscribes to Meters and records all measurements.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create and start listening
    /// using var listener = new FakeMeterListener("Mvp24Hours.*");
    /// 
    /// // Run code that records metrics
    /// await service.ProcessAsync();
    /// 
    /// // Verify metrics
    /// Assert.True(listener.HasMeasurement("mvp24hours.pipe.operations_total"));
    /// Assert.Equal(5, listener.GetMeasurements("mvp24hours.pipe.operations_total").Sum(m => m.Value));
    /// </code>
    /// </example>
    public sealed class FakeMeterListener : IDisposable
    {
        private readonly MeterListener _listener;
        private readonly ConcurrentBag<RecordedMeasurement> _measurements = new();
        private readonly string? _meterNameFilter;
        private bool _disposed;

        /// <summary>
        /// Gets all recorded measurements.
        /// </summary>
        public IReadOnlyList<RecordedMeasurement> Measurements => _measurements.ToList().AsReadOnly();

        /// <summary>
        /// Gets the total count of recorded measurements.
        /// </summary>
        public int MeasurementCount => _measurements.Count;

        /// <summary>
        /// Event raised when a new measurement is recorded.
        /// </summary>
        public event EventHandler<RecordedMeasurement>? MeasurementRecorded;

        /// <summary>
        /// Creates a new FakeMeterListener that listens to all Meters.
        /// </summary>
        public FakeMeterListener() : this(null)
        {
        }

        /// <summary>
        /// Creates a new FakeMeterListener with an optional meter name filter.
        /// </summary>
        /// <param name="meterNameFilter">
        /// Optional filter for meter names. Supports wildcard (*) at the end.
        /// Examples: "Mvp24Hours.*", "Mvp24Hours.Pipe", null (all meters).
        /// </param>
        public FakeMeterListener(string? meterNameFilter)
        {
            _meterNameFilter = meterNameFilter;

            _listener = new MeterListener();
            _listener.InstrumentPublished = OnInstrumentPublished;
            _listener.SetMeasurementEventCallback<byte>(OnMeasurement);
            _listener.SetMeasurementEventCallback<short>(OnMeasurement);
            _listener.SetMeasurementEventCallback<int>(OnMeasurement);
            _listener.SetMeasurementEventCallback<long>(OnMeasurement);
            _listener.SetMeasurementEventCallback<float>(OnMeasurement);
            _listener.SetMeasurementEventCallback<double>(OnMeasurement);
            _listener.SetMeasurementEventCallback<decimal>(OnMeasurement);
            _listener.Start();
        }

        private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
        {
            if (ShouldListenTo(instrument.Meter))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        }

        private bool ShouldListenTo(Meter meter)
        {
            if (string.IsNullOrEmpty(_meterNameFilter))
                return true;

            if (_meterNameFilter.EndsWith("*"))
            {
                var prefix = _meterNameFilter[..^1];
                return meter.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }

            return meter.Name.Equals(_meterNameFilter, StringComparison.OrdinalIgnoreCase);
        }

        private void OnMeasurement<T>(Instrument instrument, T measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) where T : struct
        {
            var tagsList = new List<KeyValuePair<string, object?>>();
            foreach (var tag in tags)
            {
                tagsList.Add(tag);
            }

            var recorded = new RecordedMeasurement(
                instrument.Meter.Name,
                instrument.Name,
                instrument.GetType().Name.Replace("`1", ""),
                instrument.Unit,
                Convert.ToDouble(measurement),
                tagsList);

            _measurements.Add(recorded);
            MeasurementRecorded?.Invoke(this, recorded);
        }

        /// <summary>
        /// Gets all measurements for the specified instrument.
        /// </summary>
        /// <param name="instrumentName">The instrument name to filter by.</param>
        /// <returns>A list of matching measurements.</returns>
        public IReadOnlyList<RecordedMeasurement> GetMeasurements(string instrumentName)
        {
            return _measurements
                .Where(m => m.InstrumentName.Equals(instrumentName, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all measurements for the specified meter.
        /// </summary>
        /// <param name="meterName">The meter name to filter by.</param>
        /// <returns>A list of matching measurements.</returns>
        public IReadOnlyList<RecordedMeasurement> GetMeasurementsFromMeter(string meterName)
        {
            return _measurements
                .Where(m => m.MeterName.Equals(meterName, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets all measurements matching a predicate.
        /// </summary>
        /// <param name="predicate">The predicate to filter measurements.</param>
        /// <returns>A list of matching measurements.</returns>
        public IReadOnlyList<RecordedMeasurement> GetMeasurements(Func<RecordedMeasurement, bool> predicate)
        {
            return _measurements.Where(predicate).ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if any measurement was recorded for the specified instrument.
        /// </summary>
        /// <param name="instrumentName">The instrument name to check for.</param>
        /// <returns>True if a matching measurement was found.</returns>
        public bool HasMeasurement(string instrumentName)
        {
            return _measurements.Any(m => m.InstrumentName.Equals(instrumentName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if any measurement was recorded from the specified meter.
        /// </summary>
        /// <param name="meterName">The meter name to check for.</param>
        /// <returns>True if a matching measurement was found.</returns>
        public bool HasMeasurementFromMeter(string meterName)
        {
            return _measurements.Any(m => m.MeterName.Equals(meterName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the sum of all values for the specified instrument (useful for counters).
        /// </summary>
        /// <param name="instrumentName">The instrument name.</param>
        /// <returns>The sum of all values.</returns>
        public double GetSum(string instrumentName)
        {
            return _measurements
                .Where(m => m.InstrumentName.Equals(instrumentName, StringComparison.OrdinalIgnoreCase))
                .Sum(m => m.Value);
        }

        /// <summary>
        /// Gets the average of all values for the specified instrument.
        /// </summary>
        /// <param name="instrumentName">The instrument name.</param>
        /// <returns>The average, or null if no measurements.</returns>
        public double? GetAverage(string instrumentName)
        {
            var measurements = _measurements
                .Where(m => m.InstrumentName.Equals(instrumentName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return measurements.Any() ? measurements.Average(m => m.Value) : null;
        }

        /// <summary>
        /// Gets the count of measurements for the specified instrument.
        /// </summary>
        /// <param name="instrumentName">The instrument name.</param>
        /// <returns>The count of measurements.</returns>
        public int GetCount(string instrumentName)
        {
            return _measurements
                .Count(m => m.InstrumentName.Equals(instrumentName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the last (most recent) measurement for the specified instrument.
        /// </summary>
        /// <param name="instrumentName">The instrument name.</param>
        /// <returns>The last measurement, or null if none.</returns>
        public RecordedMeasurement? GetLastMeasurement(string instrumentName)
        {
            return _measurements
                .Where(m => m.InstrumentName.Equals(instrumentName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the count of measurements by instrument name.
        /// </summary>
        /// <returns>A dictionary of instrument name to count.</returns>
        public IReadOnlyDictionary<string, int> GetMeasurementCountsByInstrument()
        {
            return _measurements
                .GroupBy(m => m.InstrumentName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets the count of measurements by meter name.
        /// </summary>
        /// <returns>A dictionary of meter name to count.</returns>
        public IReadOnlyDictionary<string, int> GetMeasurementCountsByMeter()
        {
            return _measurements
                .GroupBy(m => m.MeterName)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Clears all recorded measurements.
        /// </summary>
        public void Clear()
        {
            _measurements.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _listener.Dispose();
                _disposed = true;
            }
        }
    }
}

