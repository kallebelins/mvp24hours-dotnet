using System.Text.Json;
using System.Text.Json.Serialization;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Converters;
using Mvp24Hours.Core.Serialization.Json;
using Mvp24Hours.Core.Serialization.SourceGeneration;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mvp24Hours.Core.Test.Serialization;

[Trait("Category", "Unit")]
public class SerializationTest
{
    private sealed class FieldContainer
    {
        public string Name { get; set; } = string.Empty;
        public int Count;
    }

    [Fact]
    public void PropertyAndFieldsSerializerResolver_SerializesPublicFields()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new PropertyAndFieldsSerializerResolver()
        };

        string json = JsonConvert.SerializeObject(new FieldContainer { Name = "test", Count = 42 }, settings);

        json.Should().Contain("\"Count\":42");
        json.Should().Contain("\"Name\":\"test\"");
    }

    [Fact]
    public void CompositeContractResolver_UsesFirstMatchingResolver()
    {
        var composite = new CompositeContractResolver();
        composite.Add(new DefaultContractResolver());

        JsonContract contract = composite.ResolveContract(typeof(FieldContainer));

        contract.Should().NotBeNull();
        composite.Should().HaveCount(1);
    }

    [Fact]
    public void CompositeContractResolver_AddNull_Throws()
    {
        var composite = new CompositeContractResolver();

        Action act = () => composite.Add(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValueObjectConverter_RoundTripsInterfaceAsConcrete()
    {
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new ValueObjectConverter<IMessageResult, MessageResult>());

        IMessageResult original = new MessageResult("Email", "Invalid email", MessageType.Error);
        string json = JsonConvert.SerializeObject(original, settings);
        IMessageResult? restored = JsonConvert.DeserializeObject<IMessageResult>(json, settings);

        restored.Should().NotBeNull();
        restored!.Key.Should().Be("Email");
        restored.Message.Should().Be("Invalid email");
    }

    [Fact]
    public void Mvp24HoursJsonSerializerContext_RoundTripsBusinessResult()
    {
        IBusinessResult<string> result = BusinessResult.Success("ok");

        string json = System.Text.Json.JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.Default.BusinessResultString);
        BusinessResult<string>? restored = System.Text.Json.JsonSerializer.Deserialize(json, Mvp24HoursJsonSerializerContext.Default.BusinessResultString);

        restored.Should().NotBeNull();
        restored!.HasErrors.Should().BeFalse();
        restored.Data.Should().Be("ok");
    }

    [Fact]
    public void Mvp24HoursJsonSerializerContext_CreateOptions_IncludesSourceGeneratedResolver()
    {
        JsonSerializerOptions options = Mvp24HoursJsonSerializerContext.CreateOptions();

        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        options.TypeInfoResolverChain.Should().Contain(Mvp24HoursJsonSerializerContext.Default);
    }

    [Fact]
    public void Mvp24HoursJsonSerializerContext_CreateOptionsWithConverters_AddsConverter()
    {
        var converter = new JsonStringEnumConverter();
        JsonSerializerOptions options = Mvp24HoursJsonSerializerContext.CreateOptionsWithConverters(converter);

        options.Converters.Should().Contain(converter);
    }
}
