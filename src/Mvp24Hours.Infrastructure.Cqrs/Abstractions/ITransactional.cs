//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for commands that should be executed within a transaction.
/// The transaction will be committed if the handler succeeds, or rolled back on failure.
/// </summary>
/// <remarks>
/// <para>
/// Apply this interface to commands that modify data and need transactional guarantees.
/// The <see cref="TransactionBehavior{TRequest, TResponse}"/> will automatically
/// wrap the command execution in a transaction.
/// </para>
/// <para>
/// <strong>Note:</strong> The actual transaction is managed by the <see cref="Mvp24Hours.Core.Contract.Data.IUnitOfWorkAsync"/>
/// implementation. The behavior calls SaveChangesAsync on success and RollbackAsync on failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class CreateOrderCommand : IMediatorCommand&lt;Order&gt;, ITransactional
/// {
///     public string CustomerName { get; init; } = string.Empty;
///     public List&lt;OrderItemDto&gt; Items { get; init; } = new();
/// }
/// </code>
/// </example>
public interface ITransactional
{
}
