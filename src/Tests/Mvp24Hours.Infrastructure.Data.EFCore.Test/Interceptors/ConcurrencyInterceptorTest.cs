using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class ConcurrencyInterceptorTest
{
    [Fact]
    public void SaveChanges_OnAdd_SetsVersionToOne()
    {
        var interceptor = new ConcurrencyInterceptor();

        using var context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        var entity = new TestVersionedEntity { Name = "New" };
        context.VersionedEntities.Add(entity);
        context.SaveChanges();

        entity.Version.Should().Be(1);
    }

    [Fact]
    public void SaveChanges_OnModify_IncrementsVersion()
    {
        var interceptor = new ConcurrencyInterceptor();
        var databaseName = $"Version_{Guid.NewGuid():N}";

        using (var context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            context.VersionedEntities.Add(new TestVersionedEntity { Name = "Original" });
            context.SaveChanges();
        }

        using (var context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            var entity = context.VersionedEntities.Single();
            entity.Version.Should().Be(1);

            entity.Name = "Updated";
            context.SaveChanges();

            entity.Version.Should().Be(2);
        }
    }
}
