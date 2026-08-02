using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class ModelBuilderExtensionsTest
{
    [Fact]
    public void ApplyGlobalFilters_ISoftDeletable_ExcludesDeletedEntities()
    {
        string databaseName = $"GlobalFilter_Soft_{Guid.NewGuid():N}";

        using (SoftDeleteFilterDbContext context = CreateSoftDeleteFilterContext(databaseName))
        {
            context.SoftDeleteEntities.AddRange(
                new TestSoftDeleteEntity { Name = "Active", IsDeleted = false },
                new TestSoftDeleteEntity { Name = "Deleted", IsDeleted = true });
            context.SaveChanges();
        }

        using (SoftDeleteFilterDbContext context = CreateSoftDeleteFilterContext(databaseName))
        {
            var results = context.SoftDeleteEntities.ToList();

            results.Should().ContainSingle();
            results[0].Name.Should().Be("Active");

            context.SoftDeleteEntities.IgnoreQueryFilters().Count().Should().Be(2);
        }
    }

    [Fact]
    public void ApplyGlobalFilters_IEntityDateLog_ExcludesRemovedEntities()
    {
        string databaseName = $"GlobalFilter_DateLog_{Guid.NewGuid():N}";

        using (DateLogFilterDbContext context = CreateDateLogFilterContext(databaseName))
        {
            context.LogEntities.AddRange(
                new TestLogEntity { Name = "Active", Created = DateTime.UtcNow, Removed = null },
                new TestLogEntity { Name = "Removed", Created = DateTime.UtcNow, Removed = DateTime.UtcNow });
            context.SaveChanges();
        }

        using (DateLogFilterDbContext context = CreateDateLogFilterContext(databaseName))
        {
            var results = context.LogEntities.ToList();

            results.Should().ContainSingle();
            results[0].Name.Should().Be("Active");

            context.LogEntities.IgnoreQueryFilters().Count().Should().Be(2);
        }
    }

    private static SoftDeleteFilterDbContext CreateSoftDeleteFilterContext(string databaseName)
    {
        DbContextOptions<SoftDeleteFilterDbContext> options = new DbContextOptionsBuilder<SoftDeleteFilterDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new SoftDeleteFilterDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static DateLogFilterDbContext CreateDateLogFilterContext(string databaseName)
    {
        DbContextOptions<DateLogFilterDbContext> options = new DbContextOptionsBuilder<DateLogFilterDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new DateLogFilterDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class SoftDeleteFilterDbContext(DbContextOptions options) : TestDbContext(options)
    {

        // Avoid Mvp24HoursContext ApplyLogRules / default filters; only soft-delete filter under test.
        public override bool CanApplyEntityLog => false;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyGlobalFilters<ISoftDeletable>(e => !e.IsDeleted);
        }
    }

    /// <summary>
    /// Plain DbContext so Mvp24HoursContext.ApplyLogRules does not clear Removed on insert.
    /// </summary>
    private sealed class DateLogFilterDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<TestLogEntity> LogEntities => Set<TestLogEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestLogEntity>();
            modelBuilder.ApplyGlobalFilters<IEntityDateLog>(e => e.Removed == null);
        }
    }
}
