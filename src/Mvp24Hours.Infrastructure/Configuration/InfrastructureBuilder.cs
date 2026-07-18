//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.BackgroundJobs.Extensions;
using Mvp24Hours.Infrastructure.DistributedLocking.Extensions;
using Mvp24Hours.Infrastructure.Email.Options;
using Mvp24Hours.Infrastructure.FileStorage.Options;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Observability.Extensions;
using Mvp24Hours.Infrastructure.Sms.Options;

namespace Mvp24Hours.Infrastructure.Configuration;

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
internal class InfrastructureBuilder(IServiceCollection services, IConfiguration? configuration = null) : IInfrastructureBuilder
{
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
    public IConfiguration? Configuration { get; } = configuration;
    public InfrastructureOptions Options { get; } = new();

    public IInfrastructureBuilder ConfigureHttp(Action<HttpClientOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Options.Http ??= new HttpClientOptions();
        configure(Options.Http);
        return this;
    }

    public IInfrastructureBuilder ConfigureEmail(Action<EmailOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Options.Email ??= new EmailOptions();
        configure(Options.Email);
        return this;
    }

    public IInfrastructureBuilder ConfigureSms(Action<SmsOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Options.Sms ??= new SmsOptions();
        configure(Options.Sms);
        return this;
    }

    public IInfrastructureBuilder ConfigureFileStorage(Action<FileStorageOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Options.FileStorage ??= new FileStorageOptions();
        configure(Options.FileStorage);
        return this;
    }

    public IInfrastructureBuilder ConfigureDistributedLocking(Action<IDistributedLockingBuilder> configure)
    {

        // Store configuration for later execution
        _distributedLockingConfig = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    public IInfrastructureBuilder ConfigureBackgroundJobs(Action<IBackgroundJobsBuilder> configure)
    {

        // Store configuration for later execution
        _backgroundJobsConfig = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    public IInfrastructureBuilder ConfigureObservability(Action<ObservabilityOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Options.Observability ??= new ObservabilityOptions();
        configure(Options.Observability);
        return this;
    }

    public IInfrastructureBuilder ConfigureResilience(Action<ResilienceOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Options.Resilience ??= new ResilienceOptions();
        configure(Options.Resilience);
        return this;
    }

    public IInfrastructureBuilder ConfigureSecurity(Action<SecurityOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        Options.Security ??= new SecurityOptions();
        configure(Options.Security);
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

