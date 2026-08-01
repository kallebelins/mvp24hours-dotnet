using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Specifications.Customers;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerIsActiveSpecTests
{
    [Fact]
    public void IsSatisfiedByExpression_WhenActive_IsTrue()
    {
        var customer = new Customer { Name = "Active", Active = true };

        bool satisfied = new CustomerIsActiveSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedByExpression_WhenInactive_IsFalse()
    {
        var customer = new Customer { Name = "Inactive", Active = false };

        bool satisfied = new CustomerIsActiveSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeFalse();
    }
}
