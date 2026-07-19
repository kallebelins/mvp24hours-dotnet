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

        using var context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "SlowQuery" });
        var act = () => context.SaveChanges();

        act.Should().NotThrow();
    }
}
