using DotNet.Testcontainers.Builders;
using Testcontainers.MongoDb;

namespace CustomerAPI.Test.Support;

public sealed class MongoDbContainerFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

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
            IsAvailable = true;
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
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

[CollectionDefinition("MongoDb", DisableParallelization = true)]
public sealed class MongoDbCollectionDefinition : ICollectionFixture<MongoDbContainerFixture>
{
}
