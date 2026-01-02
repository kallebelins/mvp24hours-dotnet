namespace Mvp24Hours.Core.Test.Extensions;

/// <summary>
/// Testes unitários para EnumerableExtensions (métodos existentes).
/// </summary>
public class EnumerableExtensionsTest
{
    #region [ IsList Tests ]

    [Fact]
    public void IsList_WithList_ReturnsTrue()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.IsList();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsList_WithArray_ReturnsTrue()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = array.IsList();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsList_WithNull_ReturnsFalse()
    {
        // Arrange
        object? obj = null;

        // Act
        var result = obj.IsList();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsList_WithNonList_ReturnsFalse()
    {
        // Arrange
        var number = 42;

        // Act
        var result = number.IsList();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region [ IsDictionary Tests ]

    [Fact]
    public void IsDictionary_WithDictionary_ReturnsTrue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };

        // Act
        var result = dict.IsDictionary();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDictionary_WithList_ReturnsFalse()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Act
        var result = list.IsDictionary();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsDictionary_WithNull_ReturnsFalse()
    {
        // Arrange
        object? obj = null;

        // Act
        var result = obj.IsDictionary();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region [ ForEach Tests ]

    [Fact]
    public void ForEach_ExecutesActionForEachElement()
    {
        // Arrange
        var collection = new[] { 1, 2, 3, 4, 5 };
        var results = new List<int>();

        // Act
        collection.ForEach(x => results.Add(x * 2)).ToList();

        // Assert
        results.Should().Equal(2, 4, 6, 8, 10);
    }

    [Fact]
    public void ForEach_WithEmptyCollection_DoesNotExecuteAction()
    {
        // Arrange
        var collection = Enumerable.Empty<int>();
        var counter = 0;

        // Act
        collection.ForEach(x => counter++).ToList();

        // Assert
        counter.Should().Be(0);
    }

    [Fact]
    public void ForEach_IsLazy_OnlyExecutesWhenEnumerated()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };
        var counter = 0;

        // Act (não chama ToList())
        var query = collection.ForEach(x => counter++);

        // Assert
        counter.Should().Be(0); // Não foi executado ainda

        // Act (força enumeração)
        query.ToList();

        // Assert
        counter.Should().Be(3); // Agora foi executado
    }

    #endregion

    #region [ AnyOrNotNull Tests ]

    [Fact]
    public void AnyOrNotNull_WithNull_ReturnsFalse()
    {
        // Arrange
        IEnumerable<int>? collection = null;

        // Act
        var result = collection.AnyOrNotNull();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AnyOrNotNull_WithEmptyCollection_ReturnsFalse()
    {
        // Arrange
        var collection = Enumerable.Empty<int>();

        // Act
        var result = collection.AnyOrNotNull();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AnyOrNotNull_WithPopulatedCollection_ReturnsTrue()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act
        var result = collection.AnyOrNotNull();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AnyOrNotNull_WithPredicate_AppliesFilter()
    {
        // Arrange
        var collection = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = collection.AnyOrNotNull(x => x > 3);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AnyOrNotNull_WithPredicateNoMatch_ReturnsFalse()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act
        var result = collection.AnyOrNotNull(x => x > 10);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region [ AnySafe Tests ]

    [Fact]
    public void AnySafe_WithNull_ReturnsFalse()
    {
        // Arrange
        IEnumerable<int>? collection = null;

        // Act
        var result = collection.AnySafe();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AnySafe_WithEmptyCollection_ReturnsFalse()
    {
        // Arrange
        var collection = Enumerable.Empty<int>();

        // Act
        var result = collection.AnySafe();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AnySafe_WithPopulatedCollection_ReturnsTrue()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act
        var result = collection.AnySafe();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AnySafe_WithPredicate_AppliesFilter()
    {
        // Arrange
        var collection = new[] { 1, 2, 3 };

        // Act
        var result = collection.AnySafe(x => x > 2);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region [ ContainsKeySafe Tests ]

    [Fact]
    public void ContainsKeySafe_WithNullDictionary_ReturnsFalse()
    {
        // Arrange
        Dictionary<string, int>? dict = null;

        // Act
        var result = dict.ContainsKeySafe("key");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsKeySafe_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };

        // Act
        var result = dict.ContainsKeySafe("one");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsKeySafe_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };

        // Act
        var result = dict.ContainsKeySafe("three");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region [ Async Extension Tests ]

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstElement()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3 });

        // Act
        var result = await task.FirstOrDefaultAsync();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithPredicate_ReturnsMatch()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = await task.FirstOrDefaultAsync(x => x > 3);

        // Assert
        result.Should().Be(4);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WithEmptyCollection_ReturnsDefault()
    {
        // Arrange
        var task = Task.FromResult(Enumerable.Empty<int>());

        // Act
        var result = await task.FirstOrDefaultAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task LastOrDefaultAsync_ReturnsLastElement()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3 });

        // Act
        var result = await task.LastOrDefaultAsync();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task LastOrDefaultAsync_WithPredicate_ReturnsMatch()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = await task.LastOrDefaultAsync(x => x < 4);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task ElementAtOrDefaultAsync_ReturnsElementAtIndex()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = await task.ElementAtOrDefaultAsync(2);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task ElementAtOrDefaultAsync_WithInvalidIndex_ReturnsDefault()
    {
        // Arrange
        var task = Task.FromResult<IEnumerable<int>>(new[] { 1, 2, 3 });

        // Act
        var result = await task.ElementAtOrDefaultAsync(10);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region [ Performance Tests ]

    [Fact]
    public void EnumerableExtensions_WithLargeCollection_ShouldPerformEfficiently()
    {
        // Arrange
        var collection = Enumerable.Range(1, 1000000);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = collection.AnySafe(x => x > 500000);
        stopwatch.Stop();

        // Assert
        result.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion
}
