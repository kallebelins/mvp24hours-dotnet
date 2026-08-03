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
    public void AssertActivityFromSource_ShouldPassWhenSourceMatches()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("OpFromSource")) { }

        Action act = () => ActivityAssertions.AssertActivityFromSource(listener, "ActivityAssertions.Test");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityFromSource_ShouldThrowWhenSourceMissing()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("Op")) { }

        Action act = () => ActivityAssertions.AssertActivityFromSource(listener, "Other.Source");

        act.Should().Throw<AssertionException>().WithMessage("*Other.Source*");
    }

    [Fact]
    public void AssertActivityCount_ShouldPassWhenCountMatches()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("RepeatedOp")) { }
        using (TestSource.StartActivity("RepeatedOp")) { }

        Action act = () => ActivityAssertions.AssertActivityCount(listener, "RepeatedOp", 2);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityCount_ShouldThrowWhenCountMismatch()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("RepeatedOp")) { }

        Action act = () => ActivityAssertions.AssertActivityCount(listener, "RepeatedOp", 3);

        act.Should().Throw<AssertionException>().WithMessage("*Expected 3 activity*");
    }

    [Fact]
    public void AssertNoErrorActivities_ShouldPassWhenNoErrors()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("SuccessOp")) { }

        Action act = () => ActivityAssertions.AssertNoErrorActivities(listener);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoErrorActivities_ShouldThrowWhenErrorActivityExists()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? activity = TestSource.StartActivity("FailedOp"))
        {
            activity!.SetStatus(ActivityStatusCode.Error, "Something failed");
        }

        Action act = () => ActivityAssertions.AssertNoErrorActivities(listener);

        act.Should().Throw<AssertionException>().WithMessage("*error activities*");
    }

    [Fact]
    public void AssertActivityHasEvent_ShouldPassWhenEventExists()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? activity = TestSource.StartActivity("EventOp"))
        {
            activity!.AddEvent(new ActivityEvent("checkpoint"));
        }

        Action act = () => ActivityAssertions.AssertActivityHasEvent(listener, "EventOp", "checkpoint");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityHasEvent_ShouldThrowWhenEventMissing()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("NoEvents")) { }

        Action act = () => ActivityAssertions.AssertActivityHasEvent(listener, "NoEvents", "checkpoint");

        act.Should().Throw<AssertionException>().WithMessage("*does not have event 'checkpoint'*");
    }

    [Fact]
    public void AssertActivityKind_ShouldPassWhenKindMatches()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("ServerOp", ActivityKind.Server)) { }

        Action act = () => ActivityAssertions.AssertActivityKind(listener, "ServerOp", ActivityKind.Server);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityKind_ShouldThrowWhenKindMismatch()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("ClientOp", ActivityKind.Client)) { }

        Action act = () => ActivityAssertions.AssertActivityKind(listener, "ClientOp", ActivityKind.Server);

        act.Should().Throw<AssertionException>().WithMessage("*expected kind 'Server'*");
    }

    [Fact]
    public void AssertActivityDurationLessThan_ShouldPassWhenWithinLimit()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("FastOp")) { }

        Action act = () => ActivityAssertions.AssertActivityDurationLessThan(listener, "FastOp", TimeSpan.FromSeconds(5));

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertActivityDurationLessThan_ShouldThrowWhenExceedsLimit()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? activity = TestSource.StartActivity("SlowOp"))
        {
            activity!.SetEndTime(activity.StartTimeUtc.AddSeconds(2));
        }

        Action act = () => ActivityAssertions.AssertActivityDurationLessThan(listener, "SlowOp", TimeSpan.FromMilliseconds(100));

        act.Should().Throw<AssertionException>().WithMessage("*exceeded maximum*");
    }

    [Fact]
    public void AssertParentChildRelationship_ShouldPassWhenChildReferencesParent()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? parent = TestSource.StartActivity("ParentOp"))
        {
            using (TestSource.StartActivity("ChildOp")) { }
        }

        Action act = () => ActivityAssertions.AssertParentChildRelationship(listener, "ParentOp", "ChildOp");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertParentChildRelationship_ShouldThrowWhenParentMissing()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("ChildOnly")) { }

        Action act = () => ActivityAssertions.AssertParentChildRelationship(listener, "MissingParent", "ChildOnly");

        act.Should().Throw<AssertionException>().WithMessage("*Parent activity 'MissingParent' not found*");
    }

    [Fact]
    public void AssertParentChildRelationship_ShouldThrowWhenNotRelated()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (TestSource.StartActivity("ParentA")) { }
        using (TestSource.StartActivity("ChildB")) { }

        Action act = () => ActivityAssertions.AssertParentChildRelationship(listener, "ParentA", "ChildB");

        act.Should().Throw<AssertionException>().WithMessage("*is not a child of*");
    }

    [Fact]
    public void GetActivity_ShouldReturnMatchingActivity()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");
        using (Activity? activity = TestSource.StartActivity("LookupOp"))
        {
            activity!.SetTag("key", "value");
        }

        RecordedActivity recorded = ActivityAssertions.GetActivity(listener, "LookupOp");

        recorded.OperationName.Should().Be("LookupOp");
    }

    [Fact]
    public void GetActivity_ShouldThrowWhenOperationMissing()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");

        Action act = () => ActivityAssertions.GetActivity(listener, "MissingOp");

        act.Should().Throw<AssertionException>().WithMessage("*No activity found*");
    }

    [Fact]
    public void AssertActivityRecorded_WithNullListener_ShouldThrowArgumentNullException()
    {
        Action act = () => ActivityAssertions.AssertActivityRecorded(null!, "Op");

        act.Should().Throw<ArgumentNullException>().WithParameterName("listener");
    }

    [Fact]
    public void AssertActivityRecorded_WithNullOperationName_ShouldThrowArgumentNullException()
    {
        using FakeActivityListener listener = new("ActivityAssertions.*");

        Action act = () => ActivityAssertions.AssertActivityRecorded(listener, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("operationName");
    }
}
