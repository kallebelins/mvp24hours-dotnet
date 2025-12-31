//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Mvp24Hours.Core.Aspire;

/// <summary>
/// Extension methods for integrating Aspire components with Mvp24Hours.
/// </summary>
/// <remarks>
/// <para>
/// .NET Aspire provides hosting integrations for popular services like Redis, RabbitMQ, SQL Server, and MongoDB.
/// These extensions help configure Mvp24Hours to work seamlessly with Aspire-hosted components.
/// </para>
/// <para>
/// <strong>Available components:</strong>
/// </para>
/// <list type="bullet">
///   <item>Redis (Aspire.Hosting.Redis) - For caching and distributed locking</item>
///   <item>RabbitMQ (Aspire.Hosting.RabbitMQ) - For messaging</item>
///   <item>SQL Server (Aspire.Hosting.SqlServer) - For relational data</item>
///   <item>PostgreSQL (Aspire.Hosting.PostgreSQL) - For relational data</item>
///   <item>MongoDB (Aspire.Hosting.MongoDB) - For document storage</item>
/// </list>
/// <para>
/// <strong>Usage in AppHost:</strong>
/// </para>
/// <code>
/// var builder = DistributedApplication.CreateBuilder(args);
/// 
/// // Add Aspire-hosted resources
/// var redis = builder.AddRedis("cache");
/// var rabbitmq = builder.AddRabbitMQ("messaging");
/// var sql = builder.AddSqlServer("sql").AddDatabase("mydb");
/// var mongo = builder.AddMongoDB("mongo").AddDatabase("mydb");
/// 
/// // Add your API project with references to components
/// builder.AddProject&lt;Projects.MyApi&gt;("api")
///     .WithReference(redis)
///     .WithReference(rabbitmq)
///     .WithReference(sql)
///     .WithReference(mongo);
/// 
/// builder.Build().Run();
/// </code>
/// </remarks>
public static class AspireComponentExtensions
{
    /// <summary>
    /// Configures Mvp24Hours to use Redis from Aspire connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The Aspire connection name (default: "cache").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the Redis configuration for use with Mvp24Hours caching.
    /// The connection string is automatically resolved from the Aspire configuration.
    /// </para>
    /// <para>
    /// <strong>Required packages:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item>Microsoft.Extensions.Caching.StackExchangeRedis</item>
    ///   <item>AspNetCore.HealthChecks.Redis (optional, for health checks)</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddMvp24HoursRedisFromAspire(
        this IServiceCollection services,
        string connectionName = "cache")
    {
        var options = new AspireRedisOptions { ConnectionName = connectionName };
        services.AddSingleton(options);
        
        return services;
    }

    /// <summary>
    /// Configures Mvp24Hours to use RabbitMQ from Aspire connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The Aspire connection name (default: "messaging").</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the RabbitMQ configuration for use with Mvp24Hours messaging.
    /// </para>
    /// <para>
    /// <strong>Required packages:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item>RabbitMQ.Client</item>
    ///   <item>AspNetCore.HealthChecks.RabbitMQ (optional, for health checks)</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddMvp24HoursRabbitMQFromAspire(
        this IServiceCollection services,
        string connectionName = "messaging",
        Action<AspireRabbitMQOptions>? configure = null)
    {
        var options = new AspireRabbitMQOptions { ConnectionName = connectionName };
        configure?.Invoke(options);
        services.AddSingleton(options);
        
        return services;
    }

    /// <summary>
    /// Configures Mvp24Hours to use SQL Server from Aspire connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The Aspire connection name (default: "sqldb").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the SQL Server configuration for use with Mvp24Hours EFCore.
    /// </para>
    /// <para>
    /// <strong>Required packages:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item>Microsoft.EntityFrameworkCore.SqlServer</item>
    ///   <item>AspNetCore.HealthChecks.SqlServer (optional, for health checks)</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddMvp24HoursSqlServerFromAspire(
        this IServiceCollection services,
        string connectionName = "sqldb")
    {
        var options = new AspireDatabaseOptions
        {
            ConnectionName = connectionName,
            DatabaseType = AspireDatabaseType.SqlServer
        };
        services.AddSingleton(options);
        
        return services;
    }

    /// <summary>
    /// Configures Mvp24Hours to use PostgreSQL from Aspire connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The Aspire connection name (default: "postgresdb").</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMvp24HoursPostgreSqlFromAspire(
        this IServiceCollection services,
        string connectionName = "postgresdb")
    {
        var options = new AspireDatabaseOptions
        {
            ConnectionName = connectionName,
            DatabaseType = AspireDatabaseType.PostgreSql
        };
        services.AddSingleton(options);
        
        return services;
    }

    /// <summary>
    /// Configures Mvp24Hours to use MongoDB from Aspire connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionName">The Aspire connection name (default: "mongodb").</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the MongoDB configuration for use with Mvp24Hours MongoDB module.
    /// </para>
    /// <para>
    /// <strong>Required packages:</strong>
    /// </para>
    /// <list type="bullet">
    ///   <item>MongoDB.Driver</item>
    ///   <item>AspNetCore.HealthChecks.MongoDb (optional, for health checks)</item>
    /// </list>
    /// </remarks>
    public static IServiceCollection AddMvp24HoursMongoDbFromAspire(
        this IServiceCollection services,
        string connectionName = "mongodb")
    {
        var options = new AspireDatabaseOptions
        {
            ConnectionName = connectionName,
            DatabaseType = AspireDatabaseType.MongoDB
        };
        services.AddSingleton(options);
        
        return services;
    }

    /// <summary>
    /// Gets the connection string for an Aspire component.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="connectionName">The Aspire connection name.</param>
    /// <returns>The connection string, or null if not found.</returns>
    /// <remarks>
    /// Aspire uses a naming convention for connection strings based on the resource name.
    /// This helper retrieves the connection string using that convention.
    /// </remarks>
    public static string? GetAspireConnectionString(
        this IHostApplicationBuilder builder,
        string connectionName)
    {
        return builder.Configuration.GetConnectionString(connectionName)
            ?? builder.Configuration[$"ConnectionStrings:{connectionName}"];
    }
}

