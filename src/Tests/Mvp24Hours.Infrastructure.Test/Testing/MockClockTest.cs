//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Testing;

namespace Mvp24Hours.Infrastructure.Test.Testing;

[Trait("Category", "Unit")]
public class MockClockTest
{
    [Fact]
    public void FromDate_ShouldInitializeUtcMidnight()
    {
        MockClock clock = MockClock.FromDate(2024, 8, 20);

        clock.UtcNow.Should().Be(new DateTime(2024, 8, 20, 0, 0, 0, DateTimeKind.Utc));
        clock.UtcToday.Should().Be(new DateTime(2024, 8, 20, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void FromDateTime_ShouldInitializeExactUtcTime()
    {
        MockClock clock = MockClock.FromDateTime(2024, 8, 20, 13, 45, 30);

        clock.UtcNow.Should().Be(new DateTime(2024, 8, 20, 13, 45, 30, DateTimeKind.Utc));
    }

    [Fact]
    public void AdvanceBy_ShouldMoveUtcNowForward()
    {
        MockClock clock = MockClock.FromDateTime(2024, 1, 1, 0, 0, 0);

        clock.AdvanceBy(TimeSpan.FromHours(3));

        clock.UtcNow.Should().Be(new DateTime(2024, 1, 1, 3, 0, 0, DateTimeKind.Utc));
        clock.UtcNowOffset.Should().Be(new DateTimeOffset(2024, 1, 1, 3, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void RewindBy_ShouldMoveUtcNowBackward()
    {
        MockClock clock = MockClock.FromDateTime(2024, 1, 1, 12, 0, 0);

        clock.RewindBy(TimeSpan.FromMinutes(30));

        clock.UtcNow.Should().Be(new DateTime(2024, 1, 1, 11, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Reset_ShouldRestoreInitialTime()
    {
        MockClock clock = MockClock.FromDateTime(2024, 2, 1, 9, 0, 0);
        clock.AdvanceBy(TimeSpan.FromDays(2));
        clock.SetUtcNow(new DateTime(2024, 2, 10, 0, 0, 0, DateTimeKind.Utc));

        clock.Reset();

        clock.UtcNow.Should().Be(new DateTime(2024, 2, 1, 9, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void AdvanceHours_ShouldUseConvenienceMethod()
    {
        MockClock clock = MockClock.FromDateTime(2024, 3, 1, 0, 0, 0);

        clock.AdvanceHours(5);

        clock.UtcNow.Hour.Should().Be(5);
    }

    [Fact]
    public void FromYear_ShouldStartAtJanuaryFirst()
    {
        MockClock clock = MockClock.FromYear(2023);

        clock.UtcNow.Should().Be(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
