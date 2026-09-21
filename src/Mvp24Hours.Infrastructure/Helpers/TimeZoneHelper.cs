//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Helpers;

/// <summary>
/// Contains timezone functions
/// </summary>
/// <remarks>
/// <para>
/// <b>Deprecated.</b> This helper keeps process-wide mutable state: <see cref="TimeZoneIds"/> is a
/// public static list and the resolved <see cref="TimeZoneInfo"/> is cached on first use, so any
/// change to the list after the first call is silently ignored and every host in the process shares
/// the same configuration. Prefer <c>IClock</c> (<c>Mvp24Hours.Core.Contract.Infrastructure</c>) or
/// <c>TimeProvider</c>, both injectable and replaceable per host and per test.
/// </para>
/// <para>
/// <b>The replacement is not a drop-in substitution.</b> <c>GetTimeZoneNow()</c> resolves the first
/// system timezone matching <see cref="TimeZoneIds"/> (South America by default) regardless of the
/// machine's local timezone, so it is <b>not</b> equivalent to <c>IClock.Now</c> from
/// <c>SystemClock</c> or from the default <c>AddTimeProvider()</c> registration — those use
/// <see cref="TimeZoneInfo.Local"/>. To preserve the current behavior, register the clock with an
/// explicit timezone:
/// </para>
/// <code>
/// // Before:
/// services.AddMvp24HoursTimeZone(clearList: true, "America/Sao_Paulo");
/// DateTime now = TimeZoneHelper.GetTimeZoneNow();
///
/// // After:
/// TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
/// services.AddTimeProvider(TimeProvider.System, zone);   // registers IClock + TimeProvider
/// // ...then inject IClock and use clock.Now
/// </code>
/// </remarks>
[Obsolete("Use IClock (Mvp24Hours.Core.Contract.Infrastructure) or TimeProvider. Will be removed in v12.")]
public static class TimeZoneHelper
{
    public static List<string> TimeZoneIds { get; set; } = new List<string>()
    {
        { "E. South America Standard Time" },
        { "Brazil/East" },
        { "America/Sao_Paulo" }
    };

    /// <summary>
    /// Get current date and time based on South America time zone
    /// </summary>
    public static DateTime GetTimeZoneNow()
    {
        return TimeZoneInfo.ConvertTime(DateTime.UtcNow, GetTimeZoneInfo());
    }

    /// <summary>
    /// Get date and time based on South America time zone
    /// </summary>
    public static DateTime GetTimeZone(DateTime utcDateTime, DateTimeKind? kind)
    {
        DateTime dUtc = (kind ?? utcDateTime.Kind) switch
        {
            DateTimeKind.Utc => utcDateTime,
            DateTimeKind.Local => utcDateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc),
        };
        return TimeZoneInfo.ConvertTime(dUtc, GetTimeZoneInfo());
    }

    private static TimeZoneInfo? _timeZoneInfo;

    private static TimeZoneInfo GetTimeZoneInfo()
    {
        if (_timeZoneInfo != null)
        {
            return _timeZoneInfo;
        }

        _timeZoneInfo = TimeZoneInfo.GetSystemTimeZones()
            .FirstOrDefault(x => TimeZoneIds.Contains(x.Id));

        _timeZoneInfo ??= TimeZoneInfo.Local;

        return _timeZoneInfo;
    }
}
