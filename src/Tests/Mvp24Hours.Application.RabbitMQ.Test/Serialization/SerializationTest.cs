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
}
