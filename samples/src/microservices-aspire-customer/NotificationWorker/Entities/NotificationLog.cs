namespace NotificationWorker.Entities;

/// <summary>
/// Persists a record of each integration event received.
/// In this teaching sample the store is in-memory (resets on restart).
/// Swap the EF Core provider to SQL Server / PostgreSQL for production use.
/// </summary>
public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Received";
}
