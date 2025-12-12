//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation
{
    /// <summary>
    /// Default implementation of schema validator.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    public class SchemaValidator<TContext> : ISchemaValidator where TContext : DbContext
    {
        private readonly TContext _dbContext;
        private readonly SchemaValidationOptions _options;
        private readonly ILogger<SchemaValidator<TContext>> _logger;
        
        private SchemaValidationResult? _cachedResult;
        private DateTime? _cacheExpiry;

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaValidator{TContext}"/> class.
        /// </summary>
        public SchemaValidator(
            TContext dbContext,
            IOptions<SchemaValidationOptions> options,
            ILogger<SchemaValidator<TContext>> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _options = options?.Value ?? new SchemaValidationOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<SchemaValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "schemavalidator-validate-start");

            // Check cache
            if (_options.CacheValidationResults && 
                _cachedResult != null && 
                _cacheExpiry.HasValue && 
                DateTime.UtcNow < _cacheExpiry.Value)
            {
                _logger.LogDebug("Returning cached schema validation result for {DbContext}", typeof(TContext).Name);
                return _cachedResult;
            }

            var stopwatch = Stopwatch.StartNew();
            var issues = new List<SchemaIssue>();
            var warnings = new List<string>();
            var pendingMigrations = new List<string>();

            try
            {
                // Step 1: Validate connectivity
                if (!await ValidateConnectivityAsync(cancellationToken))
                {
                    issues.Add(new SchemaIssue
                    {
                        Severity = IssueSeverity.Critical,
                        Type = IssueType.ConnectionFailed,
                        ObjectName = typeof(TContext).Name,
                        Description = "Cannot connect to database"
                    });

                    return CreateResult(issues, warnings, pendingMigrations, stopwatch.Elapsed, false);
                }

                // Step 2: Check pending migrations
                if (_options.CheckPendingMigrations)
                {
                    var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
                    pendingMigrations.AddRange(pending);

                    if (pendingMigrations.Count > 0)
                    {
                        _logger.LogWarning(
                            "Database has {Count} pending migrations: {Migrations}",
                            pendingMigrations.Count,
                            string.Join(", ", pendingMigrations));

                        foreach (var migration in pendingMigrations)
                        {
                            issues.Add(new SchemaIssue
                            {
                                Severity = IssueSeverity.Warning,
                                Type = IssueType.PendingMigration,
                                ObjectName = migration,
                                Description = $"Migration '{migration}' has not been applied",
                                SuggestedFix = "Run database migrations"
                            });
                        }
                    }
                }

                // Step 3: Validate tables exist
                if (_options.ValidateTables)
                {
                    await ValidateTablesAsync(issues, warnings, cancellationToken);
                }

                // Step 4: Validate columns (if enabled)
                if (_options.ValidateColumns)
                {
                    await ValidateColumnsAsync(issues, warnings, cancellationToken);
                }

                stopwatch.Stop();

                var result = CreateResult(issues, warnings, pendingMigrations, stopwatch.Elapsed, issues.Count == 0);

                // Cache result
                if (_options.CacheValidationResults)
                {
                    _cachedResult = result;
                    _cacheExpiry = DateTime.UtcNow.Add(_options.CacheDuration);
                }

                if (result.IsValid)
                {
                    _logger.LogInformation(
                        "Schema validation passed for {DbContext} in {Duration}ms",
                        typeof(TContext).Name,
                        stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(
                        "Schema validation found {IssueCount} issues for {DbContext} in {Duration}ms",
                        issues.Count,
                        typeof(TContext).Name,
                        stopwatch.ElapsedMilliseconds);
                }

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "schemavalidator-validate-complete");

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Schema validation failed for {DbContext}", typeof(TContext).Name);

                issues.Add(new SchemaIssue
                {
                    Severity = IssueSeverity.Critical,
                    Type = IssueType.Other,
                    ObjectName = typeof(TContext).Name,
                    Description = $"Schema validation error: {ex.Message}"
                });

                return CreateResult(issues, warnings, pendingMigrations, stopwatch.Elapsed, false);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateConnectivityAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.Database.CanConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connectivity check failed for {DbContext}", typeof(TContext).Name);
                return false;
            }
        }

        /// <inheritdoc/>
        public ModelSummary GetModelSummary()
        {
            var model = _dbContext.Model;
            var entityTypes = model.GetEntityTypes().ToList();
            var tables = entityTypes
                .Select(e => e.GetTableName())
                .Where(t => t != null)
                .Distinct()
                .ToList();

            var appliedMigrations = _dbContext.Database
                .GetAppliedMigrations()
                .ToList();

            return new ModelSummary
            {
                ContextType = typeof(TContext).Name,
                ProviderName = _dbContext.Database.ProviderName ?? "Unknown",
                EntityCount = entityTypes.Count,
                EntityTypes = entityTypes.Select(e => e.ClrType.Name).ToList(),
                TableCount = tables.Count,
                Tables = tables!,
                AppliedMigrationCount = appliedMigrations.Count,
                AppliedMigrations = appliedMigrations
            };
        }

        private async Task ValidateTablesAsync(
            List<SchemaIssue> issues,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            var model = _dbContext.Model;
            var entityTypes = model.GetEntityTypes();

            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                var schema = entityType.GetSchema();

                if (string.IsNullOrEmpty(tableName))
                    continue;

                if (_options.ExcludedTables.Contains(tableName))
                    continue;

                try
                {
                    var tableExists = await CheckTableExistsAsync(tableName, schema, cancellationToken);
                    
                    if (!tableExists)
                    {
                        issues.Add(new SchemaIssue
                        {
                            Severity = IssueSeverity.Error,
                            Type = IssueType.MissingTable,
                            ObjectName = tableName,
                            Description = $"Table '{tableName}' does not exist in database",
                            Expected = "Table should exist",
                            Actual = "Table not found",
                            SuggestedFix = "Run database migrations or create the table manually"
                        });
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Could not validate table '{tableName}': {ex.Message}");
                }
            }
        }

        private async Task ValidateColumnsAsync(
            List<SchemaIssue> issues,
            List<string> warnings,
            CancellationToken cancellationToken)
        {
            var model = _dbContext.Model;
            var entityTypes = model.GetEntityTypes();

            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                if (string.IsNullOrEmpty(tableName) || _options.ExcludedTables.Contains(tableName))
                    continue;

                var properties = entityType.GetProperties();

                foreach (var property in properties)
                {
                    var columnName = property.GetColumnName();
                    if (string.IsNullOrEmpty(columnName))
                        continue;

                    try
                    {
                        var columnExists = await CheckColumnExistsAsync(tableName, columnName, cancellationToken);
                        
                        if (!columnExists)
                        {
                            issues.Add(new SchemaIssue
                            {
                                Severity = IssueSeverity.Error,
                                Type = IssueType.MissingColumn,
                                ObjectName = $"{tableName}.{columnName}",
                                Description = $"Column '{columnName}' does not exist in table '{tableName}'",
                                SuggestedFix = "Run database migrations or add the column manually"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Could not validate column '{tableName}.{columnName}': {ex.Message}");
                    }
                }
            }
        }

        private async Task<bool> CheckTableExistsAsync(string tableName, string? schema, CancellationToken cancellationToken)
        {
            var connection = _dbContext.Database.GetDbConnection();
            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandTimeout = (int)_options.ValidationTimeout.TotalSeconds;

            // SQL Server syntax - adjust for other databases
            var schemaFilter = string.IsNullOrEmpty(schema) ? "dbo" : schema;
            command.CommandText = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @TableName
                ) THEN 1 ELSE 0 END";

            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "@Schema";
            schemaParam.Value = schemaFilter;
            command.Parameters.Add(schemaParam);

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@TableName";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }

        private async Task<bool> CheckColumnExistsAsync(string tableName, string columnName, CancellationToken cancellationToken)
        {
            var connection = _dbContext.Database.GetDbConnection();
            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandTimeout = (int)_options.ValidationTimeout.TotalSeconds;

            command.CommandText = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName
                ) THEN 1 ELSE 0 END";

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@TableName";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);

            var columnParam = command.CreateParameter();
            columnParam.ParameterName = "@ColumnName";
            columnParam.Value = columnName;
            command.Parameters.Add(columnParam);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) == 1;
        }

        private static SchemaValidationResult CreateResult(
            List<SchemaIssue> issues,
            List<string> warnings,
            List<string> pendingMigrations,
            TimeSpan duration,
            bool isValid)
        {
            return new SchemaValidationResult
            {
                IsValid = isValid && !issues.Any(i => i.Severity >= IssueSeverity.Error),
                Issues = issues,
                Warnings = warnings,
                HasPendingMigrations = pendingMigrations.Count > 0,
                PendingMigrations = pendingMigrations,
                Duration = duration
            };
        }
    }
}

