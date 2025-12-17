//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations.Composition;
using System;

namespace Mvp24Hours.Infrastructure.Pipe.Builders
{
    /// <summary>
    /// Builder for creating sub-pipeline scopes for synchronous pipeline.
    /// </summary>
    public class SubPipelineBuilder : ISubPipelineBuilder
    {
        private readonly Pipeline _parentPipeline;
        private readonly IServiceProvider? _provider;
        private readonly SubPipelineOperation _subPipeline;

        internal SubPipelineBuilder(Pipeline parentPipeline, IServiceProvider? provider, string? name)
        {
            _parentPipeline = parentPipeline ?? throw new ArgumentNullException(nameof(parentPipeline));
            _provider = provider;
            _subPipeline = new SubPipelineOperation(name);
        }

        /// <inheritdoc />
        public string? Name => _subPipeline.Name;

        /// <inheritdoc />
        public ISubPipelineBuilder Add<T>() where T : class, IOperation
        {
            IOperation? instance = _provider?.GetService<T>();
            if (instance == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    instance = Activator.CreateInstance<T>();
                }
                else
                {
                    throw new ArgumentNullException(string.Empty, "Operation not found. Check if it has been registered in this context.");
                }
            }

            _subPipeline.Add(instance);
            return this;
        }

        /// <inheritdoc />
        public ISubPipelineBuilder Add(IOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _subPipeline.Add(operation);
            return this;
        }

        /// <inheritdoc />
        public IPipeline EndScope()
        {
            _parentPipeline.Add(_subPipeline);
            return _parentPipeline;
        }
    }

    /// <summary>
    /// Builder for creating sub-pipeline scopes for async pipeline.
    /// </summary>
    public class SubPipelineBuilderAsync : ISubPipelineBuilderAsync
    {
        private readonly PipelineAsync _parentPipeline;
        private readonly IServiceProvider? _provider;
        private readonly SubPipelineOperationAsync _subPipeline;

        internal SubPipelineBuilderAsync(PipelineAsync parentPipeline, IServiceProvider? provider, string? name)
        {
            _parentPipeline = parentPipeline ?? throw new ArgumentNullException(nameof(parentPipeline));
            _provider = provider;
            _subPipeline = new SubPipelineOperationAsync(name);
        }

        /// <inheritdoc />
        public string? Name => _subPipeline.Name;

        /// <inheritdoc />
        public ISubPipelineBuilderAsync Add<T>() where T : class, IOperationAsync
        {
            IOperationAsync? instance = _provider?.GetService<T>();
            if (instance == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    instance = Activator.CreateInstance<T>();
                }
                else
                {
                    throw new ArgumentNullException(string.Empty, "Operation not found. Check if it has been registered in this context.");
                }
            }

            _subPipeline.Add(instance);
            return this;
        }

        /// <inheritdoc />
        public ISubPipelineBuilderAsync Add(IOperationAsync operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _subPipeline.Add(operation);
            return this;
        }

        /// <inheritdoc />
        public IPipelineAsync EndScope()
        {
            _parentPipeline.Add(_subPipeline);
            return _parentPipeline;
        }
    }
}

