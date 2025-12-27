//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;

namespace Mvp24Hours.Core.Observability;

/// <summary>
/// Centralized ActivitySource definitions for all Mvp24Hours modules.
/// </summary>
/// <remarks>
/// <para>
/// Activity sources are used to create distributed tracing spans for operations
/// across different modules. Each module has its own activity source with
/// semantic naming conventions following OpenTelemetry standards.
/// </para>
/// <para>
/// <strong>Naming Convention:</strong>
/// Activity source names follow the pattern: <c>Mvp24Hours.{Module}</c>
/// This ensures proper categorization and filtering in observability platforms.
/// </para>
/// <para>
/// <strong>Usage with OpenTelemetry:</strong>
/// </para>
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(tracing =>
///     {
///         tracing
///             .AddSource(Mvp24HoursActivitySources.Core.Name)
///             .AddSource(Mvp24HoursActivitySources.Pipe.Name)
///             .AddSource(Mvp24HoursActivitySources.Cqrs.Name)
///             // Or add all at once:
///             .AddMvp24HoursSources()
///             .AddOtlpExporter();
///     });
/// </code>
/// </remarks>
public static class Mvp24HoursActivitySources
{
    /// <summary>
    /// The version of all ActivitySources.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// All source names for bulk registration.
    /// </summary>
    public static readonly string[] AllSourceNames =
    [
        Core.Name,
        Pipe.Name,
        Cqrs.Name,
        Data.Name,
        RabbitMQ.Name,
        WebAPI.Name,
        Caching.Name,
        CronJob.Name,
        Infrastructure.Name
    ];

    /// <summary>
    /// Core module activity source for fundamental operations.
    /// </summary>
    public static class Core
    {
        /// <summary>The name of the Core ActivitySource.</summary>
        public const string Name = "Mvp24Hours.Core";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for Core operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for guard validation operations.</summary>
            public const string Guard = "Mvp24Hours.Core.Guard";
            /// <summary>Activity for value object operations.</summary>
            public const string ValueObject = "Mvp24Hours.Core.ValueObject";
            /// <summary>Activity for specification evaluation.</summary>
            public const string Specification = "Mvp24Hours.Core.Specification";
            /// <summary>Activity for business result operations.</summary>
            public const string BusinessResult = "Mvp24Hours.Core.BusinessResult";
        }
    }

    /// <summary>
    /// Pipe module activity source for pipeline operations.
    /// </summary>
    public static class Pipe
    {
        /// <summary>The name of the Pipe ActivitySource.</summary>
        public const string Name = "Mvp24Hours.Pipe";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for Pipe operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for full pipeline execution.</summary>
            public const string Pipeline = "Mvp24Hours.Pipe.Pipeline";
            /// <summary>Activity for individual operation execution.</summary>
            public const string Operation = "Mvp24Hours.Pipe.Operation";
            /// <summary>Activity for pipeline middleware execution.</summary>
            public const string Middleware = "Mvp24Hours.Pipe.Middleware";
            /// <summary>Activity for parallel operation execution.</summary>
            public const string ParallelExecution = "Mvp24Hours.Pipe.ParallelExecution";
            /// <summary>Activity for saga/compensation execution.</summary>
            public const string Saga = "Mvp24Hours.Pipe.Saga";
        }
    }

    /// <summary>
    /// CQRS module activity source for mediator operations.
    /// </summary>
    public static class Cqrs
    {
        /// <summary>The name of the CQRS ActivitySource.</summary>
        public const string Name = "Mvp24Hours.Cqrs";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for CQRS operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for mediator request handling.</summary>
            public const string Request = "Mvp24Hours.Cqrs.Request";
            /// <summary>Activity for command handling.</summary>
            public const string Command = "Mvp24Hours.Cqrs.Command";
            /// <summary>Activity for query handling.</summary>
            public const string Query = "Mvp24Hours.Cqrs.Query";
            /// <summary>Activity for notification publishing.</summary>
            public const string Notification = "Mvp24Hours.Cqrs.Notification";
            /// <summary>Activity for domain event dispatching.</summary>
            public const string DomainEvent = "Mvp24Hours.Cqrs.DomainEvent";
            /// <summary>Activity for integration event publishing.</summary>
            public const string IntegrationEvent = "Mvp24Hours.Cqrs.IntegrationEvent";
            /// <summary>Activity for behavior execution in pipeline.</summary>
            public const string Behavior = "Mvp24Hours.Cqrs.Behavior";
            /// <summary>Activity for saga orchestration.</summary>
            public const string Saga = "Mvp24Hours.Cqrs.Saga";
        }
    }

    /// <summary>
    /// Data module activity source for repository and database operations.
    /// </summary>
    public static class Data
    {
        /// <summary>The name of the Data ActivitySource.</summary>
        public const string Name = "Mvp24Hours.Data";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for Data operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for repository query operations.</summary>
            public const string Query = "Mvp24Hours.Data.Query";
            /// <summary>Activity for repository command operations.</summary>
            public const string Command = "Mvp24Hours.Data.Command";
            /// <summary>Activity for SaveChanges operations.</summary>
            public const string SaveChanges = "Mvp24Hours.Data.SaveChanges";
            /// <summary>Activity for transaction operations.</summary>
            public const string Transaction = "Mvp24Hours.Data.Transaction";
            /// <summary>Activity for bulk operations.</summary>
            public const string BulkOperation = "Mvp24Hours.Data.BulkOperation";
            /// <summary>Activity for slow query tracking.</summary>
            public const string SlowQuery = "Mvp24Hours.Data.SlowQuery";
        }
    }

    /// <summary>
    /// RabbitMQ module activity source for messaging operations.
    /// </summary>
    public static class RabbitMQ
    {
        /// <summary>The name of the RabbitMQ ActivitySource.</summary>
        public const string Name = "Mvp24Hours.RabbitMQ";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for RabbitMQ operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for message publishing.</summary>
            public const string Publish = "Mvp24Hours.RabbitMQ.Publish";
            /// <summary>Activity for message consuming.</summary>
            public const string Consume = "Mvp24Hours.RabbitMQ.Consume";
            /// <summary>Activity for request/response pattern.</summary>
            public const string RequestResponse = "Mvp24Hours.RabbitMQ.RequestResponse";
            /// <summary>Activity for batch processing.</summary>
            public const string BatchConsume = "Mvp24Hours.RabbitMQ.BatchConsume";
            /// <summary>Activity for saga message handling.</summary>
            public const string SagaMessage = "Mvp24Hours.RabbitMQ.SagaMessage";
        }
    }

    /// <summary>
    /// WebAPI module activity source for HTTP operations.
    /// </summary>
    public static class WebAPI
    {
        /// <summary>The name of the WebAPI ActivitySource.</summary>
        public const string Name = "Mvp24Hours.WebAPI";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for WebAPI operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for request handling.</summary>
            public const string Request = "Mvp24Hours.WebAPI.Request";
            /// <summary>Activity for middleware execution.</summary>
            public const string Middleware = "Mvp24Hours.WebAPI.Middleware";
            /// <summary>Activity for exception handling.</summary>
            public const string ExceptionHandler = "Mvp24Hours.WebAPI.ExceptionHandler";
            /// <summary>Activity for rate limiting.</summary>
            public const string RateLimiting = "Mvp24Hours.WebAPI.RateLimiting";
            /// <summary>Activity for idempotency check.</summary>
            public const string Idempotency = "Mvp24Hours.WebAPI.Idempotency";
        }
    }

    /// <summary>
    /// Caching module activity source for cache operations.
    /// </summary>
    public static class Caching
    {
        /// <summary>The name of the Caching ActivitySource.</summary>
        public const string Name = "Mvp24Hours.Caching";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for Caching operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for cache get operations.</summary>
            public const string Get = "Mvp24Hours.Caching.Get";
            /// <summary>Activity for cache set operations.</summary>
            public const string Set = "Mvp24Hours.Caching.Set";
            /// <summary>Activity for cache invalidation.</summary>
            public const string Invalidate = "Mvp24Hours.Caching.Invalidate";
            /// <summary>Activity for cache refresh.</summary>
            public const string Refresh = "Mvp24Hours.Caching.Refresh";
        }
    }

    /// <summary>
    /// CronJob module activity source for scheduled job operations.
    /// </summary>
    public static class CronJob
    {
        /// <summary>The name of the CronJob ActivitySource.</summary>
        public const string Name = "Mvp24Hours.CronJob";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for CronJob operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for job execution.</summary>
            public const string JobExecution = "Mvp24Hours.CronJob.JobExecution";
            /// <summary>Activity for job scheduling.</summary>
            public const string JobScheduling = "Mvp24Hours.CronJob.JobScheduling";
        }
    }

    /// <summary>
    /// Infrastructure module activity source for cross-cutting concerns.
    /// </summary>
    public static class Infrastructure
    {
        /// <summary>The name of the Infrastructure ActivitySource.</summary>
        public const string Name = "Mvp24Hours.Infrastructure";
        
        /// <summary>The ActivitySource instance.</summary>
        public static readonly ActivitySource Source = new(Name, Version);

        /// <summary>Activity names for Infrastructure operations.</summary>
        public static class Activities
        {
            /// <summary>Activity for HTTP client requests.</summary>
            public const string HttpClient = "Mvp24Hours.Infrastructure.HttpClient";
            /// <summary>Activity for email sending.</summary>
            public const string Email = "Mvp24Hours.Infrastructure.Email";
            /// <summary>Activity for SMS sending.</summary>
            public const string Sms = "Mvp24Hours.Infrastructure.Sms";
            /// <summary>Activity for file storage operations.</summary>
            public const string FileStorage = "Mvp24Hours.Infrastructure.FileStorage";
            /// <summary>Activity for distributed locking.</summary>
            public const string DistributedLock = "Mvp24Hours.Infrastructure.DistributedLock";
            /// <summary>Activity for background jobs.</summary>
            public const string BackgroundJob = "Mvp24Hours.Infrastructure.BackgroundJob";
        }
    }
}

