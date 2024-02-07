//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    public interface IMvpRabbitMQConsumerRecoverySync : IMvpRabbitMQConsumerSync
    {
        void Failure(Exception exception, string token);
        void Rejected(object message, string token);
    }
}
