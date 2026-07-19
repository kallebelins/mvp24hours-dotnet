//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text.Json;
using Mvp24Hours.Core.Serialization.Json;
using Mvp24Hours.Core.ValueObjects;

namespace Mvp24Hours.Core.Test.Serialization;

/// <summary>
/// Unit tests for EntityId JSON converters (System.Text.Json).
/// </summary>
[Trait("Category", "Unit")]
public class EntityIdJsonConvertersTest
{
    #region Test ID Types

    public sealed class TestGuidId(Guid value) : GuidEntityId<TestGuidId>(value) { }
    public sealed class TestIntId(int value) : IntEntityId<TestIntId>(value) { }
    public sealed class TestLongId(long value) : LongEntityId<TestLongId>(value) { }
    public sealed class TestStringId(string value) : StringEntityId<TestStringId>(value) { }

    private record GuidIdHolder { public TestGuidId? Id { get; set; } }
    private record IntIdHolder { public TestIntId? Id { get; set; } }
    private record LongIdHolder { public TestLongId? Id { get; set; } }
    private record StringIdHolder { public TestStringId? Id { get; set; } }

    #endregion

    #region GuidEntityIdJsonConverter Tests

    [Fact]
    public void GuidEntityIdJsonConverter_Serialize_WritesGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new GuidEntityIdJsonConverter<TestGuidId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);

        // Assert
        json.Should().Contain(guid.ToString());
    }

    [Fact]
    public void GuidEntityIdJsonConverter_Deserialize_ReadsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        string json = $"\"{guid}\"";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new GuidEntityIdJsonConverter<TestGuidId>());

        // Act
        var id = JsonSerializer.Deserialize<TestGuidId>(json, options);

        // Assert
        id.Should().NotBeNull();
        id!.Value.Should().Be(guid);
    }

    [Fact]
    public void GuidEntityIdJsonConverter_Serialize_WithNull_WritesNull()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new GuidEntityIdJsonConverter<TestGuidId>());

        // Act
        string json = JsonSerializer.Serialize<TestGuidId?>(null, options);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void GuidEntityIdJsonConverter_Deserialize_WithNull_ReturnsDefault()
    {
        // Arrange
        string json = "null";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new GuidEntityIdJsonConverter<TestGuidId>());

        // Act
        var id = JsonSerializer.Deserialize<TestGuidId>(json, options);

        // Assert
        id.Should().BeNull();
    }

    [Fact]
    public void GuidEntityIdJsonConverter_RoundTrip_PreservesValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new GuidEntityIdJsonConverter<TestGuidId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);
        var deserialized = JsonSerializer.Deserialize<TestGuidId>(json, options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(guid);
    }

    #endregion

    #region IntEntityIdJsonConverter Tests

    [Fact]
    public void IntEntityIdJsonConverter_Serialize_WritesNumber()
    {
        // Arrange
        var id = new TestIntId(42);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IntEntityIdJsonConverter<TestIntId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);

        // Assert
        json.Should().Be("42");
    }

    [Fact]
    public void IntEntityIdJsonConverter_Deserialize_ReadsNumber()
    {
        // Arrange
        string json = "42";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IntEntityIdJsonConverter<TestIntId>());

        // Act
        var id = JsonSerializer.Deserialize<TestIntId>(json, options);

        // Assert
        id.Should().NotBeNull();
        id!.Value.Should().Be(42);
    }

    [Fact]
    public void IntEntityIdJsonConverter_Deserialize_FromString_ReadsValue()
    {
        // Arrange
        string json = "\"42\"";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IntEntityIdJsonConverter<TestIntId>());

        // Act
        var id = JsonSerializer.Deserialize<TestIntId>(json, options);

        // Assert
        id.Should().NotBeNull();
        id!.Value.Should().Be(42);
    }

    [Fact]
    public void IntEntityIdJsonConverter_RoundTrip_PreservesValue()
    {
        // Arrange
        var id = new TestIntId(999);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IntEntityIdJsonConverter<TestIntId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);
        var deserialized = JsonSerializer.Deserialize<TestIntId>(json, options);

        // Assert
        deserialized!.Value.Should().Be(999);
    }

    #endregion

    #region LongEntityIdJsonConverter Tests

    [Fact]
    public void LongEntityIdJsonConverter_Serialize_WritesNumber()
    {
        // Arrange
        var id = new TestLongId(9999999999L);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LongEntityIdJsonConverter<TestLongId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);

        // Assert
        json.Should().Be("9999999999");
    }

    [Fact]
    public void LongEntityIdJsonConverter_Deserialize_ReadsNumber()
    {
        // Arrange
        string json = "9999999999";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LongEntityIdJsonConverter<TestLongId>());

        // Act
        var id = JsonSerializer.Deserialize<TestLongId>(json, options);

        // Assert
        id!.Value.Should().Be(9999999999L);
    }

    [Fact]
    public void LongEntityIdJsonConverter_Deserialize_FromString_ReadsValue()
    {
        // Arrange
        string json = "\"9999999999\"";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LongEntityIdJsonConverter<TestLongId>());

        // Act
        var id = JsonSerializer.Deserialize<TestLongId>(json, options);

        // Assert
        id!.Value.Should().Be(9999999999L);
    }

    [Fact]
    public void LongEntityIdJsonConverter_RoundTrip_PreservesValue()
    {
        // Arrange
        var id = new TestLongId(123456789012345L);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LongEntityIdJsonConverter<TestLongId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);
        var deserialized = JsonSerializer.Deserialize<TestLongId>(json, options);

        // Assert
        deserialized!.Value.Should().Be(123456789012345L);
    }

    #endregion

    #region StringEntityIdJsonConverter Tests

    [Fact]
    public void StringEntityIdJsonConverter_Serialize_WritesString()
    {
        // Arrange
        var id = new TestStringId("ABC-123");
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StringEntityIdJsonConverter<TestStringId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);

        // Assert
        json.Should().Be("\"ABC-123\"");
    }

    [Fact]
    public void StringEntityIdJsonConverter_Deserialize_ReadsString()
    {
        // Arrange
        string json = "\"ABC-123\"";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StringEntityIdJsonConverter<TestStringId>());

        // Act
        var id = JsonSerializer.Deserialize<TestStringId>(json, options);

        // Assert
        id!.Value.Should().Be("ABC-123");
    }

    [Fact]
    public void StringEntityIdJsonConverter_RoundTrip_PreservesValue()
    {
        // Arrange
        var id = new TestStringId("my-string-id-value");
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StringEntityIdJsonConverter<TestStringId>());

        // Act
        string json = JsonSerializer.Serialize(id, options);
        var deserialized = JsonSerializer.Deserialize<TestStringId>(json, options);

        // Assert
        deserialized!.Value.Should().Be("my-string-id-value");
    }

    #endregion

    #region EntityIdJsonConverterFactory Tests

    [Fact]
    public void EntityIdJsonConverterFactory_CanConvert_GuidEntityId_ReturnsTrue()
    {
        // Arrange
        var factory = new EntityIdJsonConverterFactory();

        // Act
        bool canConvert = factory.CanConvert(typeof(TestGuidId));

        // Assert
        canConvert.Should().BeTrue();
    }

    [Fact]
    public void EntityIdJsonConverterFactory_CanConvert_IntEntityId_ReturnsTrue()
    {
        // Arrange
        var factory = new EntityIdJsonConverterFactory();

        // Act
        bool canConvert = factory.CanConvert(typeof(TestIntId));

        // Assert
        canConvert.Should().BeTrue();
    }

    [Fact]
    public void EntityIdJsonConverterFactory_CanConvert_LongEntityId_ReturnsTrue()
    {
        // Arrange
        var factory = new EntityIdJsonConverterFactory();

        // Act
        bool canConvert = factory.CanConvert(typeof(TestLongId));

        // Assert
        canConvert.Should().BeTrue();
    }

    [Fact]
    public void EntityIdJsonConverterFactory_CanConvert_StringEntityId_ReturnsTrue()
    {
        // Arrange
        var factory = new EntityIdJsonConverterFactory();

        // Act
        bool canConvert = factory.CanConvert(typeof(TestStringId));

        // Assert
        canConvert.Should().BeTrue();
    }

    [Fact]
    public void EntityIdJsonConverterFactory_CanConvert_RegularType_ReturnsFalse()
    {
        // Arrange
        var factory = new EntityIdJsonConverterFactory();

        // Act
        bool canConvert = factory.CanConvert(typeof(string));
        bool canConvertInt = factory.CanConvert(typeof(int));
        bool canConvertGuid = factory.CanConvert(typeof(Guid));

        // Assert
        canConvert.Should().BeFalse();
        canConvertInt.Should().BeFalse();
        canConvertGuid.Should().BeFalse();
    }

    [Fact]
    public void EntityIdJsonConverterFactory_CanConvert_Null_ReturnsFalse()
    {
        // Arrange
        var factory = new EntityIdJsonConverterFactory();

        // Act
        bool canConvert = factory.CanConvert(null!);

        // Assert
        canConvert.Should().BeFalse();
    }

    [Fact]
    public void EntityIdJsonConverterFactory_WithGuidId_SerializesAndDeserializes()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EntityIdJsonConverterFactory());

        // Act
        string json = JsonSerializer.Serialize(id, options);
        var deserialized = JsonSerializer.Deserialize<TestGuidId>(json, options);

        // Assert
        deserialized!.Value.Should().Be(guid);
    }

    [Fact]
    public void EntityIdJsonConverterFactory_WithIntId_SerializesAndDeserializes()
    {
        // Arrange
        var id = new TestIntId(42);
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EntityIdJsonConverterFactory());

        // Act
        string json = JsonSerializer.Serialize(id, options);
        var deserialized = JsonSerializer.Deserialize<TestIntId>(json, options);

        // Assert
        deserialized!.Value.Should().Be(42);
    }

    [Fact]
    public void EntityIdJsonConverterFactory_WithStringId_SerializesAndDeserializes()
    {
        // Arrange
        var id = new TestStringId("test-value");
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EntityIdJsonConverterFactory());

        // Act
        string json = JsonSerializer.Serialize(id, options);
        var deserialized = JsonSerializer.Deserialize<TestStringId>(json, options);

        // Assert
        deserialized!.Value.Should().Be("test-value");
    }

    #endregion
}