/// <summary>
/// Options for Aspire Redis integration.
/// </summary>
public class AspireRedisOptions
{
    /// <summary>
    /// Gets or sets the Aspire connection name.
    /// </summary>
    public string ConnectionName { get; set; } = "cache";
    
    /// <summary>
    /// Gets or sets the instance name for Redis cache.
    /// </summary>
    public string? InstanceName { get; set; }
}

/// <summary>
/// Options for Aspire RabbitMQ integration.
/// </summary>
public class AspireRabbitMQOptions
{
    /// <summary>
    /// Gets or sets the Aspire connection name.
    /// </summary>
    public string ConnectionName { get; set; } = "messaging";

    /// <summary>
    /// Gets or sets whether to enable automatic queue declaration.
    /// </summary>
    public bool AutoDeclareQueues { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable message deduplication.
    /// </summary>
    public bool EnableMessageDeduplication { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefetch count for consumers.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;
}

/// <summary>
/// Options for Aspire database integration.
/// </summary>
public class AspireDatabaseOptions
{
    /// <summary>
    /// Gets or sets the Aspire connection name.
    /// </summary>
    public string ConnectionName { get; set; } = "database";

    /// <summary>
    /// Gets or sets the database type.
    /// </summary>
    public AspireDatabaseType DatabaseType { get; set; }

    /// <summary>
    /// Gets or sets whether to enable automatic migrations.
    /// </summary>
    public bool EnableAutoMigration { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable connection resiliency.
    /// </summary>
    public bool EnableResiliency { get; set; } = true;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Supported database types for Aspire integration.
/// </summary>
public enum AspireDatabaseType
{
    /// <summary>
    /// Microsoft SQL Server.
    /// </summary>
    SqlServer,

    /// <summary>
    /// PostgreSQL.
    /// </summary>
    PostgreSql,

    /// <summary>
    /// MySQL/MariaDB.
    /// </summary>
    MySql,

    /// <summary>
    /// MongoDB (document database).
    /// </summary>
    MongoDB
}
