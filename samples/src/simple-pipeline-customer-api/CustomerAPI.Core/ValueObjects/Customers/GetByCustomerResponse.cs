namespace CustomerAPI.Core.ValueObjects.Customers
{
    public class GetByCustomerResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }
}
