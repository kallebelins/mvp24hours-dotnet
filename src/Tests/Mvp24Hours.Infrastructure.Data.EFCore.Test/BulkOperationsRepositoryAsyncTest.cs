using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task BulkInsertAsync_WithNullOptions_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        Func<Task> act = () => repository.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(1), null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkUpdateAsync_WithNullOptions_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        Func<Task> act = () => repository.BulkUpdateAsync(EfCoreTestHelpers.CreateEntities(1), null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkDeleteAsync_WithNullOptions_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        Func<Task> act = () => repository.BulkDeleteAsync(EfCoreTestHelpers.CreateEntities(1), null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task BulkInsertAsync_WithBypassChangeTrackingDisabled_ShouldInsertViaTracking()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();
        var options = new BulkOperationOptions { BypassChangeTracking = false, BatchSize = 5 };

        BulkOperationResult result = await repository.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(5, "Tracked"), options);

        result.IsSuccess.Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(5);
    }

    [Fact]
    public async Task BulkUpdateAsync_WithBypassChangeTrackingDisabled_ShouldUpdateViaTracking()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        await repository.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(3, "TrackUpdate"));
        IList<TestEntity> items = await repository.ListAsync();
        foreach (TestEntity entity in items)
        {
            entity.Score = 999;
        }

        BulkOperationResult result = await repository.BulkUpdateAsync([.. items], new BulkOperationOptions { BypassChangeTracking = false });

        result.IsSuccess.Should().BeTrue();
        (await repository.ListAsync()).Should().OnlyContain(e => e.Score == 999);
    }

    [Fact]
    public async Task BulkDeleteAsync_WithBypassChangeTrackingDisabled_ShouldDeleteViaTracking()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        await repository.BulkInsertAsync(EfCoreTestHelpers.CreateEntities(4, "TrackDelete"));
        IList<TestEntity> items = await repository.ListAsync();

        BulkOperationResult result = await repository.BulkDeleteAsync([.. items], new BulkOperationOptions { BypassChangeTracking = false });

        result.IsSuccess.Should().BeTrue();
        (await repository.ListCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_WithNullPredicate_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        Func<Task> act = () => repository.ExecuteUpdateAsync(null!, e => e.Score, 0);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteDeleteAsync_WithNullPredicate_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        Func<Task> act = () => repository.ExecuteDeleteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact(Skip = "InMemory provider does not support ExecuteUpdate/ExecuteDelete. See BulkOperationsIntegrationTest in Application.Integration.Test.")]
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

    [Fact(Skip = "InMemory provider does not support ExecuteUpdate/ExecuteDelete. See BulkOperationsIntegrationTest in Application.Integration.Test.")]
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

    [Fact]
    public async Task ExecuteUpdateAsync_WithNullSetPropertyCalls_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        Func<Task> act = () => repository.ExecuteUpdateAsync(
            e => e.Active,
            (Expression<Func<SetPropertyCalls<TestEntity>, SetPropertyCalls<TestEntity>>>)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteUpdateAsync_MultiProperty_WithNullPredicate_ShouldThrow()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        Func<Task> act = () => repository.ExecuteUpdateAsync(
            null!,
            setters => setters.SetProperty(e => e.Score, 1));

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteUpdateAsync_MultiProperty_WithNoSetters_ShouldReturnZeroWithoutQuerying()
    {
        using IServiceScope scope = _provider.CreateScope();
        IBulkOperationsRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IBulkOperationsRepositoryAsync<TestEntity>>();

        int rowsAffected = await repository.ExecuteUpdateAsync(
            e => e.Active,
            setters => setters);

        rowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_MultiProperty_WithConstantAndExpressionSetters_ShouldUpdateOnRealProvider()
    {
        // EF Core's InMemory provider does not translate ExecuteUpdate; a real relational
        // provider (Sqlite, in-process) is required to exercise the SetPropertyCalls ->
        // EF Core UpdateSettersBuilder reflection bridge end-to-end.
        string connectionString = $"Data Source=file:bulk_multi_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAlive = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
        keepAlive.Open();
        try
        {
            DbContextOptions<TestDbContext> dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(keepAlive)
                .Options;
            await using var context = new TestDbContext(dbOptions);
            await context.Database.EnsureCreatedAsync();

            context.Entities.AddRange(EfCoreTestHelpers.CreateEntities(3, "SqliteMulti").Select(e =>
            {
                e.Active = true;
                return e;
            }));
            await context.SaveChangesAsync();

            var repository = new BulkOperationsRepositoryAsync<TestEntity>(
                context,
                EfCoreTestHelpers.CreateRepositoryOptions(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<BulkOperationsRepositoryAsync<TestEntity>>.Instance);

            int rowsAffected = await repository.ExecuteUpdateAsync(
                e => e.Active,
                setters => setters
                    .SetProperty(e => e.Score, 42)
                    .SetProperty(e => e.Name, e => e.Name + "-Updated"));

            rowsAffected.Should().Be(3);
            List<TestEntity> updated = await context.Entities.AsNoTracking().ToListAsync();
            updated.Should().OnlyContain(e => e.Score == 42 && e.Name.EndsWith("-Updated"));
        }
        finally
        {
            keepAlive.Close();
        }
    }
}
