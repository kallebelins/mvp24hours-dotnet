using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Pipe.ExceptionMapping;

namespace Mvp24Hours.Application.Pipe.Test.ExceptionMapping;

[Trait("Category", "Unit")]
public class DefaultPipelineExceptionMapperTest
{
    [Fact]
    public void Map_Should_UseSpecificRuleForMatchingException()
    {
        var mapper = new DefaultPipelineExceptionMapper()
            .AddRule<ArgumentException>(
                ex => [new MessageResult($"Invalid arg: {ex.ParamName}", MessageType.Error)],
                shouldFail: true,
                shouldPropagate: false);

        IEnumerable<Core.Contract.ValueObjects.Logic.IMessageResult> messages =
            mapper.Map(new ArgumentException("bad", "id"));

        messages.Should().ContainSingle(m => m.Message == "Invalid arg: id");
        mapper.ShouldFail(new ArgumentException()).Should().BeTrue();
        mapper.ShouldPropagate(new ArgumentException()).Should().BeFalse();
    }

    [Fact]
    public void Map_Should_UseDefaultMapperForUnknownExceptions()
    {
        var mapper = new DefaultPipelineExceptionMapper()
            .SetDefaultMapper(ex => [new MessageResult($"default: {ex.Message}", MessageType.Warning)]);

        IEnumerable<Core.Contract.ValueObjects.Logic.IMessageResult> messages =
            mapper.Map(new InvalidOperationException("oops"));

        messages.Should().ContainSingle(m => m.Type == MessageType.Warning && m.Message == "default: oops");
    }

    [Fact]
    public void ShouldFailAndPropagate_Should_UseMostSpecificRule()
    {
        var mapper = new DefaultPipelineExceptionMapper()
            .AddRule<Exception>((_) => [], shouldFail: false, shouldPropagate: false)
            .AddRule<InvalidOperationException>((_) => [], shouldFail: true, shouldPropagate: true);

        mapper.ShouldFail(new InvalidOperationException()).Should().BeTrue();
        mapper.ShouldPropagate(new InvalidOperationException()).Should().BeTrue();
        mapper.ShouldFail(new ArgumentException()).Should().BeFalse();
    }

    [Fact]
    public void SetDefaultShouldFail_Should_OverrideDefaultBehavior()
    {
        var mapper = new DefaultPipelineExceptionMapper()
            .SetDefaultShouldFail(_ => false);

        mapper.ShouldFail(new Exception()).Should().BeFalse();
    }
}
