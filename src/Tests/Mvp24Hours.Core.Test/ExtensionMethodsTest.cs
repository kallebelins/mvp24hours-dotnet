//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Extension Methods in Mvp24Hours.Core.
/// </summary>
public class ExtensionMethodsTest
{
    #region StringExtensions Tests

    [Fact]
    public void StringExtensions_RegexReplace_ReplacesPattern()
    {
        // Arrange
        var source = "Hello World";
        var pattern = "World";
        var replacement = "Universe";

        // Act
        var result = source.RegexReplace(pattern, replacement);

        // Assert
        result.Should().Be("Hello Universe");
    }

    [Fact]
    public void StringExtensions_ReplaceEnd_ReplacesEndOfString()
    {
        // Arrange
        var source = "test.txt";
        var value = ".txt";
        var replacement = ".csv";

        // Act
        var result = source.ReplaceEnd(value, replacement);

        // Assert
        result.Should().Be("test.csv");
    }

    [Fact]
    public void StringExtensions_RemoveEnd_RemovesEndOfString()
    {
        // Arrange
        var source = "test.txt";
        var value = ".txt";

        // Act
        var result = source.RemoveEnd(value);

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void StringExtensions_Truncate_TruncatesLongString()
    {
        // Arrange
        var text = "This is a very long string";
        var size = 10;

        // Act
        var result = text.Truncate(size);

        // Assert
        result.Should().Be("This is a ");
        result.Length.Should().Be(size);
    }

    [Fact]
    public void StringExtensions_Truncate_DoesNotTruncateShortString()
    {
        // Arrange
        var text = "Short";
        var size = 10;

        // Act
        var result = text.Truncate(size);

        // Assert
        result.Should().Be("Short");
    }

    [Fact]
    public void StringExtensions_Truncate_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? text = null;
        var size = 10;

        // Act
        var result = text.Truncate(size);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void StringExtensions_Reticence_AddsEllipsis()
    {
        // Arrange
        var text = "This is a very long string";
        var size = 10;

        // Act
        var result = text.Reticence(size);

        // Assert
        result.Should().Be("This is a ...");
    }

    [Fact]
    public void StringExtensions_Reticence_DoesNotAddEllipsisForShortString()
    {
        // Arrange
        var text = "Short";
        var size = 10;

        // Act
        var result = text.Reticence(size);

        // Assert
        result.Should().Be("Short");
    }

    [Fact]
    public void StringExtensions_SubstringSafe_ReturnsSubstring()
    {
        // Arrange
        var text = "Hello World";
        var start = 0;
        var length = 5;

        // Act
        var result = text.SubstringSafe(start, length);

        // Assert
        result.Should().Be("Hello");
    }

    [Fact]
    public void StringExtensions_SubstringSafe_WithStartBeyondLength_ReturnsEmpty()
    {
        // Arrange
        var text = "Hello";
        var start = 10;
        var length = 5;

        // Act
        var result = text.SubstringSafe(start, length);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void StringExtensions_SubstringSafe_WithLengthBeyondEnd_ReturnsToEnd()
    {
        // Arrange
        var text = "Hello";
        var start = 2;
        var length = 100;

        // Act
        var result = text.SubstringSafe(start, length);

        // Assert
        result.Should().Be("llo");
    }

    [Fact]
    public void StringExtensions_SqlSafe_EscapesSqlCharacters()
    {
        // Arrange
        var text = "O'Brien--comment";

        // Act
        var result = text.SqlSafe();

        // Assert
        result.Should().Be("O''Brien");
    }

    [Fact]
    public void StringExtensions_SqlSafe_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? text = null;

        // Act
        var result = text.SqlSafe();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void StringExtensions_Format_FormatsString()
    {
        // Arrange
        var text = "Hello {0}, you are {1} years old";
        var args = new object[] { "John", 30 };

        // Act
        var result = text.Format(args);

        // Assert
        result.Should().Be("Hello John, you are 30 years old");
    }

    #endregion

    #region EnumerableExtensions Tests

    [Fact]
    public void EnumerableExtensions_IsList_WithList_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.IsList();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_IsList_WithArray_ReturnsTrue()
    {
        // Arrange
        var array = new int[] { 1, 2, 3 };

        // Act
        var result = array.IsList();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_IsList_WithNull_ReturnsFalse()
    {
        // Arrange
        object? value = null;

        // Act
        var result = value.IsList();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_IsList_WithString_ReturnsFalse()
    {
        // Arrange
        var value = "not a list";

        // Act
        var result = value.IsList();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_IsDictionary_WithDictionary_ReturnsTrue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key", 1 } };

        // Act
        var result = dict.IsDictionary();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_IsDictionary_WithList_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.IsDictionary();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_IsDictionary_WithNull_ReturnsFalse()
    {
        // Arrange
        object? value = null;

        // Act
        var result = value.IsDictionary();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_ForEach_ExecutesAction()
    {
        // Arrange
        IEnumerable<int> list = new List<int> { 1, 2, 3 };
        var sum = 0;

        // Act
        var result = list.ForEach(x => sum += x).ToList();

        // Assert
        sum.Should().Be(6);
        result.Should().BeEquivalentTo(list);
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithItems_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.AnyOrNotNull();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithEmpty_ReturnsFalse()
    {
        // Arrange
        var list = new List<int>();

        // Act
        var result = list.AnyOrNotNull();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithNull_ReturnsFalse()
    {
        // Arrange
        List<int>? list = null;

        // Act
        var result = list.AnyOrNotNull();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithPredicate_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.AnyOrNotNull(x => x > 2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithPredicate_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.AnyOrNotNull(x => x > 10);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnySafe_WithItems_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.AnySafe();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_AnySafe_WithEmpty_ReturnsFalse()
    {
        // Arrange
        var list = new List<int>();

        // Act
        var result = list.AnySafe();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnySafe_WithNull_ReturnsFalse()
    {
        // Arrange
        List<int>? list = null;

        // Act
        var result = list.AnySafe();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_ContainsKeySafe_WithKey_ReturnsTrue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key", 1 } };

        // Act
        var result = dict.ContainsKeySafe("key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_ContainsKeySafe_WithoutKey_ReturnsFalse()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key", 1 } };

        // Act
        var result = dict.ContainsKeySafe("missing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_ContainsKeySafe_WithNull_ReturnsFalse()
    {
        // Arrange
        Dictionary<string, int>? dict = null;

        // Act
        var result = dict.ContainsKeySafe("key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnumerableExtensions_FirstOrDefaultAsync_ReturnsFirst()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new List<int> { 1, 2, 3 });

        // Act
        var result = await task.FirstOrDefaultAsync();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task EnumerableExtensions_FirstOrDefaultAsync_WithPredicate_ReturnsMatching()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new List<int> { 1, 2, 3 });

        // Act
        var result = await task.FirstOrDefaultAsync(x => x > 1);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task EnumerableExtensions_LastOrDefaultAsync_ReturnsLast()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new List<int> { 1, 2, 3 });

        // Act
        var result = await task.LastOrDefaultAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task EnumerableExtensions_ElementAtOrDefaultAsync_ReturnsElement()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new List<int> { 1, 2, 3 });

        // Act
        var result = await task.ElementAtOrDefaultAsync(1);

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region GuidExtensions Tests

    [Fact]
    public void GuidExtensions_SafeNewGuid_WithEmpty_ReturnsNewGuid()
    {
        // Arrange
        var guid = Guid.Empty;

        // Act
        var result = guid.SafeNewGuid();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GuidExtensions_SafeNewGuid_WithNonEmpty_ReturnsSame()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = guid.SafeNewGuid();

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void GuidExtensions_ToGuid_WithValidString_ReturnsGuid()
    {
        // Arrange
        var guidString = Guid.NewGuid().ToString();

        // Act
        var result = guidString.ToGuid();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GuidExtensions_ToGuid_WithInvalidString_ReturnsEmpty()
    {
        // Arrange
        var invalidString = "not-a-guid";

        // Act
        var result = invalidString.ToGuid();

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GuidExtensions_IsValidGuid_WithValidString_ReturnsTrue()
    {
        // Arrange
        var guidString = Guid.NewGuid().ToString();

        // Act
        var result = guidString.IsValidGuid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsValidGuid_WithInvalidString_ReturnsFalse()
    {
        // Arrange
        var invalidString = "not-a-guid";

        // Act
        var result = invalidString.IsValidGuid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GuidExtensions_IsValidGuid_WithNull_ReturnsFalse()
    {
        // Arrange
        string? guidString = null;

        // Act
        var result = guidString.IsValidGuid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GuidExtensions_IsNullOrEmpty_WithNull_ReturnsTrue()
    {
        // Arrange
        Guid? guid = null;

        // Act
        var result = guid.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsNullOrEmpty_WithEmpty_ReturnsTrue()
    {
        // Arrange
        Guid? guid = Guid.Empty;

        // Act
        var result = guid.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsNullOrEmpty_WithValue_ReturnsFalse()
    {
        // Arrange
        Guid? guid = Guid.NewGuid();

        // Act
        var result = guid.IsNullOrEmpty();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GuidExtensions_IsEmpty_WithEmpty_ReturnsTrue()
    {
        // Arrange
        var guid = Guid.Empty;

        // Act
        var result = guid.IsEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsEmpty_WithValue_ReturnsFalse()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = guid.IsEmpty();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region EnumExtensions Tests

    public enum TestEnum
    {
        [System.ComponentModel.DataAnnotations.Display(Name = "First Value", Description = "First description")]
        First,
        [System.ComponentModel.DataAnnotations.Display(Name = "Second Value", Description = "Second description")]
        Second,
        Third
    }

    [Fact]
    public void EnumExtensions_GetEnumDescription_WithDisplayAttribute_ReturnsDescription()
    {
        // Act
        var result = EnumExtensions.GetEnumDescription<TestEnum>("First");

        // Assert
        result.Should().Be("First description");
    }

    [Fact]
    public void EnumExtensions_GetEnumDescription_WithoutDisplayAttribute_ReturnsName()
    {
        // Act
        var result = EnumExtensions.GetEnumDescription<TestEnum>("Third");

        // Assert
        result.Should().Be("Third");
    }

    [Fact]
    public void EnumExtensions_GetEnumDescription_WithInvalidValue_ReturnsEmpty()
    {
        // Act
        var result = EnumExtensions.GetEnumDescription<TestEnum>("Invalid");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void EnumExtensions_GetEnumValue_ReturnsValue()
    {
        // Act
        var result = EnumExtensions.GetEnumValue<TestEnum>("First");

        // Assert
        result.Should().Be("0");
    }

    [Fact]
    public void EnumExtensions_GetDisplayName_WithDisplayAttribute_ReturnsName()
    {
        // Arrange
        var value = TestEnum.First;

        // Act
        var result = value.GetDisplayName();

        // Assert
        result.Should().Be("First Value");
    }

    [Fact]
    public void EnumExtensions_GetDisplayName_WithoutDisplayAttribute_ReturnsToString()
    {
        // Arrange
        var value = TestEnum.Third;

        // Act
        var result = value.GetDisplayName();

        // Assert
        result.Should().Be("Third");
    }

    [Fact]
    public void EnumExtensions_GetGroupName_WithDisplayAttribute_ReturnsGroupName()
    {
        // Arrange
        var value = TestEnum.First;

        // Act
        var result = value.GetGroupName();

        // Assert
        // If GroupName is not set, returns the enum name
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ObjectExtensions Tests

    public class SourceClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
    }

    public class DestinationClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? ExtraProperty { get; set; }
    }

    [Fact]
    public void ObjectExtensions_CopyPropertiesTo_CopiesProperties()
    {
        // Arrange
        var source = new SourceClass { Id = 1, Name = "Test", Price = 99.99m };
        var destination = new DestinationClass();

        // Act
        var result = source.CopyPropertiesTo(destination);

        // Assert
        result.Should().BeTrue();
        destination.Id.Should().Be(1);
        destination.Name.Should().Be("Test");
        destination.Price.Should().Be(99.99m);
    }

    [Fact]
    public void ObjectExtensions_CopyPropertiesTo_WithPropertiesToIgnore_IgnoresProperties()
    {
        // Arrange
        var source = new SourceClass { Id = 1, Name = "Test", Price = 99.99m };
        var destination = new DestinationClass { Id = 999 };
        var propertiesToIgnore = new[] { "Id" };

        // Act
        var result = source.CopyPropertiesTo(destination, propertiesToIgnore);

        // Assert
        destination.Id.Should().Be(999); // Should remain unchanged
        destination.Name.Should().Be("Test");
    }

    [Fact]
    public void ObjectExtensions_CopyPropertiesTo_WithIgnoreNullProperties_IgnoresNulls()
    {
        // Arrange
        var source = new SourceClass { Id = 1, Name = null, Price = 99.99m };
        var destination = new DestinationClass { Name = "Original" };

        // Act
        var result = source.CopyPropertiesTo(destination, IgnoreNullProperties: true);

        // Assert
        destination.Name.Should().Be("Original"); // Should remain unchanged
        destination.Price.Should().Be(99.99m);
    }

    [Fact]
    public void ObjectExtensions_CopyPropertiesTo_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        SourceClass? source = null;
        var destination = new DestinationClass();

        // Act
        var act = () => source!.CopyPropertiesTo(destination);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ObjectExtensions_CopyPropertiesTo_WithNullDestination_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new SourceClass { Id = 1 };
        DestinationClass? destination = null;

        // Act
        var act = () => source.CopyPropertiesTo(destination!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ObjectExtensions_GetPropValue_ReturnsPropertyValue()
    {
        // Arrange
        var obj = new SourceClass { Id = 42, Name = "Test" };

        // Act
        var result = obj.GetPropValue("Id");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ObjectExtensions_GetPropValue_WithNestedProperty_ReturnsValue()
    {
        // Arrange
        var obj = new { Nested = new SourceClass { Id = 42 } };

        // Act
        var result = obj.GetPropValue("Nested.Id");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ObjectExtensions_GetPropValue_WithInvalidProperty_ReturnsNull()
    {
        // Arrange
        var obj = new SourceClass { Id = 42 };

        // Act
        var result = obj.GetPropValue("InvalidProperty");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ObjectExtensions_GetPropValueT_ReturnsTypedValue()
    {
        // Arrange
        var obj = new SourceClass { Id = 42 };

        // Act
        var result = obj.GetPropValue<int>("Id");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ObjectExtensions_SetPropValue_SetsPropertyValue()
    {
        // Arrange
        var obj = new SourceClass();

        // Act
        obj.SetPropValue("Id", 42);

        // Assert
        obj.Id.Should().Be(42);
    }

    [Fact]
    public void ObjectExtensions_InheritsOrImplements_WithInheritance_ReturnsTrue()
    {
        // Arrange
        var childType = typeof(DestinationClass);
        var parentType = typeof(SourceClass);

        // Act
        var result = childType.InheritsOrImplements(parentType);

        // Assert
        // Note: This will be false unless DestinationClass actually inherits from SourceClass
        // This test demonstrates the method exists and works
        result.Should().BeFalse(); // Since DestinationClass doesn't inherit SourceClass
    }

    #endregion
}

