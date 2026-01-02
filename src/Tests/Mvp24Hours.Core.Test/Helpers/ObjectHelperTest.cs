namespace Mvp24Hours.Core.Test.Helpers;

/// <summary>
/// Testes unitários para ObjectHelper (métodos existentes).
/// </summary>
public class ObjectHelperTest
{
    #region [ Clone Tests ]

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new Person { Id = 1, Name = "John", Age = 30 };

        // Act
        var clone = ObjectHelper.Clone(original);

        // Assert
        clone.Should().NotBeNull();
        clone.Should().NotBeSameAs(original);
        clone.Id.Should().Be(original.Id);
        clone.Name.Should().Be(original.Name);
        clone.Age.Should().Be(original.Age);
    }

    [Fact]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        // Arrange
        var original = new Person { Id = 1, Name = "John", Age = 30 };

        // Act
        var clone = ObjectHelper.Clone(original);
        clone.Name = "Modified";
        clone.Age = 99;

        // Assert
        original.Name.Should().Be("John");
        original.Age.Should().Be(30);
    }

    [Fact]
    public void Clone_WithComplexObject_ClonesNested()
    {
        // Arrange
        var original = new Company
        {
            Id = 1,
            Name = "Acme",
            Address = new Address { Street = "Main St", City = "NY" }
        };

        // Act
        var clone = ObjectHelper.Clone(original);

        // Assert
        clone.Should().NotBeNull();
        clone.Address.Should().NotBeSameAs(original.Address);
        clone.Address.Street.Should().Be("Main St");
    }

    [Fact]
    public void Clone_WithList_ClonesAllElements()
    {
        // Arrange
        var original = new Company
        {
            Id = 1,
            Name = "Tech",
            Employees = new List<Person>
            {
                new Person { Id = 1, Name = "John", Age = 30 },
                new Person { Id = 2, Name = "Jane", Age = 25 }
            }
        };

        // Act
        var clone = ObjectHelper.Clone(original);

        // Assert
        clone.Employees.Should().HaveCount(2);
        clone.Employees[0].Should().NotBeSameAs(original.Employees[0]);
        clone.Employees[0].Name.Should().Be("John");
    }

    [Fact]
    public void Clone_WithNull_ReturnsNull()
    {
        // Arrange
        Person? original = null;

        // Act
        var clone = ObjectHelper.Clone(original);

        // Assert
        clone.Should().BeNull();
    }

    #endregion

    #region [ ConvertToDynamic Tests ]

    [Fact]
    public void ConvertToDynamic_ConvertsToDynamicObject()
    {
        // Arrange
        var obj = new Person { Id = 1, Name = "John", Age = 30 };

        // Act
        dynamic result = ObjectHelper.ConvertToDynamic(obj);

        // Assert - ExpandoObject implementa IDictionary<string, object?>
        var dict = (IDictionary<string, object?>)result;
        dict.Should().NotBeNull();
        dict["id"].Should().Be(1);
        dict["name"].Should().Be("John");
        dict["age"].Should().Be(30);
    }

    [Fact]
    public void ConvertToDynamic_WithComplexObject_ConvertsToDynamic()
    {
        // Arrange
        var obj = new Company
        {
            Id = 1,
            Name = "Acme",
            Address = new Address { Street = "Main St", City = "NY" }
        };

        // Act
        dynamic result = ObjectHelper.ConvertToDynamic(obj);

        // Assert
        var dict = (IDictionary<string, object?>)result;
        dict.Should().NotBeNull();
        dict["id"].Should().Be(1);
        dict["name"].Should().Be("Acme");
    }

    [Fact]
    public void ConvertToDynamic_AllowsDynamicPropertyAccess()
    {
        // Arrange
        var obj = new { Id = 42, Name = "Test", Active = true };

        // Act
        dynamic result = ObjectHelper.ConvertToDynamic(obj);

        // Assert
        var dict = (IDictionary<string, object?>)result;
        dict.Should().NotBeNull();
        dict["id"].Should().Be(42);
        dict["name"].Should().Be("Test");
        dict["active"].Should().Be(true);
    }

    #endregion

    #region [ Performance Tests ]

    [Fact]
    public void Clone_WithLargeObject_ShouldPerformEfficiently()
    {
        // Arrange
        var original = new Company
        {
            Id = 1,
            Name = "Acme",
            Employees = Enumerable.Range(1, 1000)
                .Select(i => new Person { Id = i, Name = $"Person{i}", Age = i % 100 })
                .ToList()
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var clone = ObjectHelper.Clone(original);
        stopwatch.Stop();

        // Assert
        clone.Should().NotBeNull();
        clone.Employees.Should().HaveCount(1000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    #endregion

    #region [ Test Classes ]

    [Serializable]
    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    [Serializable]
    private class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Address Address { get; set; } = new();
        public List<Person> Employees { get; set; } = new();
    }

    [Serializable]
    private class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    #endregion
}
