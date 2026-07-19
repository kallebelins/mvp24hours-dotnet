//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Test.Helpers;

[Trait("Category", "Unit")]
public class TimeZoneHelperTest
{
    [Fact]
    public void GetTimeZoneNow_ShouldReturnDateTimeCloseToSouthAmericaOffset()
    {
        // Arrange
        DateTime utcNow = DateTime.UtcNow;

        // Act
        DateTime saNow = TimeZoneHelper.GetTimeZoneNow();

        // Assert
        double hoursDifference = Math.Abs((saNow - utcNow).TotalHours);
        hoursDifference.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public void GetTimeZone_WithUtcKind_ShouldConvertToSouthAmericaTime()
    {
        // Arrange
        DateTime utcDateTime = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Act
        DateTime converted = TimeZoneHelper.GetTimeZone(utcDateTime, DateTimeKind.Utc);

        // Assert
        converted.Kind.Should().Be(DateTimeKind.Unspecified);
        converted.Should().NotBe(utcDateTime);
    }

    [Fact]
    public void GetTimeZone_WithLocalKind_ShouldConvertFromLocalToSouthAmericaTime()
    {
        // Arrange
        DateTime localDateTime = new(2024, 6, 15, 9, 0, 0, DateTimeKind.Local);

        // Act
        DateTime converted = TimeZoneHelper.GetTimeZone(localDateTime, DateTimeKind.Local);

        // Assert
        converted.Should().NotBe(default);
    }

    [Fact]
    public void GetTimeZone_WithUnspecifiedKind_ShouldTreatAsUtc()
    {
        // Arrange
        DateTime unspecified = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        DateTime utc = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        DateTime fromUtc = TimeZoneHelper.GetTimeZone(utc, DateTimeKind.Utc);

        // Act
        DateTime converted = TimeZoneHelper.GetTimeZone(unspecified, null);

        // Assert — null kind uses DateTime.Kind; Unspecified is normalized via SpecifyKind(Utc)
        converted.Should().Be(fromUtc);
    }

    [Fact]
    public void TimeZoneIds_ShouldBeNonEmptyWithExpectedDefaults()
    {
        // Assert
        TimeZoneHelper.TimeZoneIds.Should().NotBeNullOrEmpty();
        TimeZoneHelper.TimeZoneIds.Should().Contain("E. South America Standard Time");
        TimeZoneHelper.TimeZoneIds.Should().Contain("Brazil/East");
        TimeZoneHelper.TimeZoneIds.Should().Contain("America/Sao_Paulo");
    }
}
