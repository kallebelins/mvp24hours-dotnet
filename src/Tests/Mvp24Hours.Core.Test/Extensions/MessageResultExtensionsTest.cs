using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public sealed class MessageResultExtensionsTest
{
    [Fact]
    public void ToMessageResult_FromString_ShouldCreateTypedMessage()
    {
        IMessageResult message = "validation failed".ToMessageResult(MessageType.Error);

        message.Message.Should().Be("validation failed");
        message.Type.Should().Be(MessageType.Error);
    }

    [Fact]
    public void ToMessageResult_FromStringWithKey_ShouldPreserveKey()
    {
        IMessageResult message = "invalid email".ToMessageResult("email", MessageType.Error);

        message.Key.Should().Be("email");
        message.Message.Should().Be("invalid email");
    }

    [Fact]
    public void ToMessageResult_FromEnumerable_ShouldMapEachMessage()
    {
        IEnumerable<IMessageResult> messages = new[] { "one", "two" }
            .ToMessageResult(MessageType.Warning, "CUSTOM");

        messages.Should().HaveCount(2);
        messages.Should().OnlyContain(message => message.Type == MessageType.Warning);
    }

    [Fact]
    public void ToMessageResult_FromException_ShouldUseInnerMessage()
    {
        InvalidOperationException exception = new(
            "outer",
            new ArgumentException("inner"));

        IMessageResult message = exception.ToMessageResult();

        message.Type.Should().Be(MessageType.Error);
        message.Message.Should().Be("inner");
    }

    [Fact]
    public void ToMessageResult_WithNullMessage_ShouldUseFallbackText()
    {
        string? value = null;

        value!.ToMessageResult(MessageType.Info).Message
            .Should().Be("Undefined message.");
    }
}
