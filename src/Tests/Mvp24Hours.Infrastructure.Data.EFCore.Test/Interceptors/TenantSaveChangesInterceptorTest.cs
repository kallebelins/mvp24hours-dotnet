using Microsoft.EntityFrameworkCore;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class TenantSaveChangesInterceptorTest
{
    [Fact]
    public void SaveChanges_OnAdd_SetsTenantIdFromProvider()
    {
        Mock<ITenantProvider> tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-abc");
        var options = new TenantInterceptorOptions
        {
            ValidateTenantOnAdd = false,
            ValidateTenantOnModify = false,
            ValidateTenantOnDelete = false
        };
        var interceptor = new TenantSaveChangesInterceptor(tenantProvider.Object, options);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: builder =>
            builder.AddInterceptors(interceptor));

        var entity = new TestTenantEntity { Name = "Tenant Item" };
        context.TenantEntities.Add(entity);
        context.SaveChanges();

        entity.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void SaveChanges_OnModify_PreventTenantIdChange_KeepsOriginalTenantId()
    {
        Mock<ITenantProvider> tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-original");
        var options = new TenantInterceptorOptions
        {
            ValidateTenantOnAdd = false,
            ValidateTenantOnModify = false,
            PreventTenantIdChange = true
        };
        string databaseName = $"Tenant_{Guid.NewGuid():N}";

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, builder =>
                   builder.AddInterceptors(new TenantSaveChangesInterceptor(tenantProvider.Object, options))))
        {
            context.TenantEntities.Add(new TestTenantEntity { Name = "Item", TenantId = "tenant-original" });
            context.SaveChanges();
        }

        tenantProvider.Setup(x => x.TenantId).Returns("tenant-other");

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, builder =>
                   builder.AddInterceptors(new TenantSaveChangesInterceptor(tenantProvider.Object, options))))
        {
            TestTenantEntity entity = context.TenantEntities.Single();
            entity.Name = "Updated";
            entity.TenantId = "tenant-other";
            context.SaveChanges();
        }

        using (TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName))
        {
            TestTenantEntity entity = context.TenantEntities.Single();
            entity.TenantId.Should().Be("tenant-original");
            entity.Name.Should().Be("Updated");
        }
    }
}
