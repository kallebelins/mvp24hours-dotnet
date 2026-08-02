//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Infrastructure.Pipe;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class PipelineMessageTest
{
    [Fact, Priority(1)]
    public void PipelineMessage_DefaultConstructor_ShouldHaveValidToken()
    {
        var message = new PipelineMessage();

        message.Token.Should().NotBeNullOrEmpty();
        message.IsFaulty.Should().BeFalse();
        message.IsLocked.Should().BeFalse();
        message.Messages.Should().BeEmpty();
    }

    [Fact, Priority(2)]
    public void PipelineMessage_WithCustomToken_ShouldUseProvidedToken()
    {
        const string token = "custom-token-123";
        var message = new PipelineMessage(token);

        message.Token.Should().Be(token);
    }

    [Fact, Priority(3)]
    public void PipelineMessage_WithNullToken_ShouldGenerateNewToken()
    {
        var message = new PipelineMessage((string?)null);

        message.Token.Should().NotBeNullOrEmpty();
    }

    [Fact, Priority(4)]
    public void PipelineMessage_WithArgs_ShouldAddContents()
    {
        // Cast to object to use PipelineMessage(params object[]?) constructor
        // (not PipelineMessage(string? token, params object[]?))
        var message = new PipelineMessage((object)"hello", (object)42);

        message.HasContent<string>().Should().BeTrue();
        message.HasContent<int>().Should().BeTrue();
        message.GetContent<string>().Should().Be("hello");
        message.GetContent<int>().Should().Be(42);
    }

    [Fact, Priority(5)]
    public void PipelineMessage_AddContent_ByType_ShouldStoreAndRetrieve()
    {
        var message = new PipelineMessage();

        message.AddContent("test-value");

        message.HasContent<string>().Should().BeTrue();
        message.GetContent<string>().Should().Be("test-value");
    }

    [Fact, Priority(6)]
    public void PipelineMessage_AddContent_ByKey_ShouldStoreAndRetrieve()
    {
        var message = new PipelineMessage();

        message.AddContent("my-key", 99);

        message.HasContent("my-key").Should().BeTrue();
        message.GetContent<int>("my-key").Should().Be(99);
    }

    [Fact, Priority(7)]
    public void PipelineMessage_AddContent_UpdatesExistingKey()
    {
        var message = new PipelineMessage();

        message.AddContent("key", "first");
        message.AddContent("key", "updated");

        message.GetContent<string>("key").Should().Be("updated");
    }

    [Fact, Priority(8)]
    public void PipelineMessage_AddContent_NullValue_ShouldNotAdd()
    {
        var message = new PipelineMessage();

        message.AddContent<string?>(null);

        message.HasContent<string>().Should().BeFalse();
    }

    [Fact, Priority(9)]
    public void PipelineMessage_GetContent_MissingKey_ShouldReturnDefault()
    {
        var message = new PipelineMessage();

        int result = message.GetContent<int>("nonexistent");

        result.Should().Be(0);
    }

    [Fact, Priority(10)]
    public void PipelineMessage_GetContentAll_ShouldReturnAllValues()
    {
        var message = new PipelineMessage();

        message.AddContent("key1", "val1");
        message.AddContent("key2", 42);

        IList<object> all = message.GetContentAll();

        all.Should().HaveCount(2);
        all.Should().Contain("val1");
        all.Should().Contain(42);
    }

    [Fact, Priority(11)]
    public void PipelineMessage_SetLock_ShouldLockMessage()
    {
        var message = new PipelineMessage();

        message.SetLock();

        message.IsLocked.Should().BeTrue();
    }

    [Fact, Priority(12)]
    public void PipelineMessage_SetFailure_ShouldMarkAsFaulty()
    {
        var message = new PipelineMessage();

        message.SetFailure();

        message.IsFaulty.Should().BeTrue();
    }

    [Fact, Priority(13)]
    public void PipelineMessage_IsFaulty_WhenErrorMessage_ShouldBeTrue()
    {
        var message = new PipelineMessage();
        message.Messages.Add(new Core.ValueObjects.Logic.MessageResult("error", MessageType.Error));

        message.IsFaulty.Should().BeTrue();
    }

    [Fact, Priority(14)]
    public void PipelineMessage_IsFaulty_WhenInfoMessage_ShouldBeFalse()
    {
        var message = new PipelineMessage();
        message.Messages.Add(new Core.ValueObjects.Logic.MessageResult("info", MessageType.Info));

        message.IsFaulty.Should().BeFalse();
    }

    [Fact, Priority(15)]
    public void PipelineMessage_DynamicContents_ShouldSetAndGetProperties()
    {
        var message = new PipelineMessage();
        message.AddContent("name", "Alice");

        dynamic contents = message.DynamicContents;

        string name = contents.name;
        name.Should().Be("Alice");
    }

    [Fact, Priority(16)]
    public void PipelineMessage_HasContent_WithTypeName_ShouldReturnTrue()
    {
        var message = new PipelineMessage();
        message.AddContent(42);

        message.HasContent(typeof(int).FullName!).Should().BeTrue();
    }

    [Fact, Priority(17)]
    public void PipelineMessage_HasContent_WithUnknownKey_ShouldReturnFalse()
    {
        var message = new PipelineMessage();

        message.HasContent("unknown-key").Should().BeFalse();
    }

    [Fact, Priority(18)]
    public void PipelineMessage_GetContent_ByType_WhenEmpty_ShouldReturnDefault()
    {
        var message = new PipelineMessage();

        List<string> result = message.GetContent<List<string>>();

        result.Should().BeNull();
    }

    [Fact, Priority(19)]
    public void PipelineMessage_MultipleMessagesWithDifferentTypes_ShouldAllBeAccessible()
    {
        var message = new PipelineMessage();

        message.AddContent("str-key", "hello");
        message.AddContent("int-key", 10);
        message.AddContent("bool-key", true);

        message.GetContent<string>("str-key").Should().Be("hello");
        message.GetContent<int>("int-key").Should().Be(10);
        message.GetContent<bool>("bool-key").Should().BeTrue();
    }

    [Fact, Priority(20)]
    public void PipelineMessage_WhenBothSetFailureAndErrorMessage_ShouldBeFaulty()
    {
        var message = new PipelineMessage();
        message.SetFailure();
        message.Messages.Add(new Core.ValueObjects.Logic.MessageResult("error", MessageType.Error));

        message.IsFaulty.Should().BeTrue();
        message.Messages.Should().HaveCount(1);
    }
}
