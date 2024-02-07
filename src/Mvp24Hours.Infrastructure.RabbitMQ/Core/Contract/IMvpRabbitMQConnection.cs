using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using RabbitMQ.Client;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Persistent connection
    /// <see href="https://github.com/dotnet-architecture/eShopOnContainers/blob/dev/src/BuildingBlocks/EventBus/EventBusRabbitMQ/IRabbitMQPersistentConnection.cs"/>
    /// </summary>
    public interface IMvpRabbitMQConnection
    {
        RabbitMQConnectionOptions Options { get; }
        bool IsConnected { get; }
        bool TryConnect();
        IModel CreateModel();
    }
}
