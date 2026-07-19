using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class TenantSaveChangesInterceptorTest
{
    [Fact]
    public void SaveChanges_OnAdd_SetsTenantIdFromProvider()
    {
        var tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-abc");
        var options = new TenantInterceptorOptions
        {
            ValidateTenantOnAdd = false,
            ValidateTenantOnModify = false,
            ValidateTenantOnDelete = false
        };
        var interceptor = new TenantSaveChangesInterceptor(tenantProvider.Object, options);

        using var context = EfCoreTestHelpers.CreateContext(configure: builder =>
            builder.AddInterceptors(interceptor));

        var entity = new TestTenantEntity { Name = "Tenant Item" };
        context.TenantEntities.Add(entity);
        context.SaveChanges();

        entity.TenantId.Should().Be("tenant-abc");
    }

    [Fact]
    public void SaveChanges_OnModify_PreventTenantIdChange_KeepsOriginalTenantId()
    {
        var tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-original");
        var options = new TenantInterceptorOptions
        {
            ValidateTenantOnAdd = false,
            ValidateTenantOnModify = false,
            PreventTenantIdChange = true
        };
        var databaseName = $"Tenant_{Guid.NewGuid():N}";

        using (var context = EfCoreTestHelpers.CreateContext(databaseName, builder =>
                   builder.AddInterceptors(new TenantSaveChangesInterceptor(tenantProvider.Object, options))))
        {
            context.TenantEntities.Add(new TestTenantEntity { Name = "Item", TenantId = "tenant-original" });
            context.SaveChanges();
        }

        tenantProvider.Setup(x => x.TenantId).Returns("tenant-other");

        using (var context = EfCoreTestHelpers.CreateContext(databaseName, builder =>
                   builder.AddInterceptors(new TenantSaveChangesInterceptor(tenantProvider.Object, options))))
        {
            var entity = context.TenantEntities.Single();
            entity.Name = "Updated";
            entity.TenantId = "tenant-other";
            context.SaveChanges();
        }

        using (var context = EfCoreTestHelpers.CreateContext(databaseName))
        {
            var entity = context.TenantEntities.Single();
            entity.TenantId.Should().Be("tenant-original");
            entity.Name.Should().Be("Updated");
        }
    }
}
