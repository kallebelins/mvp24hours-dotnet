//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Interface for enriching Activities with custom tags, events, and context.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to add custom enrichment logic to tracing spans.
/// Enrichers are called at the start and end of activities to add
/// context-specific information like user IDs, tenant IDs, or business data.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public class TenantActivityEnricher : IActivityEnricher
/// {
///     private readonly ITenantProvider _tenantProvider;
///     
///     public TenantActivityEnricher(ITenantProvider tenantProvider)
///     {
///         _tenantProvider = tenantProvider;
///     }
///     
///     public void EnrichOnStart(Activity activity, object? context)
///     {
///         var tenantId = _tenantProvider.GetTenantId();
///         activity.SetTag("tenant.id", tenantId);
///     }
///     
///     public void EnrichOnEnd(Activity activity, object? context, Exception? exception)
///     {
///         // Add any end-of-activity enrichment
///     }
/// }
/// </code>
/// </remarks>
public interface IActivityEnricher
{
    /// <summary>
    /// Gets the order in which this enricher should be executed.
    /// Lower values execute first.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Enriches the activity when it starts.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="context">Optional context object with additional data.</param>
    void EnrichOnStart(Activity activity, object? context = null);

    /// <summary>
    /// Enriches the activity when it ends.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="context">Optional context object with additional data.</param>
    /// <param name="exception">Exception if the operation failed, null if successful.</param>
    void EnrichOnEnd(Activity activity, object? context = null, Exception? exception = null);
}

/// <summary>
/// Interface for enriching Activities of a specific type.
/// </summary>
/// <typeparam name="TContext">The type of context used for enrichment.</typeparam>
public interface IActivityEnricher<in TContext> : IActivityEnricher
{
    /// <summary>
    /// Enriches the activity when it starts with typed context.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="context">The typed context object.</param>
    void EnrichOnStart(Activity activity, TContext context);

    /// <summary>
    /// Enriches the activity when it ends with typed context.
    /// </summary>
    /// <param name="activity">The activity to enrich.</param>
    /// <param name="context">The typed context object.</param>
    /// <param name="exception">Exception if the operation failed, null if successful.</param>
    void EnrichOnEnd(Activity activity, TContext context, Exception? exception = null);

    void IActivityEnricher.EnrichOnStart(Activity activity, object? context)
    {
        if (context is TContext typedContext)
        {
            EnrichOnStart(activity, typedContext);
        }
    }

    void IActivityEnricher.EnrichOnEnd(Activity activity, object? context, Exception? exception)
    {
        if (context is TContext typedContext)
        {
            EnrichOnEnd(activity, typedContext, exception);
        }
    }
}

