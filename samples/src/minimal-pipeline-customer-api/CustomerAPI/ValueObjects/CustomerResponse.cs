namespace CustomerAPI.ValueObjects
{
    public class CustomerResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
    }
}
