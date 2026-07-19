//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.DependencyInjection;

namespace Mvp24Hours.Core.Test.Contract;

/// <summary>
/// Unit tests for DI service registration attributes.
/// </summary>
[Trait("Category", "Unit")]
public class ServiceAttributesTest
{
    #region ServiceKeyAttribute Tests

    [Fact]
    public void ServiceKeyAttribute_WithValidKey_StoresKey()
    {
        // Arrange & Act
        var attr = new ServiceKeyAttribute("my-service");

        // Assert
        attr.Key.Should().Be("my-service");
    }

    [Fact]
    public void ServiceKeyAttribute_WithNullKey_ThrowsArgumentNullException()
    {
        // Act
        Func<ServiceKeyAttribute> act = () => new ServiceKeyAttribute(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ServiceKeyAttribute_WithEmptyOrWhitespaceKey_ThrowsArgumentException(string key)
    {
        // Act
        Func<ServiceKeyAttribute> act = () => new ServiceKeyAttribute(key);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public void ServiceKeyAttribute_IsAttribute()
    {
        // Act
        var attr = new ServiceKeyAttribute("test");

        // Assert
        attr.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void ServiceKeyAttribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var usageAttr = typeof(ServiceKeyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usageAttr.Should().NotBeNull();
        usageAttr!.ValidOn.Should().Be(AttributeTargets.Class);
        usageAttr.AllowMultiple.Should().BeFalse();
        usageAttr.Inherited.Should().BeFalse();
    }

    [Fact]
    public void ServiceKeyAttribute_CanBeAppliedToClass()
    {
        // Arrange
        var type = typeof(ServiceWithKey);
        var attr = type.GetCustomAttributes(typeof(ServiceKeyAttribute), false)
            .Cast<ServiceKeyAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
        attr!.Key.Should().Be("test-key");
    }

    #endregion

    #region ServiceOrderAttribute Tests

    [Fact]
    public void ServiceOrderAttribute_WithOrder_StoresOrder()
    {
        // Arrange & Act
        var attr = new ServiceOrderAttribute(5);

        // Assert
        attr.Order.Should().Be(5);
    }

    [Fact]
    public void ServiceOrderAttribute_WithNegativeOrder_StoresNegativeOrder()
    {
        // Arrange & Act
        var attr = new ServiceOrderAttribute(-1);

        // Assert
        attr.Order.Should().Be(-1);
    }

    [Fact]
    public void ServiceOrderAttribute_WithZeroOrder_StoresZero()
    {
        // Arrange & Act
        var attr = new ServiceOrderAttribute(0);

        // Assert
        attr.Order.Should().Be(0);
    }

    [Fact]
    public void ServiceOrderAttribute_IsAttribute()
    {
        // Act
        var attr = new ServiceOrderAttribute(1);

        // Assert
        attr.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void ServiceOrderAttribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var usageAttr = typeof(ServiceOrderAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usageAttr.Should().NotBeNull();
        usageAttr!.ValidOn.Should().Be(AttributeTargets.Class);
        usageAttr.AllowMultiple.Should().BeFalse();
    }

    #endregion

    #region ServiceReplaceAttribute Tests

    [Fact]
    public void ServiceReplaceAttribute_CanBeCreated()
    {
        // Act
        var attr = new ServiceReplaceAttribute();

        // Assert
        attr.Should().NotBeNull();
        attr.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void ServiceReplaceAttribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var usageAttr = typeof(ServiceReplaceAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usageAttr.Should().NotBeNull();
        usageAttr!.ValidOn.Should().Be(AttributeTargets.Class);
        usageAttr.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void ServiceReplaceAttribute_CanBeAppliedToClass()
    {
        // Arrange
        var type = typeof(ServiceWithReplace);
        var attr = type.GetCustomAttributes(typeof(ServiceReplaceAttribute), false)
            .Cast<ServiceReplaceAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
    }

    #endregion

    #region ServiceTryAddAttribute Tests

    [Fact]
    public void ServiceTryAddAttribute_CanBeCreated()
    {
        // Act
        var attr = new ServiceTryAddAttribute();

        // Assert
        attr.Should().NotBeNull();
        attr.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void ServiceTryAddAttribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var usageAttr = typeof(ServiceTryAddAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usageAttr.Should().NotBeNull();
        usageAttr!.ValidOn.Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void ServiceTryAddAttribute_CanBeAppliedToClass()
    {
        // Arrange
        var type = typeof(ServiceWithTryAdd);
        var attr = type.GetCustomAttributes(typeof(ServiceTryAddAttribute), false)
            .Cast<ServiceTryAddAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
    }

    #endregion

    #region ServiceIgnoreAttribute Tests

    [Fact]
    public void ServiceIgnoreAttribute_CanBeCreated()
    {
        // Act
        var attr = new ServiceIgnoreAttribute();

        // Assert
        attr.Should().NotBeNull();
        attr.Should().BeAssignableTo<Attribute>();
    }

    [Fact]
    public void ServiceIgnoreAttribute_HasCorrectAttributeUsage()
    {
        // Arrange
        var usageAttr = typeof(ServiceIgnoreAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        usageAttr.Should().NotBeNull();
        usageAttr!.ValidOn.Should().Be(AttributeTargets.Class);
    }

    [Fact]
    public void ServiceIgnoreAttribute_CanBeAppliedToClass()
    {
        // Arrange
        var type = typeof(ServiceWithIgnore);
        var attr = type.GetCustomAttributes(typeof(ServiceIgnoreAttribute), false)
            .Cast<ServiceIgnoreAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull();
    }

    #endregion

    #region Multiple Attributes Tests

    [Fact]
    public void ServiceKeyAndOrder_CanBeCombined()
    {
        // Arrange
        var type = typeof(ServiceWithKeyAndOrder);

        // Act
        var keyAttr = type.GetCustomAttributes(typeof(ServiceKeyAttribute), false)
            .Cast<ServiceKeyAttribute>()
            .FirstOrDefault();
        var orderAttr = type.GetCustomAttributes(typeof(ServiceOrderAttribute), false)
            .Cast<ServiceOrderAttribute>()
            .FirstOrDefault();

        // Assert
        keyAttr.Should().NotBeNull();
        keyAttr!.Key.Should().Be("combined-key");
        orderAttr.Should().NotBeNull();
        orderAttr!.Order.Should().Be(1);
    }

    #endregion

    #region Test Helper Classes

    [ServiceKey("test-key")]
    private class ServiceWithKey { }

    [ServiceReplace]
    private class ServiceWithReplace { }

    [ServiceTryAdd]
    private class ServiceWithTryAdd { }

    [ServiceIgnore]
    private class ServiceWithIgnore { }

    [ServiceKey("combined-key")]
    [ServiceOrder(1)]
    private class ServiceWithKeyAndOrder { }

    #endregion
}
