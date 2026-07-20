//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Contract;
using Mvp24Hours.Infrastructure.BackgroundJobs.Models;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs.Models;

[Trait("Category", "Unit")]
public class ParentChildJobTest
{
    [Fact]
    public void ParentJob_Constructor_WithNullParentJobId_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new ParentJob(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("parentJobId");
    }

    [Fact]
    public void ParentJob_AddChild_ShouldTrackUniqueChildIds()
    {
        var options = new ParentChildJobOptions { WaitForChildren = false };
        var parent = new ParentJob("parent-1", options);

        parent.AddChild("child-1");
        parent.AddChild("child-1");
        parent.AddChild("child-2");

        parent.ParentJobId.Should().Be("parent-1");
        parent.Options.WaitForChildren.Should().BeFalse();
        parent.ChildJobIds.Should().BeEquivalentTo(["child-1", "child-2"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParentJob_AddChild_WithEmptyChildJobId_ShouldThrowArgumentException(string? childJobId)
    {
        var parent = new ParentJob("parent-1");

        Action act = () => parent.AddChild(childJobId!);

        act.Should().Throw<ArgumentException>().WithParameterName("childJobId");
    }

    [Fact]
    public void ParentJob_RemoveChild_ShouldRemoveExistingChild()
    {
        var parent = new ParentJob("parent-1");
        parent.AddChild("child-1");

        bool removed = parent.RemoveChild("child-1");

        removed.Should().BeTrue();
        parent.ChildJobIds.Should().BeEmpty();
    }

    [Fact]
    public void ParentJob_ClearChildren_ShouldRemoveAllChildren()
    {
        var parent = new ParentJob("parent-1");
        parent.AddChild("child-1");
        parent.AddChild("child-2");

        parent.ClearChildren();

        parent.ChildJobIds.Should().BeEmpty();
    }

    [Fact]
    public void ChildJob_Constructor_ShouldSetProperties()
    {
        var child = new ChildJob("parent-1", "child-1", 2, ["sibling-1"]);

        child.ParentJobId.Should().Be("parent-1");
        child.ChildJobId.Should().Be("child-1");
        child.ExecutionOrder.Should().Be(2);
        child.SiblingDependencies.Should().ContainSingle().Which.Should().Be("sibling-1");
    }

    [Fact]
    public void ChildJob_Constructor_WithNullParentJobId_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new ChildJob(null!, "child-1");

        act.Should().Throw<ArgumentNullException>().WithParameterName("parentJobId");
    }

    [Fact]
    public void ChildJob_Constructor_WithNullChildJobId_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new ChildJob("parent-1", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("childJobId");
    }
}
