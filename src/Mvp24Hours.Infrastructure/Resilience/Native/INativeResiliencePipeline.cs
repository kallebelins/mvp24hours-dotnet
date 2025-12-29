//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Resilience.Native
{
    /// <summary>
    /// Generic interface for resilience pipelines using Microsoft.Extensions.Resilience (.NET 9+).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a unified abstraction for resilience operations using the native
    /// .NET 9 <c>ResiliencePipeline</c> from <c>Microsoft.Extensions.Resilience</c>.
    /// </para>
    /// <para>
    /// Key benefits over custom implementations:
    /// <list type="bullet">
    ///   <item>Native integration with .NET 9 ecosystem</item>
    ///   <item>Built on Polly v8 with simplified configuration</item>
    ///   <item>Automatic telemetry and OpenTelemetry integration</item>
    ///   <item>IOptions pattern support for configuration</item>
    ///   <item>Built-in rate limiting, hedging, and timeout strategies</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using the native resilience pipeline
    /// var pipeline = serviceProvider.GetRequiredService&lt;INativeResiliencePipeline&lt;string&gt;&gt;();
    /// 
    /// var result = await pipeline.ExecuteAsync(async ct =>
    /// {
    ///     return await database.GetDataAsync(ct);
    /// }, cancellationToken);
    /// </code>
    /// </example>
    public interface INativeResiliencePipeline<TResult>
    {
        /// <summary>
        /// Executes an operation with resilience policies applied.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        ValueTask<TResult> ExecuteAsync(
            Func<CancellationToken, ValueTask<TResult>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with resilience policies applied using Task-based async.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        Task<TResult> ExecuteTaskAsync(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name of this resilience pipeline.
        /// </summary>
        string Name { get; }
    }

    /// <summary>
    /// Generic interface for resilience pipelines that execute operations without return values.
    /// </summary>
    public interface INativeResiliencePipeline
    {
        /// <summary>
        /// Executes an operation with resilience policies applied.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A ValueTask representing the operation.</returns>
        ValueTask ExecuteAsync(
            Func<CancellationToken, ValueTask> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation with resilience policies applied using Task-based async.
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A Task representing the operation.</returns>
        Task ExecuteTaskAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name of this resilience pipeline.
        /// </summary>
        string Name { get; }
    }
}

