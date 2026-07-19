//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Mvp24Hours.Infrastructure.Observability;

namespace Mvp24Hours.Infrastructure.Test.Observability;

[Trait("Category", "Unit")]
public class ActivitySourcesTest
{
    [Theory]
    [InlineData(nameof(ActivitySources.Http), "Mvp24Hours.Infrastructure.Http")]
    [InlineData(nameof(ActivitySources.Email), "Mvp24Hours.Infrastructure.Email")]
    [InlineData(nameof(ActivitySources.Sms), "Mvp24Hours.Infrastructure.Sms")]
    [InlineData(nameof(ActivitySources.FileStorage), "Mvp24Hours.Infrastructure.FileStorage")]
    [InlineData(nameof(ActivitySources.DistributedLocking), "Mvp24Hours.Infrastructure.DistributedLocking")]
    [InlineData(nameof(ActivitySources.BackgroundJobs), "Mvp24Hours.Infrastructure.BackgroundJobs")]
    [InlineData(nameof(ActivitySources.Resilience), "Mvp24Hours.Infrastructure.Resilience")]
    public void ActivitySource_ShouldHaveExpectedNameAndVersion(string propertyName, string expectedName)
    {
        ActivitySource source = GetActivitySource(propertyName);

        source.Name.Should().Be(expectedName);
        source.Version.Should().Be("1.0.0");
    }

    private static ActivitySource GetActivitySource(string propertyName)
    {
        return propertyName switch
        {
            nameof(ActivitySources.Http) => ActivitySources.Http,
            nameof(ActivitySources.Email) => ActivitySources.Email,
            nameof(ActivitySources.Sms) => ActivitySources.Sms,
            nameof(ActivitySources.FileStorage) => ActivitySources.FileStorage,
            nameof(ActivitySources.DistributedLocking) => ActivitySources.DistributedLocking,
            nameof(ActivitySources.BackgroundJobs) => ActivitySources.BackgroundJobs,
            nameof(ActivitySources.Resilience) => ActivitySources.Resilience,
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, "Unknown activity source.")
        };
    }
}
