//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;

namespace Mvp24Hours.Application.Pipe.Test.Context;

[Trait("Category", "Unit")]
public class PipelineMessageContextExtensionsTest
{
    #region [ GetPipelineContext ]

    [Fact]
    public void GetPipelineContext_WithoutStoredContext_ReturnsNull()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        message.GetPipelineContext().Should().BeNull();
    }

    [Fact]
    public void GetPipelineContext_WithStoredContext_ReturnsSameInstance()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var context = new PipelineContext("corr-1");
        message.SetPipelineContext(context);

        message.GetPipelineContext().Should().BeSameAs(context);
    }

    #endregion

    #region [ GetOrCreatePipelineContext ]

    [Fact]
    public void GetOrCreatePipelineContext_WithNullMessage_Throws()
    {
        Action act = () => ((IPipelineMessage)null!).GetOrCreatePipelineContext();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetOrCreatePipelineContext_WithoutExisting_CreatesAndStoresNewContext()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        IPipelineContext context = message.GetOrCreatePipelineContext();

        context.Should().NotBeNull();
        message.GetPipelineContext().Should().BeSameAs(context);
    }

    [Fact]
    public void GetOrCreatePipelineContext_WithExisting_ReturnsSameInstance()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var context = new PipelineContext("corr-2");
        message.SetPipelineContext(context);

        IPipelineContext result = message.GetOrCreatePipelineContext();

        result.Should().BeSameAs(context);
    }

    #endregion

    #region [ SetPipelineContext ]

    [Fact]
    public void SetPipelineContext_WithNullMessage_Throws()
    {
        Action act = () => ((IPipelineMessage)null!).SetPipelineContext(new PipelineContext());

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region [ GetCorrelationId ]

    [Fact]
    public void GetCorrelationId_WithNullMessage_Throws()
    {
        Action act = () => ((IPipelineMessage)null!).GetCorrelationId();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCorrelationId_WithoutContext_FallsBackToMessageToken()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        message.GetCorrelationId().Should().Be(message.Token);
    }

    [Fact]
    public void GetCorrelationId_WithContext_ReturnsContextCorrelationId()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetPipelineContext(new PipelineContext("ctx-corr"));

        message.GetCorrelationId().Should().Be("ctx-corr");
    }

    #endregion

    #region [ GetUserId / GetTenantId ]

    [Fact]
    public void GetUserId_WithoutContext_ReturnsNull()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        message.GetUserId().Should().BeNull();
    }

    [Fact]
    public void GetUserId_WithContext_ReturnsUserId()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetPipelineContext(new PipelineContext { UserId = "user-1" });

        message.GetUserId().Should().Be("user-1");
    }

    [Fact]
    public void GetTenantId_WithoutContext_ReturnsNull()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        message.GetTenantId().Should().BeNull();
    }

    [Fact]
    public void GetTenantId_WithContext_ReturnsTenantId()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetPipelineContext(new PipelineContext { TenantId = "tenant-1" });

        message.GetTenantId().Should().Be("tenant-1");
    }

    #endregion

    #region [ CaptureSnapshot ]

    [Fact]
    public void CaptureSnapshot_WithoutContext_DoesNotThrow()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Action act = () => message.CaptureSnapshot("op", new { Foo = 1 });

        act.Should().NotThrow();
    }

    [Fact]
    public void CaptureSnapshot_WithContext_AddsSnapshotToContext()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var context = new PipelineContext();
        message.SetPipelineContext(context);

        message.CaptureSnapshot("op-1", new { Foo = 1 }, "description");

        context.Snapshots.Should().ContainSingle();
        context.LastSnapshot!.OperationName.Should().Be("op-1");
        context.LastSnapshot!.Description.Should().Be("description");
    }

    #endregion

    #region [ Context Metadata ]

    [Fact]
    public void SetContextMetadata_WithoutContext_DoesNotThrow()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Action act = () => message.SetContextMetadata("key", "value");

        act.Should().NotThrow();
    }

    [Fact]
    public void SetContextMetadata_AndGetContextMetadata_RoundTripsReferenceType()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetPipelineContext(new PipelineContext());

        message.SetContextMetadata("name", "Alice");

        message.GetContextMetadata<string>("name").Should().Be("Alice");
    }

    [Fact]
    public void GetContextMetadata_WithoutContext_ReturnsNull()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        message.GetContextMetadata<string>("missing").Should().BeNull();
    }

    [Fact]
    public void GetContextMetadataValue_WithoutContext_ReturnsDefault()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        message.GetContextMetadataValue<int>("missing").Should().Be(0);
    }

    [Fact]
    public void SetContextMetadata_AndGetContextMetadataValue_RoundTripsStructType()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetPipelineContext(new PipelineContext());

        message.SetContextMetadata("count", 42);

        message.GetContextMetadataValue<int>("count").Should().Be(42);
    }

    #endregion

    #region [ HasPipelineContext ]

    [Fact]
    public void HasPipelineContext_WithoutContext_ReturnsFalse()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        message.HasPipelineContext().Should().BeFalse();
    }

    [Fact]
    public void HasPipelineContext_WithContext_ReturnsTrue()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetPipelineContext(new PipelineContext());

        message.HasPipelineContext().Should().BeTrue();
    }

    #endregion

    #region [ CreateChildContext ]

    [Fact]
    public void CreateChildContext_WithNullMessage_Throws()
    {
        Action act = () => ((IPipelineMessage)null!).CreateChildContext();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateChildContext_WithoutParent_ReturnsNewIndependentContext()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        IPipelineContext child = message.CreateChildContext();

        child.Should().NotBeNull();
        child.ParentContextId.Should().BeNull();
    }

    [Fact]
    public void CreateChildContext_WithParent_LinksParentAndChild()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        var parent = new PipelineContext("parent-corr");
        message.SetPipelineContext(parent);

        IPipelineContext child = message.CreateChildContext();

        child.ParentContextId.Should().Be("parent-corr");
        child.CausationId.Should().Be("parent-corr");
    }

    #endregion
}
