//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Extension methods for configuring Mvp24Hours tracing with dependency injection.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient methods for registering activity enrichers
/// and configuring tracing-related services in the DI container.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// services.AddMvp24HoursTracing(options =>
/// {
///     options.AddEnricher&lt;CorrelationIdEnricher&gt;();
///     options.AddEnricher&lt;TenantContextEnricher&gt;();
/// });
/// 
/// // With OpenTelemetry:
/// services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder.AddMvp24HoursSources();
///     });
/// </code>
/// </remarks>
public static class TracingServiceExtensions
{
    /// <summary>
    /// Adds Mvp24Hours tracing services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursTracing(
        this IServiceCollection services,
        Action<TracingOptions>? configure = null)
    {
        var options = new TracingOptions();
        configure?.Invoke(options);

        // Register options
        services.AddSingleton(options);

        // Register enrichers
        foreach (var enricherType in options.EnricherTypes)
        {
            services.AddSingleton(typeof(IActivityEnricher), enricherType);
        }

        foreach (var enricherInstance in options.EnricherInstances)
        {
            services.AddSingleton(enricherInstance);
        }

        // Register composite enricher
        services.AddSingleton<IActivityEnricher>(sp =>
        {
            var enrichers = sp.GetServices<IActivityEnricher>();
            return new CompositeActivityEnricher(enrichers);
        });

        // Register trace context accessor
        services.AddSingleton<ITraceContextAccessor, TraceContextAccessor>();

        return services;
    }

    /// <summary>
    /// Adds default Mvp24Hours activity enrichers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursDefaultEnrichers(this IServiceCollection services)
    {
        services.AddSingleton<IActivityEnricher>(new CorrelationIdEnricher());
        services.AddSingleton<IActivityEnricher>(new UserContextEnricher());
        services.AddSingleton<IActivityEnricher>(new TenantContextEnricher());
        return services;
    }

    /// <summary>
    /// Adds a custom activity enricher.
    /// </summary>
    /// <typeparam name="TEnricher">The enricher type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActivityEnricher<TEnricher>(this IServiceCollection services)
        where TEnricher : class, IActivityEnricher
    {
        services.AddSingleton<IActivityEnricher, TEnricher>();
        return services;
    }

    /// <summary>
    /// Adds a custom activity enricher instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="enricher">The enricher instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActivityEnricher(
        this IServiceCollection services,
        IActivityEnricher enricher)
    {
        services.AddSingleton(enricher);
        return services;
    }
}

/// <summary>
/// Options for configuring Mvp24Hours tracing.
/// </summary>
public class TracingOptions
{
    internal List<Type> EnricherTypes { get; } = new();
    internal List<IActivityEnricher> EnricherInstances { get; } = new();

    /// <summary>
    /// Gets or sets whether to enable automatic correlation ID propagation.
    /// </summary>
    public bool EnableCorrelationIdPropagation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add user context to traces.
    /// </summary>
    public bool EnableUserContext { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add tenant context to traces.
    /// </summary>
    public bool EnableTenantContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for traces.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the service version for traces.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Adds an activity enricher by type.
    /// </summary>
    /// <typeparam name="TEnricher">The enricher type.</typeparam>
    /// <returns>The options for chaining.</returns>
    public TracingOptions AddEnricher<TEnricher>() where TEnricher : IActivityEnricher
    {
        EnricherTypes.Add(typeof(TEnricher));
        return this;
    }

    /// <summary>
    /// Adds an activity enricher instance.
    /// </summary>
    /// <param name="enricher">The enricher instance.</param>
    /// <returns>The options for chaining.</returns>
    public TracingOptions AddEnricher(IActivityEnricher enricher)
    {
        EnricherInstances.Add(enricher);
        return this;
    }
}

/// <summary>
/// Interface for accessing trace context information.
/// </summary>
public interface ITraceContextAccessor
{
    /// <summary>
    /// Gets the current trace ID.
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    /// Gets the current span ID.
    /// </summary>
    string? SpanId { get; }

    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets a baggage item by key.
    /// </summary>
    string? GetBaggageItem(string key);

