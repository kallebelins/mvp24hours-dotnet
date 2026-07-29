namespace NotificationWorker.Events;

/// <summary>
/// Mirror of CustomerAPI.Events.CustomerCreatedEvent.
/// Both services agree on the type name (<c>CustomerCreatedEvent</c>) as the RabbitMQ routing key.
/// No shared contract project is used — JSON property names must match.
/// </summary>
public class CustomerCreatedEvent
{
    public Guid CustomerId { get; set; }
    public required string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
