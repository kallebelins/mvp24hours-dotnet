using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Interceptors;

[Trait("Category", "Unit")]
public class AuditInterceptorTest
{
    [Fact]
    public async Task OnBeforeInsertAsync_WithAuditableEntity_ShouldSetCreateFields()
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc));
        var userProvider = new Mock<ICurrentUserProvider>();
        userProvider.Setup(u => u.UserId).Returns("audit-user");

        var interceptor = new AuditInterceptor(userProvider.Object, clock.Object, NullLogger<AuditInterceptor>.Instance);
        var entity = new AuditableTenantProduct { Name = "Widget" };

        await interceptor.OnBeforeInsertAsync(entity);

        entity.CreatedAt.Should().Be(clock.Object.UtcNow);
        entity.CreatedBy.Should().Be("audit-user");
        entity.ModifiedAt.Should().BeNull();
        entity.ModifiedBy.Should().BeNull();
    }

    [Fact]
    public async Task OnBeforeUpdateAsync_WithAuditableEntity_ShouldSetModifiedFields()
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 6, 2, 8, 30, 0, DateTimeKind.Utc));
        var userProvider = new Mock<ICurrentUserProvider>();
        userProvider.Setup(u => u.UserId).Returns("editor");

        var interceptor = new AuditInterceptor(userProvider.Object, clock.Object);
        var entity = new AuditableTenantProduct
        {
            Name = "Widget",
            CreatedAt = new DateTime(2025, 1, 1),
            CreatedBy = "creator"
        };

        await interceptor.OnBeforeUpdateAsync(entity);

        entity.ModifiedAt.Should().Be(clock.Object.UtcNow);
        entity.ModifiedBy.Should().Be("editor");
        entity.CreatedBy.Should().Be("creator");
    }

    [Fact]
    public async Task OnBeforeInsertAsync_WithoutUserProvider_ShouldUseDefaultUser()
    {
        var interceptor = new AuditInterceptor(defaultUser: "SystemBot");
        var entity = new AuditableTenantProduct { Name = "Default" };

        await interceptor.OnBeforeInsertAsync(entity);

        entity.CreatedBy.Should().Be("SystemBot");
    }

    [Fact]
    public async Task OnBeforeInsertAsync_WithLegacyDateLog_ShouldSetCreatedDate()
    {
        var interceptor = new AuditInterceptor();
        var entity = new LegacyDateLogEntity { Name = "legacy" };

        await interceptor.OnBeforeInsertAsync(entity);

        entity.Created.Should().NotBe(default);
        entity.Modified.Should().BeNull();
        entity.Removed.Should().BeNull();
    }

    [Fact]
    public async Task OnBeforeUpdateAsync_WithLegacyDateLog_ShouldSetModifiedDate()
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc));
        var interceptor = new AuditInterceptor(clock: clock.Object);
        var entity = new LegacyDateLogEntity { Name = "legacy", Created = new DateTime(2025, 1, 1) };

        await interceptor.OnBeforeUpdateAsync(entity);

        entity.Modified.Should().Be(clock.Object.UtcNow);
    }

    [Fact]
    public void Order_ShouldRunEarlyInPipeline()
    {
        var interceptor = new AuditInterceptor();
        interceptor.Order.Should().Be(-1000);
    }

    private sealed class LegacyDateLogEntity : IEntityBase, IEntityDateLog
    {
        public MongoDB.Bson.ObjectId Id { get; set; } = MongoDB.Bson.ObjectId.GenerateNewId();

        public string Name { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public DateTime? Modified { get; set; }

        public DateTime? Removed { get; set; }

        public object EntityKey => Id;

        public IReadOnlyCollection<Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult> GetNotifications()
        {
            return [];
        }

        public bool HasNotifications()
        {
            return false;
        }
    }
}
