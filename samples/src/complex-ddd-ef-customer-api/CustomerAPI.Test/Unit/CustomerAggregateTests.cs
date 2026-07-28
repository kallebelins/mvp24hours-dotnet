using CustomerAPI.Core.Entities;
using CustomerAPI.Core.Enums;
using CustomerAPI.Core.Exceptions;
using CustomerAPI.Core.ValueObjects.Domain;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerAggregateTests
{
    private static readonly TimeProvider Time = TimeProvider.System;

    [Fact]
    public void Create_WhenValid_SetsActiveAndRaisesEvent()
    {
        var customer = Customer.Create(new CustomerName("Ada"), Time, "note");

        customer.Name.Should().Be("Ada");
        customer.Note.Should().Be("note");
        customer.Active.Should().BeTrue();
        customer.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Rename_WhenActive_UpdatesName()
    {
        var customer = Customer.Create(new CustomerName("Ada"), Time);

        customer.Rename(new CustomerName("Augusta"));

        customer.Name.Should().Be("Augusta");
    }

    [Fact]
    public void Rename_WhenInactive_ThrowsDomainException()
    {
        var customer = Customer.Create(new CustomerName("Ada"), Time);
        customer.Deactivate();

        var act = () => customer.Rename(new CustomerName("Augusta"));

        act.Should().Throw<DomainException>()
            .WithMessage("An inactive customer cannot be renamed.");
    }

    [Fact]
    public void Deactivate_WhenActive_SetsInactive()
    {
        var customer = Customer.Create(new CustomerName("Ada"), Time);

        customer.Deactivate();

        customer.Active.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var customer = Customer.Create(new CustomerName("Ada"), Time);
        customer.Deactivate();

        customer.Deactivate();

        customer.Active.Should().BeFalse();
    }

    [Fact]
    public void AddContact_WhenActive_AddsContact()
    {
        var customer = Customer.Create(new CustomerName("Ada"), Time);

        var contact = customer.AddContact(
            ContactType.Email,
            new ContactDescription("ada@example.com"),
            Time);

        contact.Description.Should().Be("ada@example.com");
        customer.Contacts.Should().ContainSingle();
        customer.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void AddContact_WhenInactive_ThrowsDomainException()
    {
        var customer = Customer.Create(new CustomerName("Ada"), Time);
        customer.Deactivate();

        var act = () => customer.AddContact(
            ContactType.Email,
            new ContactDescription("ada@example.com"),
            Time);

        act.Should().Throw<DomainException>()
            .WithMessage("Cannot add a contact to an inactive customer.");
    }
}
