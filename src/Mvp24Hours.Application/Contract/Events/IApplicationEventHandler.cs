//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Events;

/// <summary>
/// Defines a handler for an application event of type <typeparamref name="TEvent"/>.
/// Multiple handlers can process the same event type.
/// </summary>
/// <typeparam name="TEvent">The type of application event to handle.</typeparam>
/// <remarks>
/// <para>
/// <strong>Handler Characteristics:</strong>
/// <list type="bullet">
/// <item>Multiple handlers can be registered for the same event type</item>
/// <item>Handlers are executed asynchronously</item>
/// <item>By default, handlers run in parallel</item>
/// <item>Failures in one handler don't prevent execution of others</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Handler for entity created events
/// public class SendWelcomeEmailHandler : IApplicationEventHandler&lt;EntityCreatedEvent&lt;Customer&gt;&gt;
/// {
///     private readonly IEmailService _emailService;
///     
///     public SendWelcomeEmailHandler(IEmailService emailService)
///     {
///         _emailService = emailService;
///     }
///     
///     public async Task HandleAsync(EntityCreatedEvent&lt;Customer&gt; @event, CancellationToken cancellationToken)
///     {
///         await _emailService.SendWelcomeEmailAsync(@event.Entity.Email, cancellationToken);
///     }
/// }
/// 
/// // Another handler for the same event
/// public class AuditCreationHandler : IApplicationEventHandler&lt;EntityCreatedEvent&lt;Customer&gt;&gt;
/// {
///     private readonly IAuditService _auditService;
///     
///     public AuditCreationHandler(IAuditService auditService)
///     {
///         _auditService = auditService;
///     }
///     
///     public async Task HandleAsync(EntityCreatedEvent&lt;Customer&gt; @event, CancellationToken cancellationToken)
///     {
///         await _auditService.LogCreationAsync("Customer", @event.Entity.Id, cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IApplicationEventHandler<in TEvent>
    where TEvent : IApplicationEvent
{
    /// <summary>
    /// Handles the application event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Synchronous version of the application event handler for simpler scenarios.
/// </summary>
/// <typeparam name="TEvent">The type of application event to handle.</typeparam>
/// <remarks>
/// Use this interface when handling doesn't require async operations.
/// </remarks>
public interface IApplicationEventHandlerSync<in TEvent>
    where TEvent : IApplicationEvent
{
    /// <summary>
    /// Handles the application event synchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    void Handle(TEvent @event);
}

