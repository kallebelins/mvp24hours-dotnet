using CustomerAPI.Domain.Events;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace CustomerAPI.Domain.Aggregates;

/// <summary>
/// Event-sourced Customer aggregate.
///
/// All state changes happen exclusively through domain events raised via
/// the protected <c>Raise()</c> method inherited from <see cref="AggregateRoot"/>.
/// The aggregate can always be reconstructed from scratch by replaying its event stream.
/// </summary>
public class CustomerAggregate : AggregateRoot
{
    // Required by EventStoreRepository<T> (new() constraint for reconstruction)
    public CustomerAggregate() { }

    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // -------------------------------------------------------------------------
    // Factory / Command methods
    // -------------------------------------------------------------------------

    public static CustomerAggregate Create(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var aggregate = new CustomerAggregate();
        aggregate.Raise(new CustomerCreated(Guid.NewGuid(), name, email));
        return aggregate;
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        if (!IsActive)
            throw new InvalidOperationException("Cannot rename a deactivated customer.");

        if (Name == newName)
            return;

        Raise(new CustomerRenamed(Id, newName));
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new InvalidOperationException("Customer is already deactivated.");

        Raise(new CustomerDeactivated(Id));
    }

    // -------------------------------------------------------------------------
    // Apply — deterministic state reconstruction (no I/O, no exceptions)
    // -------------------------------------------------------------------------

    protected override void Apply(CoreDomainEvent @event)
    {
        switch (@event)
        {
            case CustomerCreated e:
                Id = e.CustomerId;
                Name = e.Name;
                Email = e.Email;
                IsActive = true;
                break;

            case CustomerRenamed e:
                Name = e.NewName;
                break;

            case CustomerDeactivated:
                IsActive = false;
                break;
        }
    }
}
