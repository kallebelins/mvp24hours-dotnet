//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline
{
    /// <summary>
    /// Executes filter pipelines for consume, publish, and send operations.
    /// </summary>
    public class FilterPipelineExecutor : IFilterPipelineExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly FilterPipelineOptions _options;

        /// <summary>
        /// Creates a new filter pipeline executor.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving filters.</param>
        /// <param name="options">The filter pipeline options.</param>
        public FilterPipelineExecutor(IServiceProvider serviceProvider, FilterPipelineOptions options)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public async Task ExecuteConsumeFiltersAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            Func<IConsumeFilterContext<TMessage>, CancellationToken, Task> finalAction,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var filters = GetConsumeFilters<TMessage>();
            
            if (!filters.Any())
            {
                await finalAction(context, cancellationToken);
                return;
            }

            // Build the pipeline in reverse order
            ConsumeFilterDelegate<TMessage> pipeline = async (ctx, ct) =>
            {
                if (!ctx.ShouldSkipRemainingFilters)
                {
                    await finalAction(ctx, ct);
                }
            };

            for (int i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                var next = pipeline;
                
                pipeline = async (ctx, ct) =>
                {
                    if (ctx.ShouldSkipRemainingFilters)
                        return;
                        
                    await filter.ConsumeAsync(ctx, next, ct);
                };
            }

            await pipeline(context, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ExecutePublishFiltersAsync<TMessage>(
            IPublishFilterContext<TMessage> context,
            Func<IPublishFilterContext<TMessage>, CancellationToken, Task> finalAction,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var filters = GetPublishFilters<TMessage>();
            
            if (!filters.Any())
            {
                await finalAction(context, cancellationToken);
                return;
            }

            // Build the pipeline in reverse order
            PublishFilterDelegate<TMessage> pipeline = async (ctx, ct) =>
            {
                if (!ctx.ShouldSkipRemainingFilters && !ctx.ShouldCancelPublish)
                {
                    await finalAction(ctx, ct);
                }
            };

            for (int i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                var next = pipeline;
                
                pipeline = async (ctx, ct) =>
                {
                    if (ctx.ShouldSkipRemainingFilters || ctx.ShouldCancelPublish)
                        return;
                        
                    await filter.PublishAsync(ctx, next, ct);
                };
            }

            await pipeline(context, cancellationToken);
        }

        /// <inheritdoc />
        public async Task ExecuteSendFiltersAsync<TMessage>(
            ISendFilterContext<TMessage> context,
            Func<ISendFilterContext<TMessage>, CancellationToken, Task> finalAction,
            CancellationToken cancellationToken = default) where TMessage : class
        {
            var filters = GetSendFilters<TMessage>();
            
            if (!filters.Any())
            {
                await finalAction(context, cancellationToken);
                return;
            }

            // Build the pipeline in reverse order
            SendFilterDelegate<TMessage> pipeline = async (ctx, ct) =>
            {
                if (!ctx.ShouldSkipRemainingFilters && !ctx.ShouldCancelSend)
                {
                    await finalAction(ctx, ct);
                }
            };

            for (int i = filters.Count - 1; i >= 0; i--)
            {
                var filter = filters[i];
                var next = pipeline;
                
                pipeline = async (ctx, ct) =>
                {
                    if (ctx.ShouldSkipRemainingFilters || ctx.ShouldCancelSend)
                        return;
                        
                    await filter.SendAsync(ctx, next, ct);
                };
            }

            await pipeline(context, cancellationToken);
        }

        private List<IConsumeFilter<TMessage>> GetConsumeFilters<TMessage>() where TMessage : class
        {
            var filters = new List<IConsumeFilter<TMessage>>();

            // Get global consume filters
            var globalFilters = _serviceProvider.GetServices<IConsumeFilter>();
            foreach (var globalFilter in globalFilters)
            {
                filters.Add(new GlobalConsumeFilterAdapter<TMessage>(globalFilter));
            }

            // Get message-specific consume filters
            var specificFilters = _serviceProvider.GetServices<IConsumeFilter<TMessage>>();
            filters.AddRange(specificFilters);

            // Get filters from options
            foreach (var filterType in _options.ConsumeFilters)
            {
                if (typeof(IConsumeFilter<TMessage>).IsAssignableFrom(filterType))
                {
                    var filter = (IConsumeFilter<TMessage>?)_serviceProvider.GetService(filterType)
                        ?? (IConsumeFilter<TMessage>?)Activator.CreateInstance(filterType);
                    if (filter != null)
                    {
                        filters.Add(filter);
                    }
                }
                else if (typeof(IConsumeFilter).IsAssignableFrom(filterType))
                {
                    var globalFilter = (IConsumeFilter?)_serviceProvider.GetService(filterType)
                        ?? (IConsumeFilter?)Activator.CreateInstance(filterType);
                    if (globalFilter != null)
                    {
                        filters.Add(new GlobalConsumeFilterAdapter<TMessage>(globalFilter));
                    }
                }
            }

            return filters.Distinct().ToList();
        }

        private List<IPublishFilter<TMessage>> GetPublishFilters<TMessage>() where TMessage : class
        {
            var filters = new List<IPublishFilter<TMessage>>();

            // Get global publish filters
            var globalFilters = _serviceProvider.GetServices<IPublishFilter>();
            foreach (var globalFilter in globalFilters)
            {
                filters.Add(new GlobalPublishFilterAdapter<TMessage>(globalFilter));
            }

            // Get message-specific publish filters
            var specificFilters = _serviceProvider.GetServices<IPublishFilter<TMessage>>();
            filters.AddRange(specificFilters);

            // Get filters from options
            foreach (var filterType in _options.PublishFilters)
            {
                if (typeof(IPublishFilter<TMessage>).IsAssignableFrom(filterType))
                {
                    var filter = (IPublishFilter<TMessage>?)_serviceProvider.GetService(filterType)
                        ?? (IPublishFilter<TMessage>?)Activator.CreateInstance(filterType);
                    if (filter != null)
                    {
                        filters.Add(filter);
                    }
                }
                else if (typeof(IPublishFilter).IsAssignableFrom(filterType))
                {
                    var globalFilter = (IPublishFilter?)_serviceProvider.GetService(filterType)
                        ?? (IPublishFilter?)Activator.CreateInstance(filterType);
                    if (globalFilter != null)
                    {
                        filters.Add(new GlobalPublishFilterAdapter<TMessage>(globalFilter));
                    }
                }
            }

            return filters.Distinct().ToList();
        }

        private List<ISendFilter<TMessage>> GetSendFilters<TMessage>() where TMessage : class
        {
            var filters = new List<ISendFilter<TMessage>>();

            // Get global send filters
            var globalFilters = _serviceProvider.GetServices<ISendFilter>();
            foreach (var globalFilter in globalFilters)
            {
                filters.Add(new GlobalSendFilterAdapter<TMessage>(globalFilter));
            }

            // Get message-specific send filters
            var specificFilters = _serviceProvider.GetServices<ISendFilter<TMessage>>();
            filters.AddRange(specificFilters);

            // Get filters from options
            foreach (var filterType in _options.SendFilters)
            {
                if (typeof(ISendFilter<TMessage>).IsAssignableFrom(filterType))
                {
                    var filter = (ISendFilter<TMessage>?)_serviceProvider.GetService(filterType)
                        ?? (ISendFilter<TMessage>?)Activator.CreateInstance(filterType);
                    if (filter != null)
                    {
                        filters.Add(filter);
                    }
                }
                else if (typeof(ISendFilter).IsAssignableFrom(filterType))
                {
                    var globalFilter = (ISendFilter?)_serviceProvider.GetService(filterType)
                        ?? (ISendFilter?)Activator.CreateInstance(filterType);
                    if (globalFilter != null)
                    {
                        filters.Add(new GlobalSendFilterAdapter<TMessage>(globalFilter));
                    }
                }
            }

            return filters.Distinct().ToList();
        }

        /// <summary>
        /// Adapter to use global consume filters with specific message types.
        /// </summary>
        private class GlobalConsumeFilterAdapter<TMessage> : IConsumeFilter<TMessage> where TMessage : class
        {
            private readonly IConsumeFilter _globalFilter;

            public GlobalConsumeFilterAdapter(IConsumeFilter globalFilter)
            {
                _globalFilter = globalFilter;
            }

            public Task ConsumeAsync(
                IConsumeFilterContext<TMessage> context,
                ConsumeFilterDelegate<TMessage> next,
                CancellationToken cancellationToken = default)
            {
                return _globalFilter.ConsumeAsync(context, next, cancellationToken);
            }
        }

        /// <summary>
        /// Adapter to use global publish filters with specific message types.
        /// </summary>
        private class GlobalPublishFilterAdapter<TMessage> : IPublishFilter<TMessage> where TMessage : class
        {
            private readonly IPublishFilter _globalFilter;

            public GlobalPublishFilterAdapter(IPublishFilter globalFilter)
            {
                _globalFilter = globalFilter;
            }

            public Task PublishAsync(
                IPublishFilterContext<TMessage> context,
                PublishFilterDelegate<TMessage> next,
                CancellationToken cancellationToken = default)
            {
                return _globalFilter.PublishAsync(context, next, cancellationToken);
            }
        }

        /// <summary>
        /// Adapter to use global send filters with specific message types.
        /// </summary>
        private class GlobalSendFilterAdapter<TMessage> : ISendFilter<TMessage> where TMessage : class
        {
            private readonly ISendFilter _globalFilter;

            public GlobalSendFilterAdapter(ISendFilter globalFilter)
            {
                _globalFilter = globalFilter;
            }

            public Task SendAsync(
                ISendFilterContext<TMessage> context,
                SendFilterDelegate<TMessage> next,
                CancellationToken cancellationToken = default)
            {
                return _globalFilter.SendAsync(context, next, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Interface for the filter pipeline executor.
    /// </summary>
    public interface IFilterPipelineExecutor
    {
        /// <summary>
        /// Executes consume filters for a message.
        /// </summary>
        Task ExecuteConsumeFiltersAsync<TMessage>(
            IConsumeFilterContext<TMessage> context,
            Func<IConsumeFilterContext<TMessage>, CancellationToken, Task> finalAction,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Executes publish filters for a message.
        /// </summary>
        Task ExecutePublishFiltersAsync<TMessage>(
            IPublishFilterContext<TMessage> context,
            Func<IPublishFilterContext<TMessage>, CancellationToken, Task> finalAction,
            CancellationToken cancellationToken = default) where TMessage : class;

        /// <summary>
        /// Executes send filters for a message.
        /// </summary>
        Task ExecuteSendFiltersAsync<TMessage>(
            ISendFilterContext<TMessage> context,
            Func<ISendFilterContext<TMessage>, CancellationToken, Task> finalAction,
            CancellationToken cancellationToken = default) where TMessage : class;
    }
}

