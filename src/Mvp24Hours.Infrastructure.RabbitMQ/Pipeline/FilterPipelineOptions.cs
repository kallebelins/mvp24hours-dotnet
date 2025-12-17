//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline
{
    /// <summary>
    /// Configuration options for the RabbitMQ filter pipeline.
    /// </summary>
    public class FilterPipelineOptions
    {
        private readonly List<Type> _consumeFilters = new();
        private readonly List<Type> _publishFilters = new();
        private readonly List<Type> _sendFilters = new();

        /// <summary>
        /// Gets the registered consume filter types.
        /// </summary>
        public IReadOnlyList<Type> ConsumeFilters => _consumeFilters.AsReadOnly();

        /// <summary>
        /// Gets the registered publish filter types.
        /// </summary>
        public IReadOnlyList<Type> PublishFilters => _publishFilters.AsReadOnly();

        /// <summary>
        /// Gets the registered send filter types.
        /// </summary>
        public IReadOnlyList<Type> SendFilters => _sendFilters.AsReadOnly();

        /// <summary>
        /// Gets or sets whether to enable the logging filter by default. Default is false.
        /// </summary>
        public bool EnableLoggingFilter { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable the exception handling filter by default. Default is false.
        /// </summary>
        public bool EnableExceptionHandlingFilter { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable the correlation filter by default. Default is false.
        /// </summary>
        public bool EnableCorrelationFilter { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable the telemetry filter by default. Default is false.
        /// </summary>
        public bool EnableTelemetryFilter { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable the validation filter by default. Default is false.
        /// </summary>
        public bool EnableValidationFilter { get; set; } = false;

        /// <summary>
        /// Adds a consume filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UseConsumeFilter<TFilter>() where TFilter : class, IConsumeFilter
        {
            if (!_consumeFilters.Contains(typeof(TFilter)))
            {
                _consumeFilters.Add(typeof(TFilter));
            }
            return this;
        }

        /// <summary>
        /// Adds a typed consume filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UseConsumeFilter<TFilter, TMessage>() 
            where TFilter : class, IConsumeFilter<TMessage>
            where TMessage : class
        {
            if (!_consumeFilters.Contains(typeof(TFilter)))
            {
                _consumeFilters.Add(typeof(TFilter));
            }
            return this;
        }

        /// <summary>
        /// Adds a consume filter type to the pipeline.
        /// </summary>
        /// <param name="filterType">The filter type.</param>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UseConsumeFilter(Type filterType)
        {
            if (filterType == null) throw new ArgumentNullException(nameof(filterType));
            
            if (!typeof(IConsumeFilter).IsAssignableFrom(filterType) && 
                !IsGenericConsumeFilter(filterType))
            {
                throw new ArgumentException($"Type {filterType.Name} must implement IConsumeFilter or IConsumeFilter<TMessage>", nameof(filterType));
            }
            
            if (!_consumeFilters.Contains(filterType))
            {
                _consumeFilters.Add(filterType);
            }
            return this;
        }

        /// <summary>
        /// Adds a publish filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UsePublishFilter<TFilter>() where TFilter : class, IPublishFilter
        {
            if (!_publishFilters.Contains(typeof(TFilter)))
            {
                _publishFilters.Add(typeof(TFilter));
            }
            return this;
        }

        /// <summary>
        /// Adds a typed publish filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UsePublishFilter<TFilter, TMessage>() 
            where TFilter : class, IPublishFilter<TMessage>
            where TMessage : class
        {
            if (!_publishFilters.Contains(typeof(TFilter)))
            {
                _publishFilters.Add(typeof(TFilter));
            }
            return this;
        }

        /// <summary>
        /// Adds a publish filter type to the pipeline.
        /// </summary>
        /// <param name="filterType">The filter type.</param>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UsePublishFilter(Type filterType)
        {
            if (filterType == null) throw new ArgumentNullException(nameof(filterType));
            
            if (!typeof(IPublishFilter).IsAssignableFrom(filterType) && 
                !IsGenericPublishFilter(filterType))
            {
                throw new ArgumentException($"Type {filterType.Name} must implement IPublishFilter or IPublishFilter<TMessage>", nameof(filterType));
            }
            
            if (!_publishFilters.Contains(filterType))
            {
                _publishFilters.Add(filterType);
            }
            return this;
        }

        /// <summary>
        /// Adds a send filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UseSendFilter<TFilter>() where TFilter : class, ISendFilter
        {
            if (!_sendFilters.Contains(typeof(TFilter)))
            {
                _sendFilters.Add(typeof(TFilter));
            }
            return this;
        }

        /// <summary>
        /// Adds a typed send filter to the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UseSendFilter<TFilter, TMessage>() 
            where TFilter : class, ISendFilter<TMessage>
            where TMessage : class
        {
            if (!_sendFilters.Contains(typeof(TFilter)))
            {
                _sendFilters.Add(typeof(TFilter));
            }
            return this;
        }

        /// <summary>
        /// Adds a send filter type to the pipeline.
        /// </summary>
        /// <param name="filterType">The filter type.</param>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UseSendFilter(Type filterType)
        {
            if (filterType == null) throw new ArgumentNullException(nameof(filterType));
            
            if (!typeof(ISendFilter).IsAssignableFrom(filterType) && 
                !IsGenericSendFilter(filterType))
            {
                throw new ArgumentException($"Type {filterType.Name} must implement ISendFilter or ISendFilter<TMessage>", nameof(filterType));
            }
            
            if (!_sendFilters.Contains(filterType))
            {
                _sendFilters.Add(filterType);
            }
            return this;
        }

        /// <summary>
        /// Adds a filter to all pipelines (consume, publish, and send).
        /// The filter must implement one or more of the filter interfaces.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions UseFilter<TFilter>() where TFilter : class
        {
            var filterType = typeof(TFilter);
            
            if (typeof(IConsumeFilter).IsAssignableFrom(filterType) || IsGenericConsumeFilter(filterType))
            {
                if (!_consumeFilters.Contains(filterType))
                {
                    _consumeFilters.Add(filterType);
                }
            }
            
            if (typeof(IPublishFilter).IsAssignableFrom(filterType) || IsGenericPublishFilter(filterType))
            {
                if (!_publishFilters.Contains(filterType))
                {
                    _publishFilters.Add(filterType);
                }
            }
            
            if (typeof(ISendFilter).IsAssignableFrom(filterType) || IsGenericSendFilter(filterType))
            {
                if (!_sendFilters.Contains(filterType))
                {
                    _sendFilters.Add(filterType);
                }
            }
            
            return this;
        }

        /// <summary>
        /// Removes a consume filter from the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions RemoveConsumeFilter<TFilter>() where TFilter : class
        {
            _consumeFilters.Remove(typeof(TFilter));
            return this;
        }

        /// <summary>
        /// Removes a publish filter from the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions RemovePublishFilter<TFilter>() where TFilter : class
        {
            _publishFilters.Remove(typeof(TFilter));
            return this;
        }

        /// <summary>
        /// Removes a send filter from the pipeline.
        /// </summary>
        /// <typeparam name="TFilter">The filter type.</typeparam>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions RemoveSendFilter<TFilter>() where TFilter : class
        {
            _sendFilters.Remove(typeof(TFilter));
            return this;
        }

        /// <summary>
        /// Clears all registered filters.
        /// </summary>
        /// <returns>The options for chaining.</returns>
        public FilterPipelineOptions ClearFilters()
        {
            _consumeFilters.Clear();
            _publishFilters.Clear();
            _sendFilters.Clear();
            return this;
        }

        private static bool IsGenericConsumeFilter(Type type)
        {
            return type.GetInterfaces().Any(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumeFilter<>));
        }

        private static bool IsGenericPublishFilter(Type type)
        {
            return type.GetInterfaces().Any(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPublishFilter<>));
        }

        private static bool IsGenericSendFilter(Type type)
        {
            return type.GetInterfaces().Any(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISendFilter<>));
        }
    }
}

