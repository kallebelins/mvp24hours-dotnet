namespace CustomerAPI.Core.DTOs;

/// <summary>
/// DTO returned to callers for a customer record.
/// </summary>
public sealed record CustomerResult(
    Guid Id,
    string Name,
    string Email,
    bool Active,
    DateTimeOffset CreatedAt);
