namespace CustomerAPI.Infrastructure.Data.Entities;

/// <summary>
/// EF entity that backs the durable outbox table.
/// Rows are written inside the same transaction as domain changes and consumed by
/// <see cref="CustomerAPI.Infrastructure.Data.Stores.EfCoreIntegrationEventOutbox"/>.
/// </summary>
public class OutboxEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public string? Error { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
