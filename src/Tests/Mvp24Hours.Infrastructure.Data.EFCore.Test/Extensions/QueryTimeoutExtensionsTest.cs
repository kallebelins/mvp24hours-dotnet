using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class QueryTimeoutExtensionsTest
{
    private static TestDbContext CreateSqliteContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"Data Source=file:timeout_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;
        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void Constants_ShouldMatchDocumentedDefaults()
    {
        QueryTimeoutExtensions.DefaultReadTimeoutSeconds.Should().Be(30);
        QueryTimeoutExtensions.DefaultWriteTimeoutSeconds.Should().Be(60);
        QueryTimeoutExtensions.DefaultBulkTimeoutSeconds.Should().Be(120);
        QueryTimeoutExtensions.DefaultReportTimeoutSeconds.Should().Be(300);
    }

    [Fact]
    public async Task WithTimeoutAsync_ShouldSetTimeoutDuringActionAndRestoreAfter()
    {
        await using TestDbContext context = CreateSqliteContext();
        await SeedAsync(context);
        context.Database.SetCommandTimeout(15);
        int? timeoutDuringAction = null;

        int count = await context.WithTimeoutAsync(90, async () =>
        {
            timeoutDuringAction = context.Database.GetCommandTimeout();
            return await context.Entities.CountAsync();
        });

        count.Should().Be(2);
        timeoutDuringAction.Should().Be(90);
        context.Database.GetCommandTimeout().Should().Be(15);
    }

    [Fact]
    public async Task WithTimeoutAsync_VoidOverload_ShouldRestoreTimeout()
    {
        await using TestDbContext context = CreateSqliteContext();
        await SeedAsync(context);
        context.Database.SetCommandTimeout(20);
        int? timeoutDuringAction = null;

        await context.WithTimeoutAsync(75, async () =>
        {
            timeoutDuringAction = context.Database.GetCommandTimeout();
            await context.Entities.AnyAsync();
        });

        timeoutDuringAction.Should().Be(75);
        context.Database.GetCommandTimeout().Should().Be(20);
    }

    [Fact]
    public async Task WithBulkTimeoutAsync_ShouldUseBulkDefaultAndRestore()
    {
        await using TestDbContext context = CreateSqliteContext();
        await SeedAsync(context);
        context.Database.SetCommandTimeout(10);
        int? timeoutDuringAction = null;

        int count = await context.WithBulkTimeoutAsync(async () =>
        {
            timeoutDuringAction = context.Database.GetCommandTimeout();
            return await context.Entities.CountAsync();
        });

        count.Should().Be(2);
        timeoutDuringAction.Should().Be(QueryTimeoutExtensions.DefaultBulkTimeoutSeconds);
        context.Database.GetCommandTimeout().Should().Be(10);
    }

    [Fact]
    public async Task ToListWithTimeoutAsync_ShouldReturnResultsAndRestoreTimeout()
    {
        await using TestDbContext context = CreateSqliteContext();
        await SeedAsync(context);
        context.Database.SetCommandTimeout(25);

        List<TestEntity> results = await context.Entities
            .OrderBy(e => e.Name)
            .ToListWithTimeoutAsync(context, 55);

        results.Should().HaveCount(2);
        results.Select(e => e.Name).Should().Equal("Alpha", "Beta");
        context.Database.GetCommandTimeout().Should().Be(25);
    }

    [Fact]
    public async Task CountWithTimeoutAsync_ShouldReturnCountAndRestoreTimeout()
    {
        await using TestDbContext context = CreateSqliteContext();
        await SeedAsync(context);
        context.Database.SetCommandTimeout(18);

        int count = await context.Entities
            .Where(e => e.Active)
            .CountWithTimeoutAsync(context, 40);

        count.Should().Be(1);
        context.Database.GetCommandTimeout().Should().Be(18);
    }

    [Fact]
    public async Task SaveChangesWithWriteTimeoutAsync_ShouldPersistAndRestoreTimeout()
    {
        await using TestDbContext context = CreateSqliteContext();
        context.Database.SetCommandTimeout(12);
        context.Entities.Add(new TestEntity { Name = "Persisted", Active = true, Score = 5 });

        int affected = await context.SaveChangesWithWriteTimeoutAsync();

        affected.Should().Be(1);
        (await context.Entities.CountAsync()).Should().Be(1);
        context.Database.GetCommandTimeout().Should().Be(12);
    }

    [Fact]
    public async Task WithTimeoutAsync_OnInMemory_ShouldThrowRelationalProviderRequired()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();

        Func<Task> act = async () => await context.WithTimeoutAsync(30, async () =>
        {
            await Task.CompletedTask;
            return 1;
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*relational*");
    }

    private static async Task SeedAsync(TestDbContext context)
    {
        context.Entities.AddRange(
            new TestEntity { Name = "Alpha", Active = true, Score = 10 },
            new TestEntity { Name = "Beta", Active = false, Score = 20 });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }
}
