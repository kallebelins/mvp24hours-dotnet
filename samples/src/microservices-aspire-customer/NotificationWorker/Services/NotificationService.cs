using NotificationWorker.Data;
using NotificationWorker.Entities;

namespace NotificationWorker.Services;

/// <summary>
/// Persists a notification log entry for each received integration event.
/// </summary>
public class NotificationService(NotificationDbContext db, ILogger<NotificationService> logger) : INotificationService
{
    public async Task LogAsync(string eventType, string payload, CancellationToken ct = default)
    {
        var log = new NotificationLog
        {
            EventType = eventType,
            Payload = payload,
            Status = "Processed"
        };

        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Notification log saved: {EventType} (id={LogId})", eventType, log.Id);
    }
}
