//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.ValueObjects.Functional;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Unit tests for Functional Patterns (Maybe and Either).
/// </summary>
public class FunctionalPatternsTest
{
    #region Maybe<T>.Some Tests

    [Fact]
    public void Maybe_Some_WithValue_CreatesMaybeWithValue()
    {
        // Act
        var maybe = Maybe<int>.Some(42);

        // Assert
        maybe.HasValue.Should().BeTrue();
        maybe.HasNoValue.Should().BeFalse();
        maybe.Value.Should().Be(42);
    }

    [Fact]
    public void Maybe_Some_WithNull_ReturnsNone()
    {
        // Act
        var maybe = Maybe<string>.Some(null!);

        // Assert
        maybe.HasValue.Should().BeFalse();
        maybe.HasNoValue.Should().BeTrue();
    }

    #endregion

    #region Maybe<T>.None Tests

    [Fact]
    public void Maybe_None_HasNoValue()
    {
        // Act
        var maybe = Maybe<int>.None;

        // Assert
        maybe.HasValue.Should().BeFalse();
        maybe.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Maybe_None_AccessingValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var maybe = Maybe<int>.None;

        // Act
        var act = () => maybe.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no value*");
    }

    #endregion

    #region Maybe<T>.From Tests

    [Fact]
    public void Maybe_From_WithValue_ReturnsSome()
    {
        // Act
        var maybe = Maybe<string>.From("test");

        // Assert
        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be("test");
    }

    [Fact]
    public void Maybe_From_WithNull_ReturnsNone()
    {
        // Act
        var maybe = Maybe<string>.From(null!);

        // Assert
        maybe.HasNoValue.Should().BeTrue();
    }

    #endregion

    #region Maybe.ValueOr Tests

