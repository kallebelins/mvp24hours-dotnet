//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Observability;

/// <summary>
/// Interface for storing audit trail entries.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to persist audit entries to your preferred storage:
/// <list type="bullet">
/// <item>Database (SQL, NoSQL)</item>
/// <item>Event streaming (Kafka, Event Store)</item>
/// <item>File system</item>
/// <item>External services (Elasticsearch, Splunk)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class EfCoreAuditStore : IAuditStore
/// {
///     private readonly AuditDbContext _context;
///     
///     public async Task SaveAsync(AuditEntry entry, CancellationToken cancellationToken = default)
///     {
///         _context.AuditEntries.Add(entry);
///         await _context.SaveChangesAsync(cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IAuditStore
{
    /// <summary>
    /// Saves an audit entry to the store.
    /// </summary>
    /// <param name="entry">The audit entry to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves multiple audit entries to the store.
    /// </summary>
    /// <param name="entries">The audit entries to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an audit trail entry for a mediator operation.
/// </summary>
/// <remarks>
/// <para>
/// Audit entries capture:
/// <list type="bullet">
/// <item>What operation was performed</item>
/// <item>Who performed it</item>
/// <item>When it was performed</item>
/// <item>What data was involved (optionally)</item>
/// <item>Whether it succeeded or failed</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit entry.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the correlation ID for tracing across services.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation ID for event chains.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the request ID.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the name of the operation (request type name).
    /// </summary>
    public required string OperationName { get; set; }

    /// <summary>
    /// Gets or sets the type of operation (Command, Query, etc.).
    /// </summary>
    public required string OperationType { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the operation.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the username who performed the operation.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the operation started.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the request data (serialized for logging purposes).
    /// </summary>
    /// <remarks>
    /// Be careful with sensitive data. Use <see cref="IAuditable.ShouldAuditRequestData"/>
    /// to control what data is captured.
    /// </remarks>
    public string? RequestData { get; set; }

    /// <summary>
    /// Gets or sets the response data (serialized for logging purposes).
    /// </summary>
    /// <remarks>
    /// Be careful with sensitive data. Use <see cref="IAuditable.ShouldAuditResponseData"/>
    /// to control what data is captured.
    /// </remarks>
    public string? ResponseData { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the error type if the operation failed.
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the client IP address.
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the audit entry.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Marker interface for requests that should be audited.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on commands and queries that require audit trails.
/// The <see cref="Mvp24Hours.Infrastructure.Cqrs.Behaviors.AuditBehavior{TRequest, TResponse}"/>
/// will automatically create audit entries for these requests.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TransferMoneyCommand : IMediatorCommand&lt;TransferResult&gt;, IAuditable
/// {
///     public decimal Amount { get; init; }
///     public string FromAccount { get; init; }
///     public string ToAccount { get; init; }
///     
///     // Audit request data (amount, accounts)
///     public bool ShouldAuditRequestData => true;
///     
///     // Audit response data (transfer result)
///     public bool ShouldAuditResponseData => true;
/// }
/// </code>
/// </example>
public interface IAuditable
{
    /// <summary>
    /// Gets whether the request data should be included in the audit entry.
    /// Default is false to prevent logging sensitive data.
    /// </summary>
    bool ShouldAuditRequestData => false;

    /// <summary>
    /// Gets whether the response data should be included in the audit entry.
    /// Default is false to prevent logging sensitive data.
    /// </summary>
    bool ShouldAuditResponseData => false;

    /// <summary>
    /// Gets additional metadata to include in the audit entry.
    /// </summary>
    IReadOnlyDictionary<string, string>? GetAuditMetadata() => null;
}

/// <summary>
/// In-memory implementation of <see cref="IAuditStore"/> for testing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> This implementation stores entries in memory and
/// is not suitable for production use. Use a persistent store implementation.
/// </para>
/// </remarks>
public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly List<AuditEntry> _entries = [];
    private readonly object _lock = new();

    /// <summary>
    /// Gets a read-only view of all audit entries.
    /// </summary>
    public IReadOnlyList<AuditEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveBatchAsync(IEnumerable<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _entries.AddRange(entries);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all audit entries.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }

    /// <summary>
    /// Gets entries filtered by correlation ID.
    /// </summary>
    public IReadOnlyList<AuditEntry> GetByCorrelationId(string correlationId)
    {
        lock (_lock)
        {
            return _entries.Where(e => e.CorrelationId == correlationId).ToList();
        }
    }

    /// <summary>
    /// Gets entries filtered by user ID.
    /// </summary>
    public IReadOnlyList<AuditEntry> GetByUserId(string userId)
    {
        lock (_lock)
        {
            return _entries.Where(e => e.UserId == userId).ToList();
        }
    }

    /// <summary>
    /// Gets entries filtered by operation name.
    /// </summary>
    public IReadOnlyList<AuditEntry> GetByOperationName(string operationName)
    {
        lock (_lock)
        {
            return _entries.Where(e => e.OperationName == operationName).ToList();
        }
    }
}

