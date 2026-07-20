using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.ConnectionPool;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Profiling;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Projections;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Performance;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class PerformanceIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task IndexManager_ShouldCreateIndexesFromAttributes()
    {
        IMongoCollection<IndexedCustomer> collection = fixture.GetCollection<IndexedCustomer>();
        await collection.DeleteManyAsync(FilterDefinition<IndexedCustomer>.Empty);

        var manager = new MongoDbIndexManager();
        manager.ResetIndexCache();

        await manager.EnsureIndexesAsync(collection);

        IEnumerable<BsonDocument> indexes = await manager.GetExistingIndexesAsync(collection);
        indexes.Select(i => i["name"].AsString).Should().Contain(name => name.StartsWith("idx_", StringComparison.Ordinal));
    }

    [DockerFact]
    public async Task QueryProfiler_ShouldExplainFindAndReturnStats()
    {
        IMongoCollection<OrderDocument> collection = fixture.GetCollection<OrderDocument>("orders_profiler");
        await collection.DeleteManyAsync(FilterDefinition<OrderDocument>.Empty);
        await collection.InsertManyAsync(
        [
            new OrderDocument { Status = "Open" },
            new OrderDocument { Status = "Completed" },
            new OrderDocument { Status = "Completed" }
        ]);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<OrderDocument>(Builders<OrderDocument>.IndexKeys.Ascending(o => o.Status)));

        var profiler = new MongoDbQueryProfiler<OrderDocument>(collection);

        List<IndexInfo> indexes = await profiler.GetIndexesAsync();
        indexes.Should().NotBeEmpty();

        CollectionStats stats = await profiler.GetCollectionStatsAsync();
        stats.Count.Should().BeGreaterThanOrEqualTo(3);

        List<OrderDocument> hinted = await profiler.FindWithHintAsync(
            Builders<OrderDocument>.Filter.Eq(o => o.Status, "Completed"),
            "Status_1");
        hinted.Should().HaveCountGreaterThan(0);
    }

    [DockerFact]
    public async Task Projection_ShouldReturnSelectedFields()
    {
        IMongoCollection<IndexedCustomer> collection = fixture.GetCollection<IndexedCustomer>("projection_customers");
        await collection.DeleteManyAsync(FilterDefinition<IndexedCustomer>.Empty);
        await collection.InsertOneAsync(new IndexedCustomer { Email = "a@test.com", Active = true });

        ProjectionDefinition<IndexedCustomer> projection = MongoDbProjection<IndexedCustomer>.Include(c => c.Email, c => c.Active);
        List<IndexedCustomer> results = await collection
            .Find(FilterDefinition<IndexedCustomer>.Empty)
            .Project<IndexedCustomer>(projection)
            .ToListAsync();

        results.Should().ContainSingle();
        results[0].Email.Should().Be("a@test.com");
    }
}

[Trait("Category", "Unit")]
public class PerformanceUnitTest
{
    [Fact]
    public void IndexManager_BuildIndexModels_ShouldIncludeSingleCompoundAndTtl()
    {
        var manager = new MongoDbIndexManager();
        IReadOnlyList<CreateIndexModel<IndexedCustomer>> models = manager.BuildIndexModels<IndexedCustomer>();

        models.Should().NotBeEmpty();
        models.Should().Contain(m => m.Options != null && m.Options.Name == "idx_email_active");
        models.Should().Contain(m => m.Options != null && m.Options.Name != null && m.Options.Name.StartsWith("idx_ttl_", StringComparison.Ordinal));
    }

    [Fact]
    public void IndexManager_ResetIndexCache_ShouldAllowRebuild()
    {
        var manager = new MongoDbIndexManager();
        manager.ResetIndexCache();
        manager.BuildIndexModels<IndexedCustomer>().Should().NotBeEmpty();
    }

    [Fact]
    public void MongoDbConnectionPoolOptions_ShouldApplyToClientSettings()
    {
        var options = new MongoDbConnectionPoolOptions
        {
            MinPoolSize = 5,
            MaxPoolSize = 50,
            WaitQueueTimeoutSeconds = 15,
            MaxConnectionIdleTimeSeconds = 120,
            MaxConnectionLifetimeSeconds = 600,
            ConnectTimeoutSeconds = 10,
            SocketTimeoutSeconds = 20,
            ServerSelectionTimeoutSeconds = 25,
            HeartbeatFrequencySeconds = 5,
            IPv6 = true,
            DirectConnection = true,
            LocalThresholdMilliseconds = 20
        };

        var settings = new MongoClientSettings();
        options.ApplyTo(settings);

        settings.MinConnectionPoolSize.Should().Be(5);
        settings.MaxConnectionPoolSize.Should().Be(50);
        settings.WaitQueueTimeout.Should().Be(TimeSpan.FromSeconds(15));
        settings.SocketTimeout.Should().Be(TimeSpan.FromSeconds(20));
        settings.IPv6.Should().BeTrue();
        settings.DirectConnection.Should().BeTrue();
    }

    [Fact]
    public void MongoDbConnectionPoolOptions_ApplyTo_ShouldIgnoreNullSettings()
    {
        var options = new MongoDbConnectionPoolOptions();
        options.ApplyTo(null!);
    }

    [Fact]
    public void MongoDbProjectionOptions_BuildSourceProjection_ShouldCombineIncludeExclude()
    {
        var options = new MongoDbProjectionOptions<IndexedCustomer, CustomerDto>()
            .Include(c => c.Email)
            .Exclude(c => c.Active);

        ProjectionDefinition<IndexedCustomer> projection = options.BuildSourceProjection();
        projection.Should().NotBeNull();
    }

    [Fact]
    public void MongoDbProjectionOptions_Build_ShouldRequireProjectExpression()
    {
        var options = new MongoDbProjectionOptions<IndexedCustomer, CustomerDto>()
            .Include(c => c.Email);

        Action act = () => options.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void QueryExplainResult_ShouldDetectIndexUsage()
    {
        var withIndex = new QueryExplainResult { IndexName = "idx_status", Stage = "FETCH" };
        withIndex.UsedIndex.Should().BeTrue();
        withIndex.IsCollectionScan.Should().BeFalse();

        var collScan = new QueryExplainResult { Stage = "COLLSCAN" };
        collScan.IsCollectionScan.Should().BeTrue();
        collScan.UsedIndex.Should().BeFalse();
    }

    [Fact]
    public void MongoCompoundIndexAttribute_ShouldParseFieldDirections()
    {
        var attr = new MongoCompoundIndexAttribute { Fields = "Name:asc,Score:desc,Hash:hashed" };
        var manager = new MongoDbIndexManager();
        IReadOnlyList<CreateIndexModel<IndexedCustomer>> models = manager.BuildIndexModels<IndexedCustomer>();
        models.Should().NotBeEmpty();
    }
}
