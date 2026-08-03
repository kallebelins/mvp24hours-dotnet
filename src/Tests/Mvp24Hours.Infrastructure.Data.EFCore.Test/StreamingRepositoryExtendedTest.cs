using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class StreamingRepositoryExtendedTest : IDisposable
{
    private readonly ServiceProvider _provider;

    public StreamingRepositoryExtendedTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>($"StreamExt_{Guid.NewGuid():N}");
        services.AddMvp24HoursStreamingRepositoryAsync(options =>
        {
            options.DefaultTrackingBehavior = QueryTrackingBehavior.NoTracking;
            options.EnableQueryTags = true;
        });
        _provider = services.BuildServiceProvider();
        SeedData();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public async Task StreamAllAsync_WithPagingCriteria_ShouldRespectLimit()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var streamed = new List<TestEntity>();
        await foreach (TestEntity entity in repository.StreamAllAsync(new PagingCriteria(limit: 3, offset: 0)))
        {
            streamed.Add(entity);
        }

        streamed.Should().HaveCount(3);
    }

    [Fact]
    public async Task StreamByAsync_WithPagingCriteria_ShouldReturnFilteredPage()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var streamed = new List<TestEntity>();
        await foreach (TestEntity entity in repository.StreamByAsync(
                           e => e.Active,
                           new PagingCriteria(limit: 2, offset: 0)))
        {
            streamed.Add(entity);
        }

        streamed.Should().HaveCount(2);
        streamed.Should().OnlyContain(e => e.Active);
    }

    [Fact]
    public async Task StreamBatchesAsync_WithFilter_ShouldReturnMatchingBatches()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var batches = new List<IList<TestEntity>>();
        await foreach (IList<TestEntity> batch in repository.StreamBatchesAsync(e => e.Active, batchSize: 2))
        {
            batches.Add(batch);
        }

        batches.Should().NotBeEmpty();
        batches.SelectMany(b => b).Should().OnlyContain(e => e.Active);
    }

    [Fact]
    public async Task StreamProjectedByAsync_ShouldReturnProjectedResults()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var names = new List<string>();
        await foreach (string name in repository.StreamProjectedByAsync(e => e.Active, e => e.Name))
        {
            names.Add(name);
        }

        names.Should().HaveCount(4);
        names.Should().OnlyContain(n => n.StartsWith("StreamExt-"));
    }

    [Fact]
    public async Task StreamAndProcessAsync_WithFilter_ShouldProcessMatchingEntities()
    {
        using IServiceScope scope = _provider.CreateScope();
        var repository = (StreamingRepositoryAsync<TestEntity>)scope.ServiceProvider
            .GetRequiredService<IStreamingRepositoryAsync<TestEntity>>();

        var processed = new List<int>();
        await repository.StreamAndProcessAsync(
            e => e.Active,
            (entity, _) =>
            {
                processed.Add(entity.Id);
                return Task.CompletedTask;
            });

        processed.Should().HaveCount(4);
    }

    private void SeedData()
    {
        using IServiceScope scope = _provider.CreateScope();
        IRepositoryAsync<TestEntity> repository = scope.ServiceProvider.GetRequiredService<IRepositoryAsync<TestEntity>>();
        IUnitOfWorkAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkAsync>();
        repository.AddAsync(EfCoreTestHelpers.CreateEntities(8, "StreamExt")).GetAwaiter().GetResult();
        unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
