using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class AuditSaveChangesInterceptorTest
{
    [Fact]
    public void SaveChanges_OnAdd_SetsCreatedAtAndCreatedBy()
    {
        var userProvider = EfCoreTestHelpers.CreateUserProvider("audit-user");
        var clock = EfCoreTestHelpers.CreateClock(new DateTime(2026, 7, 18, 15, 30, 0, DateTimeKind.Utc));
        var interceptor = new AuditSaveChangesInterceptor(userProvider.Object, clock.Object);

        using var context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        var entity = new TestAuditableEntity { Name = "New" };
        context.AuditableEntities.Add(entity);
        context.SaveChanges();

        entity.CreatedAt.Should().Be(clock.Object.UtcNow);
        entity.CreatedBy.Should().Be("audit-user");
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public void SaveChanges_OnModify_SetsModifiedAtAndModifiedBy()
    {
        var userProvider = EfCoreTestHelpers.CreateUserProvider("modifier-user");
        var createdClock = EfCoreTestHelpers.CreateClock(new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc));
        var modifiedClock = EfCoreTestHelpers.CreateClock(new DateTime(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc));
        var databaseName = $"Audit_{Guid.NewGuid():N}";

        using (var context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(new AuditSaveChangesInterceptor(userProvider.Object, createdClock.Object))))
        {
            context.AuditableEntities.Add(new TestAuditableEntity { Name = "Original" });
            context.SaveChanges();
        }

        using var modifyContext = EfCoreTestHelpers.CreateContext(databaseName, options =>
            options.AddInterceptors(new AuditSaveChangesInterceptor(userProvider.Object, modifiedClock.Object)));

        var tracked = modifyContext.AuditableEntities.Single();
        var originalCreatedAt = tracked.CreatedAt;
        var originalCreatedBy = tracked.CreatedBy;
        tracked.Name = "Updated";
        modifyContext.SaveChanges();

        tracked.ModifiedAt.Should().Be(modifiedClock.Object.UtcNow);
        tracked.ModifiedBy.Should().Be("modifier-user");
        tracked.CreatedAt.Should().Be(originalCreatedAt);
        tracked.CreatedBy.Should().Be(originalCreatedBy);
    }
}
