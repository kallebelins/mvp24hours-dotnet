//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Mvp24Hours.Extensions;

namespace Mvp24Hours.Infrastructure.Test.Extensions;

[Trait("Category", "Unit")]
public class ActivityExtensionsTest
{
    [Fact]
    public void GetTraceId_WithNullActivity_ShouldReturnEmptyString()
    {
#pragma warning disable CS8600, CS8604 // Intentional null extension receiver
        Activity activity = null;
        activity.GetTraceId().Should().BeEmpty();
#pragma warning restore CS8600, CS8604
    }

    [Fact]
    public void GetSpanId_WithNullActivity_ShouldReturnEmptyString()
    {
#pragma warning disable CS8600, CS8604 // Intentional null extension receiver
        Activity activity = null;
        activity.GetSpanId().Should().BeEmpty();
#pragma warning restore CS8600, CS8604
    }

    [Fact]
    public void GetParentId_WithNullActivity_ShouldReturnEmptyString()
    {
#pragma warning disable CS8600, CS8604 // Intentional null extension receiver
        Activity activity = null;
        activity.GetParentId().Should().BeEmpty();
#pragma warning restore CS8600, CS8604
    }

    [Fact]
    public void GetTraceId_WithW3CActivity_ShouldReturnTraceId()
    {
        ActivityIdFormat previousFormat = Activity.DefaultIdFormat;
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        try
        {
            using var activity = new Activity("unit-test");
            activity.Start();

            activity.GetTraceId().Should().Be(activity.TraceId.ToHexString());
            activity.GetSpanId().Should().Be(activity.SpanId.ToHexString());
        }
        finally
        {
            Activity.DefaultIdFormat = previousFormat;
        }
    }

    [Fact]
    public void GetParentId_WithW3CChildActivity_ShouldReturnParentSpanId()
    {
        ActivityIdFormat previousFormat = Activity.DefaultIdFormat;
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;

        try
        {
            using var parent = new Activity("parent");
            parent.Start();

            using var child = new Activity("child");
            child.SetParentId(parent.Id!);
            child.Start();

            child.GetParentId().Should().Be(parent.SpanId.ToHexString());
        }
        finally
        {
            Activity.DefaultIdFormat = previousFormat;
        }
    }

    [Fact]
    public void GetTraceId_WithHierarchicalActivity_ShouldReturnRootId()
    {
        ActivityIdFormat previousFormat = Activity.DefaultIdFormat;
        Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;

        try
        {
            using var activity = new Activity("hierarchical-test");
            activity.Start();

            activity.GetTraceId().Should().Be(activity.RootId);
            activity.GetSpanId().Should().Be(activity.Id);
        }
        finally
        {
            Activity.DefaultIdFormat = previousFormat;
        }
    }

    [Fact]
    public void GetParentId_WithHierarchicalChildActivity_ShouldReturnParentId()
    {
        ActivityIdFormat previousFormat = Activity.DefaultIdFormat;
        Activity.DefaultIdFormat = ActivityIdFormat.Hierarchical;

        try
        {
            using var parent = new Activity("parent");
            parent.Start();

            using var child = new Activity("child");
            child.SetParentId(parent.Id!);
            child.Start();

            child.GetParentId().Should().Be(parent.Id);
        }
        finally
        {
            Activity.DefaultIdFormat = previousFormat;
        }
    }
}
