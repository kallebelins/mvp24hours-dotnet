//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Observability;

/// <summary>
/// Marker interface for service operations that should be audited.
/// </summary>
/// <remarks>
/// <para>
/// Operations implementing this interface will have audit trail entries created
/// when executed through observable application services.
/// </para>
/// <para>
/// <strong>Audit Entry Contents:</strong>
/// <list type="bullet">
/// <item>Operation name and type (Query/Command)</item>
/// <item>User and tenant information</item>
/// <item>Timestamp and duration</item>
/// <item>Success/failure status</item>
/// <item>Entity type and IDs affected</item>
/// </list>
/// </para>
/// </remarks>
public interface IAuditableOperation
{
    /// <summary>
    /// Gets whether the operation input data should be included in the audit trail.
    /// </summary>
    /// <remarks>
    /// Be careful with sensitive data. Only enable for non-sensitive operations.
    /// </remarks>
    bool ShouldAuditInputData => false;

    /// <summary>
    /// Gets whether the operation output data should be included in the audit trail.
    /// </summary>
    bool ShouldAuditOutputData => false;

    /// <summary>
    /// Gets additional metadata to include in the audit entry.
    /// </summary>
    /// <returns>A dictionary of key-value pairs to include in the audit.</returns>
    IDictionary<string, string>? GetAuditMetadata() => null;
}

/// <summary>
/// Represents an audit entry for an application service operation.
/// </summary>
public class ApplicationAuditEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for this audit entry.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the operation performed (e.g., "Add", "Modify", "GetById").
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of operation (Query, Command).
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the service that performed the operation.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type being operated on.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID(s) affected by the operation.
    /// </summary>
    public string? EntityIds { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the causation ID for tracing.
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// Gets or sets the user ID who performed the operation.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets the user name who performed the operation.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for the operation.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the operation started.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the duration of the operation in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error type if the operation failed.
    /// </summary>
    public string? ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the serialized input data (if audit is enabled).
    /// </summary>
    public string? InputData { get; set; }

    /// <summary>
    /// Gets or sets the serialized output data (if audit is enabled).
    /// </summary>
    public string? OutputData { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the audit entry.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the client IP address.
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Gets or sets the user agent string.
    /// </summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// Interface for storing application audit entries.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to persist audit entries to your desired storage:
/// <list type="bullet">
/// <item>Database (SQL Server, PostgreSQL, MongoDB)</item>
/// <item>File system</item>
/// <item>External audit service</item>
/// <item>Event stream (Kafka, RabbitMQ)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class EfCoreAuditStore : IApplicationAuditStore
/// {
///     private readonly AuditDbContext _context;
/// 
///     public EfCoreAuditStore(AuditDbContext context)
///     {
///         _context = context;
///     }
/// 
///     public async Task SaveAsync(ApplicationAuditEntry entry, CancellationToken cancellationToken)
///     {
///         _context.AuditEntries.Add(entry);
///         await _context.SaveChangesAsync(cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface IApplicationAuditStore
{
    /// <summary>
    /// Saves an audit entry to the store.
    /// </summary>
    /// <param name="entry">The audit entry to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(ApplicationAuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit entries by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit entries matching the correlation ID.</returns>
    Task<IList<ApplicationAuditEntry>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit entries by user ID within a time range.
    /// </summary>
    /// <param name="userId">The user ID to search for.</param>
    /// <param name="from">Start of the time range.</param>
    /// <param name="to">End of the time range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit entries matching the criteria.</returns>
    Task<IList<ApplicationAuditEntry>> GetByUserIdAsync(string userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries audit entries by entity type and ID.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of audit entries for the entity.</returns>
    Task<IList<ApplicationAuditEntry>> GetByEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
}

