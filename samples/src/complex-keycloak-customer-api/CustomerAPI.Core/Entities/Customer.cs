namespace CustomerAPI.Core.Entities;

/// <summary>
/// Represents a customer in the system.
/// </summary>
public sealed class Customer
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
