using Moq;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class AuditSaveChangesInterceptorTest
{
    [Fact]
    public void SaveChanges_OnAdd_SetsCreatedAtAndCreatedBy()
    {
        Mock<ICurrentUserProvider> userProvider = EfCoreTestHelpers.CreateUserProvider("audit-user");
        Mock<IClock> clock = EfCoreTestHelpers.CreateClock(new DateTime(2026, 7, 18, 15, 30, 0, DateTimeKind.Utc));
        var interceptor = new AuditSaveChangesInterceptor(userProvider.Object, clock.Object);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
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
        Mock<ICurrentUserProvider> userProvider = EfCoreTestHelpers.CreateUserProvider("modifier-user");
        Mock<IClock> createdClock = EfCoreTestHelpers.CreateClock(new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc));
        Mock<IClock> modifiedClock = EfCoreTestHelpers.CreateClock(new DateTime(2026, 7, 18, 16, 0, 0, DateTimeKind.Utc));
        string databaseName = $"Audit_{Guid.NewGuid():N}";

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(new AuditSaveChangesInterceptor(userProvider.Object, createdClock.Object))))
        {
            context.AuditableEntities.Add(new TestAuditableEntity { Name = "Original" });
            context.SaveChanges();
        }

        using TestDbContext modifyContext = EfCoreTestHelpers.CreateContext(databaseName, options =>
            options.AddInterceptors(new AuditSaveChangesInterceptor(userProvider.Object, modifiedClock.Object)));

        TestAuditableEntity tracked = modifyContext.AuditableEntities.Single();
        DateTime originalCreatedAt = tracked.CreatedAt;
        string originalCreatedBy = tracked.CreatedBy;
        tracked.Name = "Updated";
        modifyContext.SaveChanges();

        tracked.ModifiedAt.Should().Be(modifiedClock.Object.UtcNow);
        tracked.ModifiedBy.Should().Be("modifier-user");
        tracked.CreatedAt.Should().Be(originalCreatedAt);
        tracked.CreatedBy.Should().Be(originalCreatedBy);
    }
}
