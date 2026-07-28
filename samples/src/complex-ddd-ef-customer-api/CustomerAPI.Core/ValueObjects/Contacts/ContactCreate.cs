using CustomerAPI.Core.Enums;

namespace CustomerAPI.Core.ValueObjects.Contacts
{
    /// <summary>
    /// Input DTO for the AddContact command.
    /// The handler calls <c>customer.AddContact()</c> — no direct AutoMapper mapping to entity.
    /// </summary>
    public class ContactCreate
    {
        public ContactType Type { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
