//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Context
{
    /// <summary>
    /// Extension methods for registering pipeline context services in the DI container.
    /// </summary>
    public static class PipelineContextServiceExtensions
    {
        /// <summary>
        /// Adds pipeline context services to the service collection.
        /// This includes the context accessor and default context propagation middleware.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineContext(this IServiceCollection services)
        {
            return services.AddPipelineContext(options => { });
        }

        /// <summary>
        /// Adds pipeline context services to the service collection with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Action to configure the context options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineContext(
            this IServiceCollection services,
            Action<PipelineContextOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var options = new PipelineContextOptions();
            configure(options);

            // Register context accessor as singleton (uses AsyncLocal internally)
            services.TryAddSingleton<IPipelineContextAccessor, PipelineContextAccessor>();

            // Register context propagation middleware
            if (options.EnableContextPropagation)
            {
                var propagationOptions = new ContextPropagationOptions
                {
                    Order = options.ContextPropagationOrder,
                    EnableActivityTracing = options.EnableActivityTracing,
                    RootActivityName = options.RootActivityName,
                    StoreContextInMessage = options.StoreContextInMessage,
                    CaptureInitialSnapshot = options.CaptureInitialSnapshot,
                    CaptureFinalSnapshot = options.CaptureFinalSnapshot,
                    CaptureErrorSnapshot = options.CaptureErrorSnapshot,
                    DefaultTenantId = options.DefaultTenantId
                };

                services.AddSingleton(propagationOptions);
                services.TryAddTransient<ContextPropagationMiddleware>();
            }

            // Register operation activity middleware
            if (options.EnableOperationActivityTracing)
            {
                var activityOptions = new OperationActivityOptions
                {
                    Order = options.OperationActivityOrder,
                    EnableOperationTracing = true,
                    OperationActivityKind = options.OperationActivityKind,
                    IncludeContentCount = options.IncludeContentCountInTracing
                };

                services.AddSingleton(activityOptions);
                services.TryAddTransient<OperationActivityMiddleware>();
            }

            return services;
        }

        /// <summary>
        /// Adds pipeline context services with activity tracing enabled.
        /// This is a convenience method for enabling OpenTelemetry-compatible tracing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="activitySourceName">Optional custom activity source name.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineContextWithTracing(
            this IServiceCollection services,
            string? activitySourceName = null)
        {
            return services.AddPipelineContext(options =>
            {
                options.EnableActivityTracing = true;
                options.EnableOperationActivityTracing = true;
                options.RootActivityName = activitySourceName ?? "Pipeline.Execute";
            });
        }

        /// <summary>
        /// Adds pipeline context services with state snapshot capture enabled.
        /// This is useful for debugging and auditing pipeline execution.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPipelineContextWithSnapshots(
            this IServiceCollection services)
        {
            return services.AddPipelineContext(options =>
            {
                options.CaptureInitialSnapshot = true;
                options.CaptureFinalSnapshot = true;
                options.CaptureErrorSnapshot = true;
            });
        }
    }

    /// <summary>
    /// Configuration options for pipeline context services.
    /// </summary>
    public sealed class PipelineContextOptions
    {
        /// <summary>
        /// Gets or sets whether to enable context propagation middleware.
        /// Default is true.
        /// </summary>
        public bool EnableContextPropagation { get; set; } = true;

        /// <summary>
        /// Gets or sets the execution order for context propagation middleware.
        /// Default is -1000 (runs very early).
        /// </summary>
        public int ContextPropagationOrder { get; set; } = -1000;

        /// <summary>
        /// Gets or sets whether to enable Activity tracing for OpenTelemetry.
        /// Default is true.
        /// </summary>
        public bool EnableActivityTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the root activity.
        /// Default is "Pipeline.Execute".
        /// </summary>
        public string? RootActivityName { get; set; }

        /// <summary>
        /// Gets or sets whether to store the context in the message after execution.
        /// Default is true.
        /// </summary>
        public bool StoreContextInMessage { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture an initial state snapshot.
        /// Default is false.
        /// </summary>
        public bool CaptureInitialSnapshot { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to capture a final state snapshot.
        /// Default is false.
        /// </summary>
        public bool CaptureFinalSnapshot { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to capture an error snapshot on exception.
        /// Default is true.
        /// </summary>
        public bool CaptureErrorSnapshot { get; set; } = true;

        /// <summary>
        /// Gets or sets a default tenant ID to apply when creating new contexts.
        /// Default is null.
        /// </summary>
        public string? DefaultTenantId { get; set; }

        /// <summary>
        /// Gets or sets whether to enable operation-level activity tracing.
        /// Default is false (only root activity by default).
        /// </summary>
        public bool EnableOperationActivityTracing { get; set; } = false;

        /// <summary>
        /// Gets or sets the execution order for operation activity middleware.
        /// Default is -900 (runs after context propagation).
        /// </summary>
        public int OperationActivityOrder { get; set; } = -900;

        /// <summary>
        /// Gets or sets the ActivityKind for operation-level activities.
        /// Default is Internal.
        /// </summary>
        public System.Diagnostics.ActivityKind OperationActivityKind { get; set; } = System.Diagnostics.ActivityKind.Internal;

        /// <summary>
        /// Gets or sets whether to include content count in activity tags.
        /// Default is true.
        /// </summary>
        public bool IncludeContentCountInTracing { get; set; } = true;
    }
}

