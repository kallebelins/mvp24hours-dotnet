//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Typed;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe.Integration.OpenTelemetry
{
    /// <summary>
    /// A typed operation wrapper that adds OpenTelemetry tracing to the wrapped operation.
    /// </summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    public class TracingTypedOperation<TInput, TOutput> : ITypedOperationAsync<TInput, TOutput>
    {
        private static readonly ActivitySource ActivitySource = new("Mvp24Hours.Pipeline", "1.0.0");
        
        private readonly ITypedOperationAsync<TInput, TOutput> _innerOperation;
        private readonly ILogger<TracingTypedOperation<TInput, TOutput>>? _logger;
        private readonly string _operationName;

        /// <summary>
        /// Creates a new tracing typed operation.
        /// </summary>
        /// <param name="innerOperation">The operation to wrap.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="operationName">Optional custom operation name for the span.</param>
        public TracingTypedOperation(
            ITypedOperationAsync<TInput, TOutput> innerOperation,
            ILogger<TracingTypedOperation<TInput, TOutput>>? logger = null,
            string? operationName = null)
        {
            _innerOperation = innerOperation ?? throw new ArgumentNullException(nameof(innerOperation));
            _logger = logger;
            _operationName = operationName ?? innerOperation.GetType().Name;
        }

        /// <inheritdoc/>
        public bool IsRequired => _innerOperation.IsRequired;

        /// <inheritdoc/>
        public async Task<IOperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var spanName = $"TypedOperation.{_operationName}";

            using var activity = ActivitySource.StartActivity(spanName, ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("operation.type", _innerOperation.GetType().FullName);
                activity.SetTag("operation.input_type", typeof(TInput).Name);
                activity.SetTag("operation.output_type", typeof(TOutput).Name);
                activity.SetTag("operation.is_required", _innerOperation.IsRequired);
            }

            try
            {
                var result = await _innerOperation.ExecuteAsync(input, cancellationToken);

                if (activity != null)
                {
                    if (result.IsSuccess)
                    {
                        activity.SetStatus(ActivityStatusCode.Ok);
                        activity.SetTag("operation.result", "success");
                    }
                    else
                    {
                        activity.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
                        activity.SetTag("operation.result", "failure");
                        activity.SetTag("operation.error_message", result.ErrorMessage);
                    }

                    activity.SetTag("operation.message_count", result.Messages.Count);
                }

                return result;
            }
            catch (OperationCanceledException ex)
            {
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, "Cancelled");
                    activity.SetTag("operation.cancelled", true);
                    RecordException(activity, ex);
                }
                throw;
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    RecordException(activity, ex);
                }

                _logger?.LogError(ex, "Error executing operation {OperationName}", _operationName);
                return OperationResult<TOutput>.Failure(ex);
            }
        }

        /// <inheritdoc/>
        public async Task RollbackAsync(TInput input, CancellationToken cancellationToken = default)
        {
            var spanName = $"TypedOperation.{_operationName}.Rollback";

            using var activity = ActivitySource.StartActivity(spanName, ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("operation.type", _innerOperation.GetType().FullName);
                activity.SetTag("operation.is_rollback", true);
            }

            try
            {
                await _innerOperation.RollbackAsync(input, cancellationToken);
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    RecordException(activity, ex);
                }
                throw;
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
}

