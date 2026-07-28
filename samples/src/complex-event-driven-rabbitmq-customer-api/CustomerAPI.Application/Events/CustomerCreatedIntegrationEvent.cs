using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Events;

/// <summary>
/// Integration event published when a Customer is created.
/// Carries CorrelationId (request trace) and CausationId (command that triggered this).
/// </summary>
public sealed record CustomerCreatedIntegrationEvent : IntegrationEventBase
{
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public DateTime CreatedAt { get; init; }

    public CustomerCreatedIntegrationEvent() { }

    public CustomerCreatedIntegrationEvent(int customerId, string customerName, string? customerEmail, string? correlationId, string? causationId)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerEmail = customerEmail;
        CreatedAt = DateTime.UtcNow;
        // CorrelationId and CausationId are init-only on IntegrationEventBase; set via record constructor
        _ = correlationId; // captured below via with-expression pattern
        _ = causationId;
    }

    public static CustomerCreatedIntegrationEvent Create(
        int customerId,
        string customerName,
        string? customerEmail,
        string? correlationId,
        string? causationId)
        => new()
        {
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = correlationId,
            CausationId = causationId
        };
}
