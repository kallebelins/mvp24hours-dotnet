//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Security
{
    /// <summary>
    /// Helper class for configuring Row-Level Security (RLS) in databases.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Row-Level Security (RLS) is a database feature that allows you to control access to rows
    /// in a table based on the characteristics of the user executing a query.
    /// </para>
    /// <para>
    /// <strong>Supported Databases:</strong>
    /// <list type="bullet">
    /// <item><strong>SQL Server</strong> - Native RLS with security policies</item>
    /// <item><strong>PostgreSQL</strong> - Row Security Policies</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Benefits:</strong>
    /// <list type="bullet">
    /// <item>Security enforcement at the database level (defense in depth)</item>
    /// <item>Cannot be bypassed by application bugs</item>
    /// <item>Works with direct database access (reports, tools)</item>
    /// <item>Transparent to the application</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Considerations:</strong>
    /// <list type="bullet">
    /// <item>Requires database-level configuration (migrations or scripts)</item>
    /// <item>Performance impact should be tested</item>
    /// <item>Session context must be set before queries</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Generate SQL Server RLS scripts:
    /// var helper = new RowLevelSecurityHelper();
    /// var script = helper.GenerateSqlServerRls&lt;Product&gt;("dbo", "Products");
    /// await dbContext.Database.ExecuteSqlRawAsync(script);
    /// 
    /// // Set tenant context before queries:
    /// await helper.SetSqlServerTenantContextAsync(dbContext, "tenant123");
    /// </code>
    /// </example>
    public class RowLevelSecurityHelper
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly string _defaultSessionContextKey;
        private readonly ILogger<RowLevelSecurityHelper>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RowLevelSecurityHelper"/> class.
        /// </summary>
        /// <param name="tenantProvider">Optional tenant provider for automatic context setting.</param>
        /// <param name="sessionContextKey">The session context key for tenant ID. Default is "TenantId".</param>
        /// <param name="logger">Optional logger for telemetry.</param>
        public RowLevelSecurityHelper(
            ITenantProvider? tenantProvider = null,
            string sessionContextKey = "TenantId",
            ILogger<RowLevelSecurityHelper>? logger = null)
        {
            _tenantProvider = tenantProvider;
            _defaultSessionContextKey = sessionContextKey;
            _logger = logger;
        }

        #region SQL Server RLS

        /// <summary>
        /// Generates SQL Server Row-Level Security script for a tenant entity.
        /// </summary>
        /// <typeparam name="TEntity">The entity type implementing ITenantEntity.</typeparam>
        /// <param name="schema">The database schema. Default is "dbo".</param>
        /// <param name="tableName">The table name. If null, uses entity type name.</param>
        /// <returns>SQL script to create RLS policy.</returns>
        /// <remarks>
        /// <para>
        /// The generated script creates:
        /// <list type="bullet">
        /// <item>A security schema (if not exists)</item>
        /// <item>A predicate function that checks TenantId</item>
        /// <item>A security policy that applies the predicate</item>
        /// </list>
        /// </para>
        /// <para>
        /// <strong>Usage:</strong> Execute this script once during database setup or migration.
        /// </para>
        /// </remarks>
        public string GenerateSqlServerRls<TEntity>(
            string schema = "dbo",
            string tableName = null)
            where TEntity : class, ITenantEntity
        {
            tableName ??= typeof(TEntity).Name;
            return GenerateSqlServerRlsScript(schema, tableName, nameof(ITenantEntity.TenantId));
        }

        /// <summary>
        /// Generates SQL Server Row-Level Security script.
        /// </summary>
        /// <param name="schema">The database schema.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="tenantIdColumn">The tenant ID column name.</param>
        /// <returns>SQL script to create RLS policy.</returns>
        public string GenerateSqlServerRlsScript(
            string schema,
            string tableName,
            string tenantIdColumn = "TenantId")
        {
            var sb = new StringBuilder();

            // Create security schema if not exists
            sb.AppendLine("-- Create security schema");
            sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'Security')");
            sb.AppendLine("    EXEC('CREATE SCHEMA Security');");
            sb.AppendLine("GO");
            sb.AppendLine();

            // Create predicate function
            var functionName = $"fn_tenant_predicate_{tableName}";
            sb.AppendLine($"-- Create predicate function for {tableName}");
            sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.objects WHERE name = '{functionName}' AND type = 'IF')");
            sb.AppendLine($"    DROP FUNCTION Security.{functionName};");
            sb.AppendLine("GO");
            sb.AppendLine();
            sb.AppendLine($"CREATE FUNCTION Security.{functionName}(@{tenantIdColumn} NVARCHAR(50))");
            sb.AppendLine("RETURNS TABLE");
            sb.AppendLine("WITH SCHEMABINDING");
            sb.AppendLine("AS");
            sb.AppendLine("RETURN SELECT 1 AS Result");
            sb.AppendLine($"WHERE @{tenantIdColumn} = CONVERT(NVARCHAR(50), SESSION_CONTEXT(N'{_defaultSessionContextKey}'))");
            sb.AppendLine($"   OR SESSION_CONTEXT(N'{_defaultSessionContextKey}') IS NULL"); // Allow bypass when no context set
            sb.AppendLine("GO");
            sb.AppendLine();

            // Create security policy
            var policyName = $"TenantPolicy_{tableName}";
            sb.AppendLine($"-- Create security policy for {tableName}");
            sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = '{policyName}')");
            sb.AppendLine($"    DROP SECURITY POLICY [{schema}].[{policyName}];");
            sb.AppendLine("GO");
            sb.AppendLine();
            sb.AppendLine($"CREATE SECURITY POLICY [{schema}].[{policyName}]");
            sb.AppendLine($"ADD FILTER PREDICATE Security.{functionName}({tenantIdColumn})");
            sb.AppendLine($"    ON [{schema}].[{tableName}],");
            sb.AppendLine($"ADD BLOCK PREDICATE Security.{functionName}({tenantIdColumn})");
            sb.AppendLine($"    ON [{schema}].[{tableName}] AFTER INSERT,");
            sb.AppendLine($"ADD BLOCK PREDICATE Security.{functionName}({tenantIdColumn})");
            sb.AppendLine($"    ON [{schema}].[{tableName}] AFTER UPDATE");
            sb.AppendLine("WITH (STATE = ON);");
            sb.AppendLine("GO");

            return sb.ToString();
        }

        /// <summary>
        /// Sets the SQL Server session context for the current tenant.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID to set.</param>
        /// <remarks>
        /// Call this at the beginning of each request/operation to set the tenant context
        /// that RLS policies will use for filtering.
        /// </remarks>
        public void SetSqlServerTenantContext(DbContext context, string tenantId)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var sql = $"EXEC sp_set_session_context @key=N'{_defaultSessionContextKey}', @value=@tenantId";
            context.Database.ExecuteSqlRaw(sql, 
                new System.Data.SqlClient.SqlParameter("@tenantId", 
                    tenantId ?? (object)DBNull.Value));

            _logger?.LogDebug("SQL Server tenant context set. TenantId: {TenantId}", tenantId);
        }

        /// <summary>
        /// Sets the SQL Server session context asynchronously.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID to set.</param>
        public async System.Threading.Tasks.Task SetSqlServerTenantContextAsync(
            DbContext context,
            string tenantId,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var sql = $"EXEC sp_set_session_context @key=N'{_defaultSessionContextKey}', @value=@tenantId";
            await context.Database.ExecuteSqlRawAsync(sql,
                new[] { new System.Data.SqlClient.SqlParameter("@tenantId", 
                    tenantId ?? (object)DBNull.Value) },
                cancellationToken);

            _logger?.LogDebug("SQL Server tenant context set (async). TenantId: {TenantId}", tenantId);
        }

        /// <summary>
        /// Generates script to drop SQL Server RLS policy.
        /// </summary>
        /// <param name="schema">The database schema.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns>SQL script to drop RLS policy.</returns>
        public string GenerateSqlServerDropRlsScript(string schema, string tableName)
        {
            var sb = new StringBuilder();
            var policyName = $"TenantPolicy_{tableName}";
            var functionName = $"fn_tenant_predicate_{tableName}";

            sb.AppendLine($"-- Drop security policy for {tableName}");
            sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = '{policyName}')");
            sb.AppendLine($"    DROP SECURITY POLICY [{schema}].[{policyName}];");
            sb.AppendLine("GO");
            sb.AppendLine();
            sb.AppendLine($"-- Drop predicate function");
            sb.AppendLine($"IF EXISTS (SELECT 1 FROM sys.objects WHERE name = '{functionName}' AND type = 'IF')");
            sb.AppendLine($"    DROP FUNCTION Security.{functionName};");
            sb.AppendLine("GO");

            return sb.ToString();
        }

        #endregion

        #region PostgreSQL RLS

        /// <summary>
        /// Generates PostgreSQL Row Security Policy script for a tenant entity.
        /// </summary>
        /// <typeparam name="TEntity">The entity type implementing ITenantEntity.</typeparam>
        /// <param name="schema">The database schema. Default is "public".</param>
        /// <param name="tableName">The table name. If null, uses entity type name (lowercase).</param>
        /// <returns>SQL script to create RLS policy.</returns>
        public string GeneratePostgreSqlRls<TEntity>(
            string schema = "public",
            string tableName = null)
            where TEntity : class, ITenantEntity
        {
            tableName ??= typeof(TEntity).Name.ToLowerInvariant();
            return GeneratePostgreSqlRlsScript(schema, tableName, nameof(ITenantEntity.TenantId));
        }

        /// <summary>
        /// Generates PostgreSQL Row Security Policy script.
        /// </summary>
        /// <param name="schema">The database schema.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="tenantIdColumn">The tenant ID column name.</param>
        /// <returns>SQL script to create RLS policy.</returns>
        public string GeneratePostgreSqlRlsScript(
            string schema,
            string tableName,
            string tenantIdColumn = "TenantId")
        {
            var sb = new StringBuilder();
            var policyName = $"tenant_isolation_policy";

            // Enable RLS on the table
            sb.AppendLine($"-- Enable Row Level Security on {schema}.{tableName}");
            sb.AppendLine($"ALTER TABLE \"{schema}\".\"{tableName}\" ENABLE ROW LEVEL SECURITY;");
            sb.AppendLine();

            // Force RLS for table owner (optional, more secure)
            sb.AppendLine("-- Force RLS for table owner (optional)");
            sb.AppendLine($"-- ALTER TABLE \"{schema}\".\"{tableName}\" FORCE ROW LEVEL SECURITY;");
            sb.AppendLine();

            // Drop existing policy if exists
            sb.AppendLine("-- Drop existing policy if exists");
            sb.AppendLine($"DROP POLICY IF EXISTS {policyName} ON \"{schema}\".\"{tableName}\";");
            sb.AppendLine();

            // Create policy
            sb.AppendLine("-- Create tenant isolation policy");
            sb.AppendLine($"CREATE POLICY {policyName} ON \"{schema}\".\"{tableName}\"");
            sb.AppendLine("    FOR ALL");
            sb.AppendLine("    USING (");
            sb.AppendLine($"        \"{tenantIdColumn}\" = current_setting('app.current_tenant_id', TRUE)");
            sb.AppendLine($"        OR current_setting('app.current_tenant_id', TRUE) IS NULL"); // Bypass when not set
            sb.AppendLine("    )");
            sb.AppendLine("    WITH CHECK (");
            sb.AppendLine($"        \"{tenantIdColumn}\" = current_setting('app.current_tenant_id', TRUE)");
            sb.AppendLine($"        OR current_setting('app.current_tenant_id', TRUE) IS NULL");
            sb.AppendLine("    );");

            return sb.ToString();
        }

        /// <summary>
        /// Sets the PostgreSQL session context for the current tenant.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID to set.</param>
        public void SetPostgreSqlTenantContext(DbContext context, string tenantId)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var sql = tenantId != null
                ? $"SET app.current_tenant_id = '{tenantId}'"
                : "RESET app.current_tenant_id";
            context.Database.ExecuteSqlRaw(sql);

            _logger?.LogDebug("PostgreSQL tenant context set. TenantId: {TenantId}", tenantId);
        }

        /// <summary>
        /// Sets the PostgreSQL session context asynchronously.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID to set.</param>
        public async System.Threading.Tasks.Task SetPostgreSqlTenantContextAsync(
            DbContext context,
            string tenantId,
            System.Threading.CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var sql = tenantId != null
                ? $"SET app.current_tenant_id = '{tenantId}'"
                : "RESET app.current_tenant_id";
            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);

            _logger?.LogDebug("PostgreSQL tenant context set (async). TenantId: {TenantId}", tenantId);
        }

        /// <summary>
        /// Generates script to drop PostgreSQL RLS policy.
        /// </summary>
        /// <param name="schema">The database schema.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns>SQL script to drop RLS policy and disable RLS.</returns>
        public string GeneratePostgreSqlDropRlsScript(string schema, string tableName)
        {
            var sb = new StringBuilder();
            var policyName = $"tenant_isolation_policy";

            sb.AppendLine($"-- Drop tenant policy for {schema}.{tableName}");
            sb.AppendLine($"DROP POLICY IF EXISTS {policyName} ON \"{schema}\".\"{tableName}\";");
            sb.AppendLine();
            sb.AppendLine("-- Disable Row Level Security");
            sb.AppendLine($"ALTER TABLE \"{schema}\".\"{tableName}\" DISABLE ROW LEVEL SECURITY;");

            return sb.ToString();
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Generates RLS scripts for all tenant entities in the model.
        /// </summary>
        /// <param name="modelBuilder">The model builder with entity configurations.</param>
        /// <param name="databaseType">The target database type.</param>
        /// <param name="schema">The database schema.</param>
        /// <returns>Dictionary of table names and their RLS scripts.</returns>
        public Dictionary<string, string> GenerateRlsScriptsForModel(
            ModelBuilder modelBuilder,
            DatabaseType databaseType,
            string schema = null)
        {
            var scripts = new Dictionary<string, string>();
            schema ??= databaseType == DatabaseType.PostgreSql ? "public" : "dbo";

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var tableName = entityType.GetTableName();
                    var entitySchema = entityType.GetSchema() ?? schema;

                    var script = databaseType switch
                    {
                        DatabaseType.SqlServer => GenerateSqlServerRlsScript(
                            entitySchema, tableName, nameof(ITenantEntity.TenantId)),
                        DatabaseType.PostgreSql => GeneratePostgreSqlRlsScript(
                            entitySchema, tableName, nameof(ITenantEntity.TenantId)),
                        _ => throw new NotSupportedException($"Database type {databaseType} is not supported for RLS.")
                    };

                    scripts[$"{entitySchema}.{tableName}"] = script;
                }
            }

            return scripts;
        }

        /// <summary>
        /// Generates a combined RLS script for all tenant entities.
        /// </summary>
        /// <param name="modelBuilder">The model builder with entity configurations.</param>
        /// <param name="databaseType">The target database type.</param>
        /// <param name="schema">The database schema.</param>
        /// <returns>Combined SQL script for all tenant entities.</returns>
        public string GenerateCombinedRlsScript(
            ModelBuilder modelBuilder,
            DatabaseType databaseType,
            string schema = null)
        {
            var scripts = GenerateRlsScriptsForModel(modelBuilder, databaseType, schema);
            var sb = new StringBuilder();

            sb.AppendLine("-- Row-Level Security Configuration");
            sb.AppendLine($"-- Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"-- Database Type: {databaseType}");
            sb.AppendLine("-- =========================================");
            sb.AppendLine();

            foreach (var kvp in scripts)
            {
                sb.AppendLine($"-- Table: {kvp.Key}");
                sb.AppendLine(kvp.Value);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Supported database types for RLS.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// Microsoft SQL Server
        /// </summary>
        SqlServer,

        /// <summary>
        /// PostgreSQL
        /// </summary>
        PostgreSql
    }

    /// <summary>
    /// Extension methods for DbContext to set tenant context for RLS.
    /// </summary>
    public static class RowLevelSecurityDbContextExtensions
    {
        /// <summary>
        /// Sets the tenant context for SQL Server RLS.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="sessionKey">The session context key. Default is "TenantId".</param>
        public static void SetTenantContextForSqlServer(
            this DbContext context,
            string tenantId,
            string sessionKey = "TenantId")
        {
            var helper = new RowLevelSecurityHelper(sessionContextKey: sessionKey);
            helper.SetSqlServerTenantContext(context, tenantId);
        }

        /// <summary>
        /// Sets the tenant context for PostgreSQL RLS.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID.</param>
        public static void SetTenantContextForPostgreSql(
            this DbContext context,
            string tenantId)
        {
            var helper = new RowLevelSecurityHelper();
            helper.SetPostgreSqlTenantContext(context, tenantId);
        }

        /// <summary>
        /// Sets the tenant context asynchronously for SQL Server RLS.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="sessionKey">The session context key. Default is "TenantId".</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async System.Threading.Tasks.Task SetTenantContextForSqlServerAsync(
            this DbContext context,
            string tenantId,
            string sessionKey = "TenantId",
            System.Threading.CancellationToken cancellationToken = default)
        {
            var helper = new RowLevelSecurityHelper(sessionContextKey: sessionKey);
            await helper.SetSqlServerTenantContextAsync(context, tenantId, cancellationToken);
        }

        /// <summary>
        /// Sets the tenant context asynchronously for PostgreSQL RLS.
        /// </summary>
        /// <param name="context">The DbContext.</param>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async System.Threading.Tasks.Task SetTenantContextForPostgreSqlAsync(
            this DbContext context,
            string tenantId,
            System.Threading.CancellationToken cancellationToken = default)
        {
            var helper = new RowLevelSecurityHelper();
            await helper.SetPostgreSqlTenantContextAsync(context, tenantId, cancellationToken);
        }
    }
}

