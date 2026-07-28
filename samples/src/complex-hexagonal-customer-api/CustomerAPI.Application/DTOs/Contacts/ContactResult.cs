using CustomerAPI.Core.Enums;

namespace CustomerAPI.Application.DTOs.Contacts
{
    public class ContactResult
    {
        public int Id { get; set; }
        public ContactType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
