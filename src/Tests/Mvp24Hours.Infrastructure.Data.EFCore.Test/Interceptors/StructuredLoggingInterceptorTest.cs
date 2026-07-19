using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class StructuredLoggingInterceptorTest
{
    [Fact]
    public void SaveChanges_WithNullLogger_DoesNotThrow()
    {
        var interceptor = new StructuredLoggingInterceptor();

        using var context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "Structured" });
        var act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveChanges_WithLogger_DoesNotThrow()
    {
        var interceptor = new StructuredLoggingInterceptor(
            NullLogger.Instance,
            logParameters: false,
            outputAsJson: true);

        using var context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "Structured" });
        var act = () => context.SaveChanges();

        act.Should().NotThrow();
    }
}
