//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for commands that should be idempotent.
/// Idempotent commands can be safely retried without causing duplicate effects.
/// </summary>
/// <remarks>
/// <para>
/// Apply this interface to commands where duplicate processing should be prevented,
/// such as payment processing, order creation, etc.
/// </para>
/// <para>
/// <strong>How it works:</strong>
/// <list type="number">
/// <item>A unique key is generated from the command</item>
/// <item>The key is checked against the cache</item>
/// <item>If found, the cached response is returned</item>
/// <item>If not found, the command is executed and result cached</item>
/// </list>
/// </para>
/// <para>
/// <strong>Important:</strong> Ensure the IdempotencyKey uniquely identifies
/// the intent of the command, not just its data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ProcessPaymentCommand : IMediatorCommand&lt;PaymentResult&gt;, IIdempotentCommand
/// {
///     public Guid PaymentId { get; init; }
///     public decimal Amount { get; init; }
///     
///     // Custom idempotency key based on payment ID
///     public string? IdempotencyKey => $"payment:{PaymentId}";
///     
///     // Cache result for 24 hours
///     public TimeSpan? IdempotencyDuration => TimeSpan.FromHours(24);
/// }
/// </code>
/// </example>
public interface IIdempotentCommand
{
    /// <summary>
    /// Gets the idempotency key for this command.
    /// If null, a key will be generated from the command properties.
    /// </summary>
    /// <remarks>
    /// For commands with a natural business key (like PaymentId),
    /// it's recommended to provide a custom key based on that.
    /// </remarks>
    string? IdempotencyKey => null;

    /// <summary>
    /// Gets the duration to cache the idempotency result.
    /// If null, the default duration from options will be used.
    /// </summary>
    TimeSpan? IdempotencyDuration => null;
}
