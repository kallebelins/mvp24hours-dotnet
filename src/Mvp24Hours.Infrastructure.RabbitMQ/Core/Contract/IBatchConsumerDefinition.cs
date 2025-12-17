//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using System;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract
{
    /// <summary>
    /// Base interface for batch consumer definitions.
    /// </summary>
    public interface IBatchConsumerDefinition : IConsumerDefinition
    {
        /// <summary>
        /// Gets whether this is a batch consumer.
        /// </summary>
        bool IsBatchConsumer { get; }

        /// <summary>
        /// Gets the batch consumer options.
        /// </summary>
        BatchConsumerOptions? BatchOptions { get; }
    }

    /// <summary>
    /// Generic batch consumer definition for typed batch consumers.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    public interface IBatchConsumerDefinition<TConsumer> : IBatchConsumerDefinition
        where TConsumer : class
    {
    }
}

