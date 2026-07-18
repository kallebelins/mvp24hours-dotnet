//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Options;

namespace Mvp24Hours.Infrastructure.Sms.Services;

/// <summary>
/// In-memory implementation of SMS rate limiter for testing and development.
/// </summary>
/// <remarks>
/// <para>
/// This implementation stores rate limit counters in memory. For production use with multiple
/// instances or distributed systems, consider implementing a distributed rate limiter using
/// Redis or similar distributed cache.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemorySmsRateLimiter"/> class.
/// </remarks>
/// <param name="options">The rate limit options.</param>
/// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
public class InMemorySmsRateLimiter(IOptions<SmsRateLimitOptions> options) : ISmsRateLimiter
{
    private readonly SmsRateLimitOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly Dictionary<string, Queue<DateTimeOffset>> _counters = [];
    private readonly object _lock = new();

    /// <summary>
    /// Checks if sending a message to the specified destination is allowed.
    /// </summary>
    public Task<bool> IsAllowedAsync(string destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination cannot be null or empty.", nameof(destination));
        }

        if (!_options.Enabled)
        {
            return Task.FromResult(true);
        }

        lock (_lock)
        {
            string normalizedDestination = NormalizeDestination(destination);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset windowStart = now - _options.TimeWindow;

            // Get or create counter for destination
            if (!_counters.TryGetValue(normalizedDestination, out Queue<DateTimeOffset>? timestamps))
            {
                timestamps = new Queue<DateTimeOffset>();
                _counters[normalizedDestination] = timestamps;
            }

            // Remove timestamps outside the time window
            while (timestamps.Count > 0 && timestamps.Peek() < windowStart)
            {
                timestamps.Dequeue();
            }

            // Check if limit is exceeded
            int count = timestamps.Count;
            return Task.FromResult(count < _options.MaxMessagesPerDestination);
        }
    }

    /// <summary>
    /// Records that a message was sent to the specified destination.
    /// </summary>
    public Task RecordSentAsync(string destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination cannot be null or empty.", nameof(destination));
        }

        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            string normalizedDestination = NormalizeDestination(destination);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset windowStart = now - _options.TimeWindow;

            // Get or create counter for destination
            if (!_counters.TryGetValue(normalizedDestination, out Queue<DateTimeOffset>? timestamps))
            {
                timestamps = new Queue<DateTimeOffset>();
                _counters[normalizedDestination] = timestamps;
            }

            // Remove timestamps outside the time window
            while (timestamps.Count > 0 && timestamps.Peek() < windowStart)
            {
                timestamps.Dequeue();
            }

            // Add current timestamp
            timestamps.Enqueue(now);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the number of messages sent to the destination in the current time window.
    /// </summary>
    public Task<int> GetCountAsync(string destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination cannot be null or empty.", nameof(destination));
        }

        lock (_lock)
        {
            string normalizedDestination = NormalizeDestination(destination);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset windowStart = now - _options.TimeWindow;

            if (!_counters.TryGetValue(normalizedDestination, out Queue<DateTimeOffset>? timestamps))
            {
                return Task.FromResult(0);
            }

            // Remove timestamps outside the time window
            while (timestamps.Count > 0 && timestamps.Peek() < windowStart)
            {
                timestamps.Dequeue();
            }

            return Task.FromResult(timestamps.Count);
        }
    }

    /// <summary>
    /// Resets the rate limit for the specified destination.
    /// </summary>
    public Task ResetAsync(string destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination cannot be null or empty.", nameof(destination));
        }

        lock (_lock)
        {
            string normalizedDestination = NormalizeDestination(destination);
            _counters.Remove(normalizedDestination);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Normalizes the destination phone number for consistent key lookup.
    /// </summary>
    private static string NormalizeDestination(string destination)
    {
        // Remove common formatting characters and normalize
        return destination?.Replace(" ", "")
                          .Replace("-", "")
                          .Replace("(", "")
                          .Replace(")", "")
                          .Replace(".", "")
                          .ToLowerInvariant() ?? string.Empty;
    }
}

