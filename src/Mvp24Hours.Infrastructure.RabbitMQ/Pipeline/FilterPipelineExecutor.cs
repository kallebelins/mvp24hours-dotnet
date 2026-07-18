//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;

/// <summary>
/// Executes filter pipelines for consume, publish, and send operations.
/// </summary>
/// <remarks>
/// Creates a new filter pipeline executor.
/// </remarks>
/// <param name="serviceProvider">The service provider for resolving filters.</param>
/// <param name="options">The filter pipeline options.</param>
public class FilterPipelineExecutor(IServiceProvider serviceProvider, FilterPipelineOptions options) : IFilterPipelineExecutor
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly FilterPipelineOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public async Task ExecuteConsumeFiltersAsync<TMessage>(
        IConsumeFilterContext<TMessage> context,
        Func<IConsumeFilterContext<TMessage>, CancellationToken, Task> finalAction,
        CancellationToken cancellationToken = default) where TMessage : class
    {
        List<IConsumeFilter<TMessage>> filters = GetConsumeFilters<TMessage>();

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
            IConsumeFilter<TMessage> filter = filters[i];
            ConsumeFilterDelegate<TMessage> next = pipeline;

            pipeline = async (ctx, ct) =>
            {
                if (ctx.ShouldSkipRemainingFilters)
                {
                    return;
                }

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
        List<IPublishFilter<TMessage>> filters = GetPublishFilters<TMessage>();

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
            IPublishFilter<TMessage> filter = filters[i];
            PublishFilterDelegate<TMessage> next = pipeline;

            pipeline = async (ctx, ct) =>
            {
                if (ctx.ShouldSkipRemainingFilters || ctx.ShouldCancelPublish)
                {
                    return;
                }

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
        List<ISendFilter<TMessage>> filters = GetSendFilters<TMessage>();

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
            ISendFilter<TMessage> filter = filters[i];
            SendFilterDelegate<TMessage> next = pipeline;

            pipeline = async (ctx, ct) =>
            {
                if (ctx.ShouldSkipRemainingFilters || ctx.ShouldCancelSend)
                {
                    return;
                }

                await filter.SendAsync(ctx, next, ct);
            };
        }

        await pipeline(context, cancellationToken);
    }

    private List<IConsumeFilter<TMessage>> GetConsumeFilters<TMessage>() where TMessage : class
    {
        var filters = new List<IConsumeFilter<TMessage>>();

        // Get global consume filters
        IEnumerable<IConsumeFilter> globalFilters = _serviceProvider.GetServices<IConsumeFilter>();
        foreach (IConsumeFilter globalFilter in globalFilters)
        {
            filters.Add(new GlobalConsumeFilterAdapter<TMessage>(globalFilter));
        }

        // Get message-specific consume filters
        IEnumerable<IConsumeFilter<TMessage>> specificFilters = _serviceProvider.GetServices<IConsumeFilter<TMessage>>();
        filters.AddRange(specificFilters);

        // Get filters from options
        foreach (Type filterType in _options.ConsumeFilters)
        {
            if (typeof(IConsumeFilter<TMessage>).IsAssignableFrom(filterType))
            {
                IConsumeFilter<TMessage>? filter = (IConsumeFilter<TMessage>?)_serviceProvider.GetService(filterType)
                    ?? (IConsumeFilter<TMessage>?)Activator.CreateInstance(filterType);
                if (filter != null)
                {
                    filters.Add(filter);
                }
            }
            else if (typeof(IConsumeFilter).IsAssignableFrom(filterType))
            {
                IConsumeFilter? globalFilter = (IConsumeFilter?)_serviceProvider.GetService(filterType)
                    ?? (IConsumeFilter?)Activator.CreateInstance(filterType);
                if (globalFilter != null)
                {
                    filters.Add(new GlobalConsumeFilterAdapter<TMessage>(globalFilter));
                }
            }
        }

        return [.. filters.Distinct()];
    }

    private List<IPublishFilter<TMessage>> GetPublishFilters<TMessage>() where TMessage : class
    {
        var filters = new List<IPublishFilter<TMessage>>();

        // Get global publish filters
        IEnumerable<IPublishFilter> globalFilters = _serviceProvider.GetServices<IPublishFilter>();
        foreach (IPublishFilter globalFilter in globalFilters)
        {
            filters.Add(new GlobalPublishFilterAdapter<TMessage>(globalFilter));
        }

        // Get message-specific publish filters
        IEnumerable<IPublishFilter<TMessage>> specificFilters = _serviceProvider.GetServices<IPublishFilter<TMessage>>();
        filters.AddRange(specificFilters);

        // Get filters from options
        foreach (Type filterType in _options.PublishFilters)
        {
            if (typeof(IPublishFilter<TMessage>).IsAssignableFrom(filterType))
            {
                IPublishFilter<TMessage>? filter = (IPublishFilter<TMessage>?)_serviceProvider.GetService(filterType)
                    ?? (IPublishFilter<TMessage>?)Activator.CreateInstance(filterType);
                if (filter != null)
                {
                    filters.Add(filter);
                }
            }
            else if (typeof(IPublishFilter).IsAssignableFrom(filterType))
            {
                IPublishFilter? globalFilter = (IPublishFilter?)_serviceProvider.GetService(filterType)
                    ?? (IPublishFilter?)Activator.CreateInstance(filterType);
                if (globalFilter != null)
                {
                    filters.Add(new GlobalPublishFilterAdapter<TMessage>(globalFilter));
                }
            }
        }

        return [.. filters.Distinct()];
    }

    private List<ISendFilter<TMessage>> GetSendFilters<TMessage>() where TMessage : class
    {
        var filters = new List<ISendFilter<TMessage>>();

        // Get global send filters
        IEnumerable<ISendFilter> globalFilters = _serviceProvider.GetServices<ISendFilter>();
        foreach (ISendFilter globalFilter in globalFilters)
        {
            filters.Add(new GlobalSendFilterAdapter<TMessage>(globalFilter));
        }

        // Get message-specific send filters
        IEnumerable<ISendFilter<TMessage>> specificFilters = _serviceProvider.GetServices<ISendFilter<TMessage>>();
        filters.AddRange(specificFilters);

        // Get filters from options
        foreach (Type filterType in _options.SendFilters)
        {
            if (typeof(ISendFilter<TMessage>).IsAssignableFrom(filterType))
            {
                ISendFilter<TMessage>? filter = (ISendFilter<TMessage>?)_serviceProvider.GetService(filterType)
                    ?? (ISendFilter<TMessage>?)Activator.CreateInstance(filterType);
                if (filter != null)
                {
                    filters.Add(filter);
                }
            }
            else if (typeof(ISendFilter).IsAssignableFrom(filterType))
            {
                ISendFilter? globalFilter = (ISendFilter?)_serviceProvider.GetService(filterType)
                    ?? (ISendFilter?)Activator.CreateInstance(filterType);
                if (globalFilter != null)
                {
                    filters.Add(new GlobalSendFilterAdapter<TMessage>(globalFilter));
                }
            }
        }

        return [.. filters.Distinct()];
    }

    /// <summary>
    /// Adapter to use global consume filters with specific message types.
    /// </summary>
    private class GlobalConsumeFilterAdapter<TMessage>(IConsumeFilter globalFilter) : IConsumeFilter<TMessage> where TMessage : class
    {
        private readonly IConsumeFilter _globalFilter = globalFilter;

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
    private class GlobalPublishFilterAdapter<TMessage>(IPublishFilter globalFilter) : IPublishFilter<TMessage> where TMessage : class
    {
        private readonly IPublishFilter _globalFilter = globalFilter;

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
    private class GlobalSendFilterAdapter<TMessage>(ISendFilter globalFilter) : ISendFilter<TMessage> where TMessage : class
    {
        private readonly ISendFilter _globalFilter = globalFilter;

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

