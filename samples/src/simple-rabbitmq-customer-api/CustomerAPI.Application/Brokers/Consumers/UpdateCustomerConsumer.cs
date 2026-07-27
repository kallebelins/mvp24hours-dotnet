using CustomerAPI.Core.ValueObjects.Customers;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System.Linq;
using System.Threading.Tasks;

namespace CustomerAPI.Application.Brokers.Consumers
{
    public class UpdateCustomerConsumer(FacadeService facade, ILogger<UpdateCustomerConsumer> logger) : IMvpRabbitMQConsumerAsync
    {
        public string RoutingKey => typeof(CustomerUpdate).Name;

        public string QueueName => typeof(CustomerUpdate).Name;

        public async Task ReceivedAsync(object message, string token)
        {
            if (message is not CustomerUpdate dto)
            {
                logger.LogDebug("Received customer update with null/invalid payload.");
                return;
            }
            logger.LogDebug("Received customer update for {CustomerName}", dto.Name);
            var result = await facade.CustomerService.Update(dto.Id, dto);
            if (result.HasErrors)
            {
                throw new System.InvalidOperationException($"token:{token}|messages:{string.Join(" ; ", result.Messages.Select(x => x.Message).ToArray())}");
            }
        }
    }
}
