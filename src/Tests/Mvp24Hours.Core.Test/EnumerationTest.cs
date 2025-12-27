//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Domain.Enumerations;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Smart Enumerations (Enumeration Pattern).
/// </summary>
public class EnumerationTest
{
    #region Test Enumeration

    /// <summary>
    /// Test enumeration for order status.
    /// </summary>
    public class OrderStatus : Enumeration<OrderStatus>
    {
        public static readonly OrderStatus Pending = new(1, nameof(Pending));
        public static readonly OrderStatus Processing = new(2, nameof(Processing));
        public static readonly OrderStatus Shipped = new(3, nameof(Shipped));
        public static readonly OrderStatus Delivered = new(4, nameof(Delivered));
        public static readonly OrderStatus Cancelled = new(5, nameof(Cancelled));

        private OrderStatus(int value, string name) : base(value, name) { }

        public bool CanCancel => this == Pending || this == Processing;
    }

    #endregion

    #region FromValue Tests

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void FromValue_WithValidValue_ReturnsEnumeration(int value)
    {
        // Act
        var status = OrderStatus.FromValue(value);

        // Assert
        status.Should().NotBeNull();
        status.Value.Should().Be(value);
    }

    [Fact]
    public void FromValue_WithInvalidValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidValue = 999;

        // Act
        var act = () => OrderStatus.FromValue(invalidValue);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{invalidValue}*is not a valid value*");
    }

    [Fact]
    public void FromValue_ReturnsCorrectEnumeration()
    {
        // Act
        var pending = OrderStatus.FromValue(1);
        var shipped = OrderStatus.FromValue(3);

        // Assert
        pending.Should().Be(OrderStatus.Pending);
        shipped.Should().Be(OrderStatus.Shipped);
    }

    #endregion

    #region FromName Tests

    [Theory]
    [InlineData("Pending")]
    [InlineData("Processing")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void FromName_WithValidName_ReturnsEnumeration(string name)
    {
        // Act
        var status = OrderStatus.FromName(name);

        // Assert
        status.Should().NotBeNull();
        status.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("PENDING")]
    [InlineData("PeNdInG")]
    public void FromName_IsCaseInsensitive(string name)
    {
        // Act
        var status = OrderStatus.FromName(name);

        // Assert
        status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void FromName_WithInvalidName_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidName = "InvalidStatus";

        // Act
        var act = () => OrderStatus.FromName(invalidName);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{invalidName}*is not a valid name*");
    }

    [Fact]
    public void FromName_WithNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => OrderStatus.FromName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromName_WithWhitespace_ThrowsArgumentNullException()
    {
        // Act
        var act = () => OrderStatus.FromName("   ");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region TryFromValue Tests

    [Fact]
    public void TryFromValue_WithValidValue_ReturnsTrue()
    {
        // Act
        var result = OrderStatus.TryFromValue(1, out var status);

        // Assert
        result.Should().BeTrue();
        status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void TryFromValue_WithInvalidValue_ReturnsFalse()
    {
        // Act
        var result = OrderStatus.TryFromValue(999, out var status);

        // Assert
        result.Should().BeFalse();
        status.Should().BeNull();
    }

    #endregion

    #region TryFromName Tests

    [Fact]
    public void TryFromName_WithValidName_ReturnsTrue()
    {
        // Act
        var result = OrderStatus.TryFromName("Pending", out var status);

        // Assert
        result.Should().BeTrue();
        status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void TryFromName_WithInvalidName_ReturnsFalse()
    {
        // Act
        var result = OrderStatus.TryFromName("InvalidStatus", out var status);

        // Assert
        result.Should().BeFalse();
        status.Should().BeNull();
    }

    [Fact]
    public void TryFromName_WithNull_ReturnsFalse()
    {
        // Act
        var result = OrderStatus.TryFromName(null!, out var status);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryFromName_IsCaseInsensitive()
    {
        // Act
        var result = OrderStatus.TryFromName("pending", out var status);

        // Assert
        result.Should().BeTrue();
        status.Should().Be(OrderStatus.Pending);
    }

    #endregion

    #region GetAll Tests

    [Fact]
    public void GetAll_ReturnsAllEnumerationValues()
    {
        // Act
        var all = OrderStatus.GetAll();

        // Assert
        all.Should().HaveCount(5);
        all.Should().Contain(OrderStatus.Pending);
        all.Should().Contain(OrderStatus.Processing);
        all.Should().Contain(OrderStatus.Shipped);
        all.Should().Contain(OrderStatus.Delivered);
        all.Should().Contain(OrderStatus.Cancelled);
    }

    [Fact]
    public void GetAll_ReturnsReadOnlyCollection()
    {
        // Act
        var all = OrderStatus.GetAll();

        // Assert
        all.Should().BeAssignableTo<IReadOnlyCollection<OrderStatus>>();
    }

    #endregion

    #region IsDefined Tests

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(999, false)]
    public void IsDefined_Int_ReturnsExpectedResult(int value, bool expected)
    {
        // Act
        var result = OrderStatus.IsDefined(value);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("pending", true)]
    [InlineData("Processing", true)]
    [InlineData("Invalid", false)]
    [InlineData("", false)]
    public void IsDefined_String_ReturnsExpectedResult(string name, bool expected)
    {
        // Act
        var result = OrderStatus.IsDefined(name);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsDefined_String_WithNull_ReturnsFalse()
    {
        // Act
        var result = OrderStatus.IsDefined(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_SameEnumerations_AreEqual()
    {
        // Arrange
        var status1 = OrderStatus.Pending;
        var status2 = OrderStatus.Pending;

        // Assert
        status1.Should().Be(status2);
        (status1 == status2).Should().BeTrue();
        status1.Equals(status2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentEnumerations_AreNotEqual()
    {
        // Arrange
        var status1 = OrderStatus.Pending;
        var status2 = OrderStatus.Shipped;

        // Assert
        status1.Should().NotBe(status2);
        (status1 != status2).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithNull_ReturnsFalse()
    {
        // Arrange
        var status = OrderStatus.Pending;

        // Assert
        status.Equals(null).Should().BeFalse();
        (status == null).Should().BeFalse();
        (null == status).Should().BeFalse();
    }

    [Fact]
    public void Equality_BothNull_ReturnsTrue()
    {
        // Arrange
        OrderStatus? status1 = null;
        OrderStatus? status2 = null;

        // Assert
        (status1 == status2).Should().BeTrue();
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void Comparison_CompareTo_ReturnsCorrectOrder()
    {
        // Arrange
        var pending = OrderStatus.Pending; // Value: 1
        var shipped = OrderStatus.Shipped; // Value: 3

        // Assert
        pending.CompareTo(shipped).Should().BeLessThan(0);
        shipped.CompareTo(pending).Should().BeGreaterThan(0);
        pending.CompareTo(pending).Should().Be(0);
    }

    [Fact]
    public void Comparison_CompareTo_WithNull_ReturnsPositive()
    {
        // Arrange
        var status = OrderStatus.Pending;

        // Assert
        status.CompareTo(null).Should().BeGreaterThan(0);
    }

    [Fact]
    public void Comparison_Operators_WorkCorrectly()
    {
        // Arrange
        var pending = OrderStatus.Pending;
        var shipped = OrderStatus.Shipped;

        // Assert
        (pending < shipped).Should().BeTrue();
        (shipped > pending).Should().BeTrue();
        (pending <= shipped).Should().BeTrue();
        (shipped >= pending).Should().BeTrue();
        (pending <= pending).Should().BeTrue();
        (pending >= pending).Should().BeTrue();
    }

    #endregion

    #region Implicit Conversion Tests

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsValue()
    {
        // Arrange
        var status = OrderStatus.Processing;

        // Act
        int value = status;

        // Assert
        value.Should().Be(2);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsName()
    {
        // Arrange
        var status = OrderStatus.Processing;

        // Act
        string name = status;

        // Assert
        name.Should().Be("Processing");
    }

    [Fact]
    public void ImplicitConversion_NullToInt_ReturnsZero()
    {
        // Arrange
        OrderStatus? status = null;

        // Act
        int value = status!;

        // Assert
        value.Should().Be(0);
    }

    [Fact]
    public void ImplicitConversion_NullToString_ReturnsEmpty()
    {
        // Arrange
        OrderStatus? status = null;

        // Act
        string name = status!;

        // Assert
        name.Should().Be(string.Empty);
    }

    #endregion

    #region Behavior Tests

    [Fact]
    public void Enumeration_CanHaveCustomBehavior()
    {
        // Arrange
        var pending = OrderStatus.Pending;
        var processing = OrderStatus.Processing;
        var shipped = OrderStatus.Shipped;
        var delivered = OrderStatus.Delivered;

        // Assert - Custom CanCancel property
        pending.CanCancel.Should().BeTrue();
        processing.CanCancel.Should().BeTrue();
        shipped.CanCancel.Should().BeFalse();
        delivered.CanCancel.Should().BeFalse();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsName()
    {
        // Arrange
        var status = OrderStatus.Pending;

        // Assert
        status.ToString().Should().Be("Pending");
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameEnumerations_HaveSameHashCode()
    {
        // Arrange
        var status1 = OrderStatus.Pending;
        var status2 = OrderStatus.FromValue(1);

        // Assert
        status1.GetHashCode().Should().Be(status2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentEnumerations_HaveDifferentHashCodes()
    {
        // Arrange
        var status1 = OrderStatus.Pending;
        var status2 = OrderStatus.Shipped;

        // Assert
        status1.GetHashCode().Should().NotBe(status2.GetHashCode());
    }

    #endregion

    #region Dictionary Key Tests

    [Fact]
    public void Enumeration_CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var dictionary = new Dictionary<OrderStatus, string>
        {
            { OrderStatus.Pending, "Waiting" },
            { OrderStatus.Shipped, "On the way" }
        };

        // Assert
        dictionary[OrderStatus.Pending].Should().Be("Waiting");
        dictionary[OrderStatus.Shipped].Should().Be("On the way");
    }

    [Fact]
    public void Enumeration_LookupByFromValue_WorksWithDictionary()
    {
        // Arrange
        var dictionary = new Dictionary<OrderStatus, string>
        {
            { OrderStatus.Pending, "Waiting" }
        };

        // Act
        var key = OrderStatus.FromValue(1);
        var value = dictionary[key];

        // Assert
        value.Should().Be("Waiting");
    }

    #endregion
}

