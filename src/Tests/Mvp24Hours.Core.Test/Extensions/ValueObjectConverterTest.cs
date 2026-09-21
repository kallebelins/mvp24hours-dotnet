using Mvp24Hours.Core.Converters;
using Newtonsoft.Json;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class ValueObjectConverterTest
{
    public interface ISampleValue
    {
        string Name { get; set; }
    }

    public sealed class ConcreteSampleValue : ISampleValue
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class UnrelatedType
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class Wrapper
    {
        public ISampleValue? Value { get; set; }
    }

    #region [ CanConvert ]

    [Fact]
    public void CanConvert_WithMatchingInterfaceType_ReturnsTrue()
    {
        // Arrange
        var converter = new ValueObjectConverter<ISampleValue, ConcreteSampleValue>();

        // Act
        bool result = converter.CanConvert(typeof(ISampleValue));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanConvert_WithUnrelatedType_ReturnsFalse()
    {
        // Arrange
        var converter = new ValueObjectConverter<ISampleValue, ConcreteSampleValue>();

        // Act
        bool result = converter.CanConvert(typeof(UnrelatedType));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanConvert_WithConcreteType_ReturnsFalse()
    {
        // Arrange
        var converter = new ValueObjectConverter<ISampleValue, ConcreteSampleValue>();

        // Act
        bool result = converter.CanConvert(typeof(ConcreteSampleValue));

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region [ Serialize / Deserialize round-trip ]

    [Fact]
    public void SerializeAndDeserialize_WithInterfaceProperty_RoundTripsThroughConcreteType()
    {
        // Arrange
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new ValueObjectConverter<ISampleValue, ConcreteSampleValue>());
        var wrapper = new Wrapper { Value = new ConcreteSampleValue { Name = "Alpha" } };

        // Act
        string json = JsonConvert.SerializeObject(wrapper, settings);
        Wrapper? result = JsonConvert.DeserializeObject<Wrapper>(json, settings);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        result.Value.Should().BeOfType<ConcreteSampleValue>();
        result.Value!.Name.Should().Be("Alpha");
    }

    [Fact]
    public void Serialize_WithInterfaceProperty_ProducesConcreteTypeJsonShape()
    {
        // Arrange
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new ValueObjectConverter<ISampleValue, ConcreteSampleValue>());
        var wrapper = new Wrapper { Value = new ConcreteSampleValue { Name = "Beta" } };

        // Act
        string json = JsonConvert.SerializeObject(wrapper, settings);

        // Assert
        json.Should().Contain("\"Name\":\"Beta\"");
    }

    #endregion
}
