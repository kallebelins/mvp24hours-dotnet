using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Dto;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers
{
    public class CustomerWithCtorConsumer : IMvpRabbitMQConsumerAsync
    {
        private readonly ILogger<CustomerWithCtorConsumer>? _logger;

        public CustomerWithCtorConsumer(CustomerEvent _event, ILogger<CustomerWithCtorConsumer>? logger = null)
        {
            _logger = logger;
            _logger?.LogDebug("CustomerWithCtorConsumer initialized with event: {Event}", _event?.ToSerialize());
            if (_event == null || _event.Name != "event")
            {
                throw new ArgumentException("Error event.");
            }
        }

        public string RoutingKey => typeof(CustomerWithCtorConsumer).Name;

        public string QueueName => typeof(CustomerWithCtorConsumer).Name;

        public async Task ReceivedAsync(object message, string token)
        {
            _logger?.LogDebug("CustomerWithCtorConsumer received message: {Message}, Token: {Token}", message?.ToSerialize(), token);
            await Task.CompletedTask;
        }
    }
}
