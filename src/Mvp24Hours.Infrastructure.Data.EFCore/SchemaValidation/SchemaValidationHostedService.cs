//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation
{
    /// <summary>
    /// Background service that validates database schema on application startup.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    public class SchemaValidationHostedService<TContext> : IHostedService
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SchemaValidationOptions _options;
        private readonly ILogger<SchemaValidationHostedService<TContext>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaValidationHostedService{TContext}"/> class.
        /// </summary>
        public SchemaValidationHostedService(
            IServiceProvider serviceProvider,
            IOptions<SchemaValidationOptions> options,
            ILogger<SchemaValidationHostedService<TContext>> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? new SchemaValidationOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.ValidateOnStartup)
            {
                _logger.LogDebug("Schema validation on startup is disabled for {DbContext}", typeof(TContext).Name);
                return;
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "schemavalidationhostedservice-startasync-start");

            _logger.LogInformation("Starting schema validation for {DbContext}", typeof(TContext).Name);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var validator = scope.ServiceProvider.GetRequiredService<ISchemaValidator>();

                // Log model summary
                if (_options.EnableDetailedLogging)
                {
                    var summary = validator.GetModelSummary();
                    _logger.LogInformation(
                        "Model summary for {DbContext}: {EntityCount} entities, {TableCount} tables, {MigrationCount} applied migrations",
                        typeof(TContext).Name,
                        summary.EntityCount,
                        summary.TableCount,
                        summary.AppliedMigrationCount);
                }

                // Run validation
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.ValidationTimeout);

                var result = await validator.ValidateAsync(cts.Token);

                // Process results
                if (result.IsValid)
                {
                    _logger.LogInformation(
                        "Schema validation passed for {DbContext} in {Duration}ms",
                        typeof(TContext).Name,
                        result.Duration.TotalMilliseconds);
                }
                else
                {
                    var criticalIssues = result.Issues.Where(i => i.Severity >= IssueSeverity.Error).ToList();
                    var warningIssues = result.Issues.Where(i => i.Severity == IssueSeverity.Warning).ToList();

                    foreach (var issue in result.Issues)
                    {
                        var logLevel = issue.Severity switch
                        {
                            IssueSeverity.Critical => LogLevel.Critical,
                            IssueSeverity.Error => LogLevel.Error,
                            IssueSeverity.Warning => LogLevel.Warning,
                            _ => LogLevel.Information
                        };

                        _logger.Log(
                            logLevel,
                            "[{Severity}] {Type}: {Description} (Object: {ObjectName})",
                            issue.Severity,
                            issue.Type,
                            issue.Description,
                            issue.ObjectName);
                    }

                    if (_options.ThrowOnValidationFailure && criticalIssues.Count > 0)
                    {
                        throw new SchemaValidationException(
                            $"Schema validation failed for {typeof(TContext).Name} with {criticalIssues.Count} critical issues",
                            result);
                    }

                    _logger.LogWarning(
                        "Schema validation completed with issues for {DbContext}: {CriticalCount} critical, {WarningCount} warnings in {Duration}ms",
                        typeof(TContext).Name,
                        criticalIssues.Count,
                        warningIssues.Count,
                        result.Duration.TotalMilliseconds);
                }

                // Log pending migrations warning
                if (result.HasPendingMigrations)
                {
                    _logger.LogWarning(
                        "Database {DbContext} has {Count} pending migrations: {Migrations}",
                        typeof(TContext).Name,
                        result.PendingMigrations.Count,
                        string.Join(", ", result.PendingMigrations));
                }

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "schemavalidationhostedservice-startasync-complete");
            }
            catch (SchemaValidationException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Schema validation timed out for {DbContext}", typeof(TContext).Name);
                
                if (_options.ThrowOnValidationFailure)
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Schema validation failed for {DbContext}", typeof(TContext).Name);
                TelemetryHelper.Execute(TelemetryLevels.Error, "schemavalidationhostedservice-startasync-error", ex.Message);

                if (_options.ThrowOnValidationFailure)
                {
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Schema validation service stopping for {DbContext}", typeof(TContext).Name);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Exception thrown when schema validation fails.
    /// </summary>
    public class SchemaValidationException : Exception
    {
        /// <summary>
        /// Gets the validation result.
        /// </summary>
        public SchemaValidationResult ValidationResult { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaValidationException"/> class.
        /// </summary>
        public SchemaValidationException(string message, SchemaValidationResult validationResult)
            : base(message)
        {
            ValidationResult = validationResult;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaValidationException"/> class.
        /// </summary>
        public SchemaValidationException(string message, SchemaValidationResult validationResult, Exception innerException)
            : base(message, innerException)
        {
            ValidationResult = validationResult;
        }
    }
}

