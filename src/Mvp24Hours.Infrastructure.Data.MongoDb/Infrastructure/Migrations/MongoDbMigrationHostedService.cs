//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations
{
    /// <summary>
    /// Background service that runs MongoDB migrations on application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service automatically applies pending migrations when the application starts.
    /// Configure using <see cref="MongoDbMigrationOptions.AutoMigrateOnStartup"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMongoDbMigrations(options =>
    /// {
    ///     options.MigrationAssemblies = new[] { typeof(MyMigration).Assembly };
    ///     options.AutoMigrateOnStartup = true;
    ///     options.FailOnMigrationError = true;
    /// });
    /// </code>
    /// </example>
    public sealed class MongoDbMigrationHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MongoDbMigrationOptions _options;
        private readonly ILogger<MongoDbMigrationHostedService>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbMigrationHostedService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="options">The migration options.</param>
        /// <param name="logger">The logger instance.</param>
        public MongoDbMigrationHostedService(
            IServiceProvider serviceProvider,
            IOptions<MongoDbMigrationOptions> options,
            ILogger<MongoDbMigrationHostedService>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.AutoMigrateOnStartup)
            {
                _logger?.LogDebug("Auto-migration on startup is disabled.");
                return;
            }

            // Delay startup if configured
            if (_options.StartupDelaySeconds > 0)
            {
                _logger?.LogInformation(
                    "Waiting {DelaySeconds}s before running migrations...",
                    _options.StartupDelaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(_options.StartupDelaySeconds), stoppingToken);
            }

            await RunMigrationsAsync(stoppingToken);
        }

        private async Task RunMigrationsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            try
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMongoDbMigrationRunner>();

                _logger?.LogInformation("Running MongoDB migrations on startup...");

                var result = await runner.MigrateAsync(cancellationToken);

                if (result.Success)
                {
                    if (result.MigrationsApplied > 0)
                    {
                        _logger?.LogInformation(
                            "Successfully applied {Count} migrations. Database version: {Version}",
                            result.MigrationsApplied,
                            result.EndVersion);
                    }
                    else
                    {
                        _logger?.LogInformation(
                            "No pending migrations. Database version: {Version}",
                            result.EndVersion);
                    }
                }
                else
                {
                    var message = $"Migration failed: {result.Error}";

                    if (_options.FailOnMigrationError)
                    {
                        _logger?.LogCritical(message);
                        throw new InvalidOperationException(message);
                    }

                    _logger?.LogError(message);
                }
            }
            catch (Exception ex) when (!_options.FailOnMigrationError)
            {
                _logger?.LogError(ex, "Migration failed but FailOnMigrationError is disabled. Continuing startup.");
            }
        }
    }
}

