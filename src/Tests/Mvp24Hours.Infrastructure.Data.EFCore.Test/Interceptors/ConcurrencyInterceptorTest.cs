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

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
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
        string databaseName = $"Version_{Guid.NewGuid():N}";

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            context.VersionedEntities.Add(new TestVersionedEntity { Name = "Original" });
            context.SaveChanges();
        }

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            TestVersionedEntity entity = context.VersionedEntities.Single();
            entity.Version.Should().Be(1);

            entity.Name = "Updated";
            context.SaveChanges();

            entity.Version.Should().Be(2);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_OnAdd_SetsVersionToOne()
    {
        var interceptor = new ConcurrencyInterceptor();

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        var entity = new TestVersionedEntity { Name = "AsyncNew" };
        context.VersionedEntities.Add(entity);
        await context.SaveChangesAsync();

        entity.Version.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_OnModify_IncrementsVersion()
    {
        var interceptor = new ConcurrencyInterceptor();
        string databaseName = $"VersionAsync_{Guid.NewGuid():N}";

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            context.VersionedEntities.Add(new TestVersionedEntity { Name = "OriginalAsync" });
            await context.SaveChangesAsync();
        }

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            TestVersionedEntity entity = await context.VersionedEntities.SingleAsync();
            entity.Version.Should().Be(1);

            entity.Name = "UpdatedAsync";
            await context.SaveChangesAsync();

            entity.Version.Should().Be(2);
        }
    }

    [Fact]
    public void SaveChanges_WithUnchangedEntity_ShouldNotModifyVersion()
    {
        var interceptor = new ConcurrencyInterceptor();
        string databaseName = $"VersionUnchanged_{Guid.NewGuid():N}";

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            context.VersionedEntities.Add(new TestVersionedEntity { Name = "Untouched" });
            context.SaveChanges();
        }

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            TestVersionedEntity entity = context.VersionedEntities.Single();
            entity.Version.Should().Be(1);

            // No modification made; SaveChanges should be a no-op for this entity's version.
            context.SaveChanges();

            entity.Version.Should().Be(1);
        }
    }

    [Fact]
    public void SaveChanges_WithDeletedEntity_ShouldNotThrowOrChangeVersion()
    {
        var interceptor = new ConcurrencyInterceptor();
        string databaseName = $"VersionDeleted_{Guid.NewGuid():N}";

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            context.VersionedEntities.Add(new TestVersionedEntity { Name = "ToDelete" });
            context.SaveChanges();
        }

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            TestVersionedEntity entity = context.VersionedEntities.Single();
            context.VersionedEntities.Remove(entity);

            Action act = () => context.SaveChanges();

            act.Should().NotThrow();
        }
    }
}
