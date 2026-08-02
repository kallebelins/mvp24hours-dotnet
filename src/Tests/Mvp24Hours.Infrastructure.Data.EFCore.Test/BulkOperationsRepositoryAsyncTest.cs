using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class BulkOperationsRepositoryAsyncTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"Bulk_{Guid.NewGuid():N}";

    public BulkOperationsRepositoryAsyncTest()
    {
        _provider = EfCoreTestHelpers.CreateBulkServices(_databaseName);
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public async Task BulkInsertAsync_ShouldInsertAllEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(20, "BulkInsert");

        BulkOperationResult result = await repository.BulkInsertAsync(entities);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().BeGreaterThan(0);
        (await repository.ListCountAsync()).Should().Be(20);
    }

    [Fact]
    public async Task BulkInsertAsync_WithOptions_ShouldReportProgress()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(12, "BulkProgress");
        var progressReported = new List<(int processed, int total)>();
        var options = new BulkOperationOptions
        {
            BatchSize = 4,
            ProgressCallback = (processed, total) => progressReported.Add((processed, total))
        };

        BulkOperationResult result = await repository.BulkInsertAsync(entities, options);

        result.IsSuccess.Should().BeTrue();
        progressReported.Should().NotBeEmpty();
        progressReported.Last().total.Should().Be(12);
    }

    [Fact]
    public async Task BulkInsertAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        BulkOperationResult result = await repository.BulkInsertAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task BulkUpdateAsync_ShouldUpdateAllEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        await repository.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(8, "BulkUpdate"));
        IList<TestEntity> inserted = await repository.ListAsync();
        foreach (TestEntity entity in inserted)
        {
            entity.Name = $"Updated-{entity.Name}";
        }

        BulkOperationResult result = await repository.BulkUpdateAsync([.. inserted]);

        result.IsSuccess.Should().BeTrue();
        (await repository.ListAsync()).Should().OnlyContain(e => e.Name.StartsWith("Updated-"));
    }

    [Fact]
    public async Task BulkUpdateAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        BulkOperationResult result = await repository.BulkUpdateAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteAsync_ShouldDeleteAllEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        await repository.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(7, "BulkDelete"));
        IList<TestEntity> inserted = await repository.ListAsync();

        BulkOperationResult result = await repository.BulkDeleteAsync([.. inserted]);

        result.IsSuccess.Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithEmptyList_ShouldReturnSuccessWithZeroRows()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        BulkOperationResult result = await repository.BulkDeleteAsync([]);

        result.IsSuccess.Should().BeTrue();
        result.RowsAffected.Should().Be(0);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteUpdate/ExecuteDelete")]
    [Trait("Category", "RequiresRealDatabase")]
    public async Task ExecuteUpdateAsync_ShouldUpdateMatchingEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        await repository.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(5, "ExecuteUpdate"));

        int rowsAffected = await repository.ExecuteUpdateAsync(
            e => e.Active,
            e => e.Score,
            0);

        rowsAffected.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "InMemory provider does not support ExecuteUpdate/ExecuteDelete")]
    [Trait("Category", "RequiresRealDatabase")]
    public async Task ExecuteDeleteAsync_ShouldDeleteMatchingEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        List<TestEntity> entities = EfCoreTestHelpers.CreateEntities(6, "ExecuteDelete");
        entities.ForEach(e => e.Active = false);
        await repository.BulkInsertAsync(entities);

        int rowsAffected = await repository.ExecuteDeleteAsync(e => !e.Active);

        rowsAffected.Should().BeGreaterThan(0);
    }
}
