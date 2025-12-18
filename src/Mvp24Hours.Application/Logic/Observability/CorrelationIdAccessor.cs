//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Threading;
using Mvp24Hours.Application.Contract.Observability;

namespace Mvp24Hours.Application.Logic.Observability;

/// <summary>
/// Thread-safe implementation of <see cref="ICorrelationIdContext"/> using AsyncLocal storage.
/// </summary>
/// <remarks>
/// <para>
/// This implementation uses <see cref="AsyncLocal{T}"/> to maintain correlation IDs
/// across async operations within the same logical execution context.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// <list type="bullet">
/// <item>Register as Singleton in DI</item>
/// <item>Set correlation ID at request start (middleware)</item>
/// <item>Access correlation ID throughout the request pipeline</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration
/// services.AddSingleton&lt;ICorrelationIdContext, CorrelationIdAccessor&gt;();
/// services.AddSingleton&lt;ICorrelationIdAccessor&gt;(sp => sp.GetRequiredService&lt;ICorrelationIdContext&gt;());
/// 
/// // In middleware
/// var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
///     ?? Guid.NewGuid().ToString();
/// _correlationIdContext.SetCorrelationId(correlationId);
/// </code>
/// </example>
public sealed class CorrelationIdAccessor : ICorrelationIdContext
{
    private static readonly AsyncLocal<CorrelationIdHolder> _correlationIdCurrent = new();

    /// <inheritdoc />
    public string? CorrelationId => _correlationIdCurrent.Value?.CorrelationId;

    /// <inheritdoc />
    public string? CausationId => _correlationIdCurrent.Value?.CausationId;

    /// <inheritdoc />
    public bool HasCorrelationId => !string.IsNullOrEmpty(_correlationIdCurrent.Value?.CorrelationId);

    /// <inheritdoc />
    public void SetCorrelationId(string correlationId)
    {
        var holder = _correlationIdCurrent.Value;
        if (holder != null)
        {
            holder.CorrelationId = correlationId;
        }
        else
        {
            _correlationIdCurrent.Value = new CorrelationIdHolder { CorrelationId = correlationId };
        }
    }

    /// <inheritdoc />
    public void SetCausationId(string causationId)
    {
        var holder = _correlationIdCurrent.Value;
        if (holder != null)
        {
            holder.CausationId = causationId;
        }
        else
        {
            _correlationIdCurrent.Value = new CorrelationIdHolder { CausationId = causationId };
        }
    }

    /// <inheritdoc />
    public string EnsureCorrelationId()
    {
        var holder = _correlationIdCurrent.Value;
        if (holder == null)
        {
            holder = new CorrelationIdHolder { CorrelationId = Guid.NewGuid().ToString() };
            _correlationIdCurrent.Value = holder;
        }
        else if (string.IsNullOrEmpty(holder.CorrelationId))
        {
            holder.CorrelationId = Guid.NewGuid().ToString();
        }

        return holder.CorrelationId!;
    }

    /// <summary>
    /// Creates a scoped context with the specified correlation ID that is automatically
    /// restored when disposed.
    /// </summary>
    /// <param name="correlationId">The correlation ID to use within the scope.</param>
    /// <returns>A disposable scope that restores the previous correlation ID when disposed.</returns>
    public IDisposable BeginScope(string correlationId)
    {
        return new CorrelationIdScope(this, correlationId);
    }

    /// <summary>
    /// Creates a scoped context with a new correlation ID.
    /// </summary>
    /// <returns>A disposable scope with a new correlation ID.</returns>
    public IDisposable BeginScope()
    {
        return new CorrelationIdScope(this, Guid.NewGuid().ToString());
    }

    private sealed class CorrelationIdHolder
    {
        public string? CorrelationId { get; set; }
        public string? CausationId { get; set; }
    }
}

/// <summary>
/// Represents a scope for correlation ID that automatically restores the previous value on dispose.
/// </summary>
public sealed class CorrelationIdScope : IDisposable
{
    private readonly CorrelationIdAccessor _accessor;
    private readonly string? _previousCorrelationId;
    private readonly string? _previousCausationId;
    private bool _disposed;

    /// <summary>
    /// Creates a new correlation ID scope.
    /// </summary>
    /// <param name="accessor">The correlation ID accessor.</param>
    /// <param name="correlationId">The new correlation ID for this scope.</param>
    public CorrelationIdScope(CorrelationIdAccessor accessor, string correlationId)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _previousCorrelationId = accessor.CorrelationId;
        _previousCausationId = accessor.CausationId;

        // Set the previous correlation as causation if we have one
        if (_previousCorrelationId != null)
        {
            accessor.SetCausationId(_previousCorrelationId);
        }

        accessor.SetCorrelationId(correlationId);
    }

    /// <summary>
    /// Restores the previous correlation ID.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_previousCorrelationId != null)
        {
            _accessor.SetCorrelationId(_previousCorrelationId);
        }

        if (_previousCausationId != null)
        {
            _accessor.SetCausationId(_previousCausationId);
        }
    }
}

/// <summary>
/// Static helper for accessing the current correlation ID without DI.
/// </summary>
/// <remarks>
/// <para>
/// This is useful for scenarios where DI is not available, such as static methods
/// or extension methods. Prefer using <see cref="ICorrelationIdAccessor"/> through DI
/// when possible.
/// </para>
/// </remarks>
public static class CorrelationIdContext
{
    private static readonly CorrelationIdAccessor _instance = new();

    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    public static string? Current => _instance.CorrelationId;

    /// <summary>
    /// Gets the current causation ID.
    /// </summary>
    public static string? Causation => _instance.CausationId;

    /// <summary>
    /// Sets the current correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    public static void SetCurrent(string correlationId) => _instance.SetCorrelationId(correlationId);

    /// <summary>
    /// Sets the current causation ID.
    /// </summary>
    /// <param name="causationId">The causation ID to set.</param>
    public static void SetCausation(string causationId) => _instance.SetCausationId(causationId);

    /// <summary>
    /// Ensures a correlation ID exists, creating one if necessary.
    /// </summary>
    /// <returns>The existing or newly created correlation ID.</returns>
    public static string Ensure() => _instance.EnsureCorrelationId();

    /// <summary>
    /// Creates a new scope with the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID for the scope.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginScope(string correlationId) => _instance.BeginScope(correlationId);

    /// <summary>
    /// Creates a new scope with a new correlation ID.
    /// </summary>
    /// <returns>A disposable scope with a new correlation ID.</returns>
    public static IDisposable BeginScope() => _instance.BeginScope();
}

