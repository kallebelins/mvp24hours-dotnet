//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Observability;
using AssertionException = Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException;

namespace Mvp24Hours.Infrastructure.Test.Testing.Assertions;

[Trait("Category", "Unit")]
public class ActivityAssertionsTest
{
    private static readonly ActivitySource TestSource = new("ActivityAssertions.Test");

    [Fact]
    public void AssertActivityRecorded_ShouldPassWhenOperationExists()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("CreateUser")) { }

        Action act = () => ActivityAssertions.AssertActivityRecorded(listener, "CreateUser");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityRecorded_ShouldThrowWhenOperationMissing()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("OtherOp")) { }

        Action act = () => ActivityAssertions.AssertActivityRecorded(listener, "CreateUser");

        act.Should().Throw<AssertionException>()
            .WithMessage("*CreateUser*");
    }

    [Fact]
    public void AssertNoActivitiesRecorded_ShouldPassWhenEmpty()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");

        Action act = () => ActivityAssertions.AssertNoActivitiesRecorded(listener);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoActivitiesRecorded_ShouldThrowWhenActivitiesExist()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("Unexpected")) { }

        Action act = () => ActivityAssertions.AssertNoActivitiesRecorded(listener);

        act.Should().Throw<AssertionException>()
            .WithMessage("*Expected no activities*");
    }

    [Fact]
    public void AssertActivityHasTag_ShouldPassWhenTagExistsWithExpectedValue()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? activity = TestSource.StartActivity("TaggedOp"))
        {
            activity!.SetTag("tenant.id", "acme");
        }

        Action act = () => ActivityAssertions.AssertActivityHasTag(listener, "TaggedOp", "tenant.id", "acme");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityHasTag_ShouldPassWhenOnlyPresenceIsRequired()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? activity = TestSource.StartActivity("PresenceOp"))
        {
            activity!.SetTag("feature.flag", "enabled");
        }

        Action act = () => ActivityAssertions.AssertActivityHasTag(listener, "PresenceOp", "feature.flag");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityHasTag_ShouldThrowWhenTagValueMismatch()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? activity = TestSource.StartActivity("MismatchOp"))
        {
            activity!.SetTag("env", "dev");
        }

        Action act = () => ActivityAssertions.AssertActivityHasTag(listener, "MismatchOp", "env", "prod");

        act.Should().Throw<AssertionException>()
            .WithMessage("*expected 'prod' but was 'dev'*");
    }

    [Fact]
    public void AssertActivityHasTag_ShouldThrowWhenTagMissing()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("NoTags")) { }

        Action act = () => ActivityAssertions.AssertActivityHasTag(listener, "NoTags", "missing.tag");

        act.Should().Throw<AssertionException>()
            .WithMessage("*does not have tag 'missing.tag'*");
    }

    [Fact]
    public void AssertActivityRecorded_WithNullListener_ShouldThrowArgumentNullException()
    {
        Action act = () => ActivityAssertions.AssertActivityRecorded(null!, "Op");

        act.Should().Throw<ArgumentNullException>().WithParameterName("listener");
    }
}
