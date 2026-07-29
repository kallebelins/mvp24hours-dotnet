namespace CustomerAPI.Core.Entities
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Email { get; set; }
    }
}
