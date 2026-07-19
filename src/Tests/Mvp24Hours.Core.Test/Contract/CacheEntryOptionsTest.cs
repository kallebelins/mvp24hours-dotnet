//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Caching;

namespace Mvp24Hours.Core.Test.Contract;

/// <summary>
/// Unit tests for CacheEntryOptions and CacheEntryPriority.
/// </summary>
[Trait("Category", "Unit")]
public class CacheEntryOptionsTest
{
    #region Default Values

    [Fact]
    public void CacheEntryOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new CacheEntryOptions();

        // Assert
        options.AbsoluteExpiration.Should().BeNull();
        options.AbsoluteExpirationRelativeToNow.Should().BeNull();
        options.SlidingExpiration.Should().BeNull();
        options.Priority.Should().Be(CacheEntryPriority.Normal);
        options.Tags.Should().BeNull();
        options.Dependencies.Should().BeNull();
    }

    #endregion

    #region Factory Method Tests

    [Fact]
    public void CacheEntryOptions_FromDuration_SetsAbsoluteExpirationRelativeToNow()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(5);

        // Act
        var options = CacheEntryOptions.FromDuration(duration);

        // Assert
        options.AbsoluteExpirationRelativeToNow.Should().Be(duration);
        options.SlidingExpiration.Should().BeNull();
        options.AbsoluteExpiration.Should().BeNull();
    }

    [Fact]
    public void CacheEntryOptions_WithSlidingExpiration_SetsSlidingExpiration()
    {
        // Arrange
        var slidingExpiration = TimeSpan.FromMinutes(2);

        // Act
        var options = CacheEntryOptions.WithSlidingExpiration(slidingExpiration);

        // Assert
        options.SlidingExpiration.Should().Be(slidingExpiration);
        options.AbsoluteExpirationRelativeToNow.Should().BeNull();
        options.AbsoluteExpiration.Should().BeNull();
    }

    [Fact]
    public void CacheEntryOptions_WithBothExpirations_SetsBothValues()
    {
        // Arrange
        var absoluteExpiration = TimeSpan.FromMinutes(10);
        var slidingExpiration = TimeSpan.FromMinutes(2);

        // Act
        var options = CacheEntryOptions.WithBothExpirations(absoluteExpiration, slidingExpiration);

        // Assert
        options.AbsoluteExpirationRelativeToNow.Should().Be(absoluteExpiration);
        options.SlidingExpiration.Should().Be(slidingExpiration);
        options.AbsoluteExpiration.Should().BeNull();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void CacheEntryOptions_AbsoluteExpiration_CanBeSet()
    {
        // Arrange
        var absoluteExpiration = DateTimeOffset.UtcNow.AddHours(1);
        var options = new CacheEntryOptions();

        // Act
        options.AbsoluteExpiration = absoluteExpiration;

        // Assert
        options.AbsoluteExpiration.Should().Be(absoluteExpiration);
    }

    [Fact]
    public void CacheEntryOptions_Priority_CanBeChanged()
    {
        // Arrange
        var options = new CacheEntryOptions();

        // Act
        options.Priority = CacheEntryPriority.High;

        // Assert
        options.Priority.Should().Be(CacheEntryPriority.High);
    }

    [Fact]
    public void CacheEntryOptions_Tags_CanBeSet()
    {
        // Arrange
        var options = new CacheEntryOptions();
        var tags = new List<string> { "products", "category:electronics" };

        // Act
        options.Tags = tags;

        // Assert
        options.Tags.Should().BeEquivalentTo(tags);
    }

    [Fact]
    public void CacheEntryOptions_Dependencies_CanBeSet()
    {
        // Arrange
        var options = new CacheEntryOptions();
        var deps = new List<string> { "config:global", "user:123:permissions" };

        // Act
        options.Dependencies = deps;

        // Assert
        options.Dependencies.Should().BeEquivalentTo(deps);
    }

    #endregion

    #region CacheEntryPriority Tests

    [Fact]
    public void CacheEntryPriority_HasCorrectValues()
    {
        // Assert
        ((int)CacheEntryPriority.Low).Should().Be(0);
        ((int)CacheEntryPriority.Normal).Should().Be(1);
        ((int)CacheEntryPriority.High).Should().Be(2);
        ((int)CacheEntryPriority.NeverRemove).Should().Be(3);
    }

    [Theory]
    [InlineData(CacheEntryPriority.Low)]
    [InlineData(CacheEntryPriority.Normal)]
    [InlineData(CacheEntryPriority.High)]
    [InlineData(CacheEntryPriority.NeverRemove)]
    public void CacheEntryOptions_AllPriorities_CanBeAssigned(CacheEntryPriority priority)
    {
        // Arrange
        var options = new CacheEntryOptions();

        // Act
        options.Priority = priority;

        // Assert
        options.Priority.Should().Be(priority);
    }

    #endregion

    #region Combination Tests

    [Fact]
    public void CacheEntryOptions_FullConfig_AllPropertiesSet()
    {
        // Arrange & Act
        var options = new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Priority = CacheEntryPriority.High,
            Tags = new List<string> { "users", "active" },
            Dependencies = new List<string> { "config:global" }
        };

        // Assert
        options.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromHours(1));
        options.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(15));
        options.Priority.Should().Be(CacheEntryPriority.High);
        options.Tags.Should().HaveCount(2);
        options.Dependencies.Should().HaveCount(1);
    }

    [Fact]
    public void CacheEntryOptions_FromDuration_ReturnsNewInstance()
    {
        // Arrange
        var duration1 = TimeSpan.FromMinutes(5);
        var duration2 = TimeSpan.FromMinutes(10);

        // Act
        var options1 = CacheEntryOptions.FromDuration(duration1);
        var options2 = CacheEntryOptions.FromDuration(duration2);

        // Assert
        options1.Should().NotBeSameAs(options2);
        options1.AbsoluteExpirationRelativeToNow.Should().Be(duration1);
        options2.AbsoluteExpirationRelativeToNow.Should().Be(duration2);
    }

    #endregion
}
