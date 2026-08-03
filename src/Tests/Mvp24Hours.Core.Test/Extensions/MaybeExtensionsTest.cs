using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Extensions.Functional;
using Mvp24Hours.Core.ValueObjects.Functional;
using Mvp24Hours.Core.ValueObjects.Logic;

namespace Mvp24Hours.Core.Test.Extensions;

[Trait("Category", "Unit")]
public class MaybeExtensionsTest
{
    [Fact]
    public void ToMaybe_OnValue_Should_CreateSome()
    {
        var maybe = "hello".ToMaybe();

        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be("hello");
    }

    [Fact]
    public void ToMaybe_OnNullableStruct_Should_HandleNull()
    {
        int? value = null;

        var maybe = value.ToMaybe();

        maybe.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void FirstOrNone_Should_ReturnFirstOrNone()
    {
        new int[] { 1, 2, 3 }.FirstOrNone().Value.Should().Be(1);
        Array.Empty<int>().FirstOrNone().HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void FirstOrNone_WithPredicate_Should_Filter()
    {
        Maybe<int> maybe = new[] { 1, 2, 3 }.FirstOrNone(x => x > 1);

        maybe.Value.Should().Be(2);
    }

    [Fact]
    public void SingleOrNone_Should_ThrowWhenMultiple()
    {
        Action act = () => new[] { 1, 2 }.SingleOrNone();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LastOrNone_Should_ReturnLastElement()
    {
        Maybe<int> maybe = new[] { 1, 2, 3 }.LastOrNone();

        maybe.Value.Should().Be(3);
    }

    [Fact]
    public void Values_Should_FilterOutNone()
    {
        IEnumerable<int> values = new Maybe<int>[]
        {
            Maybe<int>.Some(1),
            Maybe<int>.None,
            Maybe<int>.Some(3)
        }.Values();

        values.Should().Equal(1, 3);
    }

    [Fact]
    public void GetValueOrNone_Should_ReadDictionary()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };

        dictionary.GetValueOrNone("a").Value.Should().Be(1);
        dictionary.GetValueOrNone("missing").HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void BusinessResultToMaybe_Should_MapSuccessAndFailure()
    {
        IBusinessResult<string> success = new BusinessResult<string>("ok");
        IBusinessResult<string> failure = new BusinessResult<string>(null, [new MessageResult("e", "fail", Core.Enums.MessageType.Error)]);

        success.ToMaybe().Value.Should().Be("ok");
        failure.ToMaybe().HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void ToBusinessResult_Should_MapMaybe()
    {
        var some = Maybe<string>.Some("value");
        Maybe<string> none = Maybe<string>.None;

        some.ToBusinessResult().HasErrors.Should().BeFalse();
        none.ToBusinessResult("missing").HasErrors.Should().BeTrue();
    }

    [Fact]
    public void EitherConversions_Should_MapLeftAndRight()
    {
        var right = Either<string, int>.Right(7);
        var left = Either<string, int>.Left("err");

        right.ToMaybe().Value.Should().Be(7);
        left.ToMaybe().HasNoValue.Should().BeTrue();
        Maybe<int>.Some(5).ToEither("fallback").IsRight.Should().BeTrue();
        Maybe<int>.None.ToEither(() => "fallback").IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task MapAsync_And_BindAsync_Should_TransformMaybe()
    {
        var some = Maybe<int>.Some(2);
        Maybe<int> none = Maybe<int>.None;

        Maybe<int> mapped = await some.MapAsync(x => Task.FromResult(x * 2));
        Maybe<int> bound = await some.BindAsync(x => Task.FromResult(Maybe<int>.Some(x + 1)));

        mapped.Value.Should().Be(4);
        bound.Value.Should().Be(3);
        (await none.MapAsync(x => Task.FromResult(x))).HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Combine_Should_ReturnTupleWhenAllSome()
    {
        var a = Maybe<int>.Some(1);
        var b = Maybe<int>.Some(2);
        var c = Maybe<int>.Some(3);

        a.Combine(b).Value.Should().Be((1, 2));
        a.Combine(b, c).Value.Should().Be((1, 2, 3));
        a.Combine(Maybe<int>.None).HasNoValue.Should().BeTrue();
    }
}
