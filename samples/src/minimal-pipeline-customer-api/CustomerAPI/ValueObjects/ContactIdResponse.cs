using CustomerAPI.Enums;

namespace CustomerAPI.ValueObjects
{
    public class ContactIdResponse
    {
        public ContactType Type { get; set; }
        public required string Description { get; set; } = string.Empty;
    }
}
