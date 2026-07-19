//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Pipe.Context;
using Xunit.Priority;

namespace Mvp24Hours.Application.Pipe.Test.Context;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class PipelineContextTest
{
    [Fact, Priority(1)]
    public void PipelineContext_DefaultConstructor_ShouldGenerateCorrelationId()
    {
        var ctx = new PipelineContext();

        ctx.CorrelationId.Should().NotBeNullOrEmpty();
        ctx.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact, Priority(2)]
    public void PipelineContext_WithCorrelationId_ShouldUseProvidedId()
    {
        const string id = "corr-123";
        var ctx = new PipelineContext(id);

        ctx.CorrelationId.Should().Be(id);
    }

    [Fact, Priority(3)]
    public void PipelineContext_EmptyCorrelationId_ShouldThrow()
    {
        Action act = () => _ = new PipelineContext(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact, Priority(4)]
    public void PipelineContext_SetMetadata_ShouldStoreValue()
    {
        var ctx = new PipelineContext();

        ctx.SetMetadata("key1", 42);

        ctx.GetMetadata<int>("key1").Should().Be(42);
        ctx.HasMetadata("key1").Should().BeTrue();
    }

    [Fact, Priority(5)]
    public void PipelineContext_GetMetadata_NonExistentKey_ShouldReturnDefault()
    {
        var ctx = new PipelineContext();

        int? result = ctx.GetMetadata<int?>("nonexistent");

        result.Should().BeNull();
    }

    [Fact, Priority(6)]
    public void PipelineContext_SetMetadata_NullValue_ShouldRemoveKey()
    {
        var ctx = new PipelineContext();
        ctx.SetMetadata("key", "value");

        ctx.SetMetadata<string?>("key", null);

        ctx.HasMetadata("key").Should().BeFalse();
    }

    [Fact, Priority(7)]
    public void PipelineContext_RemoveMetadata_ShouldRemoveKey()
    {
        var ctx = new PipelineContext();
        ctx.SetMetadata("key", "val");

        bool removed = ctx.RemoveMetadata("key");

        removed.Should().BeTrue();
        ctx.HasMetadata("key").Should().BeFalse();
    }

    [Fact, Priority(8)]
    public void PipelineContext_RemoveMetadata_NonExistentKey_ShouldReturnFalse()
    {
        var ctx = new PipelineContext();

        bool removed = ctx.RemoveMetadata("ghost");

        removed.Should().BeFalse();
    }

    [Fact, Priority(9)]
    public void PipelineContext_HasMetadata_EmptyKey_ShouldReturnFalse()
    {
        var ctx = new PipelineContext();

        ctx.HasMetadata(string.Empty).Should().BeFalse();
    }

    [Fact, Priority(10)]
    public void PipelineContext_SetMetadata_EmptyKey_ShouldThrow()
    {
        var ctx = new PipelineContext();

        Action act = () => ctx.SetMetadata(string.Empty, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact, Priority(11)]
    public void PipelineContext_Metadata_ShouldBeReadOnlyDictionary()
    {
        var ctx = new PipelineContext();
        ctx.SetMetadata("a", 1);
        ctx.SetMetadata("b", 2);

        ctx.Metadata.Should().HaveCount(2);
        ctx.Metadata.Should().ContainKey("a");
    }

    [Fact, Priority(12)]
    public void PipelineContext_UserContext_ShouldSetAndRead()
    {
        var ctx = new PipelineContext
        {
            UserId = "user-1",
            UserName = "Alice",
            TenantId = "tenant-99"
        };

        ctx.UserId.Should().Be("user-1");
        ctx.UserName.Should().Be("Alice");
        ctx.TenantId.Should().Be("tenant-99");
        ctx.HasUser.Should().BeTrue();
    }

    [Fact, Priority(13)]
    public void PipelineContext_HasUser_WhenNoUserId_ShouldBeFalse()
    {
        var ctx = new PipelineContext();

        ctx.HasUser.Should().BeFalse();
    }

    [Fact, Priority(14)]
    public void PipelineContext_CaptureSnapshot_ShouldAddToList()
    {
        var ctx = new PipelineContext();

        ctx.CaptureSnapshot("OpA", new { value = 1 }, "desc A");

        ctx.Snapshots.Should().HaveCount(1);
        ctx.Snapshots[0].OperationName.Should().Be("OpA");
        ctx.Snapshots[0].Description.Should().Be("desc A");
        ctx.LastSnapshot.Should().NotBeNull();
    }

    [Fact, Priority(15)]
    public void PipelineContext_CaptureMultipleSnapshots_ShouldBeOrdered()
    {
        var ctx = new PipelineContext();

        ctx.CaptureSnapshot("Op1", null);
        ctx.CaptureSnapshot("Op2", null);

        ctx.Snapshots.Should().HaveCount(2);
        ctx.Snapshots[0].SequenceNumber.Should().BeLessThan(ctx.Snapshots[1].SequenceNumber);
        ctx.LastSnapshot!.OperationName.Should().Be("Op2");
    }

    [Fact, Priority(16)]
    public void PipelineContext_ClearSnapshots_ShouldRemoveAll()
    {
        var ctx = new PipelineContext();
        ctx.CaptureSnapshot("Op1", null);
        ctx.CaptureSnapshot("Op2", null);

        ctx.ClearSnapshots();

        ctx.Snapshots.Should().BeEmpty();
        ctx.LastSnapshot.Should().BeNull();
    }

    [Fact, Priority(17)]
    public void PipelineContext_CaptureSnapshot_EmptyName_ShouldThrow()
    {
        var ctx = new PipelineContext();

        Action act = () => ctx.CaptureSnapshot(string.Empty, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact, Priority(18)]
    public void PipelineContext_CreateChildContext_ShouldInheritProperties()
    {
        var parent = new PipelineContext("parent-corr")
        {
            UserId = "user-1",
            UserName = "Alice",
            TenantId = "tenant-1"
        };
        parent.SetMetadata("key", "value");

        var child = parent.CreateChildContext();

        child.CorrelationId.Should().NotBe("parent-corr");
        child.CausationId.Should().Be("parent-corr");
        child.ParentContextId.Should().Be("parent-corr");
        child.UserId.Should().Be("user-1");
        child.TenantId.Should().Be("tenant-1");
        child.GetMetadata<string>("key").Should().Be("value");
    }

    [Fact, Priority(19)]
    public void PipelineContext_CloneWithCorrelationId_ShouldCreateNewContextWithSameData()
    {
        var original = new PipelineContext("orig-corr")
        {
            UserId = "user-2",
            CausationId = "caus-1"
        };
        original.SetMetadata("x", 10);

        var cloned = original.CloneWithCorrelationId("new-corr");

        cloned.CorrelationId.Should().Be("new-corr");
        cloned.UserId.Should().Be("user-2");
        cloned.CausationId.Should().Be("caus-1");
        cloned.GetMetadata<int>("x").Should().Be(10);
    }

    [Fact, Priority(20)]
    public void PipelineContext_WithUser_ShouldCreateContextWithUser()
    {
        var ctx = PipelineContext.WithUser("u1", "Bob", "t1");

        ctx.UserId.Should().Be("u1");
        ctx.UserName.Should().Be("Bob");
        ctx.TenantId.Should().Be("t1");
        ctx.HasUser.Should().BeTrue();
    }

    [Fact, Priority(21)]
    public void PipelineContext_ForTenant_ShouldCreateContextForTenant()
    {
        var ctx = PipelineContext.ForTenant("tenant-42");

        ctx.TenantId.Should().Be("tenant-42");
    }

    [Fact, Priority(22)]
    public void PipelineContext_SnapshotContainsMetadataCopy()
    {
        var ctx = new PipelineContext();
        ctx.SetMetadata("snap-key", "snap-val");

        ctx.CaptureSnapshot("Op", null);

        ctx.Snapshots[0].Metadata.Should().NotBeNull();
        ctx.Snapshots[0].Metadata!.Should().ContainKey("snap-key");
    }
}
