//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Marker interface for notifications that can be processed by multiple handlers.
/// Unlike requests, notifications are fire-and-forget and can have zero or more handlers.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Use Cases:</strong>
/// <list type="bullet">
/// <item>Domain events (e.g., OrderCreated, UserRegistered)</item>
/// <item>Cross-cutting concerns (e.g., logging, auditing)</item>
/// <item>Decoupled communication between components</item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong> This interface (<see cref="IMediatorNotification"/>) is for 
/// in-process notifications handled by the Mediator and is DIFFERENT from 
/// <c>IBusinessEvent</c> in <c>Mvp24Hours.Core.Contract.Infrastructure.Pipe</c> which is used 
/// for message broker integration (RabbitMQ, etc.).
/// </para>
/// <para>
/// <strong>Naming Convention:</strong>
/// <list type="bullet">
/// <item><c>IMediatorNotification</c> - In-process notifications via Mediator (this interface)</item>
/// <item><c>IBusinessEvent</c> - Events for message brokers (existing)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Comparison with Requests:</strong>
/// <list type="bullet">
/// <item>Requests: One handler per request type, returns response</item>
/// <item>Notifications: Zero or more handlers, no return value (fire-and-forget)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Define a notification
/// public class OrderCreatedNotification : IMediatorNotification
/// {
///     public int OrderId { get; init; }
///     public string CustomerEmail { get; init; } = string.Empty;
///     public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
/// }
/// 
/// // Multiple handlers can process the same notification
/// public class SendOrderConfirmationEmailHandler : IMediatorNotificationHandler&lt;OrderCreatedNotification&gt;
/// {
///     public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
///     {
///         // Send email logic...
///     }
/// }
/// 
/// public class UpdateInventoryHandler : IMediatorNotificationHandler&lt;OrderCreatedNotification&gt;
/// {
///     public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
///     {
///         // Update inventory logic...
///     }
/// }
/// 
/// public class AuditOrderHandler : IMediatorNotificationHandler&lt;OrderCreatedNotification&gt;
/// {
///     public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
///     {
///         // Audit logging logic...
///     }
/// }
/// </code>
/// </example>
public interface IMediatorNotification
{
}

/// <summary>
/// Defines a handler for a notification of type <typeparamref name="TNotification"/>.
/// Multiple handlers can process the same notification type.
/// </summary>
/// <typeparam name="TNotification">The type of notification to handle.</typeparam>
/// <remarks>
/// <para>
/// Unlike request handlers, notification handlers:
/// <list type="bullet">
/// <item>Can have multiple implementations for the same notification type</item>
/// <item>Are executed in sequence by default (configurable to parallel)</item>
/// <item>Do not return a value</item>
/// <item>Failures in one handler don't prevent execution of other handlers (configurable)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class UserRegisteredNotification : IMediatorNotification
/// {
///     public int UserId { get; init; }
///     public string Email { get; init; } = string.Empty;
/// }
/// 
/// // Handler 1: Send welcome email
/// public class WelcomeEmailHandler : IMediatorNotificationHandler&lt;UserRegisteredNotification&gt;
/// {
///     private readonly IEmailService _emailService;
///     
///     public WelcomeEmailHandler(IEmailService emailService)
///     {
///         _emailService = emailService;
///     }
///     
///     public async Task Handle(UserRegisteredNotification notification, CancellationToken cancellationToken)
///     {
///         await _emailService.SendWelcomeEmailAsync(notification.Email, cancellationToken);
///     }
/// }
/// 
/// // Handler 2: Create user analytics profile
/// public class AnalyticsHandler : IMediatorNotificationHandler&lt;UserRegisteredNotification&gt;
/// {
///     private readonly IAnalyticsService _analytics;
///     
///     public AnalyticsHandler(IAnalyticsService analytics)
///     {
///         _analytics = analytics;
///     }
///     
///     public async Task Handle(UserRegisteredNotification notification, CancellationToken cancellationToken)
///     {
///         await _analytics.TrackUserRegistrationAsync(notification.UserId, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IMediatorNotificationHandler<in TNotification>
    where TNotification : IMediatorNotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}

