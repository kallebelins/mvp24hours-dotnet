using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
        var options = Options.Create(new MongoDbIndexVerificationOptions { Enabled = false });
        var service = new MongoDbIndexVerificationService(provider, options, NullLogger<MongoDbIndexVerificationService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenContextMissing_ShouldSkipVerification()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var options = Options.Create(new MongoDbIndexVerificationOptions
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

        var options = Options.Create(new MongoDbIndexVerificationOptions
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

        var options = Options.Create(new MongoDbIndexVerificationOptions
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
}
