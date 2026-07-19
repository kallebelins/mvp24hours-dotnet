//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Testing;

namespace Mvp24Hours.Infrastructure.Test.Testing;

[Trait("Category", "Unit")]
public class FakeTimeProviderHelperTest
{
    [Fact]
    public void FixedAt_WithDateTimeOffset_ShouldAlwaysReturnSameUtcNow()
    {
        DateTimeOffset fixedTime = new(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        TimeProvider provider = FakeTimeProviderHelper.FixedAt(fixedTime);

        provider.GetUtcNow().Should().Be(fixedTime);
        provider.GetUtcNow().Should().Be(fixedTime);
    }

    [Fact]
    public void FixedAt_WithDateParts_ShouldReturnExpectedUtcNow()
    {
        TimeProvider provider = FakeTimeProviderHelper.FixedAt(2024, 1, 1, 12, 0, 0);

        provider.GetUtcNow().Should().Be(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void FromClock_WithMockClock_ShouldReflectAdvanceBy()
    {
        MockClock clock = MockClock.FromDateTime(2024, 3, 1, 8, 0, 0);
        TimeProvider provider = FakeTimeProviderHelper.FromClock(clock);

        provider.GetUtcNow().Should().Be(clock.UtcNowOffset);

        clock.AdvanceBy(TimeSpan.FromHours(2));

        provider.GetUtcNow().Should().Be(clock.UtcNowOffset);
        provider.GetUtcNow().UtcDateTime.Should().Be(new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ToClock_FromFixedProvider_ShouldReturnMatchingUtcNow()
    {
        DateTimeOffset fixedTime = new(2025, 12, 25, 0, 0, 0, TimeSpan.Zero);
        TimeProvider provider = FakeTimeProviderHelper.FixedAt(fixedTime);

        IClock clock = FakeTimeProviderHelper.ToClock(provider);

        clock.UtcNowOffset.Should().Be(fixedTime);
        clock.UtcNow.Should().Be(fixedTime.UtcDateTime);
    }

    [Fact]
    public void ToClock_RoundTripWithMockClock_ShouldPreserveAdvancingTime()
    {
        MockClock original = MockClock.FromDateTime(2024, 7, 4, 14, 0, 0);
        TimeProvider provider = FakeTimeProviderHelper.FromClock(original);
        IClock roundTrip = FakeTimeProviderHelper.ToClock(provider);

        roundTrip.UtcNowOffset.Should().Be(original.UtcNowOffset);

        original.AdvanceBy(TimeSpan.FromMinutes(45));

        roundTrip.UtcNowOffset.Should().Be(original.UtcNowOffset);
    }

    [Fact]
    public void ToClock_WithCustomTimeZone_ShouldExposeLocalTimeZone()
    {
        TimeZoneInfo utcZone = TimeZoneInfo.Utc;
        TimeProvider provider = FakeTimeProviderHelper.FixedAt(2024, 5, 10, 15, 0, 0);

        IClock clock = FakeTimeProviderHelper.ToClock(provider, utcZone);

        clock.UtcNowOffset.Should().Be(new DateTimeOffset(2024, 5, 10, 15, 0, 0, TimeSpan.Zero));
        clock.Now.Should().Be(new DateTime(2024, 5, 10, 15, 0, 0, DateTimeKind.Utc));
    }
}
