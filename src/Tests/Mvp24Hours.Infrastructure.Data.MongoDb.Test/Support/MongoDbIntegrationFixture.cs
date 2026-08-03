//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using DotNet.Testcontainers.Builders;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

/// <summary>
/// Shared MongoDB Testcontainer for integration tests. Survives Docker-unavailable scenarios
/// without failing the whole suite (paired with <see cref="DockerFactAttribute"/>).
/// </summary>
public sealed class MongoDbIntegrationFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;

    public IMongoClient Client { get; private set; } = null!;

    public IMongoDatabase Database { get; private set; } = null!;

    public string ConnectionString { get; private set; } = string.Empty;

    public string DatabaseName { get; } = $"mvp24hours_test_{Guid.NewGuid():N}";

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        try
        {
            _container = new MongoDbBuilder("mongo:6.0").Build();
            await _container.StartAsync().ConfigureAwait(false);
            ConnectionString = _container.GetConnectionString();
            Client = new MongoClient(ConnectionString);
            Database = Client.GetDatabase(DatabaseName);
            IsAvailable = true;
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            IsAvailable = false;
            ConnectionString = string.Empty;
            Client = null!;
            Database = null!;
        }
    }

    public async Task DisposeAsync()
    {
        if (IsAvailable && Client is not null)
        {
            await Client.DropDatabaseAsync(DatabaseName).ConfigureAwait(false);
        }

        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    public IMongoCollection<T> GetCollection<T>(string? name = null)
    {
        if (!IsAvailable || Database is null)
        {
            throw new InvalidOperationException(DockerAvailability.SkipReason);
        }

        return Database.GetCollection<T>(name ?? typeof(T).Name);
    }

    private static bool IsDockerUnavailable(Exception ex)
    {
        if (ex is DockerUnavailableException)
        {
            return true;
        }

        if (ex is AggregateException aggregate)
        {
            return aggregate.Flatten().InnerExceptions.Any(static e => e is DockerUnavailableException);
        }

        return false;
    }
}
