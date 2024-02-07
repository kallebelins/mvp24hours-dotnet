//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Enums;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration
{
    [Serializable]
    public class RabbitMQOptions
    {
        public string Exchange { get; set; } = "amq.direct";
        public MvpRabbitMQExchangeType ExchangeType { get; set; } = MvpRabbitMQExchangeType.direct;
        public string RoutingKey { get; set; }
        public string QueueName { get; set; }
        public bool Durable { get; set; } = true;
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public Dictionary<string, object> ExchangeArguments { get; set; }
        public Dictionary<string, object> QueueArguments { get; set; }
        public IBasicProperties BasicProperties { get; set; }
    }
}
