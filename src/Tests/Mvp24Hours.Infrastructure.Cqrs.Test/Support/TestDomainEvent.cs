//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Test domain event for user registration.
/// </summary>
public record UserRegisteredEvent : MediatorDomainEventBase
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Test domain event for order placement.
/// </summary>
public record OrderPlacedEvent(int OrderId, decimal Amount) : MediatorDomainEventBase;

/// <summary>
/// Handler for UserRegisteredEvent.
/// </summary>
public class UserRegisteredEventHandler : IMediatorDomainEventHandler<UserRegisteredEvent>
{
    public static List<string> HandledEvents { get; } = [];

    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        HandledEvents.Add($"User {notification.UserId} registered with email {notification.Email}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for UserRegisteredEvent.
/// </summary>
public class WelcomeEmailHandler : IMediatorDomainEventHandler<UserRegisteredEvent>
{
    public static List<string> HandledEvents { get; } = [];

    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        HandledEvents.Add($"Welcome email sent to {notification.Email}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for OrderPlacedEvent.
/// </summary>
public class OrderPlacedEventHandler : IMediatorDomainEventHandler<OrderPlacedEvent>
{
    public static List<string> HandledEvents { get; } = [];

    public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        HandledEvents.Add($"Order {notification.OrderId} placed with amount {notification.Amount}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test entity that has domain events.
/// </summary>
public class TestAggregate : CoreHasDomainEvents
{
    private readonly List<CoreDomainEvent> _domainEvents = [];

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public IReadOnlyCollection<CoreDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Register(string email)
    {
        Name = email.Split('@')[0];
        _domainEvents.Add(new UserRegisteredEvent { UserId = Id, Email = email });
    }

    public void PlaceOrder(decimal amount)
    {
        _domainEvents.Add(new OrderPlacedEvent(Id * 100, amount));
    }
}

