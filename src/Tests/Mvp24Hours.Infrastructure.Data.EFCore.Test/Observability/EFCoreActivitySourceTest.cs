using System.Diagnostics;
using Mvp24Hours.Infrastructure.Data.EFCore.Observability;
using Mvp24Hours.Infrastructure.Testing.Observability;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Observability;

[Trait("Category", "Unit")]
public class EFCoreActivitySourceTest
{
    [Fact]
    public void Source_ShouldExposeExpectedNameAndVersion()
    {
        EFCoreActivitySource.SourceName.Should().Be("Mvp24Hours.EFCore");
        EFCoreActivitySource.Version.Should().Be("1.0.0");
        EFCoreActivitySource.Source.Name.Should().Be(EFCoreActivitySource.SourceName);
        EFCoreActivitySource.MeterName.Should().Be("Mvp24Hours.EFCore.Metrics");
    }

    [Fact]
    public void StartQueryActivity_ShouldSetOperationAndStatementTags()
    {
        using var listener = new FakeActivityListener(EFCoreActivitySource.SourceName);

        using Activity? activity = EFCoreActivitySource.StartQueryActivity("SELECT 1", "AppDb");
        activity.Should().NotBeNull();
        EFCoreActivitySource.SetSuccess(activity, rowsAffected: 1);

        activity!.GetTagItem(EFCoreActivitySource.TagNames.DbOperation).Should().Be("SELECT");
        activity.GetTagItem(EFCoreActivitySource.TagNames.DbStatement).Should().Be("SELECT 1");
        activity.GetTagItem(EFCoreActivitySource.TagNames.DbName).Should().Be("AppDb");
        activity.GetTagItem(EFCoreActivitySource.TagNames.IsSuccess).Should().Be(true);
        activity.GetTagItem(EFCoreActivitySource.TagNames.RowsAffected).Should().Be(1);
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.Stop();

        listener.HasActivity(EFCoreActivitySource.ActivityNames.Query).Should().BeTrue();
    }

    [Fact]
    public void StartCommandActivity_ShouldSetOperationTag()
    {
        using var listener = new FakeActivityListener(EFCoreActivitySource.SourceName);

        using Activity? activity = EFCoreActivitySource.StartCommandActivity("INSERT INTO t VALUES (1)", "INSERT", "AppDb");
        activity.Should().NotBeNull();
        activity!.GetTagItem(EFCoreActivitySource.TagNames.DbOperation).Should().Be("INSERT");
        activity.Stop();

        listener.HasActivity(EFCoreActivitySource.ActivityNames.Command).Should().BeTrue();
    }

    [Fact]
    public void StartSlowQueryActivity_ShouldAddEventAndTags()
    {
        using var listener = new FakeActivityListener(EFCoreActivitySource.SourceName);

        using Activity? activity = EFCoreActivitySource.StartSlowQueryActivity("SELECT * FROM big", 1500, 1000);
        activity.Should().NotBeNull();
        activity!.GetTagItem(EFCoreActivitySource.TagNames.IsSlowQuery).Should().Be(true);
        activity.Events.Should().Contain(e => e.Name == "slow_query_detected");
        activity.Stop();

        listener.HasActivity(EFCoreActivitySource.ActivityNames.SlowQuery).Should().BeTrue();
    }

    [Fact]
    public void SetError_ShouldMarkActivityAsFailed()
    {
        using var listener = new FakeActivityListener(EFCoreActivitySource.SourceName);

        using Activity? activity = EFCoreActivitySource.StartQueryActivity("SELECT 1");
        EFCoreActivitySource.SetError(activity, new InvalidOperationException("boom"));

        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.GetTagItem(EFCoreActivitySource.TagNames.IsSuccess).Should().Be(false);
        activity.GetTagItem(EFCoreActivitySource.TagNames.ErrorMessage).Should().Be("boom");
        activity.Events.Should().Contain(e => e.Name == "exception");
        activity.Stop();

        listener.HasActivity(EFCoreActivitySource.ActivityNames.Query).Should().BeTrue();
    }

    [Fact]
    public void SetContext_ShouldSetCorrelationTenantAndUser()
    {
        using var listener = new FakeActivityListener(EFCoreActivitySource.SourceName);

        using Activity? activity = EFCoreActivitySource.StartQueryActivity("SELECT 1");
        EFCoreActivitySource.SetContext(activity, "corr-1", "tenant-a", "user-b");
        EFCoreActivitySource.SetDuration(activity, 42.5);

        activity!.GetTagItem(EFCoreActivitySource.TagNames.CorrelationId).Should().Be("corr-1");
        activity.GetTagItem(EFCoreActivitySource.TagNames.TenantId).Should().Be("tenant-a");
        activity.GetTagItem(EFCoreActivitySource.TagNames.UserId).Should().Be("user-b");
        activity.GetTagItem(EFCoreActivitySource.TagNames.QueryDurationMs).Should().Be(42.5);
        activity.Stop();

        listener.HasActivity(EFCoreActivitySource.ActivityNames.Query).Should().BeTrue();
    }

    [Fact]
    public void StartQueryActivity_WithLongSql_ShouldTruncateStatement()
    {
        using var listener = new FakeActivityListener(EFCoreActivitySource.SourceName);
        string longSql = new('x', 2500);

        using Activity? activity = EFCoreActivitySource.StartQueryActivity(longSql);
        string? statement = activity!.GetTagItem(EFCoreActivitySource.TagNames.DbStatement) as string;
        activity.Stop();

        statement.Should().NotBeNull();
        statement!.Should().Contain("[TRUNCATED]");
        statement.Length.Should().BeLessThan(longSql.Length);
        listener.HasActivity(EFCoreActivitySource.ActivityNames.Query).Should().BeTrue();
    }

    [Fact]
    public void SetSuccess_AndSetError_WithNullActivity_ShouldNotThrow()
    {
        Action success = () => EFCoreActivitySource.SetSuccess(null, 1);
        Action error = () => EFCoreActivitySource.SetError(null, new Exception("x"));
        Action context = () => EFCoreActivitySource.SetContext(null, "c");
        Action duration = () => EFCoreActivitySource.SetDuration(null, 1);

        success.Should().NotThrow();
        error.Should().NotThrow();
        context.Should().NotThrow();
        duration.Should().NotThrow();
    }

    [Fact]
    public void ExtensionMethods_ShouldSetTags()
    {
        using var listener = new FakeActivityListener(EFCoreActivitySource.SourceName);

        using Activity? activity = EFCoreActivitySource.StartQueryActivity("SELECT 1");
        activity!.WithDatabaseSystem("sqlserver")
            .WithDatabaseName("orders")
            .AsSlowQuery(2000, 1000);

        activity!.GetTagItem(EFCoreActivitySource.TagNames.DbSystem).Should().Be("sqlserver");
        activity.GetTagItem(EFCoreActivitySource.TagNames.DbName).Should().Be("orders");
        activity.GetTagItem(EFCoreActivitySource.TagNames.IsSlowQuery).Should().Be(true);
        activity.Stop();

        listener.HasActivity(EFCoreActivitySource.ActivityNames.Query).Should().BeTrue();
    }
}
