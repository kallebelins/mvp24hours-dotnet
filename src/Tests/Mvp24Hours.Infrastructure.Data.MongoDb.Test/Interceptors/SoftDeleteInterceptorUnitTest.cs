using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Interceptors;

[Trait("Category", "Unit")]
public class SoftDeleteInterceptorUnitTest
{
    [Fact]
    public void Order_ShouldRunAfterAuditInterceptor()
    {
        var interceptor = new SoftDeleteInterceptor();

        interceptor.Order.Should().Be(-900);
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_ForNonSoftDeletableEntity_ShouldProceedWithPhysicalDelete()
    {
        var interceptor = new SoftDeleteInterceptor();
        var entity = new SoftDeletePlainDoc { Name = "permanent" };

        DeleteInterceptionResult result = await interceptor.OnBeforeDeleteAsync(entity);

        result.ShouldProceed.Should().BeTrue();
        result.ConvertToSoftDelete.Should().BeFalse();
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_ForSoftDeletableEntity_ShouldMarkDeletedFields()
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 8, 3, 10, 0, 0, DateTimeKind.Utc));
        var userProvider = new Mock<ICurrentUserProvider>();
        userProvider.Setup(u => u.UserId).Returns("deleter");

        var interceptor = new SoftDeleteInterceptor(
            userProvider.Object,
            clock.Object,
            NullLogger<SoftDeleteInterceptor>.Instance);
        var entity = new SoftDeleteTrackedDoc { Name = "to-delete", IsDeleted = false };

        DeleteInterceptionResult result = await interceptor.OnBeforeDeleteAsync(entity);

        result.ConvertToSoftDelete.Should().BeTrue();
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(clock.Object.UtcNow);
        entity.DeletedBy.Should().Be("deleter");
    }

    [Fact]
    public async Task OnBeforeDeleteAsync_WithoutUserProvider_ShouldUseDefaultUser()
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 8, 3, 10, 0, 0, DateTimeKind.Utc));
        var interceptor = new SoftDeleteInterceptor(clock: clock.Object, defaultUser: "System");

        var entity = new SoftDeleteTrackedDoc { Name = "system-delete" };

        await interceptor.OnBeforeDeleteAsync(entity);

        entity.DeletedBy.Should().Be("System");
    }

    [Fact]
    public async Task OnAfterDeleteAsync_WhenSoftDeleted_ShouldComplete()
    {
        var interceptor = new SoftDeleteInterceptor(logger: NullLogger<SoftDeleteInterceptor>.Instance);
        var entity = new SoftDeleteTrackedDoc { Name = "done", IsDeleted = true };

        Func<Task> act = () => interceptor.OnAfterDeleteAsync(entity, wasSoftDeleted: true);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnAfterDeleteAsync_WhenPhysicallyDeleted_ShouldComplete()
    {
        var interceptor = new SoftDeleteInterceptor();
        var entity = new SoftDeletePlainDoc { Name = "removed" };

        Func<Task> act = () => interceptor.OnAfterDeleteAsync(entity, wasSoftDeleted: false);

        await act.Should().NotThrowAsync();
    }
}

public class SoftDeletePlainDoc : IEntityBase
{
    public string Name { get; set; } = string.Empty;
    public object? EntityKey => Name;
}

public class SoftDeleteTrackedDoc : ISoftDeletable, IEntityBase
{
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
    public object? EntityKey => Name;
}
