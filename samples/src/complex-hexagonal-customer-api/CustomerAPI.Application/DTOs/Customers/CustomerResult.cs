namespace CustomerAPI.Application.DTOs.Customers
{
    public class CustomerResult
    {
        public int Id { get; set; }
        public required string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
        public bool Active { get; set; }
    }
}
