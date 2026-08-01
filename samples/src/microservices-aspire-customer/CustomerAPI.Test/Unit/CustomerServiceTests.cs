using CustomerAPI.Data;
using CustomerAPI.Events;
using CustomerAPI.Models;
using CustomerAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsCustomerAndPublishesEvent()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new CustomerDbContext(options);

        var rabbit = new Mock<IMvpRabbitMQClient>();
        var logger = new Mock<ILogger<CustomerService>>();
        var service = new CustomerService(db, rabbit.Object, logger.Object);

        CustomerResponse created = await service.CreateAsync(
            new CreateCustomerRequest { Name = "Ada Lovelace", Email = "ada@example.com" });

        created.Name.Should().Be("Ada Lovelace");
        (await db.Customers.CountAsync()).Should().Be(1);
        rabbit.Verify(
            client => client.PublishAsync(
                It.IsAny<CustomerCreatedEvent>(),
                nameof(CustomerCreatedEvent),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
