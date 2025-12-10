//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions;

/// <summary>
/// Represents an empty return type for requests that don't return a value.
/// Used because generics in C# don't support void as a type parameter.
/// </summary>
/// <remarks>
/// <para>
/// This struct is similar to the Unit type in functional programming languages.
/// It represents "no meaningful value" and is used for commands that perform
/// actions without returning data.
/// </para>
/// <para>
/// <strong>Usage:</strong> When defining a command that doesn't return a value,
/// use <see cref="IMediatorRequest"/> which internally uses <see cref="IMediatorRequest{Unit}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Command without return value
/// public class DeleteUserCommand : IMediatorRequest
/// {
///     public int UserId { get; set; }
/// }
/// 
/// // Handler returns Unit.Value
/// public class DeleteUserCommandHandler : IMediatorRequestHandler&lt;DeleteUserCommand&gt;
/// {
///     public async Task&lt;Unit&gt; Handle(DeleteUserCommand request, CancellationToken cancellationToken)
///     {
///         // Delete user logic...
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
    /// <summary>
    /// Gets the default (and only) value of Unit.
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Gets a completed Task containing Unit.Value.
    /// Useful for returning from async methods that don't produce a value.
    /// </summary>
    public static readonly Task<Unit> Task = System.Threading.Tasks.Task.FromResult(Value);

    /// <inheritdoc />
    public int CompareTo(Unit other) => 0;

    /// <inheritdoc />
    int IComparable.CompareTo(object? obj) => 0;

    /// <inheritdoc />
    public override int GetHashCode() => 0;

    /// <inheritdoc />
    public bool Equals(Unit other) => true;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Unit;

    /// <inheritdoc />
    public override string ToString() => "()";

    /// <summary>
    /// Equality operator. Always returns true since all Unit values are equal.
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Inequality operator. Always returns false since all Unit values are equal.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}

