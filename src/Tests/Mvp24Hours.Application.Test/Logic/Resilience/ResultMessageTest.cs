using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Logic.Resilience;
using Mvp24Hours.Core.Enums;

namespace Mvp24Hours.Application.Test.Logic.Resilience;

[Trait("Category", "Unit")]
public class ResultMessageTest
{
    [Fact]
    public void Error_ShouldCreateErrorMessageWithCode()
    {
        ResultMessage message = ResultMessage.Error("Invalid value", "VALIDATION.INVALID", "Amount", 0);

        message.Severity.Should().Be(MessageSeverity.Error);
        message.ErrorCode.Should().Be("VALIDATION.INVALID");
        message.PropertyName.Should().Be("Amount");
        message.AttemptedValue.Should().Be(0);
        message.Type.Should().Be(MessageType.Error);
    }

    [Fact]
    public void Warning_ShouldCreateWarningMessage()
    {
        ResultMessage message = ResultMessage.Warning("Low stock", "INVENTORY.LOW", "Quantity");

        message.Severity.Should().Be(MessageSeverity.Warning);
        message.Type.Should().Be(MessageType.Warning);
    }

    [Fact]
    public void Info_ShouldUseKeyForLookup()
    {
        ResultMessage message = ResultMessage.Info("Additional detail", "INFO.DETAIL");

        message.Severity.Should().Be(MessageSeverity.Info);
        message.Key.Should().Be("INFO.DETAIL");
        message.Type.Should().Be(MessageType.Info);
    }

    [Fact]
    public void Constructor_WithNullMessage_ShouldThrow()
    {
        Func<ResultMessage> act = () => new ResultMessage(MessageSeverity.Error, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToString_ShouldIncludeSeverityCodeAndProperty()
    {
        ResultMessage message = ResultMessage.Error("Bad", "ERR.BAD", "Field");

        message.ToString().Should().Contain("[Error]");
        message.ToString().Should().Contain("(ERR.BAD)");
        message.ToString().Should().Contain("Field:");
        message.ToString().Should().Contain("Bad");
    }

    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        ResultMessage left = ResultMessage.Error("Same", "CODE", "Prop");
        ResultMessage right = ResultMessage.Error("Same", "CODE", "Prop");

        left.Should().Be(right);
    }
}
