using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using CustomerAPI.Core.Specifications.Customers;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerHasEmailContactSpecTests
{
    [Fact]
    public void IsSatisfiedByExpression_WhenActiveWithEmail_IsTrue()
    {
        var customer = new Customer
        {
            Active = true,
            Contacts =
            [
                new Contact { Type = ContactType.Email, Description = "a@b.com", Active = true }
            ]
        };

        var satisfied = new CustomerHasEmailContactSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedByExpression_WhenOnlyCellPhone_IsFalse()
    {
        var customer = new Customer
        {
            Active = true,
            Contacts =
            [
                new Contact { Type = ContactType.CellPhone, Description = "+5511999999999", Active = true }
            ]
        };

        var satisfied = new CustomerHasEmailContactSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeFalse();
    }
}
