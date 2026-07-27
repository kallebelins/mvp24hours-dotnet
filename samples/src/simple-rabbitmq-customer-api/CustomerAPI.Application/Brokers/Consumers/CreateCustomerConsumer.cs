using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Brokers.Consumers
{
    public class CreateCustomerConsumer(FacadeService facade, ILogger<CreateCustomerConsumer> logger) : IMvpRabbitMQConsumerAsync
    {
        public string RoutingKey => typeof(CustomerCreate).Name;

        public string QueueName => typeof(CustomerCreate).Name;

        public async Task ReceivedAsync(object message, string token)
        {
            if (message is not CustomerCreate dto)
            {
                logger.LogDebug("Received customer create with null/invalid payload.");
                return;
            }
            logger.LogDebug("Received customer create for {CustomerName}", dto.Name);
            var result = await facade.CustomerService.Create(dto);
            if (result.HasErrors)
            {
                throw new System.InvalidOperationException(string.Join(" | ", result.Messages.Select(x => x.Message).ToArray()));
            }
        }
    }
}
