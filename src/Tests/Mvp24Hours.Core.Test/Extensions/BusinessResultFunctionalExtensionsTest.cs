using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class BusinessResultFunctionalExtensionsTest
{
    [Fact]
    public void Match_WithSuccess_ShouldInvokeOnSuccess()
    {
        IBusinessResult<int> result = BusinessResult.Success(42);

        string message = result.Match(
            onSuccess: value => $"ok:{value}",
            onFailure: _ => "fail");

        message.Should().Be("ok:42");
    }

    [Fact]
    public void Match_WithFailure_ShouldInvokeOnFailure()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("boom");

        string message = result.Match(
            onSuccess: _ => "ok",
            onFailure: errors => errors.Single().Message);

        message.Should().Be("boom");
    }

    [Fact]
    public void Match_WithNullResult_ShouldThrow()
    {
        IBusinessResult<int>? result = null;

        Action act = () => result!.Match(_ => "ok", _ => "fail");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MatchAction_WithSuccess_ShouldExecuteOnSuccessAndReturnOriginal()
    {
        IBusinessResult<string> result = BusinessResult.Success("data");
        bool executed = false;

        IBusinessResult<string> returned = result.Match(
            onSuccess: value =>
            {
                value.Should().Be("data");
                executed = true;
            },
            onFailure: _ => Assert.Fail("Should not run on failure"));

        executed.Should().BeTrue();
        returned.Should().BeSameAs(result);
    }

    [Fact]
    public void MatchAction_WithFailure_ShouldExecuteOnFailure()
    {
        IBusinessResult<string> result = BusinessResult.Failure<string>("error");
        bool executed = false;

        result.Match(
            onSuccess: _ => Assert.Fail("Should not run on success"),
            onFailure: errors =>
            {
                errors.Should().ContainSingle();
                executed = true;
            });

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task MatchAsync_WithSuccess_ShouldAwaitOnSuccess()
    {
        IBusinessResult<int> result = BusinessResult.Success(7);

        string message = await result.MatchAsync(
            onSuccess: async value =>
            {
                await Task.Yield();
                return $"ok:{value}";
            },
            onFailure: async _ =>
            {
                await Task.Yield();
                return "fail";
            });

        message.Should().Be("ok:7");
    }

    [Fact]
    public async Task MatchAsync_WithFailure_ShouldAwaitOnFailure()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("bad");

        string message = await result.MatchAsync(
            onSuccess: async _ =>
            {
                await Task.Yield();
                return "ok";
            },
            onFailure: async errors =>
            {
                await Task.Yield();
                return errors.Single().Message;
            });

        message.Should().Be("bad");
    }

    [Fact]
    public void Map_WithSuccess_ShouldTransformData()
    {
        IBusinessResult<int> result = BusinessResult.Success(10, token: "tok");

        IBusinessResult<string> mapped = result.Map(value => $"#{value}");

        mapped.Data.Should().Be("#10");
        mapped.HasErrors.Should().BeFalse();
        mapped.Token.Should().Be("tok");
    }

    [Fact]
    public void Map_WithFailure_ShouldPassThroughErrors()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("err", token: "tok");

        IBusinessResult<string> mapped = result.Map(_ => "ignored");

        mapped.Data.Should().BeNull();
        mapped.HasErrors.Should().BeTrue();
        mapped.Messages!.Single().Message.Should().Be("err");
        mapped.Token.Should().Be("tok");
    }

    [Fact]
    public async Task MapAsync_WithSuccess_ShouldAwaitMapper()
    {
        IBusinessResult<int> result = BusinessResult.Success(3);

        IBusinessResult<string> mapped = await result.MapAsync(async value =>
        {
            await Task.Yield();
            return value.ToString();
        });

        mapped.Data.Should().Be("3");
    }

    [Fact]
    public async Task MapAsync_OnTaskResult_ShouldAwaitAndMap()
    {
        Task<IBusinessResult<int>> resultTask = Task.FromResult<IBusinessResult<int>>(BusinessResult.Success(9));

        IBusinessResult<string> mapped = await resultTask.MapAsync(value => $"v{value}");

        mapped.Data.Should().Be("v9");
    }

    [Fact]
    public void Bind_WithSuccess_ShouldExecuteBinder()
    {
        IBusinessResult<int> result = BusinessResult.Success(1, token: "parent");

        IBusinessResult<string> bound = result.Bind(value => BusinessResult.Success(value.ToString()));

        bound.Data.Should().Be("1");
        bound.Token.Should().Be("parent");
    }

    [Fact]
    public void Bind_WithFailure_ShouldSkipBinder()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("fail", token: "parent");
        bool binderCalled = false;

        IBusinessResult<string> bound = result.Bind(_ =>
        {
            binderCalled = true;
            return BusinessResult.Success("x");
        });

        binderCalled.Should().BeFalse();
        bound.HasErrors.Should().BeTrue();
        bound.Token.Should().Be("parent");
    }

    [Fact]
    public void Bind_ShouldPreserveParentTokenWhenChildHasNone()
    {
        IBusinessResult<int> result = BusinessResult.Success(1, token: "parent-token");

        IBusinessResult<string> bound = result.Bind(_ => BusinessResult.Success("child"));

        bound.Token.Should().Be("parent-token");
    }

    [Fact]
    public async Task BindAsync_WithSuccess_ShouldAwaitBinder()
    {
        IBusinessResult<int> result = BusinessResult.Success(5, token: "tok");

        IBusinessResult<string> bound = await result.BindAsync(async value =>
        {
            await Task.Yield();
            return BusinessResult.Success(value.ToString(), token: null);
        });

        bound.Data.Should().Be("5");
        bound.Token.Should().Be("tok");
    }

    [Fact]
    public async Task BindAsync_OnTaskResult_ShouldAwaitAndBind()
    {
        Task<IBusinessResult<int>> resultTask = Task.FromResult<IBusinessResult<int>>(BusinessResult.Success(2));

        IBusinessResult<string> bound = await resultTask.BindAsync(value => BusinessResult.Success(value.ToString()));

        bound.Data.Should().Be("2");
    }

    [Fact]
    public async Task BindAsync_OnTaskResultWithAsyncBinder_ShouldAwaitBoth()
    {
        Task<IBusinessResult<int>> resultTask = Task.FromResult<IBusinessResult<int>>(BusinessResult.Success(4));

        IBusinessResult<string> bound = await resultTask.BindAsync(async value =>
        {
            await Task.Yield();
            return BusinessResult.Success(value.ToString());
        });

        bound.Data.Should().Be("4");
    }

    [Fact]
    public void Tap_WithSuccess_ShouldExecuteSideEffect()
    {
        IBusinessResult<int> result = BusinessResult.Success(99);
        int tapped = 0;

        IBusinessResult<int> returned = result.Tap(value => tapped = value);

        tapped.Should().Be(99);
        returned.Should().BeSameAs(result);
    }

    [Fact]
    public void Tap_WithFailure_ShouldNotExecuteSideEffect()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("err");
        bool tapped = false;

        result.Tap(_ => tapped = true);

        tapped.Should().BeFalse();
    }

    [Fact]
    public void TapError_WithFailure_ShouldExecuteSideEffect()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("err");
        bool tapped = false;

        result.TapError(errors =>
        {
            errors.Should().ContainSingle();
            tapped = true;
        });

        tapped.Should().BeTrue();
    }

    [Fact]
    public void TapError_WithSuccess_ShouldNotExecuteSideEffect()
    {
        IBusinessResult<int> result = BusinessResult.Success(1);
        bool tapped = false;

        result.TapError(_ => tapped = true);

        tapped.Should().BeFalse();
    }

    [Fact]
    public async Task TapAsync_WithSuccess_ShouldAwaitSideEffect()
    {
        IBusinessResult<int> result = BusinessResult.Success(8);
        int tapped = 0;

        IBusinessResult<int> returned = await result.TapAsync(async value =>
        {
            await Task.Yield();
            tapped = value;
        });

        tapped.Should().Be(8);
        returned.Should().BeSameAs(result);
    }

    [Fact]
    public void IsSuccess_AndIsFailure_ShouldReflectResultState()
    {
        BusinessResult.Success(1).IsSuccess().Should().BeTrue();
        BusinessResult.Failure<int>("x").IsSuccess().Should().BeFalse();
        IBusinessResult<int>? nullSuccessResult = null;
        nullSuccessResult!.IsSuccess().Should().BeFalse();

        BusinessResult.Success(1).IsFailure().Should().BeFalse();
        BusinessResult.Failure<int>("x").IsFailure().Should().BeTrue();
        IBusinessResult<int>? nullFailureResult = null;
        nullFailureResult!.IsFailure().Should().BeTrue();
    }

    [Fact]
    public void GetValueOrDefault_ShouldReturnDataOrDefault()
    {
        BusinessResult.Success(12).GetValueOrDefault(0).Should().Be(12);
        BusinessResult.Failure<int>("x").GetValueOrDefault(0).Should().Be(0);
        BusinessResult.Failure<int>("x").GetValueOrDefault().Should().Be(0);
    }

    [Fact]
    public void GetValueOrThrow_WithSuccess_ShouldReturnData()
    {
        BusinessResult.Success("ok").GetValueOrThrow().Should().Be("ok");
    }

    [Fact]
    public void GetValueOrThrow_WithFailure_ShouldThrowWithMessages()
    {
        IBusinessResult<string> result = BusinessResult.Failure<string>("first", "KEY1");

        Action act = () => result.GetValueOrThrow();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*first*");
    }

    [Fact]
    public void OrElse_WithSuccess_ShouldReturnOriginal()
    {
        IBusinessResult<int> result = BusinessResult.Success(1);

        IBusinessResult<int> final = result.OrElse(() => BusinessResult.Success(99));

        final.Should().BeSameAs(result);
    }

    [Fact]
    public void OrElse_WithFailure_ShouldInvokeFallback()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("fail");

        IBusinessResult<int> final = result.OrElse(() => BusinessResult.Success(99));

        final.Data.Should().Be(99);
    }

    [Fact]
    public async Task OrElseAsync_WithFailure_ShouldAwaitFallback()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("fail");

        IBusinessResult<int> final = await result.OrElseAsync(async () =>
        {
            await Task.Yield();
            return BusinessResult.Success(77);
        });

        final.Data.Should().Be(77);
    }

    [Fact]
    public void Ensure_WhenPredicateFails_ShouldReturnFailure()
    {
        IBusinessResult<int> result = BusinessResult.Success(5, token: "tok");

        IBusinessResult<int> ensured = result.Ensure(
            value => value > 10,
            "too small",
            "SIZE");

        ensured.HasErrors.Should().BeTrue();
        ensured.Messages!.Single().Message.Should().Be("too small");
        ensured.Token.Should().Be("tok");
    }

    [Fact]
    public void Ensure_WhenAlreadyFailed_ShouldReturnOriginalFailure()
    {
        IBusinessResult<int> result = BusinessResult.Failure<int>("existing");

        IBusinessResult<int> ensured = result.Ensure(_ => false, "ignored");

        ensured.Should().BeSameAs(result);
    }

    [Fact]
    public void Ensure_WhenPredicatePasses_ShouldReturnOriginal()
    {
        IBusinessResult<int> result = BusinessResult.Success(20);

        IBusinessResult<int> ensured = result.Ensure(value => value > 10, "ignored");

        ensured.Should().BeSameAs(result);
    }
}
