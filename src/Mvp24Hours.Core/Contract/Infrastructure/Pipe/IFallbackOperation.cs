//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Defines fallback behavior for an operation.
    /// Operations implementing this interface will execute a fallback when the main operation fails.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyFallbackOperation : OperationBaseAsync, IFallbackOperation
    /// {
    ///     public override async Task ExecuteAsync(IPipelineMessage input)
    ///     {
    ///         // Main operation logic - may fail
    ///         await CallExternalApi(input);
    ///     }
    /// 
    ///     public Task ExecuteFallbackAsync(IPipelineMessage input, Exception exception)
    ///     {
    ///         // Fallback logic - use cached data or default value
    ///         input.AddContent("result", GetCachedOrDefaultValue());
    ///         return Task.CompletedTask;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IFallbackOperation
    {
        /// <summary>
        /// Gets the types of exceptions that should trigger the fallback.
        /// Null or empty means all exceptions trigger the fallback.
        /// Default implementation returns null (all exceptions).
        /// </summary>
        Type[]? FallbackOnExceptions => null;

        /// <summary>
        /// Gets whether the fallback should be executed when the operation is faulty (has errors).
        /// Default implementation returns true.
        /// </summary>
        bool FallbackOnFaulty => true;

        /// <summary>
        /// Determines whether a specific exception should trigger the fallback.
        /// Override this for custom fallback logic.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <returns>True if the fallback should execute, false otherwise.</returns>
        bool ShouldFallback(Exception exception)
        {
            if (FallbackOnExceptions == null || FallbackOnExceptions.Length == 0)
                return true;

            foreach (var type in FallbackOnExceptions)
            {
                if (type.IsInstanceOfType(exception))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Executes the fallback logic when the main operation fails.
        /// </summary>
        /// <param name="input">The pipeline message.</param>
        /// <param name="exception">The exception that triggered the fallback (null if faulty without exception).</param>
        /// <returns>A task representing the asynchronous fallback operation.</returns>
        Task ExecuteFallbackAsync(IPipelineMessage input, Exception? exception);

        /// <summary>
        /// Called when the fallback starts executing.
        /// Override for custom logging or behavior.
        /// </summary>
        /// <param name="exception">The exception that triggered the fallback.</param>
        void OnFallbackStarting(Exception? exception) { }

        /// <summary>
        /// Called when the fallback completes successfully.
        /// Override for custom logging or behavior.
        /// </summary>
        void OnFallbackCompleted() { }

        /// <summary>
        /// Called when the fallback itself fails.
        /// Override for custom logging or behavior.
        /// </summary>
        /// <param name="fallbackException">The exception from the fallback.</param>
        void OnFallbackFailed(Exception fallbackException) { }
    }

    /// <summary>
    /// Synchronous version of fallback operation interface.
    /// </summary>
    public interface IFallbackOperationSync
    {
        /// <summary>
        /// Gets the types of exceptions that should trigger the fallback.
        /// Null or empty means all exceptions trigger the fallback.
        /// </summary>
        Type[]? FallbackOnExceptions => null;

        /// <summary>
        /// Gets whether the fallback should be executed when the operation is faulty.
        /// </summary>
        bool FallbackOnFaulty => true;

        /// <summary>
        /// Determines whether a specific exception should trigger the fallback.
        /// </summary>
        bool ShouldFallback(Exception exception)
        {
            if (FallbackOnExceptions == null || FallbackOnExceptions.Length == 0)
                return true;

            foreach (var type in FallbackOnExceptions)
            {
                if (type.IsInstanceOfType(exception))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Executes the fallback logic when the main operation fails.
        /// </summary>
        void ExecuteFallback(IPipelineMessage input, Exception? exception);
    }

    /// <summary>
    /// Configuration options for fallback behavior.
    /// </summary>
    public class FallbackOptions
    {
        /// <summary>
        /// Gets or sets the types of exceptions that should trigger the fallback.
        /// Null or empty means all exceptions trigger the fallback.
        /// Default: null (all exceptions).
        /// </summary>
        public Type[]? FallbackOnExceptions { get; set; }

        /// <summary>
        /// Gets or sets whether the fallback should be executed when the operation is faulty.
        /// Default: true.
        /// </summary>
        public bool FallbackOnFaulty { get; set; } = true;

        /// <summary>
        /// Gets or sets a custom predicate to determine if fallback should execute.
        /// When set, this takes precedence over FallbackOnExceptions.
        /// Default: null.
        /// </summary>
        public Func<Exception, bool>? ShouldFallbackPredicate { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when fallback starts.
        /// Default: null.
        /// </summary>
        public Action<Exception?>? OnFallbackStarting { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when fallback completes.
        /// Default: null.
        /// </summary>
        public Action? OnFallbackCompleted { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when fallback fails.
        /// Default: null.
        /// </summary>
        public Action<Exception>? OnFallbackFailed { get; set; }

        /// <summary>
        /// Gets or sets the fallback action for async operations.
        /// </summary>
        public Func<IPipelineMessage, Exception?, Task>? FallbackAction { get; set; }

        /// <summary>
        /// Gets or sets the fallback action for sync operations.
        /// </summary>
        public Action<IPipelineMessage, Exception?>? FallbackActionSync { get; set; }

        /// <summary>
        /// Creates default fallback options.
        /// </summary>
        public static FallbackOptions Default => new();

        /// <summary>
        /// Determines whether a specific exception should trigger the fallback.
        /// </summary>
        public bool ShouldFallback(Exception exception)
        {
            if (ShouldFallbackPredicate != null)
                return ShouldFallbackPredicate(exception);

            if (FallbackOnExceptions == null || FallbackOnExceptions.Length == 0)
                return true;

            foreach (var type in FallbackOnExceptions)
            {
                if (type.IsInstanceOfType(exception))
                    return true;
            }

            return false;
        }
    }
}

