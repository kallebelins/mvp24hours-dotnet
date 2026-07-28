namespace CustomerAPI.Application.DTOs.Customers
{
    public class CustomerQuery
    {
        public string? Name { get; set; }
        public bool? Active { get; set; }
        public bool HasEmailContact { get; set; }
    }
}
