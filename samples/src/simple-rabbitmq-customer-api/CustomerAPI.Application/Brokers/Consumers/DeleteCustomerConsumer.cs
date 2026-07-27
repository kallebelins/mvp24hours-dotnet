using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Brokers.Consumers
{
    public class DeleteCustomerConsumer(FacadeService facade, ILogger<DeleteCustomerConsumer> logger) : IMvpRabbitMQConsumerAsync
    {
        public string RoutingKey => typeof(CustomerDelete).Name;

        public string QueueName => typeof(CustomerDelete).Name;

        public async Task ReceivedAsync(object message, string token)
        {
            if (message is not CustomerDelete dto)
            {
                logger.LogDebug("Received customer delete with null/invalid payload.");
                return;
            }
            logger.LogDebug("Received customer delete for {CustomerId}", dto.Id);
            var result = await facade.CustomerService.Delete(dto.Id);
            if (result.HasErrors)
            {
                throw new System.InvalidOperationException($"token:{token}|messages:{string.Join(" ; ", result.Messages.Select(x => x.Message).ToArray())}");
            }
        }
    }
}
