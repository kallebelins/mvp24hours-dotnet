namespace CustomerAPI.Domain.Entities;

/// <summary>
/// Audit log written by the RabbitMQ consumer after successfully processing a CustomerCreated integration event.
/// Demonstrates a consumer-side side effect (write) that is idempotency-safe via the inbox pattern.
/// </summary>
public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
