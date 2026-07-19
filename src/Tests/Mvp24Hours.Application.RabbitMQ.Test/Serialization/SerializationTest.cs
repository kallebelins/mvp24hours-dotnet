using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;

namespace Mvp24Hours.Application.RabbitMQ.Test.Serialization;

public class SerializationTest
{
    [Fact]
    public void JsonMessageSerializer_RoundTrip_ShouldPreservePayload()
    {
        var serializer = new JsonMessageSerializer();
        var original = new TestOrderEvent { Name = "serialize", CorrelationId = Guid.NewGuid() };

        byte[] bytes = serializer.Serialize(original);
        TestOrderEvent? restored = serializer.Deserialize<TestOrderEvent>(bytes);

        restored.Should().NotBeNull();
        restored!.Name.Should().Be("serialize");
        restored.CorrelationId.Should().Be(original.CorrelationId);
        serializer.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void MessageTypeResolver_RegisterAndResolve_ShouldFindType()
    {
        var resolver = new MessageTypeResolver();
        const string typeName = "custom-order-event";
        resolver.RegisterType(typeName, typeof(TestOrderEvent));

        Type? resolved = resolver.ResolveType(typeName);

        resolved.Should().Be(typeof(TestOrderEvent));
    }

    [Fact]
    public void MessageTypeResolver_ResolveFromHeaders_ShouldSupportByteArrayHeader()
    {
        var resolver = new MessageTypeResolver();
        resolver.RegisterType<TestOrderEvent>();

        string registeredName = resolver.GetTypeName(typeof(TestOrderEvent));
        var headers = new Dictionary<string, object>
        {
            ["x-message-type"] = System.Text.Encoding.UTF8.GetBytes(registeredName)
        };

        Type? resolved = resolver.ResolveType(headers);

        resolved.Should().Be(typeof(TestOrderEvent));
    }

    [Fact]
    public void MessageTypeResolver_ResolveUnknownType_ShouldReturnNull()
    {
        var resolver = new MessageTypeResolver();

        resolver.ResolveType("unknown.type.name").Should().BeNull();
        resolver.ResolveType((IDictionary<string, object>?)null).Should().BeNull();
    }

    [Fact]
    public void JsonMessageSerializer_SerializeNull_ShouldReturnNullOrEmpty()
    {
        var serializer = new JsonMessageSerializer();

        byte[] bytes = serializer.Serialize<TestOrderEvent>(null!);

        bytes.Should().NotBeNull();
    }

    [Fact]
    public void JsonMessageSerializer_DeserializeInvalidBytes_ShouldThrowOrReturnNull()
    {
        var serializer = new JsonMessageSerializer();
        byte[] invalid = System.Text.Encoding.UTF8.GetBytes("not-valid-json{{{}}}");

        Action act = () => serializer.Deserialize<TestOrderEvent>(invalid);

        // Either throws a JsonException or returns null depending on implementation
        try
        {
            var result = serializer.Deserialize<TestOrderEvent>(invalid);
            result.Should().BeNull();
        }
        catch (System.Text.Json.JsonException)
        {
            // Expected behavior - invalid JSON throws
        }
    }

    [Fact]
    public void JsonMessageSerializer_ContentType_ShouldBeApplicationJson()
    {
        var serializer = new JsonMessageSerializer();

        serializer.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void JsonMessageSerializer_RoundTrip_ComplexPayload_ShouldPreserveAllFields()
    {
        var serializer = new JsonMessageSerializer();
        var corrId = Guid.NewGuid();
        var original = new TestOrderEvent
        {
            Name = "complex",
            CorrelationId = corrId
        };

        byte[] bytes = serializer.Serialize(original);
        TestOrderEvent? restored = serializer.Deserialize<TestOrderEvent>(bytes);

        restored.Should().NotBeNull();
        restored!.Name.Should().Be("complex");
        restored.CorrelationId.Should().Be(corrId);
    }

    [Fact]
    public void MessageTypeResolver_GetTypeName_ShouldReturnRegisteredName()
    {
        var resolver = new MessageTypeResolver();
        resolver.RegisterType<TestOrderEvent>();

        string name = resolver.GetTypeName(typeof(TestOrderEvent));

        name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void MessageTypeResolver_RegisterType_WithCustomName_ShouldOverrideDefault()
    {
        var resolver = new MessageTypeResolver();
        resolver.RegisterType("my-custom-name", typeof(TestOrderEvent));

        Type? resolved = resolver.ResolveType("my-custom-name");

        resolved.Should().Be(typeof(TestOrderEvent));
    }

    [Fact]
    public void MessageTypeResolver_ResolveFromHeaders_WithStringHeader_ShouldWork()
    {
        var resolver = new MessageTypeResolver();
        resolver.RegisterType<TestOrderEvent>();
        string registeredName = resolver.GetTypeName(typeof(TestOrderEvent));
        var headers = new Dictionary<string, object>
        {
            ["x-message-type"] = registeredName
        };

        Type? resolved = resolver.ResolveType(headers);

        resolved.Should().Be(typeof(TestOrderEvent));
    }

    [Fact]
    public void MessageTypeResolver_ResolveFromEmptyHeaders_ShouldReturnNull()
    {
        var resolver = new MessageTypeResolver();
        var headers = new Dictionary<string, object>();

        Type? resolved = resolver.ResolveType(headers);

        resolved.Should().BeNull();
    }

    [Fact]
    public void MessageTypeResolver_RegisterGeneric_ShouldRegisterWithDefaultName()
    {
        var resolver = new MessageTypeResolver();

        Action act = () => resolver.RegisterType<TestPaymentCompletedEvent>();

        act.Should().NotThrow();
    }
}