    /// <summary>
    /// Sets a baggage item.
    /// </summary>
    void SetBaggageItem(string key, string value);

    /// <summary>
    /// Gets the current Activity.
    /// </summary>
    Activity? CurrentActivity { get; }
}

/// <summary>
/// Default implementation of <see cref="ITraceContextAccessor"/>.
/// </summary>
public class TraceContextAccessor : ITraceContextAccessor
{
    /// <inheritdoc />
    public string? TraceId => Activity.Current?.TraceId.ToString();

    /// <inheritdoc />
    public string? SpanId => Activity.Current?.SpanId.ToString();

    /// <inheritdoc />
    public string? CorrelationId
    {
        get
        {
            var activity = Activity.Current;
            if (activity == null) return null;

            return activity.GetBaggageItem("correlation.id") 
                   ?? activity.TraceId.ToString();
        }
    }

    /// <inheritdoc />
    public Activity? CurrentActivity => Activity.Current;

    /// <inheritdoc />
    public string? GetBaggageItem(string key)
    {
        return Activity.Current?.GetBaggageItem(key);
    }

    /// <inheritdoc />
    public void SetBaggageItem(string key, string value)
    {
        Activity.Current?.SetBaggage(key, value);
    }
}

/// <summary>
/// Extension methods for OpenTelemetry TracerProviderBuilder to add Mvp24Hours sources.
/// </summary>
/// <remarks>
/// <para>
/// Use these extensions when configuring OpenTelemetry tracing:
/// </para>
/// <code>
/// services.AddOpenTelemetry()
///     .WithTracing(builder =>
///     {
///         builder
///             .AddMvp24HoursSources()
///             .AddAspNetCoreInstrumentation()
///             .AddOtlpExporter();
///     });
/// </code>
/// </remarks>
public static class OpenTelemetryBuilderExtensions
{
    /// <summary>
    /// Gets all Mvp24Hours ActivitySource names.
    /// </summary>
    /// <returns>Array of activity source names.</returns>
    public static string[] GetMvp24HoursActivitySourceNames()
    {
        return Mvp24HoursActivitySources.AllSourceNames;
    }

    /// <summary>
    /// Gets activity source names for specific modules.
    /// </summary>
    /// <param name="includeCore">Include Core module.</param>
    /// <param name="includePipe">Include Pipe module.</param>
    /// <param name="includeCqrs">Include CQRS module.</param>
    /// <param name="includeData">Include Data module.</param>
    /// <param name="includeRabbitMQ">Include RabbitMQ module.</param>
    /// <param name="includeWebAPI">Include WebAPI module.</param>
    /// <param name="includeCaching">Include Caching module.</param>
    /// <param name="includeCronJob">Include CronJob module.</param>
    /// <param name="includeInfrastructure">Include Infrastructure module.</param>
    /// <returns>Array of selected activity source names.</returns>
    public static string[] GetMvp24HoursActivitySourceNames(
        bool includeCore = true,
        bool includePipe = true,
        bool includeCqrs = true,
        bool includeData = true,
        bool includeRabbitMQ = true,
        bool includeWebAPI = true,
        bool includeCaching = true,
        bool includeCronJob = true,
        bool includeInfrastructure = true)
    {
        var sources = new List<string>();

        if (includeCore) sources.Add(Mvp24HoursActivitySources.Core.Name);
        if (includePipe) sources.Add(Mvp24HoursActivitySources.Pipe.Name);
        if (includeCqrs) sources.Add(Mvp24HoursActivitySources.Cqrs.Name);
        if (includeData) sources.Add(Mvp24HoursActivitySources.Data.Name);
        if (includeRabbitMQ) sources.Add(Mvp24HoursActivitySources.RabbitMQ.Name);
        if (includeWebAPI) sources.Add(Mvp24HoursActivitySources.WebAPI.Name);
        if (includeCaching) sources.Add(Mvp24HoursActivitySources.Caching.Name);
        if (includeCronJob) sources.Add(Mvp24HoursActivitySources.CronJob.Name);
        if (includeInfrastructure) sources.Add(Mvp24HoursActivitySources.Infrastructure.Name);

        return sources.ToArray();
    }
}

