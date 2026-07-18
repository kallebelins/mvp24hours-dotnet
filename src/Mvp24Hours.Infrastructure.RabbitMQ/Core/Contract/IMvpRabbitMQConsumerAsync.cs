//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

public interface IMvpRabbitMQConsumerAsync : IMvpRabbitMQConsumer
{
    Task ReceivedAsync(object message, string token);
}
