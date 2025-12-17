//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations.Branch;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Pipe.Builders
{
    /// <summary>
    /// Builder for creating conditional branches for synchronous pipeline.
    /// </summary>
    public class ConditionalBranchBuilder : IConditionalBranchBuilder<IPipeline>
    {
        private readonly Pipeline _pipeline;
        private readonly IServiceProvider? _provider;
        private readonly ConditionalBranchOperation _branchOperation = new();

        internal ConditionalBranchBuilder(Pipeline pipeline, IServiceProvider? provider)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _provider = provider;
        }

        /// <inheritdoc />
        public IConditionalBranchBuilder<IPipeline> Case(string key, Func<IPipelineMessage, bool> condition, Action<IPipeline> configure)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // Create a temporary pipeline to collect operations
            var tempPipeline = new Pipeline(_provider);
            configure(tempPipeline);

            var operations = tempPipeline.GetOperations();
            if (operations.Count > 0)
            {
                _branchOperation.AddCase(key, condition, operations.ToArray());
            }

            return this;
        }

        /// <inheritdoc />
        public IConditionalBranchBuilder<IPipeline> Default(Action<IPipeline> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // Create a temporary pipeline to collect operations
            var tempPipeline = new Pipeline(_provider);
            configure(tempPipeline);

            var operations = tempPipeline.GetOperations();
            if (operations.Count > 0)
            {
                _branchOperation.SetDefault(operations.ToArray());
            }

            return this;
        }

        /// <inheritdoc />
        public IPipeline EndSwitch()
        {
            _pipeline.Add(_branchOperation);
            return _pipeline;
        }
    }

    /// <summary>
    /// Builder for creating conditional branches for async pipeline.
    /// </summary>
    public class ConditionalBranchBuilderAsync : IConditionalBranchBuilder<IPipelineAsync>
    {
        private readonly PipelineAsync _pipeline;
        private readonly IServiceProvider? _provider;
        private readonly ConditionalBranchOperationAsync _branchOperation = new();

        internal ConditionalBranchBuilderAsync(PipelineAsync pipeline, IServiceProvider? provider)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _provider = provider;
        }

        /// <inheritdoc />
        public IConditionalBranchBuilder<IPipelineAsync> Case(string key, Func<IPipelineMessage, bool> condition, Action<IPipelineAsync> configure)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // Create a temporary pipeline to collect operations
            var tempPipeline = new PipelineAsync(_provider);
            configure(tempPipeline);

            var operations = tempPipeline.GetOperations();
            if (operations.Count > 0)
            {
                _branchOperation.AddCase(key, condition, operations.ToArray());
            }

            return this;
        }

        /// <inheritdoc />
        public IConditionalBranchBuilder<IPipelineAsync> Default(Action<IPipelineAsync> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // Create a temporary pipeline to collect operations
            var tempPipeline = new PipelineAsync(_provider);
            configure(tempPipeline);

            var operations = tempPipeline.GetOperations();
            if (operations.Count > 0)
            {
                _branchOperation.SetDefault(operations.ToArray());
            }

            return this;
        }

        /// <inheritdoc />
        public IPipelineAsync EndSwitch()
        {
            _pipeline.Add(_branchOperation);
            return _pipeline;
        }
    }
}

