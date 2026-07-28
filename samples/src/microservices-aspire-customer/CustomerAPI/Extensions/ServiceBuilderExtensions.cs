using CustomerAPI.Data;
using CustomerAPI.Services;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;

namespace CustomerAPI.Extensions;

/// <summary>
/// Extension methods for registering CustomerAPI services.
/// </summary>
public static class ServiceBuilderExtensions
{
    /// <summary>
    /// Registers EF Core DbContext.
    /// <para>
    /// Aspire injects the SQL Server connection string via the environment variable
    /// <c>ConnectionStrings__MyAspireCustomerDb</c> when running with AppHost.
    /// Falls back to in-memory database for standalone/CI runs.
    /// </para>
    /// </summary>
    public static IServiceCollection AddCustomerDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MyAspireCustomerDb");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<CustomerDbContext>(options =>
                options.UseSqlServer(connectionString));
        }
        else
        {
            services.AddDbContext<CustomerDbContext>(options =>
                options.UseInMemoryDatabase("MyAspireCustomerDb"));
        }

        return services;
    }

    /// <summary>
    /// Registers application services.
    /// </summary>
    public static IServiceCollection AddCustomerServices(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        return services;
    }

    /// <summary>
    /// Configures the RabbitMQ client for publishing integration events.
    /// <para>
    /// Aspire injects the RabbitMQ connection string via
    /// <c>ConnectionStrings__messaging</c> when running with AppHost.
    /// Falls back to localhost for standalone development.
    /// </para>
    /// </summary>
    public static IServiceCollection AddCustomerMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var rabbitConnectionString = configuration.GetConnectionString("messaging")
            ?? "amqp://guest:guest@localhost:5672";

        services.AddMvpRabbitMQ(rabbit =>
        {
            rabbit.Host(rabbitConnectionString, connection =>
            {
                connection.DispatchConsumersAsync(true);
                connection.RetryCount(3);
            });

            // Publisher-only: no consumers registered in this service.
            rabbit.ConfigureClient(clientOptions =>
            {
                clientOptions.Exchange = "amq.direct";
                clientOptions.MaxRedeliveredCount = 1;
            });
        });

        return services;
    }

    /// <summary>
    /// Registers health checks for SQL Server and RabbitMQ.
    /// Falls back gracefully when connection strings are absent (in-memory mode).
    /// </summary>
    public static IServiceCollection AddCustomerHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = services.AddHealthChecks();

        var sqlCs = configuration.GetConnectionString("MyAspireCustomerDb");
        if (!string.IsNullOrWhiteSpace(sqlCs))
        {
            builder.AddSqlServer(
                sqlCs,
                healthQuery: "SELECT 1;",
                name: "SqlServer-CustomerDb",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["ready"]);
        }

        var rabbitCs = configuration.GetConnectionString("messaging");
        if (!string.IsNullOrWhiteSpace(rabbitCs))
        {
            builder.AddRabbitMQ(
                rabbitCs,
                name: "RabbitMQ",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: ["ready"]);
        }

        return services;
    }
}
