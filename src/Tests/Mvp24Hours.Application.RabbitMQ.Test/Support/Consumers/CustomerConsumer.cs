using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers
{
    public class CustomerConsumer : IMvpRabbitMQConsumerAsync
    {
        public string RoutingKey => typeof(CustomerConsumer).Name;

        public string QueueName => typeof(CustomerConsumer).Name;

        public async Task ReceivedAsync(object message, string token)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "customer-consumer-received", $"dto:{message?.ToSerialize()}|token:{token}");
            await Task.CompletedTask;
        }
    }
}
