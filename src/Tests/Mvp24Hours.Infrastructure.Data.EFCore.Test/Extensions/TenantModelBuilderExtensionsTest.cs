using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class TenantModelBuilderExtensionsTest
{
    [Fact]
    public void ApplyTenantQueryFilters_ReturnsOnlyMatchingTenantId()
    {
        var tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-a");
        var databaseName = $"TenantFilter_{Guid.NewGuid():N}";

        using (var context = CreateTenantContext(databaseName, tenantProvider.Object))
        {
            context.TenantEntities.AddRange(
                new TestTenantEntity { Name = "A1", TenantId = "tenant-a" },
                new TestTenantEntity { Name = "B1", TenantId = "tenant-b" },
                new TestTenantEntity { Name = "A2", TenantId = "tenant-a" });
            context.SaveChanges();
        }

        using (var context = CreateTenantContext(databaseName, tenantProvider.Object))
        {
            var results = context.TenantEntities.ToList();

            results.Should().HaveCount(2);
            results.Should().OnlyContain(e => e.TenantId == "tenant-a");
            context.TenantEntities.IgnoreQueryFilters().Count().Should().Be(3);
        }
    }

    [Fact]
    public void ApplyTenantQueryFilters_NullTenantId_BypassesFilter()
    {
        var tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-a");
        tenantProvider.Setup(x => x.TenantId).Returns((string)null!);
        tenantProvider.Setup(x => x.HasTenant).Returns(false);

        var databaseName = $"TenantNull_{Guid.NewGuid():N}";

        using (var context = CreateTenantContext(databaseName, tenantProvider.Object))
        {
            context.TenantEntities.AddRange(
                new TestTenantEntity { Name = "A1", TenantId = "tenant-a" },
                new TestTenantEntity { Name = "B1", TenantId = "tenant-b" });
            context.SaveChanges();
        }

        using (var context = CreateTenantContext(databaseName, tenantProvider.Object))
        {
            context.TenantEntities.Count().Should().Be(2);
        }
    }

    [Fact]
    public void ApplyTenantQueryFilters_EmptyTenantId_DoesNotMatchOtherTenants()
    {
        var tenantProvider = EfCoreTestHelpers.CreateTenantProvider(string.Empty);
        tenantProvider.Setup(x => x.HasTenant).Returns(false);

        var databaseName = $"TenantEmpty_{Guid.NewGuid():N}";

        using (var context = CreateTenantContext(databaseName, tenantProvider.Object))
        {
            context.TenantEntities.AddRange(
                new TestTenantEntity { Name = "Empty", TenantId = string.Empty },
                new TestTenantEntity { Name = "Other", TenantId = "tenant-b" });
            context.SaveChanges();
        }

        using (var context = CreateTenantContext(databaseName, tenantProvider.Object))
        {
            var results = context.TenantEntities.ToList();

            results.Should().ContainSingle();
            results[0].Name.Should().Be("Empty");
        }
    }

    [Fact]
    public void ApplyTenantQueryFilters_NullProvider_ThrowsArgumentNullException()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestTenantEntity>();

        Action act = () => modelBuilder.ApplyTenantQueryFilters(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("tenantProvider");
    }

    [Fact]
    public void ApplyTenantAndSoftDeleteFilters_FiltersByTenantAndExcludesDeleted()
    {
        var tenantProvider = EfCoreTestHelpers.CreateTenantProvider("tenant-a");
        var databaseName = $"TenantSoft_{Guid.NewGuid():N}";

        using (var context = CreateCombinedFilterContext(databaseName, tenantProvider.Object))
        {
            context.CombinedEntities.AddRange(
                new TenantSoftDeleteEntity { Name = "Keep", TenantId = "tenant-a", IsDeleted = false },
                new TenantSoftDeleteEntity { Name = "Deleted", TenantId = "tenant-a", IsDeleted = true },
                new TenantSoftDeleteEntity { Name = "OtherTenant", TenantId = "tenant-b", IsDeleted = false });
            context.SoftDeleteEntities.Add(
                new TestSoftDeleteEntity { Name = "SoftOnly", IsDeleted = true });
            context.SaveChanges();
        }

        using (var context = CreateCombinedFilterContext(databaseName, tenantProvider.Object))
        {
            context.CombinedEntities.ToList().Should().ContainSingle(e => e.Name == "Keep");
            context.SoftDeleteEntities.Count().Should().Be(0);
            context.CombinedEntities.IgnoreQueryFilters().Count().Should().Be(3);
        }
    }

    [Fact]
    public void ConfigureTenantProperties_SetsMaxLengthRequiredAndIndex()
    {
        var options = new DbContextOptionsBuilder<TenantConfigDbContext>()
            .UseInMemoryDatabase($"TenantConfig_{Guid.NewGuid():N}")
            .Options;

        using var context = new TenantConfigDbContext(options);
        context.Database.EnsureCreated();

        var entityType = context.Model.FindEntityType(typeof(TestTenantEntity));
        entityType.Should().NotBeNull();

        var tenantIdProperty = entityType!.FindProperty(nameof(ITenantEntity.TenantId));
        tenantIdProperty.Should().NotBeNull();
        tenantIdProperty!.GetMaxLength().Should().Be(100);
        tenantIdProperty.IsNullable.Should().BeFalse();

        entityType.GetIndexes()
            .Should()
            .Contain(i => i.GetDatabaseName() == "IX_TestTenantEntity_TenantId");
    }

    private static TenantFilterDbContext CreateTenantContext(string databaseName, ITenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<TenantFilterDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ReplaceService<IModelCacheKeyFactory, TenantAwareModelCacheKeyFactory>()
            .Options;

        var context = new TenantFilterDbContext(options, tenantProvider);
        context.Database.EnsureCreated();
        return context;
    }

    private static CombinedFilterDbContext CreateCombinedFilterContext(string databaseName, ITenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<CombinedFilterDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ReplaceService<IModelCacheKeyFactory, TenantAwareModelCacheKeyFactory>()
            .Options;

        var context = new CombinedFilterDbContext(options, tenantProvider);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class TenantAwareModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
        {
            return context switch
            {
                TenantFilterDbContext tenant => (typeof(TenantFilterDbContext), tenant.CacheKey, designTime),
                CombinedFilterDbContext combined => (typeof(CombinedFilterDbContext), combined.CacheKey, designTime),
                _ => (context.GetType(), designTime)
            };
        }
    }

    private sealed class TenantFilterDbContext : TestDbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public TenantFilterDbContext(DbContextOptions options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public string? CacheKey => _tenantProvider.TenantId ?? "__null__";

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyTenantQueryFilters(_tenantProvider);
        }
    }

    private sealed class CombinedFilterDbContext : TestDbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public CombinedFilterDbContext(DbContextOptions options, ITenantProvider tenantProvider)
            : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public string? CacheKey => _tenantProvider.TenantId ?? "__null__";

        public DbSet<TenantSoftDeleteEntity> CombinedEntities => Set<TenantSoftDeleteEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyTenantAndSoftDeleteFilters(_tenantProvider);
        }
    }

    private sealed class TenantConfigDbContext : TestDbContext
    {
        public TenantConfigDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ConfigureTenantProperties(maxLength: 100, isRequired: true, createIndex: true);
        }
    }

    public sealed class TenantSoftDeleteEntity : EntityBase<int>, ITenantEntity, ISoftDeletable
    {
        public string Name { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string DeletedBy { get; set; } = string.Empty;
    }
}
