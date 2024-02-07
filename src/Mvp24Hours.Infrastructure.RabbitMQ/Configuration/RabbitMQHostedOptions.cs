//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    [Serializable]
    public sealed class RabbitMQHostedOptions
    {
        public TimerCallback Callback { get; set; }
        public object State { get; set; }
        public TimeSpan DueTime { get; set; } = TimeSpan.Zero;
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(3);
    }
}
