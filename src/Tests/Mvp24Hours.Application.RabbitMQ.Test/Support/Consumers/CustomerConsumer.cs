using Microsoft.Extensions.Logging;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers
{
    public class CustomerConsumer : IMvpRabbitMQConsumerAsync
    {
        private readonly ILogger<CustomerConsumer>? _logger;

        public CustomerConsumer(ILogger<CustomerConsumer>? logger = null)
        {
            _logger = logger;
        }

        public string RoutingKey => typeof(CustomerConsumer).Name;

        public string QueueName => typeof(CustomerConsumer).Name;

        public async Task ReceivedAsync(object message, string token)
        {
            _logger?.LogDebug("CustomerConsumer received message: {Message}, Token: {Token}", message?.ToSerialize(), token);
            await Task.CompletedTask;
        }
    }
}
