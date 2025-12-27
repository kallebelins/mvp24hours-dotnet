//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Base class for activity enrichers providing common functionality.
/// </summary>
/// <remarks>
/// <para>
/// This abstract class provides a convenient base for implementing
/// <see cref="IActivityEnricher"/>. Override the methods you need.
/// </para>
/// </remarks>
public abstract class ActivityEnricherBase : IActivityEnricher
{
    /// <inheritdoc />
    public virtual int Order => 0;

    /// <inheritdoc />
    public virtual void EnrichOnStart(Activity activity, object? context = null)
    {
        // Override in derived classes
    }

    /// <inheritdoc />
    public virtual void EnrichOnEnd(Activity activity, object? context = null, Exception? exception = null)
    {
        // Override in derived classes
    }
}

/// <summary>
/// Base class for activity enrichers with typed context.
/// </summary>
/// <typeparam name="TContext">The type of context used for enrichment.</typeparam>
public abstract class ActivityEnricherBase<TContext> : IActivityEnricher<TContext>
{
    /// <inheritdoc />
    public virtual int Order => 0;

    /// <inheritdoc />
    public abstract void EnrichOnStart(Activity activity, TContext context);

    /// <inheritdoc />
    public abstract void EnrichOnEnd(Activity activity, TContext context, Exception? exception = null);
}

/// <summary>
/// Enricher that adds correlation ID to activities.
/// </summary>
public class CorrelationIdEnricher : ActivityEnricherBase
{
    /// <summary>
    /// Gets or sets the header name for correlation ID.
    /// </summary>
    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// Gets or sets the function to retrieve the correlation ID.
    /// </summary>
    public Func<string?>? GetCorrelationId { get; set; }

    /// <inheritdoc />
    public override int Order => -100; // Run early

    /// <inheritdoc />
    public override void EnrichOnStart(Activity activity, object? context = null)
    {
        var correlationId = GetCorrelationId?.Invoke() ?? Activity.Current?.GetBaggageItem("correlation.id");
        
        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag(SemanticTags.CorrelationId, correlationId);
            activity.SetBaggage("correlation.id", correlationId);
        }
    }
}

/// <summary>
/// Enricher that adds user context to activities.
/// </summary>
public class UserContextEnricher : ActivityEnricherBase
{
    /// <summary>
    /// Gets or sets the function to retrieve the user ID.
    /// </summary>
    public Func<string?>? GetUserId { get; set; }

    /// <summary>
    /// Gets or sets the function to retrieve the user name.
    /// </summary>
    public Func<string?>? GetUserName { get; set; }

    /// <summary>
    /// Gets or sets the function to retrieve user roles.
    /// </summary>
    public Func<IEnumerable<string>?>? GetUserRoles { get; set; }

    /// <inheritdoc />
    public override int Order => -90; // Run after correlation

    /// <inheritdoc />
    public override void EnrichOnStart(Activity activity, object? context = null)
    {
        var userId = GetUserId?.Invoke();
        if (!string.IsNullOrEmpty(userId))
        {
            activity.SetTag(SemanticTags.EnduserId, userId);
        }

        var userName = GetUserName?.Invoke();
        if (!string.IsNullOrEmpty(userName))
        {
            activity.SetTag(SemanticTags.EnduserName, userName);
        }

        var roles = GetUserRoles?.Invoke();
        if (roles != null)
        {
            activity.SetTag(SemanticTags.EnduserRoles, string.Join(",", roles));
        }
    }
}

/// <summary>
/// Enricher that adds tenant context to activities.
/// </summary>
public class TenantContextEnricher : ActivityEnricherBase
{
    /// <summary>
    /// Gets or sets the function to retrieve the tenant ID.
    /// </summary>
    public Func<string?>? GetTenantId { get; set; }

    /// <summary>
    /// Gets or sets the function to retrieve the tenant name.
    /// </summary>
    public Func<string?>? GetTenantName { get; set; }

    /// <inheritdoc />
    public override int Order => -80; // Run after user context

    /// <inheritdoc />
    public override void EnrichOnStart(Activity activity, object? context = null)
    {
        var tenantId = GetTenantId?.Invoke();
        if (!string.IsNullOrEmpty(tenantId))
        {
            activity.SetTag(SemanticTags.TenantId, tenantId);
            activity.SetBaggage("tenant.id", tenantId);
        }

        var tenantName = GetTenantName?.Invoke();
        if (!string.IsNullOrEmpty(tenantName))
        {
            activity.SetTag(SemanticTags.TenantName, tenantName);
        }
    }
}

/// <summary>
/// Composite enricher that runs multiple enrichers in order.
/// </summary>
public class CompositeActivityEnricher : IActivityEnricher
{
    private readonly IEnumerable<IActivityEnricher> _enrichers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeActivityEnricher"/> class.
    /// </summary>
    /// <param name="enrichers">The enrichers to compose.</param>
    public CompositeActivityEnricher(IEnumerable<IActivityEnricher> enrichers)
    {
        _enrichers = enrichers?.OrderBy(e => e.Order) ?? Enumerable.Empty<IActivityEnricher>();
    }

    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public void EnrichOnStart(Activity activity, object? context = null)
    {
        foreach (var enricher in _enrichers)
        {
            try
            {
                enricher.EnrichOnStart(activity, context);
            }
            catch
            {
                // Enrichers should not fail the operation
            }
        }
    }

    /// <inheritdoc />
    public void EnrichOnEnd(Activity activity, object? context = null, Exception? exception = null)
    {
        foreach (var enricher in _enrichers)
        {
            try
            {
                enricher.EnrichOnEnd(activity, context, exception);
            }
            catch
            {
                // Enrichers should not fail the operation
            }
        }
    }
}

