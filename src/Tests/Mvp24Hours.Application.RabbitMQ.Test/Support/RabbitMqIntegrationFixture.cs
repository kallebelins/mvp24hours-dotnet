//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using DotNet.Testcontainers.Builders;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support;

/// <summary>
/// Shared RabbitMQ Testcontainer for integration tests. Survives Docker-unavailable scenarios
/// without failing the whole suite (paired with <see cref="DockerFactAttribute"/>).
/// </summary>
public sealed class RabbitMqIntegrationFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;

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
            _container = new RabbitMqBuilder("rabbitmq:3.13-management")
                .WithUsername("guest")
                .WithPassword("guest")
                .WithCleanUp(true)
                .Build();

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

    /// <summary>
    /// Opens a channel against the running container. Caller must dispose the returned connection.
    /// </summary>
    public (IConnection Connection, IModel Channel) CreateConnectionAndChannel()
    {
        if (!IsAvailable || string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException(DockerAvailability.SkipReason);
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri(ConnectionString),
            DispatchConsumersAsync = true
        };

        IConnection connection = factory.CreateConnection();
        IModel channel = connection.CreateModel();
        return (connection, channel);
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
