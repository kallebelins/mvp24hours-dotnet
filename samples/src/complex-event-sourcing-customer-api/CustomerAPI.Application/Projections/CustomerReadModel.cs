namespace CustomerAPI.Application.Projections;

/// <summary>
/// Denormalized read model kept in memory for fast queries.
/// Updated synchronously after each write command (inline projection update).
/// </summary>
public class CustomerReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long Version { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
