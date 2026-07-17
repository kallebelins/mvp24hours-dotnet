//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;

namespace Mvp24Hours.Infrastructure.RabbitMQ
{
    /// <summary>
    /// 
    /// </summary>
    public class MvpRabbitMQHostedService : IHostedService
    {
        private readonly TimerCallback callback;
        private readonly object? state;
        private readonly TimeSpan dueTime;
        private readonly TimeSpan period;

        /// <summary>
        /// 
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public MvpRabbitMQHostedService(IOptions<RabbitMQHostedOptions> options)
            : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public MvpRabbitMQHostedService(RabbitMQHostedOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options), "Options is required.");
            }
            this.callback = options.Callback;
            this.state = options.State;
            this.dueTime = options.DueTime;
            this.period = options.Period;
        }

        /// <summary>
        /// 
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = new Timer(callback, state, dueTime, period);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
