using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

public sealed class MongoDbIntegrationFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:6.0").Build();

    public IMongoClient Client { get; private set; } = null!;

    public IMongoDatabase Database { get; private set; } = null!;

    public string DatabaseName { get; } = $"mvp24hours_test_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);
        Client = new MongoClient(_container.GetConnectionString());
        Database = Client.GetDatabase(DatabaseName);
    }

    public async Task DisposeAsync()
    {
        if (Client != null)
        {
            await Client.DropDatabaseAsync(DatabaseName).ConfigureAwait(false);
        }

        await _container.DisposeAsync().ConfigureAwait(false);
    }

    public IMongoCollection<T> GetCollection<T>(string? name = null)
    {
        return Database.GetCollection<T>(name ?? typeof(T).Name);
    }
}
