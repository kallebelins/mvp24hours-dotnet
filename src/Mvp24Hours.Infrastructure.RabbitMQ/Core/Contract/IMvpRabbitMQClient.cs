//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    public interface IMvpRabbitMQClient
    {
        void Consume();
        string Publish(object message, string routingKey, string tokenDefault = null);
        void Register(Type consumerType);
        void Register<T>() where T : class, IMvpRabbitMQConsumer;
        void Unregister(Type consumerType);
        void Unregister<T>() where T : class, IMvpRabbitMQConsumer;
    }
}