//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    [Serializable]
    public sealed class RabbitMQConnectionOptions
    {
        public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672";
        public RabbitMQConnection Configuration { get; set; }
        public int RetryCount { get; set; } = 3;
        public bool DispatchConsumersAsync { get; set; } = true;
    }
}
