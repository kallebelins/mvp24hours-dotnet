using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Base;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

internal sealed class TestableBulkOperationsRepositoryAsync(
    Mvp24HoursContext dbContext,
    IOptions<MongoDbRepositoryOptions> options) : BulkOperationsRepositoryAsync<TestEntity>(dbContext, options)
{
    public void SetCollection(IMongoCollection<TestEntity> collection)
    {
        dbEntities = collection;
    }
}

internal sealed class TestableRepositoryAsyncWithInterceptors(
    Mvp24HoursContext dbContext,
    IOptions<MongoDbRepositoryOptions> options,
    IMongoDbInterceptorPipeline? interceptorPipeline = null) : RepositoryAsyncWithInterceptors<TestEntity>(dbContext, options, interceptorPipeline)
{
    public void SetCollection(IMongoCollection<TestEntity> collection)
    {
        dbEntities = collection;
    }
}

internal sealed class TestableRepository(
    Mvp24HoursContext dbContext,
    IOptions<MongoDbRepositoryOptions> options,
    ILogger<RepositoryBase<TestEntity>>? logger = null) : Repository<TestEntity>(dbContext, options, logger)
{
    public void SetCollection(IMongoCollection<TestEntity> collection)
    {
        dbEntities = collection;
    }
}

internal sealed class TestableReadOnlyRepository(
    Mvp24HoursContext dbContext,
    IOptions<MongoDbRepositoryOptions> options,
    ILogger<RepositoryBase<TestEntity>>? logger = null) : ReadOnlyRepository<TestEntity>(dbContext, options, logger)
{
    public void SetCollection(IMongoCollection<TestEntity> collection)
    {
        dbEntities = collection;
    }
}

internal sealed class TestableRepositoryAsync(
    Mvp24HoursContext dbContext,
    IOptions<MongoDbRepositoryOptions> options,
    ILogger<RepositoryAsync<TestEntity>>? logger = null) : RepositoryAsync<TestEntity>(dbContext, options, logger)
{
    public void SetCollection(IMongoCollection<TestEntity> collection)
    {
        dbEntities = collection;
    }
}

internal sealed class TestableReadOnlyRepositoryAsync(
    Mvp24HoursContext dbContext,
    IOptions<MongoDbRepositoryOptions> options,
    ILogger<RepositoryBase<TestEntity>>? logger = null) : ReadOnlyRepositoryAsync<TestEntity>(dbContext, options, logger)
{
    public void SetCollection(IMongoCollection<TestEntity> collection)
    {
        dbEntities = collection;
    }
}

internal sealed class FakeAsyncCursor<T>(IReadOnlyList<T> items) : IAsyncCursor<T>
{
    private bool _hasMoved;

    public IEnumerable<T> Current => items;

    public void Dispose()
    {
    }

    public bool MoveNext(CancellationToken cancellationToken = default)
    {
        if (!_hasMoved)
        {
            _hasMoved = true;
            return items.Count > 0;
        }

        return false;
    }

    public Task<bool> MoveNextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MoveNext(cancellationToken));
    }
}

internal static class MongoDbTestContextFactory
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";

    public static Mvp24HoursContext Create(string? databaseName = null)
    {
        return new Mvp24HoursContext(databaseName ?? $"unit_test_{Guid.NewGuid():N}", ConnectionString);
    }

    public static IOptions<MongoDbRepositoryOptions> CreateRepositoryOptions()
    {
        return Options.Create(new MongoDbRepositoryOptions());
    }
}
