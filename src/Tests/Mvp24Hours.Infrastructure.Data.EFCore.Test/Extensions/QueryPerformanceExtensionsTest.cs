using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class QueryPerformanceExtensionsTest
{
    [Fact]
    public async Task AsSplitQueryIf_WhenTrue_ShouldStillExecute()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> results = await context.Entities
            .AsSplitQueryIf(true)
            .ToListAsync();

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task AsSplitQueryIf_WhenFalse_ShouldStillExecute()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> results = await context.Entities
            .AsSplitQueryIf(false)
            .ToListAsync();

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task TagWithCallerInfo_ShouldAllowQueryExecution()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> results = await context.Entities
            .TagWithCallerInfo("UnitTest")
            .Where(e => e.Active)
            .ToListAsync();

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task TagWithMany_ShouldAllowQueryExecution()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> results = await context.Entities
            .TagWithMany("TagA", "TagB", "TagC")
            .ToListAsync();

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task TagWithIf_WhenTrue_ShouldAllowQueryExecution()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> results = await context.Entities
            .TagWithIf(true, "ConditionalTag")
            .ToListAsync();

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task TagWithIf_WhenFalse_ShouldAllowQueryExecution()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> results = await context.Entities
            .TagWithIf(false, "SkippedTag")
            .ToListAsync();

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task IgnoreQueryFiltersIf_WhenTrue_ShouldIncludeSoftDeletedLogEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedLogEntitiesAsync(context);

        List<TestLogEntity> filtered = await context.LogEntities.ToListAsync();
        List<TestLogEntity> all = await context.LogEntities
            .IgnoreQueryFiltersIf(true)
            .ToListAsync();

        filtered.Should().HaveCount(1);
        filtered.Single().Name.Should().Be("Active");
        all.Should().HaveCount(2);
        all.Select(e => e.Name).Should().BeEquivalentTo("Active", "Removed");
    }

    [Fact]
    public async Task IgnoreQueryFiltersIf_WhenFalse_ShouldKeepSoftDeleteFilter()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedLogEntitiesAsync(context);

        List<TestLogEntity> results = await context.LogEntities
            .IgnoreQueryFiltersIf(false)
            .ToListAsync();

        results.Should().HaveCount(1);
        results.Single().Name.Should().Be("Active");
    }

    [Fact]
    public async Task OptimizeForPaging_ShouldSkipAndTake()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> page = await context.Entities
            .OrderBy(e => e.Id)
            .OptimizeForPaging(skip: 1, take: 1, operationTag: "PageEntities")
            .ToListAsync();

        page.Should().HaveCount(1);
        page.Single().Name.Should().Be("B");
        context.Entry(page.Single()).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task OptimizeForCount_ShouldAllowCount()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        int count = await context.Entities
            .Where(e => e.Active)
            .OptimizeForCount("CountActive")
            .CountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task OptimizeForSingleLookup_WhenNotForUpdate_ShouldNotTrack()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        TestEntity? entity = await context.Entities
            .OptimizeForSingleLookup(forUpdate: false, operationTag: "GetByName")
            .FirstOrDefaultAsync(e => e.Name == "A");

        entity.Should().NotBeNull();
        context.Entry(entity!).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task OptimizeForSingleLookup_WhenForUpdate_ShouldTrack()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        TestEntity? entity = await context.Entities
            .OptimizeForSingleLookup(forUpdate: true)
            .FirstOrDefaultAsync(e => e.Name == "A");

        entity.Should().NotBeNull();
        context.Entry(entity!).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    public async Task OptimizeForReadPerformance_ShouldNotTrackAndExecute()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedEntitiesAsync(context);

        List<TestEntity> withoutIncludes = await context.Entities
            .OptimizeForReadPerformance(hasCollectionIncludes: false, operationTag: "ReadAll")
            .ToListAsync();

        List<TestEntity> withIncludes = await context.Entities
            .OptimizeForReadPerformance(hasCollectionIncludes: true, operationTag: "ReadWithIncludes")
            .ToListAsync();

        withoutIncludes.Should().HaveCount(3);
        withoutIncludes.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
        withIncludes.Should().HaveCount(3);
        withIncludes.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    private static async Task SeedEntitiesAsync(TestDbContext context)
    {
        context.Entities.AddRange(
            new TestEntity { Name = "A", Active = true, Score = 10 },
            new TestEntity { Name = "B", Active = false, Score = 20 },
            new TestEntity { Name = "C", Active = true, Score = 30 });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task SeedLogEntitiesAsync(TestDbContext context)
    {
        // CanApplyEntityLog clears Removed on Added — soft-delete after insert.
        context.LogEntities.AddRange(
            new TestLogEntity { Name = "Active" },
            new TestLogEntity { Name = "Removed" });
        await context.SaveChangesAsync();

        TestLogEntity removed = await context.LogEntities.SingleAsync(e => e.Name == "Removed");
        removed.Removed = DateTime.UtcNow;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
