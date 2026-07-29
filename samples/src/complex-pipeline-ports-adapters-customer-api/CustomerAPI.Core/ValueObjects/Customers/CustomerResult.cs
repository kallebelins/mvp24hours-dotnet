namespace CustomerAPI.Core.ValueObjects.Customers
{
    public class CustomerResult
    {
        public int Id { get; set; }
        public required string Name { get; set; }
    }
}
