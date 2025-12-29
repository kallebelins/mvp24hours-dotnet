//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Extension methods for integrating OpenTelemetry Logging with Mvp24Hours.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient methods for configuring OpenTelemetry logging
/// with Mvp24Hours-specific defaults, resource attributes, and log enrichment.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// builder.Logging.AddOpenTelemetry(options =>
/// {
///     options.IncludeFormattedMessage = true;
///     options.IncludeScopes = true;
///     options.ParseStateValues = true;
///     
///     // Add Mvp24Hours resource attributes
///     options.SetResourceBuilder(
///         ResourceBuilder.CreateDefault()
///             .AddMvp24HoursResourceAttributes("MyService", "1.0.0")
///             .AddService("MyService"));
///     
///     options.AddOtlpExporter();
/// });
/// </code>
/// </remarks>
public static class OpenTelemetryLoggingExtensions
{
    /// <summary>
    /// Gets the default resource attributes for Mvp24Hours services.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="serviceVersion">The service version.</param>
    /// <param name="environment">Optional environment name.</param>
    /// <returns>Dictionary of resource attributes.</returns>
    public static Dictionary<string, object> GetMvp24HoursResourceAttributes(
        string serviceName,
        string? serviceVersion = null,
        string? environment = null)
    {
        var attributes = new Dictionary<string, object>
        {
            [LogResourceAttributes.ServiceName] = serviceName,
            [LogResourceAttributes.TelemetrySdkName] = "Mvp24Hours",
            [LogResourceAttributes.TelemetrySdkLanguage] = "dotnet",
            [LogResourceAttributes.TelemetrySdkVersion] = Mvp24HoursActivitySources.Version,
            [LogResourceAttributes.HostName] = Environment.MachineName,
            [LogResourceAttributes.ProcessPid] = Environment.ProcessId,
            [LogResourceAttributes.ProcessRuntimeName] = ".NET",
            [LogResourceAttributes.ProcessRuntimeVersion] = Environment.Version.ToString()
        };

        if (!string.IsNullOrEmpty(serviceVersion))
        {
            attributes[LogResourceAttributes.ServiceVersion] = serviceVersion;
        }
        else
        {
            // Try to get version from entry assembly
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var version = entryAssembly.GetName().Version;
                if (version != null)
                {
                    attributes[LogResourceAttributes.ServiceVersion] = version.ToString();
                }
            }
        }

        if (!string.IsNullOrEmpty(environment))
        {
            attributes[LogResourceAttributes.DeploymentEnvironment] = environment;
        }
        else
        {
            // Try to get from ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            if (!string.IsNullOrEmpty(env))
            {
                attributes[LogResourceAttributes.DeploymentEnvironment] = env;
            }
        }

        // Generate service instance ID
        attributes[LogResourceAttributes.ServiceInstanceId] = $"{Environment.MachineName}-{Environment.ProcessId}";

        return attributes;
    }

    /// <summary>
    /// Gets recommended OpenTelemetry logging configuration values.
    /// </summary>
    /// <returns>A logging configuration helper.</returns>
    public static OpenTelemetryLoggingConfig GetRecommendedConfig()
    {
        return new OpenTelemetryLoggingConfig
        {
            IncludeFormattedMessage = true,
            IncludeScopes = true,
            ParseStateValues = true,
            AddExceptionDetails = true
        };
    }
}

