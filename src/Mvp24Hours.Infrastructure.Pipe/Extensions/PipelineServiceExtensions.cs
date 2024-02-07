//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Configuration;
using System;

namespace Mvp24Hours.Extensions
{
    public static class PipelineServiceExtensions
    {
        /// <summary>
        /// Add pipeline engine
        /// </summary>
        public static IServiceCollection AddMvp24HoursPipeline(this IServiceCollection services,
            Action<PipelineOptions> options = null,
            Func<IServiceProvider, IPipeline> factory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<PipelineOptions>(options =>
                {
                    options.IsBreakOnFail = false;
                });
            }

            if (factory != null)
            {
                services.Add(new ServiceDescriptor(typeof(IPipeline), factory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IPipeline), typeof(Pipeline), lifetime));
            }

            return services;
        }

        /// <summary>
        /// Add pipeline engine async
        /// </summary>
        public static IServiceCollection AddMvp24HoursPipelineAsync(this IServiceCollection services,
            Action<PipelineAsyncOptions> options = null,
            Func<IServiceProvider, IPipelineAsync> factory = null,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            if (options != null)
            {
                services.Configure(options);
            }
            else
            {
                services.Configure<PipelineAsyncOptions>(options =>
                {
                    options.IsBreakOnFail = false;
                });
            }

            if (factory != null)
            {
                services.Add(new ServiceDescriptor(typeof(IPipelineAsync), factory, lifetime));
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IPipelineAsync), typeof(PipelineAsync), lifetime));
            }

            return services;
        }
    }
}
