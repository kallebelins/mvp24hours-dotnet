using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class QueryTrackingExtensionsTest
{
    [Fact]
    public async Task AsNoTracking_ShouldNotTrackEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> entities = await QueryTrackingExtensions
            .AsNoTracking(context.Entities)
            .ToListAsync();

        entities.Should().HaveCount(2);
        foreach (TestEntity entity in entities)
        {
            context.Entry(entity).State.Should().Be(EntityState.Detached);
        }
    }

    [Fact]
    public async Task AsTracking_ShouldTrackEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        await SeedAsync(context);

        List<TestEntity> entities = await QueryTrackingExtensions
            .AsTracking(context.Entities)
            .ToListAsync();

        entities.Should().NotBeEmpty();
        foreach (TestEntity entity in entities)
        {
            context.Entry(entity).State.Should().Be(EntityState.Unchanged);
        }
    }

    [Fact]
    public async Task WithTracking_NoTracking_ShouldDetachEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> entities = await context.Entities
            .WithTracking(QueryTrackingBehavior.NoTracking)
            .ToListAsync();

        entities.Should().NotBeEmpty();
        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    [Fact]
    public async Task WithTracking_TrackAll_ShouldTrackEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        await SeedAsync(context);

        List<TestEntity> entities = await context.Entities
            .WithTracking(QueryTrackingBehavior.TrackAll)
            .ToListAsync();

        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Unchanged);
    }

    [Fact]
    public async Task WithTracking_NoTrackingWithIdentityResolution_ShouldDetachEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> entities = await context.Entities
            .WithTracking(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            .ToListAsync();

        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    [Fact]
    public async Task AsNoTrackingIf_WhenTrue_ShouldNotTrack()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> entities = await context.Entities
            .AsNoTrackingIf(true)
            .ToListAsync();

        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    [Fact]
    public async Task AsNoTrackingIf_WhenFalse_ShouldKeepDefaultTracking()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> entities = await context.Entities
            .AsNoTrackingIf(false)
            .ToListAsync();

        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Unchanged);
    }

    [Fact]
    public async Task OptimizeForReading_WithoutIncludes_ShouldNotTrack()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> entities = await context.Entities
            .OptimizeForReading(hasIncludes: false)
            .ToListAsync();

        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    [Fact]
    public async Task OptimizeForReading_WithIncludes_ShouldNotTrack()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> entities = await context.Entities
            .OptimizeForReading(hasIncludes: true)
            .ToListAsync();

        entities.Should().OnlyContain(e => context.Entry(e).State == EntityState.Detached);
    }

    private static async Task SeedAsync(TestDbContext context)
    {
        context.Entities.AddRange(
            new TestEntity { Name = "Tracked-1", Active = true, Score = 10 },
            new TestEntity { Name = "Tracked-2", Active = false, Score = 20 });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
