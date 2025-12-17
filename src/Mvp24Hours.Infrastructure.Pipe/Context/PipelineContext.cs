//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Pipe.Context
{
    /// <summary>
    /// Default implementation of <see cref="IPipelineContext"/> providing comprehensive
    /// context management for pipeline execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation supports:
    /// <list type="bullet">
    /// <item>Correlation and causation tracking for distributed tracing</item>
    /// <item>User and tenant context for multi-tenant scenarios</item>
    /// <item>Flexible metadata storage for custom data</item>
    /// <item>State snapshots for debugging and auditing</item>
    /// <item>Activity integration for OpenTelemetry compatibility</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class PipelineContext : IPipelineContext
    {
        #region [ Static ]

        /// <summary>
        /// The ActivitySource for pipeline tracing.
        /// </summary>
        public static readonly ActivitySource ActivitySource = new("Mvp24Hours.Infrastructure.Pipe", "1.0.0");

        #endregion

        #region [ Fields ]

        private readonly Dictionary<string, object> _metadata = new();
        private readonly List<PipelineStateSnapshot> _snapshots = new();
        private int _snapshotSequence = 0;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new pipeline context with an auto-generated correlation ID.
        /// </summary>
        public PipelineContext()
            : this(Guid.NewGuid().ToString("N"))
        {
        }

        /// <summary>
        /// Creates a new pipeline context with the specified correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID for this context.</param>
        public PipelineContext(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                throw new ArgumentException("Correlation ID cannot be null or empty.", nameof(correlationId));
            }

            CorrelationId = correlationId;
            CreatedAt = DateTime.UtcNow;
        }

        #endregion

        #region [ Identification ]

        /// <inheritdoc />
        public string CorrelationId { get; }

        /// <inheritdoc />
        public string? CausationId { get; set; }

        /// <inheritdoc />
        public string? ParentContextId { get; set; }

        /// <inheritdoc />
        public DateTime CreatedAt { get; }

        #endregion

        #region [ User Context ]

        /// <inheritdoc />
        public string? UserId { get; set; }

        /// <inheritdoc />
        public string? UserName { get; set; }

        /// <inheritdoc />
        public string? TenantId { get; set; }

        /// <inheritdoc />
        public bool HasUser => !string.IsNullOrEmpty(UserId);

        #endregion

        #region [ Metadata ]

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Metadata => _metadata;

        /// <inheritdoc />
        public T? GetMetadata<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default;
            }

            if (_metadata.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return default;
        }

        /// <inheritdoc />
        public void SetMetadata<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));
            }

            if (value is null)
            {
                _metadata.Remove(key);
            }
            else
            {
                _metadata[key] = value;
            }
        }

        /// <inheritdoc />
        public bool RemoveMetadata(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _metadata.Remove(key);
        }

        /// <inheritdoc />
        public bool HasMetadata(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _metadata.ContainsKey(key);
        }

        #endregion

        #region [ Diagnostics ]

        /// <inheritdoc />
        public Activity? CurrentActivity { get; set; }

        /// <inheritdoc />
        public string? TraceId => CurrentActivity?.TraceId.ToString() ?? Activity.Current?.TraceId.ToString();

        /// <inheritdoc />
        public string? SpanId => CurrentActivity?.SpanId.ToString() ?? Activity.Current?.SpanId.ToString();

        /// <inheritdoc />
        public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));
            }

            var activity = ActivitySource.StartActivity(operationName, kind);

            if (activity != null)
            {
                activity.SetTag("correlation.id", CorrelationId);

                if (!string.IsNullOrEmpty(CausationId))
                {
                    activity.SetTag("causation.id", CausationId);
                }

                if (!string.IsNullOrEmpty(UserId))
                {
                    activity.SetTag("user.id", UserId);
                }

                if (!string.IsNullOrEmpty(TenantId))
                {
                    activity.SetTag("tenant.id", TenantId);
                }

                CurrentActivity = activity;
            }

            return activity;
        }

        #endregion

        #region [ State Snapshots ]

        /// <inheritdoc />
        public IReadOnlyList<PipelineStateSnapshot> Snapshots => _snapshots;

        /// <inheritdoc />
        public void CaptureSnapshot(string operationName, object? state, string? description = null)
        {
            if (string.IsNullOrEmpty(operationName))
            {
                throw new ArgumentException("Operation name cannot be null or empty.", nameof(operationName));
            }

            var snapshot = new PipelineStateSnapshot
            {
                OperationName = operationName,
                CorrelationId = CorrelationId,
                Description = description,
                State = state,
                SpanId = SpanId,
                Metadata = _metadata.Count > 0 ? new Dictionary<string, object>(_metadata) : null,
                SequenceNumber = ++_snapshotSequence
            };

            _snapshots.Add(snapshot);
        }

        /// <inheritdoc />
        public PipelineStateSnapshot? LastSnapshot => _snapshots.Count > 0 ? _snapshots[^1] : null;

        /// <inheritdoc />
        public void ClearSnapshots()
        {
            _snapshots.Clear();
            // Keep sequence number to maintain uniqueness
        }

        #endregion

        #region [ Factory Methods ]

        /// <inheritdoc />
        public IPipelineContext CreateChildContext()
        {
            var childContext = new PipelineContext
            {
                CausationId = CorrelationId,
                ParentContextId = CorrelationId,
                UserId = UserId,
                UserName = UserName,
                TenantId = TenantId
            };

            // Copy metadata to child
            foreach (var kvp in _metadata)
            {
                childContext._metadata[kvp.Key] = kvp.Value;
            }

            return childContext;
        }

        /// <inheritdoc />
        public IPipelineContext CloneWithCorrelationId(string newCorrelationId)
        {
            var clonedContext = new PipelineContext(newCorrelationId)
            {
                CausationId = CausationId,
                ParentContextId = ParentContextId,
                UserId = UserId,
                UserName = UserName,
                TenantId = TenantId
            };

            // Copy metadata
            foreach (var kvp in _metadata)
            {
                clonedContext._metadata[kvp.Key] = kvp.Value;
            }

            return clonedContext;
        }

        #endregion

        #region [ Integration ]

        /// <inheritdoc />
        public void PopulateFromRequestContext(IRequestContext requestContext)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            CausationId = requestContext.CausationId;
            UserId = requestContext.UserId;
            UserName = requestContext.UserName;
            TenantId = requestContext.TenantId;

            // Copy items from request context to metadata
            foreach (var item in requestContext.Items)
            {
                _metadata[item.Key] = item.Value;
            }
        }

        /// <inheritdoc />
        public IRequestContext ToRequestContext()
        {
            var requestContext = new DefaultRequestContext(CorrelationId)
            {
                CausationId = CausationId,
                UserId = UserId,
                UserName = UserName,
                TenantId = TenantId
            };

            // Copy metadata to request context items
            foreach (var kvp in _metadata)
            {
                requestContext.Items[kvp.Key] = kvp.Value;
            }

            return requestContext;
        }

        #endregion

        #region [ Static Factory Methods ]

        /// <summary>
        /// Creates a new context from an existing <see cref="IRequestContext"/>.
        /// </summary>
        /// <param name="requestContext">The request context to create from.</param>
        /// <returns>A new pipeline context populated from the request context.</returns>
        public static PipelineContext FromRequestContext(IRequestContext requestContext)
        {
            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext));
            }

            var context = new PipelineContext(requestContext.CorrelationId);
            context.PopulateFromRequestContext(requestContext);
            return context;
        }

        /// <summary>
        /// Creates a new context with user information.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="tenantId">Optional tenant ID.</param>
        /// <returns>A new pipeline context with user information.</returns>
        public static PipelineContext WithUser(string userId, string? userName = null, string? tenantId = null)
        {
            return new PipelineContext
            {
                UserId = userId,
                UserName = userName,
                TenantId = tenantId
            };
        }

        /// <summary>
        /// Creates a new context for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <returns>A new pipeline context for the tenant.</returns>
        public static PipelineContext ForTenant(string tenantId)
        {
            return new PipelineContext
            {
                TenantId = tenantId
            };
        }

        #endregion
    }
}

