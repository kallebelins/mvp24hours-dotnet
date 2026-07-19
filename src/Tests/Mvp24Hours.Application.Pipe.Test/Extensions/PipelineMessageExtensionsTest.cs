//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Extensions;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class PipelineMessageExtensionsTest
{
    [Fact, Priority(1)]
    public void ToMessage_FromString_ShouldCreateMessageWithContent()
    {
        IPipelineMessage msg = "hello".ToMessage();

        msg.Should().NotBeNull();
        msg.HasContent<string>().Should().BeTrue();
        msg.GetContent<string>().Should().Be("hello");
    }

    [Fact, Priority(2)]
    public void ToMessage_WithKey_ShouldAddContentUnderKey()
    {
        IPipelineMessage msg = 42.ToMessage("number");

        msg.HasContent("number").Should().BeTrue();
        msg.GetContent<int>("number").Should().Be(42);
    }

    [Fact, Priority(3)]
    public void ToMessageWithToken_ShouldUseProvidedToken()
    {
        IPipelineMessage msg = "data".ToMessageWithToken("my-token");

        msg.Token.Should().Be("my-token");
        msg.GetContent<string>().Should().Be("data");
    }

    [Fact, Priority(4)]
    public void ToMessageWithToken_NullToken_ShouldGenerateNewToken()
    {
        IPipelineMessage msg = "data".ToMessageWithToken(null);

        msg.Token.Should().NotBeNullOrEmpty();
    }

    [Fact, Priority(5)]
    public void ToMessage_NullValue_ShouldCreateEmptyMessage()
    {
        IPipelineMessage msg = ((string?)null)!.ToMessage();

        msg.Should().NotBeNull();
        msg.HasContent<string>().Should().BeFalse();
    }

    [Fact, Priority(6)]
    public void AddMessage_ToList_ShouldAddMessageResult()
    {
        IPipelineMessage msg = new PipelineMessage();

        msg.Messages.AddMessage("warn msg", MessageType.Warning);

        msg.Messages.Should().HaveCount(1);
        msg.Messages[0].Message.Should().Be("warn msg");
        msg.Messages[0].Type.Should().Be(MessageType.Warning);
    }

    [Fact, Priority(7)]
    public void AddMessage_WithKey_ToList_ShouldAddKeyedMessage()
    {
        IPipelineMessage msg = new PipelineMessage();

        msg.Messages.AddMessage("key1", "keyed error", MessageType.Error);

        msg.Messages.Should().HaveCount(1);
        msg.Messages[0].Key.Should().Be("key1");
        msg.Messages[0].Message.Should().Be("keyed error");
    }

    [Fact, Priority(8)]
    public void AddError_ShouldMarkMessageAsFaulty()
    {
        IPipelineMessage msg = new PipelineMessage();

        msg.AddError("critical error");

        msg.IsFaulty.Should().BeTrue();
        msg.Messages.Should().HaveCount(1);
        msg.Messages[0].Type.Should().Be(MessageType.Error);
        msg.Messages[0].Message.Should().Be("critical error");
    }

    [Fact, Priority(9)]
    public void AddMessage_NullMessages_ShouldThrow()
    {
        IList<Core.Contract.ValueObjects.Logic.IMessageResult>? list = null;

        Action act = () => list!.AddMessage("msg", MessageType.Info);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(10)]
    public void AddMessage_NullMessage_ShouldThrow()
    {
        IPipelineMessage msg = new PipelineMessage();

        Action act = () => msg.Messages.AddMessage(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(11)]
    public void ToMessage_Integer_ShouldCreateMessageWithIntContent()
    {
        IPipelineMessage msg = 100.ToMessage();

        msg.HasContent<int>().Should().BeTrue();
        msg.GetContent<int>().Should().Be(100);
    }

    [Fact, Priority(12)]
    public void ToMessageWithToken_WithKeyAndToken_ShouldSetBoth()
    {
        IPipelineMessage msg = "val".ToMessageWithToken("val-key", "tok-123");

        msg.Token.Should().Be("tok-123");
        msg.HasContent("val-key").Should().BeTrue();
        msg.GetContent<string>("val-key").Should().Be("val");
    }
}
