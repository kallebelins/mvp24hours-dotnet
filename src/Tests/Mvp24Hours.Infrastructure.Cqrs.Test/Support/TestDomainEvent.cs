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
/// Uses <see cref="AsyncLocal{T}"/> so parallel test classes do not pollute assertions.
/// </summary>
public class UserRegisteredEventHandler : IMediatorDomainEventHandler<UserRegisteredEvent>
{
    private static readonly AsyncLocal<List<string>?> Current = new();

    /// <summary>
    /// Starts capturing handled events for the current async flow.
    /// </summary>
    public static List<string> BeginCapture()
    {
        var list = new List<string>();
        Current.Value = list;
        return list;
    }

    /// <summary>
    /// Stops capturing for the current async flow.
    /// </summary>
    public static void EndCapture()
    {
        Current.Value = null;
    }

    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        Current.Value?.Add($"User {notification.UserId} registered with email {notification.Email}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for UserRegisteredEvent.
/// Uses <see cref="AsyncLocal{T}"/> so parallel test classes do not pollute assertions.
/// </summary>
public class WelcomeEmailHandler : IMediatorDomainEventHandler<UserRegisteredEvent>
{
    private static readonly AsyncLocal<List<string>?> Current = new();

    /// <summary>
    /// Starts capturing handled events for the current async flow.
    /// </summary>
    public static List<string> BeginCapture()
    {
        var list = new List<string>();
        Current.Value = list;
        return list;
    }

    /// <summary>
    /// Stops capturing for the current async flow.
    /// </summary>
    public static void EndCapture()
    {
        Current.Value = null;
    }

    public Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        Current.Value?.Add($"Welcome email sent to {notification.Email}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for OrderPlacedEvent.
/// Uses <see cref="AsyncLocal{T}"/> so parallel test classes do not pollute assertions.
/// </summary>
public class OrderPlacedEventHandler : IMediatorDomainEventHandler<OrderPlacedEvent>
{
    private static readonly AsyncLocal<List<string>?> Current = new();

    /// <summary>
    /// Starts capturing handled events for the current async flow.
    /// </summary>
    public static List<string> BeginCapture()
    {
        var list = new List<string>();
        Current.Value = list;
        return list;
    }

    /// <summary>
    /// Stops capturing for the current async flow.
    /// </summary>
    public static void EndCapture()
    {
        Current.Value = null;
    }

    public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        Current.Value?.Add($"Order {notification.OrderId} placed with amount {notification.Amount}");
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
