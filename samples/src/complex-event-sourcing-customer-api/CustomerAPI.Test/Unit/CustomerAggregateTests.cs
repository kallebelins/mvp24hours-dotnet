using CustomerAPI.Domain.Aggregates;
using CustomerAPI.Domain.Events;

namespace CustomerAPI.Test.Unit;

[Trait("Category", "Unit")]
public class CustomerAggregateTests
{
    [Fact]
    public void CustomerAggregate_Create_WhenValid_RaisesCreated()
    {
        CustomerAggregate aggregate = CustomerAggregate.Create("Ada Lovelace", "ada@example.com");

        aggregate.Id.Should().NotBe(Guid.Empty);
        aggregate.Name.Should().Be("Ada Lovelace");
        aggregate.Email.Should().Be("ada@example.com");
        aggregate.IsActive.Should().BeTrue();
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerCreated>();
    }

    [Theory]
    [InlineData(null, "ada@example.com")]
    [InlineData("", "ada@example.com")]
    [InlineData("   ", "ada@example.com")]
    [InlineData("Ada", null)]
    [InlineData("Ada", "")]
    [InlineData("Ada", "   ")]
    public void CustomerAggregate_Create_WhenInvalid_Throws(string? name, string? email)
    {
        Action act = () => CustomerAggregate.Create(name!, email!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CustomerAggregate_Rename_WhenActive_RaisesRenamed()
    {
        CustomerAggregate aggregate = CustomerAggregate.Create("Ada", "ada@example.com");
        aggregate.ClearUncommittedEvents();

        aggregate.Rename("Augusta Ada");

        aggregate.Name.Should().Be("Augusta Ada");
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerRenamed>();
    }

    [Fact]
    public void CustomerAggregate_Rename_WhenSameName_DoesNothing()
    {
        CustomerAggregate aggregate = CustomerAggregate.Create("Ada", "ada@example.com");
        aggregate.ClearUncommittedEvents();

        aggregate.Rename("Ada");

        aggregate.UncommittedEvents.Should().BeEmpty();
    }

    [Fact]
    public void CustomerAggregate_Rename_WhenDeactivated_Throws()
    {
        CustomerAggregate aggregate = CustomerAggregate.Create("Ada", "ada@example.com");
        aggregate.Deactivate();
        aggregate.ClearUncommittedEvents();

        Action act = () => aggregate.Rename("Augusta");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deactivated*");
    }

    [Fact]
    public void CustomerAggregate_Rename_WhenBlank_Throws()
    {
        CustomerAggregate aggregate = CustomerAggregate.Create("Ada", "ada@example.com");

        Action act = () => aggregate.Rename("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CustomerAggregate_Deactivate_WhenActive_RaisesDeactivated()
    {
        CustomerAggregate aggregate = CustomerAggregate.Create("Ada", "ada@example.com");
        aggregate.ClearUncommittedEvents();

        aggregate.Deactivate();

        aggregate.IsActive.Should().BeFalse();
        aggregate.UncommittedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CustomerDeactivated>();
    }

    [Fact]
    public void CustomerAggregate_Deactivate_WhenAlreadyInactive_Throws()
    {
        CustomerAggregate aggregate = CustomerAggregate.Create("Ada", "ada@example.com");
        aggregate.Deactivate();

        Action act = () => aggregate.Deactivate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already deactivated*");
    }
}
