//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Infrastructure.Clock;
using Mvp24Hours.Core.Infrastructure.GuidGenerators;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Clock and GUID Generator implementations.
/// </summary>
[Trait("Category", "Unit")]
public class ClockAndGuidTest
{
    #region SystemClock Tests

    [Fact]
    public void SystemClock_UtcNow_ReturnsCurrentUtcTime()
    {
        // Arrange
        SystemClock clock = SystemClock.Instance;
        DateTime before = DateTime.UtcNow;

        // Act
        DateTime result = clock.UtcNow;

        // Assert
        DateTime after = DateTime.UtcNow;
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void SystemClock_Now_ReturnsCurrentLocalTime()
    {
        // Arrange
        SystemClock clock = SystemClock.Instance;
        DateTime before = DateTime.Now;

        // Act
        DateTime result = clock.Now;

        // Assert
        DateTime after = DateTime.Now;
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void SystemClock_UtcToday_ReturnsCurrentUtcDate()
    {
        // Arrange
        SystemClock clock = SystemClock.Instance;

        // Act
        DateTime result = clock.UtcToday;

        // Assert
        result.Should().Be(DateTime.UtcNow.Date);
        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void SystemClock_Today_ReturnsCurrentLocalDate()
    {
        // Arrange
        SystemClock clock = SystemClock.Instance;

        // Act
        DateTime result = clock.Today;

        // Assert
        result.Should().Be(DateTime.Today);
    }

    [Fact]
    public void SystemClock_UtcNowOffset_ReturnsCurrentUtcOffset()
    {
        // Arrange
        SystemClock clock = SystemClock.Instance;
        DateTimeOffset before = DateTimeOffset.UtcNow;

        // Act
        DateTimeOffset result = clock.UtcNowOffset;

        // Assert
        DateTimeOffset after = DateTimeOffset.UtcNow;
        result.Should().BeOnOrAfter(before);
        result.Should().BeOnOrBefore(after);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SystemClock_Instance_ReturnsSingletonInstance()
    {
        // Act
        SystemClock instance1 = SystemClock.Instance;
        SystemClock instance2 = SystemClock.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    #endregion

    #region TestClock Tests

    [Fact]
    public void TestClock_Creation_WithInitialTime_SetsCorrectTime()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        var clock = new TestClock(initialTime);

        // Assert
        clock.UtcNow.Should().Be(initialTime);
    }

    [Fact]
    public void TestClock_DefaultCreation_SetsCurrentTime()
    {
        // Arrange
        DateTime before = DateTime.UtcNow;

        // Act
        var clock = new TestClock();

        // Assert
        DateTime after = DateTime.UtcNow;
        clock.UtcNow.Should().BeOnOrAfter(before);
        clock.UtcNow.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void TestClock_AdvanceBy_AdvancesTime()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        clock.AdvanceBy(TimeSpan.FromHours(2));

        // Assert
        clock.UtcNow.Should().Be(new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TestClock_AdvanceSeconds_AdvancesTimeBySeconds()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        clock.AdvanceSeconds(30);

        // Assert
        clock.UtcNow.Second.Should().Be(30);
    }

    [Fact]
    public void TestClock_AdvanceMinutes_AdvancesTimeByMinutes()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        clock.AdvanceMinutes(45);

        // Assert
        clock.UtcNow.Minute.Should().Be(45);
    }

    [Fact]
    public void TestClock_AdvanceHours_AdvancesTimeByHours()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        clock.AdvanceHours(5);

        // Assert
        clock.UtcNow.Hour.Should().Be(15);
    }

    [Fact]
    public void TestClock_AdvanceDays_AdvancesTimeByDays()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        clock.AdvanceDays(10);

        // Assert
        clock.UtcNow.Day.Should().Be(25);
    }

    [Fact]
    public void TestClock_RewindBy_RewindsTime()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        clock.RewindBy(TimeSpan.FromHours(2));

        // Assert
        clock.UtcNow.Should().Be(new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TestClock_SetUtcNow_SetsSpecificTime()
    {
        // Arrange
        var clock = new TestClock();
        var newTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        clock.SetUtcNow(newTime);

        // Assert
        clock.UtcNow.Should().Be(newTime);
    }

    [Fact]
    public void TestClock_Reset_ResetsToInitialTime()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);
        clock.AdvanceBy(TimeSpan.FromDays(30));

        // Act
        clock.Reset();

        // Assert
        clock.UtcNow.Should().Be(initialTime);
    }

    [Fact]
    public void TestClock_UtcToday_ReturnsDateOnly()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 30, 45, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        DateTime result = clock.UtcToday;

        // Assert
        result.Should().Be(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TestClock_UtcNowOffset_ReturnsCorrectOffset()
    {
        // Arrange
        var initialTime = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);

        // Act
        DateTimeOffset result = clock.UtcNowOffset;

        // Assert
        result.UtcDateTime.Should().Be(initialTime);
        result.Offset.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region StandardGuidGenerator Tests

    [Fact]
    public void StandardGuidGenerator_NewGuid_ReturnsUniqueGuids()
    {
        // Arrange
        StandardGuidGenerator generator = StandardGuidGenerator.Instance;
        var guids = new HashSet<Guid>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            guids.Add(generator.NewGuid());
        }

        // Assert
        guids.Should().HaveCount(1000);
    }

    [Fact]
    public void StandardGuidGenerator_NewGuid_ReturnsNonEmptyGuids()
    {
        // Arrange
        StandardGuidGenerator generator = StandardGuidGenerator.Instance;

        // Act
        Guid guid = generator.NewGuid();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void StandardGuidGenerator_Instance_ReturnsSingletonInstance()
    {
        // Act
        StandardGuidGenerator instance1 = StandardGuidGenerator.Instance;
        StandardGuidGenerator instance2 = StandardGuidGenerator.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    #endregion

    #region SequentialGuidGenerator Tests

    [Fact]
    public void SequentialGuidGenerator_NewGuid_ReturnsSequentialGuids()
    {
        // Arrange
        var generator = new SequentialGuidGenerator(SequentialGuidType.SqlServer);
        var guids = new List<Guid>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            guids.Add(generator.NewGuid());
            Thread.Sleep(1); // Small delay to ensure different timestamps
        }

        // Assert - Verify uniqueness
        guids.Distinct().Should().HaveCount(100);
    }

    [Fact]
    public void SequentialGuidGenerator_SqlServer_ReturnsNonEmptyGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = SequentialGuidGenerator.SqlServer;

        // Act
        Guid guid = generator.NewGuid();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SequentialGuidGenerator_PostgreSql_ReturnsNonEmptyGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = SequentialGuidGenerator.PostgreSql;

        // Act
        Guid guid = generator.NewGuid();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SequentialGuidGenerator_MySql_ReturnsNonEmptyGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = SequentialGuidGenerator.MySql;

        // Act
        Guid guid = generator.NewGuid();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SequentialGuidGenerator_Binary_ReturnsNonEmptyGuids()
    {
        // Arrange
        SequentialGuidGenerator generator = SequentialGuidGenerator.Binary;

        // Act
        Guid guid = generator.NewGuid();

        // Assert
        guid.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SequentialGuidGenerator_StaticMethod_ReturnsSequentialGuids()
    {
        // Act
        Guid guid1 = SequentialGuidGenerator.NewSequentialGuid();
        Thread.Sleep(1);
        Guid guid2 = SequentialGuidGenerator.NewSequentialGuid();

        // Assert
        guid1.Should().NotBe(guid2);
    }

    #endregion

    #region DeterministicGuidGenerator Tests

    [Fact]
    public void DeterministicGuidGenerator_Sequential_ReturnsSequentialGuids()
    {
        // Arrange
        var generator = new DeterministicGuidGenerator();

        // Act
        Guid first = generator.NewGuid();
        Guid second = generator.NewGuid();
        Guid third = generator.NewGuid();

        // Assert
        first.Should().NotBe(second);
        second.Should().NotBe(third);
    }

    [Fact]
    public void DeterministicGuidGenerator_Sequential_ReturnsPredictableSequence()
    {
        // Arrange
        var generator1 = new DeterministicGuidGenerator();
        var generator2 = new DeterministicGuidGenerator();

        // Act
        Guid first1 = generator1.NewGuid();
        Guid first2 = generator2.NewGuid();

        // Assert
        first1.Should().Be(first2);
    }

    [Fact]
    public void DeterministicGuidGenerator_WithPredefinedGuids_ReturnsInOrder()
    {
        // Arrange
        var guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var guid3 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var generator = new DeterministicGuidGenerator(guid1, guid2, guid3);

        // Act & Assert
        generator.NewGuid().Should().Be(guid1);
        generator.NewGuid().Should().Be(guid2);
        generator.NewGuid().Should().Be(guid3);
    }

    [Fact]
    public void DeterministicGuidGenerator_WithPredefinedGuids_ThrowsWhenExhausted()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var generator = new DeterministicGuidGenerator(guid1);

        // Act
        generator.NewGuid(); // Consume the only GUID
        Func<Guid> act = () => generator.NewGuid();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No more predefined GUIDs*");
    }

    [Fact]
    public void DeterministicGuidGenerator_AddGuids_AddsMoreGuids()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var generator = new DeterministicGuidGenerator(guid1);
        generator.NewGuid(); // Consume guid1

        // Act
        generator.AddGuids(guid2);
        Guid result = generator.NewGuid();

        // Assert
        result.Should().Be(guid2);
    }

    [Fact]
    public void DeterministicGuidGenerator_RemainingCount_ReturnsCorrectCount()
    {
        // Arrange
        var generator = new DeterministicGuidGenerator(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Assert
        generator.RemainingCount.Should().Be(3);
        generator.NewGuid();
        generator.RemainingCount.Should().Be(2);
    }

    [Fact]
    public void DeterministicGuidGenerator_Reset_ClearsQueueAndResetCounter()
    {
        // Arrange
        var generator = new DeterministicGuidGenerator();
        generator.NewGuid();
        generator.NewGuid();

        // Act
        generator.Reset();
        Guid result = generator.NewGuid();

        // Assert - After reset, should start from 1 again
        var generator2 = new DeterministicGuidGenerator();
        Guid expected = generator2.NewGuid();
        result.Should().Be(expected);
    }

    [Fact]
    public void DeterministicGuidGenerator_FromNumber_CreatesGuidFromNumber()
    {
        // Act
        Guid guid1 = DeterministicGuidGenerator.FromNumber(1);
        Guid guid2 = DeterministicGuidGenerator.FromNumber(2);
        Guid guid100 = DeterministicGuidGenerator.FromNumber(100);

        // Assert
        guid1.Should().NotBe(guid2);
        guid1.Should().NotBe(guid100);
        guid2.Should().NotBe(guid100);
    }

    [Fact]
    public void DeterministicGuidGenerator_FromNumber_SameNumberReturnsSameGuid()
    {
        // Act
        Guid guid1 = DeterministicGuidGenerator.FromNumber(42);
        Guid guid2 = DeterministicGuidGenerator.FromNumber(42);

        // Assert
        guid1.Should().Be(guid2);
    }

    [Fact]
    public void DeterministicGuidGenerator_FromSeed_CreatesDeterministicGuid()
    {
        // Act
        Guid guid1 = DeterministicGuidGenerator.FromSeed("test-seed");
        Guid guid2 = DeterministicGuidGenerator.FromSeed("test-seed");
        Guid guid3 = DeterministicGuidGenerator.FromSeed("different-seed");

        // Assert
        guid1.Should().Be(guid2);
        guid1.Should().NotBe(guid3);
    }

    [Fact]
    public void DeterministicGuidGenerator_FromSeed_WithNull_ThrowsArgumentNullException()
    {
        // Act
        Func<Guid> act = () => DeterministicGuidGenerator.FromSeed(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DeterministicGuidGenerator_FromSeed_WithEmpty_ThrowsArgumentNullException()
    {
        // Act
        Func<Guid> act = () => DeterministicGuidGenerator.FromSeed(string.Empty);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void TestClock_CanBeUsedForTimeDependentTests()
    {
        // Arrange
        var initialTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = new TestClock(initialTime);
        var expirationDays = 30;
        DateTime createdAt = clock.UtcNow;
        DateTime expiresAt = createdAt.AddDays(expirationDays);

        // Act - Advance time past expiration
        clock.AdvanceDays(31);

        // Assert
        (clock.UtcNow > expiresAt).Should().BeTrue();
    }

    [Fact]
    public void DeterministicGuidGenerator_CanBeUsedForTestAssertions()
    {
        // Arrange
        var expectedId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var generator = new DeterministicGuidGenerator(expectedId);

        // Act
        Guid generatedId = generator.NewGuid();

        // Assert
        generatedId.Should().Be(expectedId);
    }

    #endregion
}

