//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Threading;

namespace Mvp24Hours.Infrastructure.Pipe.Context
{
    /// <summary>
    /// Provides access to the current pipeline context within pipeline operations.
    /// This follows the ambient context pattern similar to HttpContextAccessor in ASP.NET Core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Purpose:</strong>
    /// Allows pipeline operations and middlewares to access the current context
    /// without requiring explicit context passing through all method signatures.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong>
    /// The default implementation uses AsyncLocal to provide thread-safe context
    /// storage that flows across async operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyOperation : OperationBaseAsync
    /// {
    ///     private readonly IPipelineContextAccessor _contextAccessor;
    ///     
    ///     public MyOperation(IPipelineContextAccessor contextAccessor)
    ///     {
    ///         _contextAccessor = contextAccessor;
    ///     }
    ///     
    ///     public override async Task ExecuteAsync(IPipelineMessage input)
    ///     {
    ///         var context = _contextAccessor.Context;
    ///         if (context != null)
    ///         {
    ///             _logger.LogInformation("CorrelationId: {CorrelationId}", context.CorrelationId);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IPipelineContextAccessor
    {
        /// <summary>
        /// Gets or sets the current pipeline context.
        /// Returns null if no context has been set for the current execution flow.
        /// </summary>
        IPipelineContext? Context { get; set; }

        /// <summary>
        /// Gets whether a context is currently available.
        /// </summary>
        bool HasContext { get; }

        /// <summary>
        /// Sets the context and returns a disposable that restores the previous context when disposed.
        /// This is useful for nested pipeline executions where you want to temporarily replace the context.
        /// </summary>
        /// <param name="context">The context to set.</param>
        /// <returns>A disposable that restores the previous context when disposed.</returns>
        IDisposable BeginScope(IPipelineContext context);
    }

    /// <summary>
    /// Default implementation of <see cref="IPipelineContextAccessor"/> using AsyncLocal
    /// for thread-safe context storage that flows across async operations.
    /// </summary>
    public sealed class PipelineContextAccessor : IPipelineContextAccessor
    {
        private static readonly AsyncLocal<ContextHolder> _contextCurrent = new();

        /// <inheritdoc />
        public IPipelineContext? Context
        {
            get => _contextCurrent.Value?.Context;
            set
            {
                var holder = _contextCurrent.Value;
                if (holder != null)
                {
                    // Clear current context trapped in the AsyncLocal
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the context in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _contextCurrent.Value = new ContextHolder { Context = value };
                }
            }
        }

        /// <inheritdoc />
        public bool HasContext => _contextCurrent.Value?.Context != null;

        /// <inheritdoc />
        public IDisposable BeginScope(IPipelineContext context)
        {
            var previousContext = Context;
            Context = context;
            return new ContextScope(this, previousContext);
        }

        /// <summary>
        /// Holder class for the context to allow clearing across all ExecutionContexts.
        /// </summary>
        private sealed class ContextHolder
        {
            public IPipelineContext? Context;
        }

        /// <summary>
        /// Disposable scope that restores the previous context when disposed.
        /// </summary>
        private sealed class ContextScope : IDisposable
        {
            private readonly PipelineContextAccessor _accessor;
            private readonly IPipelineContext? _previousContext;
            private bool _disposed;

            public ContextScope(PipelineContextAccessor accessor, IPipelineContext? previousContext)
            {
                _accessor = accessor;
                _previousContext = previousContext;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _accessor.Context = _previousContext;
                    _disposed = true;
                }
            }
        }
    }
}

