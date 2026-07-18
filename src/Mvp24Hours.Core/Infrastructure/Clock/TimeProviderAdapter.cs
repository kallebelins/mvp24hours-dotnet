//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Mvp24Hours.Core.Contract.Infrastructure;

namespace Mvp24Hours.Core.Infrastructure.Clock;

/// <summary>
/// Adapter that wraps a <see cref="TimeProvider"/> to implement <see cref="IClock"/>.
/// </summary>
/// <remarks>
/// <para>
/// This adapter enables using the native .NET 8+ TimeProvider with existing code that depends on IClock.
/// Use <see cref="TimeProvider.System"/> for production or <see cref="Microsoft.Extensions.Time.Testing.FakeTimeProvider"/>
/// for testing.
/// </para>
/// <para>
/// Benefits of using TimeProvider over custom implementations:
/// - Standard .NET abstraction (works with all .NET 8+ libraries)
/// - Built-in FakeTimeProvider for testing (Microsoft.Extensions.TimeProvider.Testing)
/// - Future-proof: .NET 9 and beyond will continue to use this API
/// - No custom code to maintain
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In production (using system time)
/// services.AddSingleton(TimeProvider.System);
/// services.AddSingleton&lt;IClock, TimeProviderAdapter&gt;();
/// 
/// // In tests (using fake time)
/// var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero));
/// services.AddSingleton&lt;TimeProvider&gt;(fakeTime);
/// services.AddSingleton&lt;IClock, TimeProviderAdapter&gt;();
/// 
/// // Advance time in tests
/// fakeTime.Advance(TimeSpan.FromHours(1));
/// </code>
/// </example>
/// <remarks>
/// Creates a new adapter wrapping the specified TimeProvider with custom timezone.
/// </remarks>
/// <param name="timeProvider">The TimeProvider to wrap.</param>
/// <param name="localTimeZone">The timezone to use for local time conversions.</param>
public sealed class TimeProviderAdapter(TimeProvider timeProvider, TimeZoneInfo localTimeZone) : IClock
{

    /// <summary>
    /// Creates a new adapter wrapping the specified TimeProvider.
    /// </summary>
    /// <param name="timeProvider">The TimeProvider to wrap.</param>
    public TimeProviderAdapter(TimeProvider timeProvider)
        : this(timeProvider, TimeZoneInfo.Local)
    {
    }

    /// <inheritdoc />
    public DateTime UtcNow => TimeProvider.GetUtcNow().UtcDateTime;

    /// <inheritdoc />
    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, LocalTimeZone);

    /// <inheritdoc />
    public DateTime UtcToday => UtcNow.Date;

    /// <inheritdoc />
    public DateTime Today => Now.Date;

    /// <inheritdoc />
    public DateTimeOffset UtcNowOffset => TimeProvider.GetUtcNow();

    /// <inheritdoc />
    public DateTimeOffset NowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, LocalTimeZone);

    /// <summary>
    /// Gets the underlying TimeProvider.
    /// </summary>
    public TimeProvider TimeProvider { get; } = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <summary>
    /// Gets the local timezone used for conversions.
    /// </summary>
    public TimeZoneInfo LocalTimeZone { get; } = localTimeZone ?? throw new ArgumentNullException(nameof(localTimeZone));

    /// <summary>
    /// Creates a default adapter using TimeProvider.System.
    /// </summary>
    public static TimeProviderAdapter System { get; } = new(TimeProvider.System);
}

