using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.WebAPI.Extensions;
using Npgsql;
using WebStatus.Configuration;

namespace WebStatus.Extensions;

public static class ServiceBuilderExtensions
{
    public static IServiceCollection AddMyOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<HealthCatalogOptions>()
            .Bind(configuration.GetSection(HealthCatalogOptions.SectionName))
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddMyHealthCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        var connections = configuration.GetSection(ConnectionStringsOptions.SectionName)
            .Get<ConnectionStringsOptions>()
            ?? throw new InvalidOperationException("ConnectionStrings configuration is required.");

        var catalog = configuration.GetSection(HealthCatalogOptions.SectionName)
            .Get<HealthCatalogOptions>() ?? new HealthCatalogOptions();

        // Register RabbitMQ connection so AddMvp24HoursRabbitMQHealthCheck can resolve IMvpRabbitMQConnection.
        services.AddMvp24HoursRabbitMQ(
            Array.Empty<Type>(),
            connectionOptions => connectionOptions.ConnectionString = connections.RabbitMQ);

        services.AddMvp24HoursHealthChecks(options =>
        {
            options.EnableDetailedResponses = true;
            options.IncludeExceptionDetails = true;
            options.EnableUI = false;
        });

        services.AddHealthChecks()
            .AddMvp24HoursSqlServerCheck(
                connections.SqlServer,
                name: "sqlserver",
                tags: ["db", "database", "sqlserver", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddMvp24HoursPostgreSqlCheck<NpgsqlConnection>(
                connections.PostgreSql,
                name: "postgresql",
                tags: ["db", "database", "postgresql", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddMvp24HoursMySqlCheck<MySqlConnection>(
                connections.MySql,
                name: "mysql",
                tags: ["db", "database", "mysql", "ready"],
                timeout: TimeSpan.FromSeconds(5))
            .AddRedis(
                connections.Redis,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["cache", "redis", "ready"])
            .AddMongoDbHealthCheck(
                connectionString: connections.MongoDb,
                databaseName: catalog.MongoDatabaseName,
                name: "mongodb",
                tags: ["database", "mongodb", "ready"],
                timeout: TimeSpan.FromSeconds(5));

        // Mvp RabbitMQ check (separate registration that resolves IMvpRabbitMQConnection).
        services.AddMvp24HoursRabbitMQHealthCheck(
            name: "rabbitmq",
            tags: ["messaging", "rabbitmq", "ready"]);

        services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(15);
                setup.MaximumHistoryEntriesPerEndpoint(50);
                setup.AddHealthCheckEndpoint("mvp24hours-catalog", "/hc");
            })
            .AddInMemoryStorage();

        return services;
    }
}
