using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using CustomerAPI.Core.Specifications.Customers;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerSpecificationTests
{
    [Fact]
    public void CustomerHasEmailContactSpec_WhenActiveWithEmail_IsSatisfied()
    {
        var customer = new Customer
        {
            Name = "Test",
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
    public void CustomerHasEmailContactSpec_WhenInactive_IsNotSatisfied()
    {
        var customer = new Customer
        {
            Name = "Test",
            Active = false,
            Contacts =
            [
                new Contact { Type = ContactType.Email, Description = "a@b.com", Active = true }
            ]
        };

        var satisfied = new CustomerHasEmailContactSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeFalse();
    }

    [Fact]
    public void CustomerHasNoContactSpec_WhenActiveWithoutContacts_IsSatisfied()
    {
        var customer = new Customer
        {
            Name = "Test",
            Active = true,
            Contacts = []
        };

        var satisfied = new CustomerHasNoContactSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeTrue();
    }

    [Fact]
    public void CustomerIsPropectSpec_WhenNoteContainsProspect_IsSatisfied()
    {
        var customer = new Customer
        {
            Name = "Test",
            Active = true,
            Note = "VIP prospect lead",
            Contacts = []
        };

        var satisfied = new CustomerIsPropectSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeTrue();
    }

    [Fact]
    public void CustomerIsPropectSpec_WhenNoteMissingProspect_IsNotSatisfied()
    {
        var customer = new Customer
        {
            Name = "Test",
            Active = true,
            Note = "regular customer",
            Contacts = []
        };

        var satisfied = new CustomerIsPropectSpec().IsSatisfiedByExpression.Compile()(customer);

        satisfied.Should().BeFalse();
    }
}
