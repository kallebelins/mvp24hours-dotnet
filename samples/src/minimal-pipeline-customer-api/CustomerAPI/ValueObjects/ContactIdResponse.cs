using CustomerAPI.Enums;

namespace CustomerAPI.ValueObjects
{
    public class ContactIdResponse
    {
        public ContactType Type { get; set; }
        public string Description { get; set; }
    }
}
