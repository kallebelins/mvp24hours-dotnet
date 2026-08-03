using Mvp24Hours.Application.Contract.Resilience;
using Mvp24Hours.Application.Extensions;
using Mvp24Hours.Application.Logic.Resilience;

namespace Mvp24Hours.Application.Test.Extensions;

[Trait("Category", "Unit")]
public class BusinessResultWithStatusExtensionsTest
{
    [Fact]
    public void Match_OnSuccess_ShouldInvokeOnSuccess()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.Success(10);

        string value = result.Match(
            data => $"ok:{data}",
            (_, _) => "failed");

        value.Should().Be("ok:10");
    }

    [Fact]
    public void Match_OnFailure_ShouldInvokeOnFailure()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.NotFound<int>("missing");

        string value = result.Match(
            _ => "ok",
            (status, errors) => $"{status}:{errors.Count}");

        value.Should().Be($"{ResultStatusCode.NotFound}:1");
    }

    [Fact]
    public void Match_WithNullResult_ShouldThrow()
    {
        IBusinessResultWithStatus<int>? result = null;

        Action act = () => result!.Match(_ => "ok", (_, _) => "fail");

        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    [Fact]
    public void MatchStatus_ShouldRouteByStatusCode()
    {
        IBusinessResultWithStatus<string> notFound = BusinessResultWithStatus.NotFound<string>("missing");
        IBusinessResultWithStatus<string> validation = BusinessResultWithStatus.ValidationFailed<string>("invalid");
        IBusinessResultWithStatus<string> success = BusinessResultWithStatus.Success("done");

        notFound.MatchStatus(_ => "success", () => "not-found", _ => "validation", (_, _) => "other")
            .Should().Be("not-found");
        validation.MatchStatus(_ => "success", () => "not-found", _ => "validation", (_, _) => "other")
            .Should().Be("validation");
        success.MatchStatus(_ => "success", () => "not-found", _ => "validation", (_, _) => "other")
            .Should().Be("success");
    }

    [Fact]
    public void Map_OnSuccess_ShouldTransformData()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.Success(5);

        IBusinessResultWithStatus<string> mapped = result.Map(v => $"value:{v}");

        mapped.HasErrors.Should().BeFalse();
        mapped.Data.Should().Be("value:5");
    }

    [Fact]
    public void Map_OnFailure_ShouldPreserveStatusAndMessages()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.Failure<int>(
            ResultStatusCode.Conflict,
            "conflict");

        IBusinessResultWithStatus<string> mapped = result.Map(v => v.ToString());

        mapped.HasErrors.Should().BeTrue();
        mapped.StatusCode.Should().Be(ResultStatusCode.Conflict);
        mapped.Data.Should().BeNull();
    }

    [Fact]
    public async Task MapAsync_OnSuccess_ShouldTransformData()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.Success(3);

        IBusinessResultWithStatus<string> mapped = await result.MapAsync(v => Task.FromResult($"n:{v}"));

        mapped.Data.Should().Be("n:3");
    }

    [Fact]
    public void Bind_OnSuccess_ShouldChainResultsAndPreserveToken()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.Success(2, token: "token-1");

        IBusinessResultWithStatus<string> bound = result.Bind(v =>
            BusinessResultWithStatus.Success(v.ToString()));

        bound.Data.Should().Be("2");
        bound.Token.Should().Be("token-1");
    }

    [Fact]
    public void Bind_OnFailure_ShouldShortCircuit()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.NotFound<int>("missing");

        IBusinessResultWithStatus<string> bound = result.Bind(_ =>
            BusinessResultWithStatus.Success("should-not-run"));

        bound.HasErrors.Should().BeTrue();
        bound.StatusCode.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public async Task BindAsync_OnSuccess_ShouldChainResults()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.Success(8, token: "bind-token");

        IBusinessResultWithStatus<double> bound = await result.BindAsync(v =>
            Task.FromResult<IBusinessResultWithStatus<double>>(BusinessResultWithStatus.Success(v * 1.5)));

        bound.Data.Should().Be(12);
        bound.Token.Should().Be("bind-token");
    }

    [Fact]
    public void Tap_OnSuccess_ShouldExecuteSideEffect()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.Success("payload");
        string? captured = null;

        IBusinessResultWithStatus<string> same = result.Tap(v => captured = v);

        captured.Should().Be("payload");
        same.Should().BeSameAs(result);
    }

    [Fact]
    public void TapError_OnFailure_ShouldExecuteSideEffect()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.NotFound<string>("missing");
        ResultStatusCode? capturedStatus = null;

        result.TapError((status, _) => capturedStatus = status);

        capturedStatus.Should().Be(ResultStatusCode.NotFound);
    }

    [Fact]
    public void TapWarning_WithWarnings_ShouldExecuteSideEffect()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.SuccessWithWarning("data", "warn");
        int warningCount = 0;

        result.TapWarning(warnings => warningCount = warnings.Count);

        warningCount.Should().Be(1);
    }

    [Fact]
    public void Ensure_WhenPredicateFails_ShouldReturnFailure()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.Success(0, token: "ensure-token");

        IBusinessResultWithStatus<int> ensured = result.Ensure(
            v => v > 0,
            ResultStatusCode.ValidationFailed,
            "must be positive",
            "VAL.POSITIVE");

        ensured.HasErrors.Should().BeTrue();
        ensured.StatusCode.Should().Be(ResultStatusCode.ValidationFailed);
        ensured.Token.Should().Be("ensure-token");
    }

    [Fact]
    public void Ensure_WhenAlreadyFailed_ShouldReturnOriginalResult()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.NotFound<int>("missing");

        IBusinessResultWithStatus<int> ensured = result.Ensure(v => v > 0, ResultStatusCode.ValidationFailed, "invalid");

        ensured.Should().BeSameAs(result);
    }

    [Fact]
    public void ToBusinessResult_ShouldCopyDataAndMessages()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.Success("data", token: "tok");

        IBusinessResult<string> converted = result.ToBusinessResult();

        converted.Data.Should().Be("data");
        converted.Token.Should().Be("tok");
    }

    [Fact]
    public void GetFirstError_WithErrors_ShouldReturnMessage()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.Failure<string>(
            ResultStatusCode.InternalError,
            [ResultMessage.Error("first error"), ResultMessage.Error("second error")]);

        result.GetFirstError().Should().Be("first error");
    }

    [Fact]
    public void GetFirstWarning_WithWarnings_ShouldReturnMessage()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.SuccessWithWarning("data", "warn-1");

        result.GetFirstWarning().Should().Be("warn-1");
    }

    [Fact]
    public void GetValueOrThrow_OnFailure_ShouldThrowWithDetails()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.NotFound<string>("entity missing");

        Action act = () => result.GetValueOrThrow();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*NotFound*entity missing*");
    }

    [Fact]
    public void GetValueOrDefault_OnFailure_ShouldReturnDefault()
    {
        IBusinessResultWithStatus<int> result = BusinessResultWithStatus.NotFound<int>("missing");

        result.GetValueOrDefault(99).Should().Be(99);
    }

    [Fact]
    public void OrElse_OnFailure_ShouldInvokeFallback()
    {
        IBusinessResultWithStatus<string> failed = BusinessResultWithStatus.NotFound<string>("missing");
        IBusinessResultWithStatus<string> fallback = BusinessResultWithStatus.Success("fallback");

        IBusinessResultWithStatus<string> result = failed.OrElse(() => fallback);

        result.Data.Should().Be("fallback");
    }

    [Fact]
    public async Task OrElseAsync_OnFailure_ShouldInvokeFallback()
    {
        IBusinessResultWithStatus<string> failed = BusinessResultWithStatus.NotFound<string>("missing");
        IBusinessResultWithStatus<string> fallback = BusinessResultWithStatus.Success("async-fallback");

        IBusinessResultWithStatus<string> result = await failed.OrElseAsync(() => Task.FromResult(fallback));

        result.Data.Should().Be("async-fallback");
    }

    [Fact]
    public void Map_WithNullResult_ShouldThrow()
    {
        IBusinessResultWithStatus<int>? result = null;

        Action act = () => result!.Map(v => v.ToString());

        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    [Fact]
    public void Bind_WithNullResult_ShouldThrow()
    {
        IBusinessResultWithStatus<int>? result = null;

        Action act = () => result!.Bind(v => BusinessResultWithStatus.Success(v.ToString()));

        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    [Fact]
    public void Tap_WithNullResult_ShouldThrow()
    {
        IBusinessResultWithStatus<string>? result = null;

        Action act = () => result!.Tap(_ => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    [Fact]
    public void TapError_WithNullResult_ShouldThrow()
    {
        IBusinessResultWithStatus<string>? result = null;

        Action act = () => result!.TapError((_, _) => { });

        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    [Fact]
    public void Ensure_WithNullResult_ShouldThrow()
    {
        IBusinessResultWithStatus<int>? result = null;

        Action act = () => result!.Ensure(v => v > 0, ResultStatusCode.ValidationFailed, "invalid");

        act.Should().Throw<ArgumentNullException>().WithParameterName("result");
    }

    [Fact]
    public void GetValueOrThrow_OnSuccess_ShouldReturnData()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.Success("value");

        result.GetValueOrThrow().Should().Be("value");
    }

    [Fact]
    public void OrElse_OnSuccess_ShouldReturnOriginalResult()
    {
        IBusinessResultWithStatus<string> success = BusinessResultWithStatus.Success("original");

        IBusinessResultWithStatus<string> result = success.OrElse(() => BusinessResultWithStatus.Success("fallback"));

        result.Should().BeSameAs(success);
    }

    [Fact]
    public void GetFirstError_WithoutErrors_ShouldReturnNull()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.Success("ok");

        result.GetFirstError().Should().BeNull();
    }

    [Fact]
    public void GetFirstWarning_WithoutWarnings_ShouldReturnNull()
    {
        IBusinessResultWithStatus<string> result = BusinessResultWithStatus.Success("ok");

        result.GetFirstWarning().Should().BeNull();
    }

    [Fact]
    public void MatchStatus_WithOtherFailure_ShouldInvokeOnOtherFailure()
    {
        IBusinessResultWithStatus<string> conflict = BusinessResultWithStatus.Failure<string>(
            ResultStatusCode.Conflict,
            "conflict");

        conflict.MatchStatus(_ => "success", () => "not-found", _ => "validation", (_, _) => "other")
            .Should().Be("other");
    }
}
