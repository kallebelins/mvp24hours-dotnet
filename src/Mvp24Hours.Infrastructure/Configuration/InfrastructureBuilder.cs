//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Extensions;
using Mvp24Hours.Infrastructure.DistributedLocking.Extensions;
using Mvp24Hours.Infrastructure.Email.Extensions;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.FileStorage.Extensions;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.Http.Extensions;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Observability.Extensions;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Mvp24Hours.Infrastructure.Security.Extensions;
using Mvp24Hours.Infrastructure.Sms.Extensions;
using Mvp24Hours.Infrastructure.Sms.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Mvp24Hours.Infrastructure.Configuration
{
    /// <summary>
    /// Builder interface for configuring Infrastructure subsystems.
    /// </summary>
    public interface IInfrastructureBuilder
    {
        /// <summary>
        /// Gets the service collection.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        IConfiguration? Configuration { get; }

        /// <summary>
        /// Gets the infrastructure options.
        /// </summary>
        InfrastructureOptions Options { get; }

        /// <summary>
        /// Configures HTTP client services.
        /// </summary>
        IInfrastructureBuilder ConfigureHttp(Action<HttpClientOptions> configure);

        /// <summary>
        /// Configures email services.
        /// </summary>
        IInfrastructureBuilder ConfigureEmail(Action<EmailOptions> configure);

        /// <summary>
        /// Configures SMS services.
        /// </summary>
        IInfrastructureBuilder ConfigureSms(Action<SmsOptions> configure);

        /// <summary>
        /// Configures file storage services.
        /// </summary>
        IInfrastructureBuilder ConfigureFileStorage(Action<FileStorageOptions> configure);

        /// <summary>
        /// Configures distributed locking services.
        /// </summary>
        IInfrastructureBuilder ConfigureDistributedLocking(Action<IDistributedLockingBuilder> configure);

        /// <summary>
        /// Configures background jobs services.
        /// </summary>
        IInfrastructureBuilder ConfigureBackgroundJobs(Action<IBackgroundJobsBuilder> configure);

        /// <summary>
        /// Configures observability services.
        /// </summary>
        IInfrastructureBuilder ConfigureObservability(Action<ObservabilityOptions> configure);

        /// <summary>
        /// Configures resilience services.
        /// </summary>
        IInfrastructureBuilder ConfigureResilience(Action<ResilienceOptions> configure);

        /// <summary>
        /// Configures security/secret provider services.
        /// </summary>
        IInfrastructureBuilder ConfigureSecurity(Action<SecurityOptions> configure);
    }

    /// <summary>
    /// Builder implementation for configuring Infrastructure subsystems.
    /// </summary>
    internal class InfrastructureBuilder : IInfrastructureBuilder
    {
        private readonly InfrastructureOptions _options = new();

        public IServiceCollection Services { get; }
        public IConfiguration? Configuration { get; }
        public InfrastructureOptions Options => _options;

        public InfrastructureBuilder(IServiceCollection services, IConfiguration? configuration = null)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Configuration = configuration;
        }

        public IInfrastructureBuilder ConfigureHttp(Action<HttpClientOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _options.Http ??= new HttpClientOptions();
            configure(_options.Http);
            return this;
        }

        public IInfrastructureBuilder ConfigureEmail(Action<EmailOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _options.Email ??= new EmailOptions();
            configure(_options.Email);
            return this;
        }

        public IInfrastructureBuilder ConfigureSms(Action<SmsOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _options.Sms ??= new SmsOptions();
            configure(_options.Sms);
            return this;
        }

        public IInfrastructureBuilder ConfigureFileStorage(Action<FileStorageOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _options.FileStorage ??= new FileStorageOptions();
            configure(_options.FileStorage);
            return this;
        }

        public IInfrastructureBuilder ConfigureDistributedLocking(Action<IDistributedLockingBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // Store configuration for later execution
            _distributedLockingConfig = configure;
            return this;
        }

        public IInfrastructureBuilder ConfigureBackgroundJobs(Action<IBackgroundJobsBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            // Store configuration for later execution
            _backgroundJobsConfig = configure;
            return this;
        }

        public IInfrastructureBuilder ConfigureObservability(Action<ObservabilityOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _options.Observability ??= new ObservabilityOptions();
            configure(_options.Observability);
            return this;
        }

        public IInfrastructureBuilder ConfigureResilience(Action<ResilienceOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _options.Resilience ??= new ResilienceOptions();
            configure(_options.Resilience);
            return this;
        }

        public IInfrastructureBuilder ConfigureSecurity(Action<SecurityOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            _options.Security ??= new SecurityOptions();
            configure(_options.Security);
            return this;
        }

        // Store delegates for later execution
        private Action<IDistributedLockingBuilder>? _distributedLockingConfig;
        private Action<IBackgroundJobsBuilder>? _backgroundJobsConfig;

        /// <summary>
        /// Executes stored configurations that require builder pattern.
        /// </summary>
        internal void ExecuteBuilderConfigurations()
        {
            if (_distributedLockingConfig != null)
            {
                Services.AddDistributedLocking(_distributedLockingConfig);
            }

            if (_backgroundJobsConfig != null)
            {
                Services.AddBackgroundJobs(_backgroundJobsConfig);
            }
        }
    }
}