    [Fact]
    public void Maybe_ValueOr_WithValue_ReturnsValue()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);

        // Act
        var result = maybe.ValueOr(0);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void Maybe_ValueOr_WithNone_ReturnsDefault()
    {
        // Arrange
        var maybe = Maybe<int>.None;

        // Act
        var result = maybe.ValueOr(100);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void Maybe_ValueOrFactory_WithNone_CallsFactory()
    {
        // Arrange
        var maybe = Maybe<int>.None;
        var factoryCalled = false;

        // Act
        var result = maybe.ValueOr(() =>
        {
            factoryCalled = true;
            return 100;
        });

        // Assert
        result.Should().Be(100);
        factoryCalled.Should().BeTrue();
    }

    [Fact]
    public void Maybe_ValueOrFactory_WithValue_DoesNotCallFactory()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);
        var factoryCalled = false;

        // Act
        var result = maybe.ValueOr(() =>
        {
            factoryCalled = true;
            return 100;
        });

        // Assert
        result.Should().Be(42);
        factoryCalled.Should().BeFalse();
    }

    #endregion

    #region Maybe.Map Tests

    [Fact]
    public void Maybe_Map_WithValue_TransformsValue()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);

        // Act
        var result = maybe.Map(x => x * 2);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(84);
    }

    [Fact]
    public void Maybe_Map_WithNone_ReturnsNone()
    {
        // Arrange
        var maybe = Maybe<int>.None;

        // Act
        var result = maybe.Map(x => x * 2);

        // Assert
        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Maybe_Map_ChainedTransformations_Work()
    {
        // Arrange
        var maybe = Maybe<int>.Some(10);

        // Act
        var result = maybe
            .Map(x => x * 2)
            .Map(x => x + 5)
            .Map(x => x.ToString());

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Should().Be("25");
    }

    #endregion

    #region Maybe.Bind Tests

    [Fact]
    public void Maybe_Bind_WithValue_ReturnsTransformedMaybe()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);

        // Act
        var result = maybe.Bind(x => Maybe<string>.Some($"Value: {x}"));

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Should().Be("Value: 42");
    }

    [Fact]
    public void Maybe_Bind_WithNone_ReturnsNone()
    {
        // Arrange
        var maybe = Maybe<int>.None;

        // Act
        var result = maybe.Bind(x => Maybe<string>.Some($"Value: {x}"));

        // Assert
        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Maybe_Bind_WhenTransformationReturnsNone_ReturnsNone()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);

        // Act
        var result = maybe.Bind(x => Maybe<string>.None);

        // Assert
        result.HasNoValue.Should().BeTrue();
    }

    #endregion

    #region Maybe.Match Tests

    [Fact]
    public void Maybe_Match_WithValue_ExecutesSomeFunction()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);

        // Act
        var result = maybe.Match(
            some: value => $"Found: {value}",
            none: () => "Not found"
        );

        // Assert
        result.Should().Be("Found: 42");
    }

    [Fact]
    public void Maybe_Match_WithNone_ExecutesNoneFunction()
    {
        // Arrange
        var maybe = Maybe<int>.None;

        // Act
        var result = maybe.Match(
            some: value => $"Found: {value}",
            none: () => "Not found"
        );

        // Assert
        result.Should().Be("Not found");
    }

    [Fact]
    public void Maybe_Match_Action_WithValue_ExecutesSomeAction()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);
        var someExecuted = false;
        var noneExecuted = false;

        // Act
        maybe.Match(
            some: _ => someExecuted = true,
            none: () => noneExecuted = true
        );

        // Assert
        someExecuted.Should().BeTrue();
        noneExecuted.Should().BeFalse();
    }

    [Fact]
    public void Maybe_Match_Action_WithNone_ExecutesNoneAction()
    {
        // Arrange
        var maybe = Maybe<int>.None;
        var someExecuted = false;
        var noneExecuted = false;

        // Act
        maybe.Match(
            some: _ => someExecuted = true,
            none: () => noneExecuted = true
        );

        // Assert
        someExecuted.Should().BeFalse();
        noneExecuted.Should().BeTrue();
    }

    #endregion

    #region Maybe.Where Tests

    [Fact]
    public void Maybe_Where_WithMatchingPredicate_ReturnsSame()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);

        // Act
        var result = maybe.Where(x => x > 10);

        // Assert
        result.HasValue.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Maybe_Where_WithNonMatchingPredicate_ReturnsNone()
    {
        // Arrange
        var maybe = Maybe<int>.Some(5);

        // Act
        var result = maybe.Where(x => x > 10);

        // Assert
        result.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Maybe_Where_WithNone_ReturnsNone()
    {
        // Arrange
        var maybe = Maybe<int>.None;

        // Act
        var result = maybe.Where(x => x > 10);

        // Assert
        result.HasNoValue.Should().BeTrue();
    }

    #endregion

    #region Maybe.Tap Tests

    [Fact]
    public void Maybe_Tap_WithValue_ExecutesAction()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);
        var actionExecuted = false;
        var capturedValue = 0;

        // Act
        var result = maybe.Tap(x =>
        {
            actionExecuted = true;
            capturedValue = x;
        });

        // Assert
        actionExecuted.Should().BeTrue();
        capturedValue.Should().Be(42);
        result.Should().Be(maybe);
    }

    [Fact]
    public void Maybe_Tap_WithNone_DoesNotExecuteAction()
    {
        // Arrange
        var maybe = Maybe<int>.None;
        var actionExecuted = false;

        // Act
        var result = maybe.Tap(_ => actionExecuted = true);

        // Assert
        actionExecuted.Should().BeFalse();
        result.HasNoValue.Should().BeTrue();
    }

    #endregion

    #region Maybe Equality Tests

    [Fact]
    public void Maybe_Equality_SameValues_AreEqual()
    {
        // Arrange
        var maybe1 = Maybe<int>.Some(42);
        var maybe2 = Maybe<int>.Some(42);

        // Assert
        maybe1.Should().Be(maybe2);
        (maybe1 == maybe2).Should().BeTrue();
        (maybe1 != maybe2).Should().BeFalse();
    }

    [Fact]
    public void Maybe_Equality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var maybe1 = Maybe<int>.Some(42);
        var maybe2 = Maybe<int>.Some(100);

        // Assert
        maybe1.Should().NotBe(maybe2);
        (maybe1 != maybe2).Should().BeTrue();
    }

    [Fact]
    public void Maybe_Equality_BothNone_AreEqual()
    {
        // Arrange
        var maybe1 = Maybe<int>.None;
        var maybe2 = Maybe<int>.None;

        // Assert
        maybe1.Should().Be(maybe2);
        (maybe1 == maybe2).Should().BeTrue();
    }

    [Fact]
    public void Maybe_Equality_SomeAndNone_AreNotEqual()
    {
        // Arrange
        var some = Maybe<int>.Some(42);
        var none = Maybe<int>.None;

        // Assert
        some.Should().NotBe(none);
        (some != none).Should().BeTrue();
    }

    #endregion

    #region Either.Left Tests

    [Fact]
    public void Either_Left_CreatesLeftEither()
    {
        // Act
        var either = Either<string, int>.Left("Error");

        // Assert
        either.IsLeft.Should().BeTrue();
        either.IsRight.Should().BeFalse();
        either.LeftValue.Should().Be("Error");
    }

    [Fact]
    public void Either_Left_AccessingRightValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var either = Either<string, int>.Left("Error");

        // Act
        var act = () => either.RightValue;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Left*");
    }

    #endregion

    #region Either.Right Tests

    [Fact]
    public void Either_Right_CreatesRightEither()
    {
        // Act
        var either = Either<string, int>.Right(42);

        // Assert
        either.IsRight.Should().BeTrue();
        either.IsLeft.Should().BeFalse();
        either.RightValue.Should().Be(42);
    }

    [Fact]
    public void Either_Right_AccessingLeftValue_ThrowsInvalidOperationException()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var act = () => either.LeftValue;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Right*");
    }

    #endregion

    #region Either.Match Tests

    [Fact]
    public void Either_Match_WithRight_ExecutesRightFunction()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var result = either.Match(
            left: error => $"Error: {error}",
            right: value => $"Success: {value}"
        );

        // Assert
        result.Should().Be("Success: 42");
    }

    [Fact]
    public void Either_Match_WithLeft_ExecutesLeftFunction()
    {
        // Arrange
        var either = Either<string, int>.Left("Something went wrong");

        // Act
        var result = either.Match(
            left: error => $"Error: {error}",
            right: value => $"Success: {value}"
        );

        // Assert
        result.Should().Be("Error: Something went wrong");
    }

    [Fact]
    public void Either_Match_Action_WithRight_ExecutesRightAction()
    {
        // Arrange
        var either = Either<string, int>.Right(42);
        var leftExecuted = false;
        var rightExecuted = false;

        // Act
        either.Match(
            left: _ => leftExecuted = true,
            right: _ => rightExecuted = true
        );

        // Assert
        leftExecuted.Should().BeFalse();
        rightExecuted.Should().BeTrue();
    }

    #endregion

    #region Either.Map Tests

    [Fact]
    public void Either_Map_WithRight_TransformsRightValue()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var result = either.Map(x => x * 2);

        // Assert
        result.IsRight.Should().BeTrue();
        result.RightValue.Should().Be(84);
    }

    [Fact]
    public void Either_Map_WithLeft_PassesThroughLeft()
    {
        // Arrange
        var either = Either<string, int>.Left("Error");

        // Act
        var result = either.Map(x => x * 2);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.LeftValue.Should().Be("Error");
    }

    #endregion

    #region Either.MapLeft Tests

    [Fact]
    public void Either_MapLeft_WithLeft_TransformsLeftValue()
    {
        // Arrange
        var either = Either<string, int>.Left("error");

        // Act
        var result = either.MapLeft(e => e.ToUpperInvariant());

        // Assert
        result.IsLeft.Should().BeTrue();
        result.LeftValue.Should().Be("ERROR");
    }

    [Fact]
    public void Either_MapLeft_WithRight_PassesThroughRight()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var result = either.MapLeft(e => e.ToUpperInvariant());

        // Assert
        result.IsRight.Should().BeTrue();
        result.RightValue.Should().Be(42);
    }

    #endregion

    #region Either.Bind Tests

    [Fact]
    public void Either_Bind_WithRight_AppliesTransformation()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var result = either.Bind(x =>
            x > 0
                ? Either<string, string>.Right($"Positive: {x}")
                : Either<string, string>.Left("Not positive")
        );

        // Assert
        result.IsRight.Should().BeTrue();
        result.RightValue.Should().Be("Positive: 42");
    }

    [Fact]
    public void Either_Bind_WithLeft_BypassesTransformation()
    {
        // Arrange
        var either = Either<string, int>.Left("Original error");

        // Act
        var result = either.Bind(x => Either<string, string>.Right($"Value: {x}"));

        // Assert
        result.IsLeft.Should().BeTrue();
        result.LeftValue.Should().Be("Original error");
    }

    #endregion

    #region Either.RightOr Tests

    [Fact]
    public void Either_RightOr_WithRight_ReturnsRightValue()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var result = either.RightOr(0);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void Either_RightOr_WithLeft_ReturnsDefault()
    {
        // Arrange
        var either = Either<string, int>.Left("Error");

        // Act
        var result = either.RightOr(100);

        // Assert
        result.Should().Be(100);
    }

    #endregion

    #region Either.ToMaybe Tests

    [Fact]
    public void Either_ToMaybe_WithRight_ReturnsSome()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var maybe = either.ToMaybe();

        // Assert
        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be(42);
    }

    [Fact]
    public void Either_ToMaybe_WithLeft_ReturnsNone()
    {
        // Arrange
        var either = Either<string, int>.Left("Error");

        // Act
        var maybe = either.ToMaybe();

        // Assert
        maybe.HasNoValue.Should().BeTrue();
    }

    #endregion

    #region Either Equality Tests

    [Fact]
    public void Either_Equality_SameRightValues_AreEqual()
    {
        // Arrange
        var either1 = Either<string, int>.Right(42);
        var either2 = Either<string, int>.Right(42);

        // Assert
        either1.Should().Be(either2);
        (either1 == either2).Should().BeTrue();
    }

    [Fact]
    public void Either_Equality_SameLeftValues_AreEqual()
    {
        // Arrange
        var either1 = Either<string, int>.Left("Error");
        var either2 = Either<string, int>.Left("Error");

        // Assert
        either1.Should().Be(either2);
        (either1 == either2).Should().BeTrue();
    }

    [Fact]
    public void Either_Equality_LeftAndRight_AreNotEqual()
    {
        // Arrange
        var left = Either<int, int>.Left(42);
        var right = Either<int, int>.Right(42);

        // Assert
        left.Should().NotBe(right);
        (left != right).Should().BeTrue();
    }

    #endregion

    #region Static Helper Tests

    [Fact]
    public void Maybe_StaticSome_CreatesCorrectMaybe()
    {
        // Act
        var maybe = Maybe.Some(42);

        // Assert
        maybe.HasValue.Should().BeTrue();
        maybe.Value.Should().Be(42);
    }

    [Fact]
    public void Maybe_StaticNone_CreatesNone()
    {
        // Act
        var maybe = Maybe.None<int>();

        // Assert
        maybe.HasNoValue.Should().BeTrue();
    }

    [Fact]
    public void Either_StaticLeft_CreatesCorrectEither()
    {
        // Act
        var either = Either.Left<string, int>("Error");

        // Assert
        either.IsLeft.Should().BeTrue();
        either.LeftValue.Should().Be("Error");
    }

    [Fact]
    public void Either_StaticRight_CreatesCorrectEither()
    {
        // Act
        var either = Either.Right<string, int>(42);

        // Assert
        either.IsRight.Should().BeTrue();
        either.RightValue.Should().Be(42);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void Maybe_ToString_WithValue_ReturnsSomeRepresentation()
    {
        // Arrange
        var maybe = Maybe<int>.Some(42);

        // Act
        var result = maybe.ToString();

        // Assert
        result.Should().Be("Some(42)");
    }

    [Fact]
    public void Maybe_ToString_WithNone_ReturnsNoneRepresentation()
    {
        // Arrange
        var maybe = Maybe<int>.None;

        // Act
        var result = maybe.ToString();

        // Assert
        result.Should().Be("None");
    }

    [Fact]
    public void Either_ToString_WithRight_ReturnsRightRepresentation()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var result = either.ToString();

        // Assert
        result.Should().Be("Right(42)");
    }

    [Fact]
    public void Either_ToString_WithLeft_ReturnsLeftRepresentation()
    {
        // Arrange
        var either = Either<string, int>.Left("Error");

        // Act
        var result = either.ToString();

        // Assert
        result.Should().Be("Left(Error)");
    }

    #endregion
}

