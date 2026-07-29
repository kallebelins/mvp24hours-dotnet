namespace CustomerAPI.Core.ValueObjects.Customers
{
    /// <summary>
    /// Input DTO for the CreateCustomer command.
    /// The handler constructs the Customer aggregate via <c>Customer.Create()</c> — no AutoMapper mapping needed.
    /// </summary>
    public class CustomerCreate
    {
        public required string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
