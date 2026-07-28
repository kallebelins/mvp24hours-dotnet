namespace CustomerAPI.Application.DTOs.Customers;

public sealed record CustomerResult
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool Active { get; init; }
    public DateTime Created { get; init; }
}
