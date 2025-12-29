//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Testing;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for TimeProvider integration with IClock.
/// Tests the adapters, extensions, and DI registration.
/// </summary>
public class TimeProviderTest
{
    #region TimeProviderAdapter Tests

    [Fact]
    public void TimeProviderAdapter_UtcNow_ReturnsTimeProviderUtcTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = FakeTimeProviderHelper.FixedAt(fixedTime);
        var adapter = new TimeProviderAdapter(timeProvider);

        // Act
        var result = adapter.UtcNow;

        // Assert
        result.Should().Be(fixedTime.UtcDateTime);
    }

    [Fact]
    public void TimeProviderAdapter_UtcNowOffset_ReturnsTimeProviderOffset()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = FakeTimeProviderHelper.FixedAt(fixedTime);
        var adapter = new TimeProviderAdapter(timeProvider);

        // Act
        var result = adapter.UtcNowOffset;

        // Assert
        result.Should().Be(fixedTime);
    }

    [Fact]
    public void TimeProviderAdapter_UtcToday_ReturnsDatePart()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = FakeTimeProviderHelper.FixedAt(fixedTime);
        var adapter = new TimeProviderAdapter(timeProvider);

        // Act
        var result = adapter.UtcToday;

        // Assert
        result.Should().Be(new DateTime(2024, 6, 15));
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void TimeProviderAdapter_System_UsesSystemTimeProvider()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var result = TimeProviderAdapter.System.UtcNow;
        var after = DateTime.UtcNow;

        // Assert
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void TimeProviderAdapter_WithCustomTimeZone_ConvertsLocalTimeCorrectly()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero); // UTC noon
        var timeProvider = FakeTimeProviderHelper.FixedAt(fixedTime);
        var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var adapter = new TimeProviderAdapter(timeProvider, easternZone);

        // Act
        var localTime = adapter.Now;

        // Assert
        // Eastern Daylight Time is UTC-4 in June
        localTime.Hour.Should().Be(8); // 12 UTC - 4 = 8 EDT
    }

    #endregion

    #region ClockAdapter Tests

    [Fact]
    public void ClockAdapter_GetUtcNow_ReturnsClockTime()
    {
        // Arrange
        var testClock = new TestClock(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));
        var adapter = new ClockAdapter(testClock);

        // Act
        var result = adapter.GetUtcNow();

        // Assert
        result.Should().Be(new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ClockAdapter_GetUtcNow_ReflectsClockChanges()
    {
        // Arrange
        var testClock = new TestClock(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));
        var adapter = new ClockAdapter(testClock);

        // Act
        var before = adapter.GetUtcNow();
        testClock.AdvanceBy(TimeSpan.FromHours(1));
        var after = adapter.GetUtcNow();

        // Assert
        before.Hour.Should().Be(10);
        after.Hour.Should().Be(11);
    }

    [Fact]
    public void ClockAdapter_LocalTimeZone_ReturnsConfiguredZone()
    {
        // Arrange
        var testClock = new TestClock();
        var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var adapter = new ClockAdapter(testClock, easternZone);

        // Act
        var result = adapter.LocalTimeZone;

        // Assert
        result.Should().Be(easternZone);
    }

    #endregion

    #region DI Registration Tests

    [Fact]
    public void AddTimeProvider_RegistersBothTimeProviderAndIClock()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTimeProvider();
        var provider = services.BuildServiceProvider();

        // Assert
        var timeProvider = provider.GetService<TimeProvider>();
        var clock = provider.GetService<IClock>();

        timeProvider.Should().NotBeNull();
        timeProvider.Should().Be(TimeProvider.System);
        clock.Should().NotBeNull();
        clock.Should().BeOfType<TimeProviderAdapter>();
    }

    [Fact]
    public void AddTimeProvider_WithCustomProvider_RegistersCustomProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var fixedTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var customProvider = FakeTimeProviderHelper.FixedAt(fixedTime);

        // Act
        services.AddTimeProvider(customProvider);
        var provider = services.BuildServiceProvider();

        // Assert
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        var clock = provider.GetRequiredService<IClock>();

        timeProvider.GetUtcNow().Should().Be(fixedTime);
        clock.UtcNow.Should().Be(fixedTime.UtcDateTime);
    }

    [Fact]
    public void AddClock_RegistersBothClockAndTimeProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var testClock = new TestClock(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));

        // Act
        services.AddClock(testClock);
        var provider = services.BuildServiceProvider();

        // Assert
        var clock = provider.GetService<IClock>();
        var timeProvider = provider.GetService<TimeProvider>();

        clock.Should().Be(testClock);
        timeProvider.Should().NotBeNull();
        timeProvider.Should().BeOfType<ClockAdapter>();
    }

    [Fact]
    public void AddSystemClock_RegistersSystemClockInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSystemClock();
        var provider = services.BuildServiceProvider();

        // Assert
        var clock = provider.GetRequiredService<IClock>();
        clock.Should().Be(SystemClock.Instance);
    }

    [Fact]
    public void ReplaceTimeProvider_ReplacesExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTimeProvider(); // Register system time

        var fixedTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var customProvider = FakeTimeProviderHelper.FixedAt(fixedTime);

        // Act
        services.ReplaceTimeProvider(customProvider);
        var provider = services.BuildServiceProvider();

        // Assert
        var timeProvider = provider.GetRequiredService<TimeProvider>();
        timeProvider.GetUtcNow().Should().Be(fixedTime);
    }

    [Fact]
    public void ReplaceClock_ReplacesExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSystemClock(); // Register system clock

        var testClock = new TestClock(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));

        // Act
        services.ReplaceClock(testClock);
        var provider = services.BuildServiceProvider();

        // Assert
        var clock = provider.GetRequiredService<IClock>();
        clock.UtcNow.Should().Be(testClock.UtcNow);
    }

    #endregion

    #region FakeTimeProviderHelper Tests

    [Fact]
    public void FakeTimeProviderHelper_FixedAt_CreatesFixedTimeProvider()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var provider = FakeTimeProviderHelper.FixedAt(fixedTime);

        // Assert
        provider.GetUtcNow().Should().Be(fixedTime);
    }

    [Fact]
    public void FakeTimeProviderHelper_FixedAt_WithDateTimeParams_CreatesFixedTimeProvider()
    {
        // Act
        var provider = FakeTimeProviderHelper.FixedAt(2024, 6, 15, 10, 30, 0);

        // Assert
        provider.GetUtcNow().Should().Be(new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void FakeTimeProviderHelper_FromClock_WrapsClockAsTimeProvider()
    {
        // Arrange
        var testClock = new TestClock(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));

        // Act
        var timeProvider = FakeTimeProviderHelper.FromClock(testClock);

        // Assert
        timeProvider.GetUtcNow().UtcDateTime.Should().Be(testClock.UtcNow);
    }

    [Fact]
    public void FakeTimeProviderHelper_ToClock_WrapsTimeProviderAsClock()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = FakeTimeProviderHelper.FixedAt(fixedTime);

        // Act
        var clock = FakeTimeProviderHelper.ToClock(timeProvider);

        // Assert
        clock.UtcNow.Should().Be(fixedTime.UtcDateTime);
    }

    #endregion

    #region Bidirectional Adapter Tests

    [Fact]
    public void RoundTrip_TimeProvider_Clock_TimeProvider_PreservesTime()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var originalProvider = FakeTimeProviderHelper.FixedAt(fixedTime);

        // Act: TimeProvider -> IClock -> TimeProvider
        var clock = new TimeProviderAdapter(originalProvider);
        var restoredProvider = new ClockAdapter(clock);

        // Assert
        restoredProvider.GetUtcNow().Should().Be(fixedTime);
    }

    [Fact]
    public void RoundTrip_Clock_TimeProvider_Clock_PreservesTime()
    {
        // Arrange
        var testClock = new TestClock(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));

        // Act: IClock -> TimeProvider -> IClock
        var timeProvider = new ClockAdapter(testClock);
        var restoredClock = new TimeProviderAdapter(timeProvider);

        // Assert
        restoredClock.UtcNow.Should().Be(testClock.UtcNow);
    }

    [Fact]
    public void TimeAdvancement_ThroughAdapter_IsReflected()
    {
        // Arrange
        var testClock = new TestClock(new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));
        var timeProvider = new ClockAdapter(testClock);

        // Act
        var before = timeProvider.GetUtcNow();
        testClock.AdvanceBy(TimeSpan.FromHours(2));
        var after = timeProvider.GetUtcNow();

        // Assert
        before.Hour.Should().Be(10);
        after.Hour.Should().Be(12);
        (after - before).Should().Be(TimeSpan.FromHours(2));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TimeProviderAdapter_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new TimeProviderAdapter(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void ClockAdapter_NullClock_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new ClockAdapter(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
    }

    [Fact]
    public void AddTimeProvider_NullTimeProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var action = () => services.AddTimeProvider(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void AddClock_NullClock_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var action = () => services.AddClock(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("clock");
    }

    #endregion
}

