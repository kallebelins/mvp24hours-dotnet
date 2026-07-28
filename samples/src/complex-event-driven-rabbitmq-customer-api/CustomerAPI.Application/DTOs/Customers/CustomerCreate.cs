using System.ComponentModel.DataAnnotations;

namespace CustomerAPI.Application.DTOs.Customers;

public sealed record CustomerCreate
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [EmailAddress, MaxLength(250)]
    public string? Email { get; init; }
}
