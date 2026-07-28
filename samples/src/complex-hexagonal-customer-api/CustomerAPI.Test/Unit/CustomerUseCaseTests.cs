using CustomerAPI.Application.DTOs.Contacts;
using CustomerAPI.Application.DTOs.Customers;
using CustomerAPI.Application.UseCases;
using CustomerAPI.Application.Validations.Contacts;
using CustomerAPI.Application.Validations.Customers;
using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Ports;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerUseCaseTests
{
    [Fact]
    public async Task CreateCustomerAsync_WhenValid_PersistsViaWritePort()
    {
        var writePort = new Mock<ICustomerWritePort>();
        writePort
            .Setup(p => p.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) =>
            {
                c.Id = 42;
                return c;
            });

        var useCase = new CustomerUseCase(
            Mock.Of<ICustomerReadPort>(),
            writePort.Object,
            Mock.Of<IContactReadPort>(),
            Mock.Of<IContactWritePort>(),
            new CustomerCreateValidator(),
            new CustomerUpdateValidator(),
            new ContactCreateValidator(),
            new ContactUpdateValidator(),
            TimeProvider.System,
            NullLogger<CustomerUseCase>.Instance);

        var result = await useCase.CreateCustomerAsync(new CustomerCreate { Name = "Ada" });

        result.HasErrors.Should().BeFalse();
        result.Data.Should().Be(42);
        writePort.Verify(p => p.AddAsync(
            It.Is<Customer>(c => c.Name == "Ada" && c.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
