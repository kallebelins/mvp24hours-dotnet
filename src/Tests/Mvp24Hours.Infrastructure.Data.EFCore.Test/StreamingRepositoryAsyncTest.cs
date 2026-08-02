using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class StreamingRepositoryAsyncTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _databaseName = $"Stream_{Guid.NewGuid():N}";

    public StreamingRepositoryAsyncTest()
    {
        _provider = EfCoreTestHelpers.CreateStreamingServices(_databaseName);
        SeedData();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public async Task StreamAllAsync_ShouldYieldAllEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var streamed = new List<TestEntity>();
        await foreach (TestEntity entity in repository.StreamAllAsync())
        {
            streamed.Add(entity);
        }

        streamed.Should().HaveCount(8);
    }

    [Fact]
    public async Task StreamByAsync_ShouldYieldFilteredEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var streamed = new List<TestEntity>();
        await foreach (TestEntity entity in repository.StreamByAsync(e => e.Active))
        {
            streamed.Add(entity);
        }

        streamed.Should().HaveCount(4);
        streamed.Should().OnlyContain(e => e.Active);
    }

    [Fact]
    public async Task StreamBatchesAsync_ShouldReturnConfiguredBatchSizes()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var batches = new List<IList<TestEntity>>();
        await foreach (IList<TestEntity> batch in repository.StreamBatchesAsync(batchSize: 3))
        {
            batches.Add(batch);
        }

        batches.Should().HaveCount(3);
        batches[0].Should().HaveCount(3);
        batches[1].Should().HaveCount(3);
        batches[2].Should().HaveCount(2);
    }

    [Fact]
    public async Task StreamProjectedAsync_ShouldReturnProjectedResults()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var names = new List<string>();
        await foreach (string name in repository.StreamProjectedAsync(e => e.Name))
        {
            names.Add(name);
        }

        names.Should().HaveCount(8);
        names.Should().OnlyContain(n => n.StartsWith("Stream-"));
    }

    [Fact]
    public async Task StreamAndProcessAsync_ShouldProcessAllEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var processedIds = new List<int>();

        await repository.StreamAndProcessAsync(
            (entity, _) =>
            {
                lock (processedIds)
                {
                    processedIds.Add(entity.Id);
                }
                return Task.CompletedTask;
            },
            maxDegreeOfParallelism: 2);

        processedIds.Should().HaveCount(8);
        processedIds.Should().OnlyHaveUniqueItems();
    }

    private void SeedData()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        repository.AddAsync(EfCoreTestHelpers.CreateEntities(8, "Stream")).GetAwaiter().GetResult();
        unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
