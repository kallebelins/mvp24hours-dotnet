namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class GuidExtensionsTest
{
    #region [ SafeNewGuid ]

    [Fact]
    public void SafeNewGuid_WithEmptyGuid_ReturnsNewNonEmptyGuid()
    {
        // Act
        Guid result = Guid.Empty.SafeNewGuid();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SafeNewGuid_WithNonEmptyGuid_ReturnsSameGuid()
    {
        // Arrange
        Guid original = Guid.NewGuid();

        // Act
        Guid result = original.SafeNewGuid();

        // Assert
        result.Should().Be(original);
    }

    #endregion

    #region [ ToGuid ]

    [Fact]
    public void ToGuid_WithValidGuidString_ReturnsParsedGuid()
    {
        // Arrange
        Guid original = Guid.NewGuid();

        // Act
        Guid result = original.ToString().ToGuid();

        // Assert
        result.Should().Be(original);
    }

    [Fact]
    public void ToGuid_WithInvalidString_ReturnsEmpty()
    {
        // Act
        Guid result = "not-a-guid".ToGuid();

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ToGuid_WithNullString_ReturnsEmpty()
    {
        // Act
        Guid result = ((string)null!).ToGuid();

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ToGuid_WithEmptyString_ReturnsEmpty()
    {
        // Act
        Guid result = string.Empty.ToGuid();

        // Assert
        result.Should().Be(Guid.Empty);
    }

    #endregion

    #region [ IsValidGuid ]

    [Fact]
    public void IsValidGuid_WithValidGuidString_ReturnsTrue()
    {
        // Act
        bool result = Guid.NewGuid().ToString().IsValidGuid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidGuid_WithInvalidString_ReturnsFalse()
    {
        // Act
        bool result = "not-a-guid".IsValidGuid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidGuid_WithNull_ReturnsFalse()
    {
        // Act
        bool result = ((string?)null).IsValidGuid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidGuid_WithEmpty_ReturnsFalse()
    {
        // Act
        bool result = string.Empty.IsValidGuid();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region [ IsNullOrEmpty ]

    [Fact]
    public void IsNullOrEmpty_WithNull_ReturnsTrue()
    {
        // Arrange
        Guid? value = null;

        // Act
        bool result = value.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithEmptyGuid_ReturnsTrue()
    {
        // Arrange
        Guid? value = Guid.Empty;

        // Act
        bool result = value.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNullOrEmpty_WithNonEmptyGuid_ReturnsFalse()
    {
        // Arrange
        Guid? value = Guid.NewGuid();

        // Act
        bool result = value.IsNullOrEmpty();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region [ IsEmpty ]

    [Fact]
    public void IsEmpty_WithEmptyGuid_ReturnsTrue()
    {
        // Act
        bool result = Guid.Empty.IsEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_WithNonEmptyGuid_ReturnsFalse()
    {
        // Act
        bool result = Guid.NewGuid().IsEmpty();

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
