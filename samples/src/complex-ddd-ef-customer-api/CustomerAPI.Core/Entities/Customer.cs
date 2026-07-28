using CustomerAPI.Core.Enums;
using CustomerAPI.Core.Events;
using CustomerAPI.Core.Exceptions;
using CustomerAPI.Core.ValueObjects.Domain;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;
using System;
using System.Collections.Generic;

namespace CustomerAPI.Core.Entities
{
    /// <summary>
    /// Customer aggregate root.
    /// <para>
    /// All state changes go through the domain methods below.
    /// Invariants are enforced here; no public setters for business-critical properties.
    /// </para>
    /// </summary>
    public class Customer : EntityBase<int>, IAggregateRoot, IHasDomainEvents
    {
        private readonly List<Contact> _contacts = [];
        private readonly List<IDomainEvent> _domainEvents = [];

        /// <summary>Parameterless ctor for EF Core reconstitution.</summary>
        protected Customer() { }

        // ------------------------------------------------------------------ //
        // Read-only properties
        // ------------------------------------------------------------------ //

        public DateTime Created { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? Note { get; private set; }
        public bool Active { get; private set; }

        /// <summary>
        /// Contacts owned by this customer. Access via the aggregate root only.
        /// The backing field <c>_contacts</c> is configured in <c>CustomerConfiguration</c>.
        /// </summary>
        public IReadOnlyCollection<Contact> Contacts => _contacts.AsReadOnly();

        // ------------------------------------------------------------------ //
        // IHasDomainEvents
        // ------------------------------------------------------------------ //

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        private void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        // ------------------------------------------------------------------ //
        // Factory
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a new active customer and raises <see cref="CustomerCreatedDomainEvent"/>.
        /// </summary>
        public static Customer Create(CustomerName name, TimeProvider timeProvider, string? note = null)
        {
            var customer = new Customer
            {
                Created = timeProvider.GetUtcNow().UtcDateTime,
                Name = name.Value,
                Note = note,
                Active = true
            };
            // Domain event — dispatched by the command handler after persistence.
            customer.RaiseDomainEvent(new CustomerCreatedDomainEvent(0, name.Value));
            return customer;
        }

        // ------------------------------------------------------------------ //
        // Domain methods — each enforces an invariant
        // ------------------------------------------------------------------ //

        /// <summary>Renames the customer. Inactive customers may not be renamed.</summary>
        public void Rename(CustomerName newName)
        {
            if (!Active)
                throw new DomainException("An inactive customer cannot be renamed.");

            Name = newName.Value;
        }

        /// <summary>Updates the free-text note (no invariant beyond length, validated by value object).</summary>
        public void UpdateNote(string? note)
        {
            Note = note;
        }

        /// <summary>
        /// Deactivates the customer.
        /// Idempotent: calling on an already-inactive customer is a no-op.
        /// </summary>
        public void Deactivate()
        {
            if (!Active) return;
            Active = false;
        }

        /// <summary>
        /// Adds a contact to this customer and raises <see cref="ContactAddedDomainEvent"/>.
        /// Inactive customers may not receive new contacts.
        /// </summary>
        /// <returns>The newly created <see cref="Contact"/> (its Id is 0 until EF assigns it).</returns>
        public Contact AddContact(ContactType type, ContactDescription description, TimeProvider timeProvider)
        {
            if (!Active)
                throw new DomainException("Cannot add a contact to an inactive customer.");

            var contact = Contact.Create(type, description, timeProvider);
            _contacts.Add(contact);
            RaiseDomainEvent(new ContactAddedDomainEvent(Id, Name, type, description.Value));
            return contact;
        }

        /// <summary>
        /// Removes a contact from this customer by its identifier.
        /// Throws <see cref="DomainException"/> when the contact does not belong to this aggregate.
        /// </summary>
        public void RemoveContact(int contactId)
        {
            var contact = _contacts.Find(c => c.Id == contactId)
                ?? throw new DomainException($"Contact {contactId} does not belong to customer {Id}.");

            _contacts.Remove(contact);
        }
    }
}
