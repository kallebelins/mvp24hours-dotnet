namespace CustomerAPI.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? WelcomeGiftCode { get; set; }
    public bool WelcomeEmailSent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
