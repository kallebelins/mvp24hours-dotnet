using CustomerAPI.Domain.Entities;
using CustomerAPI.Domain.Enums;
using CustomerAPI.Domain.Specifications.Customers;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerSpecificationTests
{
    [Fact]
    public void CustomerHasEmailContactSpec_WhenActiveWithEmail_IsSatisfied()
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
    public void CustomerHasNoContactSpec_WhenActiveWithoutContacts_IsSatisfied()
    {
        var customer = new Customer
        {
            Active = true,
            Contacts = []
        };

        var satisfied = new CustomerHasNoContactSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeTrue();
    }

    [Fact]
    public void CustomerHasNoContactSpec_WhenHasContacts_IsNotSatisfied()
    {
        var customer = new Customer
        {
            Active = true,
            Contacts =
            [
                new Contact { Type = ContactType.CellPhone, Description = "11999999999", Active = true }
            ]
        };

        var satisfied = new CustomerHasNoContactSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeFalse();
    }

    [Fact]
    public void CustomerIsPropectSpec_WhenActiveWithContacts_IsSatisfied()
    {
        var customer = new Customer
        {
            Active = true,
            Contacts =
            [
                new Contact { Type = ContactType.Email, Description = "a@b.com", Active = true }
            ]
        };

        var satisfied = new CustomerIsPropectSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeTrue();
    }

    [Fact]
    public void CustomerIsPropectSpec_WhenNoContacts_IsNotSatisfied()
    {
        var customer = new Customer
        {
            Active = true,
            Contacts = []
        };

        var satisfied = new CustomerIsPropectSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeFalse();
    }
}
