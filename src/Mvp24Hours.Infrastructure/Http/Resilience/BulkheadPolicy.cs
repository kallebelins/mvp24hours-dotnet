//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Resilience
{
    /// <summary>
    /// Options for configuring a Bulkhead policy.
    /// </summary>
    public class BulkheadPolicyOptions
    {
        /// <summary>
        /// Gets or sets whether the bulkhead policy is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of parallel executions allowed. Default is 10.
        /// </summary>
        public int MaxParallelization { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum number of queued actions waiting for execution. Default is 100.
        /// </summary>
        public int MaxQueuedActions { get; set; } = 100;
    }

    /// <summary>
    /// Bulkhead policy implementation that limits the number of concurrent executions
    /// to prevent resource exhaustion and provide isolation between different operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Bulkhead pattern isolates resources (like thread pools) to prevent a failure
    /// in one part of the system from cascading and exhausting all resources.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Limits concurrent executions to prevent resource exhaustion</item>
    /// <item>Configurable queue size for waiting requests</item>
    /// <item>Automatic rejection when capacity is exceeded</item>
    /// <item>Detailed logging of bulkhead events</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class BulkheadPolicy : IHttpResiliencePolicy
    {
        private readonly ILogger<BulkheadPolicy>? _logger;
        private readonly BulkheadPolicyOptions _options;
        private readonly AsyncBulkheadPolicy<HttpResponseMessage> _policy;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkheadPolicy"/> class.
        /// </summary>
        /// <param name="options">The bulkhead policy options.</param>
        /// <param name="logger">Optional logger instance.</param>
        public BulkheadPolicy(BulkheadPolicyOptions options, ILogger<BulkheadPolicy>? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
            _policy = CreatePolicy();
        }

        /// <inheritdoc/>
        public string PolicyName => "BulkheadPolicy";

        /// <summary>
        /// Gets the number of available slots for parallel execution.
        /// </summary>
        public int AvailableParallelization => _policy.BulkheadAvailableCount;

        /// <summary>
        /// Gets the number of available slots in the queue.
        /// </summary>
        public int AvailableQueueSlots => _policy.QueueAvailableCount;

        /// <inheritdoc/>
        public Task<HttpResponseMessage> ExecuteAsync(
            Func<HttpRequestMessage> requestFactory,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync,
            CancellationToken cancellationToken = default)
        {
            if (requestFactory == null)
            {
                throw new ArgumentNullException(nameof(requestFactory));
            }

            if (sendAsync == null)
            {
                throw new ArgumentNullException(nameof(sendAsync));
            }

            if (!_options.Enabled)
            {
                var request = requestFactory();
                return sendAsync(request, cancellationToken);
            }

            return _policy.ExecuteAsync(
                async (ct) =>
                {
                    var request = requestFactory();
                    return await sendAsync(request, ct);
                },
                cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncPolicy<HttpResponseMessage> GetPollyPolicy() => _policy;

        private AsyncBulkheadPolicy<HttpResponseMessage> CreatePolicy()
        {
            return Policy.BulkheadAsync<HttpResponseMessage>(
                maxParallelization: _options.MaxParallelization,
                maxQueuingActions: _options.MaxQueuedActions,
                onBulkheadRejectedAsync: (context) =>
                {
                    OnBulkheadRejected(context);
                    return Task.CompletedTask;
                });
        }

        private void OnBulkheadRejected(Context context)
        {
            _logger?.LogWarning(
                "Bulkhead rejected execution. " +
                "Available parallelization: {AvailableParallelization}/{MaxParallelization}, " +
                "Available queue slots: {AvailableQueueSlots}/{MaxQueuedActions}",
                AvailableParallelization, _options.MaxParallelization,
                AvailableQueueSlots, _options.MaxQueuedActions);
        }
    }
}

