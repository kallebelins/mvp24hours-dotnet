using CustomerAPI.Application.Repositories;
using CustomerAPI.Application.Sagas.Steps;
using CustomerAPI.Domain.Sagas;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CreateCustomerStepTests
{
    [Fact]
    public async Task CreateCustomerStep_WhenExecuted_PersistsCustomer()
    {
        var repository = new InMemoryCustomerRepository();
        var step = new CreateCustomerStep(repository);
        var data = new OnboardCustomerData
        {
            Name = "Grace Hopper",
            Email = "grace@example.com"
        };

        await step.ExecuteAsync(data);

        data.CustomerId.Should().NotBeNull();
        var stored = await repository.GetByIdAsync(data.CustomerId!.Value);
        stored.Should().NotBeNull();
        stored!.Name.Should().Be("Grace Hopper");
        stored.Email.Should().Be("grace@example.com");
    }

    [Fact]
    public async Task CreateCustomerStep_Compensate_DeletesCustomer()
    {
        var repository = new InMemoryCustomerRepository();
        var step = new CreateCustomerStep(repository);
        var data = new OnboardCustomerData
        {
            Name = "Alan Turing",
            Email = "alan@example.com"
        };

        await step.ExecuteAsync(data);
        data.CustomerId.Should().NotBeNull();

        await step.CompensateAsync(data);

        var stored = await repository.GetByIdAsync(data.CustomerId!.Value);
        stored.Should().BeNull();
    }
}
