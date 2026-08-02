using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class CommandLoggingInterceptorTest
{
    [Fact]
    public void SaveChanges_WithNullLogger_DoesNotThrow()
    {
        var interceptor = new CommandLoggingInterceptor();

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "Logged" });
        Func<int> act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveChanges_WithMockLogger_DoesNotThrow()
    {
        var interceptor = new CommandLoggingInterceptor(NullLogger.Instance, logAllQueries: false);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "Logged" });
        Func<int> act = () => context.SaveChanges();

        act.Should().NotThrow();
    }
}
