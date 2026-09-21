//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class TimeZoneHelperTest
{
    /// <summary>
    /// Resolves the timezone the way <c>TimeZoneHelper</c> resolves it internally, so the
    /// equivalence tests below do not depend on the machine's local timezone.
    /// </summary>
    private static TimeZoneInfo ResolveHelperTimeZone()
    {
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        List<string> ids = TimeZoneHelper.TimeZoneIds;
#pragma warning restore CS0618
        return TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => ids.Contains(x.Id))
            ?? TimeZoneInfo.Local;
    }

    [Fact]
    public void GetTimeZoneNow_ShouldReturnDateTimeCloseToSouthAmericaOffset()
    {
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        // Arrange
        DateTime utcNow = DateTime.UtcNow;

        // Act
        DateTime saNow = TimeZoneHelper.GetTimeZoneNow();

        // Assert
        double hoursDifference = Math.Abs((saNow - utcNow).TotalHours);
        hoursDifference.Should().BeLessThanOrEqualTo(5);
#pragma warning restore CS0618
    }

    [Fact]
    public void GetTimeZone_WithUtcKind_ShouldConvertToSouthAmericaTime()
    {
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        // Arrange
        DateTime utcDateTime = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        DateTime converted = TimeZoneHelper.GetTimeZone(utcDateTime, DateTimeKind.Utc);

        // Assert
        converted.Kind.Should().Be(DateTimeKind.Unspecified);
        converted.Should().NotBe(utcDateTime);
#pragma warning restore CS0618
    }

    [Fact]
    public void GetTimeZone_WithLocalKind_ShouldConvertFromLocalToSouthAmericaTime()
    {
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        // Arrange
        DateTime localDateTime = new(2024, 6, 15, 9, 0, 0, DateTimeKind.Local);

        // Act
        DateTime converted = TimeZoneHelper.GetTimeZone(localDateTime, DateTimeKind.Local);

        // Assert
        converted.Should().NotBe(default);
#pragma warning restore CS0618
    }

    [Fact]
    public void GetTimeZone_WithUnspecifiedKind_ShouldTreatAsUtc()
    {
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        // Arrange
        DateTime unspecified = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        DateTime utc = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        DateTime fromUtc = TimeZoneHelper.GetTimeZone(utc, DateTimeKind.Utc);

        // Act
        DateTime converted = TimeZoneHelper.GetTimeZone(unspecified, null);

        // Assert — null kind uses DateTime.Kind; Unspecified is normalized via SpecifyKind(Utc)
        converted.Should().Be(fromUtc);
#pragma warning restore CS0618
    }

    [Fact]
    public void TimeZoneIds_ShouldBeNonEmptyWithExpectedDefaults()
    {
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        // Assert
        TimeZoneHelper.TimeZoneIds.Should().NotBeNullOrEmpty();
        TimeZoneHelper.TimeZoneIds.Should().Contain("E. South America Standard Time");
        TimeZoneHelper.TimeZoneIds.Should().Contain("Brazil/East");
        TimeZoneHelper.TimeZoneIds.Should().Contain("America/Sao_Paulo");
#pragma warning restore CS0618
    }

    #region [ IClock equivalence (task 4.2b) ]

    /// <summary>
    /// Documents the supported migration path: an <c>IClock</c> built from
    /// <c>TimeProviderAdapter</c> with the <b>same explicit timezone</b> the helper resolves is
    /// equivalent to <c>TimeZoneHelper.GetTimeZoneNow()</c>.
    /// </summary>
    [Fact]
    public void GetTimeZoneNow_MatchesIClockNow_WhenClockIsRegisteredWithTheSameTimeZone()
    {
        // Arrange
        TimeZoneInfo zone = ResolveHelperTimeZone();
        IClock clock = new TimeProviderAdapter(TimeProvider.System, zone);

        // Act
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        DateTime helperNow = TimeZoneHelper.GetTimeZoneNow();
#pragma warning restore CS0618
        DateTime clockNow = clock.Now;

        // Assert
        Math.Abs((clockNow - helperNow).TotalSeconds).Should().BeLessThan(5);
    }

    /// <summary>
    /// Documents why the 34 <c>TimeZoneHelper</c> call sites cannot be swapped mechanically:
    /// the default clock registrations (<c>SystemClock</c>, <c>AddTimeProvider()</c>,
    /// <c>AddSystemClock()</c>) use <see cref="TimeZoneInfo.Local"/>, while the helper resolves the
    /// first system timezone matching <c>TimeZoneIds</c>. The two agree only when both offsets match.
    /// </summary>
    [Fact]
    public void GetTimeZoneNow_DiffersFromDefaultIClockNow_ByTheLocalTimeZoneOffsetDelta()
    {
        // Arrange
        TimeZoneInfo zone = ResolveHelperTimeZone();
        DateTime utcNow = DateTime.UtcNow;
        TimeSpan expectedDelta = TimeZoneInfo.Local.GetUtcOffset(utcNow) - zone.GetUtcOffset(utcNow);

        // Act
        // intentional: covers the obsolete TimeZoneHelper until removal in v12
#pragma warning disable CS0618
        DateTime helperNow = TimeZoneHelper.GetTimeZoneNow();
#pragma warning restore CS0618
        DateTime defaultClockNow = SystemClock.Instance.Now;

        // Assert
        TimeSpan actualDelta = defaultClockNow - helperNow;
        Math.Abs((actualDelta - expectedDelta).TotalSeconds).Should().BeLessThan(5);
    }

    #endregion
}
