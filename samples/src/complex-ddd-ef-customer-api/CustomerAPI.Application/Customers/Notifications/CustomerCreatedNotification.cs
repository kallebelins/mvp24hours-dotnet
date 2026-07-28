using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Notifications;

/// <summary>
/// In-process notification dispatched after a Customer is successfully persisted.
/// Maps from the domain event raised by <c>Customer.Create()</c>.
/// </summary>
public sealed record CustomerCreatedNotification(int CustomerId, string CustomerName) : IMediatorNotification;
