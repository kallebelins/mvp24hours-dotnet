using CustomerAPI.Core.Entities;
using CustomerAPI.WebAPI.Data;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class InMemoryCustomerStoreTests
{
    [Fact]
    public void GetById_WhenCustomerExists_ReturnsCustomer()
    {
        var store = new InMemoryCustomerStore();
        Guid id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        Customer? customer = store.GetById(id);

        customer.Should().NotBeNull();
        customer!.Name.Should().Be("Alice Smith");
    }

    [Fact]
    public void Add_ThenGetById_ReturnsAddedCustomer()
    {
        var store = new InMemoryCustomerStore();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            Active = true
        };

        store.Add(customer);

        store.GetById(customer.Id).Should().BeEquivalentTo(customer);
    }
}
