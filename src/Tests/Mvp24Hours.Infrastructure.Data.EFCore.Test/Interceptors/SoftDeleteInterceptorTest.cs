using Microsoft.EntityFrameworkCore;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class SoftDeleteInterceptorTest
{
    [Fact]
    public void SaveChanges_OnRemove_ConvertsToSoftDelete()
    {
        Mock<ICurrentUserProvider> userProvider = EfCoreTestHelpers.CreateUserProvider("delete-user");
        Mock<IClock> clock = EfCoreTestHelpers.CreateClock(new DateTime(2026, 7, 18, 14, 0, 0, DateTimeKind.Utc));
        var interceptor = new SoftDeleteInterceptor(userProvider.Object, clock.Object);
        string databaseName = $"SoftDelete_{Guid.NewGuid():N}";

        using (SoftDeleteTestDbContext context = CreateSoftDeleteContext(databaseName, interceptor))
        {
            context.SoftDeleteEntities.Add(new TestSoftDeleteEntity { Name = "ToDelete" });
            context.SaveChanges();
        }

        using (SoftDeleteTestDbContext context = CreateSoftDeleteContext(databaseName, interceptor))
        {
            TestSoftDeleteEntity entity = context.SoftDeleteEntities.Single();
            context.SoftDeleteEntities.Remove(entity);
            context.SaveChanges();
        }

        using (SoftDeleteTestDbContext context = CreateSoftDeleteContext(databaseName, interceptor))
        {
            context.SoftDeleteEntities.Count().Should().Be(0);

            TestSoftDeleteEntity deleted = context.SoftDeleteEntities
                .IgnoreQueryFilters()
                .Single();

            deleted.IsDeleted.Should().BeTrue();
            deleted.DeletedAt.Should().Be(clock.Object.UtcNow);
            deleted.DeletedBy.Should().Be("delete-user");
            deleted.Name.Should().Be("ToDelete");
        }
    }

    private static SoftDeleteTestDbContext CreateSoftDeleteContext(string databaseName, SoftDeleteInterceptor interceptor)
    {
        DbContextOptionsBuilder<SoftDeleteTestDbContext> optionsBuilder = new DbContextOptionsBuilder<SoftDeleteTestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(interceptor);

        var context = new SoftDeleteTestDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class SoftDeleteTestDbContext(DbContextOptions options) : TestDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplySoftDeleteGlobalFilter();
        }
    }
}
