using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.HealthChecks;
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Infrastructure;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class InfrastructureServicesIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task MongoDbHealthCheck_ShouldReturnHealthyForRunningContainer()
    {
        IOptions<MongoDbOptions> options = MongoDbIntegrationTestHelper.CreateMongoDbOptions(fixture);
        var healthCheck = new MongoDbHealthCheck(options);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("pingResult");
        result.Data["pingResult"].Should().Be(1d);
    }

    [DockerFact]
    public async Task MongoDbHealthCheck_WithVerifyDatabaseAccess_ShouldListCollections()
    {
        IOptions<MongoDbOptions> options = MongoDbIntegrationTestHelper.CreateMongoDbOptions(fixture);
        IOptions<MongoDbHealthCheckOptions> healthCheckOptions = Options.Create(new MongoDbHealthCheckOptions
        {
            VerifyDatabaseAccess = true
        });
        var healthCheck = new MongoDbHealthCheck(options, healthCheckOptions);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("collectionsAccessible");
        result.Data["collectionsAccessible"].Should().Be(true);
    }

    [DockerFact]
    public async Task MongoDbHealthCheck_WithIncludeServerStatus_ShouldCompleteWithoutThrowing()
    {
        IOptions<MongoDbOptions> options = MongoDbIntegrationTestHelper.CreateMongoDbOptions(fixture);
        IOptions<MongoDbHealthCheckOptions> healthCheckOptions = Options.Create(new MongoDbHealthCheckOptions
        {
            IncludeServerStatus = true
        });
        var healthCheck = new MongoDbHealthCheck(options, healthCheckOptions);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("database");
    }

    [DockerFact]
    public async Task MongoDbMigrationRunner_ShouldApplyAndRollbackMigrations()
    {
        string databaseName = $"migrations_{Guid.NewGuid():N}";
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
        IOptions<MongoDbMigrationOptions> migrationOptions = Options.Create(new MongoDbMigrationOptions
        {
            MigrationAssemblies = [typeof(CreateProductsCollectionMigration).Assembly],
            AppliedBy = "integration-test"
        });
        var runner = new MongoDbMigrationRunner(context, migrationOptions);

        (await runner.GetCurrentVersionAsync()).Should().Be(0);

        IReadOnlyList<IMongoDbMigration> pending = await runner.GetPendingMigrationsAsync();
        pending.Should().HaveCount(2);

        MigrationResult migrateResult = await runner.MigrateAsync();
        migrateResult.Success.Should().BeTrue();
        migrateResult.MigrationsApplied.Should().Be(2);
        migrateResult.EndVersion.Should().Be(2);

        (await context.Database.ListCollectionNamesAsync()).ToList()
            .Should().Contain("products");

        IReadOnlyList<MongoDbMigrationHistory> applied = await runner.GetAppliedMigrationsAsync();
        applied.Should().HaveCount(2);
        applied.Should().OnlyContain(m => m.Status == MigrationStatus.Completed);

        MigrationResult rollbackResult = await runner.RollbackLastAsync();
        rollbackResult.Success.Should().BeTrue();
        rollbackResult.EndVersion.Should().Be(1);

        (await runner.GetCurrentVersionAsync()).Should().Be(1);
    }

    [DockerFact]
    public async Task MongoDbMigrationRunner_MigrateToVersion_ShouldApplySingleMigration()
    {
        string databaseName = $"migrations_partial_{Guid.NewGuid():N}";
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
        IOptions<MongoDbMigrationOptions> migrationOptions = Options.Create(new MongoDbMigrationOptions
        {
            MigrationAssemblies = [typeof(CreateProductsCollectionMigration).Assembly],
            AppliedBy = "integration-test"
        });
        var runner = new MongoDbMigrationRunner(context, migrationOptions);

        MigrationResult result = await runner.MigrateToVersionAsync(1);

        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(1);
        result.EndVersion.Should().Be(1);

        IReadOnlyList<IMongoDbMigration> stillPending = await runner.GetPendingMigrationsAsync();
        stillPending.Should().ContainSingle(m => m.Version == 2);
    }

    [DockerFact]
    public async Task MongoDbMigrationRunner_MigrateAsync_WhenUpToDate_ShouldReturnZeroApplied()
    {
        string databaseName = $"migrations_uptodate_{Guid.NewGuid():N}";
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
        IOptions<MongoDbMigrationOptions> migrationOptions = Options.Create(new MongoDbMigrationOptions
        {
            MigrationAssemblies = [typeof(CreateProductsCollectionMigration).Assembly],
            AppliedBy = "integration-test"
        });
        var runner = new MongoDbMigrationRunner(context, migrationOptions);

        await runner.MigrateAsync();

        MigrationResult secondRun = await runner.MigrateAsync();

        secondRun.Success.Should().BeTrue();
        secondRun.MigrationsApplied.Should().Be(0);
        secondRun.StartVersion.Should().Be(2);
        secondRun.EndVersion.Should().Be(2);
    }

    [DockerFact]
    public async Task MongoDbMigrationRunner_RollbackToVersion_ShouldRollbackMultipleMigrations()
    {
        string databaseName = $"migrations_rollback_{Guid.NewGuid():N}";
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
        IOptions<MongoDbMigrationOptions> migrationOptions = Options.Create(new MongoDbMigrationOptions
        {
            MigrationAssemblies = [typeof(CreateProductsCollectionMigration).Assembly],
            AppliedBy = "integration-test"
        });
        var runner = new MongoDbMigrationRunner(context, migrationOptions);

        await runner.MigrateAsync();
        (await runner.GetCurrentVersionAsync()).Should().Be(2);

        MigrationResult rollbackResult = await runner.RollbackToVersionAsync(0);

        rollbackResult.Success.Should().BeTrue();
        rollbackResult.MigrationsApplied.Should().Be(2);
        rollbackResult.EndVersion.Should().Be(0);
        (await runner.GetCurrentVersionAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task MongoDbMigrationRunner_RollbackLast_WhenNoMigrationsApplied_ShouldReturnSuccess()
    {
        string databaseName = $"migrations_empty_{Guid.NewGuid():N}";
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
        IOptions<MongoDbMigrationOptions> migrationOptions = Options.Create(new MongoDbMigrationOptions
        {
            MigrationAssemblies = [typeof(CreateProductsCollectionMigration).Assembly],
            AppliedBy = "integration-test"
        });
        var runner = new MongoDbMigrationRunner(context, migrationOptions);

        MigrationResult result = await runner.RollbackLastAsync();

        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(0);
        (await runner.GetCurrentVersionAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task MongoDbMigrationRunner_RollbackToVersion_WhenTargetNotLessThanCurrent_ShouldNoOp()
    {
        string databaseName = $"migrations_noop_{Guid.NewGuid():N}";
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture, databaseName);
        IOptions<MongoDbMigrationOptions> migrationOptions = Options.Create(new MongoDbMigrationOptions
        {
            MigrationAssemblies = [typeof(CreateProductsCollectionMigration).Assembly],
            AppliedBy = "integration-test"
        });
        var runner = new MongoDbMigrationRunner(context, migrationOptions);

        await runner.MigrateToVersionAsync(1);

        MigrationResult result = await runner.RollbackToVersionAsync(1);

        result.Success.Should().BeTrue();
        result.MigrationsApplied.Should().Be(0);
        (await runner.GetCurrentVersionAsync()).Should().Be(1);
    }
}

public sealed class CreateProductsCollectionMigration : IMongoDbMigration
{
    public int Version => 1;

    public string Description => "Create products collection for integration tests";

    public Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        return database.CreateCollectionAsync("products", cancellationToken: cancellationToken);
    }

    public Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        return database.DropCollectionAsync("products", cancellationToken: cancellationToken);
    }
}

public sealed class AddProductsNameIndexMigration : IMongoDbMigration
{
    public int Version => 2;

    public string Description => "Add name index to products collection";

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("products");
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("Name")),
            cancellationToken: cancellationToken);
    }

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("products");
        await collection.Indexes.DropOneAsync("Name_1", cancellationToken);
    }
}
