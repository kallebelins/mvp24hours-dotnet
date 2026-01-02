namespace Mvp24Hours.Core.Test.Helpers;

/// <summary>
/// Testes unitários para JsonHelper (métodos existentes).
/// </summary>
public class JsonHelperTest
{
    #region [ Serialize Tests ]

    [Fact]
    public void Serialize_WithSimpleObject_ReturnsJsonString()
    {
        // Arrange
        var obj = new Person { Id = 1, Name = "John", Age = 30 };

        // Act
        var json = JsonHelper.Serialize(obj);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("id"); // CamelCase
        json.Should().Contain("name");
        json.Should().Contain("age");
    }

    [Fact]
    public void Serialize_WithNull_ReturnsNull()
    {
        // Act
        var json = JsonHelper.Serialize<Person>(null);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void Serialize_WithComplexObject_SerializesCorrectly()
    {
        // Arrange
        var obj = new Company
        {
            Id = 1,
            Name = "Acme Corp",
            Employees = new List<Person>
            {
                new Person { Id = 1, Name = "John", Age = 30 },
                new Person { Id = 2, Name = "Jane", Age = 25 }
            }
        };

        // Act
        var json = JsonHelper.Serialize(obj);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("employees");
        json.Should().Contain("John");
        json.Should().Contain("Jane");
    }

    #endregion

    #region [ Deserialize Tests ]

    [Fact]
    public void Deserialize_WithValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"John\",\"age\":30}";

        // Act
        var result = JsonHelper.Deserialize<Person>(json);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("John");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_WithComplexObject_DeserializesCorrectly()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"Acme Corp\",\"employees\":[{\"id\":1,\"name\":\"John\",\"age\":30}]}";

        // Act
        var result = JsonHelper.Deserialize<Company>(json);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Name.Should().Be("Acme Corp");
        result.Employees.Should().HaveCount(1);
        result.Employees.First().Name.Should().Be("John");
    }

    [Fact]
    public void Deserialize_WithType_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"John\",\"age\":30}";
        var type = typeof(Person);

        // Act
        var result = JsonHelper.Deserialize(json, type);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Person>();
        ((Person)result).Name.Should().Be("John");
    }

    #endregion

    #region [ Serialize/Deserialize Round Trip Tests ]

    [Fact]
    public void SerializeDeserialize_RoundTrip_PreservesData()
    {
        // Arrange
        var original = new Person { Id = 42, Name = "Alice", Age = 28 };

        // Act
        var json = JsonHelper.Serialize(original);
        var deserialized = JsonHelper.Deserialize<Person>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Id.Should().Be(original.Id);
        deserialized.Name.Should().Be(original.Name);
        deserialized.Age.Should().Be(original.Age);
    }

    [Fact]
    public void SerializeDeserialize_WithCollections_PreservesAllElements()
    {
        // Arrange
        var original = new Company
        {
            Id = 1,
            Name = "Tech Corp",
            Employees = new List<Person>
            {
                new Person { Id = 1, Name = "John", Age = 30 },
                new Person { Id = 2, Name = "Jane", Age = 25 },
                new Person { Id = 3, Name = "Bob", Age = 35 }
            }
        };

        // Act
        var json = JsonHelper.Serialize(original);
        var deserialized = JsonHelper.Deserialize<Company>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Employees.Should().HaveCount(3);
        deserialized.Employees.Select(e => e.Name).Should().Equal("John", "Jane", "Bob");
    }

    #endregion

    #region [ DeserializeAnonymous Tests ]

    [Fact]
    public void DeserializeAnonymous_WithAnonymousType_ReturnsObject()
    {
        // Arrange
        var json = "{\"id\":1,\"name\":\"John\"}";
        var template = new { id = 0, name = "" };

        // Act
        var result = JsonHelper.DeserializeAnonymous(json, template);

        // Assert
        result.Should().NotBeNull();
        result.id.Should().Be(1);
        result.name.Should().Be("John");
    }

    #endregion

    #region [ Special Cases Tests ]

    [Fact]
    public void Serialize_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var obj = new Person { Id = 1, Name = "José 世界 🌍", Age = 30 };

        // Act
        var json = JsonHelper.Serialize(obj);
        var deserialized = JsonHelper.Deserialize<Person>(json);

        // Assert
        deserialized.Name.Should().Be("José 世界 🌍");
    }

    [Fact]
    public void Serialize_WithDateTime_HandlesCorrectly()
    {
        // Arrange
        var obj = new Event { Id = 1, Name = "Meeting", Date = new DateTime(2025, 1, 2) };

        // Act
        var json = JsonHelper.Serialize(obj);
        var deserialized = JsonHelper.Deserialize<Event>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Date.Year.Should().Be(2025);
        deserialized.Date.Month.Should().Be(1);
        deserialized.Date.Day.Should().Be(2);
    }

    [Fact]
    public void Serialize_WithEnum_HandlesCorrectly()
    {
        // Arrange
        var obj = new Order { Id = 1, Status = Status.Active };

        // Act
        var json = JsonHelper.Serialize(obj);
        var deserialized = JsonHelper.Deserialize<Order>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.Status.Should().Be(Status.Active);
    }

    [Fact]
    public void Serialize_ReferenceLoop_IgnoresLoop()
    {
        // Arrange
        var parent = new ParentNode { Name = "Parent" };
        var child = new ChildNode { Name = "Child", Parent = parent };
        parent.Child = child;

        // Act (não deve lançar exceção)
        var json = JsonHelper.Serialize(parent);

        // Assert
        json.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region [ Performance Tests ]

    [Fact]
    public void Serialize_WithLargeObject_ShouldPerformEfficiently()
    {
        // Arrange
        var largeList = Enumerable.Range(1, 10000)
            .Select(i => new Person { Id = i, Name = $"Person{i}", Age = i % 100 })
            .ToList();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var json = JsonHelper.Serialize(largeList);
        stopwatch.Stop();

        // Assert
        json.Should().NotBeNullOrEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public void Deserialize_WithLargeJson_ShouldPerformEfficiently()
    {
        // Arrange
        var largeList = Enumerable.Range(1, 10000)
            .Select(i => new Person { Id = i, Name = $"Person{i}", Age = i % 100 })
            .ToList();
        var json = JsonHelper.Serialize(largeList);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = JsonHelper.Deserialize<List<Person>>(json);
        stopwatch.Stop();

        // Assert
        result.Should().HaveCount(10000);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion

    #region [ Test Classes ]

    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Person> Employees { get; set; } = new();
    }

    private class Event
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    private class Order
    {
        public int Id { get; set; }
        public Status Status { get; set; }
    }

    private enum Status
    {
        Active,
        Inactive,
        Pending
    }

    private class ParentNode
    {
        public string Name { get; set; } = string.Empty;
        public ChildNode? Child { get; set; }
    }

    private class ChildNode
    {
        public string Name { get; set; } = string.Empty;
        public ParentNode? Parent { get; set; }
    }

    #endregion
}
