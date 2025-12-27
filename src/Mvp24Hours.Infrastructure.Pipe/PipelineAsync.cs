//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Application.Pipe.Async;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Pipe.Configuration;
using Mvp24Hours.Infrastructure.Pipe.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Pipe
{
    /// <summary>
    /// <see cref="Mvp24Hours.Core.Contract.Infrastructure.Pipe.IPipelineAsync"/>
    /// </summary>
    public class PipelineAsync : PipelineBase, IPipelineAsync
    {
        #region [ Ctor ]

        public PipelineAsync(IServiceProvider _provider = null)
        {
            this.provider = _provider;
            this._logger = _provider?.GetService<ILogger<PipelineAsync>>();
            _logger?.LogDebug("PipelineAsync: Constructor");

            var options = _provider?.GetService<IOptions<PipelineOptions>>()?.Value;
            if (options != null)
            {
                this.IsBreakOnFail = options.IsBreakOnFail;
                this.ForceRollbackOnFalure = options.ForceRollbackOnFalure;
            }

            operations = [];
            executedOperations = [];

            preCustomInterceptors = [];
            postCustomInterceptors = [];
            dictionaryInterceptors = [];

            dictionaryEventInterceptors = [];
            preEventCustomInterceptors = [];
            postEventCustomInterceptors = [];
        }

        #endregion

        #region [ Fields / Properties ]
        private readonly IServiceProvider provider;
        private readonly ILogger<PipelineAsync> _logger;
        private readonly List<IOperationAsync> operations;
        private readonly List<IOperationAsync> executedOperations;

        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>> preCustomInterceptors;
        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>> postCustomInterceptors;
        private readonly Dictionary<PipelineInterceptorType, List<IOperationAsync>> dictionaryInterceptors;

        private readonly Dictionary<PipelineInterceptorType, List<EventHandler<IPipelineMessage, EventArgs>>> dictionaryEventInterceptors;
        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> preEventCustomInterceptors;
        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> postEventCustomInterceptors;
        #endregion

        #region [ Methods ]

        #region [ Get ]
        public List<IOperationAsync> GetOperations() => operations;
        public Dictionary<PipelineInterceptorType, List<IOperationAsync>> GetInterceptors() => dictionaryInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>> GetPreInterceptors() => preCustomInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>> GetPostInterceptors() => postCustomInterceptors;
        public Dictionary<PipelineInterceptorType, List<EventHandler<IPipelineMessage, EventArgs>>> GetEvents() => dictionaryEventInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> GetPreEvents() => preEventCustomInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> GetPostEvents() => postEventCustomInterceptors;
        #endregion

        public IPipelineAsync Add<T>() where T : class, IOperationAsync
        {
            IOperationAsync instance = provider?.GetService<T>();
            if (instance == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    return Add(Activator.CreateInstance<T>());
                }
                else
                {
                    throw new ArgumentNullException(string.Empty, "Operation not found. Check if it has been registered in this context.");
                }
            }
            return Add(instance);
        }
        public IPipelineAsync Add(IOperationAsync operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "Operation has not been defined or is null.");
            }
            this.operations.Add(operation);
            return this;
        }
        public IPipelineAsync Add(Action<IPipelineMessage> action, bool isRequired = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Action is mandatory.");
            }
            this.operations.Add(new OperationActionAsync(action, isRequired));
            return this;
        }

        public IPipelineAsync AddBuilder<T>() where T : class, IPipelineBuilderAsync
        {
            IPipelineBuilderAsync pipelineBuilder = provider?.GetService<T>();
            if (pipelineBuilder == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    return Activator.CreateInstance<T>().Builder(this);
                }
                else
                {
                    throw new ArgumentNullException(string.Empty, "PipelineBuilder not found. Check if it has been registered in this context.");
                }
            }
            return pipelineBuilder.Builder(this);
        }
        public IPipelineAsync AddBuilder(IPipelineBuilderAsync pipelineBuilder)
        {
            if (pipelineBuilder == null)
            {
                throw new ArgumentNullException(nameof(pipelineBuilder), "PipelineBuilder has not been defined or is null.");
            }
            pipelineBuilder.Builder(this);
            return this;
        }

        public IPipelineAsync AddInterceptors<T>(PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation) where T : class, IOperationAsync
        {
            IOperationAsync instance = provider?.GetService<T>();
            if (instance == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    return AddInterceptors(Activator.CreateInstance<T>(), pipelineInterceptor);
                }
                else
                {
                    throw new ArgumentNullException(string.Empty, "Operation not found. Check if it has been registered in this context.");
                }
            }
            return AddInterceptors(instance, pipelineInterceptor);
        }
        public IPipelineAsync AddInterceptors(IOperationAsync operation, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "Operation has not been defined or is null.");
            }
            if (!dictionaryInterceptors.TryGetValue(pipelineInterceptor, out List<IOperationAsync> value))
            {
                value = [];
                this.dictionaryInterceptors.Add(pipelineInterceptor, value);
            }

            value.Add(operation);
            return this;
        }
        public IPipelineAsync AddInterceptors(Action<IPipelineMessage> action, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Action is mandatory.");
            }
            if (!dictionaryInterceptors.TryGetValue(pipelineInterceptor, out List<IOperationAsync> value))
            {
                value = [];
                this.dictionaryInterceptors.Add(pipelineInterceptor, value);
            }

            value.Add(new OperationActionAsync(action));
            return this;
        }
        public IPipelineAsync AddInterceptors<T>(Func<IPipelineMessage, bool> condition, bool postOperation = true) where T : class, IOperationAsync
        {
            IOperationAsync instance = provider?.GetService<T>();
            if (instance == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    return AddInterceptors(Activator.CreateInstance<T>(), condition, postOperation);
                }
                else
                {
                    throw new ArgumentNullException(string.Empty, "Operation not found. Check if it has been registered in this context.");
                }
            }
            return AddInterceptors(instance, condition, postOperation);
        }
        public IPipelineAsync AddInterceptors(IOperationAsync operation, Func<IPipelineMessage, bool> condition, bool postOperation = true)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "Operation has not been defined or is null.");
            }
            if (postOperation)
            {
                this.postCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>(condition, operation));
            }
            else
            {
                this.preCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>(condition, operation));
            }
            return this;
        }
        public IPipelineAsync AddInterceptors(Action<IPipelineMessage> action, Func<IPipelineMessage, bool> condition, bool postOperation = true)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Action is mandatory.");
            }
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition), "Condition is mandatory.");
            }
            if (postOperation)
            {
                this.postCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>(condition, new OperationActionAsync(action)));
            }
            else
            {
                this.preCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperationAsync>(condition, new OperationActionAsync(action)));
            }
            return this;
        }
        public IPipelineAsync AddInterceptors(EventHandler<IPipelineMessage, EventArgs> handler, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "Handler has not been defined or is null.");
            }

            if (!dictionaryEventInterceptors.TryGetValue(pipelineInterceptor, out List<EventHandler<IPipelineMessage, EventArgs>> value))
            {
                value = [];
                this.dictionaryEventInterceptors.Add(pipelineInterceptor, value);
            }

            value.Add(handler);
            return this;
        }
        public IPipelineAsync AddInterceptors(EventHandler<IPipelineMessage, EventArgs> handler, Func<IPipelineMessage, bool> condition, bool postOperation = true)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "Handler is mandatory.");
            }
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition), "Condition is mandatory.");
            }
            if (postOperation)
            {
                this.postEventCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>(condition, handler));
            }
            else
            {
                this.preEventCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>(condition, handler));
            }
            return this;
        }

        public async Task ExecuteAsync(IPipelineMessage input = null)
        {
            executedOperations.Clear();
            _logger?.LogDebug("PipelineAsync: ExecuteAsync started");
            try
            {
                Message = input ?? Message;
                Message = await RunOperationsAsync(this.operations, Message);
            }
            finally { _logger?.LogDebug("PipelineAsync: ExecuteAsync completed"); }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
        internal virtual async Task<IPipelineMessage> RunOperationsAsync(List<IOperationAsync> _operations, IPipelineMessage input, bool onlyOperationDefault = false)
        {
            if (!_operations.AnySafe())
            {
                return input;
            }

            input ??= new PipelineMessage();

            var currentException = default(Exception);

            if (!onlyOperationDefault)
            {
                await RunEventInterceptorsAsync(input, PipelineInterceptorType.FirstOperation);
                await RunOperationInterceptorsAsync(input, PipelineInterceptorType.FirstOperation);
            }

            _ = await _operations.Aggregate(Task.FromResult(input), async (currentAsync, operation) =>
            {
                var current = await currentAsync;

                if (!onlyOperationDefault)
                {
                    if ((current.IsFaulty) && this.IsBreakOnFail)
                    {
                        return current;
                    }

                    if (current.IsLocked && !operation.IsRequired)
                    {
                        return current;
                    }
                }

                try
                {
                    // pre-operation
                    if (!onlyOperationDefault)
                    {
                        // events
                        await RunCustomEventInterceptorsAsync(input, false);
                        await RunEventInterceptorsAsync(input, PipelineInterceptorType.PreOperation);

                        // operations
                        await RunCustomOperationInterceptorsAsync(input, false);
                        await RunOperationInterceptorsAsync(input, PipelineInterceptorType.PreOperation);
                    }

                    // operation                    
                    _logger?.LogDebug("PipelineAsync: Executing operation {OperationName}", operation.GetType().Name);
                    try
                    {
                        await operation.ExecuteAsync(current);
                    }
                    finally { _logger?.LogDebug("PipelineAsync: Operation {OperationName} completed", operation.GetType().Name); }

                    // post-operation
                    if (!onlyOperationDefault)
                    {
                        // events
                        await RunEventInterceptorsAsync(input, PipelineInterceptorType.PostOperation);
                        await RunCustomEventInterceptorsAsync(input);

                        // operations
                        await RunOperationInterceptorsAsync(current, PipelineInterceptorType.PostOperation);
                        await RunCustomOperationInterceptorsAsync(current);

                        if (current.IsLocked)
                        {
                            await RunEventInterceptorsAsync(input, PipelineInterceptorType.Locked, true);
                            await RunOperationInterceptorsAsync(current, PipelineInterceptorType.Locked, true);
                        }

                        if (current.IsFaulty)
                        {
                            await RunEventInterceptorsAsync(input, PipelineInterceptorType.Faulty, true);
                            await RunOperationInterceptorsAsync(current, PipelineInterceptorType.Faulty, true);
                        }
                        else
                        {
                            executedOperations.Add(operation);
                        }
                    }

                    return current;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "PipelineAsync: Execute operation failure");
                    current.Messages.Add(new MessageResult((ex.InnerException ?? ex).Message, MessageType.Error));
                    input.AddContent(ex);
                    currentException = ex;
                }
                return current;
            });

            if (!onlyOperationDefault && (!input.IsFaulty))
            {
                await RunEventInterceptorsAsync(input, PipelineInterceptorType.LastOperation);
                await RunOperationInterceptorsAsync(input, PipelineInterceptorType.LastOperation);
            }

            if (!onlyOperationDefault && input.IsFaulty && ForceRollbackOnFalure)
            {
                await RunRollbackOperationsAsync(input);
            }

            if (!onlyOperationDefault && input.IsFaulty && AllowPropagateException && currentException != null)
            {
                throw currentException;
            }

            return input;
        }

        protected virtual async Task RunOperationInterceptorsAsync(IPipelineMessage input, PipelineInterceptorType interceptorType, bool canClearList = false)
        {
            if (dictionaryInterceptors.TryGetValue(interceptorType, out List<IOperationAsync> value))
            {
                await RunOperationsAsync(value, input, true);
                if (canClearList)
                {
                    dictionaryInterceptors.Remove(interceptorType);
                }
            }
        }
        protected virtual async Task RunCustomOperationInterceptorsAsync(IPipelineMessage input, bool postOperation = true)
        {
            if (postOperation)
            {
                if (postCustomInterceptors.AnySafe())
                {
                    foreach (var ci in postCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        await RunOperationsAsync([ci.Value], input, true);
                    }
                }
            }
            else
            {
                if (preCustomInterceptors.AnySafe())
                {
                    foreach (var ci in preCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        await RunOperationsAsync([ci.Value], input, true);
                    }
                }
            }
        }
        protected virtual async Task RunEventInterceptorsAsync(IPipelineMessage input, PipelineInterceptorType interceptorType, bool canClearList = false)
        {
            if (dictionaryEventInterceptors.TryGetValue(interceptorType, out List<EventHandler<IPipelineMessage, EventArgs>> value))
            {
                await RunEventsAsync(value, input);
                if (canClearList)
                {
                    dictionaryEventInterceptors.Remove(interceptorType);
                }
            }
        }
        protected virtual async Task RunCustomEventInterceptorsAsync(IPipelineMessage input, bool postOperation = true)
        {
            if (postOperation)
            {
                if (postEventCustomInterceptors.AnySafe())
                {
                    foreach (var ci in postEventCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        await RunEventsAsync([ci.Value], input);
                    }
                }
            }
            else
            {
                if (preEventCustomInterceptors.AnySafe())
                {
                    foreach (var ci in preEventCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        await RunEventsAsync([ci.Value], input);
                    }
                }
            }
        }
        protected virtual async Task RunEventsAsync(List<EventHandler<IPipelineMessage, EventArgs>> _handlers, IPipelineMessage input)
        {
            if (_handlers.AnySafe())
            {
                foreach (var handler in _handlers)
                {
                    if (handler == null)
                    {
                        continue;
                    }

                    _logger?.LogDebug("PipelineAsync: Executing event handler {HandlerName}", handler.GetType().Name);
                    try
                    {
                        await Task.Factory.StartNew(() => handler(input, EventArgs.Empty));
                    }
                    finally { _logger?.LogDebug("PipelineAsync: Event handler {HandlerName} completed", handler.GetType().Name); }
                }
            }
        }
        private async Task RunRollbackOperationsAsync(IPipelineMessage input)
        {
            if (executedOperations.AnySafe())
            {
                foreach (var executedOperation in executedOperations.Reverse<IOperationAsync>())
                {
                    if (executedOperation == null)
                    {
                        continue;
                    }

                    _logger?.LogDebug("PipelineAsync: Rolling back operation {OperationName}", executedOperation.GetType().Name);
                    try
                    {
                        await executedOperation.RollbackAsync(input);
                    }
                    catch (Exception ex) { _logger?.LogError(ex, "PipelineAsync: Rollback operation failure"); }
                    finally { _logger?.LogDebug("PipelineAsync: Rollback operation {OperationName} completed", executedOperation.GetType().Name); }
                }
            }
        }

        #endregion
    }
}
