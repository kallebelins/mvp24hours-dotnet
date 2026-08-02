//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Typed;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class OperationResultTest
{
    [Fact, Priority(1)]
    public void OperationResult_Success_ShouldBeSuccessful()
    {
        IOperationResult<int> result = OperationResult<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.Messages.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact, Priority(2)]
    public void OperationResult_Success_WithMessages_ShouldIncludeMessages()
    {
        MessageResult[] msgs = [new MessageResult("info msg", Core.Enums.MessageType.Info)];

        IOperationResult<string> result = OperationResult<string>.Success("hello", msgs);

        result.IsSuccess.Should().BeTrue();
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Message.Should().Be("info msg");
    }

    [Fact, Priority(3)]
    public void OperationResult_Failure_FromString_ShouldHaveError()
    {
        IOperationResult<int> result = OperationResult<int>.Failure("something went wrong");

        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(0);
        result.ErrorMessage.Should().Contain("something went wrong");
    }

    [Fact, Priority(4)]
    public void OperationResult_Failure_FromException_ShouldHaveExceptionMessage()
    {
        var ex = new InvalidOperationException("boom");

        IOperationResult<string> result = OperationResult<string>.Failure(ex);

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("boom");
    }

    [Fact, Priority(5)]
    public void OperationResult_Failure_FromMessages_ShouldContainAll()
    {
        MessageResult[] msgs =
        [
            new MessageResult("err1", Core.Enums.MessageType.Error),
            new MessageResult("err2", Core.Enums.MessageType.Error)
        ];

        IOperationResult<int> result = OperationResult<int>.Failure(msgs);

        result.IsFailure.Should().BeTrue();
        result.Messages.Should().HaveCount(2);
        result.ErrorMessage.Should().Contain("err1").And.Contain("err2");
    }

    [Fact, Priority(6)]
    public void OperationResult_Create_ShouldSetValuesCorrectly()
    {
        IOperationResult<double> result = OperationResult<double>.Create(3.14, true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3.14);
    }

    [Fact, Priority(7)]
    public void OperationResult_Map_OnSuccess_ShouldTransformValue()
    {
        var result = OperationResult<int>.Success(5);

        OperationResult<string> mapped = result.Map(v => v.ToString());

        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("5");
    }

    [Fact, Priority(8)]
    public void OperationResult_Map_OnFailure_ShouldPropagateFailure()
    {
        var result = OperationResult<int>.Failure("error");

        OperationResult<string> mapped = result.Map(v => v.ToString());

        mapped.IsFailure.Should().BeTrue();
        mapped.ErrorMessage.Should().Contain("error");
    }

    [Fact, Priority(9)]
    public void OperationResult_Map_TransformThrows_ShouldReturnFailure()
    {
        var result = OperationResult<int>.Success(5);

        OperationResult<string> mapped = result.Map<string>(_ => throw new Exception("transform failed"));

        mapped.IsFailure.Should().BeTrue();
        mapped.ErrorMessage.Should().Contain("transform failed");
    }

    [Fact, Priority(10)]
    public void OperationResult_Bind_OnSuccess_ShouldChainOperation()
    {
        var result = OperationResult<int>.Success(10);

        OperationResult<string> bound = result.Bind(v => OperationResult<string>.Success($"val:{v}"));

        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("val:10");
    }

    [Fact, Priority(11)]
    public void OperationResult_Bind_OnFailure_ShouldShortCircuit()
    {
        var result = OperationResult<int>.Failure("fail");

        bool bindCalled = false;
        OperationResult<string> bound = result.Bind(v =>
        {
            bindCalled = true;
            return OperationResult<string>.Success(v.ToString());
        });

        bound.IsFailure.Should().BeTrue();
        bindCalled.Should().BeFalse();
    }

    [Fact, Priority(12)]
    public void OperationResult_Bind_WhenBindThrows_ShouldReturnFailure()
    {
        var result = OperationResult<int>.Success(1);

        OperationResult<string> bound = result.Bind<string>(_ => throw new Exception("bind failed"));

        bound.IsFailure.Should().BeTrue();
        bound.ErrorMessage.Should().Contain("bind failed");
    }

    [Fact, Priority(13)]
    public void OperationResult_Match_OnSuccess_ShouldCallSuccessHandler()
    {
        var result = OperationResult<int>.Success(7);

        string output = result.Match(
            onSuccess: v => $"success:{v}",
            onFailure: msgs => "failure"
        );

        output.Should().Be("success:7");
    }

    [Fact, Priority(14)]
    public void OperationResult_Match_OnFailure_ShouldCallFailureHandler()
    {
        var result = OperationResult<int>.Failure("oops");

        string output = result.Match(
            onSuccess: v => "success",
            onFailure: msgs => $"failure:{msgs.Count}"
        );

        output.Should().StartWith("failure:1");
    }

    [Fact, Priority(15)]
    public void OperationResult_Match_NullSuccessHandler_ShouldThrow()
    {
        var result = OperationResult<int>.Success(1);

        Action act = () => result.Match<string>(null!, msgs => "f");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(16)]
    public void OperationResult_Match_NullFailureHandler_ShouldThrow()
    {
        var result = OperationResult<int>.Success(1);

        Action act = () => result.Match<string>(v => "s", null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(17)]
    public void OperationResult_ImplicitConversion_ShouldCreateSuccess()
    {
        OperationResult<int> result = 99;

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(99);
    }

    [Fact, Priority(18)]
    public void OperationResult_Map_NullTransform_ShouldThrow()
    {
        var result = OperationResult<int>.Success(1);

        Action act = () => result.Map<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(19)]
    public void OperationResult_Bind_NullBind_ShouldThrow()
    {
        var result = OperationResult<int>.Success(1);

        Action act = () => result.Bind<string>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact, Priority(20)]
    public void OperationResult_StaticHelper_Success_ShouldWork()
    {
        IOperationResult<int> result = OperationResult.Success(5);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
    }

    [Fact, Priority(21)]
    public void OperationResult_StaticHelper_SuccessVoid_ShouldWork()
    {
        IOperationResult<object> result = OperationResult.Success();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact, Priority(22)]
    public void OperationResult_StaticHelper_FailureString_ShouldWork()
    {
        IOperationResult<object> result = OperationResult.Failure("generic error");

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("generic error");
    }

    [Fact, Priority(23)]
    public void OperationResult_StaticHelper_FailureException_ShouldWork()
    {
        IOperationResult<int> result = OperationResult.Failure<int>(new Exception("ex msg"));

        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("ex msg");
    }
}
