using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class BulkOperationsExtensionsTest
{
    [Fact]
    public async Task BulkInsertAsync_ShouldInsertEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(5, "BulkExtInsert");

        BulkOperationResult result = await context.BulkInsertAsync(entities);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(5);
        (await context.Entities.CountAsync()).Should().Be(5);
    }

    [Fact]
    public async Task BulkInsertAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();

        BulkOperationResult result = await context.BulkInsertAsync(new List<TestEntity>());

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
        result.ElapsedTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task BulkUpdateAsync_ShouldUpdateEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await context.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(4, "BulkExtUpdate"));
        context.ChangeTracker.Clear();

        List<TestEntity> entities = await EntityFrameworkQueryableExtensions
            .AsNoTracking(context.Entities)
            .ToListAsync();
        foreach (TestEntity entity in entities)
        {
            entity.Name = $"Updated-{entity.Name}";
            entity.Score = 99;
        }

        BulkOperationResult result = await context.BulkUpdateAsync(entities);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().BeGreaterThan(0);
        (await EntityFrameworkQueryableExtensions.AsNoTracking(context.Entities).ToListAsync())
            .Should().OnlyContain(e => e.Name.StartsWith("Updated-") && e.Score == 99);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();

        BulkOperationResult result = await context.BulkUpdateAsync(new List<TestEntity>());

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteAsync_ShouldDeleteEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await context.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(3, "BulkExtDelete"));
        context.ChangeTracker.Clear();

        List<TestEntity> entities = await EntityFrameworkQueryableExtensions
            .AsNoTracking(context.Entities)
            .ToListAsync();

        BulkOperationResult result = await context.BulkDeleteAsync(entities);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().BeGreaterThan(0);
        (await context.Entities.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();

        BulkOperationResult result = await context.BulkDeleteAsync(new List<TestEntity>());

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteUpdate/ExecuteDelete")]
    [Trait("Category", "RequiresRealDatabase")]
    public async Task ExecuteUpdateAsync_ByProperty_ShouldUpdateMatchingEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        await context.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(5, "ExecUpdate"));

        int rowsAffected = await context.ExecuteUpdateAsync<TestEntity, int>(
            e => e.Active,
            e => e.Score,
            0);

        rowsAffected.Should().BeGreaterThan(0);
        (await context.Entities.CountAsync(e => e.Score == 0)).Should().BeGreaterThan(0);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteUpdate/ExecuteDelete")]
    [Trait("Category", "RequiresRealDatabase")]
    public async Task ExecuteDeleteAsync_ShouldDeleteMatchingEntities()
    {
        await using TestDbContext context = EfCoreTestHelpers.CreateContext();
        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(6, "ExecDelete");
        entities.ForEach(e => e.Active = false);
        await context.BulkInsertAsync(entities);

        int rowsAffected = await context.ExecuteDeleteAsync<TestEntity>(e => !e.Active);

        rowsAffected.Should().BeGreaterThan(0);
        (await context.Entities.CountAsync()).Should().Be(0);
    }
}
