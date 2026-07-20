using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class CompiledQueryExtensionsTest
{
    private static readonly Func<TestDbContext, IEnumerable<TestEntity>> GetAll =
        CompiledQueryExtensions.CompileGetAll<TestDbContext, TestEntity>();

    private static readonly Func<TestDbContext, IAsyncEnumerable<TestEntity>> GetAllAsync =
        CompiledQueryExtensions.CompileGetAllAsync<TestDbContext, TestEntity>();

    private static readonly Func<TestDbContext, int, int, IAsyncEnumerable<TestEntity>> GetPagedAsync =
        CompiledQueryExtensions.CompilePagedAsync<TestDbContext, TestEntity>();

    [Fact]
    public void CompileGetById_ShouldCreateDelegate()
    {
        Func<TestDbContext, int, TestEntity?> query =
            CompiledQueryExtensions.CompileGetById<TestDbContext, TestEntity, int>(e => e.Id);

        query.Should().NotBeNull();
    }

    [Fact]
    public async Task CompileGetById_WhenExecuted_ThrowsBecausePredicateIsBuiltViaMethodCall()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        Func<TestDbContext, int, TestEntity?> query =
            CompiledQueryExtensions.CompileGetById<TestDbContext, TestEntity, int>(e => e.Id);

        Action act = () => _ = query(context, 1);

        act.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public async Task CompileGetAll_ShouldReturnAllEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        List<TestEntity> results = [.. GetAll(context)];

        results.Should().HaveCount(3);
        results.Select(e => e.Name).Should().BeEquivalentTo("Compiled-1", "Compiled-2", "Compiled-3");
    }

    [Fact]
    public void CompileAny_ShouldCreateDelegate()
    {
        Func<TestDbContext, bool, bool> query =
            CompiledQueryExtensions.CompileAny<TestDbContext, TestEntity, bool>((e, active) => e.Active == active);

        query.Should().NotBeNull();
    }

    [Fact]
    public async Task CompileAny_WhenExecuted_ThrowsBecausePredicateUsesCompileInvocation()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        Func<TestDbContext, bool, bool> query =
            CompiledQueryExtensions.CompileAny<TestDbContext, TestEntity, bool>((e, active) => e.Active == active);

        Action act = () => _ = query(context, true);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CompileCount_ShouldCreateDelegate()
    {
        Func<TestDbContext, bool, int> query =
            CompiledQueryExtensions.CompileCount<TestDbContext, TestEntity, bool>((e, active) => e.Active == active);

        query.Should().NotBeNull();
    }

    [Fact]
    public async Task CompileCount_WhenExecuted_ThrowsBecausePredicateUsesCompileInvocation()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        Func<TestDbContext, bool, int> query =
            CompiledQueryExtensions.CompileCount<TestDbContext, TestEntity, bool>((e, active) => e.Active == active);

        Action act = () => _ = query(context, true);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CompileGetByIdAsync_ShouldCreateDelegate()
    {
        Func<TestDbContext, int, Task<TestEntity?>> query =
            CompiledQueryExtensions.CompileGetByIdAsync<TestDbContext, TestEntity, int>(e => e.Id);

        query.Should().NotBeNull();
    }

    [Fact]
    public async Task CompileGetByIdAsync_WhenExecuted_ThrowsBecausePredicateIsBuiltViaMethodCall()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        Func<TestDbContext, int, Task<TestEntity?>> query =
            CompiledQueryExtensions.CompileGetByIdAsync<TestDbContext, TestEntity, int>(e => e.Id);

        Func<Task> act = async () => _ = await query(context, 1);

        await act.Should().ThrowAsync<InvalidCastException>();
    }

    [Fact]
    public async Task CompileGetAllAsync_ShouldEnumerateAllEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        var results = new List<TestEntity>();
        await foreach (TestEntity entity in GetAllAsync(context))
        {
            results.Add(entity);
        }

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task CompilePagedAsync_ShouldSkipAndTake()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await SeedAsync(context);

        var page = new List<TestEntity>();
        await foreach (TestEntity entity in GetPagedAsync(context, 1, 1))
        {
            page.Add(entity);
        }

        page.Should().HaveCount(1);
    }

    private static async Task<List<TestEntity>> SeedAsync(TestDbContext context)
    {
        var entities = new List<TestEntity>
        {
            new() { Name = "Compiled-1", Active = true, Score = 10 },
            new() { Name = "Compiled-2", Active = false, Score = 20 },
            new() { Name = "Compiled-3", Active = true, Score = 30 }
        };
        context.Entities.AddRange(entities);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        return entities;
    }
}
