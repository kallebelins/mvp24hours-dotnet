using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;
using Mvp24Hours.Infrastructure.Data.MongoDb.Testing;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Testing;

[Trait("Category", "Unit")]
public class MongoDbContextFactoryUnitTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new MongoDbContextFactory((MongoDbInMemoryOptions)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullContextFactory_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new MongoDbContextFactory(
            new MongoDbInMemoryOptions(),
            (Func<MongoDbOptions, Mvp24HoursContext>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateContextWithData_WithNullSeeder_ShouldThrowArgumentNullException()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions { ConnectionString = "mongodb://127.0.0.1:27017" });

        Action act = () => _ = factory.CreateContextWithData((IMongoDataSeeder)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateContextWithData_WithNullSeedAction_ShouldThrowArgumentNullException()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions { ConnectionString = "mongodb://127.0.0.1:27017" });

        Action act = () => _ = factory.CreateContextWithData((Action<Mvp24HoursContext>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateContextWithDataAsync_WithNullSeeder_ShouldThrowArgumentNullException()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions { ConnectionString = "mongodb://127.0.0.1:27017" });

        Func<Task> act = () => factory.CreateContextWithDataAsync((IMongoDataSeederAsync)null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void DropDatabase_WithNullContext_ShouldThrowArgumentNullException()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions { ConnectionString = "mongodb://127.0.0.1:27017" });

        Action act = () => factory.DropDatabase(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildMongoDbOptions_ShouldApplyConfigureOptionsCallback()
    {
        using var factory = new TestMongoDbContextFactory(new MongoDbInMemoryOptions
        {
            ConnectionString = "mongodb://127.0.0.1:27017",
            ConfigureOptions = options => options.ReadPreference = "secondaryPreferred"
        });

        MongoDbOptions options = factory.BuildOptionsForTest();

        options.ReadPreference.Should().Be("secondaryPreferred");
    }

    [Fact]
    public void MongoDbContextHelper_CreateContext_ShouldUseProvidedDatabaseName()
    {
        using Mvp24HoursContext context = MongoDbContextHelper.CreateContext("mongodb://127.0.0.1:27017", "FixedDb");

        context.DatabaseName.Should().Be("FixedDb");
    }

    [Fact]
    public void MongoDbContextHelper_CreateOptions_ShouldConfigureTimeouts()
    {
        MongoDbOptions options = MongoDbContextHelper.CreateOptions("mongodb://127.0.0.1:27017", "OptionsDb");

        options.DatabaseName.Should().Be("OptionsDb");
        options.ConnectionTimeoutSeconds.Should().Be(30);
        options.EnableCommandLogging.Should().BeTrue();
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class MongoDbContextFactoryIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public void CreateContext_ShouldReturnWorkingContext()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions
        {
            ConnectionString = fixture.ConnectionString,
            UseUniqueDatabaseName = true
        });

        using Mvp24HoursContext context = factory.CreateContext();

        context.DatabaseName.Should().NotBeNullOrWhiteSpace();
        context.Database.Should().NotBeNull();
    }

    [DockerFact]
    public void CreateContextWithData_ShouldSeedDocuments()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions
        {
            ConnectionString = fixture.ConnectionString,
            UseUniqueDatabaseName = true
        });

        using Mvp24HoursContext context = factory.CreateContextWithData(ctx => ctx.Set<TestEntity>().InsertOne(new TestEntity { Name = "Seeded" }));

        long count = context.Set<TestEntity>().CountDocuments(FilterDefinition<TestEntity>.Empty);
        count.Should().Be(1);
    }

    [DockerFact]
    public async Task CreateContextWithDataAsync_ShouldSeedDocuments()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions
        {
            ConnectionString = fixture.ConnectionString,
            UseUniqueDatabaseName = true
        });

        using Mvp24HoursContext context = await factory.CreateContextWithDataAsync(async (ctx, ct) => await ctx.Set<TestEntity>().InsertOneAsync(new TestEntity { Name = "AsyncSeeded" }, cancellationToken: ct));

        long count = await context.Set<TestEntity>().CountDocumentsAsync(FilterDefinition<TestEntity>.Empty);
        count.Should().Be(1);
    }

    [DockerFact]
    public async Task DropDatabaseAsync_ShouldRemoveDatabase()
    {
        using var factory = new MongoDbContextFactory(new MongoDbInMemoryOptions
        {
            ConnectionString = fixture.ConnectionString,
            UseUniqueDatabaseName = true
        });
        Mvp24HoursContext context = factory.CreateContext();
        string databaseName = context.DatabaseName;
        await context.Set<TestEntity>().InsertOneAsync(new TestEntity { Name = "DropMe" });

        await factory.DropDatabaseAsync(context);

        bool exists = (await fixture.Client.ListDatabaseNamesAsync()).ToList().Contains(databaseName);
        exists.Should().BeFalse();
        context.Dispose();
    }

    [DockerFact]
    public void CreateContextWithCustomFactory_ShouldUseProvidedFactory()
    {
        using var factory = new MongoDbContextFactory(
            new MongoDbInMemoryOptions { ConnectionString = fixture.ConnectionString },
            options => new Mvp24HoursContext(options));

        using Mvp24HoursContext context = factory.CreateContext();

        context.Should().NotBeNull();
    }

    [DockerFact]
    public void CreateContextWithTenantProviders_ShouldInitializeContext()
    {
        using var factory = new MongoDbContextFactory(
            new MongoDbInMemoryOptions
            {
                ConnectionString = fixture.ConnectionString,
                EnableMultiTenancy = true
            },
            tenantProvider: new FakeTenantProvider("tenant-a"),
            logger: NullLogger<MongoDbContextFactory>.Instance);

        using Mvp24HoursContext context = factory.CreateContext();

        context.EnableMultiTenancy.Should().BeTrue();
        context.TenantProvider.Should().NotBeNull();
    }
}

internal sealed class TestMongoDbContextFactory(MongoDbInMemoryOptions options) : MongoDbContextFactory(options)
{
    public MongoDbOptions BuildOptionsForTest()
    {
        return BuildMongoDbOptions();
    }
}

internal sealed class TestEntitySeeder : IMongoDataSeeder
{
    public void Seed(Mvp24HoursContext context)
    {
        context.Set<TestEntity>().InsertOne(new TestEntity { Name = "TypedSeeder" });
    }
}

internal sealed class TestEntitySeederAsync : IMongoDataSeederAsync
{
    public Task SeedAsync(Mvp24HoursContext context, CancellationToken cancellationToken = default)
    {
        return context.Set<TestEntity>().InsertOneAsync(new TestEntity { Name = "TypedAsyncSeeder" }, cancellationToken: cancellationToken);
    }
}
