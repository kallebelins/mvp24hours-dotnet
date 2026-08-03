using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Infrastructure;

[Trait("Category", "Unit")]
public class MongoDbIndexVerificationServiceTest
{
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldCompleteWithoutVerification()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<MongoDbIndexVerificationOptions> options = Options.Create(new MongoDbIndexVerificationOptions { Enabled = false });
        var service = new MongoDbIndexVerificationService(provider, options, NullLogger<MongoDbIndexVerificationService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenContextMissing_ShouldSkipVerification()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<MongoDbIndexVerificationOptions> options = Options.Create(new MongoDbIndexVerificationOptions
        {
            Enabled = true,
            AssembliesToScan = [typeof(IndexedCustomer).Assembly]
        });
        var service = new MongoDbIndexVerificationService(provider, options, NullLogger<MongoDbIndexVerificationService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await service.StopAsync(CancellationToken.None);
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbIndexVerificationServiceIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task ExecuteAsync_ShouldVerifyAndCreateIndexesForIndexedEntities()
    {
        var services = new ServiceCollection();
        services.AddSingleton(MongoDbIntegrationTestHelper.CreateContext(fixture));
        ServiceProvider provider = services.BuildServiceProvider();

        IOptions<MongoDbIndexVerificationOptions> options = Options.Create(new MongoDbIndexVerificationOptions
        {
            Enabled = true,
            CreateMissingIndexes = true,
            FailOnMissingIndexes = false,
            FailOnVerificationError = false,
            AssembliesToScan = [typeof(IndexedCustomer).Assembly]
        });
        var service = new MongoDbIndexVerificationService(provider, options, NullLogger<MongoDbIndexVerificationService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await service.StopAsync(CancellationToken.None);
    }

    [DockerFact]
    public async Task ExecuteAsync_WithStartupDelay_ShouldWaitBeforeVerification()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_ => MongoDbIntegrationTestHelper.CreateContext(fixture));
        ServiceProvider provider = services.BuildServiceProvider();

        IOptions<MongoDbIndexVerificationOptions> options = Options.Create(new MongoDbIndexVerificationOptions
        {
            Enabled = true,
            StartupDelaySeconds = 0,
            CreateMissingIndexes = true,
            AssembliesToScan = [typeof(IndexedCustomer).Assembly]
        });
        var service = new MongoDbIndexVerificationService(provider, options);

        DateTimeOffset started = DateTimeOffset.UtcNow;
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await service.StopAsync(CancellationToken.None);

        (DateTimeOffset.UtcNow - started).Should().BeGreaterThan(TimeSpan.FromMilliseconds(50));
    }

    [DockerFact]
    public async Task ExecuteAsync_WithFailOnMissingIndexes_ShouldThrowWhenWrongIndexExists()
    {
        string databaseName = $"index_fail_test_{Guid.NewGuid():N}";
        IMongoDatabase database = fixture.Client.GetDatabase(databaseName);
        IMongoCollection<IndexedCustomer> collection = database.GetCollection<IndexedCustomer>("indexed_customers");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<IndexedCustomer>(
            Builders<IndexedCustomer>.IndexKeys.Ascending(x => x.Active),
            new CreateIndexOptions { Name = "wrong_active_only" }));

        var services = new ServiceCollection();
        services.AddSingleton(new Mvp24HoursContext(databaseName, fixture.ConnectionString));
        ServiceProvider provider = services.BuildServiceProvider();

        IOptions<MongoDbIndexVerificationOptions> options = Options.Create(new MongoDbIndexVerificationOptions
        {
            Enabled = true,
            CreateMissingIndexes = false,
            FailOnMissingIndexes = true,
            FailOnVerificationError = false,
            StartupDelaySeconds = 0,
            AssembliesToScan = [typeof(IndexedCustomer).Assembly]
        });
        var service = new MongoDbIndexVerificationService(provider, options, NullLogger<MongoDbIndexVerificationService>.Instance);

        MethodInfo? verifyMethod = typeof(MongoDbIndexVerificationService)
            .GetMethod("VerifyIndexesAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        try
        {
            Func<Task> act = () => (Task)verifyMethod!.Invoke(service, [CancellationToken.None])!;

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*missing indexes*");
        }
        finally
        {
            await fixture.Client.DropDatabaseAsync(databaseName);
        }
    }
}
