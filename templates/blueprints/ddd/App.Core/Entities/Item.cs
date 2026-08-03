using App.Core.Events;
using App.Core.ValueObjects.Domain;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;

namespace App.Core.Entities;

/// <summary>
/// Item aggregate root. All state changes go through domain methods.
/// </summary>
public class Item : EntityBase<int>, IAggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Item() { }

    public DateTime Created { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public bool Active { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    private void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public static Item Create(ItemName name, TimeProvider timeProvider, string? note = null)
    {
        var item = new Item
        {
            Created = timeProvider.GetUtcNow().UtcDateTime,
            Name = name.Value,
            Note = note,
            Active = true
        };
        item.RaiseDomainEvent(new ItemCreatedDomainEvent(0, name.Value));
        return item;
    }

    public void Rename(ItemName newName) => Name = newName.Value;
}
