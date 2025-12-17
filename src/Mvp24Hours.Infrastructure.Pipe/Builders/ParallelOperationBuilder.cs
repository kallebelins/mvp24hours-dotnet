//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations.Parallel;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Pipe.Builders
{
    /// <summary>
    /// Builder for creating parallel operation groups for synchronous pipeline.
    /// </summary>
    public class ParallelOperationBuilder : IParallelOperationBuilder<IPipeline>
    {
        private readonly Pipeline _pipeline;
        private readonly IServiceProvider? _provider;
        private readonly List<IOperation> _operations = new();
        private int? _maxDegreeOfParallelism;
        private bool _requireAllSuccess = true;

        internal ParallelOperationBuilder(Pipeline pipeline, IServiceProvider? provider)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _provider = provider;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipeline> Add<T>() where T : class
        {
            if (!typeof(IOperation).IsAssignableFrom(typeof(T)))
                throw new ArgumentException($"Type {typeof(T).Name} must implement IOperation");

            IOperation? instance = _provider?.GetService<T>() as IOperation;
            if (instance == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    instance = Activator.CreateInstance<T>() as IOperation;
                }
            }

            if (instance == null)
                throw new InvalidOperationException($"Could not create instance of {typeof(T).Name}");

            _operations.Add(instance);
            return this;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipeline> Add(object operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (operation is not IOperation op)
                throw new ArgumentException($"Operation must implement IOperation", nameof(operation));

            _operations.Add(op);
            return this;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipeline> WithMaxDegreeOfParallelism(int maxDegree)
        {
            if (maxDegree < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegree), "Max degree must be at least 1");

            _maxDegreeOfParallelism = maxDegree;
            return this;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipeline> RequireAllSuccess(bool require = true)
        {
            _requireAllSuccess = require;
            return this;
        }

        /// <inheritdoc />
        public IPipeline EndParallel()
        {
            var parallelGroup = new ParallelOperationGroup(_operations, _maxDegreeOfParallelism, _requireAllSuccess);
            _pipeline.Add(parallelGroup);
            return _pipeline;
        }
    }

    /// <summary>
    /// Builder for creating parallel operation groups for async pipeline.
    /// </summary>
    public class ParallelOperationBuilderAsync : IParallelOperationBuilder<IPipelineAsync>
    {
        private readonly PipelineAsync _pipeline;
        private readonly IServiceProvider? _provider;
        private readonly List<IOperationAsync> _operations = new();
        private int? _maxDegreeOfParallelism;
        private bool _requireAllSuccess = true;

        internal ParallelOperationBuilderAsync(PipelineAsync pipeline, IServiceProvider? provider)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _provider = provider;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipelineAsync> Add<T>() where T : class
        {
            if (!typeof(IOperationAsync).IsAssignableFrom(typeof(T)))
                throw new ArgumentException($"Type {typeof(T).Name} must implement IOperationAsync");

            IOperationAsync? instance = _provider?.GetService<T>() as IOperationAsync;
            if (instance == null)
            {
                Type type = typeof(T);
                if (type.IsClass && !type.IsAbstract)
                {
                    instance = Activator.CreateInstance<T>() as IOperationAsync;
                }
            }

            if (instance == null)
                throw new InvalidOperationException($"Could not create instance of {typeof(T).Name}");

            _operations.Add(instance);
            return this;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipelineAsync> Add(object operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (operation is not IOperationAsync op)
                throw new ArgumentException($"Operation must implement IOperationAsync", nameof(operation));

            _operations.Add(op);
            return this;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipelineAsync> WithMaxDegreeOfParallelism(int maxDegree)
        {
            if (maxDegree < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegree), "Max degree must be at least 1");

            _maxDegreeOfParallelism = maxDegree;
            return this;
        }

        /// <inheritdoc />
        public IParallelOperationBuilder<IPipelineAsync> RequireAllSuccess(bool require = true)
        {
            _requireAllSuccess = require;
            return this;
        }

        /// <inheritdoc />
        public IPipelineAsync EndParallel()
        {
            var parallelGroup = new ParallelOperationGroupAsync(_operations, _maxDegreeOfParallelism, _requireAllSuccess);
            _pipeline.Add(parallelGroup);
            return _pipeline;
        }
    }
}

