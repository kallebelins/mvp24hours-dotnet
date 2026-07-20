using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class Mvp24HoursContextTest
{
    [Fact]
    public void SaveChanges_WhenCanApplyEntityLog_ShouldStampCreatedOnTestLogEntity()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var entity = new TestLogEntity { Name = "CreatedStamp" };

        context.LogEntities.Add(entity);
        context.SaveChanges();

        entity.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        entity.Modified.Should().BeNull();
        entity.Removed.Should().BeNull();
    }

    [Fact]
    public void SaveChanges_WhenModifiedAndRemovedIsNull_ShouldStampModified()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var entity = new TestLogEntity { Name = "Before" };
        context.LogEntities.Add(entity);
        context.SaveChanges();

        entity.Name = "After";
        context.SaveChanges();

        entity.Modified.Should().NotBeNull();
        entity.Modified.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void SaveChanges_WhenRemovedIsNotNull_ShouldNotStampModified()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var entity = new TestLogEntity { Name = "SoftDelete" };
        context.LogEntities.Add(entity);
        context.SaveChanges();

        DateTime? modifiedBefore = entity.Modified;
        entity.Name = "SoftDeleted";
        entity.Removed = DateTime.UtcNow;
        context.SaveChanges();

        entity.Modified.Should().Be(modifiedBefore);
    }

    [Fact]
    public void GlobalFilter_ShouldExcludeEntitiesWithRemovedSet()
    {
        string databaseName = $"Filter_{Guid.NewGuid():N}";
        using (TestDbContext seed = EfCoreTestHelpers.CreateContext(databaseName))
        {
            var active = new TestLogEntity { Name = "Active" };
            var removed = new TestLogEntity { Name = "Removed" };
            seed.LogEntities.AddRange(active, removed);
            seed.SaveChanges();

            removed.Removed = DateTime.UtcNow;
            seed.SaveChanges();
        }

        using TestDbContext query = EfCoreTestHelpers.CreateContext(databaseName);

        query.LogEntities.Should().ContainSingle(e => e.Name == "Active");
        query.LogEntities.IgnoreQueryFilters().Should().HaveCount(2);
    }

    [Fact]
    public void SaveChanges_WhenCanApplyEntityLogIsFalse_ShouldNotStampDates()
    {
        var options = new DbContextOptionsBuilder<TestDbContextNoLog>()
            .UseInMemoryDatabase($"NoLog_{Guid.NewGuid():N}")
            .Options;
        using var context = new TestDbContextNoLog(options);
        context.Database.EnsureCreated();

        var entity = new TestLogEntity { Name = "NoStamp" };
        context.LogEntities.Add(entity);
        context.SaveChanges();

        entity.Created.Should().Be(default);
        entity.Modified.Should().BeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldApplyLogRules()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var entity = new TestLogEntity { Name = "AsyncStamp" };

        context.LogEntities.Add(entity);
        await context.SaveChangesAsync();

        entity.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SaveChangesAsync_WithAcceptAllChanges_ShouldApplyLogRules()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var entity = new TestLogEntity { Name = "AsyncAccept" };

        context.LogEntities.Add(entity);
        await context.SaveChangesAsync(acceptAllChangesOnSuccess: true);

        entity.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
        entity.Modified.Should().BeNull();
    }

    [Fact]
    public void SaveChanges_WithEntityLogBy_ShouldStampCreatedByAndModifiedBy()
    {
        var options = new DbContextOptionsBuilder<TestDbContextWithUser>()
            .UseInMemoryDatabase($"WithUser_{Guid.NewGuid():N}")
            .Options;
        using var context = new TestDbContextWithUser(options, entityLogBy: "user-42");
        context.Database.EnsureCreated();

        var entity = new TestEntityLog { Name = "Audited" };
        context.EntityLogs.Add(entity);
        context.SaveChanges();

        entity.CreatedBy.Should().Be("user-42");
        entity.ModifiedBy.Should().BeNull();

        entity.Name = "Audited-Updated";
        context.SaveChanges();

        entity.ModifiedBy.Should().Be("user-42");
        entity.Modified.Should().NotBeNull();
    }
}
