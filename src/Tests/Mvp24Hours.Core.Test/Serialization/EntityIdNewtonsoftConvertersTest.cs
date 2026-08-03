//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Converters;
using Mvp24Hours.Core.ValueObjects;
using Newtonsoft.Json;

namespace Mvp24Hours.Core.Test.Serialization;

/// <summary>
/// Unit tests for EntityId JSON converters (Newtonsoft.Json).
/// </summary>
[Trait("Category", "Unit")]
public class EntityIdNewtonsoftConvertersTest
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

    #region GuidEntityIdNewtonsoftConverter Tests

    [Fact]
    public void GuidEntityIdNewtonsoftConverter_Serialize_WritesGuidString()
    {
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);
        var settings = new JsonSerializerSettings
        {
            Converters = { new GuidEntityIdNewtonsoftConverter<TestGuidId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);

        json.Should().Contain(guid.ToString());
    }

    [Fact]
    public void GuidEntityIdNewtonsoftConverter_Deserialize_ReadsGuidString()
    {
        var guid = Guid.NewGuid();
        string json = $"\"{guid}\"";
        var settings = new JsonSerializerSettings
        {
            Converters = { new GuidEntityIdNewtonsoftConverter<TestGuidId>() }
        };

        TestGuidId? id = JsonConvert.DeserializeObject<TestGuidId>(json, settings);

        id.Should().NotBeNull();
        id!.Value.Should().Be(guid);
    }

    [Fact]
    public void GuidEntityIdNewtonsoftConverter_Serialize_WithNull_WritesNull()
    {
        var settings = new JsonSerializerSettings
        {
            Converters = { new GuidEntityIdNewtonsoftConverter<TestGuidId>() }
        };

        string json = JsonConvert.SerializeObject(null, typeof(TestGuidId), settings);

        json.Should().Be("null");
    }

    [Fact]
    public void GuidEntityIdNewtonsoftConverter_Deserialize_WithNull_ReturnsDefault()
    {
        string json = "null";
        var settings = new JsonSerializerSettings
        {
            Converters = { new GuidEntityIdNewtonsoftConverter<TestGuidId>() }
        };

        TestGuidId? id = JsonConvert.DeserializeObject<TestGuidId>(json, settings);

        id.Should().BeNull();
    }

    [Fact]
    public void GuidEntityIdNewtonsoftConverter_Deserialize_WithInvalidValue_Throws()
    {
        string json = "12345";
        var settings = new JsonSerializerSettings
        {
            Converters = { new GuidEntityIdNewtonsoftConverter<TestGuidId>() }
        };

        Action act = () => JsonConvert.DeserializeObject<TestGuidId>(json, settings);

        act.Should().Throw<JsonSerializationException>();
    }

    [Fact]
    public void GuidEntityIdNewtonsoftConverter_RoundTrip_PreservesValue()
    {
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);
        var settings = new JsonSerializerSettings
        {
            Converters = { new GuidEntityIdNewtonsoftConverter<TestGuidId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);
        TestGuidId? deserialized = JsonConvert.DeserializeObject<TestGuidId>(json, settings);

        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(guid);
    }

    #endregion

    #region IntEntityIdNewtonsoftConverter Tests

    [Fact]
    public void IntEntityIdNewtonsoftConverter_Serialize_WritesNumber()
    {
        var id = new TestIntId(42);
        var settings = new JsonSerializerSettings
        {
            Converters = { new IntEntityIdNewtonsoftConverter<TestIntId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);

        json.Should().Be("42");
    }

    [Fact]
    public void IntEntityIdNewtonsoftConverter_Deserialize_ReadsNumber()
    {
        string json = "42";
        var settings = new JsonSerializerSettings
        {
            Converters = { new IntEntityIdNewtonsoftConverter<TestIntId>() }
        };

        TestIntId? id = JsonConvert.DeserializeObject<TestIntId>(json, settings);

        id.Should().NotBeNull();
        id!.Value.Should().Be(42);
    }

    [Fact]
    public void IntEntityIdNewtonsoftConverter_Deserialize_FromString_ReadsValue()
    {
        string json = "\"42\"";
        var settings = new JsonSerializerSettings
        {
            Converters = { new IntEntityIdNewtonsoftConverter<TestIntId>() }
        };

        TestIntId? id = JsonConvert.DeserializeObject<TestIntId>(json, settings);

        id.Should().NotBeNull();
        id!.Value.Should().Be(42);
    }

    [Fact]
    public void IntEntityIdNewtonsoftConverter_Deserialize_WithInvalidValue_Throws()
    {
        string json = "\"not-a-number\"";
        var settings = new JsonSerializerSettings
        {
            Converters = { new IntEntityIdNewtonsoftConverter<TestIntId>() }
        };

        Action act = () => JsonConvert.DeserializeObject<TestIntId>(json, settings);

        act.Should().Throw<JsonSerializationException>();
    }

    [Fact]
    public void IntEntityIdNewtonsoftConverter_RoundTrip_PreservesValue()
    {
        var id = new TestIntId(999);
        var settings = new JsonSerializerSettings
        {
            Converters = { new IntEntityIdNewtonsoftConverter<TestIntId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);
        TestIntId? deserialized = JsonConvert.DeserializeObject<TestIntId>(json, settings);

        deserialized!.Value.Should().Be(999);
    }

    #endregion

    #region LongEntityIdNewtonsoftConverter Tests

    [Fact]
    public void LongEntityIdNewtonsoftConverter_Serialize_WritesNumber()
    {
        var id = new TestLongId(9999999999L);
        var settings = new JsonSerializerSettings
        {
            Converters = { new LongEntityIdNewtonsoftConverter<TestLongId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);

        json.Should().Be("9999999999");
    }

    [Fact]
    public void LongEntityIdNewtonsoftConverter_Deserialize_ReadsNumber()
    {
        string json = "9999999999";
        var settings = new JsonSerializerSettings
        {
            Converters = { new LongEntityIdNewtonsoftConverter<TestLongId>() }
        };

        TestLongId? id = JsonConvert.DeserializeObject<TestLongId>(json, settings);

        id!.Value.Should().Be(9999999999L);
    }

    [Fact]
    public void LongEntityIdNewtonsoftConverter_Deserialize_FromString_ReadsValue()
    {
        string json = "\"9999999999\"";
        var settings = new JsonSerializerSettings
        {
            Converters = { new LongEntityIdNewtonsoftConverter<TestLongId>() }
        };

        TestLongId? id = JsonConvert.DeserializeObject<TestLongId>(json, settings);

        id!.Value.Should().Be(9999999999L);
    }

    [Fact]
    public void LongEntityIdNewtonsoftConverter_Deserialize_WithInvalidValue_Throws()
    {
        string json = "\"invalid\"";
        var settings = new JsonSerializerSettings
        {
            Converters = { new LongEntityIdNewtonsoftConverter<TestLongId>() }
        };

        Action act = () => JsonConvert.DeserializeObject<TestLongId>(json, settings);

        act.Should().Throw<JsonSerializationException>();
    }

    [Fact]
    public void LongEntityIdNewtonsoftConverter_RoundTrip_PreservesValue()
    {
        var id = new TestLongId(123456789012345L);
        var settings = new JsonSerializerSettings
        {
            Converters = { new LongEntityIdNewtonsoftConverter<TestLongId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);
        TestLongId? deserialized = JsonConvert.DeserializeObject<TestLongId>(json, settings);

        deserialized!.Value.Should().Be(123456789012345L);
    }

    #endregion

    #region StringEntityIdNewtonsoftConverter Tests

    [Fact]
    public void StringEntityIdNewtonsoftConverter_Serialize_WritesString()
    {
        var id = new TestStringId("ABC-123");
        var settings = new JsonSerializerSettings
        {
            Converters = { new StringEntityIdNewtonsoftConverter<TestStringId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);

        json.Should().Be("\"ABC-123\"");
    }

    [Fact]
    public void StringEntityIdNewtonsoftConverter_Deserialize_ReadsString()
    {
        string json = "\"ABC-123\"";
        var settings = new JsonSerializerSettings
        {
            Converters = { new StringEntityIdNewtonsoftConverter<TestStringId>() }
        };

        TestStringId? id = JsonConvert.DeserializeObject<TestStringId>(json, settings);

        id!.Value.Should().Be("ABC-123");
    }

    [Fact]
    public void StringEntityIdNewtonsoftConverter_Deserialize_WithInvalidValue_Throws()
    {
        string json = "123";
        var settings = new JsonSerializerSettings
        {
            Converters = { new StringEntityIdNewtonsoftConverter<TestStringId>() }
        };

        Action act = () => JsonConvert.DeserializeObject<TestStringId>(json, settings);

        act.Should().Throw<JsonSerializationException>();
    }

    [Fact]
    public void StringEntityIdNewtonsoftConverter_RoundTrip_PreservesValue()
    {
        var id = new TestStringId("my-string-id-value");
        var settings = new JsonSerializerSettings
        {
            Converters = { new StringEntityIdNewtonsoftConverter<TestStringId>() }
        };

        string json = JsonConvert.SerializeObject(id, settings);
        TestStringId? deserialized = JsonConvert.DeserializeObject<TestStringId>(json, settings);

        deserialized!.Value.Should().Be("my-string-id-value");
    }

    #endregion

    #region EntityIdNewtonsoftConverter Tests

    [Fact]
    public void EntityIdNewtonsoftConverter_CanConvert_GuidEntityId_ReturnsTrue()
    {
        var converter = new EntityIdNewtonsoftConverter();

        converter.CanConvert(typeof(TestGuidId)).Should().BeTrue();
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_CanConvert_IntEntityId_ReturnsTrue()
    {
        var converter = new EntityIdNewtonsoftConverter();

        converter.CanConvert(typeof(TestIntId)).Should().BeTrue();
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_CanConvert_LongEntityId_ReturnsTrue()
    {
        var converter = new EntityIdNewtonsoftConverter();

        converter.CanConvert(typeof(TestLongId)).Should().BeTrue();
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_CanConvert_StringEntityId_ReturnsTrue()
    {
        var converter = new EntityIdNewtonsoftConverter();

        converter.CanConvert(typeof(TestStringId)).Should().BeTrue();
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_CanConvert_RegularType_ReturnsFalse()
    {
        var converter = new EntityIdNewtonsoftConverter();

        converter.CanConvert(typeof(string)).Should().BeFalse();
        converter.CanConvert(typeof(int)).Should().BeFalse();
        converter.CanConvert(typeof(Guid)).Should().BeFalse();
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_CanConvert_Null_ReturnsFalse()
    {
        var converter = new EntityIdNewtonsoftConverter();

        converter.CanConvert(null!).Should().BeFalse();
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_WithGuidId_SerializesAndDeserializes()
    {
        var guid = Guid.NewGuid();
        var id = new TestGuidId(guid);
        var settings = new JsonSerializerSettings
        {
            Converters = { new EntityIdNewtonsoftConverter() }
        };

        string json = JsonConvert.SerializeObject(id, settings);
        TestGuidId? deserialized = JsonConvert.DeserializeObject<TestGuidId>(json, settings);

        deserialized!.Value.Should().Be(guid);
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_WithIntId_SerializesAndDeserializes()
    {
        var id = new TestIntId(42);
        var settings = new JsonSerializerSettings
        {
            Converters = { new EntityIdNewtonsoftConverter() }
        };

        string json = JsonConvert.SerializeObject(id, settings);
        TestIntId? deserialized = JsonConvert.DeserializeObject<TestIntId>(json, settings);

        deserialized!.Value.Should().Be(42);
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_WithStringId_SerializesAndDeserializes()
    {
        var id = new TestStringId("test-value");
        var settings = new JsonSerializerSettings
        {
            Converters = { new EntityIdNewtonsoftConverter() }
        };

        string json = JsonConvert.SerializeObject(id, settings);
        TestStringId? deserialized = JsonConvert.DeserializeObject<TestStringId>(json, settings);

        deserialized!.Value.Should().Be("test-value");
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_Deserialize_WithNull_ReturnsDefault()
    {
        string json = "null";
        var settings = new JsonSerializerSettings
        {
            Converters = { new EntityIdNewtonsoftConverter() }
        };

        TestGuidId? id = JsonConvert.DeserializeObject<TestGuidId>(json, settings);

        id.Should().BeNull();
    }

    [Fact]
    public void EntityIdNewtonsoftConverter_Deserialize_WithInvalidValue_Throws()
    {
        string json = "\"not-a-guid\"";
        var settings = new JsonSerializerSettings
        {
            Converters = { new EntityIdNewtonsoftConverter() }
        };

        Action act = () => JsonConvert.DeserializeObject<TestGuidId>(json, settings);

        act.Should().Throw<JsonSerializationException>();
    }

    #endregion
}
