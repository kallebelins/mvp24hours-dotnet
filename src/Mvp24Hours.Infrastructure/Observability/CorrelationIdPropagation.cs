//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Observability
{
    /// <summary>
    /// Helper class for propagating correlation IDs across infrastructure operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Correlation IDs are used to track requests across multiple services and operations.
    /// This class provides utilities to extract, set, and propagate correlation IDs
    /// through Activity baggage and custom headers.
    /// </para>
    /// <para>
    /// <strong>Correlation ID Sources:</strong>
    /// <list type="bullet">
    /// <item>Activity.Current?.Id (from OpenTelemetry trace context)</item>
    /// <item>Baggage (custom correlation ID set by application)</item>
    /// <item>Generated GUID (fallback if no correlation ID exists)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class CorrelationIdPropagation
    {
        /// <summary>
        /// Baggage key for correlation ID.
        /// </summary>
        public const string CorrelationIdBaggageKey = "correlation.id";

        /// <summary>
        /// HTTP header name for correlation ID.
        /// </summary>
        public const string CorrelationIdHeaderName = "X-Correlation-Id";

        /// <summary>
        /// Gets the current correlation ID from Activity context or generates a new one.
        /// </summary>
        /// <returns>The correlation ID.</returns>
        /// <remarks>
        /// <para>
        /// This method checks the following sources in order:
        /// 1. Activity.Current?.Baggage[CorrelationIdBaggageKey]
        /// 2. Activity.Current?.Id (trace ID)
        /// 3. Generated GUID (fallback)
        /// </para>
        /// </remarks>
        public static string GetCorrelationId()
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                // Check baggage first (explicit correlation ID)
                var correlationId = activity.GetBaggageItem(CorrelationIdBaggageKey);
                if (!string.IsNullOrEmpty(correlationId))
                {
                    return correlationId;
                }

                // Fallback to trace ID
                if (!string.IsNullOrEmpty(activity.Id))
                {
                    return activity.Id;
                }
            }

            // Generate new correlation ID as fallback
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Sets the correlation ID in the current Activity baggage.
        /// </summary>
        /// <param name="correlationId">The correlation ID to set.</param>
        /// <remarks>
        /// <para>
        /// If no Activity exists, this method does nothing. The correlation ID
        /// will be propagated to child activities and can be extracted via
        /// <see cref="GetCorrelationId"/>.
        /// </para>
        /// </remarks>
        public static void SetCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                return;
            }

            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetBaggage(CorrelationIdBaggageKey, correlationId);
            }
        }

        /// <summary>
        /// Ensures a correlation ID exists in the current Activity context.
        /// </summary>
        /// <returns>The correlation ID (existing or newly created).</returns>
        /// <remarks>
        /// <para>
        /// If a correlation ID already exists in baggage, it is returned.
        /// Otherwise, a new correlation ID is generated and set in baggage.
        /// </para>
        /// </remarks>
        public static string EnsureCorrelationId()
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                var existing = activity.GetBaggageItem(CorrelationIdBaggageKey);
                if (!string.IsNullOrEmpty(existing))
                {
                    return existing;
                }
            }

            var correlationId = Guid.NewGuid().ToString("N");
            SetCorrelationId(correlationId);
            return correlationId;
        }

        /// <summary>
        /// Adds correlation ID to a dictionary (e.g., HTTP headers or metadata).
        /// </summary>
        /// <param name="headers">The dictionary to add the correlation ID to.</param>
        /// <param name="correlationId">Optional correlation ID. If not provided, gets from context.</param>
        /// <remarks>
        /// <para>
        /// This method adds the correlation ID to the dictionary using the standard
        /// header name <see cref="CorrelationIdHeaderName"/>. If the correlation ID
        /// is not provided, it is retrieved from the current Activity context.
        /// </para>
        /// </remarks>
        public static void AddCorrelationIdHeader(
            System.Collections.Generic.IDictionary<string, string> headers,
            string? correlationId = null)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            var id = correlationId ?? GetCorrelationId();
            headers[CorrelationIdHeaderName] = id;
        }
    }
}

