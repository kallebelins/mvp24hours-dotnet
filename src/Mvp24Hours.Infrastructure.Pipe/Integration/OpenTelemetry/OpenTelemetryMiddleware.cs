//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.OpenTelemetry;

/// <summary>
/// Pipeline middleware that integrates with OpenTelemetry for distributed tracing.
/// Creates spans for each pipeline execution with detailed metadata.
/// </summary>
/// <remarks>
/// Creates a new OpenTelemetry middleware.
/// </remarks>
/// <param name="logger">Optional logger.</param>
/// <param name="options">Optional configuration options.</param>
public class OpenTelemetryMiddleware(
    ILogger<OpenTelemetryMiddleware>? logger = null,
    OpenTelemetryOptions? options = null) : IPipelineMiddleware
{
    private static readonly ActivitySource ActivitySource = new("Mvp24Hours.Pipeline", "1.0.0");
    private readonly ILogger<OpenTelemetryMiddleware>? _logger = logger;
    private readonly OpenTelemetryOptions _options = options ?? new OpenTelemetryOptions();

    /// <inheritdoc/>
    public int Order => -1000; // Execute early to wrap all other middleware

    /// <inheritdoc/>
    public async Task ExecuteAsync(
        IPipelineMessage message,
        Func<Task> next,
        CancellationToken cancellationToken = default)
    {
        string spanName = "Pipeline.Operation";

        using Activity? activity = ActivitySource.StartActivity(spanName, ActivityKind.Internal);

        if (activity == null)
        {
            await next();
            return;
        }

        // Add common tags
        SetActivityTags(activity, message);

        bool startLocked = message.IsLocked;
        bool startFaulted = message.IsFaulty;

        try
        {
            await next();

            // Record result status
            if (!startLocked && message.IsLocked)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Pipeline locked");
                activity.SetTag("pipeline.locked", true);
                activity.SetTag("pipeline.lock_reason", GetLockReason(message));
            }
            else if (!startFaulted && message.IsFaulty)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Pipeline faulted");
                activity.SetTag("pipeline.faulted", true);
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("pipeline.success", true);
            }

            // Add message count
            if (_options.IncludeMessageDetails)
            {
                activity.SetTag("pipeline.message_count", message.Messages?.Count ?? 0);
            }
        }
        catch (OperationCanceledException ex)
        {
            activity.SetStatus(ActivityStatusCode.Error, "Cancelled");
            activity.SetTag("pipeline.cancelled", true);
            RecordException(activity, ex);
            throw;
        }
        catch (Exception ex)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            RecordException(activity, ex);
            throw;
        }
    }

    private void SetActivityTags(Activity activity, IPipelineMessage message)
    {
        if (message.Token != null)
        {
            activity.SetTag("pipeline.token", message.Token.ToString());
        }

        if (_options.IncludeInputDetails)
        {
            activity.SetTag("pipeline.input.is_locked", message.IsLocked);
            activity.SetTag("pipeline.input.is_faulted", message.IsFaulty);
        }

        // Add custom tags from options
        if (_options.CustomTags != null)
        {
            foreach (KeyValuePair<string, object> tag in _options.CustomTags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }
    }

    private static void RecordException(Activity activity, Exception exception)
    {
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message }
        };

        if (exception.StackTrace != null)
        {
            tags.Add("exception.stacktrace", exception.StackTrace);
        }

        activity.AddEvent(new ActivityEvent("exception", tags: tags));
    }

    private static string? GetLockReason(IPipelineMessage message)
    {
        if (message.Messages == null || message.Messages.Count == 0)
        {
            return null;
        }

        var errors = new List<string>();
        foreach (IMessageResult msg in message.Messages)
        {
            if (msg.Type == Core.Enums.MessageType.Error)
            {
                errors.Add(msg.Message);
            }
        }

        return errors.Count > 0 ? string.Join("; ", errors) : null;
    }
}

/// <summary>
/// Synchronous version of OpenTelemetry middleware for non-async pipelines.
/// </summary>
/// <remarks>
/// Creates a new synchronous OpenTelemetry middleware.
/// </remarks>
/// <param name="logger">Optional logger.</param>
/// <param name="options">Optional configuration options.</param>
public class OpenTelemetryMiddlewareSync(
    ILogger<OpenTelemetryMiddlewareSync>? logger = null,
    OpenTelemetryOptions? options = null) : IPipelineMiddlewareSync
{
    private static readonly ActivitySource ActivitySource = new("Mvp24Hours.Pipeline", "1.0.0");
    private readonly ILogger<OpenTelemetryMiddlewareSync>? _logger = logger;
    private readonly OpenTelemetryOptions _options = options ?? new OpenTelemetryOptions();

    /// <inheritdoc/>
    public int Order => -1000;

    /// <inheritdoc/>
    public void Execute(IPipelineMessage message, Action next)
    {
        string spanName = "Pipeline.Operation";

        using Activity? activity = ActivitySource.StartActivity(spanName, ActivityKind.Internal);

        if (activity == null)
        {
            next();
            return;
        }

        SetActivityTags(activity, message);
        bool startLocked = message.IsLocked;
        bool startFaulted = message.IsFaulty;

        try
        {
            next();

            if (!startLocked && message.IsLocked)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Pipeline locked");
                activity.SetTag("pipeline.locked", true);
            }
            else if (!startFaulted && message.IsFaulty)
            {
                activity.SetStatus(ActivityStatusCode.Error, "Pipeline faulted");
                activity.SetTag("pipeline.faulted", true);
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("pipeline.success", true);
            }
        }
        catch (Exception ex)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            RecordException(activity, ex);
            throw;
        }
    }

    private void SetActivityTags(Activity activity, IPipelineMessage message)
    {
        if (message.Token != null)
        {
            activity.SetTag("pipeline.token", message.Token.ToString());
        }

        if (_options.IncludeInputDetails)
        {
            activity.SetTag("pipeline.input.is_locked", message.IsLocked);
            activity.SetTag("pipeline.input.is_faulted", message.IsFaulty);
        }
    }

    private static void RecordException(Activity activity, Exception exception)
    {
        var tags = new ActivityTagsCollection
        {
            { "exception.type", exception.GetType().FullName },
            { "exception.message", exception.Message }
        };

        if (exception.StackTrace != null)
        {
            tags.Add("exception.stacktrace", exception.StackTrace);
        }

        activity.AddEvent(new ActivityEvent("exception", tags: tags));
    }
}
