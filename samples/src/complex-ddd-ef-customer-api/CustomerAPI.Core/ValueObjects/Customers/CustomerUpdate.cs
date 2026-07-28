namespace CustomerAPI.Core.ValueObjects.Customers
{
    /// <summary>
    /// Input DTO for the UpdateCustomer command.
    /// The handler calls <c>customer.Rename()</c> and <c>customer.UpdateNote()</c> — no direct mapping to entity.
    /// </summary>
    public class CustomerUpdate
    {
        public string Name { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
