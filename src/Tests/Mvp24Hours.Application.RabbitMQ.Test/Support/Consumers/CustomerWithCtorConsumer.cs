using Mvp24Hours.Application.RabbitMQ.Test.Support.Dto;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers
{
    public class CustomerWithCtorConsumer : IMvpRabbitMQConsumerAsync
    {
        public CustomerWithCtorConsumer(CustomerEvent _event)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "customer-consumer-ctor", $"dto:{_event?.ToSerialize()}");
            if (_event == null || _event.Name != "event")
            {
                throw new ArgumentException("Error event.");
            }
        }

        public string RoutingKey => typeof(CustomerWithCtorConsumer).Name;

        public string QueueName => typeof(CustomerWithCtorConsumer).Name;

        public async Task ReceivedAsync(object message, string token)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "customer-consumer-received", $"dto:{message?.ToSerialize()}|token:{token}");
            await Task.CompletedTask;
        }
    }
}
