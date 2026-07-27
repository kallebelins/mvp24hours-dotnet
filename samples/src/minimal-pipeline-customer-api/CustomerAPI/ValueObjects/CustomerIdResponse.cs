namespace CustomerAPI.ValueObjects
{
    public class CustomerIdResponse : CustomerResponse
    {
        public IList<ContactIdResponse> Contacts { get; set; }
    }
}
