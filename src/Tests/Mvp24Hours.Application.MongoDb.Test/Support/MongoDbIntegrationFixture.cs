//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using DotNet.Testcontainers.Builders;
using Testcontainers.MongoDb;
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Support;

/// <summary>
/// Shared MongoDB Testcontainer for integration tests. Survives Docker-unavailable scenarios
/// without failing the whole suite (paired with <see cref="DockerFactAttribute"/>).
/// </summary>
public sealed class MongoDbIntegrationFixture : IAsyncLifetime
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
            ConnectionString = string.Empty;
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
