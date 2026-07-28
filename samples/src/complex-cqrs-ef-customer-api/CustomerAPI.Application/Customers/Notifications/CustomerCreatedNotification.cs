using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace CustomerAPI.Application.Customers.Notifications;

public sealed record CustomerCreatedNotification(int CustomerId, string Name) : IMediatorNotification;
