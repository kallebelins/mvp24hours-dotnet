//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Test.Testing.Observability;

[Trait("Category", "Unit")]
public class FakeActivityListenerTest
{
    private static readonly ActivitySource MatchingSource = new("TestApp.Module");
    private static readonly ActivitySource OtherSource = new("OtherApp.Module");

    [Fact]
    public void StartAndStopActivity_FromMatchingSource_ShouldBeRecorded()
    {
        using FakeActivityListener listener = new("TestApp.*");

        using (Activity? activity = MatchingSource.StartActivity("ProcessOrder"))
        {
            activity.Should().NotBeNull();
            activity!.SetTag("order.id", "123");
        }

        listener.ActivityCount.Should().Be(1);
        listener.HasActivity("ProcessOrder").Should().BeTrue();
        listener.GetActivities("ProcessOrder").First().GetTag("order.id").Should().Be("123");
    }

    [Fact]
    public void StartAndStopActivity_FromNonMatchingSource_ShouldNotBeRecorded()
    {
        using FakeActivityListener listener = new("TestApp.*");

        using (OtherSource.StartActivity("IgnoredOperation")) { }

        listener.ActivityCount.Should().Be(0);
        listener.HasActivity("IgnoredOperation").Should().BeFalse();
    }

    [Fact]
    public void Clear_ShouldRemoveRecordedActivities()
    {
        using FakeActivityListener listener = new("TestApp.*");
        using (MatchingSource.StartActivity("Temporary")) { }

        listener.Clear();

        listener.ActivityCount.Should().Be(0);
        listener.Activities.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldBeIdempotent()
    {
        FakeActivityListener listener = new("TestApp.*");
        using (MatchingSource.StartActivity("BeforeDispose")) { }

        listener.Dispose();
        Action secondDispose = () => listener.Dispose();

        secondDispose.Should().NotThrow();
    }

    [Fact]
    public void WildcardFilter_ShouldMatchSourcePrefixCaseInsensitively()
    {
        using FakeActivityListener listener = new("testapp.*");
        using ActivitySource lowerSource = new("TestApp.Payments");

        using (lowerSource.StartActivity("ChargeCard")) { }

        listener.HasActivity("ChargeCard").Should().BeTrue();
        listener.HasActivityFromSource("TestApp.Payments").Should().BeTrue();
    }

    [Fact]
    public void ExactSourceFilter_ShouldMatchOnlyExactSourceName()
    {
        using FakeActivityListener listener = new("TestApp.Module");

        using (MatchingSource.StartActivity("Allowed")) { }
        using ActivitySource sibling = new("TestApp.Other");
        using (sibling.StartActivity("Blocked")) { }

        listener.ActivityCount.Should().Be(1);
        listener.HasActivity("Allowed").Should().BeTrue();
        listener.HasActivity("Blocked").Should().BeFalse();
    }

    [Fact]
    public void GetActivitiesFromSource_ShouldReturnFilteredActivities()
    {
        using FakeActivityListener listener = new();
        using ActivitySource sourceA = new("Source.A");
        using ActivitySource sourceB = new("Source.B");

        using (sourceA.StartActivity("OpA1")) { }
        using (sourceA.StartActivity("OpA2")) { }
        using (sourceB.StartActivity("OpB1")) { }

        listener.GetActivitiesFromSource("Source.A").Should().HaveCount(2);
        listener.GetActivitiesFromSource("Source.B").Should().HaveCount(1);
    }

    [Fact]
    public void ActivityRecordedEvent_ShouldFireWhenActivityStops()
    {
        using FakeActivityListener listener = new("TestApp.*");
        RecordedActivity? captured = null;
        listener.ActivityRecorded += (_, activity) => captured = activity;

        using (MatchingSource.StartActivity("EventOp")) { }

        captured.Should().NotBeNull();
        captured!.OperationName.Should().Be("EventOp");
    }
}
