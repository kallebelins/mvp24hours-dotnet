//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Integration.OpenTelemetry;
using System;
using System.Diagnostics;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for OpenTelemetry integration with Mvp24Hours pipelines.
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        /// <summary>
        /// The ActivitySource name used by the pipeline OpenTelemetry integration.
        /// Use this to register the source with OpenTelemetry SDK.
        /// </summary>
        public const string ActivitySourceName = "Mvp24Hours.Pipeline";

        /// <summary>
        /// Adds OpenTelemetry tracing middleware to the pipeline.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This middleware creates spans for each pipeline operation using System.Diagnostics.Activity.
        /// To see these spans in your tracing backend, you need to register the ActivitySource:
        /// </para>
        /// <code>
        /// builder.Services.AddOpenTelemetry()
        ///     .WithTracing(tracing => tracing
        ///         .AddSource(OpenTelemetryExtensions.ActivitySourceName)
        ///         .AddOtlpExporter());
        /// </code>
        /// </remarks>
        public static IServiceCollection AddPipelineOpenTelemetry(
            this IServiceCollection services,
            Action<OpenTelemetryOptions>? configure = null)
        {
            var options = new OpenTelemetryOptions();
            configure?.Invoke(options);
            services.TryAddSingleton(options);

            // Register async middleware
            services.AddSingleton<IPipelineMiddleware>(sp =>
            {
                var logger = sp.GetService<ILogger<OpenTelemetryMiddleware>>();
                var opts = sp.GetService<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
                return new OpenTelemetryMiddleware(logger, opts);
            });

            // Register sync middleware
            services.AddSingleton<IPipelineMiddlewareSync>(sp =>
            {
                var logger = sp.GetService<ILogger<OpenTelemetryMiddlewareSync>>();
                var opts = sp.GetService<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();
                return new OpenTelemetryMiddlewareSync(logger, opts);
            });

            return services;
        }

        /// <summary>
        /// Creates an Activity for pipeline execution.
        /// </summary>
        /// <param name="activityName">The name of the activity.</param>
        /// <param name="kind">The activity kind.</param>
        /// <returns>The created activity, or null if no listener is registered.</returns>
        public static Activity? StartPipelineActivity(string activityName, ActivityKind kind = ActivityKind.Internal)
        {
            return new ActivitySource(ActivitySourceName, "1.0.0")
                .StartActivity(activityName, kind);
        }

        /// <summary>
        /// Adds pipeline-specific tags to an activity.
        /// </summary>
        /// <param name="activity">The activity to tag.</param>
        /// <param name="pipelineName">The name of the pipeline.</param>
        /// <param name="operationCount">The number of operations in the pipeline.</param>
        /// <returns>The activity for chaining.</returns>
        public static Activity? SetPipelineTags(
            this Activity? activity,
            string pipelineName,
            int? operationCount = null)
        {
            if (activity == null)
                return null;

            activity.SetTag("pipeline.name", pipelineName);
            
            if (operationCount.HasValue)
            {
                activity.SetTag("pipeline.operation_count", operationCount.Value);
            }

            return activity;
        }

        /// <summary>
        /// Records a pipeline event on the activity.
        /// </summary>
        /// <param name="activity">The activity.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="description">Optional event description.</param>
        /// <returns>The activity for chaining.</returns>
        public static Activity? RecordPipelineEvent(
            this Activity? activity,
            string eventName,
            string? description = null)
        {
            if (activity == null)
                return null;

            var tags = new ActivityTagsCollection();
            
            if (description != null)
            {
                tags.Add("event.description", description);
            }

            activity.AddEvent(new ActivityEvent(eventName, tags: tags));

            return activity;
        }

        /// <summary>
        /// Sets the pipeline result status on the activity.
        /// </summary>
        /// <param name="activity">The activity.</param>
        /// <param name="isSuccess">Whether the pipeline succeeded.</param>
        /// <param name="errorMessage">Optional error message for failed pipelines.</param>
        /// <returns>The activity for chaining.</returns>
        public static Activity? SetPipelineResult(
            this Activity? activity,
            bool isSuccess,
            string? errorMessage = null)
        {
            if (activity == null)
                return null;

            if (isSuccess)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
                activity.SetTag("pipeline.result", "success");
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Error, errorMessage);
                activity.SetTag("pipeline.result", "failure");
                
                if (errorMessage != null)
                {
                    activity.SetTag("pipeline.error_message", errorMessage);
                }
            }

            return activity;
        }
    }
}

