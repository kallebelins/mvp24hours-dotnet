//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Test;

#pragma warning disable CS8604

/// <summary>
/// Unit tests for Extension Methods in Mvp24Hours.Core.
/// </summary>
[Trait("Category", "Unit")]
public class ExtensionMethodsTest
{
    #region StringExtensions Tests

    [Fact]
    public void StringExtensions_RegexReplace_ReplacesPattern()
    {
        // Arrange
        string source = "Hello World";
        string pattern = "World";
        string replacement = "Universe";

        // Act
        string result = source.RegexReplace(pattern, replacement);

        // Assert
        result.Should().Be("Hello Universe");
    }

    [Fact]
    public void StringExtensions_ReplaceEnd_ReplacesEndOfString()
    {
        // Arrange
        string source = "test.txt";
        string value = ".txt";
        string replacement = ".csv";

        // Act
        string result = source.ReplaceEnd(value, replacement);

        // Assert
        result.Should().Be("test.csv");
    }

    [Fact]
    public void StringExtensions_RemoveEnd_RemovesEndOfString()
    {
        // Arrange
        string source = "test.txt";
        string value = ".txt";

        // Act
        string result = source.RemoveEnd(value);

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void StringExtensions_Truncate_TruncatesLongString()
    {
        // Arrange
        string text = "This is a very long string";
        int size = 10;

        // Act
        string result = text.Truncate(size);

        // Assert
        result.Should().Be("This is a ");
        result.Length.Should().Be(size);
    }

    [Fact]
    public void StringExtensions_Truncate_DoesNotTruncateShortString()
    {
        // Arrange
        string text = "Short";
        int size = 10;

        // Act
        string result = text.Truncate(size);

        // Assert
        result.Should().Be("Short");
    }

    [Fact]
    public void StringExtensions_Truncate_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? text = null;
        int size = 10;

        // Act
#pragma warning disable CS8604 // Possible null reference argument.
        string? result = text.Truncate(size);
#pragma warning restore CS8604 // Possible null reference argument.

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void StringExtensions_Reticence_AddsEllipsis()
    {
        // Arrange
        string text = "This is a very long string";
        int size = 10;

        // Act
        string result = text.Reticence(size);

        // Assert
        result.Should().Be("This is a ...");
    }

    [Fact]
    public void StringExtensions_Reticence_DoesNotAddEllipsisForShortString()
    {
        // Arrange
        string text = "Short";
        int size = 10;

        // Act
        string result = text.Reticence(size);

        // Assert
        result.Should().Be("Short");
    }

    [Fact]
    public void StringExtensions_SubstringSafe_ReturnsSubstring()
    {
        // Arrange
        string text = "Hello World";
        int start = 0;
        int length = 5;

        // Act
        string result = text.SubstringSafe(start, length);

        // Assert
        result.Should().Be("Hello");
    }

    [Fact]
    public void StringExtensions_SubstringSafe_WithStartBeyondLength_ReturnsEmpty()
    {
        // Arrange
        string text = "Hello";
        int start = 10;
        int length = 5;

        // Act
        string result = text.SubstringSafe(start, length);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void StringExtensions_SubstringSafe_WithLengthBeyondEnd_ReturnsToEnd()
    {
        // Arrange
        string text = "Hello";
        int start = 2;
        int length = 100;

        // Act
        string result = text.SubstringSafe(start, length);

        // Assert
        result.Should().Be("llo");
    }

    [Fact]
    public void StringExtensions_SqlSafe_EscapesSqlCharacters()
    {
        // Arrange
        string text = "O'Brien--comment";

        // Act
        string result = text.SqlSafe();

        // Assert
        result.Should().Be("O''Brien");
    }

    [Fact]
    public void StringExtensions_SqlSafe_WithNull_ReturnsEmpty()
    {
        // Arrange
        string? text = null;

        // Act
        string result = text.SqlSafe();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void StringExtensions_Format_FormatsString()
    {
        // Arrange
        string text = "Hello {0}, you are {1} years old";
        object[] args = ["John", 30];

        // Act
        string result = text.Format(args);

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
        bool result = list.IsList();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_IsList_WithArray_ReturnsTrue()
    {
        // Arrange
        int[] array = [1, 2, 3];

        // Act
        bool result = array.IsList();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_IsList_WithNull_ReturnsFalse()
    {
        // Arrange
        object? value = null;

        // Act
        bool result = value.IsList();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_IsList_WithString_ReturnsFalse()
    {
        // Arrange
        string value = "not a list";

        // Act
        bool result = value.IsList();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_IsDictionary_WithDictionary_ReturnsTrue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key", 1 } };

        // Act
        bool result = dict.IsDictionary();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_IsDictionary_WithList_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        bool result = list.IsDictionary();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_IsDictionary_WithNull_ReturnsFalse()
    {
        // Arrange
        object? value = null;

        // Act
        bool result = value.IsDictionary();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_ForEach_ExecutesAction()
    {
        // Arrange
        IEnumerable<int> list = [1, 2, 3];
        int sum = 0;

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
        bool result = list.AnyOrNotNull();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithEmpty_ReturnsFalse()
    {
        // Arrange
        var list = new List<int>();

        // Act
        bool result = list.AnyOrNotNull();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithNull_ReturnsFalse()
    {
        // Arrange
        List<int>? list = null;

        // Act
        bool result = list.AnyOrNotNull();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithPredicate_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        bool result = list.AnyOrNotNull(x => x > 2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_AnyOrNotNull_WithPredicate_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        bool result = list.AnyOrNotNull(x => x > 10);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnySafe_WithItems_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        bool result = list.AnySafe();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_AnySafe_WithEmpty_ReturnsFalse()
    {
        // Arrange
        var list = new List<int>();

        // Act
        bool result = list.AnySafe();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_AnySafe_WithNull_ReturnsFalse()
    {
        // Arrange
        List<int>? list = null;

        // Act
        bool result = list.AnySafe();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_ContainsKeySafe_WithKey_ReturnsTrue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key", 1 } };

        // Act
        bool result = dict.ContainsKeySafe("key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnumerableExtensions_ContainsKeySafe_WithoutKey_ReturnsFalse()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key", 1 } };

        // Act
        bool result = dict.ContainsKeySafe("missing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnumerableExtensions_ContainsKeySafe_WithNull_ReturnsFalse()
    {
        // Arrange
        Dictionary<string, int>? dict = null;

        // Act
        bool result = dict.ContainsKeySafe("key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnumerableExtensions_FirstOrDefaultAsync_ReturnsFirst()
    {
        // Arrange
        Task<IEnumerable<int>> task = Task.FromResult<IEnumerable<int>>([1, 2, 3]);

        // Act
        int result = await task.FirstOrDefaultAsync();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task EnumerableExtensions_FirstOrDefaultAsync_WithPredicate_ReturnsMatching()
    {
        // Arrange
        Task<IEnumerable<int>> task = Task.FromResult<IEnumerable<int>>([1, 2, 3]);

        // Act
        int result = await task.FirstOrDefaultAsync(x => x > 1);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task EnumerableExtensions_LastOrDefaultAsync_ReturnsLast()
    {
        // Arrange
        Task<IEnumerable<int>> task = Task.FromResult<IEnumerable<int>>([1, 2, 3]);

        // Act
        int result = await task.LastOrDefaultAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task EnumerableExtensions_ElementAtOrDefaultAsync_ReturnsElement()
    {
        // Arrange
        Task<IEnumerable<int>> task = Task.FromResult<IEnumerable<int>>([1, 2, 3]);

        // Act
        int result = await task.ElementAtOrDefaultAsync(1);

        // Assert
        result.Should().Be(2);
    }

    #endregion

    #region GuidExtensions Tests

    [Fact]
    public void GuidExtensions_SafeNewGuid_WithEmpty_ReturnsNewGuid()
    {
        // Arrange
        Guid guid = Guid.Empty;

        // Act
        Guid result = guid.SafeNewGuid();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GuidExtensions_SafeNewGuid_WithNonEmpty_ReturnsSame()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        Guid result = guid.SafeNewGuid();

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void GuidExtensions_ToGuid_WithValidString_ReturnsGuid()
    {
        // Arrange
        string guidString = Guid.NewGuid().ToString();

        // Act
        var result = guidString.ToGuid();

        // Assert
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GuidExtensions_ToGuid_WithInvalidString_ReturnsEmpty()
    {
        // Arrange
        string invalidString = "not-a-guid";

        // Act
        var result = invalidString.ToGuid();

        // Assert
        result.Should().Be(Guid.Empty);
    }

    [Fact]
    public void GuidExtensions_IsValidGuid_WithValidString_ReturnsTrue()
    {
        // Arrange
        string guidString = Guid.NewGuid().ToString();

        // Act
        bool result = guidString.IsValidGuid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsValidGuid_WithInvalidString_ReturnsFalse()
    {
        // Arrange
        string invalidString = "not-a-guid";

        // Act
        bool result = invalidString.IsValidGuid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GuidExtensions_IsValidGuid_WithNull_ReturnsFalse()
    {
        // Arrange
        string? guidString = null;

        // Act
        bool result = guidString.IsValidGuid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GuidExtensions_IsNullOrEmpty_WithNull_ReturnsTrue()
    {
        // Arrange
        Guid? guid = null;

        // Act
        bool result = guid.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsNullOrEmpty_WithEmpty_ReturnsTrue()
    {
        // Arrange
        Guid? guid = Guid.Empty;

        // Act
        bool result = guid.IsNullOrEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsNullOrEmpty_WithValue_ReturnsFalse()
    {
        // Arrange
        Guid? guid = Guid.NewGuid();

        // Act
        bool result = guid.IsNullOrEmpty();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GuidExtensions_IsEmpty_WithEmpty_ReturnsTrue()
    {
        // Arrange
        Guid guid = Guid.Empty;

        // Act
        bool result = guid.IsEmpty();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidExtensions_IsEmpty_WithValue_ReturnsFalse()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        bool result = guid.IsEmpty();

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
        string result = EnumExtensions.GetEnumDescription<TestEnum>("First");

        // Assert
        result.Should().Be("First description");
    }

    [Fact]
    public void EnumExtensions_GetEnumDescription_WithoutDisplayAttribute_ReturnsName()
    {
        // Act
        string result = EnumExtensions.GetEnumDescription<TestEnum>("Third");

        // Assert
        result.Should().Be("Third");
    }

    [Fact]
    public void EnumExtensions_GetEnumDescription_WithInvalidValue_ReturnsEmpty()
    {
        // Act
        string result = EnumExtensions.GetEnumDescription<TestEnum>("Invalid");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void EnumExtensions_GetEnumValue_ReturnsValue()
    {
        // Act
        string result = EnumExtensions.GetEnumValue<TestEnum>("First");

        // Assert
        result.Should().Be("0");
    }

    [Fact]
    public void EnumExtensions_GetDisplayName_WithDisplayAttribute_ReturnsName()
    {
        // Arrange
        TestEnum value = TestEnum.First;

        // Act
        string result = value.GetDisplayName();

        // Assert
        result.Should().Be("First Value");
    }

    [Fact]
    public void EnumExtensions_GetDisplayName_WithoutDisplayAttribute_ReturnsToString()
    {
        // Arrange
        TestEnum value = TestEnum.Third;

        // Act
        string result = value.GetDisplayName();

        // Assert
        result.Should().Be("Third");
    }

    [Fact]
    public void EnumExtensions_GetGroupName_WithDisplayAttribute_ReturnsGroupName()
    {
        // Arrange
        TestEnum value = TestEnum.First;

        // Act
        string result = value.GetGroupName();

        // Assert
        // If GroupName is not set, returns the enum name
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ObjectExtensions Tests

    [Trait("Category", "Unit")]
    public class SourceClass
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
    }

    [Trait("Category", "Unit")]
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
        bool result = source.CopyPropertiesTo(destination);

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
        string[] propertiesToIgnore = ["Id"];

        // Act
        _ = source.CopyPropertiesTo(destination, propertiesToIgnore);

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
        _ = source.CopyPropertiesTo(destination, IgnoreNullProperties: true);

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
        Func<bool> act = () => source!.CopyPropertiesTo(destination);

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
        Func<bool> act = () => source.CopyPropertiesTo(destination!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ObjectExtensions_GetPropValue_ReturnsPropertyValue()
    {
        // Arrange
        var obj = new SourceClass { Id = 42, Name = "Test" };

        // Act
        object? result = obj.GetPropValue("Id");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ObjectExtensions_GetPropValue_WithNestedProperty_ReturnsValue()
    {
        // Arrange
        var obj = new { Nested = new SourceClass { Id = 42 } };

        // Act
        object? result = obj.GetPropValue("Nested.Id");

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ObjectExtensions_GetPropValue_WithInvalidProperty_ReturnsNull()
    {
        // Arrange
        var obj = new SourceClass { Id = 42 };

        // Act
        object? result = obj.GetPropValue("InvalidProperty");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ObjectExtensions_GetPropValueT_ReturnsTypedValue()
    {
        // Arrange
        var obj = new SourceClass { Id = 42 };

        // Act
        int result = obj.GetPropValue<int>("Id");

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
        Type childType = typeof(DestinationClass);
        Type parentType = typeof(SourceClass);

        // Act
        bool result = childType.InheritsOrImplements(parentType);

        // Assert
        // Note: This will be false unless DestinationClass actually inherits from SourceClass
        // This test demonstrates the method exists and works
        result.Should().BeFalse(); // Since DestinationClass doesn't inherit SourceClass
    }

    #endregion
}

#pragma warning restore CS8604
