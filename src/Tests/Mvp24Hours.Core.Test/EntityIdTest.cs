//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.ValueObjects;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Strongly-Typed Entity IDs.
/// </summary>
public class EntityIdTest
{
    #region Test ID Types

    // Define test ID types
    public sealed class TestGuidId : GuidEntityId<TestGuidId>
    {
        public TestGuidId(Guid value) : base(value) { }
        public static TestGuidId New() => new(Guid.NewGuid());
        public static TestGuidId Empty => new(Guid.Empty);
    }

    public sealed class AnotherGuidId : GuidEntityId<AnotherGuidId>
    {
        public AnotherGuidId(Guid value) : base(value) { }
        public static AnotherGuidId New() => new(Guid.NewGuid());
    }

    public sealed class TestIntId : IntEntityId<TestIntId>
    {
        public TestIntId(int value) : base(value) { }
    }

    public sealed class TestLongId : LongEntityId<TestLongId>
    {
        public TestLongId(long value) : base(value) { }
    }

    public sealed class TestStringId : StringEntityId<TestStringId>
    {
        public TestStringId(string value) : base(value) { }
    }

    #endregion

    #region GuidEntityId Tests

    [Fact]
    public void GuidEntityId_Creation_StoresValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = new TestGuidId(guid);

        // Assert
        id.Value.Should().Be(guid);
    }

    [Fact]
    public void GuidEntityId_New_CreatesNewGuid()
    {
        // Act
        var id = TestGuidId.New();

        // Assert
        id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GuidEntityId_IsEmpty_ReturnsTrueForEmptyGuid()
    {
        // Act
        var id = TestGuidId.Empty;

        // Assert
        id.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GuidEntityId_IsEmpty_ReturnsFalseForNonEmptyGuid()
    {
        // Act
        var id = TestGuidId.New();

        // Assert
        id.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void GuidEntityId_Equality_SameValues_AreEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new TestGuidId(guid);
        var id2 = new TestGuidId(guid);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.Equals(id2).Should().BeTrue();
    }

    [Fact]
    public void GuidEntityId_Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var id1 = TestGuidId.New();
        var id2 = TestGuidId.New();

        // Assert
        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GuidEntityId_Comparison_WorksCorrectly()
    {
        // Arrange
        var guid1 = new Guid("00000000-0000-0000-0000-000000000001");
        var guid2 = new Guid("00000000-0000-0000-0000-000000000002");
        var id1 = new TestGuidId(guid1);
        var id2 = new TestGuidId(guid2);

        // Act
        var comparison = id1.CompareTo(id2);

        // Assert
        comparison.Should().BeLessThan(0);
    }

    [Fact]
    public void GuidEntityId_ImplicitConversion_ReturnsGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);

        // Act
        Guid result = id;

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void GuidEntityId_ToString_ReturnsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);

        // Assert
        id.ToString().Should().Be(guid.ToString());
    }

    #endregion

    #region IntEntityId Tests

    [Fact]
    public void IntEntityId_Creation_StoresValue()
    {
        // Arrange
        var value = 42;

        // Act
        var id = new TestIntId(value);

        // Assert
        id.Value.Should().Be(value);
    }

    [Fact]
    public void IntEntityId_IsDefault_ReturnsTrueForZero()
    {
        // Act
        var id = new TestIntId(0);

        // Assert
        id.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void IntEntityId_IsDefault_ReturnsFalseForNonZero()
    {
        // Act
        var id = new TestIntId(1);

        // Assert
        id.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void IntEntityId_Equality_SameValues_AreEqual()
    {
        // Arrange
        var id1 = new TestIntId(42);
        var id2 = new TestIntId(42);

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void IntEntityId_Comparison_WorksCorrectly()
    {
        // Arrange
        var id1 = new TestIntId(10);
        var id2 = new TestIntId(20);

        // Act
        var comparison = id1.CompareTo(id2);

        // Assert
        comparison.Should().BeLessThan(0);
    }

    [Fact]
    public void IntEntityId_ImplicitConversion_ReturnsInt()
    {
        // Arrange
        var id = new TestIntId(42);

        // Act
        int result = id;

        // Assert
        result.Should().Be(42);
    }

    #endregion

    #region LongEntityId Tests

    [Fact]
    public void LongEntityId_Creation_StoresValue()
    {
        // Arrange
        var value = 9999999999L;

        // Act
        var id = new TestLongId(value);

        // Assert
        id.Value.Should().Be(value);
    }

    [Fact]
    public void LongEntityId_IsDefault_ReturnsTrueForZero()
    {
        // Act
        var id = new TestLongId(0);

        // Assert
        id.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void LongEntityId_IsDefault_ReturnsFalseForNonZero()
    {
        // Act
        var id = new TestLongId(1);

        // Assert
        id.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void LongEntityId_Equality_SameValues_AreEqual()
    {
        // Arrange
        var id1 = new TestLongId(9999999999L);
        var id2 = new TestLongId(9999999999L);

        // Assert
        id1.Should().Be(id2);
    }

    [Fact]
    public void LongEntityId_ImplicitConversion_ReturnsLong()
    {
        // Arrange
        var id = new TestLongId(9999999999L);

        // Act
        long result = id;

        // Assert
        result.Should().Be(9999999999L);
    }

    #endregion

    #region StringEntityId Tests

    [Fact]
    public void StringEntityId_Creation_StoresValue()
    {
        // Arrange
        var value = "ABC-123";

        // Act
        var id = new TestStringId(value);

        // Assert
        id.Value.Should().Be(value);
    }

    [Fact]
    public void StringEntityId_IsEmpty_ReturnsTrueForEmpty()
    {
        // Act
        var id = new TestStringId(string.Empty);

        // Assert
        id.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void StringEntityId_IsEmpty_ReturnsFalseForNonEmpty()
    {
        // Act
        var id = new TestStringId("test");

        // Assert
        id.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void StringEntityId_Creation_WithNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new TestStringId(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StringEntityId_Equality_SameValues_AreEqual()
    {
        // Arrange
        var id1 = new TestStringId("ABC-123");
        var id2 = new TestStringId("ABC-123");

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void StringEntityId_ImplicitConversion_ReturnsString()
    {
        // Arrange
        var id = new TestStringId("ABC-123");

        // Act
        string result = id;

        // Assert
        result.Should().Be("ABC-123");
    }

    #endregion

    #region Type Safety Tests

    [Fact]
    public void EntityId_DifferentTypes_HaveSameUnderlyingValue_ButDifferentReferences()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var testId = new TestGuidId(guid);
        var anotherId = new AnotherGuidId(guid);

        // Assert - They share the same underlying GUID value but are different types
        // This demonstrates compile-time type safety - you can't accidentally mix ID types
        testId.Value.Should().Be(anotherId.Value);
        
        // They are different object instances
        ReferenceEquals(testId, anotherId).Should().BeFalse();
        
        // The types are different at compile time, preventing accidental mixing
        testId.GetType().Should().NotBe(anotherId.GetType());
    }

    [Fact]
    public void EntityId_GetHashCode_SameForSameValues()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = new TestGuidId(guid);
        var id2 = new TestGuidId(guid);

        // Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void EntityId_GetHashCode_DifferentForDifferentValues()
    {
        // Arrange
        var id1 = TestGuidId.New();
        var id2 = TestGuidId.New();

        // Assert
        id1.GetHashCode().Should().NotBe(id2.GetHashCode());
    }

    [Fact]
    public void EntityId_CanBeUsedAsKey_InDictionary()
    {
        // Arrange
        var id1 = TestGuidId.New();
        var id2 = TestGuidId.New();
        var dictionary = new Dictionary<TestGuidId, string>
        {
            { id1, "Value1" },
            { id2, "Value2" }
        };

        // Assert
        dictionary[id1].Should().Be("Value1");
        dictionary[id2].Should().Be("Value2");
    }

    [Fact]
    public void EntityId_Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var id = TestGuidId.New();

        // Assert
        id.Equals(null).Should().BeFalse();
    }

    #endregion
}

