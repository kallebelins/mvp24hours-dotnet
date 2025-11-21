//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Core.Events;
using System;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Defines pipeline engine async
    /// </summary>
    /// <example>
    /// <code>
    ///     IPipelineAsync pipeline = new PipelineAsync();
    ///     pipeline.AddAsync(new FileLogWriteOperation());
    ///     await pipeline.Execute(filter.ToMessage());
    ///     return pipeline.GetMessage().ToBusinessAsync{Product}();
    /// </code>
    /// </example>
    public interface IPipelineAsync
    {
        /// <summary>
        /// Get message package
        /// </summary>
        /// <returns></returns>
        IPipelineMessage GetMessage();
        /// <summary>
        /// Records operations
        /// </summary>
        IPipelineAsync Add<T>() where T : class, IOperationAsync;
        /// <summary>
        /// Records operations
        /// </summary>
        IPipelineAsync Add(IOperationAsync operation);
        /// <summary>
        /// Records operations
        /// </summary>
        IPipelineAsync Add(Action<IPipelineMessage> action, bool isRequired = false);
        /// <summary>
        /// Records operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors<T>(PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation) where T : class, IOperationAsync;
        /// <summary>
        /// Records operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors(IOperationAsync operation, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation);
        /// <summary>
        /// Records operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors(Action<IPipelineMessage> action, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation);
        /// <summary>
        /// Records operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors<T>(Func<IPipelineMessage, bool> condition, bool postOperation = true) where T : class, IOperationAsync;
        /// <summary>
        /// Records operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors(IOperationAsync operation, Func<IPipelineMessage, bool> condition, bool postOperation = true);
        /// <summary>
        /// Records operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors(Action<IPipelineMessage> action, Func<IPipelineMessage, bool> condition, bool postOperation = true);
        /// <summary>
        /// Records event operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors(Mvp24Hours.Core.Events.EventHandler<IPipelineMessage, EventArgs> handler, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation);
        /// <summary>
        /// Records event operations interceptors
        /// </summary>
        IPipelineAsync AddInterceptors(Mvp24Hours.Core.Events.EventHandler<IPipelineMessage, EventArgs> handler, Func<IPipelineMessage, bool> condition, bool postOperation = true);
        /// <summary>
        /// Performs async operations
        /// </summary>
        Task ExecuteAsync(IPipelineMessage input = null);
    }
}
