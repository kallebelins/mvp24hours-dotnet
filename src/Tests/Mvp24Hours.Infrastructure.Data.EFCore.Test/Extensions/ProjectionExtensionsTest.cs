using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class ProjectionExtensionsTest
{
    private sealed class EntityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
    }

    private sealed class GroupDto
    {
        public bool Active { get; set; }
        public int Count { get; set; }
        public int TotalScore { get; set; }
    }

    [Fact]
    public async Task ProjectTo_ShouldSelectProjectedProperties()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<EntityDto> results = await context.Entities
            .ProjectTo(e => new EntityDto { Id = e.Id, Name = e.Name, Score = e.Score })
            .ToListAsync();

        results.Should().HaveCount(3);
        results.Should().OnlyContain(d => !string.IsNullOrEmpty(d.Name));
    }

    [Fact]
    public async Task ProjectToListAsync_ShouldReturnProjectedList()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        IList<EntityDto> results = await context.Entities
            .Where(e => e.Active)
            .ProjectToListAsync(e => new EntityDto { Id = e.Id, Name = e.Name, Score = e.Score });

        results.Should().HaveCount(2);
        results.Select(r => r.Name).Should().BeEquivalentTo("A", "C");
    }

    [Fact]
    public async Task ProjectToSingleAsync_ShouldReturnSingleOrNull()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        EntityDto? found = await context.Entities
            .Where(e => e.Name == "B")
            .ProjectToSingleAsync(e => new EntityDto { Id = e.Id, Name = e.Name, Score = e.Score });

        EntityDto? missing = await context.Entities
            .Where(e => e.Name == "Missing")
            .ProjectToSingleAsync(e => new EntityDto { Id = e.Id, Name = e.Name, Score = e.Score });

        found.Should().NotBeNull();
        found!.Name.Should().Be("B");
        found.Score.Should().Be(20);
        missing.Should().BeNull();
    }

    [Fact]
    public async Task ProjectToCountAsync_ShouldReturnCount()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        int count = await context.Entities
            .Where(e => e.Active)
            .ProjectToCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task ProjectToExistsAsync_ShouldReturnExistence()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        bool exists = await context.Entities.Where(e => e.Score > 15).ProjectToExistsAsync();
        bool missing = await context.Entities.Where(e => e.Score > 1000).ProjectToExistsAsync();

        exists.Should().BeTrue();
        missing.Should().BeFalse();
    }

    [Fact]
    public async Task ProjectToSumAsync_ShouldSumDecimalProperty()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        decimal sum = await context.Entities.ProjectToSumAsync(e => (decimal)e.Score);

        sum.Should().Be(60m);
    }

    [Fact]
    public async Task ProjectToAverageAsync_ShouldAverageDecimalProperty()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        decimal average = await context.Entities.ProjectToAverageAsync(e => (decimal)e.Score);

        average.Should().Be(20m);
    }

    [Fact]
    public async Task ProjectToMaxAsync_ShouldReturnMaximum()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        int max = await context.Entities.ProjectToMaxAsync(e => e.Score);

        max.Should().Be(30);
    }

    [Fact]
    public async Task ProjectToMinAsync_ShouldReturnMinimum()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        int min = await context.Entities.ProjectToMinAsync(e => e.Score);

        min.Should().Be(10);
    }

    [Fact]
    public async Task ProjectToGroupedAsync_ShouldGroupResults()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        IList<GroupDto> groups = await context.Entities.ProjectToGroupedAsync(
            e => e.Active,
            (active, items) => new GroupDto
            {
                Active = active,
                Count = items.Count(),
                TotalScore = items.Sum(i => i.Score)
            });

        groups.Should().HaveCount(2);
        groups.Single(g => g.Active).Count.Should().Be(2);
        groups.Single(g => g.Active).TotalScore.Should().Be(40);
        groups.Single(g => !g.Active).Count.Should().Be(1);
        groups.Single(g => !g.Active).TotalScore.Should().Be(20);
    }

    [Fact]
    public async Task MapToListAsync_ShouldMapInMemory()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        IList<string> names = await context.Entities
            .OrderBy(e => e.Id)
            .MapToListAsync(e => e.Name.ToUpperInvariant());

        names.Should().Equal("A", "B", "C");
    }

    private static async Task SeedAsync(TestDbContext context)
    {
        context.Entities.AddRange(
            new TestEntity { Name = "A", Active = true, Score = 10 },
            new TestEntity { Name = "B", Active = false, Score = 20 },
            new TestEntity { Name = "C", Active = true, Score = 30 });
        await context.SaveChangesAsync();
    }
}