/// <summary>
/// Helper class containing recommended OpenTelemetry logging configuration values.
/// </summary>
public class OpenTelemetryLoggingConfig
{
    /// <summary>
    /// Gets or sets whether to include the formatted message in log records.
    /// </summary>
    public bool IncludeFormattedMessage { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include logging scopes in log records.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to parse state values into individual attributes.
    /// </summary>
    public bool ParseStateValues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to add exception details to log records.
    /// </summary>
    public bool AddExceptionDetails { get; set; } = true;
}

/// <summary>
/// Provides methods for creating log scopes with OpenTelemetry-compatible attributes.
/// </summary>
public static class LogScopeFactory
{
    /// <summary>
    /// Creates a scope for HTTP request logging.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">URL path.</param>
    /// <param name="traceId">Optional trace ID.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginHttpScope(
        ILogger logger,
        string method,
        string path,
        string? traceId = null)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.HttpRequestMethod] = method,
            [SemanticTags.UrlPath] = path
        };

        if (!string.IsNullOrEmpty(traceId))
        {
            props["TraceId"] = traceId;
        }
        else if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
        }

        return logger.BeginScope(props);
    }

    /// <summary>
    /// Creates a scope for database operation logging.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbSystem">Database system (e.g., sqlserver, postgresql, mongodb).</param>
    /// <param name="operation">Operation type (e.g., SELECT, INSERT).</param>
    /// <param name="table">Optional table name.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginDbScope(
        ILogger logger,
        string dbSystem,
        string operation,
        string? table = null)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.DbSystem] = dbSystem,
            [SemanticTags.DbOperation] = operation
        };

        if (!string.IsNullOrEmpty(table))
        {
            props[SemanticTags.DbSqlTable] = table;
        }

        if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
        }

        return logger.BeginScope(props);
    }

    /// <summary>
    /// Creates a scope for messaging operation logging.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="system">Messaging system (e.g., rabbitmq, kafka).</param>
    /// <param name="destination">Queue or topic name.</param>
    /// <param name="messageId">Optional message ID.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginMessagingScope(
        ILogger logger,
        string system,
        string destination,
        string? messageId = null)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.MessagingSystem] = system,
            [SemanticTags.MessagingDestinationName] = destination
        };

        if (!string.IsNullOrEmpty(messageId))
        {
            props[SemanticTags.MessagingMessageId] = messageId;
        }

        if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
            props[SemanticTags.CorrelationId] = 
                Activity.Current.GetBaggageItem("correlation.id") 
                ?? Activity.Current.TraceId.ToString();
        }

        return logger.BeginScope(props);
    }

    /// <summary>
    /// Creates a scope for CQRS/Mediator operation logging.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="requestName">Request type name.</param>
    /// <param name="requestType">Request type (Command, Query, Notification).</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginMediatorScope(
        ILogger logger,
        string requestName,
        string requestType)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.MediatorRequestName] = requestName,
            [SemanticTags.MediatorRequestType] = requestType
        };

        if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
            props["SpanId"] = Activity.Current.SpanId.ToString();
        }

        return logger.BeginScope(props);
    }

    /// <summary>
    /// Creates a scope for pipeline operation logging.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pipelineName">Pipeline name.</param>
    /// <param name="operationName">Current operation name.</param>
    /// <param name="operationIndex">Operation index in pipeline.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginPipelineScope(
        ILogger logger,
        string pipelineName,
        string operationName,
        int operationIndex)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.PipelineName] = pipelineName,
            [SemanticTags.PipelineOperationName] = operationName,
            [SemanticTags.PipelineOperationIndex] = operationIndex
        };

        if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
        }

        return logger.BeginScope(props);
    }

    /// <summary>
    /// Creates a scope for cache operation logging.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheSystem">Cache system (redis, memory).</param>
    /// <param name="operation">Operation type (get, set, delete).</param>
    /// <param name="key">Cache key.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginCacheScope(
        ILogger logger,
        string cacheSystem,
        string operation,
        string key)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.CacheSystem] = cacheSystem,
            [SemanticTags.CacheOperation] = operation,
            [SemanticTags.CacheKey] = key
        };

        if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
        }

        return logger.BeginScope(props);
    }

    /// <summary>
    /// Creates a scope for background job logging.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jobId">Job ID.</param>
    /// <param name="jobType">Job type name.</param>
    /// <param name="attempt">Current attempt number.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginJobScope(
        ILogger logger,
        string jobId,
        string jobType,
        int attempt = 1)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.JobId] = jobId,
            [SemanticTags.JobType] = jobType,
            [SemanticTags.JobAttempt] = attempt
        };

        if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
        }

        return logger.BeginScope(props);
    }

    /// <summary>
    /// Creates a scope for error logging with structured exception details.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="errorCode">Optional application-specific error code.</param>
    /// <param name="errorCategory">Optional error category.</param>
    /// <returns>A disposable scope.</returns>
    public static IDisposable BeginErrorScope(
        ILogger logger,
        Exception exception,
        string? errorCode = null,
        string? errorCategory = null)
    {
        var props = new Dictionary<string, object>
        {
            [SemanticTags.ErrorType] = exception.GetType().FullName ?? exception.GetType().Name,
            [SemanticTags.ErrorMessage] = exception.Message
        };

        if (!string.IsNullOrEmpty(errorCode))
        {
            props[SemanticTags.ErrorCode] = errorCode;
        }

        if (!string.IsNullOrEmpty(errorCategory))
        {
            props[SemanticTags.ErrorCategory] = errorCategory;
        }

        if (Activity.Current != null)
        {
            props["TraceId"] = Activity.Current.TraceId.ToString();
        }

        return logger.BeginScope(props);
    }
}

/// <summary>
/// Helper class for structured logging with high-performance source-generated methods.
/// </summary>
/// <remarks>
/// <para>
/// This class demonstrates the pattern for creating high-performance log methods
/// using the <c>[LoggerMessage]</c> attribute. Copy these patterns to create
/// module-specific logger message classes.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // In your module, create a partial class:
/// public static partial class MyModuleLogMessages
/// {
///     [LoggerMessage(
///         EventId = 1001,
///         Level = LogLevel.Information,
///         Message = "Processing {RequestType} {RequestName}")]
///     public static partial void ProcessingRequest(
///         ILogger logger,
///         string requestType,
///         string requestName);
/// }
/// </code>
/// </remarks>
public static class LogMessagePatterns
{
    // Event ID ranges for Mvp24Hours modules:
    // 1000-1999: Core
    // 2000-2999: Pipe
    // 3000-3999: CQRS
    // 4000-4999: Data (EFCore/MongoDB)
    // 5000-5999: RabbitMQ
    // 6000-6999: WebAPI
    // 7000-7999: Caching
    // 8000-8999: CronJob
    // 9000-9999: Infrastructure

    /// <summary>
    /// Base event ID for Core module log events.
    /// </summary>
    public const int CoreBaseEventId = 1000;

    /// <summary>
    /// Base event ID for Pipe module log events.
    /// </summary>
    public const int PipeBaseEventId = 2000;

    /// <summary>
    /// Base event ID for CQRS module log events.
    /// </summary>
    public const int CqrsBaseEventId = 3000;

    /// <summary>
    /// Base event ID for Data module log events.
    /// </summary>
    public const int DataBaseEventId = 4000;

    /// <summary>
    /// Base event ID for RabbitMQ module log events.
    /// </summary>
    public const int RabbitMQBaseEventId = 5000;

    /// <summary>
    /// Base event ID for WebAPI module log events.
    /// </summary>
    public const int WebAPIBaseEventId = 6000;

    /// <summary>
    /// Base event ID for Caching module log events.
    /// </summary>
    public const int CachingBaseEventId = 7000;

    /// <summary>
    /// Base event ID for CronJob module log events.
    /// </summary>
    public const int CronJobBaseEventId = 8000;

    /// <summary>
    /// Base event ID for Infrastructure module log events.
    /// </summary>
    public const int InfrastructureBaseEventId = 9000;
}


