using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class SlowQueryInterceptorTest
{
    [Fact]
    public void SaveChanges_WithInterceptor_DoesNotThrow()
    {
        var interceptor = new SlowQueryInterceptor(
            slowQueryThreshold: TimeSpan.FromSeconds(10),
            logger: NullLogger.Instance,
            createActivities: false);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "SlowQuery" });
        Func<int> act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithCustomThresholdsAndCallback_ShouldCreateInstance()
    {
        bool callbackInvoked = false;
        var interceptor = new SlowQueryInterceptor(
            slowQueryThreshold: TimeSpan.FromMilliseconds(100),
            writeSlowQueryThreshold: TimeSpan.FromMilliseconds(200),
            logger: NullLogger.Instance,
            createActivities: false,
            onSlowQueryDetected: (_, _, _) => callbackInvoked = true);

        interceptor.Should().NotBeNull();
        callbackInvoked.Should().BeFalse();
    }

    [Fact]
    public async Task SaveChangesAsync_WithInterceptor_DoesNotThrow()
    {
        var interceptor = new SlowQueryInterceptor(
            slowQueryThreshold: TimeSpan.FromSeconds(10),
            createActivities: false);

        await using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "SlowQueryAsync" });
        Func<Task<int>> act = () => context.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }
}
