using Microsoft.EntityFrameworkCore;
using Moq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class InterceptorEdgeCasesTest
{
    [Fact]
    public async Task SoftDeleteInterceptor_SaveChangesAsync_UsesDefaultUserWhenProviderMissing()
    {
        var interceptor = new SoftDeleteInterceptor();
        string databaseName = $"SoftDeleteAsync_{Guid.NewGuid():N}";

        using (GenericSoftDeleteTestDbContext seedContext = CreateGenericSoftDeleteContext(databaseName, interceptor))
        {
            seedContext.GenericSoftDeleteEntities.Add(new TestGenericSoftDeleteEntity { Name = "AsyncDelete" });
            await seedContext.SaveChangesAsync();
        }

        using (GenericSoftDeleteTestDbContext deleteContext = CreateGenericSoftDeleteContext(databaseName, interceptor))
        {
            TestGenericSoftDeleteEntity entity = deleteContext.GenericSoftDeleteEntities.Single();
            deleteContext.GenericSoftDeleteEntities.Remove(entity);
            await deleteContext.SaveChangesAsync();
        }

        using GenericSoftDeleteTestDbContext verifyContext = CreateGenericSoftDeleteContext(databaseName, interceptor);
        TestGenericSoftDeleteEntity deleted = verifyContext.GenericSoftDeleteEntities
            .IgnoreQueryFilters()
            .Single();

        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedBy.Should().Be("System");
        deleted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public void TenantSaveChangesInterceptor_OnDelete_WithCrossTenantValidation_ShouldThrow()
    {
        Mock<ITenantProvider> tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-a");
        var options = new TenantInterceptorOptions
        {
            RequireTenant = false,
            ValidateTenantOnAdd = false,
            ValidateTenantOnModify = false,
            ValidateTenantOnDelete = true,
            PreventTenantIdChange = false
        };
        string databaseName = $"TenantDelete_{Guid.NewGuid():N}";

        using (TestDbContext seedContext = EfCoreTestHelpers.CreateContext(databaseName, builder =>
                   builder.AddInterceptors(new TenantSaveChangesInterceptor(tenantProvider.Object, options))))
        {
            seedContext.TenantEntities.Add(new TestTenantEntity { Name = "Item", TenantId = "tenant-a" });
            seedContext.SaveChanges();
        }

        tenantProvider.Setup(x => x.TenantId).Returns("tenant-b");

        using TestDbContext deleteContext = EfCoreTestHelpers.CreateContext(databaseName, builder =>
            builder.AddInterceptors(new TenantSaveChangesInterceptor(tenantProvider.Object, options)));

        TestTenantEntity entity = deleteContext.TenantEntities.Single();
        deleteContext.TenantEntities.Remove(entity);

        Action act = () => deleteContext.SaveChanges();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cross-tenant*");
    }

    [Fact]
    public void TenantSaveChangesInterceptor_WhenTenantRequiredAndMissing_ShouldThrowOnAdd()
    {
        Mock<ITenantProvider> tenantProvider = new();
        tenantProvider.Setup(x => x.TenantId).Returns(string.Empty);
        tenantProvider.Setup(x => x.HasTenant).Returns(false);

        var options = new TenantInterceptorOptions
        {
            RequireTenant = true,
            ValidateTenantOnAdd = false,
            ValidateTenantOnModify = false,
            ValidateTenantOnDelete = false
        };
        var interceptor = new TenantSaveChangesInterceptor(tenantProvider.Object, options);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: builder =>
            builder.AddInterceptors(interceptor));

        context.TenantEntities.Add(new TestTenantEntity { Name = "NoTenant" });

        Action act = () => context.SaveChanges();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No tenant context is set*");
    }

    [Fact]
    public async Task AuditSaveChangesInterceptor_SaveChangesAsync_OnAdd_UsesDefaultUserAndSetsEntityDateLog()
    {
        var interceptor = new AuditSaveChangesInterceptor(defaultUser: "SystemAudit");
        string databaseName = $"AuditAsync_{Guid.NewGuid():N}";

        await using TestDbContext context = EfCoreTestHelpers.CreateContext(databaseName, options =>
            options.AddInterceptors(interceptor));

        var entity = new TestLogEntity { Name = "Logged" };
        context.LogEntities.Add(entity);
        await context.SaveChangesAsync();

        entity.Created.Should().NotBe(default);
        entity.Modified.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrencyInterceptor_SaveChangesAsync_OnModify_IncrementsVersion()
    {
        var interceptor = new ConcurrencyInterceptor();
        string databaseName = $"VersionAsync_{Guid.NewGuid():N}";

        using (TestDbContext seedContext = EfCoreTestHelpers.CreateContext(databaseName, options =>
                   options.AddInterceptors(interceptor)))
        {
            seedContext.VersionedEntities.Add(new TestVersionedEntity { Name = "Original" });
            await seedContext.SaveChangesAsync();
        }

        await using TestDbContext modifyContext = EfCoreTestHelpers.CreateContext(databaseName, options =>
            options.AddInterceptors(interceptor));

        TestVersionedEntity entity = modifyContext.VersionedEntities.Single();
        entity.Name = "Updated";
        await modifyContext.SaveChangesAsync();

        entity.Version.Should().Be(2);
    }

    private static GenericSoftDeleteTestDbContext CreateGenericSoftDeleteContext(
        string databaseName,
        SoftDeleteInterceptor interceptor)
    {
        DbContextOptionsBuilder<GenericSoftDeleteTestDbContext> optionsBuilder =
            new DbContextOptionsBuilder<GenericSoftDeleteTestDbContext>()
                .UseInMemoryDatabase(databaseName)
                .AddInterceptors(interceptor);

        var context = new GenericSoftDeleteTestDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class GenericSoftDeleteTestDbContext(DbContextOptions options) : Mvp24HoursContext(options)
    {
        public DbSet<TestGenericSoftDeleteEntity> GenericSoftDeleteEntities => Set<TestGenericSoftDeleteEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplySoftDeleteGlobalFilter();
        }
    }

    private sealed class TestGenericSoftDeleteEntity : EntityBase<int>, ISoftDeletable<string>
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeletedBy { get; set; } = string.Empty;
    }
}
