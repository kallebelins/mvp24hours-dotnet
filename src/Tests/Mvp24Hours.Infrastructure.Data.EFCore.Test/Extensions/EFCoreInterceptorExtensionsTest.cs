using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class EFCoreInterceptorExtensionsTest
{
    [Fact]
    public void AddMvp24HoursEFCoreSoftDeleteInterceptor_RegistersInterceptor()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursEFCoreSoftDeleteInterceptor();

        services.Should().Contain(d =>
            d.ServiceType == typeof(SoftDeleteInterceptor) &&
            d.Lifetime == ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<SoftDeleteInterceptor>().Should().NotBeNull();
    }

    [Fact]
    public void SaveChanges_WithSoftDeleteInterceptor_ConvertsDeleteToModified()
    {
        var clockNow = new DateTime(2026, 8, 28, 9, 30, 0, DateTimeKind.Utc);
        var services = new ServiceCollection();
        services.AddSingleton(EfCoreTestHelpers.CreateUserProvider("registered-user").Object);
        services.AddSingleton(EfCoreTestHelpers.CreateClock(clockNow).Object);
        services.AddMvp24HoursEFCoreSoftDeleteInterceptor();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        SoftDeleteInterceptor interceptor = scope.ServiceProvider.GetRequiredService<SoftDeleteInterceptor>();

        string databaseName = $"DiSoftDelete_{Guid.NewGuid():N}";

        using (SoftDeleteFilterDbContext context = CreateContext(databaseName, interceptor))
        {
            context.SoftDeleteEntities.Add(new TestSoftDeleteEntity { Name = "ToDelete" });
            context.SaveChanges();
        }

        using (SoftDeleteFilterDbContext context = CreateContext(databaseName, interceptor))
        {
            context.SoftDeleteEntities.Remove(context.SoftDeleteEntities.Single());
            context.SaveChanges();
        }

        using (SoftDeleteFilterDbContext context = CreateContext(databaseName, interceptor))
        {
            context.SoftDeleteEntities.Should().BeEmpty();

            TestSoftDeleteEntity deleted = context.SoftDeleteEntities.IgnoreQueryFilters().Single();
            deleted.IsDeleted.Should().BeTrue();
            deleted.DeletedAt.Should().Be(clockNow);
            deleted.DeletedBy.Should().Be("registered-user");
            deleted.Name.Should().Be("ToDelete");
        }
    }

    /// <summary>
    /// <see cref="ICurrentUserProvider"/> and <see cref="IClock"/> are resolved with
    /// <c>GetService</c>, so the registration must work without them.
    /// </summary>
    [Fact]
    public void SaveChanges_WithSoftDeleteInterceptorAndNoOptionalDependencies_UsesDefaultUser()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursEFCoreSoftDeleteInterceptor("batch-job");

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        SoftDeleteInterceptor interceptor = scope.ServiceProvider.GetRequiredService<SoftDeleteInterceptor>();

        string databaseName = $"DiSoftDeleteNoDeps_{Guid.NewGuid():N}";

        using (SoftDeleteFilterDbContext context = CreateContext(databaseName, interceptor))
        {
            context.SoftDeleteEntities.Add(new TestSoftDeleteEntity { Name = "NoDeps" });
            context.SaveChanges();
            context.SoftDeleteEntities.Remove(context.SoftDeleteEntities.Single());
            context.SaveChanges();
        }

        using (SoftDeleteFilterDbContext context = CreateContext(databaseName, interceptor))
        {
            TestSoftDeleteEntity deleted = context.SoftDeleteEntities.IgnoreQueryFilters().Single();
            deleted.IsDeleted.Should().BeTrue();
            deleted.DeletedBy.Should().Be("batch-job");
            deleted.DeletedAt.Should().NotBeNull();
        }
    }

    /// <summary>
    /// Non-regression for the deprecated <c>ApplyLogRules</c> path: it still stamps
    /// <c>Created</c>/<c>Modified</c> and still takes no action on <c>EntityState.Deleted</c>,
    /// so a context-level delete remains physical.
    /// </summary>
    [Fact]
    public void SaveChanges_WithApplyLogRulesOnly_KeepsCurrentBehavior()
    {
        string databaseName = $"LegacyLogRules_{Guid.NewGuid():N}";
        var entity = new TestLogEntity { Name = "Legacy" };

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName))
        {
            context.LogEntities.Add(entity);
            context.SaveChanges();

            entity.Created.Should().NotBe(default);
            entity.Modified.Should().BeNull();
            entity.Removed.Should().BeNull();

            entity.Name = "LegacyModified";
            context.SaveChanges();

            entity.Modified.Should().NotBeNull();
        }

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName))
        {
            context.LogEntities.Remove(context.LogEntities.Single());
            context.SaveChanges();
        }

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName))
        {
            context.LogEntities.IgnoreQueryFilters().Should().BeEmpty();
        }
    }

    private static SoftDeleteFilterDbContext CreateContext(string databaseName, SoftDeleteInterceptor interceptor)
    {
        DbContextOptionsBuilder<SoftDeleteFilterDbContext> optionsBuilder =
            new DbContextOptionsBuilder<SoftDeleteFilterDbContext>()
                .UseInMemoryDatabase(databaseName)
                .AddInterceptors(interceptor);

        var context = new SoftDeleteFilterDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class SoftDeleteFilterDbContext(DbContextOptions options) : TestDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplySoftDeleteGlobalFilter();
        }
    }
}
