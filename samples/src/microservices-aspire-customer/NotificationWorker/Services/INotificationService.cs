namespace NotificationWorker.Services;

public interface INotificationService
{
    Task LogAsync(string eventType, string payload, CancellationToken ct = default);
}
