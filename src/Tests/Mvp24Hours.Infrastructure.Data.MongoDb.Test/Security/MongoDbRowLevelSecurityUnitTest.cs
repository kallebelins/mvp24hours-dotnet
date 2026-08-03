using MongoDB.Driver;
using Moq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Security;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Security;

[Trait("Category", "Unit")]
public class MongoDbRowLevelSecurityUnitTest
{
    [Fact]
    public void CreateSecurityFilter_ForPlainEntity_ShouldReturnEmptyFilter()
    {
        var rls = new MongoDbRowLevelSecurity();

        FilterDefinition<RlsPlainDoc> filter = rls.CreateSecurityFilter<RlsPlainDoc>();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void CreateSecurityFilter_ForTenantEntity_ShouldIncludeTenantFilter()
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.HasTenant).Returns(true);
        tenantProvider.SetupGet(t => t.TenantId).Returns("tenant-a");

        var rls = new MongoDbRowLevelSecurity(tenantProvider.Object);

        FilterDefinition<RlsTenantDoc> filter = rls.CreateSecurityFilter<RlsTenantDoc>();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void CreateSecurityFilter_ForSoftDeletableEntity_ShouldIncludeIsDeletedFilter()
    {
        var rls = new MongoDbRowLevelSecurity();

        FilterDefinition<RlsSoftDeletableDoc> filter = rls.CreateSecurityFilter<RlsSoftDeletableDoc>();

        filter.Should().NotBeNull();
    }

    [Fact]
    public void CreateSecurityFilter_WithAdditionalFilters_ShouldCombineFilters()
    {
        var rls = new MongoDbRowLevelSecurity();
        FilterDefinition<RlsSoftDeletableDoc> additional =
            Builders<RlsSoftDeletableDoc>.Filter.Eq(d => d.Name, "active");

        FilterDefinition<RlsSoftDeletableDoc> filter =
            rls.CreateSecurityFilter(additional);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void CreateSecurityFilter_WithNullAdditionalFilters_ShouldReturnSecurityFilterOnly()
    {
        var rls = new MongoDbRowLevelSecurity();

        FilterDefinition<RlsPlainDoc> filter = rls.CreateSecurityFilter<RlsPlainDoc>(null!);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void WrapWithSecurity_ShouldCombineExistingFilter()
    {
        var rls = new MongoDbRowLevelSecurity();
        FilterDefinition<RlsSoftDeletableDoc> existing =
            Builders<RlsSoftDeletableDoc>.Filter.Eq(d => d.Name, "x");

        FilterDefinition<RlsSoftDeletableDoc> filter = rls.WrapWithSecurity(existing);

        filter.Should().NotBeNull();
    }

    [Fact]
    public void WrapWithSecurity_WithExpression_ShouldBuildCombinedFilter()
    {
        var rls = new MongoDbRowLevelSecurity();

        FilterDefinition<RlsSoftDeletableDoc> filter =
            rls.WrapWithSecurity((RlsSoftDeletableDoc d) => d.Name == "x");

        filter.Should().NotBeNull();
    }

    [Fact]
    public void ValidateEntityAccess_WithMatchingTenant_ShouldNotThrow()
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.HasTenant).Returns(true);
        tenantProvider.SetupGet(t => t.TenantId).Returns("tenant-a");

        var rls = new MongoDbRowLevelSecurity(tenantProvider.Object);
        var entity = new RlsTenantDoc { TenantId = "tenant-a", Name = "allowed" };

        Action act = () => rls.ValidateEntityAccess(entity);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateEntityAccess_WithMismatchedTenant_ShouldThrowUnauthorized()
    {
        var tenantProvider = new Mock<ITenantProvider>();
        tenantProvider.SetupGet(t => t.HasTenant).Returns(true);
        tenantProvider.SetupGet(t => t.TenantId).Returns("tenant-a");

        var rls = new MongoDbRowLevelSecurity(tenantProvider.Object);
        var entity = new RlsTenantDoc { TenantId = "tenant-b", Name = "denied" };

        Action act = () => rls.ValidateEntityAccess(entity);

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void ValidateEntityAccess_WithSoftDeletedEntity_ShouldThrowInvalidOperation()
    {
        var rls = new MongoDbRowLevelSecurity();
        var entity = new RlsSoftDeletableDoc { Name = "deleted", IsDeleted = true };

        Action act = () => rls.ValidateEntityAccess(entity);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateEntityAccess_WithNullEntity_ShouldThrowArgumentNull()
    {
        var rls = new MongoDbRowLevelSecurity();

        Action act = () => rls.ValidateEntityAccess<RlsPlainDoc>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OwnerBasedSecurityPolicy_ForAdmin_ShouldSkipFilter()
    {
        var userProvider = new Mock<ICurrentUserProvider>();
        userProvider.SetupGet(u => u.UserId).Returns("admin");
        var policy = new OwnerBasedSecurityPolicy(isAdminCheck: _ => true);

        FilterDefinition<RlsOwnedDoc>? filter = policy.CreateFilter<RlsOwnedDoc>(null, userProvider.Object);

        filter.Should().BeNull();
    }

    [Fact]
    public void OwnerBasedSecurityPolicy_ForNonOwner_ShouldValidateAccess()
    {
        var userProvider = new Mock<ICurrentUserProvider>();
        userProvider.SetupGet(u => u.UserId).Returns("user-1");
        var policy = new OwnerBasedSecurityPolicy();
        var entity = new RlsOwnedDoc { CreatedBy = "user-2", Name = "owned" };

        Action act = () => policy.ValidateAccess(entity, null, userProvider.Object);

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void CompositeSecurityPolicy_ShouldCombineChildPolicies()
    {
        var userProvider = new Mock<ICurrentUserProvider>();
        userProvider.SetupGet(u => u.UserId).Returns("user-1");
        CompositeSecurityPolicy composite = new CompositeSecurityPolicy(new OwnerBasedSecurityPolicy())
            .AddPolicy(new OwnerBasedSecurityPolicy(isAdminCheck: _ => false));

        FilterDefinition<RlsOwnedDoc>? filter = composite.CreateFilter<RlsOwnedDoc>(null, userProvider.Object);

        filter.Should().NotBeNull();
    }
}

public class RlsPlainDoc
{
    public string Name { get; set; } = string.Empty;
}

public class RlsTenantDoc : ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class RlsSoftDeletableDoc : ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string DeletedBy { get; set; } = string.Empty;
}

public class RlsOwnedDoc
{
    public string CreatedBy { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
