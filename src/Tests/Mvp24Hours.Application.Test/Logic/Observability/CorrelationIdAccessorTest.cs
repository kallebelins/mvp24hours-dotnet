using Mvp24Hours.Application.Logic.Observability;

namespace Mvp24Hours.Application.Test.Logic.Observability;

[Trait("Category", "Unit")]
public class CorrelationIdAccessorTest
{
    [Fact]
    public void SetAndGetCorrelationId_ShouldPersistInAsyncLocal()
    {
        var accessor = new CorrelationIdAccessor();

        accessor.SetCorrelationId("corr-123");

        accessor.CorrelationId.Should().Be("corr-123");
        accessor.HasCorrelationId.Should().BeTrue();
    }

    [Fact]
    public void EnsureCorrelationId_WhenMissing_ShouldGenerateGuid()
    {
        var accessor = new CorrelationIdAccessor();

        string id = accessor.EnsureCorrelationId();

        id.Should().NotBeNullOrWhiteSpace();
        accessor.CorrelationId.Should().Be(id);
    }

    [Fact]
    public void BeginScope_ShouldRestorePreviousCorrelationIdOnDispose()
    {
        var accessor = new CorrelationIdAccessor();
        accessor.SetCorrelationId("outer");

        using (accessor.BeginScope("inner"))
        {
            accessor.CorrelationId.Should().Be("inner");
            accessor.CausationId.Should().Be("outer");
        }

        accessor.CorrelationId.Should().Be("outer");
    }

    [Fact]
    public void CorrelationIdContext_StaticHelpers_ShouldWork()
    {
        using (CorrelationIdContext.BeginScope("static-scope"))
        {
            CorrelationIdContext.Current.Should().Be("static-scope");
        }
    }

    [Fact]
    public void SetCausationId_ShouldStoreCausation()
    {
        var accessor = new CorrelationIdAccessor();

        accessor.SetCausationId("cause-1");

        accessor.CausationId.Should().Be("cause-1");
    }
}
