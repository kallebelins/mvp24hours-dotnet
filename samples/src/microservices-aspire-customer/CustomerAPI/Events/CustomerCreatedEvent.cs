namespace CustomerAPI.Events;

/// <summary>
/// Integration event published to RabbitMQ when a customer is successfully created.
/// The NotificationWorker subscribes to this event and persists a notification log entry.
/// </summary>
/// <remarks>
/// The routing key used is the type name: <c>CustomerCreatedEvent</c>.
/// Both services must agree on this name — no shared contract project is needed for a teaching sample.
/// </remarks>
public record CustomerCreatedEvent(
    Guid CustomerId,
    string Name,
    string Email,
    DateTime CreatedAt);
