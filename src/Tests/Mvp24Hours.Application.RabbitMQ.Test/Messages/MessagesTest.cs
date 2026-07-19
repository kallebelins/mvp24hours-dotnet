using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Messages;

namespace Mvp24Hours.Application.RabbitMQ.Test.Messages;

public class MessagesTest
{
    [Fact]
    public void Message_DefaultConstructor_ShouldHaveDefaultValues()
    {
        var message = new Message<TestOrderEvent>();

        message.MessageId.Should().NotBeNullOrWhiteSpace();
        message.MessageType.Should().NotBeNullOrWhiteSpace();
        message.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        message.ContentType.Should().Be("application/json");
        message.Headers.Should().NotBeNull();
        message.CorrelationId.Should().BeNull();
        message.CausationId.Should().BeNull();
        message.SourceApplication.Should().BeNull();
    }

    [Fact]
    public void Message_PayloadConstructor_ShouldSetPayload()
    {
        var payload = new TestOrderEvent { Name = "test", CorrelationId = Guid.NewGuid() };
        var message = new Message<TestOrderEvent>(payload);

        message.Payload.Should().NotBeNull();
        message.Payload.Name.Should().Be("test");
        message.Payload.CorrelationId.Should().Be(payload.CorrelationId);
    }

    [Fact]
    public void Message_CorrelationIdConstructor_ShouldSetCorrelationId()
    {
        var payload = new TestOrderEvent { Name = "corr-test" };
        var message = new Message<TestOrderEvent>(payload, "corr-123");

        message.Payload.Name.Should().Be("corr-test");
        message.CorrelationId.Should().Be("corr-123");
    }

    [Fact]
    public void Message_Create_ShouldSetAllProperties()
    {
        var payload = new TestOrderEvent { Name = "factory" };
        var headers = new Dictionary<string, object> { ["x-custom"] = "value" };

        var message = Message<TestOrderEvent>.Create(
            payload,
            correlationId: "corr-abc",
            causationId: "cause-xyz",
            sourceApplication: "my-service",
            headers: headers);

        message.Payload.Name.Should().Be("factory");
        message.CorrelationId.Should().Be("corr-abc");
        message.CausationId.Should().Be("cause-xyz");
        message.SourceApplication.Should().Be("my-service");
        message.Headers.Should().ContainKey("x-custom");
        message.Headers["x-custom"].Should().Be("value");
    }

    [Fact]
    public void Message_Create_WithNullHeaders_ShouldUseEmptyHeaders()
    {
        var payload = new TestOrderEvent();

        var message = Message<TestOrderEvent>.Create(payload, headers: null);

        message.Headers.Should().NotBeNull();
        message.Headers.Should().BeEmpty();
    }

    [Fact]
    public void Message_MessageType_ShouldContainTypeName()
    {
        var message = new Message<TestOrderEvent>();

        message.MessageType.Should().Contain(nameof(TestOrderEvent));
    }

    [Fact]
    public void Message_MessageId_ShouldBeUniquePerInstance()
    {
        var msg1 = new Message<TestOrderEvent>();
        var msg2 = new Message<TestOrderEvent>();

        msg1.MessageId.Should().NotBe(msg2.MessageId);
    }

    [Fact]
    public void Message_Timestamp_ShouldBeUtcNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var message = new Message<TestOrderEvent>();
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        message.Timestamp.Should().BeAfter(before);
        message.Timestamp.Should().BeBefore(after);
    }

    [Fact]
    public void Message_Headers_ShouldBeMutable()
    {
        var message = new Message<TestOrderEvent>
        {
            Headers = new Dictionary<string, object>()
        };

        message.Headers["x-key"] = "value";
        message.Headers.Should().ContainKey("x-key");
    }

    [Fact]
    public void Message_InitProperties_ShouldBeSet()
    {
        var payload = new TestOrderEvent { Name = "init" };
        var message = new Message<TestOrderEvent>
        {
            MessageId = "custom-id-123",
            CorrelationId = "corr-xyz",
            CausationId = "cause-abc",
            SourceApplication = "my-app",
            ContentType = "application/x-custom",
            Payload = payload
        };

        message.MessageId.Should().Be("custom-id-123");
        message.CorrelationId.Should().Be("corr-xyz");
        message.CausationId.Should().Be("cause-abc");
        message.SourceApplication.Should().Be("my-app");
        message.ContentType.Should().Be("application/x-custom");
        message.Payload.Name.Should().Be("init");
    }

    [Fact]
    public void Message_WithValueType_ShouldWork()
    {
        var message = new Message<int>(42);

        message.Payload.Should().Be(42);
        message.MessageType.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Message_WithString_ShouldWork()
    {
        var message = new Message<string>("hello");

        message.Payload.Should().Be("hello");
    }
}
