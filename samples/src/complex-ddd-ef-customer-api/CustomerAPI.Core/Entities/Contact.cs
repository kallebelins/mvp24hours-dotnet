using CustomerAPI.Core.Enums;
using CustomerAPI.Core.ValueObjects.Domain;
using Mvp24Hours.Core.Entities;
using System;
using System.Text.Json.Serialization;

namespace CustomerAPI.Core.Entities
{
    /// <summary>
    /// Contact entity. Lives inside the Customer aggregate; can only be
    /// created or removed through <see cref="Customer"/> domain methods.
    /// </summary>
    public class Contact : EntityBase<int>
    {
        // Parameterless ctor for EF Core reconstitution.
        protected Contact() { }

        /// <summary>
        /// Factory used exclusively by <see cref="Customer.AddContact"/>.
        /// </summary>
        internal static Contact Create(ContactType type, ContactDescription description, TimeProvider timeProvider)
        {
            return new Contact
            {
                Type = type,
                Description = description.Value,
                Created = timeProvider.GetUtcNow().UtcDateTime,
                Active = true
            };
        }

        public DateTime Created { get; private set; }

        /// <summary>FK to the owning Customer aggregate root. Set by EF via relationship fixup.</summary>
        [JsonIgnore]
        public int CustomerId { get; private set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ContactType Type { get; private set; }

        public string Description { get; private set; } = string.Empty;

        public bool Active { get; private set; }
    }
}
