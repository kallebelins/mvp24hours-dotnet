using System.ComponentModel.DataAnnotations;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class EnumExtensionsTest
{
    private enum SampleStatus
    {
        [Display(Description = "Pending Status", GroupName = "Waiting", Name = "Is Pending")]
        Pending,

        [Display(Description = "Approved Status", GroupName = "Waiting")]
        Approved,

        NoAttribute
    }

    #region [ GetEnumDescription ]

    [Fact]
    public void GetEnumDescription_WithDisplayAttribute_ReturnsDescription()
    {
        // Act
        string result = EnumExtensions.GetEnumDescription<SampleStatus>("Pending");

        // Assert
        result.Should().Be("Pending Status");
    }

    [Fact]
    public void GetEnumDescription_WithoutDisplayAttribute_ReturnsName()
    {
        // Act
        string result = EnumExtensions.GetEnumDescription<SampleStatus>("NoAttribute");

        // Assert
        result.Should().Be("NoAttribute");
    }

    [Fact]
    public void GetEnumDescription_CaseInsensitiveMatch_ReturnsDescription()
    {
        // Act
        string result = EnumExtensions.GetEnumDescription<SampleStatus>("pending");

        // Assert
        result.Should().Be("Pending Status");
    }

    [Fact]
    public void GetEnumDescription_NonexistentValue_ReturnsEmpty()
    {
        // Act
        string result = EnumExtensions.GetEnumDescription<SampleStatus>("DoesNotExist");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region [ GetEnumValue ]

    [Fact]
    public void GetEnumValue_WithValidName_ReturnsUnderlyingValue()
    {
        // Act
        string result = EnumExtensions.GetEnumValue<SampleStatus>("Approved");

        // Assert
        result.Should().Be(((int)SampleStatus.Approved).ToString());
    }

    [Fact]
    public void GetEnumValue_NonexistentValue_ReturnsEmpty()
    {
        // Act
        string result = EnumExtensions.GetEnumValue<SampleStatus>("DoesNotExist");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region [ GetGroupName ]

    [Fact]
    public void GetGroupName_WithDisplayAttribute_ReturnsGroupName()
    {
        // Act
        string result = SampleStatus.Pending.GetGroupName();

        // Assert
        result.Should().Be("Waiting");
    }

    [Fact]
    public void GetGroupName_WithoutDisplayAttribute_ReturnsEnumName()
    {
        // Act
        string result = SampleStatus.NoAttribute.GetGroupName();

        // Assert
        result.Should().Be("NoAttribute");
    }

    #endregion

    #region [ GetDisplayName ]

    [Fact]
    public void GetDisplayName_WithDisplayName_ReturnsName()
    {
        // Act
        string result = SampleStatus.Pending.GetDisplayName();

        // Assert
        result.Should().Be("Is Pending");
    }

    [Fact]
    public void GetDisplayName_WithoutDisplayAttribute_ReturnsEnumName()
    {
        // Act
        string result = SampleStatus.NoAttribute.GetDisplayName();

        // Assert
        result.Should().Be("NoAttribute");
    }

    [Fact]
    public void GetDisplayName_WithDisplayAttributeButNoName_ReturnsEnumName()
    {
        // Act
        string result = SampleStatus.Approved.GetDisplayName();

        // Assert
        result.Should().Be("Approved");
    }

    #endregion
}
