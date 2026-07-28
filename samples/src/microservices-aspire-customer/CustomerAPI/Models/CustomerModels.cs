using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Models;

/// <summary>Request DTO for creating a customer.</summary>
public class CreateCustomerRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Response DTO returned after creating or retrieving a customer.</summary>
public class CustomerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
