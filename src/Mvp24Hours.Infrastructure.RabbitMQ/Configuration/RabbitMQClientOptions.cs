//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    [Serializable]
    public class RabbitMQClientOptions : RabbitMQOptions
    {
        public int MaxRedeliveredCount { get; set; } = 3;
        public RabbitMQOptions DeadLetter { get; set; }
    }
}
