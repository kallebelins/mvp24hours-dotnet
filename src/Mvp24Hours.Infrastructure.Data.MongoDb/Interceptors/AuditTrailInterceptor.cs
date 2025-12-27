//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors
{
    /// <summary>
    /// MongoDB interceptor that creates an audit trail for all CRUD operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor logs detailed audit trail entries for all database operations,
    /// including the user, timestamp, operation type, and optionally the entity data.
    /// </para>
    /// <para>
    /// The audit trail can be stored to:
    /// <list type="bullet">
    ///   <item>Logger (ILogger) - for standard logging infrastructure</item>
    ///   <item>Custom IAuditStore - for persistent audit storage (when available)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Privacy Consideration:</strong> Entity data logging is disabled by default.
    /// Enable only when needed and ensure compliance with data protection regulations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with basic audit trail:
    /// services.AddScoped&lt;IMongoDbInterceptor, AuditTrailInterceptor&gt;();
    /// 
    /// // Register with entity data logging:
    /// services.AddScoped&lt;IMongoDbInterceptor&gt;(sp =>
    ///     new AuditTrailInterceptor(
    ///         sp.GetService&lt;ICurrentUserProvider&gt;(),
    ///         sp.GetService&lt;IClock&gt;(),
    ///         sp.GetService&lt;ILogger&lt;AuditTrailInterceptor&gt;&gt;(),
    ///         logEntityData: true));
    /// </code>
    /// </example>
    public class AuditTrailInterceptor : MongoDbInterceptorBase
    {
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IClock _clock;
        private readonly ILogger _logger;
        private readonly bool _logEntityData;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditTrailInterceptor"/> class.
        /// </summary>
        /// <param name="currentUserProvider">Optional provider for the current user.</param>
        /// <param name="clock">Optional clock for getting current time.</param>
        /// <param name="logger">Optional logger for structured audit logging.</param>
        /// <param name="logEntityData">If true, logs the entity data in the audit trail. Default is false for privacy.</param>
        public AuditTrailInterceptor(
            ICurrentUserProvider currentUserProvider = null,
            IClock clock = null,
            ILogger<AuditTrailInterceptor> logger = null,
            bool logEntityData = false)
        {
            _currentUserProvider = currentUserProvider;
            _clock = clock;
            _logger = logger;
            _logEntityData = logEntityData;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 3 // Prevent deep object graphs
            };
        }

        /// <inheritdoc />
        public override int Order => int.MaxValue; // Run last to capture final state

        /// <inheritdoc />
        public override Task OnAfterInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            LogAuditEntry(AuditOperation.Insert, entity);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task OnAfterUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            LogAuditEntry(AuditOperation.Update, entity);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task OnAfterDeleteAsync<T>(T entity, bool wasSoftDeleted, CancellationToken cancellationToken = default)
        {
            var operation = wasSoftDeleted ? AuditOperation.SoftDelete : AuditOperation.Delete;
            LogAuditEntry(operation, entity);
            return Task.CompletedTask;
        }

        private void LogAuditEntry<T>(AuditOperation operation, T entity) where T : class, IEntityBase
        {
            var auditEntry = new AuditTrailEntry
            {
                Timestamp = GetCurrentTime(),
                UserId = GetCurrentUser(),
                Operation = operation.ToString(),
                EntityType = typeof(T).Name,
                EntityId = entity.EntityKey?.ToString(),
                EntityData = _logEntityData ? TrySerializeEntity(entity) : null
            };

            _logger?.LogInformation(
                "MongoDB Audit Trail: {Operation} on {EntityType} (Id: {EntityId}) by {UserId} at {Timestamp}",
                auditEntry.Operation,
                auditEntry.EntityType,
                auditEntry.EntityId,
                auditEntry.UserId,
                auditEntry.Timestamp);

            if (_logEntityData && !string.IsNullOrEmpty(auditEntry.EntityData))
            {
                _logger?.LogDebug("Entity Data: {EntityData}", auditEntry.EntityData);
            }
        }

        private string TrySerializeEntity<T>(T entity) where T : class
        {
            try
            {
                return JsonSerializer.Serialize(entity, _jsonOptions);
            }
            catch (Exception ex)
            {
                return $"[Serialization failed: {ex.Message}]";
            }
        }

        private DateTime GetCurrentTime()
        {
            return _clock?.UtcNow ?? DateTime.UtcNow;
        }

        private string GetCurrentUser()
        {
            return _currentUserProvider?.UserId ?? "System";
        }
    }

    /// <summary>
    /// Represents an audit trail entry for MongoDB operations.
    /// </summary>
    public class AuditTrailEntry
    {
        /// <summary>
        /// Gets or sets the timestamp of the operation.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the user who performed the operation.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of operation performed.
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Gets or sets the type name of the entity.
        /// </summary>
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets the ID of the entity.
        /// </summary>
        public string EntityId { get; set; }

        /// <summary>
        /// Gets or sets the serialized entity data (optional, for detailed auditing).
        /// </summary>
        public string EntityData { get; set; }
    }

    /// <summary>
    /// Defines the types of audit operations.
    /// </summary>
    public enum AuditOperation
    {
        /// <summary>
        /// Entity was inserted/created.
        /// </summary>
        Insert,

        /// <summary>
        /// Entity was updated/modified.
        /// </summary>
        Update,

        /// <summary>
        /// Entity was physically deleted.
        /// </summary>
        Delete,

        /// <summary>
        /// Entity was soft deleted (marked as deleted but not removed).
        /// </summary>
        SoftDelete
    }
}

