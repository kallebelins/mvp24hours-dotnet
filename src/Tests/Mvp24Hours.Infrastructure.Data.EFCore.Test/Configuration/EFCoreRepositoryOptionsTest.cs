using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Configuration;

[Trait("Category", "Unit")]
public class EFCoreRepositoryOptionsTest
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var options = new EFCoreRepositoryOptions();

        options.MaxQtyByQueryPage.Should().Be(ConstantsHelper.Data.MaxQtyByQueryPage);
        options.TransactionIsolationLevel.Should().BeNull();
        options.DefaultTrackingBehavior.Should().Be(QueryTrackingBehavior.TrackAll);
        options.UseSplitQueries.Should().BeFalse();
        options.EnableQueryTags.Should().BeFalse();
        options.QueryTagPrefix.Should().Be("Mvp24Hours");
        options.EnableSensitiveDataLogging.Should().BeFalse();
        options.SlowQueryThresholdMs.Should().Be(1000);
        options.StreamingBufferSize.Should().Be(100);
        options.UseAutoMapperProjection.Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldGetAndSetValues()
    {
        var options = new EFCoreRepositoryOptions
        {
            MaxQtyByQueryPage = 50,
            TransactionIsolationLevel = IsolationLevel.Serializable,
            DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking,
            UseSplitQueries = true,
            EnableQueryTags = true,
            QueryTagPrefix = "Custom",
            EnableSensitiveDataLogging = true,
            SlowQueryThresholdMs = 250,
            StreamingBufferSize = 25,
            UseAutoMapperProjection = true
        };

        options.MaxQtyByQueryPage.Should().Be(50);
        options.TransactionIsolationLevel.Should().Be(IsolationLevel.Serializable);
        options.DefaultTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
        options.UseSplitQueries.Should().BeTrue();
        options.EnableQueryTags.Should().BeTrue();
        options.QueryTagPrefix.Should().Be("Custom");
        options.EnableSensitiveDataLogging.Should().BeTrue();
        options.SlowQueryThresholdMs.Should().Be(250);
        options.StreamingBufferSize.Should().Be(25);
        options.UseAutoMapperProjection.Should().BeTrue();
    }
}
