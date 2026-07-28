namespace CustomerAPI.Infrastructure.Data.Entities;

/// <summary>
/// EF entity that backs the inbox deduplication table.
/// A row is inserted when a consumer first processes a given message ID.
/// Subsequent deliveries of the same ID are short-circuited as duplicates.
/// </summary>
public class InboxEntry
{
    public Guid MessageId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
