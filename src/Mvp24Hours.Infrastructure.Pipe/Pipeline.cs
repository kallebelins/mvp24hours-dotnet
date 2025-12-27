//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Application.Pipe;
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
    /// <see cref="Mvp24Hours.Core.Contract.Infrastructure.Pipe.IPipeline"/>
    /// </summary>
    public class Pipeline : PipelineBase, IPipeline
    {
        #region [ Ctor ]

        public Pipeline(IServiceProvider _provider = null)
        {
            this.provider = _provider;
            this._logger = _provider?.GetService<ILogger<Pipeline>>();
            _logger?.LogDebug("Pipeline: Constructor");

            var options = _provider?.GetService<IOptions<PipelineOptions>>()?.Value;
            if (options != null)
            {
                this.IsBreakOnFail = options.IsBreakOnFail;
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
        private readonly ILogger<Pipeline> _logger;
        private readonly List<IOperation> operations;
        private readonly List<IOperation> executedOperations;

        private readonly Dictionary<PipelineInterceptorType, List<IOperation>> dictionaryInterceptors;
        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, IOperation>> preCustomInterceptors;
        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, IOperation>> postCustomInterceptors;

        private readonly Dictionary<PipelineInterceptorType, List<EventHandler<IPipelineMessage, EventArgs>>> dictionaryEventInterceptors;
        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> preEventCustomInterceptors;
        private readonly List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> postEventCustomInterceptors;
        #endregion

        #region [ Methods ]

        #region [ Get ]
        public List<IOperation> GetOperations() => operations;
        public Dictionary<PipelineInterceptorType, List<IOperation>> GetInterceptors() => dictionaryInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, IOperation>> GetPreInterceptors() => preCustomInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, IOperation>> GetPostInterceptors() => postCustomInterceptors;
        public Dictionary<PipelineInterceptorType, List<EventHandler<IPipelineMessage, EventArgs>>> GetEvents() => dictionaryEventInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> GetPreEvents() => preEventCustomInterceptors;
        public List<KeyValuePair<Func<IPipelineMessage, bool>, EventHandler<IPipelineMessage, EventArgs>>> GetPostEvents() => postEventCustomInterceptors;
        #endregion

        public IPipeline Add<T>() where T : class, IOperation
        {
            IOperation instance = provider?.GetService<T>();
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
        public IPipeline Add(IOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "Operation has not been defined or is null.");
            }
            this.operations.Add(operation);
            return this;
        }
        public IPipeline Add(Action<IPipelineMessage> action, bool isRequired = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Action is mandatory.");
            }
            this.operations.Add(new OperationAction(action, isRequired));
            return this;
        }

        public IPipeline AddBuilder<T>() where T : class, IPipelineBuilder
        {
            IPipelineBuilder pipelineBuilder = provider?.GetService<T>();
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
            var result = pipelineBuilder.Builder(this);
            return result;
        }
        public IPipeline AddBuilder(IPipelineBuilder pipelineBuilder)
        {
            if (pipelineBuilder == null)
            {
                throw new ArgumentNullException(nameof(pipelineBuilder), "PipelineBuilder has not been defined or is null.");
            }
            pipelineBuilder.Builder(this);
            return this;
        }

        public IPipeline AddInterceptors<T>(PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation) where T : class, IOperation
        {
            IOperation instance = provider?.GetService<T>();
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
        public IPipeline AddInterceptors(IOperation operation, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "Operation has not been defined or is null.");
            }
            if (!dictionaryInterceptors.TryGetValue(pipelineInterceptor, out List<IOperation> value))
            {
                value = [];
                this.dictionaryInterceptors.Add(pipelineInterceptor, value);
            }

            value.Add(operation);
            return this;
        }
        public IPipeline AddInterceptors(Action<IPipelineMessage> action, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Action is mandatory.");
            }
            if (!dictionaryInterceptors.TryGetValue(pipelineInterceptor, out List<IOperation> value))
            {
                value = [];
                this.dictionaryInterceptors.Add(pipelineInterceptor, value);
            }

            value.Add(new OperationAction(action));
            return this;
        }
        public IPipeline AddInterceptors<T>(Func<IPipelineMessage, bool> condition, bool postOperation = true) where T : class, IOperation
        {
            IOperation instance = provider?.GetService<T>();
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
        public IPipeline AddInterceptors(IOperation operation, Func<IPipelineMessage, bool> condition, bool postOperation = true)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "Operation has not been defined or is null.");
            }
            if (postOperation)
            {
                this.postCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperation>(condition, operation));
            }
            else
            {
                this.preCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperation>(condition, operation));
            }
            return this;
        }
        public IPipeline AddInterceptors(Action<IPipelineMessage> action, Func<IPipelineMessage, bool> condition, bool postOperation = true)
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
                this.postCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperation>(condition, new OperationAction(action)));
            }
            else
            {
                this.preCustomInterceptors.Add(new KeyValuePair<Func<IPipelineMessage, bool>, IOperation>(condition, new OperationAction(action)));
            }
            return this;
        }
        public IPipeline AddInterceptors(EventHandler<IPipelineMessage, EventArgs> handler, PipelineInterceptorType pipelineInterceptor = PipelineInterceptorType.PostOperation)
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
        public IPipeline AddInterceptors(EventHandler<IPipelineMessage, EventArgs> handler, Func<IPipelineMessage, bool> condition, bool postOperation = true)
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

        public void Execute(IPipelineMessage input = null)
        {
            executedOperations.Clear();
            _logger?.LogDebug("Pipeline: Execute started");
            try
            {
                Message = input ?? Message;
                Message = RunOperations(this.operations, Message);
            }
            finally { _logger?.LogDebug("Pipeline: Execute completed"); }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
        protected virtual IPipelineMessage RunOperations(List<IOperation> _operations, IPipelineMessage input, bool onlyOperationDefault = false)
        {
            if (!_operations.AnySafe())
            {
                return input;
            }

            input ??= new PipelineMessage();

            var currentException = default(Exception);

            if (!onlyOperationDefault)
            {
                RunEventInterceptors(input, PipelineInterceptorType.FirstOperation);
                RunOperationInterceptors(input, PipelineInterceptorType.FirstOperation);
            }

            _ = _operations.Aggregate(input, (current, operation) =>
              {
                  if ((current.IsFaulty) && this.IsBreakOnFail)
                  {
                      return current;
                  }

                  if (current.IsLocked && !operation.IsRequired)
                  {
                      return current;
                  }

                  try
                  {
                      // pre-operation
                      if (!onlyOperationDefault)
                      {
                          // events
                          RunCustomEventInterceptors(input, false);
                          RunEventInterceptors(input, PipelineInterceptorType.PreOperation);

                          // operations
                          RunCustomOperationInterceptors(input, false);
                          RunOperationInterceptors(input, PipelineInterceptorType.PreOperation);
                      }

                      // operation
                      _logger?.LogDebug("Pipeline: Executing operation {OperationName}", operation.GetType().Name);
                      try
                      {
                          operation.Execute(current);
                      }
                      finally { _logger?.LogDebug("Pipeline: Operation {OperationName} completed", operation.GetType().Name); }

                      // post-operation
                      if (!onlyOperationDefault)
                      {
                          // events
                          RunEventInterceptors(input, PipelineInterceptorType.PostOperation);
                          RunCustomEventInterceptors(input);

                          // operations
                          RunOperationInterceptors(current, PipelineInterceptorType.PostOperation);
                          RunCustomOperationInterceptors(current);

                          if (current.IsLocked)
                          {
                              RunEventInterceptors(input, PipelineInterceptorType.Locked, true);
                              RunOperationInterceptors(current, PipelineInterceptorType.Locked, true);
                          }

                          if (current.IsFaulty)
                          {
                              RunEventInterceptors(input, PipelineInterceptorType.Faulty, true);
                              RunOperationInterceptors(current, PipelineInterceptorType.Faulty, true);
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
                      _logger?.LogError(ex, "Pipeline: Execute operation failure");
                      current.Messages.Add(new MessageResult((ex.InnerException ?? ex).Message, MessageType.Error));
                      input.AddContent(ex);
                      currentException = ex;
                  }
                  return current;
              });

            if (!onlyOperationDefault && (!input.IsFaulty))
            {
                RunEventInterceptors(input, PipelineInterceptorType.LastOperation);
                RunOperationInterceptors(input, PipelineInterceptorType.LastOperation);
            }

            if (!onlyOperationDefault && input.IsFaulty && ForceRollbackOnFalure)
            {
                RunRollbackOperations(input);
            }

            if (!onlyOperationDefault && input.IsFaulty && AllowPropagateException && currentException != null)
            {
                throw currentException;
            }

            return input;
        }

        protected virtual void RunOperationInterceptors(IPipelineMessage input, PipelineInterceptorType interceptorType, bool canClearList = false)
        {
            if (dictionaryInterceptors.TryGetValue(interceptorType, out List<IOperation> value))
            {
                RunOperations(value, input, true);
                if (canClearList)
                {
                    dictionaryInterceptors.Remove(interceptorType);
                }
            }
        }
        protected virtual void RunCustomOperationInterceptors(IPipelineMessage input, bool postOperation = true)
        {
            if (postOperation)
            {
                if (postCustomInterceptors.AnySafe())
                {
                    foreach (var ci in postCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        RunOperations([ci.Value], input, true);
                    }
                }
            }
            else
            {
                if (preCustomInterceptors.AnySafe())
                {
                    foreach (var ci in preCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        RunOperations([ci.Value], input, true);
                    }
                }
            }
        }
        protected virtual void RunEventInterceptors(IPipelineMessage input, PipelineInterceptorType interceptorType, bool canClearList = false)
        {
            if (dictionaryEventInterceptors.TryGetValue(interceptorType, out List<EventHandler<IPipelineMessage, EventArgs>> value))
            {
                RunEvents(value, input);
                if (canClearList)
                {
                    dictionaryEventInterceptors.Remove(interceptorType);
                }
            }
        }
        protected virtual void RunCustomEventInterceptors(IPipelineMessage input, bool postOperation = true)
        {
            if (postOperation)
            {
                if (postEventCustomInterceptors.AnySafe())
                {
                    foreach (var ci in postEventCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        RunEvents([ci.Value], input);
                    }
                }
            }
            else
            {
                if (preEventCustomInterceptors.AnySafe())
                {
                    foreach (var ci in preEventCustomInterceptors.Where(ci => ci.Key.Invoke(input)))
                    {
                        RunEvents([ci.Value], input);
                    }
                }
            }
        }
        protected virtual void RunEvents(List<EventHandler<IPipelineMessage, EventArgs>> _handlers, IPipelineMessage input)
        {
            if (_handlers.AnySafe())
            {
                foreach (var handler in _handlers)
                {
                    if (handler == null)
                    {
                        continue;
                    }
                    _logger?.LogDebug("Pipeline: Executing event handler {HandlerName}", handler.GetType().Name);
                    try
                    {
                        Task.Factory.StartNew(() => handler(input, EventArgs.Empty));
                    }
                    finally { _logger?.LogDebug("Pipeline: Event handler {HandlerName} completed", handler.GetType().Name); }
                }
            }
        }
        private void RunRollbackOperations(IPipelineMessage input)
        {
            if (executedOperations.AnySafe())
            {
                foreach (var executedOperation in executedOperations.Reverse<IOperation>())
                {
                    if (executedOperation == null)
                    {
                        continue;
                    }

                    _logger?.LogDebug("Pipeline: Rolling back operation {OperationName}", executedOperation.GetType().Name);
                    try
                    {
                        executedOperation.Rollback(input);
                    }
                    catch (Exception ex) { _logger?.LogError(ex, "Pipeline: Rollback operation failure"); }
                    finally { _logger?.LogDebug("Pipeline: Rollback operation {OperationName} completed", executedOperation.GetType().Name); }
                }
            }
        }

        #endregion
    }
}
